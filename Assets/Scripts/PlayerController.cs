using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerController : MonoBehaviour
{
	Rigidbody2D rb;
	Vector2 velocity;
	float lastXDir = 0; // Keep track of what the player input is LAST frame to track changes
	public float maxSpeed = 3f;
	public float accelerationTime = 0.3f;
	public float decelerationTime = 0.5f;
	public float turnTime = 0.1f;
	public float surfaceDistance = 1.01f;
	public float terminalVelocity = 7f;

	public float jumpApexHeight, jumpApexTime, minTimeBetweenJumps;

	float timer, lastGroundedTime, lastNotGroundedTime;
	public float coyoteTime;

	float currentSpeed = 0f;
    Vector2 colliderSize;
	bool isJumping = false;

	bool canMoveHorizontally = true;

	ParticleSystem particles;
	Transform plrVisuals;

    public enum FacingDirection {
		left, right
	}

	public void Start() {
		rb = GetComponent<Rigidbody2D>();
		colliderSize = GetComponent<BoxCollider2D>().size;

		particles = GetComponent<ParticleSystem>();
		particles.Stop();

		plrVisuals = transform.Find("Visuals");

		shovelPos = transform.Find("ShovelPos");
	}

	public void Update() {
		timer += Time.deltaTime;

		// The input from the player needs to be determined and
		// then passed in the to the MovementUpdate which should
		// manage the actual movement of the character.
		Vector2 playerInput = new(Input.GetAxisRaw("Horizontal"), 0);
		MovementUpdate(playerInput);

		if (Input.GetKeyDown(KeyCode.Space)) Jump();

		// Flight dash
		if (Input.GetKeyDown(KeyCode.LeftShift)) FlightDashButtonToggle(true);
		else if (Input.GetKeyUp(KeyCode.LeftShift)) FlightDashButtonToggle(false);

		if (Input.GetKey(KeyCode.LeftShift)) FlightDashButtonHeld();

		// Wall climbing
		if (!wallClimbing && usedWallJump) {
			if (WallClimbCheck()) {
				WallClimbButton();
			}
		}

		// Terminal velocity clamp
		rb.velocity = new(rb.velocity.x, Mathf.Clamp(rb.velocity.y, rb.velocity.y, terminalVelocity));

		// Shovel throw
		if (Input.GetKeyDown(KeyCode.F)) ThrowShovel();
	}

	float FeatherSpeed(float featherTime) {
		float featherSpeed = maxSpeed / featherTime; // Convert the feather time from seconds into a speed

		// Increment current speed variable by feather speed; clamp between 0 & max speed
		return Mathf.Clamp(currentSpeed + (featherSpeed * Time.deltaTime), 0, maxSpeed);
	}

	private void MovementUpdate(Vector2 playerInput) {
		if (wallClimbing || usedWallJump) return;

		if (playerInput.x == 0 && currentSpeed > 0) { // No input but still moving
			// Decelerate
			lastXDir = 0; // Make it instantly clear there is no input
			currentSpeed = FeatherSpeed(-decelerationTime); // Decelerate down to 0
			if (currentSpeed == 0) velocity.x = 0; // Kill the velocity entirely
		} else if (playerInput.x != 0) {
			//// Moving
			if (lastXDir != playerInput.x && currentSpeed > 0) { // The X direction last set is different from current one & we were already moving
				// Swap directions
				currentSpeed = FeatherSpeed(-turnTime); // Decelerate
				if (currentSpeed == 0) { // Finished decelerating
					lastXDir = playerInput.x; // Match X directions so the acceleration is used next instead

					// Match velocity to input to start moving in the right direction; needs to be done at the end to continue the conditional
					velocity.x = playerInput.x; 
				}
			} else {
				// Accelerate
				currentSpeed = FeatherSpeed(accelerationTime); // Accelerate to full speed
				velocity = playerInput; // Match velocity
				lastXDir = playerInput.x; // Match X direction
			}
		}

		if (!isFlightDashing && !wallClimbing && !usedWallJump) 
			rb.velocity = new(canMoveHorizontally ? (currentSpeed * velocity.x) : 0, rb.velocity.y);
	}

	bool GroundedRay(float xPos) {
		// Raycast downwards slightly longer than the length of the character from the given X position
		RaycastHit2D ray = Physics2D.Raycast(new(xPos, rb.position.y), Vector2.down, surfaceDistance, 1 << 3);

		bool isGrounded = ray;

		if (ray.collider != null) {
			if (Mathf.Abs((rb.position.y - ray.point.y) - (colliderSize.y / 2)) >= 0.03) isGrounded = false;
		}

		if (isGrounded) usedWallJump = false;

		return isGrounded;
	}

	public bool IsGrounded() {
		if (isJumping || isFlightDashing) return false;

		bool leftRay = GroundedRay(rb.position.x - (colliderSize.x / 2));
		bool middleRay = GroundedRay(rb.position.x);
		bool rightRay = GroundedRay(rb.position.x + (colliderSize.x / 2));

		bool isGrounded = (leftRay || middleRay || rightRay); // Only need one of the three rays to hit

		if (isGrounded) lastGroundedTime = timer; // Track the last time the character was grounded
		else lastNotGroundedTime = timer; // Track last time player was not grounded

		return isGrounded;
	}

	public bool IsWalking() {
		if (!canMoveHorizontally) return false;

		return (velocity.x != 0 && currentSpeed >= (maxSpeed * 0.5f)); // If the velocity is 0 then there is no movement
	}

	FacingDirection currentDirection = FacingDirection.right; // Whichever direction the character is currently facing
	public FacingDirection GetFacingDirection() {
		if (velocity.x == 0 || isFlightDashing) return currentDirection; // Keep facing the way we've already been facing

		FacingDirection dir = (velocity.x == -1) ? FacingDirection.left : FacingDirection.right; // -1 = left, 1 = right
		currentDirection = dir; // Update the current direction

		return dir;
	}

	/// Jump
	void Jump() {
		if (isJumping) return;
		if (usedWallJump) return;

		bool isGrounded = IsGrounded();

		if (wallClimbing) isGrounded = true;
		else if (!isGrounded && (timer - lastGroundedTime > coyoteTime)) return; // Coyote time
		if (!wallClimbing && timer - lastNotGroundedTime < minTimeBetweenJumps) return; // Minimum interval between being grounded and jumping
		float initialJumpVelocity = (2 * jumpApexHeight) / jumpApexTime; // Inital jump velocity

		if (!wallClimbing && Mathf.Abs(rb.velocity.y) > 1.6f) return; // Helps prevent hopping off corners

		lastGroundedTime -= coyoteTime; // Prevent overlap
		rb.gravityScale = 0;

		float wallClimbVelocityToUse = 0;
		if (wallClimbing) {
			usedWallJump = true;
			velocity.x *= -1;
			wallClimbVelocityToUse = (GetFacingDirection() == FacingDirection.left) ? -wallClimbProjectionVelocity : wallClimbProjectionVelocity;
			wallClimbing = false;
		}

		rb.velocity = new Vector2(usedWallJump ? wallClimbVelocityToUse : rb.velocity.x, initialJumpVelocity);
		isJumping = true;

		StartCoroutine(EndJumpAfterApex()); // Stop jump
	}
	private IEnumerator EndJumpAfterApex() {
		yield return new WaitForSeconds(jumpApexTime);

		// Re-enable gravity and let the character fall naturally
		rb.gravityScale = 2;
		isJumping = false;
	}

	////////////////////////////////////////////////////////
	////////////// Final Assignment Mechanics //////////////
	////////////////////////////////////////////////////////
	public float holdDashButtonTime = 1.0f;
	bool isFlightDashing = false;
	bool attemptingFlightDash = false;
	public float flightDashSpeed = 8f;
	public float flightDashRotationDegrees = 60;
	bool startedFlight = false;

	////////////////////////////////// Flight Dash //////////////////////////////////
	float startedHoldingDashButton = 0;

	void FlightDashButtonToggle(bool toggle) {
		if (toggle && usedWallJump) return;

		if (!toggle && wallClimbing || usedWallJump) {
			// Stop wall climbing
			StopWallClimb();
			return;
		}

		if ((!isFlightDashing) && (isJumping || !IsGrounded())) {
			// Wall climbing
			WallClimbButton();
			return;
		};
		if (canMoveHorizontally == !toggle) return; // No change

		if (WallClimbCheck()) {
			// Check for a wall
			WallClimbButton();
			return;
		}

		// Flight dash chargeup
		attemptingFlightDash = toggle;
		rb.gravityScale = toggle ? 0 : 2;
		isFlightDashing = false;
		canMoveHorizontally = !toggle;
		currentSpeed = 0;
		rb.velocity = Vector2.zero;
		startedFlight = false;

		plrVisuals.transform.rotation = Quaternion.Euler(0, 0, 0);

		if (toggle) particles.Play();
		else particles.Stop();

		if (toggle) startedHoldingDashButton = timer;
	}

	void FlightDashButtonHeld() {
		if (wallClimbing) {
			WallClimbButton();
			return;
		}
		if (!attemptingFlightDash) return;
		if (timer - startedHoldingDashButton < holdDashButtonTime) return;

		isFlightDashing = true;

		bool facingRight = (GetFacingDirection() == FacingDirection.right);

		if (!startedFlight) {
			startedFlight = true;
			rb.velocity = new(facingRight ? flightDashSpeed : -flightDashSpeed, 0);
		} else if (Mathf.Abs(rb.velocity.x) != flightDashSpeed) {
			// Hit an obstacle so stop
			FlightDashButtonToggle(false);
			return;
		}
		plrVisuals.transform.rotation = Quaternion.Euler(0, 0, facingRight ? -flightDashRotationDegrees : flightDashRotationDegrees);
	}

	////////////////////////////////// Wall Climbing //////////////////////////////////
	bool wallClimbing = false;
	public float wallClimbRayDistance = 1.1f;
	bool usedWallJump = false;
	public float wallClimbProjectionVelocity = 5f;

	bool WallClimbCheck() {
		if (attemptingFlightDash || isFlightDashing) return false;

		return Physics2D.Raycast(
			new(rb.position.x, rb.position.y), 
			GetFacingDirection() == FacingDirection.left ? Vector2.left : Vector2.right, 
			wallClimbRayDistance, 
			1 << 3
		);
	}

	void WallClimbButton() {
		if (!WallClimbCheck()) return;
		if (isJumping) return;

		if (!wallClimbing) {
			// Once at start
			isJumping = false;
			wallClimbing = true;
			rb.gravityScale = 0;
			rb.velocity = Vector2.zero;
			usedWallJump = false;
		}
	}

	void StopWallClimb() {
		wallClimbing = false;
		usedWallJump = false;

		if (!isJumping) {
			rb.gravityScale = 2;
		}
	}

	/////////////////////// Shovel Throwing ///////////////////////
	public GameObject shovelPrefab;
	public Vector2 shovelThrowVelocity;
	Transform shovelPos;
	bool shovelThrowCooldown = false;

	void ThrowShovel() {
		if (wallClimbing || isFlightDashing) return;

		if (shovelThrowCooldown) return;
		shovelThrowCooldown = true;
		StartCoroutine(DisableShovelThrowCooldown());

		GameObject shovel = Instantiate(shovelPrefab, shovelPos.position, shovelPos.rotation);
		Destroy(shovel, 10);

		bool facingRight = (GetFacingDirection() == FacingDirection.right);

		Rigidbody2D shovelRb = shovel.GetComponent<Rigidbody2D>();
		shovelRb.velocity = new(facingRight ? shovelThrowVelocity.x : -shovelThrowVelocity.x, shovelThrowVelocity.y);

		if (!facingRight) shovelRb.rotation = 180;
	}

	IEnumerator DisableShovelThrowCooldown() {
		yield return new WaitForSeconds(1);

		shovelThrowCooldown = false;
	}
}

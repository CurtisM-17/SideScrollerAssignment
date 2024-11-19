using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

	public float jumpApexHeight, jumpApexTime;
	float jumpVelocity;

	float currentSpeed = 0f;
    Vector2 colliderSize;
	bool isJumping = false;

	float timer;

    public enum FacingDirection {
		left, right
	}

	public void Start() {
		rb = GetComponent<Rigidbody2D>();
		colliderSize = GetComponent<BoxCollider2D>().size;

        jumpVelocity = 2 * (jumpApexHeight / jumpApexTime);
    }

	public void Update() {
		timer += Time.deltaTime;

		// The input from the player needs to be determined and
		// then passed in the to the MovementUpdate which should
		// manage the actual movement of the character.
		Vector2 playerInput = new(Input.GetAxisRaw("Horizontal"), 0);
		MovementUpdate(playerInput);

		if (Input.GetKey(KeyCode.Space)) Jump();
		if (isJumping) Jump();

		rb.velocity = new(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -jumpVelocity, terminalVelocity));
	}

	float FeatherSpeed(float featherTime) {
		float featherSpeed = maxSpeed / featherTime; // Convert the feather time from seconds into a speed

		// Increment current speed variable by feather speed; clamp between 0 & max speed
		return Mathf.Clamp(currentSpeed + (featherSpeed * Time.deltaTime), 0, maxSpeed);
	}

	private void MovementUpdate(Vector2 playerInput) {
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

		rb.velocity = new(currentSpeed * velocity.x, rb.velocity.y);
	}

	bool GroundedRay(float xPos)
	{
        // Raycast downwards slightly longer than the length of the character from the given X position
		return Physics2D.Raycast(new(xPos, rb.position.y), Vector2.down, surfaceDistance, 1 << 3);
	}

	public bool IsGrounded() {
		if (isJumping) return false;

		bool leftRay = GroundedRay(rb.position.x - (colliderSize.x / 2));
		bool middleRay = GroundedRay(rb.position.x);
		bool rightRay = GroundedRay(rb.position.x + (colliderSize.x / 2));

		// Only need one of the three rays to hit
		return (leftRay || middleRay || rightRay);
	}

	public bool IsWalking() {
		return (velocity.x != 0 && currentSpeed >= (maxSpeed * 0.5f)); // If the velocity is 0 then there is no movement
	}

	FacingDirection currentDirection = FacingDirection.right; // Whichever direction the character is currently facing

	public FacingDirection GetFacingDirection() {
		if (velocity.x == 0) return currentDirection; // Keep facing the way we've already been facing

		FacingDirection dir = (velocity.x == -1) ? FacingDirection.left : FacingDirection.right; // -1 = left, 1 = right
		currentDirection = dir; // Update the current direction

		return dir;
	}

	/// Jump
	float jumpStartPos;

	void Jump() {
		if (!isJumping) {
            // Begin jump
            if (isJumping || !IsGrounded()) return;

            jumpVelocity = 2 * (jumpApexHeight / jumpApexTime); // Account for runtime updates

			isJumping = true;
			rb.gravityScale = 0;
			jumpStartPos = rb.position.y;

			rb.velocity = new(rb.velocity.x, jumpVelocity);
		}

		if (rb.position.y >= jumpStartPos + jumpApexHeight) rb.velocity = new(rb.velocity.x, -jumpVelocity);
		//print(rb.position.y + " | " + jumpStartPos + " | " + jumpApexHeight + " | " + (jumpStartPos + jumpApexHeight));
		if (rb.velocity.y == 0) isJumping = false;

		//float gravity = (-2 * jumpApexHeight) / (jumpApexTime * jumpApexTime);
		//rb.velocity = new(rb.velocity.x, gravity * (timer - jumpStartTime) + jumpVelocity);
	}
}

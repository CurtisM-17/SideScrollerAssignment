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

	float currentSpeed = 0f;

	public enum FacingDirection {
		left, right
	}

	public void Start() {
		rb = GetComponent<Rigidbody2D>();
	}

	public void Update() {
		// The input from the player needs to be determined and
		// then passed in the to the MovementUpdate which should
		// manage the actual movement of the character.
		Vector2 playerInput = new(Input.GetAxisRaw("Horizontal"), 0);
		MovementUpdate(playerInput);
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
	}

	public void FixedUpdate() { // Because we're dealing with RigidBodies
		rb.position += currentSpeed * Time.fixedDeltaTime * velocity;
	}

	public bool IsGrounded() {
		// Raycast downwards slightly longer than the length of the character from the center
		return Physics2D.Raycast(rb.position, Vector2.down, 1.01f, 1 << 3); // Use bitshift to stick to layer 3 (ground layer only)
	}

	public bool IsWalking() {
		return (velocity.x != 0); // If the velocity is 0 then there is no movement
	}

	FacingDirection currentDirection = FacingDirection.right; // Whichever direction the character is currently facing

	public FacingDirection GetFacingDirection() {
		if (velocity.x == 0) return currentDirection; // Keep facing the way we've already been facing

		FacingDirection dir = (velocity.x == -1) ? FacingDirection.left : FacingDirection.right; // -1 = left, 1 = right
		currentDirection = dir; // Update the current direction

		return dir;
	}
}

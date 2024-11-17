using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
	Rigidbody2D rb;
	Vector2 velocity;
	float lastXDir = 0; // Keep track of what the player input is LAST frame to track changes
	public float speed;
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
		float featherSpeed = speed / featherTime;
		return Mathf.Clamp(currentSpeed + (featherSpeed * Time.deltaTime), 0, speed);
	}

	private void MovementUpdate(Vector2 playerInput) {
		if (playerInput.x == 0 && currentSpeed > 0) {
			// Decelerate
			lastXDir = 0;
			currentSpeed = FeatherSpeed(-decelerationTime);
			if (currentSpeed == 0) velocity.x = 0;
		} else if (playerInput.x != 0) {
			//// Moving
			if (lastXDir != playerInput.x && currentSpeed > 0) {
				// Swap directions
				currentSpeed = FeatherSpeed(-turnTime);
				if (currentSpeed == 0) {
					lastXDir = playerInput.x;
					velocity.x = playerInput.x;
				}
			} else {
				// Accelerate
				currentSpeed = FeatherSpeed(accelerationTime);
				velocity = playerInput;
				lastXDir = playerInput.x;
			}
		}
	}

	public void FixedUpdate() { // Because we're dealing with RigidBodies
		rb.position += currentSpeed * Time.fixedDeltaTime * velocity;
	}

	public bool IsWalking() {
		return (velocity.x != 0);
	}
	public bool IsGrounded() {
		return true;
	}

	FacingDirection currentDirection = FacingDirection.right;

	public FacingDirection GetFacingDirection() {
		if (velocity.x == 0) return currentDirection;

		FacingDirection dir = (velocity.x == -1) ? FacingDirection.left : FacingDirection.right;
		currentDirection = dir;

		return dir;
	}
}

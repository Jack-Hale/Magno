using Godot;
using System;

public partial class Slime : CharacterBody2D
{
	public const float speed = 300.0f;
	public const float jumpVelocity = -400.0f;

	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	private bool canMove = true;

	public override void _Process(double delta) {
		
	}

	public override void _PhysicsProcess(double delta)
	{
		
		Vector2 velocity = Velocity;
		if (IsInGroup("Magnetic")) {
			if (IsInGroup("Affected")) {
				canMove = false;
			} else {
				canMove = true;
			}
		} else {
			canMove = true;
		}

		if (canMove) {
			// Add the gravity.
			if (!IsOnFloor())
				velocity.Y += gravity * (float)delta;

			// Handle Jump.
			if (IsOnFloor()) {
				velocity.Y = jumpVelocity;
			}
		}

		Velocity = velocity;
		MoveAndSlide();
	}
}

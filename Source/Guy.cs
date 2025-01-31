using Godot;
using System;

public partial class Guy : CharacterBody2D
{
	public const float Speed = 300.0f;
	public const float JumpVelocity = -400.0f;

	private bool canMove = true;

	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

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
				// velocity.Y = JumpVelocity;
			}
		}

		Velocity = velocity;
		MoveAndSlide();
	}
}

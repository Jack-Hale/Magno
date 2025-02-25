using Godot;
using System;

public partial class LargeSlime : CharacterBody2D
{
	public const float speed = 300.0f;
	public const float jumpVelocity = -400.0f;
	
	private bool affected = true;

	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;
		if (IsInGroup("Magnetic")) {
			if (IsInGroup("Affected")) {
				affected = true;
			} else {
				affected = false;
			}
		} else {
			affected = false;
		}

		// Add the gravity.
		if (!IsOnFloor())
			velocity.Y += gravity * (float)delta;

		if (!affected) {
			// Handle Jump.
			if (IsOnFloor()) {
				velocity.Y = jumpVelocity;
			}
		} else {
			// velocity.X += 10;
		}

		Velocity = velocity;
		MoveAndSlide();
	}
}

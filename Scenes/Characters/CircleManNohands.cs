using Godot;
using System;

public partial class CircleManNohands : CharacterBody2D {
	[Export]
	public float maxSpeed = 400;
	[Export]
	public float jumpVelocity = -180f;
	[Export]
	public float jumpHoldTime = 0.15f;
	[Export]
	public float friction = 2200f;
	[Export]
	public float airFriction = 1f;

	[Export]
	public float acceleration = 2200f;
	[Export]
	public float airAcceleration = 1800f;

	private PathFindingComponent _pathFindingComponent;
	public override void _Ready() {
		_pathFindingComponent = GetNode<PathFindingComponent>("PathFindingComponent");
	}


	public override void _PhysicsProcess(double delta) {
		Vector2 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor()) {
			velocity += GetGravity() * (float)delta;
		}
		else {
			velocity.Y -= 100;
		}

		velocity = _pathFindingComponent.ApplyFriction(true, velocity, friction, delta);

		Velocity = velocity;
		MoveAndSlide();
	}
}

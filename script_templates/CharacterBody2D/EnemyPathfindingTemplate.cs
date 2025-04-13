using Godot;
using System;

public partial class EnemyPathfindingTemplate : CharacterBody2D {
	public float maxSpeed = 200.0f;
	public float jumpVelocity = -800.0f;
	float friction = 2200;
	float acceleration = 2200;
	float airAcceleration = 1800;
	private Sprite2D _sprite2D;
	private PathFindingComponent _pathFinding;
	private CharacterBody2D player;

    public override void _Ready() {
        _pathFinding = GetNode<PathFindingComponent>("PathFindingComponent");
		_sprite2D = GetNode<Sprite2D>("Sprite2D");
		player = _pathFinding.GetPlayer();
    }


	public override void _PhysicsProcess(double delta) {
		Vector2 velocity = Velocity;
		Vector2 direction = Vector2.Zero;

		// Add the gravity.
		if (!IsOnFloor()) {
			velocity += GetGravity() * (float)delta;
		}

		if (IsInGroup("CanSeePlayer") || IsInGroup("LookingForPlayer")) {
			direction = GlobalPosition.DirectionTo(_pathFinding.GetLastDetectionPoint());

			_sprite2D.FlipH = direction.X < 0;
		} else {

		}

		velocity.X = _pathFinding.MoveCharacter(true, velocity, direction, maxSpeed, acceleration, airAcceleration, delta).X;
		if (direction == Vector2.Zero) {
			velocity.X = _pathFinding.ApplyFriction(true, velocity, friction, delta).X;
		}

		Velocity = velocity;
		MoveAndSlide();
	}
}
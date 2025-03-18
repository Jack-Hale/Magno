using Godot;
using System;

public partial class Bug : CharacterBody2D
{
	public float maxSpeed = 100.0f;
	public float acceleration = 60.0f;
	float friction = 2200;
	float airAcceleration = 1800;

	public float jumpVelocity = -400.0f;
	private PathFindingComponent _pathFinding;
	private Sprite2D _sprite;
	private CharacterBody2D player;
	private Vector2 draw1 = Vector2.Zero;
	private Vector2 draw2 = Vector2.Zero;
	public override void _Ready() {
		_sprite = GetNode<Sprite2D>("Sprite2D");
		_pathFinding = GetNode<PathFindingComponent>("PathFindingComponent");
		player = _pathFinding.GetPlayer();
	}

    public override void _Draw() {
		DrawLine(ToLocal(draw1), ToLocal(draw2), Colors.Green);
    }

    public override void _PhysicsProcess(double delta) {
		Vector2 velocity = Velocity;
		Vector2 direction = Vector2.Zero;

		if (IsInGroup("CanSeePlayer") || IsInGroup("LookingForPlayer")) {
			direction = GlobalPosition.DirectionTo(_pathFinding.GetLastDetectionPoint());
		} 

		velocity = _pathFinding.MoveCharacter(false, velocity, direction, maxSpeed, friction, acceleration, airAcceleration, delta);
		
		if (IsInGroup("LookingForPlayer")) {

		}

		velocity = _pathFinding.AvoidWallsAir(velocity, 60, 30, 40);

		_sprite.Rotation = Mathf.Atan2(velocity.Y, velocity.X);

		Velocity = velocity;
		MoveAndSlide();
	}

	private Vector2 IdleBehaviour() {
		return Vector2.Zero;
	}
}

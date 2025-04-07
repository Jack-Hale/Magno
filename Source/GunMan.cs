using Godot;
using System;
public partial class GunMan : CharacterBody2D
{
	public float maxSpeed = 300.0f;
	public float jumpVelocity = -800.0f;
	float friction = 2200;
	float acceleration = 2200;
	float airAcceleration = 1800;
	private Sprite2D _sprite2D;
	private ProjectileLauncher _projectileLauncher;
	private ProjectileComponent _projectileComponent;
	private PathFindingComponent _pathFinding;
	private bool hasTarget = false;
	private CharacterBody2D player;

	public override void _Ready() {
		_sprite2D = GetNode<Sprite2D>("Sprite2D");
		_pathFinding = GetNode<PathFindingComponent>("PathFindingComponent");
		_projectileLauncher = GetNode<ProjectileLauncher>("ProjectileLauncher");
		_projectileComponent = _projectileLauncher.GetProjectileComponent();

		player = _pathFinding.GetPlayer();
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor()) {
			velocity += GetGravity() * (float)delta;
		}

		Vector2 direction = Vector2.Zero;


		if (IsOnFloor() && _pathFinding.CheckNoFloor(velocity, 10, 5)) {
			if (IsInGroup("CanSeePlayer") || IsInGroup("LookingForPlayer")) {
				velocity = Jump(velocity);
			}
		}

		if (IsOnFloor() && _pathFinding.AvoidWallsGround(velocity, 60, 30)) {
			if (IsInGroup("CanSeePlayer") || IsInGroup("LookingForPlayer")) {
				velocity = Jump(velocity);
			}
		}

		if (IsOnFloor() && _pathFinding.CheckGroundAbove(velocity, 40, 30)) {
			if (IsInGroup("CanSeePlayer") || IsInGroup("LookingForPlayer")) {
				velocity = Jump(velocity);
			}
		}

		if (IsInGroup("CanSeePlayer") || IsInGroup("LookingForPlayer")) {
			hasTarget = true;
			_projectileComponent.Shoot();
			
			_projectileLauncher.LookAt(_pathFinding.GetLastDetectionPoint());
			direction = GlobalPosition.DirectionTo(_pathFinding.GetLastDetectionPoint());
			_projectileLauncher.SetFlipH(Mathf.Sign(direction.X) < 0);
			_sprite2D.FlipH = _projectileLauncher.GetFlipH();
		} else {
			hasTarget = false;
		}
		velocity.X = _pathFinding.MoveCharacter(true, velocity, direction, maxSpeed, acceleration, airAcceleration, delta).X;
		if (direction == Vector2.Zero) {
			velocity.X = _pathFinding.ApplyFriction(true, velocity, friction, delta).X;
		}


		if (Velocity.X != 0 && !hasTarget) {
			_sprite2D.FlipH = Velocity.X < 0;
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	private Vector2 Jump(Vector2 velocity) {
		velocity.Y = jumpVelocity;
		return velocity;
	}
}

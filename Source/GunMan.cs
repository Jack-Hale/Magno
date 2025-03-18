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
	private Sprite2D _gunSprite;
	private Node2D _gun;
	private Vector2 projectilePosition;
	private ProjectileComponent _projectileComponent;
	private PathFindingComponent _pathFinding;
	private bool hasTarget = false;
	private CharacterBody2D player;

	public override void _Ready() {
		_sprite2D = GetNode<Sprite2D>("Sprite2D");
		_gun = GetNode<Node2D>("Gun");
		_gunSprite = _gun.GetNode<Sprite2D>("Sprite2D");
		_projectileComponent = _gun.GetNode<ProjectileComponent>("ProjectileComponent");
		_pathFinding = GetNode<PathFindingComponent>("PathFindingComponent");
		projectilePosition = _projectileComponent.Position;

		player = _pathFinding.GetPlayer();
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor()) {
			velocity += GetGravity() * (float)delta;
		}

		// if (Input.IsActionJustPressed("ui_up") && IsOnFloor()) {
		// 	velocity = Jump(velocity);
		// }
		// Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

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
			if (IsInGroup("CanSeePlayer")) {
				_gun.LookAt(player.GlobalPosition);
			} else {
				_gun.LookAt(_pathFinding.GetLastDetectionPoint());
			}
			direction = GlobalPosition.DirectionTo(_pathFinding.GetLastDetectionPoint());
		} else {
			hasTarget = false;
		}
		velocity.X = _pathFinding.MoveCharacter(true, velocity, direction, maxSpeed, friction, acceleration, airAcceleration, delta).X;


		bool flip = false;
		float angle = (_gun.Rotation % (2 * Mathf.Pi) + (2 * Mathf.Pi)) % (2 * Mathf.Pi);

		// Testing if the angle of the gun has it pointing on the left side of the character to flip its sprites
		if (angle >= (3 * Mathf.Pi / 2) || angle <= (Mathf.Pi / 2)) {
			_gunSprite.FlipV = false;
			_projectileComponent.Position = projectilePosition;
		} else {
			_gunSprite.FlipV = true;
			_projectileComponent.Position = new Vector2(projectilePosition.X, -projectilePosition.Y);
			flip = true;
		}

		if (hasTarget) {
			_sprite2D.FlipH = flip;
		} else {
			_gun.Rotation = _sprite2D.FlipH ? Mathf.Pi : 0;
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

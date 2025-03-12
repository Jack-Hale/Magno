using Godot;
using System;

public partial class GunMan : CharacterBody2D
{
	public const float Speed = 300.0f;
	public const float JumpVelocity = -400.0f;
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

		// Handle Jump.
		if (Input.IsActionJustPressed("ui_up") && IsOnFloor()) {
			velocity.Y = JumpVelocity;
		}

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		if (direction != Vector2.Zero) {
			velocity.X = direction.X * Speed;
		} else {
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
		}


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

		if (IsInGroup("CanSeePlayer")) {
			hasTarget = true;
			_gun.LookAt(player.GlobalPosition);
			_projectileComponent.Shoot();
		} else {
			hasTarget = false;
		}

		Velocity = velocity;
		MoveAndSlide();
	}
}

using Godot;
using System;

public partial class GunMan : CharacterBody2D
{
	public const float Speed = 300.0f;
	public const float JumpVelocity = -400.0f;
	private Sprite2D _sprite2D;
	private Sprite2D _gunSprite;
	private Node2D _gun;
	private Vector2 gunPosition;
	private Vector2 projectilePosition;
	private float projectileRotation;
	private ProjectileComponent _projectileComponent;

	public override void _Ready() {
		_sprite2D = GetNode<Sprite2D>("Sprite2D");
		_gun = GetNode<Node2D>("Gun");
		_gunSprite = _gun.GetNode<Sprite2D>("Sprite2D");
		_projectileComponent = GetNode<ProjectileComponent>("ProjectileComponent");
		gunPosition = _gun.Position;
		projectilePosition = _projectileComponent.Position;
		projectileRotation = _projectileComponent.Rotation;
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		// Handle Jump.
		if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		if (direction != Vector2.Zero)
		{
			velocity.X = direction.X * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
		}

		if (Velocity.X != 0) 
		{
			bool flip = Velocity.X < 0;
			_sprite2D.FlipH = flip;
			_gunSprite.FlipH = flip;
			_gun.Position = new Vector2(flip ? -gunPosition.X : gunPosition.X, _gun.Position.Y);
			_projectileComponent.Position = new Vector2(flip ? -projectilePosition.X : projectilePosition.X, _projectileComponent.Position.Y);
			_projectileComponent.Rotation = flip ? Mathf.Pi - projectileRotation : projectileRotation;
		}

		Velocity = velocity;
		MoveAndSlide();
	}
}

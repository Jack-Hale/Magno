using Godot;
using System;
using System.ComponentModel;

public partial class PullStick : RigidBody2D
{
	private ItemComponent _itemComponent;
	private CollisionShape2D _collisionShape1;
	private CollisionShape2D _collisionShape2;
	private Sprite2D _sprite1;
	private Sprite2D _sprite2;
	private Player player;
	private RayCast2D rayCast1;
	private RayCast2D rayCast2;

	private float moveInTime = 0f;
	private float moveOutTime = 0f;
	private float moveTimeMax = 0.03f;
	private float moveSpeed = 14f;

	private bool push = false;
	private bool movingOut = false;
	private bool movingIn = true;

	private Vector2 spritePosition;
	private Vector2 collisionPosition;
	
	public override void _Ready() {
		_itemComponent = GetNode<ItemComponent>("ItemComponent");
		
		_itemComponent.OnUseItemLeft += UseItemLeft;
		_itemComponent.OnUseItemRight += UseItemRight;

		_collisionShape1 = GetNode<CollisionShape2D>("CollisionShape2D");
		_collisionShape2 = GetNode<CollisionShape2D>("CollisionShape2D2");
		_sprite1 = GetNode<Sprite2D>("Sprite2D");
		_sprite2 = GetNode<Sprite2D>("Sprite2D2");
		rayCast1 = GetNode<RayCast2D>("RayCast2D");
		rayCast2 = GetNode<RayCast2D>("RayCast2D2");

		player = _itemComponent.GetPlayer();

		_collisionShape1.AddToGroup("MainCollisionShape");

		collisionPosition = _collisionShape2.Position;
		spritePosition = _sprite2.Position;
	}

	public override void _Process(double delta)	{

		if (!movingIn && !movingOut) {
			_sprite2.Position = spritePosition;
			_collisionShape2.Position = collisionPosition;
		}

		if (moveInTime > 0) {
			movingIn = true;
			if (rayCast1.IsColliding() || rayCast2.IsColliding()) {
				player.ApplyForce(Vector2.Right.Rotated(player.GetMagnetRotation()), 1500);
			}

			_sprite2.Position += new Vector2(-moveSpeed, 0);
			_collisionShape2.Position += new Vector2(-moveSpeed, 0);

			moveInTime -= (float)delta;

			if (moveInTime <= 0 && !push) {
				moveOutTime = moveTimeMax;
			}
		} else {
			movingIn = false;
		}

		if (moveOutTime > 0) {
			movingOut = true;
			_sprite2.Position += new Vector2(moveSpeed, 0);
			_collisionShape2.Position += new Vector2(moveSpeed, 0);
			moveOutTime -= (float)delta;

			if (moveOutTime <= 0 && push) {
				moveInTime = moveTimeMax;
			}
		} else {
			movingOut = false;
		}
	}

	private void UseItemRight() {
		if (moveInTime <= 0) {
			moveOutTime = moveTimeMax;
			push = true;
		}
	}
	private void UseItemLeft() {
		if (moveOutTime <= 0) {
			moveInTime = moveTimeMax;
			push = false;
		}
	}
}


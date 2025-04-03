using Godot;
using System;

public partial class PullStick : RigidBody2D
{
	private ItemComponent _itemComponent;
	private CollisionShape2D _collisionShape1;
	private CollisionShape2D _collisionShape2;
	private Sprite2D _sprite1;
	private Sprite2D _sprite2;
	private Player player;
	private RayCast2D _rayCast1;
	private RayCast2D _rayCast2;
	private RayCast2D _rayCast3;

	private float moveInTime = 0f;
	private float moveOutTime = 0f;
	private float moveTimeMax = 0.03f;
	private float moveSpeed = 14f;

	private bool push = false;
	private bool movingOut = false;
	private bool movingIn = true;

	private Vector2 spritePosition;
	private Vector2 collisionPosition;
	private bool impulse = false;
	
	public override void _Ready() {
		_itemComponent = GetNode<ItemComponent>("ItemComponent");
		
		_itemComponent.OnUseItemLeft += UseItemLeft;
		_itemComponent.OnUseItemRight += UseItemRight;

		_collisionShape1 = GetNode<CollisionShape2D>("CollisionShape2D");
		_collisionShape2 = GetNode<CollisionShape2D>("CollisionShape2D2");
		_sprite1 = GetNode<Sprite2D>("Sprite2D");
		_sprite2 = GetNode<Sprite2D>("Sprite2D2");
		_rayCast1 = GetNode<RayCast2D>("RayCast2D");
		_rayCast2 = GetNode<RayCast2D>("RayCast2D2");
		_rayCast3 = GetNode<RayCast2D>("RayCast2D3");

		player = _itemComponent.GetPlayer();

		_rayCast1.AddException(player);
		_rayCast2.AddException(player);
		_rayCast3.AddException(player);

		_collisionShape1.AddToGroup("MainCollisionShape");

		collisionPosition = _collisionShape2.Position;
		spritePosition = _sprite2.Position;
	}

	public override void _PhysicsProcess(double delta)	{

		if (!movingIn && !movingOut) {
			_sprite2.Position = spritePosition;
			_collisionShape2.Position = collisionPosition;
		}

		if (moveInTime > 0) {
			movingIn = true;
			if (_rayCast1.IsColliding() || _rayCast2.IsColliding()) {
				player.AddImpulse(Vector2.Right.Rotated(player.GetMagnetRotation())*1500);
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
			// impulse boolean prevents force being applied more than once in one firing
			if (_rayCast3.IsColliding() && !impulse && push) {
				if (_rayCast3.GetCollider() is TileMapLayer) {
					// MoveOutTime must be set to zero to reset the collision. Prevents stick from getting stuck in ground
					moveOutTime = 0;
					player.ApplyForce(Vector2.Right.Rotated(player.GetMagnetRotation() + Mathf.DegToRad(180)), 1500);
					impulse = true;

				} else if (_rayCast3.GetCollider() is RigidBody2D rigid) {
					rigid.ApplyImpulse(Vector2.Right.Rotated(player.GetMagnetRotation()) * 1000);
					impulse = true;

				} else if (_rayCast3.GetCollider() is CharacterBody2D character) {
					character.AddImpulse(Vector2.Right.Rotated(player.GetMagnetRotation()) * 1000);
					impulse = true;
				}
			}

			movingOut = true;
			_sprite2.Position += new Vector2(moveSpeed, 0);
			_collisionShape2.Position += new Vector2(moveSpeed, 0);
			moveOutTime -= (float)delta;

			if (moveOutTime <= 0 ) {
				impulse = false;
				if (push) {
					moveInTime = moveTimeMax;
				}
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


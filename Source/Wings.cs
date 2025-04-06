using Godot;
using System;

public partial class Wings : RigidBody2D
{
	private ItemComponent _itemComponent;
	private PhysicsBody2D itemOwner = null;
	private CharacterBody2D charOwner = null;
	private RigidBody2D rigidOwner = null;
	private float flapForce = 500;
	private float flapTimer = 0f;
	private float flapTimerMax = 0.2f;
	private Player player;
	
	public override void _Ready() {
		_itemComponent = GetNode<ItemComponent>("ItemComponent");
		
		_itemComponent.OnUseItemLeft += UseItemLeft;
		_itemComponent.OnUseItemRight += UseItemRight;

		itemOwner = _itemComponent.GetItemOwner();

		player = _itemComponent.GetPlayer();
	}

    public override void _Process(double delta) {
		if (itemOwner != _itemComponent.GetItemOwner()) {
			itemOwner = _itemComponent.GetItemOwner();
			if (itemOwner is CharacterBody2D c) {
				rigidOwner = null;				
				charOwner = c;
			} else if (itemOwner is RigidBody2D r) {
				charOwner = null;				
				rigidOwner = r;
			} else {
				charOwner = null;
				rigidOwner = null;
			}
		}
    }

	public override void _PhysicsProcess(double delta)	{
		if (_itemComponent.GetItemOwner() != player || !_itemComponent.GetIsPlayerMagnet()) {
			if (flapTimer > 0) {
				flapTimer -= (float)delta;
			}

			if (flapTimer <= 0)  {
				if (_itemComponent.GetItemOwner() == player) {
					player.ApplyForce(Vector2.Up * flapForce);
				} else {
					if (charOwner != null) {
						charOwner.ApplyImpulse(Vector2.Up * flapForce);
					}

					if (rigidOwner != null) {
						rigidOwner.ApplyImpulse(Vector2.Up * flapForce);
					}
				}
			
				flapTimer = flapTimerMax;
			}
		}
	}

	private void UseItemRight() {
		if (_itemComponent.GetItemOwner() == player) {
			player.ApplyForce(new Vector2(0, -flapForce));
		}
	}
	private void UseItemLeft() {
		if (_itemComponent.GetItemOwner() == player) {
			player.ApplyForce(new Vector2(0, 2 * -flapForce));	
		}
	}
}


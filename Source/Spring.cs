using Godot;
using System;

public partial class Spring : RigidBody2D
{
	private ItemComponent _itemComponent;
	private Player player;
	
	private PhysicsBody2D itemOwner = null;
	private CharacterBody2D charOwner = null;
	private RigidBody2D rigidOwner = null;
	
	public override void _Ready() {
		_itemComponent = GetNode<ItemComponent>("ItemComponent");
		
		_itemComponent.OnUseItemLeft += UseItem;

		player = _itemComponent.GetPlayer();
		
		itemOwner = _itemComponent.GetItemOwner();
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
		if (_itemComponent.GetIsBeingHeld()) {
			// If any part of the player EXCEPT the player itself touches the ground, bounce
			if (player.GetIsAnyOnFloor() && !player.GetIsOnFloor()) {
				player.ApplyForce(Vector2.Up * player.GetPreFloorVelocity().Y);
			}
		}
	}

	private void UseItem() {
		
	}
}


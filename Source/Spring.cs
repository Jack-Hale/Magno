using Godot;
using System;

public partial class Spring : RigidBody2D
{
	private ItemComponent _itemComponent;
	private Player player;
	
	public override void _Ready() {
		_itemComponent = GetNode<ItemComponent>("ItemComponent");
		
		_itemComponent.OnUseItemLeft += UseItem;

		player = _itemComponent.GetPlayer();
	}

	public override void _PhysicsProcess(double delta)	{
		if (_itemComponent.GetIsBeingHeld()) {
			// If any part of the player EXCEPT the player itself touches the ground, bounce
			if (player.GetIsAnyOnFloor() && !player.GetIsOnFloor()) {
				player.ApplyForce(Vector2.Up, player.GetPreFloorVelocity().Y);
			}
		}
	}

	private void UseItem() {
		
	}
}


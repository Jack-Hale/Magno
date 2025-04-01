using Godot;
using System;

public partial class Spring : RigidBody2D
{
	private ItemComponent _itemComponent;
	private Player player;
	
	public override void _Ready() {
		_itemComponent = GetNode<ItemComponent>("ItemComponent");
		
		_itemComponent.OnUseItem += UseItem;

		player = _itemComponent.GetPlayer();
	}

	public override void _PhysicsProcess(double delta)	{
		if (player.IsOnFloor() && !player.GetCharacter().IsOnFloor()) {
			player.ApplyForce(Vector2.Up, player.GetPreFloorVelocity().Y);
		}
	}

	private void UseItem() {
		
	}
}


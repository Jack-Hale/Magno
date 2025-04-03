using Godot;
using System;

public partial class PullStick : RigidBody2D
{
	private ItemComponent _itemComponent;
	private CollisionShape2D _collisionShape;
	
	public override void _Ready() {
		_itemComponent = GetNode<ItemComponent>("ItemComponent");
		
		_itemComponent.OnUseItem += UseItem;

		_collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		_collisionShape.AddToGroup("MainCollisionShape");
	}

	public override void _Process(double delta)	{

	}

	private void UseItem() {
		
	}
}


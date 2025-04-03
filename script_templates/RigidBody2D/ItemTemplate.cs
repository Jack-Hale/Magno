using Godot;
using System;

public partial class Item : RigidBody2D
{
	private ItemComponent _itemComponent;
	
	public override void _Ready() {
		_itemComponent = GetNode<ItemComponent>("ItemComponent");
		
		_itemComponent.OnUseItemLeft += UseItemLeft;
		_itemComponent.OnUseItemRight += UseItemRight;
	}

	public override void _Process(double delta)	{

	}

	private void UseItemRight() {
		
	}
	private void UseItemLeft() {
		
	}
}

using Godot;
using System;

public partial class Item : RigidBody2D {	
	private ItemComponent _itemComponent;
	private PhysicsBody2D itemOwner;
	private CharacterBody2D charOwner = null;
	private RigidBody2D rigidOwner = null;
	
	
	public override void _Ready() {
		_itemComponent = GetNode<ItemComponent>("ItemComponent");
		
		_itemComponent.OnUseItemLeft += UseItemLeft;
		_itemComponent.OnUseItemRight += UseItemRight;

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

	private void UseItemRight() {
		
	}
	private void UseItemLeft() {
		
	}
}

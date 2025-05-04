using Godot;
using System;

public partial class MagnetSphere : RigidBody2D {	
	private ItemComponent _itemComponent;
	private PhysicsBody2D itemOwner;
	private CharacterBody2D charOwner = null;
	private RigidBody2D rigidOwner = null;
	private Magnet _magnet;
	private Magnet _magnet2;
	private Magnet _magnet3;
	private Magnet _magnet4;
	private Magnet _magnet5;
	private Magnet _magnet6;
	private Magnet _magnet7;
	private Magnet _magnet8;
	public override void _Ready() {
		_itemComponent = GetNode<ItemComponent>("ItemComponent");
		
		_itemComponent.OnUseItemLeft += UseItemLeft;
		_itemComponent.OnUseItemRight += UseItemRight;

		itemOwner = _itemComponent.GetItemOwner();

		_magnet = GetNode<Magnet>("Magnet");
		_magnet2 = GetNode<Magnet>("Magnet2");
		_magnet3 = GetNode<Magnet>("Magnet3");
		_magnet4 = GetNode<Magnet>("Magnet4");
		_magnet5 = GetNode<Magnet>("Magnet5");
		_magnet6 = GetNode<Magnet>("Magnet6");
		_magnet7 = GetNode<Magnet>("Magnet7");
		_magnet8 = GetNode<Magnet>("Magnet8");
	}

	public override void _Process(double delta) {
		if (itemOwner != _itemComponent.GetItemOwner()) {
			itemOwner = _itemComponent.GetItemOwner();
			if (itemOwner is CharacterBody2D c) {
				rigidOwner = null;				
				charOwner = c;
				SetMagnetsParent(charOwner);
			} else if (itemOwner is RigidBody2D r) {
				charOwner = null;				
				rigidOwner = r;
				SetMagnetsParent(rigidOwner);
			} else {
				charOwner = null;
				rigidOwner = null;
				SetMagnetsParent(this);
			}
		}
	}

	public void SetMagnetsParent(PhysicsBody2D parent) {
		if (parent is CharacterBody2D characterParent) {
			_magnet.SetCharacterParent(characterParent);
			_magnet2.SetCharacterParent(characterParent);
			_magnet3.SetCharacterParent(characterParent);
			_magnet4.SetCharacterParent(characterParent);
			_magnet5.SetCharacterParent(characterParent);
			_magnet6.SetCharacterParent(characterParent);
			_magnet7.SetCharacterParent(characterParent);
			_magnet8.SetCharacterParent(characterParent);
		}

		if (parent is RigidBody2D rigidParent) {
			_magnet.SetRigidParent(rigidParent);
			_magnet2.SetRigidParent(rigidParent);
			_magnet3.SetRigidParent(rigidParent);
			_magnet4.SetRigidParent(rigidParent);
			_magnet5.SetRigidParent(rigidParent);
			_magnet6.SetRigidParent(rigidParent);
			_magnet7.SetRigidParent(rigidParent);
			_magnet8.SetRigidParent(rigidParent);
		}
	}

	private void UseItemRight() {
		
	}
	private void UseItemLeft() {
		
	}
}


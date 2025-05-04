using Godot;
using System;

public partial class MagnetStick : RigidBody2D {	
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
	private Magnet _magnet9;
	
	public override void _Ready() {
		_itemComponent = GetNode<ItemComponent>("ItemComponent");
		_magnet = GetNode<Magnet>("Magnet");
		_magnet2 = GetNode<Magnet>("Magnet2");
		_magnet3 = GetNode<Magnet>("Magnet3");
		_magnet4 = GetNode<Magnet>("Magnet4");
		_magnet5 = GetNode<Magnet>("Magnet5");
		_magnet6 = GetNode<Magnet>("Magnet6");
		_magnet7 = GetNode<Magnet>("Magnet7");
		_magnet8 = GetNode<Magnet>("Magnet8");
		_magnet9 = GetNode<Magnet>("Magnet9");
		
		_itemComponent.OnUseItemLeft += UseItemLeft;
		_itemComponent.OnUseItemRight += UseItemRight;

		itemOwner = _itemComponent.GetItemOwner();

		_magnet.SetActivation(false, false);
		_magnet2.SetActivation(false, false);
		_magnet3.SetActivation(false, false);
		_magnet4.SetActivation(false, false);
		_magnet5.SetActivation(false, false);
		_magnet6.SetActivation(false, false);
		_magnet7.SetActivation(false, false);
		_magnet8.SetActivation(false, false);
		_magnet9.SetActivation(false, false);
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
			_magnet9.SetCharacterParent(characterParent);
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
			_magnet9.SetRigidParent(rigidParent);
		}
	}

	private void UseItemRight() {
		_magnet.ToggleActivation(false, true);
		_magnet2.ToggleActivation(false, true);
		_magnet3.ToggleActivation(false, true);
		_magnet4.ToggleActivation(false, true);
		_magnet5.ToggleActivation(false, true);
		_magnet6.ToggleActivation(false, true);
		_magnet7.ToggleActivation(false, true);
		_magnet8.ToggleActivation(false, true);
		_magnet9.ToggleActivation(false, true);
	}
	private void UseItemLeft() {
		_magnet.ToggleActivation(true, false);
		_magnet2.ToggleActivation(true, false);
		_magnet3.ToggleActivation(true, false);
		_magnet4.ToggleActivation(true, false);
		_magnet5.ToggleActivation(true, false);
		_magnet6.ToggleActivation(true, false);
		_magnet7.ToggleActivation(true, false);
		_magnet8.ToggleActivation(true, false);
		_magnet9.ToggleActivation(true, false);
	}
}


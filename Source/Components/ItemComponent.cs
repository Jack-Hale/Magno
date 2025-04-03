using Godot;
using Godot.Collections;
using System;

public partial class ItemComponent : Node
{
	private RigidBody2D parent;

	public event Action OnUseItemLeft;
	public event Action OnUseItemRight;
	private CharacterBody2D player;
	private CharacterBody2D itemParentChar;
	private RigidBody2D itemParentRig;

	private bool IsBeingHeld = false;
	public override void _Ready() {
		Array<Node> array = GetTree().Root.GetChildren();
		for (int i = 0; i < array.Count; i++) {
			player = array[i].GetNodeOrNull<CharacterBody2D>("Player");
		}
		if (GetParent() is RigidBody2D node) {
			parent = node;

			parent.AddToGroup("Item");
		} else {
			GD.PushError($"Item {GetParent()} {GetParent().Name} needs to be a RigidBody2D but is a {GetParent().GetType()}");
			GD.PrintErr($"Item {GetParent()} {GetParent().Name} needs to be a RigidBody2D but is a {GetParent().GetType()}");
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)	{
		if (GetParent() is Magnet magnet) {
			if (magnet.GetParent() is CharacterBody2D character) {
				itemParentChar = character;
				itemParentRig = null;
			} else if (magnet.GetParent() is RigidBody2D rigid) {
				itemParentRig = rigid;
				itemParentChar = null;
			} else {
				itemParentRig = null;
				itemParentChar = null;
			}
		}
	}

	public void SetIsBeingHeld(bool IsBeingHeld) {
		this.IsBeingHeld = IsBeingHeld;
	}

	public bool GetIsBeingHeld() {
		return IsBeingHeld;
	}

	public void UseItemLeft() {
		OnUseItemLeft?.Invoke();
	}
	public void UseItemRight() {
		OnUseItemRight?.Invoke();
	}

	public Player GetPlayer() {
		return (Player) player;
	}

	public PhysicsBody2D GetItemOwner() {
		if (itemParentChar != null) {
			return itemParentChar;
		} else if (itemParentRig != null) {
			return itemParentRig;
		} else {
			return null;
		}
	}
}

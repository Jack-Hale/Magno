using Godot;
using System;

public partial class ItemComponent : Node
{
	private RigidBody2D parent;

	public event Action OnUseItem;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
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

	}

	public void UseItem() {
		OnUseItem?.Invoke();
	}
}

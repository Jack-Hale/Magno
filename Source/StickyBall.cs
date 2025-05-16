using Godot;
using Godot.Collections;
using System;

public partial class StickyBall : RigidBody2D {	
	MagneticReparentComponent magneticReparent = new();
	MagneticComponent _magneticComponent;
	CharacterBody2D player;
	public override void _Ready() {
		Connect("body_entered", new Callable(this, MethodName.OnBodyEntered));
		ContactMonitor = true;
		MaxContactsReported = 5;

		_magneticComponent = GetNode<MagneticComponent>("MagneticComponent");

		Array<Node> array = GetTree().Root.GetChildren();
		for (int i = 0; i < array.Count; i++) {
			player = array[i].GetNodeOrNull<CharacterBody2D>("Player");
		}
	}

	public override void _Process(double delta) {

	}

	public void OnBodyEntered(Node body) {
		if (body is CharacterBody2D characterBody && body != player) {
			MagneticParentStruct magneticParentStruct = new(_magneticComponent.GetWeakMultiplier(), _magneticComponent.GetStrongMultiplier(), _magneticComponent.GetBlastMultiplier(), true);
			MagneticComponentStruct magneticComponentStruct = new(SwapCondition.SwapWhenHitSurface, 2, true, false, SwapCondition.SwapWhenHitSurface, 2);

			magneticReparent.RemagnifyCharater(characterBody.ToLocal(GlobalPosition), this, characterBody, magneticParentStruct, magneticComponentStruct);
		}
	}
}

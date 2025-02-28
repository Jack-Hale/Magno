using Godot;
using Godot.Collections;
using System;

public partial class DamageBall : Area2D
{
	private DamageComponent _damageComponent;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		Visible = false;

		_damageComponent = GetNode<DamageComponent>("DamageComponent");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)	{
		if (Visible) {
			GlobalPosition = GetGlobalMousePosition();
			_damageComponent.SetCanDamage(true);
		} else {
			_damageComponent.SetCanDamage(false);
		}

		if (Godot.Input.IsActionJustPressed("ToggleGodmode")) {
			Visible = !Visible;
		}
	}
}

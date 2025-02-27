using Godot;
using Godot.Collections;
using System;

public partial class DamageBall : Area2D
{

	Dictionary<PhysicsBody2D, HealthComponent> damageArray = new Dictionary<PhysicsBody2D, HealthComponent>();
	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		Connect("body_entered", new Callable(this, MethodName.OnBodyEntered));
		Connect("body_exited", new Callable(this, MethodName.OnBodyExited));

		Visible = false;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)	{
		if (Visible) {
			GlobalPosition = GetGlobalMousePosition();

			if (damageArray.Keys.Count > 0) {
				foreach (var body in damageArray.Keys) {
					HealthComponent healthComponent = damageArray[body];
					healthComponent.TakeDamage(3, 0.01f, GetInstanceId());
				}
			}
		}

		if (Godot.Input.IsActionJustPressed("ToggleGodmode")) {
			Visible = !Visible;
		}
	}

	public void OnBodyEntered(Node body) {
		if (body is PhysicsBody2D physicsBody) {
			Array<Node> bodyChildren = physicsBody.GetChildren();
			for (int i = 0; i < bodyChildren.Count; i++) {
				if (bodyChildren[i] is HealthComponent healthComponent && !damageArray.ContainsKey(physicsBody)) {
					damageArray.Add(physicsBody, healthComponent);

				}
			}
		}
		

	}

	public void OnBodyExited(Node body) {
		if (body is PhysicsBody2D physicsBody) {
			if (damageArray.ContainsKey(physicsBody)) {
				damageArray.Remove(physicsBody);
			}
		}
	}
}

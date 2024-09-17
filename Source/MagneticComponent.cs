using Godot;
using System;

public partial class MagneticComponent : Node2D
{
	[Export]
	public RigidBody2D Object;

	private bool attached = false;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {	
		GetParent().AddToGroup("Magnetic");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {
		Object.GravityScale = attached ? 0 : 1;
	}

    public void AttractObject(Vector2 collisionPoint, Vector2 attractionPoint, float beamLength) {
		attached = true;
		float magnetStrength = Math.Clamp(beamLength - attractionPoint.DistanceTo(Object.GlobalPosition), 1, beamLength);
		Object.ApplyForce((attractionPoint - Object.GlobalPosition) * magnetStrength, collisionPoint - Object.GlobalPosition);
	}

	public void Dettach() {
		attached = false;
	}
}
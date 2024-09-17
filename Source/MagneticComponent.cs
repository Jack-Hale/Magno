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

    public void ForceObject(Vector2 collisionPoint, Vector2 attractionPoint, float beamLength, bool pull) {
		attached = true;

		Object.SetCollisionMaskValue(2, false);
		Object.SetCollisionMaskValue(4, false);
		Object.SetCollisionLayerValue(3, false);
		Object.SetCollisionLayerValue(5, true);

		Vector2 pushForce = pull ? attractionPoint - Object.GlobalPosition : Object.GlobalPosition - attractionPoint;

		float magnetStrength = Math.Clamp(beamLength - attractionPoint.DistanceTo(Object.GlobalPosition), 1, beamLength);
		Object.ApplyForce(pushForce * magnetStrength, collisionPoint - Object.GlobalPosition);
	}

	public void Dettach() {
		attached = false;
		Object.SetCollisionMaskValue(2, true);
		Object.SetCollisionMaskValue(4, true);
		Object.SetCollisionLayerValue(3, true);
		Object.SetCollisionLayerValue(5, false);
	}
}
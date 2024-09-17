using Godot;
using System;
using System.Reflection;

public partial class MagneticComponent : Node2D
{
	[Export]
	public RigidBody2D Object;

	private bool attached = false;

	private Joint2D joint;
	private PhysicsBody2D parent;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {	
		GetParent().AddToGroup("Magnetic");

		if (Object.GetParent() is PhysicsBody2D) {
			parent = (PhysicsBody2D) Object.GetParent();
			foreach (var child in Object.GetParent().GetChildren()) {
				if (child is Joint2D) {
					joint = (Joint2D) child;
				}
			}
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {
		Object.GravityScale = attached ? 0 : 1;

		if (parent != null) {

			// GD.Print(parent);
			if (parent.GlobalPosition.DistanceTo(Object.GlobalPosition) > 12) {
				joint.NodeB = null;
			}
		}
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
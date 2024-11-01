using Godot;
using System;
using System.Reflection;

public partial class MagneticComponent : Node2D
{
	[Export]
	private RigidBody2D Object;
	[Export]
	private float ObjectDampener = 4;	
	[Export]
	private float CharacterDampener = 50;
	[Export]
	private float ObjectMultiplier = 5;
	[Export]
	private float CharacterMultiplier;
	private CharacterBody2D characterObject;
	private bool attached = false;

	private Joint2D joint;
	private PhysicsBody2D parent;

	private bool connected;

	private Area2D _magnetHoldRegion;

	private Vector2 draw1 = Vector2.Zero;
	private Vector2 draw2 = Vector2.Zero;
	

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		GetParent().AddToGroup("Magnetic");
		PhysicsBody2D parent = null;

		// Gaining access to parent of the magnetic object if there is one
		if (Object.GetParent() is PhysicsBody2D) {
			parent = (PhysicsBody2D) Object.GetParent();

			// Getting the joint connecting the parent to the object
			foreach (var child in Object.GetParent().GetChildren()) {
				if (child is Joint2D) {
					joint = (Joint2D) child;
				}
			}
		}

		// Handling if parent object is a CharacterBody2D
		if (parent != null && parent is CharacterBody2D) {
			connected = true;
			characterObject = (CharacterBody2D) parent;

			// Making the parent respond to magnets
			characterObject.AddToGroup("Magnetic");

			// Connecting the magnet hold region exit trigger
			if (characterObject.FindChild("MagnetHoldRegion") != null) {
				_magnetHoldRegion = characterObject.GetNode<Area2D>("MagnetHoldRegion");

        		_magnetHoldRegion.Connect("body_exited", new Callable(this, MethodName.OnBodyExited));
			} else {
				GD.PrintErr(characterObject.Name, " HAS NO \"MagnetHoldRegion\"");
				GD.PushError(characterObject.Name, " HAS NO \"MagnetHoldRegion\"");
			}
		} else {
			connected = false;
		}
	}
	
	private void OnBodyExited(Node body)
    {
		// If Object has exited, disconnect all trace of Object from characterObject
    	if (body == Object) {
			connected = false;
		}
    }

	public override void _Draw()
    {
        DrawLine(ToLocal(draw1), ToLocal(draw2), Colors.Red, 1.0f);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta) {
		if (Object != null) {
			// Object.GravityScale = attached ? 0 : 1;
		}
		draw1 = Object.GlobalPosition;
		if (Object.GetParent() is PhysicsBody2D pb)
		draw2 = (pb.Position);
		
		// Removes any connection between Object and characterObject
		if (characterObject != null && !connected) {
			// Disconnect object from parent joint
			joint.NodeB = null;

			// Store object space data
			Vector2 ObjectPosition = Object.GlobalPosition;
			float ObjectRotation = Object.GlobalRotation;
			Vector2 ObjectVelocity = Object.LinearVelocity;

			// Store the scene tree to put the object back into
			SceneTree sceneTree = GetTree();

			// Remove all reference from parent to object
			characterObject.RemoveFromGroup("Magnetic");
			characterObject.RemoveChild(Object);
			characterObject = null;

			// Add object back into scene tree
			sceneTree.Root.AddChild(Object);

			// Return object to it's original movement state
			Object.GlobalPosition = ObjectPosition;
			Object.GlobalRotation = ObjectRotation;
			Object.LinearVelocity = ObjectVelocity;
		}

		QueueRedraw();
	}

    public void ForceObject(Vector2 collisionPoint, Vector2 attractionPoint, float beamLength, bool pull, bool strongMagnet) {
		attached = true;

		Object.SetCollisionMaskValue(2, false);
		Object.SetCollisionLayerValue(3, false);
		Object.SetCollisionLayerValue(5, true);
		
		// Vector that is positive or negative depending on what pull mode the magnet is in
		Vector2 pushForce = pull ? attractionPoint - Object.GlobalPosition : Object.GlobalPosition - attractionPoint;
		// GD.Print(pushForce);

		// Vector that is larger the closer the Object is to the magnet
		float magnetStrength = Math.Clamp(beamLength - attractionPoint.DistanceTo(Object.GlobalPosition), 1, beamLength);

		// Push the object at a higher velocity
		if (strongMagnet) {
			if (characterObject != null) {
				
			} else if (Object != null) {
				Object.ApplyForce(pushForce * magnetStrength * ObjectMultiplier, collisionPoint - Object.GlobalPosition);
			}
		} else {
			// Handle force if parent exists. Apply force to the parent not the metal object
			if (characterObject != null) {
				// TODO: Disable the movement of the characterObject defined by the object itself
				characterObject.AddToGroup("Affected");

				characterObject.Velocity = pushForce * magnetStrength / CharacterDampener;
				characterObject.MoveAndSlide();

			// Handle force if no parent
			} else if (Object != null) {
				Object.ApplyForce(pushForce * magnetStrength / ObjectDampener, collisionPoint - Object.GlobalPosition);
			}
		}
	}

	public void ForceObjectWtihArea(Vector2 collisionPoint, Vector2 attractionPoint, float beamLength, bool pull, bool strongMagnet, double delta) {
		attached = true;

		Object.SetCollisionMaskValue(2, false);
		Object.SetCollisionLayerValue(3, false);
		Object.SetCollisionLayerValue(5, true);
		
		// Vector that is positive or negative depending on what pull mode the magnet is in
		Vector2 pushForce = pull ? attractionPoint - Object.GlobalPosition : Object.GlobalPosition - attractionPoint;
		// GD.Print(pushForce);
	
		// Vector that is larger the closer the Object is to the magnet
		float magnetStrength = Math.Clamp(beamLength - attractionPoint.DistanceTo(Object.GlobalPosition), 1, beamLength);
		// Push the object at a higher velocity
		// if (strongMagnet) {
		// 	if (characterObject != null) {
				
		// 	} else if (Object != null) {
		// 		Object.ApplyForce(pushForce * magnetStrength * ObjectMultiplier * (float)delta, Object.GlobalPosition);
		// 	}
		// } else {
			// Handle force if parent exists. Apply force to the parent not the metal object
			if (characterObject != null) {
				// TODO: Disable the movement of the characterObject defined by the object itself
				characterObject.AddToGroup("Affected");

				characterObject.Velocity = pushForce * magnetStrength * 1 * (float)delta;
				// characterObject.Velocity = pushForce * magnetStrength / CharacterDampener * (float)delta;
				characterObject.MoveAndSlide();

			// Handle force if no parent
			} else if (Object != null) {
				// Object.ApplyForce(pushForce * magnetStrength / ObjectDampener * (float)delta, Object.GlobalPosition);
				Object.ApplyForce(pushForce * magnetStrength * 20 * (float)delta, collisionPoint - Object.GlobalPosition);
				// draw1 = Object.GlobalPosition;
				// draw2 = pushForce * magnetStrength * 1 * (float)delta;
				// Object.ApplyForce(attractionPoint - Object.GlobalPosition * 100 * (float)delta);
			}
		// }
	}

	public void Dettach() {
		attached = false;
		Object.SetCollisionMaskValue(2, true);
		Object.SetCollisionLayerValue(3, true);
		Object.SetCollisionLayerValue(5, false);

		if (characterObject != null) {
			characterObject.RemoveFromGroup("Affected");
		}
	}

	public RigidBody2D GetObject() {
		return Object;
	}

	public CharacterBody2D GetCharacterObject() {
		if (characterObject != null) {
			return characterObject;
		}
		return null;
	}
	
	public bool IsCharacterObject() {
		if (characterObject != null) {
			return true;
		}
		return false;
	}
}
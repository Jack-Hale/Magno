using Godot;
using System;
using System.Reflection;

public partial class MagneticComponent : Node2D
{
	[Export]
	private RigidBody2D Object;

	[Export]
	private CharacterBody2D CharacterObject;

	[Export]
	private float WeakMultiplier = 16;	
	[Export]
	private float StrongMultiplier = 40;
	[Export]
	private float BlastMultiplier = 1000;

	private bool attached = false;

	private Joint2D joint;
	private Magnet parent;

	private bool connected;

	private Area2D _magnetHoldRegion;

	private Vector2 draw1 = Vector2.Zero;
	private Vector2 draw2 = Vector2.Zero;
	

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		GetParent().AddToGroup("Magnetic");
		PhysicsBody2D parent = null;

		if (Object != null) {
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
			if (parent is CharacterBody2D) {
				connected = true;
				CharacterObject = (CharacterBody2D) parent;

				// Object.SetCollisionLayerValue(3, false);

				// Making the parent respond to magnets
				CharacterObject.AddToGroup("Magnetic");
				
				Object.RemoveFromGroup("Magnetic");

				// Connecting the magnet hold region exit trigger
				if (CharacterObject.FindChild("MagnetHoldRegion") != null) {
					_magnetHoldRegion = CharacterObject.GetNode<Area2D>("MagnetHoldRegion");

					_magnetHoldRegion.Connect("body_exited", new Callable(this, MethodName.OnBodyExited));
				} else {
					GD.PrintErr(CharacterObject.Name, " HAS NO \"MagnetHoldRegion\"");
					GD.PushError(CharacterObject.Name, " HAS NO \"MagnetHoldRegion\"");
				}
			} else {
				connected = false;
			}

			if (parent != null && parent is RigidBody2D rigid) {
				rigid.ContinuousCd = RigidBody2D.CcdMode.CastShape;
			}
		} else if (CharacterObject != null) {
			
		} else {
			GD.PushError("No Object or Character Object assigned to ", this);
		}
	}
	
	private void OnBodyExited(Node body)
    {
		// If Object has exited, disconnect all trace of Object from characterObject
    	if (body == Object) {
			// connected = false;
		}
    }

	public override void _Draw()
    {
        // DrawLine(ToLocal(draw1), ToLocal(draw2), Colors.Red, 1.0f);
	}

    public override void _Process(double delta)
    {
		
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _PhysicsProcess(double delta) {
		
		// Removes any connection between Object and characterObject
		if (Object != null && CharacterObject != null && !connected) {
			// Disconnect object from parent joint
			joint.NodeB = null;

			// Store object space data
			Vector2 ObjectPosition = Object.GlobalPosition;
			float ObjectRotation = Object.GlobalRotation;
			Vector2 ObjectVelocity = Object.LinearVelocity;

			// Store the scene tree to put the object back into
			SceneTree sceneTree = GetTree();

			// Remove all reference from parent to object
			CharacterObject.RemoveFromGroup("Magnetic");
			CharacterObject.RemoveChild(Object);
			// GD.Print("disconnect", characterObject);
			CharacterObject = null;

			// Add object back into scene tree
			sceneTree.Root.AddChild(Object);

			// Return object to it's original movement state
			Object.GlobalPosition = ObjectPosition;
			Object.GlobalRotation = ObjectRotation;
			Object.LinearVelocity = ObjectVelocity;
		}

		QueueRedraw();
	}

	public bool IsBeingHeld() {
		Node parentCheck = GetParent();

		// Iterate through parents until Magnet or Root is found
		while (parentCheck is not Magnet && parentCheck != GetTree().Root) {
			parentCheck = parentCheck.GetParent();
		}

		return parentCheck is Magnet;
	}

	public void ForceObject(Vector2 collisionPoint, Vector2 attractionPoint, float beamLength, bool pull, bool strongMagnet, bool blast, double delta) {
		attached = true;

		// Object.SetCollisionMaskValue(2, false);
		// Object.SetCollisionLayerValue(1, false);
		// Object.SetCollisionLayerValue(3, false);
		// Object.SetCollisionLayerValue(5, true);

		Vector2 pushForce;
		float magnetStrength;

		// Handle force if parent exists. Apply force to the parent not the metal object
		if (CharacterObject != null) {
			// Vector that is positive or negative depending on what pull mode the magnet is in
			pushForce = pull ? attractionPoint - CharacterObject.GlobalPosition : CharacterObject.GlobalPosition - attractionPoint;
		
			// Vector that is larger the closer the Object is to the magnet
			magnetStrength = Math.Clamp(beamLength - attractionPoint.DistanceTo(CharacterObject.GlobalPosition), 1, beamLength);

			// TODO: Disable the movement of the characterObject defined by the object itself
			CharacterObject.AddToGroup("Affected");

			CharacterObject.Velocity = pushForce * magnetStrength * 1 * (float)delta;
			// characterObject.Velocity = pushForce * magnetStrength / CharacterDampener * (float)delta;
			CharacterObject.MoveAndSlide();


		// Handle force if no parent
		} else if (Object != null) {
			// Vector that is positive or negative depending on what pull mode the magnet is in
			pushForce = pull ? attractionPoint - Object.GlobalPosition : Object.GlobalPosition - attractionPoint;
		
			// Vector that is larger the closer the Object is to the magnet
			magnetStrength = Math.Clamp(beamLength - attractionPoint.DistanceTo(Object.GlobalPosition), 1, beamLength);

			float multiplier = blast ? BlastMultiplier : strongMagnet ? StrongMultiplier : WeakMultiplier;
			
			Object.ApplyForce(pushForce * magnetStrength * multiplier * (float)delta, collisionPoint - Object.GlobalPosition);
		}
	}

	public void Dettach() {
		attached = false;
		// Object.SetCollisionMaskValue(2, true);
		// Object.SetCollisionLayerValue(1, true);
		// Object.SetCollisionLayerValue(3, true);
		// Object.SetCollisionLayerValue(5, false);

		if (CharacterObject != null) {
			CharacterObject.Rotation = 0;
			CharacterObject.RemoveFromGroup("Affected");
		}
	}

	public void SetMagnetParent(Magnet newParent) {
		parent = newParent;
	}

	public Magnet GetMagnetParent() {
		return parent;
	}

	public RigidBody2D GetObject() {
		return Object;
	}

	public CharacterBody2D GetCharacterObject() {
		if (CharacterObject != null) {
			return CharacterObject;
		}
		return null;
	}
	
	public bool IsCharacterObject() {
		if (CharacterObject != null) {
			return true;
		}
		return false;
	}

	public void ZeroVelocity() {
		Object.LinearVelocity = Vector2.Zero;
		Object.AngularVelocity = 0;
	}
}
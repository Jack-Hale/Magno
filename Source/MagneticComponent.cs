using Godot;
using System;
using System.Reflection;

public partial class MagneticComponent : Node2D
{
	[Export]
	private RigidBody2D Object;
	private CharacterBody2D characterObject;
	private bool attached = false;

	private Joint2D joint;
	private PhysicsBody2D parent;

	private bool connected;

	private Area2D _magnetHoldRegion;

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
			// connected = false;
		}
    }

	public override void _Draw()
    {
        // DrawLine(ToLocal(Object.GlobalPosition), ToLocal(characterObject.GlobalPosition), Colors.Red, 1.0f);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta) {
		if (Object != null) {
			// Object.GravityScale = attached ? 0 : 1;
		}

		// Removes any connection between Object and characterObject
		if (characterObject != null && !connected) {
			GD.Print("HERE");
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

    public void ForceObject(Vector2 collisionPoint, Vector2 attractionPoint, float beamLength, bool pull) {
		attached = true;

		Object.SetCollisionMaskValue(2, false);
		Object.SetCollisionLayerValue(3, false);
		Object.SetCollisionLayerValue(5, true);

		// Vector that is positive or negative depending on what pull mode the magnet is in
		Vector2 pushForce = pull ? attractionPoint - Object.GlobalPosition : Object.GlobalPosition - attractionPoint;

		// Vector that is larger the closer the Object is to the magnet
		float magnetStrength = Math.Clamp(beamLength - attractionPoint.DistanceTo(Object.GlobalPosition), 1, beamLength);

		// Handle force if parent exists. Apply force to the parent not the metal object
		if (characterObject != null) {
			// TODO: Disable the movement of the characterObject defined by the object itself
			characterObject.AddToGroup("Affected");

			//TODO: Make the dampener value based on the mass of the object pulled
			float dampener = 50;
			characterObject.Velocity = pushForce * magnetStrength / dampener;
			characterObject.MoveAndSlide();

		// Handle force if no parent
		} else if (Object != null) {
			Object.ApplyForce(pushForce * magnetStrength/4, collisionPoint - Object.GlobalPosition);
		}
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
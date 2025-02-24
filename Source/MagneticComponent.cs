using Godot;
using System;
using System.Reflection;

public partial class MagneticComponent : Node2D
{
	[Export]
	private float weakMultiplier = 7;	
	[Export]
	private float strongMultiplier = 10;
	[Export]
	private float blastMultiplier = 400;

	private RigidBody2D rigidObject;
	private CharacterBody2D characterObject;
	private Joint2D joint;
	private Magnet magnetParent;
	private MagneticCharacterComponent magCharComp;

	private RigidBody2D bodyCopy;

	private bool connected;

	private Area2D _magnetHoldRegion;

	private Vector2 draw1 = Vector2.Zero;
	private Vector2 draw2 = Vector2.Zero;

	private Node parent;
	private RigidBody2D objectCollisionL = new RigidBody2D();
	private RigidBody2D objectCollisionM = new RigidBody2D();
	
	public MagneticComponent() {
		Name = "MagneticComponent";
		AddToGroup("MagneticComponent");
	}
	public MagneticComponent(MagneticCharacterComponent magneticCharacterComponent) {
		magCharComp = magneticCharacterComponent;
		Name = "MagneticComponent";
		AddToGroup("MagneticComponent");
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		Node parent = GetParent();

		if (parent is RigidBody2D) {
			rigidObject = (RigidBody2D)parent;
			Node objectParent = rigidObject.GetParent();

			rigidObject.AddToGroup("Magnetic");

			// Checks if the magnetic object is a part of a larger body that it needs to
			// impart its magneticism onto
			if (objectParent is PhysicsBody2D) {
				objectParent = (PhysicsBody2D) objectParent;
				Sprite2D magnetSprite = null;
				
				// Getting a copy of the sprite to add to the parent
				foreach (var child in rigidObject.GetChildren()) {
					if (child is Sprite2D) {
						magnetSprite = (Sprite2D) child.Duplicate();
						break;
					}
				}

				if (magnetSprite != null) {
					magnetSprite.Position = rigidObject.Position;
					objectParent.CallDeferred("add_child", magnetSprite);
				}

				// Disabling the rigid object while it is within the larger object
				for (int i = 1; i <= 32; i++) {
					objectCollisionL.SetCollisionLayerValue(i, rigidObject.GetCollisionLayerValue(i));
					objectCollisionM.SetCollisionMaskValue(i, rigidObject.GetCollisionMaskValue(i));

					rigidObject.SetCollisionLayerValue(i, false);
					rigidObject.SetCollisionMaskValue(i, false);
				}
				
				rigidObject.Visible = false;
				rigidObject.Sleeping = true;
				// Object.ProcessMode = ProcessModeEnum.Disabled;

			}

			// Handling if parent object is a CharacterBody2D
			if (objectParent is CharacterBody2D) {
				foreach (var child in objectParent.GetParent().GetChildren()) {
					if (child is MagneticCharacterComponent) {
						magCharComp = (MagneticCharacterComponent)child;
						break;
					}
				}

				if (magCharComp == null) {
					GD.PrintErr(objectParent, " requires MagneticCharacterComponent");
					GD.PushError(objectParent, " requires MagneticCharacterComponent");
				}

				// Generates the RigidBody2D copy of the character
				bodyCopy = magCharComp.InitialiseBodyCopy();

				connected = true;
				characterObject = (CharacterBody2D) objectParent;
				characterObject.AddToGroup("Magnetic");


				// Connecting the magnet hold region exit trigger
				// if (CharacterObject.FindChild("MagnetHoldRegion") != null) {
				// 	_magnetHoldRegion = CharacterObject.GetNode<Area2D>("MagnetHoldRegion");

				// 	_magnetHoldRegion.Connect("body_exited", new Callable(this, MethodName.OnBodyExited));
				// } else {
				// 	GD.PrintErr(CharacterObject.Name, " HAS NO \"MagnetHoldRegion\"");
				// 	GD.PushError(CharacterObject.Name, " HAS NO \"MagnetHoldRegion\"");
				// }
			}

			if (parent != null && parent is RigidBody2D rigid) {
				rigid.ContinuousCd = RigidBody2D.CcdMode.CastShape;
			}

		} else if (parent is CharacterBody2D objectParent) {

			foreach (var child in objectParent.GetParent().GetChildren()) {
				if (child is MagneticCharacterComponent) {
					magCharComp = (MagneticCharacterComponent)child;
					break;
				}
			}

			if (magCharComp == null) {
				GD.PrintErr(objectParent, " requires MagneticCharacterComponent");
				GD.PushError(objectParent, " requires MagneticCharacterComponent");
			}

			// Generates the RigidBody2D copy of the character
			bodyCopy = magCharComp.InitialiseBodyCopy();

			connected = true;
			characterObject = objectParent;
			characterObject.AddToGroup("Magnetic");
		} else {
			GD.PrintErr($"parent of {Name}:{this} ({parent.Name} {parent}) is not RigidBody2D");
			GD.PushError($"parent of {Name}:{this} ({parent.Name} {parent}) is not RigidBody2D");
		}
	}
	
	// private void OnBodyExited(Node body)
    // {
	// 	// If Object has exited the magnet hold region, disconnect all trace of Object from characterObject
    // 	if (body == Object) {
	// 		// connected = false;
	// 	}
    // }

	public override void _Draw()
    {
        // DrawLine(ToLocal(draw1), ToLocal(draw2), Colors.Red, 1.0f);
	}

    public override void _Process(double delta)
    {
		
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _PhysicsProcess(double delta) {
		// // Removes any connection between Object and characterObject
		// if (Object != null && CharacterObject != null && !connected) {
		// 	// Disconnect object from parent joint
		// 	joint.NodeB = null;

		// 	// Store object space data
		// 	Transform2D transform = Object.Transform;

		// 	// Store the scene tree to put the object back into
		// 	SceneTree sceneTree = GetTree();

		// 	// Remove all reference from parent to object
		// 	CharacterObject.RemoveFromGroup("Magnetic");
		// 	CharacterObject.RemoveChild(Object);
		// 	// GD.Print("disconnect", characterObject);
		// 	CharacterObject = null;

		// 	// Add object back into scene tree
		// 	sceneTree.Root.AddChild(Object);

		// 	// Return object to it's original movement state
		// 	Object.Transform = transform;

		// 	Object.AddToGroup("Magnetic");
		// }

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

	// Applies the Magnetic force onto the parent object
	public void ForceObject(Vector2 collisionPoint, Vector2 attractionPoint, float beamLength, bool pull, bool strongMagnet, bool blast, double delta) {

		// Vector that is positive or negative depending on what pull mode the magnet is in
		Vector2 pushForce = pull ? attractionPoint - rigidObject.GlobalPosition : rigidObject.GlobalPosition - attractionPoint;
	
		// Vector that is larger the closer the Object is to the magnet
		float magnetStrength = Math.Clamp(beamLength - attractionPoint.DistanceTo(rigidObject.GlobalPosition), 1, beamLength);

		float multiplier = blast ? blastMultiplier : strongMagnet ? strongMultiplier : weakMultiplier;
		
		if (rigidObject != null) {
			rigidObject.ApplyForce(pushForce * magnetStrength * multiplier * (float)delta, collisionPoint - rigidObject.GlobalPosition);
		}
	}

	public void Dettach() {
		if (characterObject != null) {
			characterObject.RemoveFromGroup("Affected");
		}
	}

	public MagneticCharacterComponent GetMagneticCharacterComponent() {
		return magCharComp;
	}

	public void SetMagnetParent(Magnet newParent) {
		magnetParent = newParent;
	}

	public Magnet GetMagnetParent() {
		return magnetParent;
	}

	public RigidBody2D GetObject() {
		return rigidObject;
	}
}
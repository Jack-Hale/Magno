using Godot;
using Godot.Collections;
using System;

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
	private Magnet magnetParent;
	private MagneticCharacterComponent magCharComp;

	private RigidBody2D bodyCopy;

	private Vector2 draw1 = Vector2.Zero;
	private Vector2 draw2 = Vector2.Zero;
	private float exitTimer = 0;
	private bool inExitSequence = false;
	private bool largeCharacter;
	private float exitTimerDefault;


	private Node rigidObjectParent;
	private Sprite2D magnetSprite = null;

	private RigidBody2D objectCollisionL = new RigidBody2D();
	private RigidBody2D objectCollisionM = new RigidBody2D();

	private Tuple<bool, Vector2> magnetData = new Tuple<bool, Vector2>(false, Vector2.Inf);
	private Tuple<Vector2, Vector2> forceData = new Tuple<Vector2, Vector2>(Vector2.Zero, Vector2.Zero);
	private Tuple<bool, bool> strengthData = new Tuple<bool, bool>(false, false);
	
	public MagneticComponent() {
		Name = "MagneticComponent";
		AddToGroup("MagneticComponent");
	}
	public MagneticComponent(MagneticCharacterComponent magneticCharacterComponent) {
		magCharComp = magneticCharacterComponent;
		Name = "MagneticComponent";
		AddToGroup("MagneticComponent");
		largeCharacter = magCharComp.GetLargeCharacter();
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

				exitTimerDefault = magCharComp.GetExitTimerDefault();
				largeCharacter = magCharComp.GetLargeCharacter();

				rigidObjectParent = magCharComp.GetParent().GetParent();

				// Generates the RigidBody2D copy of the character
				bodyCopy = magCharComp.InitialiseBodyCopy();

				characterObject = (CharacterBody2D) objectParent;
				characterObject.AddToGroup("Magnetic");
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

			characterObject = objectParent;
			characterObject.AddToGroup("Magnetic");
		} else {
			GD.PrintErr($"parent of {Name}:{this} ({parent.Name} {parent}) is not RigidBody2D");
			GD.PushError($"parent of {Name}:{this} ({parent.Name} {parent}) is not RigidBody2D");
		}
	}

	public override void _Draw() {
        // DrawLine(ToLocal(draw1), ToLocal(draw2), Colors.Red, 4.0f);
	}

    public override void _Process(double delta) {

    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _PhysicsProcess(double delta) {
		if (characterObject != null && rigidObject != null) {
			switch (magCharComp.exitCondition)
			{
				case ExitCondition.TimeLimit:
					if (!inExitSequence && characterObject.IsInGroup("Affected")) {
						exitTimer = exitTimerDefault;
						inExitSequence = true;
					}

					if (inExitSequence) {
						if (exitTimer > 0) {
							exitTimer -= (float) delta;
						} else {
							EnableRigidObject();
							inExitSequence = false;
						}

						if (inExitSequence && !characterObject.IsInGroup("Affected")) {
							inExitSequence = false;
						}
					}

					break;
				case ExitCondition.StrongForce:
					if (magCharComp.GetBodyCopyStrengthData().Item2) {
						EnableRigidObject();
					}
					break;
				case ExitCondition.Throw:
					if (magCharComp.GetBodyCopyStrengthData().Item1) {
						EnableRigidObject();
					}
					break;
			}
		}

		QueueRedraw();
	}

	// Destroys connection between Character and Rigid objects and removes any ability for Character to be magnetic
	public void EnableRigidObject() {
		if (characterObject != null) {
			magCharComp.SwapToCharacter();
			Node parent = rigidObject.GetParent();
			parent.RemoveChild(rigidObject);
			
			rigidObjectParent.AddChild(rigidObject);


			for (int i = 1; i <= 32; i++) {
				rigidObject.SetCollisionLayerValue(i, objectCollisionL.GetCollisionLayerValue(i));
				rigidObject.SetCollisionMaskValue(i, objectCollisionM.GetCollisionMaskValue(i));
			}
			characterObject.RemoveChild(magnetSprite);

			rigidObject.Visible = true;
			rigidObject.Sleeping = false;

			Sprite2D characterSprite = null;
			Array<Node> characterChildren = characterObject.GetChildren();
			for (int i = 0; i < characterChildren.Count; i++) {
				if (characterChildren[i] is Sprite2D sprite) {
					characterSprite = sprite;
				}
			}

			float width = characterSprite.Texture.GetWidth();
			float height = characterSprite.Texture.GetHeight();
			double characterSize = Math.Sqrt(Math.Pow(width/2, 2) + Math.Pow(height/2, 2));
			GD.Print($"width {width/2} heigh {height/2} angle {characterSize}");

			// Ensures the rigidObject spawns in the direction of the magnet force when exiting character
			var direction = (magCharComp.GetBodyCopyMagnetData().Item2 - characterObject.GlobalPosition).Normalized();
			Vector2 spawnLocation = characterObject.GlobalPosition + (magCharComp.GetBodyCopyMagnetData().Item1 ? 1 : -1) * (direction * ((float)characterSize));

			rigidObject.GlobalPosition = spawnLocation;
			rigidObject.LinearVelocity = Vector2.Zero;

			characterObject = null;
			largeCharacter = false;
			magCharComp.DettachMetalObject();
			magCharComp = null;
		}
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
	public void ForceObject(Vector2 collisionPoint, Vector2 attractionPoint, float beamLength, bool pull, bool strongMagnet, bool blast, double delta, bool largeCharacter) {
		

		// Vector that is positive or negative depending on what pull mode the magnet is in
		Vector2 pushForce = pull ? attractionPoint - rigidObject.GlobalPosition : rigidObject.GlobalPosition - attractionPoint;
	
		// Vector that is larger the closer the Object is to the magnet
		float magnetStrength = Math.Clamp(beamLength - attractionPoint.DistanceTo(rigidObject.GlobalPosition), 1, beamLength);

		float multiplier = blast ? blastMultiplier : strongMagnet ? strongMultiplier : weakMultiplier;

		Vector2 force = pushForce * magnetStrength * multiplier * (float)delta;
		Vector2 position = collisionPoint - rigidObject.GlobalPosition;
		
		magnetData = new Tuple<bool, Vector2>(pull, attractionPoint);
		forceData = new Tuple<Vector2, Vector2>(force, position);
		strengthData = new Tuple<bool, bool>(blast, strongMagnet);

		if (!largeCharacter) {
			if (rigidObject != null) {
				rigidObject.ApplyForce(force, position);
			}
		}
	}

	public Tuple<bool, Vector2> GetMagnetData() {
		return magnetData;
	}

	public Tuple<Vector2, Vector2> GetForceData() {
		return forceData;
	}

	public Tuple<bool, bool> GetStrengthData() {
		return strengthData;
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

	public bool GetLargeCharacter() {
		return largeCharacter;
	}
}
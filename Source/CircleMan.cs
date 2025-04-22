using Godot;
using Godot.Collections;
using System;

public partial class CircleMan : CharacterBody2D {
	public float maxSpeed = 100.0f;
	public float acceleration = 60.0f;
	float friction = 2200;
	float airAcceleration = 1800;

	public float jumpVelocity = -400.0f;

	private bool affected = true;

	private MagneticCharacterComponent magCharComp = null;
	private PathFindingComponent _pathFindingComponent;

	// The pull mode of the magnet affecting the enemy
	// Pulling = true, Pushing = false
	private bool magnetPull = false;

	// Position of the magnet affecting the enemy 
	private Vector2 magnetAttractionPoint = Vector2.Zero; 

	// Force applied to the enemy from one magnet
	private Vector2 magnetForce = Vector2.Zero;

	// Position on the enemy the force is being applied
	private Vector2 magnetForcePosition = Vector2.Zero;  

	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	private float throwCooldownMax = 2;
	private float throwCooldown = 0;
	private bool collectedMagnetObjects = false;

	private RigidBody2D _hand1;
	private RigidBody2D _hand2;
	private Area2D hand1Copy;
	private Area2D hand2Copy;
	public override void _Ready() {
		foreach (var child in GetParent().GetChildren()) {
			if (child is MagneticCharacterComponent) {
				magCharComp = (MagneticCharacterComponent) child;
			}
		}

		_pathFindingComponent = GetNode<PathFindingComponent>("PathFindingComponent");
		_hand1 = GetNode<RigidBody2D>("Hand1");
		_hand2 = GetNode<RigidBody2D>("Hand2");
	}

    public override void _Process(double delta) {
		// Waiting for duplicate sprites to exist to extract
		if (!collectedMagnetObjects) {
			Dictionary<Area2D, NodePath> objects = magCharComp.GetDuplicateObjects();

			// If none exist, arrays will be null
			collectedMagnetObjects = objects == null;

			if (objects != null && objects.Count > 0) {
				foreach (var item in objects.Keys) {
					if (objects[item] == magCharComp.GetParent().GetPathTo(_hand1)) {
						hand1Copy = item;
					}

					if (objects[item] == magCharComp.GetParent().GetPathTo(_hand2)) {
						hand2Copy = item;
					}
				}
			}
		}
    }


	public override void _PhysicsProcess(double delta) {
		Vector2 velocity = Velocity;
		Vector2 direction = Vector2.Zero;


		if (throwCooldown > 0) {
			throwCooldown -= (float) delta;
		}


		if (IsInGroup("CanSeePlayer") || IsInGroup("LookingForPlayer")) {
			if (throwCooldown <= 0) {
				ThrowBox();
				throwCooldown = throwCooldownMax;
			}
		}

		
		// Handles magnetic states
		if (IsInGroup("Magnetic")) {
			if (magCharComp == null) {
				GD.PrintErr($"No MagneticCharacterComponent found on magnetic character {this} {Name}");
				GD.PushError($"No MagneticCharacterComponent found on magnetic character {this} {Name}");
			}
			if (IsInGroup("Affected")) {
				affected = true;
				Tuple<bool, Vector2> magentData = magCharComp.GetBodyCopyMagnetData();
				magnetPull = magentData.Item1;
				magnetAttractionPoint = magentData.Item2;

				Tuple<Vector2, Vector2> forceData = magCharComp.GetBodyCopyForceData();
				magnetForce = forceData.Item1;
				magnetForcePosition = forceData.Item2;
			} else {
				affected = false;
				magnetPull = false;
				magnetAttractionPoint = Vector2.Zero;
			}
		} else {
			affected = false;
			magCharComp = null;
		}

		// Add the gravity.
		if (!IsOnFloor())
			velocity.Y += gravity * (float)delta;

		if (affected) {
			// Handle behaviour when affected by a magnet
			
		} else {
			// Handle behaviour when unaffected by a magnet
			
		}

		if (Godot.Input.IsActionJustPressed("ToggleGodmode")) {
			ThrowBox();
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	private void ThrowBox() {
		if (hand1Copy != null && hand2Copy != null) {
			PackedScene scene = (PackedScene)GD.Load("res://Scenes/Objects/big_box.tscn");
			RigidBody2D box = (RigidBody2D) scene.Instantiate();
			if (GlobalPosition.DirectionTo(_pathFindingComponent.GetLastDetectionPoint()).X > 0) {
				box.GlobalPosition = hand1Copy.GlobalPosition;
			} else {
				box.GlobalPosition = hand2Copy.GlobalPosition;
			}
			box.LinearVelocity = box.GlobalPosition.DirectionTo(_pathFindingComponent.GetLastDetectionPoint()) * 4000;

			Node tree = GetTree().Root.GetChild(0);
			tree.AddChild(box);
		}
	}
}

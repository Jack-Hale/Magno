using Godot;
using System;

public partial class Slime : CharacterBody2D
{
	public float maxSpeed = 300.0f;
	public float jumpVelocity = -400.0f;

	float friction = 2200;
	float acceleration = 2200;
	float airAcceleration = 1800;

	private bool affected = true;

	private MagneticCharacterComponent magCharComp = null;

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
	private PathFindingComponent _pathFinding;
	private CharacterBody2D player;

	private float jumpTimer = 0;
	private float jumpTimerDefault = 1;

	private bool wasOnFloor = false;
	private float randomDirection = 0;

	public override void _Ready() {
		_pathFinding = GetNode<PathFindingComponent>("PathFindingComponent");
		player = _pathFinding.GetPlayer();
		foreach (var child in GetParent().GetChildren()) {
			if (child is MagneticCharacterComponent) {
				magCharComp = (MagneticCharacterComponent) child;
			}
		}
	}

	public override void _PhysicsProcess(double delta) {
		Vector2 velocity = Velocity;
		
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

		Vector2 direction = Vector2.Zero;

		// Add the gravity.
		if (!IsOnFloor())
			velocity.Y += gravity * (float)delta;

		Random random = new();
		if (IsOnFloor()) {
			
			randomDirection = random.NextSingle() - 0.5f;
			if (wasOnFloor != IsOnFloor()) {
				jumpTimer = jumpTimerDefault + random.NextSingle();
			}

			if (jumpTimer <= 0) {
				velocity.Y = jumpVelocity * (1 + random.NextSingle());
			} else {
				jumpTimer -= (float)delta;
			}
		}

		if (affected) {
			// Handle behaviour when affected by a magnet
			
		} else {
			// Handle behaviour when unaffected by a magnet
			
		}

		if (IsInGroup("CanSeePlayer") || IsInGroup("LookingForPlayer")) {
			if (!IsOnFloor()) {
				direction = GlobalPosition.DirectionTo(_pathFinding.GetLastDetectionPoint());
			}
		} else {
			if (!IsOnFloor()) {
				direction = new Vector2(randomDirection >= 0 ? 1 : -1, 0);
			}
		}

		velocity.X = _pathFinding.MoveCharacter(true, velocity, direction, maxSpeed, friction, acceleration, airAcceleration, delta).X;

		Velocity = velocity;
		wasOnFloor = IsOnFloor();
		MoveAndSlide();
	}
}

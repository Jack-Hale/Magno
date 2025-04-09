using Godot;
using System;

public partial class Guy : CharacterBody2D
{	
public const float speed = 300.0f;
	public float maxSpeed = 300.0f;
	public float jumpVelocity = -800.0f;
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
	private ProjectileLauncher _projectileLauncher;
	private ProjectileComponent _projectileComponent;
	public override void _Ready() {
		foreach (var child in GetParent().GetChildren()) {
			if (child is MagneticCharacterComponent) {
				magCharComp = (MagneticCharacterComponent) child;
			}
		}
		_pathFinding = GetNode<PathFindingComponent>("PathFindingComponent");

		_projectileLauncher = (ProjectileLauncher) magCharComp.GetPhysicsItems()[0];
		_projectileComponent = _projectileLauncher.GetProjectileComponent();
	}

	public override void _PhysicsProcess(double delta) {
		Vector2 velocity = Velocity;
		Vector2 direction = Vector2.Zero;
		
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
		direction = GlobalPosition.DirectionTo(_pathFinding.GetLastDetectionPoint());

		velocity.X = _pathFinding.MoveCharacter(true, velocity, direction, maxSpeed, acceleration, airAcceleration, delta).X;
		if (direction == Vector2.Zero) {
			velocity.X = _pathFinding.ApplyFriction(true, velocity, friction, delta).X;
		}

		if (IsInGroup("LookingForPlayer") || IsInGroup("CanSeePlayer")) {
			_projectileLauncher.LookAt(_pathFinding.GetLastDetectionPoint());
			
			_projectileComponent.Shoot();
		}

		// Add the gravity.
		if (!IsOnFloor())
			velocity.Y += gravity * (float)delta;

		if (affected) {
			// Handle behaviour when affected by a magnet
			
		} else {
			// Handle behaviour when unaffected by a magnet
			
		}

		Velocity = velocity;
		MoveAndSlide();
	}
}

using Godot;
using System;

public partial class Wasp : CharacterBody2D {
	public float maxSpeed = 100.0f;
	public float acceleration = 60.0f;
	float friction = 2200;
	float airAcceleration = 1800;

	public float jumpVelocity = -400.0f;

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

	private Sprite2D _sprite;
	private Sprite2D _gunSprite;

	private PathFindingComponent _pathFinding;
	private ProjectileComponent _projectileComponent;
	private Node2D _gun;
	private Vector2 projectilePosition;
	private float gunRotation;
	private Vector2 gunPosition;
	private ProjectileLauncher _projectileLauncher;
	public override void _Ready() {
		foreach (var child in GetParent().GetChildren()) {
			if (child is MagneticCharacterComponent) {
				magCharComp = (MagneticCharacterComponent) child;
			}
		}
		_pathFinding = GetNode<PathFindingComponent>("PathFindingComponent");
		_sprite = GetNode<Sprite2D>("Sprite2D");

		_projectileLauncher = GetNode<ProjectileLauncher>("ProjectileLauncher");
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


		if (affected) { // Handle behaviour when affected by a magnet
			
		} else { // Handle behaviour when unaffected by a magnet


			if (IsInGroup("CanSeePlayer") || IsInGroup("LookingForPlayer")) {
				direction = GlobalPosition.DirectionTo(_pathFinding.GetLastDetectionPoint() + Vector2.Up*200);
				
				_projectileComponent.Shoot();

				_projectileLauncher.LookAt(_pathFinding.GetLastDetectionPoint());

				_projectileLauncher.SetFlipH(Mathf.Sign(direction.X) < 0);
			}

			velocity = _pathFinding.MoveCharacter(false, velocity, direction, maxSpeed, acceleration, airAcceleration, delta);
			if (direction == Vector2.Zero) {
				velocity = _pathFinding.ApplyFriction(false, velocity, friction, delta);
			}

			velocity = _pathFinding.AvoidWallsAir(velocity, 60, 30, 40);
			_sprite.FlipH = _projectileLauncher.GetFlipH();
		}

		// bool flip = Mathf.Sign(direction.X) < 0;
		// _gunSprite.FlipH = flip;
		
		// _projectileComponent.SetHFlip(flip);



		// GD.Print(_projectileComponent.Position);

		Velocity = velocity;
		MoveAndSlide();
	}
}

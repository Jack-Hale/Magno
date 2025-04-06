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

	public override void _Ready() {
		foreach (var child in GetParent().GetChildren()) {
			if (child is MagneticCharacterComponent) {
				magCharComp = (MagneticCharacterComponent) child;
			}
		}
		_pathFinding = GetNode<PathFindingComponent>("PathFindingComponent");
		_sprite = GetNode<Sprite2D>("Sprite2D");
		_gun = GetNode<Node2D>("Gun");
		_gunSprite = _gun.GetNode<Sprite2D>("Sprite2D");
		_projectileComponent = _gun.GetNode<ProjectileComponent>("ProjectileComponent");
		projectilePosition = _projectileComponent.Position;
		gunRotation = _gun.Rotation;
		gunPosition = _gun.Position;
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
			bool flip = false;
			float angle = (_gun.Rotation % (2 * Mathf.Pi) + (2 * Mathf.Pi)) % (2 * Mathf.Pi);

			// Testing if the angle of the gun has it pointing on the left side of the character to flip its sprites
			if (angle >= (3 * Mathf.Pi / 2) || angle <= (Mathf.Pi / 2)) {
				_gunSprite.FlipV = false;
				_projectileComponent.Position = projectilePosition;
			} else {
				_gunSprite.FlipV = true;
				_projectileComponent.Position = new Vector2(projectilePosition.X, -projectilePosition.Y);
				flip = true;
			}

			if (IsInGroup("CanSeePlayer") || IsInGroup("LookingForPlayer")) {
				direction = GlobalPosition.DirectionTo(_pathFinding.GetLastDetectionPoint() + Vector2.Up*200);
				_gun.LookAt(_pathFinding.GetLastDetectionPoint());
				_gun.Rotate(flip ? -gunRotation : gunRotation);
				_projectileComponent.Shoot();
				_gun.Position = new Vector2(flip ? -gunPosition.X : gunPosition.X, gunPosition.Y); 
				_sprite.FlipH = flip;
				_projectileComponent.SetHFlip(flip);
			} else {
				_gun.Rotation = _sprite.FlipH ? Mathf.Pi : 0 + gunRotation;
			}

			velocity = _pathFinding.MoveCharacter(false, velocity, direction, maxSpeed, acceleration, airAcceleration, delta);
			if (direction == Vector2.Zero) {
				velocity = _pathFinding.ApplyFriction(false, velocity, friction, delta);
			}

			velocity = _pathFinding.AvoidWallsAir(velocity, 60, 30, 40);
			_sprite.FlipH = flip;
		}

		// bool flip = Mathf.Sign(direction.X) < 0;
		// _gunSprite.FlipH = flip;
		
		// _projectileComponent.SetHFlip(flip);



		// GD.Print(_projectileComponent.Position);

		Velocity = velocity;
		MoveAndSlide();
	}
}

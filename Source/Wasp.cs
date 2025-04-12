using Godot;
using Godot.Collections;
using System;
using System.Linq;

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
	private Sprite2D _wingsSprite;
	private Sprite2D _gunSprite;

	private PathFindingComponent _pathFinding;
	private ProjectileComponent _projectileComponent;
	private RigidBody2D _gun;
	private Vector2 projectilePosition;
	private float gunRotation;
	private Vector2 gunPosition;
	private Vector2 wingsPosition;
	private ProjectileLauncher _projectileLauncher;
	private AnimationPlayer _animationPlayer;
	private RigidBody2D _wings;
	bool hasWings = true;
	bool hasGun = true;
	bool collectedMagnetSprites = false;
	bool collectedMagnetPlayers = false;
	public override void _Ready() {
		foreach (var child in GetParent().GetChildren()) {
			if (child is MagneticCharacterComponent) {
				magCharComp = (MagneticCharacterComponent) child;
			}
		}
		_pathFinding = GetNode<PathFindingComponent>("PathFindingComponent");
		_sprite = GetNode<Sprite2D>("Sprite2D");
		_wings = GetNode<RigidBody2D>("Wings");
		_wingsSprite = _wings.GetNode<Sprite2D>("Sprite2D");
		_gun = GetNode<RigidBody2D>("Gun");

		_projectileLauncher = (ProjectileLauncher) magCharComp.GetPhysicsItems().Keys.First();
		_projectileComponent = _projectileLauncher.GetProjectileComponent();

		_animationPlayer = _wings.GetNode<AnimationPlayer>("AnimationPlayer");

		wingsPosition = _wingsSprite.Position;
	}

	public override void _Process(double delta) {
		
		// Waiting for duplicate sprites to exist to extract
		if (!collectedMagnetSprites && !collectedMagnetPlayers) {
			Dictionary<AnimationPlayer, AnimationPlayer> players = magCharComp.GetDuplicatePlayers();
			Dictionary<Sprite2D, Sprite2D> sprites = magCharComp.GetDuplicateSprites();

			// If none exist, arrays will be null
			collectedMagnetSprites = sprites == null;
			collectedMagnetPlayers = players == null;

			if (players != null && players.Keys.Count > 0) {
				foreach (var item in players.Keys) {
					if (_animationPlayer == players[item]) {
						_animationPlayer = item;
						collectedMagnetPlayers = true;
					}
				}
			}

			if (sprites != null && sprites.Keys.Count > 0) {
				foreach (var item in sprites.Keys) {
					if (_wingsSprite == sprites[item]) {
						_wingsSprite = item;
						collectedMagnetSprites = true;
					}
				}
			}
		}

		if (magCharComp != null) {
			if(magCharComp.Dettach()) {
				DettachProjectile();
			}
		}
	}

	public override void _PhysicsProcess(double delta) {
		Vector2 velocity = Velocity;
		Vector2 direction = Vector2.Zero;

		if (hasWings) {
			if (GetNodeOrNull<RigidBody2D>("Wings") == null) {
				_wings = null;
				_wingsSprite = null;
				hasWings = false;
			}
		}

		if (hasGun) {
			if (GetNodeOrNull<RigidBody2D>("Gun") == null) {
				_gun = null;
				_projectileLauncher = null;
				hasGun = false;
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
		if (!IsOnFloor() && !hasWings) {
			velocity += GetGravity() * (float)delta;
		}


		if (affected) { // Handle behaviour when affected by a magnet
		} else { // Handle behaviour when unaffected by a magnet
			if (IsInGroup("CanSeePlayer") || IsInGroup("LookingForPlayer")) {
				direction = GlobalPosition.DirectionTo(_pathFinding.GetLastDetectionPoint() + Vector2.Up*200);
				if (_projectileLauncher != null) {
					_projectileComponent.Shoot();

					_projectileLauncher.LookAt(_pathFinding.GetLastDetectionPoint());

				}
			}

			if (hasWings) {
				velocity = _pathFinding.MoveCharacter(!hasWings, velocity, direction, maxSpeed, acceleration, airAcceleration, delta);
				velocity = _pathFinding.AvoidWallsAir(velocity, 60, 30, 40);
			}
			_sprite.FlipH = direction.X < 0;
			if (hasWings) _wingsSprite.FlipH = _sprite.FlipH;

			if (direction == Vector2.Zero) {
				velocity = _pathFinding.ApplyFriction(!hasWings, velocity, friction, delta);
			}
			

			if (_projectileLauncher != null) {
				_projectileLauncher.SetFlipH(_sprite.FlipH);
			}

			if (hasWings) _wingsSprite.Position = new Vector2(_wingsSprite.FlipH ? -wingsPosition.X : wingsPosition.X, wingsPosition.Y);
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	private void DettachProjectile() {
		_projectileLauncher = null;
	}
}

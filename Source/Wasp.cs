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
	private Sprite2D _wingsSpriteOriginal;
	private Sprite2D _wingsSprite;
	private PathFindingComponent _pathFinding;
	private ProjectileComponent _projectileComponent;
	private RigidBody2D _gun;
	private Vector2 projectilePosition;
	private float gunRotation;
	private Vector2 gunPosition;
	private Vector2 wingsPosition;
	private float wingsRotation;
	private ProjectileLauncher _projectileLauncher;
	private AnimationPlayer _animationPlayer;
	private RigidBody2D _wings;
	private bool hasWings = true;
	private bool hasGun = true;
	private bool collectedMagnetObjects = false;
	private bool collectedMagnetPlayers = false;
	private CollisionShape2D _collision;
	private float collisionRotation;
	private Vector2 collisionPosition;
	private Area2D gunCopy = null;
	private Area2D wingsCopy = null;
	private CollisionShape2D wingsShape = new();
	private Vector2 wingsShapeScale = Vector2.One;
	private Vector2 wingsShapePosition = Vector2.One;
	private float wingsShapeRotation = 0;
	private CollisionShape2D gunShape = new();
	private float gunShapeRotation = 0;
	private Vector2 gunShapePosition = Vector2.One;

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
		_wingsSpriteOriginal = _wingsSprite;
		_gun = GetNode<RigidBody2D>("Gun");
		_collision = GetNode<CollisionShape2D>("CollisionShape2D");
		collisionRotation = _collision.Rotation;
		collisionPosition = _collision.Position;

		_projectileLauncher = (ProjectileLauncher) magCharComp.GeneratePhysicsItems().Keys.First();
		_projectileComponent = _projectileLauncher.GetProjectileComponent();

		wingsPosition = _wingsSprite.Position;
		wingsRotation = _wingsSprite.Rotation;
	}

	public override void _Process(double delta) {
		
		// Waiting for duplicate sprites to exist to extract
		if (!collectedMagnetObjects) {
			Dictionary<Area2D, NodePath> objects = magCharComp.GetDuplicateObjects();

			// If none exist, arrays will be null
			collectedMagnetObjects = objects == null;

			if (objects != null && objects.Count > 0) {
				foreach (var item in objects.Keys) {
					if (objects[item] == magCharComp.GetParent().GetPathTo(_wings)) {
						wingsCopy = item;
					}

					if (objects[item] == magCharComp.GetParent().GetPathTo(_gun)) {
						gunCopy = item;
						gunCopy.Position = _gun.Position;
					}
				}
			}

			if (gunCopy != null) {
				foreach (var item in gunCopy.GetChildren()) {
					if (item is CollisionShape2D shape) {
						gunShape = shape;
						gunShapeRotation = gunShape.Rotation;
						gunShapePosition = gunShape.Position;
					}
				}
			}

			if (wingsCopy != null) {
				foreach (var item in wingsCopy.GetChildren()) {
					if (item is AnimationPlayer player) {
						_animationPlayer = player;
					}
					if (item is Sprite2D sprite) {
						_wingsSprite = sprite;
					}
					if (item is CollisionShape2D shape) {
						wingsShape = shape;
						wingsShapeScale = wingsShape.Scale;
						wingsShapeRotation = wingsShape.Rotation;
						wingsShapePosition = wingsShape.Position;
					}
				}
				collectedMagnetObjects = true;
			}
		}

		if (magCharComp != null) {
			if (magCharComp.Detach()) {
				_projectileLauncher = null;
			}
		}
	}

	public override void _PhysicsProcess(double delta) {
		if (collectedMagnetObjects) {
			Vector2 velocity = Velocity;
			Vector2 direction = Vector2.Zero;

			if (hasWings) {
				if (GetNodeOrNull<RigidBody2D>("Wings") == null) {
					_wings = null;
					wingsCopy = null;
					wingsShape = null;
					_wingsSpriteOriginal.FlipH = false;
					_wingsSpriteOriginal.Position = wingsPosition;
					_wingsSpriteOriginal.Rotation = wingsRotation;
					_wingsSprite = null;
					_wingsSpriteOriginal = null;
					hasWings = false;

					Rotation = Mathf.DegToRad(56);
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

			}

			if (IsInGroup("CanSeePlayer") || IsInGroup("LookingForPlayer")) {
				if (hasWings) {
					direction = GlobalPosition.DirectionTo(_pathFinding.GetLastDetectionPoint() + Vector2.Up*200);
				} else {
					direction = -GlobalPosition.DirectionTo(_pathFinding.GetLastDetectionPoint());
				}
				if (_projectileLauncher != null) {

					_projectileComponent.Shoot();
					float angle = _pathFinding.GetAngle(GlobalPosition, _pathFinding.GetLastDetectionPoint());

					if (angle < Mathf.Pi && angle > 0) {
						_projectileLauncher.LookAt(_pathFinding.GetLastDetectionPoint());
					} else {
						_projectileLauncher.Rotation = angle > 3*MathF.PI/2 ? 0 : Mathf.Pi;
					}
					gunCopy.Position = _projectileLauncher.Position;

					gunCopy.Rotation = _projectileLauncher.Rotation;

				}
			} else {
				float distanceFromGround = _pathFinding.GetDistanceFromCollsionLayer(1000, Vector2.Down, (1u << 0) | (1u << 7));
				if (distanceFromGround < 200) {
					direction = Vector2.Up;
				}
			}

			if (hasWings) {
				velocity = _pathFinding.MoveCharacter(false, velocity, direction, maxSpeed, acceleration, airAcceleration, delta);
				velocity = _pathFinding.AvoidWallsAir(velocity, 60, 30, 40);
			} else {
				velocity = _pathFinding.MoveCharacter(true, velocity, direction, maxSpeed, acceleration, airAcceleration, delta);
			}
			bool flip = direction.X < 0;

			_sprite.FlipH = flip;
			_collision.Rotation = flip ? -collisionRotation : collisionRotation;
			_collision.Position = flip ? new Vector2(-collisionPosition.X, collisionPosition.Y) : collisionPosition;
			
			if (hasWings) _wingsSprite.FlipH = _sprite.FlipH;

			if (direction == Vector2.Zero) {
				velocity = _pathFinding.ApplyFriction(!hasWings, velocity, friction, delta);
			}
			
			if (_projectileLauncher != null) {
				_projectileLauncher.SetFlipH(_sprite.FlipH);
			}

			if (hasWings) {
				wingsCopy.Position = new Vector2(_wingsSprite.FlipH ? -wingsPosition.X : wingsPosition.X, wingsPosition.Y);
				wingsShape.Rotation = wingsShapeRotation * (_wingsSprite.FlipH ? -1 : 1);
				wingsShape.Position = new Vector2(_wingsSprite.FlipH ? -wingsShapePosition.X : wingsShapePosition.X, wingsShapePosition.Y);
			} else {
				Rotation = Mathf.DegToRad(56 * Mathf.Sign(direction.X));
			}

			Velocity = velocity;
			MoveAndSlide();
		}
	}
}

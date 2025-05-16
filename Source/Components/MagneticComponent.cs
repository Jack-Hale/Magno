using Godot;
using Godot.Collections;
using System;

public enum ExitCondition {
	CannotExit,
	TimeLimit,
	StrongForce,
	Throw,
	HitSurfaceAfterThrow
}

public partial class MagneticComponent : Node2D {
	[Export]
	private float weakMultiplier = 7;	
	[Export]
	private float strongMultiplier = 10;
	[Export]
	private float blastMultiplier = 800;
	[Export]
	private bool canJoin = true;
	[Export]
	private ExitCondition exitCondition;
	[Export]
	private float exitTimer = 2;
	[Export]
	private bool impartCollisionOnParent = false;
	[Export]
	private float reconnectCooldownDefault = 2;
	private float reconnectCooldown = 0;
	private RigidBody2D rigidObject;
	private CharacterBody2D characterObject;
	private Magnet magnetParent;
	private MagneticCharacterComponent magCharComp;
	private bool exitTriggered = false;

	private Vector2 draw1 = Vector2.Zero;
	private Vector2 draw2 = Vector2.Zero;
	private bool inTimeLimitExit = false;
	private bool isRigidPhysics;
	private float exitTimerDefault;
	private Node rigidObjectParent;
	private RigidBody2D objectCollisionL = new RigidBody2D();
	private RigidBody2D objectCollisionM = new RigidBody2D();
	private uint collisionL = 0;
	private uint collisionM = 0;
	private Tuple<bool, Vector2> magnetData = new Tuple<bool, Vector2>(false, Vector2.Inf);
	private Tuple<Vector2, Vector2> forceData = new Tuple<Vector2, Vector2>(Vector2.Zero, Vector2.Zero);
	private Tuple<bool, bool> strengthData = new Tuple<bool, bool>(false, false);
	private bool secondaryObject = false;
	private Sprite2D rigidSprite;
	private Sprite2D rigidSpriteDupe;
	private Vector2 rigidSpriteDupePosition;
	private Vector2 rigidSpriteResetPosition;
	private bool collectedDupeSprite = false;
	private AnimationPlayer rigidPlayer;
	private CollisionShape2D rigidCollision;
	private bool disableMagneticism = false;
	private bool waitForHit = false;
	private bool magnetCharacter = false;
	private bool isPhysicsItem = false;
	private Node2D physicsItem = null;
	private float shakeStrength = 0;
	private float shakeAmount = 1;
	private float shakeFade = -5;
	private Area2D objectDuplicate = null;
	public MagneticComponent() {
		Name = "MagneticComponent";
		AddToGroup("MagneticComponent");
	}

	public MagneticComponent(MagneticCharacterComponent magneticCharacterComponent, float weakMultiplier, float strongMultiplier, float blastMultiplier, bool canJoin, ExitCondition exitCondition, float exitTimer) {
		magCharComp = magneticCharacterComponent;
		Name = "MagneticComponent";
		AddToGroup("MagneticComponent");
		isRigidPhysics = magCharComp.GetIsRigidPhysics();

		this.weakMultiplier = weakMultiplier;
		this.strongMultiplier = strongMultiplier;
		this.blastMultiplier = blastMultiplier;
		this.canJoin = canJoin;
		this.exitCondition = exitCondition;
		this.exitTimer = exitTimer;
	}
	
	public MagneticComponent(MagneticCharacterComponent magneticCharacterComponent, float weakMultiplier, float strongMultiplier, float blastMultiplier, bool canJoin) {
		magCharComp = magneticCharacterComponent;
		Name = "MagneticComponent";
		AddToGroup("MagneticComponent");
		isRigidPhysics = magCharComp.GetIsRigidPhysics();

		this.weakMultiplier = weakMultiplier;
		this.strongMultiplier = strongMultiplier;
		this.blastMultiplier = blastMultiplier;
		this.canJoin = canJoin;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {

		Node parent = GetParent();

		bool playerParent = false;

		Array<Node> array = GetTree().Root.GetChildren();
		for (int i = 0; i < array.Count; i++) {
			Node node = array[i].GetNodeOrNull<CharacterBody2D>("Player");

			if (parent.GetParent() == node) {
				playerParent = true;
			}
		}
		if (!playerParent) {
			if (parent is RigidBody2D rb) {
				rigidObject = rb;
				Node objectParent = rigidObject.GetParent();
				
				// Storing a copy of the sprite and animation player being copied onto character
				Array<Node> children = rigidObject.GetChildren();
				for (int i = 0; i < children.Count; i++) {
					if (children[i] is Sprite2D sprite) {
						rigidSprite = sprite;
					}

					if (children[i] is AnimationPlayer player) {
						rigidPlayer = player;
					}

					if (children[i] is CollisionShape2D collision) {
						rigidCollision = collision;
					}

					if (children[i].IsInGroup("HasPhysics")) {
						isPhysicsItem = true;
						physicsItem = (Node2D) children[i];
						Array<Node> physChildren = children[i].GetChildren();
						for (int j = 0; j < physChildren.Count; j++) {
							if (physChildren[j] is Sprite2D physSprite) {
								rigidSprite = physSprite;
							}
						}
					}
				}

				if (rigidSprite == null) {
					GD.PrintErr($"{rigidObject} {rigidObject.Name}'s MagnetComponent needs to be below any physics objects in tree");
					GD.PushError($"{rigidObject} {rigidObject.Name}'s MagnetComponent needs to be below any physics objects in tree");
				}

				rigidObject.AddToGroup("Magnetic");
				
				InitialiseForCharacterOwner(objectParent, null);

				if (parent != null && parent is RigidBody2D rigid) {
					rigid.ContinuousCd = RigidBody2D.CcdMode.CastShape;
				}

			// Character doesnt contain a magnetic object and is magnetic itself
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

				characterObject = objectParent;
				characterObject.AddToGroup("Magnetic");
			} else {
				GD.PrintErr($"parent of {Name}:{this} ({parent.Name} {parent}) is not RigidBody2D");
				GD.PushError($"parent of {Name}:{this} ({parent.Name} {parent}) is not RigidBody2D");
			}
		}
	}

	public override void _Draw() {
        // DrawLine(ToLocal(draw1), ToLocal(draw2), Colors.Red, 4.0f);
	}

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _PhysicsProcess(double delta) {

		if (reconnectCooldown > 0) {
			reconnectCooldown -= (float) delta;
		}

		if (rigidObject.IsInGroup("ReconnectionCooldown")) {
			rigidObject.RemoveFromGroup("ReconnectionCooldown");
		}

		// Waiting for duplicate sprite to exist to extract
		if (!collectedDupeSprite && magnetCharacter) {
			if (isPhysicsItem) {
				Dictionary<Node2D, Node2D> items = magCharComp.GetPhysicsItems();

				if (items != null && items.Keys.Count > 0) {
					foreach (var item in items.Keys) {
						if (items[item] == physicsItem) {
							Array<Node> children = item.GetChildren();
							for (int i = 0; i < children.Count; i++) {
								if (children[i] is Sprite2D sprite) {
									rigidSpriteDupe = sprite;
									// rigidSpriteDupePosition = rigidSpriteDupe.Position;
									collectedDupeSprite = true;
								}
							}
						}
					}
				} 
			} else {
				Dictionary<Area2D, NodePath> objects = magCharComp.GetDuplicateObjects();
				
				foreach (var item in objects.Keys) {
					if (objects[item] == magCharComp.GetParent().GetPathTo(rigidObject)) {
						Array<Node> children = item.GetChildren();
						for (int i = 0; i < children.Count; i++) {
							if (children[i] is Sprite2D sprite) {
								collectedDupeSprite = true;
								rigidSpriteDupe = sprite;
								rigidSpriteResetPosition = rigidSpriteDupe.Position;
							}
						}
					}
				}
			}			
		}

		if (characterObject != null && rigidObject != null) {
			switch (exitCondition) {
				case ExitCondition.Throw:
					if (magCharComp.GetBodyCopyStrengthData().Item1) {
						EnableRigidObject(true);
					}
					break;
				case ExitCondition.HitSurfaceAfterThrow:
					if (magCharComp.GetBodyCopyStrengthData().Item1) {
						waitForHit = true;
					}
					break;
				case ExitCondition.StrongForce:
					if (magCharComp.GetBodyCopyStrengthData().Item2) {
						EnableRigidObject(true);
					}
					break;
			}

			// GD.Print(exitTriggered, inTimeLimitExit);
			if (exitTriggered || inTimeLimitExit) {
				
				switch (exitCondition) {
					case ExitCondition.CannotExit:
						break;
					case ExitCondition.TimeLimit:
						if (!inTimeLimitExit && characterObject.IsInGroup("Affected")) {
							exitTimer = exitTimerDefault;
							shakeStrength = shakeAmount;
							rigidSpriteDupePosition = rigidSpriteDupe.Position;
							inTimeLimitExit = true;
						}

						if (inTimeLimitExit) {
							if (exitTimer > 0) {
								exitTimer -= (float) GetProcessDeltaTime();
								shakeFade = -exitTimer;

								if (rigidSpriteDupe != null) {
									// If sprite is out of range, reset and recollect sprite position.
									if (ShakeSprite(rigidSpriteDupe, rigidSpriteDupePosition)) {
										shakeStrength = 0;
									} else {
										if (shakeStrength == 0) {
											rigidSpriteDupePosition = rigidSpriteDupe.Position;
											shakeStrength = shakeAmount;
										}
									}
								}

							} else {
								exitTimer = exitTimerDefault;
								
								EnableRigidObject(true);
								inTimeLimitExit = false;
							}

							if (inTimeLimitExit && !exitTriggered) {
								inTimeLimitExit = false;
							}

							if (!inTimeLimitExit) {
								if (rigidSpriteDupe != null) {
									if (rigidSpriteDupe.Position != rigidSpriteResetPosition) {
										rigidSpriteDupe.Position = rigidSpriteResetPosition;
									}
								}
								shakeStrength = 0;
							}
						}

						break;
				}
			}

			if (magCharComp != null) {
				if (magCharComp.GetHitDetected()) {
					if (waitForHit) {
						EnableRigidObject(false);
					} else {
						magCharComp.ResetHitDetected();
					}
				}
			}
		}

		canJoin = !disableMagneticism;

		QueueRedraw();
	}

	/// <summary>
	/// Causes a shaking effect on a sprite.
	/// </summary>
	/// <returns><para>If the sprite is outside of the range it should be, 
	/// meaning the sprite's position has been updated outside of this function.</para></returns>
	public bool ShakeSprite(Sprite2D sprite, Vector2 originalSpritePosition) {
		float delta = (float) GetProcessDeltaTime();
		RandomNumberGenerator rand = new();
		float shakeMax = 5;
		if (shakeStrength > shakeMax) {
			shakeStrength = shakeMax;
		}
		bool findPosition = false;
		if (shakeStrength > 0) {
			shakeStrength = Mathf.Lerp(shakeStrength, 0, shakeFade * delta);
			Vector2 newPosition = originalSpritePosition + new Vector2(
				rand.RandfRange(-shakeStrength, shakeStrength), rand.RandfRange(-shakeStrength, shakeStrength)
			);

			// Checks if the sprite's position is outside of the range
			// Range is defined as shakeStrength plus squareroot2 to account for extra length from diagonal movement
			findPosition = sprite.Position.DistanceTo(newPosition) > Mathf.Abs(shakeStrength) + Mathf.Sqrt2 + 1;
			sprite.Position = newPosition;
		}

		return findPosition;
	}

	public void TriggerExitCase() {
		exitTriggered = true;
	}

	public void StopExitCase() {
		exitTriggered = false;
	}

	/// <summary>
	/// Destroys connection between Character and Rigid objects and removes any ability for Character to be magnetic.
	/// </summary>
	public void EnableRigidObject(bool magnetCause) {
		if (characterObject != null) {
			magCharComp.SwapToCharacter();
			magCharComp.DetachMetalObject(rigidObject);

			Node parent = rigidObject.GetParent();
			parent.RemoveChild(rigidObject);
			
			rigidObjectParent.AddChild(rigidObject);

			rigidObject.CollisionLayer = collisionL;
			rigidObject.CollisionMask = collisionM;

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

			// Ensures the rigidObject spawns in the direction of the magnet force when exiting character
			Vector2 characterPosition = magCharComp.isCharacter ? characterObject.GlobalPosition : magCharComp.GetBodyCopy().GlobalPosition;

			var direction = Vector2.Up;
			bool magnetMode = true;

			if (magnetCause) {
				direction = (magCharComp.GetBodyCopyMagnetData().Item2 - characterPosition).Normalized();
				magnetMode = magCharComp.GetBodyCopyMagnetData().Item1;
			}

			Vector2 spawnLocation = characterPosition + ((magnetMode ? 1 : -1) * (direction * ((float)characterSize)));

			rigidObject.GlobalPosition = spawnLocation;
			rigidObject.LinearVelocity = Vector2.Zero;
			
			characterObject = null;
			isRigidPhysics = true;

			secondaryObject = false;
			collectedDupeSprite = false;
			magnetCharacter = false;

			rigidObject.AddToGroup("ReconnectionCooldown");

			magCharComp.TryRemoveMagnetism();
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

	public Magnet GetMagnet() {
		Node parentCheck = GetParent();

		// Iterate through parents until Magnet or Root is found
		while (parentCheck is not Magnet && parentCheck != GetTree().Root) {
			parentCheck = parentCheck.GetParent();
		}
		if (parentCheck is Magnet magnet) {
			return magnet;
		}
		return null;
	}

	// Applies the Magnetic force onto the parent object
	public void ForceObject(Vector2 collisionPoint, Vector2 attractionPoint, float beamLength, bool pull, bool strongMagnet, bool blast, double delta, bool isRigidPhysics) {
		if (!secondaryObject && !disableMagneticism) {
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
			
			if (!isRigidPhysics) {
				if (rigidObject != null) {
					rigidObject.ApplyForce(force, position);
				}
			}
		}
	}

	public void DetachFromMagnet() {
		Magnet magnet = GetMagnet();
		if (magnet != null) {
			magnet.Detach();
		}
	}

	/// <summary>
	/// Gets the data representing the parameters of a magnet affecting parent.
	/// <para>bool Item1 = pull mode of magnet (true = pull, false = push)</para>
	/// <para>Vector2 Item2 = position the magnet is affecting parent</para>
	/// </summary>
	/// <returns>Tuple containing both bool and vector.</returns>
	public Tuple<bool, Vector2> GetMagnetData() {
		return magnetData;
	}

	/// <summary>
	/// Gets the data representing the force of a magnet affecting parent.
	/// <para>Vector2 Item1 = force vector</para>
	/// <para>Vector2 Item2 = position force is being applied</para>
	/// </summary>
	/// <returns>Tuple containing both vectors.</returns>
	public Tuple<Vector2, Vector2> GetForceData() {
		return forceData;
	}

	/// <summary>
	/// Gets the data representing the strength of a magnet affecting parent.
	/// <para>bool Item1 = if blast</para>
	/// <para>bool Item2 = if strong magnet</para>
	/// </summary>
	/// <returns>Tuple containing both booleans.</returns>
	public Tuple<bool, bool> GetStrengthData() {
		return strengthData;
	}

	public void InitialiseForCharacterOwner(Node objectParent, Node sceneParent) {
		if (objectParent is PhysicsBody2D op && objectParent is not StaticBody2D) {
			objectParent = op;

			if (objectParent is CharacterBody2D) {
				foreach (var child in objectParent.GetParent().GetChildren()) {
					if (child is MagneticCharacterComponent) {
						magCharComp = (MagneticCharacterComponent)child;
						magnetCharacter = true;
						break;
					}
				}
			}

			if (magnetCharacter) {
				// Disabling the rigid object while it is within the larger object
				collisionL = rigidObject.CollisionLayer;
				collisionM = rigidObject.CollisionMask;

				rigidObject.CollisionLayer = 0;
				rigidObject.CollisionMask = 0;
				
				rigidObject.Visible = false;
				rigidObject.Sleeping = true;
			}
		}

		// Character contains a magnetic object that imparts its magneticism onto the character
		if (magnetCharacter) {
			if (magCharComp == null) {
				GD.PrintErr(objectParent, " requires MagneticCharacterComponent");
				GD.PushError(objectParent, " requires MagneticCharacterComponent");
			}

			exitTimerDefault = exitTimer;
			isRigidPhysics = magCharComp.GetIsRigidPhysics();

			if (sceneParent == null) {
				sceneParent = magCharComp.GetParent().GetParent();
			}

			rigidObjectParent = sceneParent;

			characterObject = (CharacterBody2D) objectParent;
			if (characterObject.IsInGroup("Magnetic")) {
				secondaryObject = true;
			}
			characterObject.AddToGroup("Magnetic");
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

	public bool GetIsRigidPhysics() {
		return isRigidPhysics;
	}
	public Sprite2D GetRigidSprite() {
		return rigidSprite;
	}
	public AnimationPlayer GetRigidPlayer() {
		return rigidPlayer;
	}

	public CollisionShape2D GetRigidCollision() {
		return rigidCollision;
	}

	public bool GetRagDollOnAnyForce() {
		if (magCharComp != null) {
			return magCharComp.GetRagDollOnAnyForce();
		}
		return false;
	}

	public void DisableMagneticism(bool disable) {
		disableMagneticism = disable;
	}

	public float GetWeakMultiplier() {
		return weakMultiplier;
	}
	public float GetStrongMultiplier() {
		return strongMultiplier;
	}
	public float GetBlastMultiplier() {
		return blastMultiplier;
	}
	public bool GetCanJoin() {
		return canJoin;
	}
	public ExitCondition GetExitCondition() {
		return exitCondition;
	}
	public float GetExitTimer() {
		return exitTimer;
	}
	public bool GetImpartCollisionOnParent() {
		return impartCollisionOnParent;
	}

	public void SetWeakMultiplier(float weakMultiplier) {
		this.weakMultiplier = weakMultiplier;
	}
	public void SetStrongMultiplier(float strongMultiplier) {
		this.strongMultiplier = strongMultiplier;
	}
	public void SetBlastMultiplier(float blastMultiplier) {
		this.blastMultiplier = blastMultiplier;
	}
	public void SetCanJoin(bool canJoin) {
		this.canJoin = canJoin;
	}
	public void SetExitCondition(ExitCondition exitCondition) {
		this.exitCondition = exitCondition;
	}
	public void SetExitTimer(float exitTimer) {
		this.exitTimer = exitTimer;
	}
}

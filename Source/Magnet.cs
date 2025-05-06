using Godot;
using Godot.Collections;
using System;

public partial class Magnet : Area2D
{
	[Export]
	private bool activated = false;
	[Export]
	private bool pullMode = true;
	[Export]
	private bool strongMagnet = false;
	[Export]
	private PhysicsBody2D parent;
	[Export]
	private bool dropItemOnDeactivate = true;

	private RigidBody2D parentRigid;
	private CharacterBody2D parentCharacter;
	private bool canJoin;
	private PhysicsBody2D EnteredBody;

	private CollisionShape2D collision = null;
	private Array<CollisionShape2D> heldObjectCollisions = new();
	private Array<float> heldObjectInitRotations = new();
	private Array<CollisionShape2D> objectCollisions = new();

	private Area2D _magnetBeam;

	CollisionPolygon2D _beamArea;
	private float beamLength;

	private RayCast2D _tileBeamCast;
	private RayCast2D _objectCheck;
	private RayCast2D _beamCheck1;
	private RayCast2D _beamCheck2;
	private RayCast2D _beamCheck3;
	private StaticBody2D _physicsObject;
	
	private PhysicsBody2D attachedObject;
	private MagneticComponent attachedObjectMagComp;
	private Dictionary<PhysicsBody2D, MagneticComponent> attractedObjects = new();
	private Dictionary<Area2D, MagneticComponent> affectedDuplicates = new();
	private Sprite2D _beamSpriteWeak;
	private Sprite2D _beamSpriteStrong;

	private bool isObjectAttached = false;

	private Marker2D _anchor;
	private Vector2 anchorPositionDefault;
	private float anchorOffset = 0;

	private Node objectParent;

	private bool blast;
	private bool isItem = false;

	private uint objectCollisionLayer = 0;
	
	private Vector2 draw1 = Vector2.Zero;
	private Vector2 draw2 = Vector2.Zero;
	private Vector2 draw3 = Vector2.Zero;
	private Vector2 draw4 = Vector2.Zero;
	private Vector2 draw5 = Vector2.Zero;
	private Vector2 draw6 = Vector2.Zero;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		_anchor = GetNode<Marker2D>("Anchor");
		_anchor.AddToGroup("MagnetAnchor");
		anchorPositionDefault = _anchor.Position;
		_magnetBeam = GetNode<Area2D>("MagnetBeam");
		_magnetBeam.AddToGroup("NoRagdollInclusion");
		_beamSpriteWeak = GetNode<Sprite2D>("BeamSpriteWeak");
		_beamSpriteWeak.AddToGroup("NoRagdollInclusion");
		_beamSpriteStrong = GetNode<Sprite2D>("BeamSpriteStrong");
		_beamSpriteStrong.AddToGroup("NoRagdollInclusion");
		_tileBeamCast = GetNode<RayCast2D>("TileBeamCast");
		_objectCheck = GetNode<RayCast2D>("ObjectCheck");
		_physicsObject = GetNode<StaticBody2D>("PhysicsObject");

		_beamCheck1 = _magnetBeam.GetNode<RayCast2D>("BeamCheck1");
		_beamCheck2 = _magnetBeam.GetNode<RayCast2D>("BeamCheck2");
		_beamCheck3 = _magnetBeam.GetNode<RayCast2D>("BeamCheck3");

		_magnetBeam.Connect("body_entered", new Callable(this, MethodName.OnBodyEnteredBeam));
		_magnetBeam.Connect("body_exited", new Callable(this, MethodName.OnBodyExitedBeam));

		_magnetBeam.Connect("area_entered", new Callable(this, MethodName.OnAreaEnteredBeam));
		_magnetBeam.Connect("area_exited", new Callable(this, MethodName.OnAreaExitedBeam));

		Connect("body_entered", new Callable(this, MethodName.OnBodyEntered));
		Connect("body_exited", new Callable(this, MethodName.OnBodyExited));

		if (parent != null) {
			if (parent is RigidBody2D rigidBody) parentRigid = rigidBody;
			if (parent is CharacterBody2D characterBody) parentCharacter = characterBody;
			_physicsObject.AddCollisionExceptionWith(parent);
		}

		collision = (CollisionShape2D)_physicsObject.GetNode<CollisionShape2D>("CollisionShape2D").Duplicate();
		collision.SetMeta("IgnoreCollision", true);
		collision.Name = "MagnetCollision";
		collision.AddToGroup("NoRagdollInclusion");
		parent.CallDeferred("add_child", collision);
		collision.Position = _physicsObject.Position;

		
		_beamArea = _magnetBeam.GetNode<CollisionPolygon2D>("BeamArea");
		
		float minX = float.MaxValue;
		float maxX = float.MinValue;
		
		foreach (Vector2 vector in _beamArea.Polygon) {
			if (vector.X > maxX) maxX = vector.X;
			if (vector.X < minX) minX = vector.X;
		}

		beamLength = maxX - minX;

		if (_magnetBeam == null) {
			GD.PrintErr("No Area2D found");
			GD.PushError("No Area2D found");
		}

		canJoin = pullMode;

		SetActivation(activated && !strongMagnet, activated && strongMagnet);
	}

	public override void _Draw()
	{
		// DrawLine(ToLocal(draw1), ToLocal(draw2), Colors.Green, 4.0f);
		// DrawLine(ToLocal(draw3), ToLocal(draw4), Colors.Blue, 4.0f);
		// DrawLine(ToLocal(draw5), ToLocal(draw6), Colors.Blue, 3.0f);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {
		// Removing velocity on first tick object is removed
		if (!isObjectAttached && attachedObject != null) {
			
			if (attachedObject is RigidBody2D rigidBody) {
				rigidBody.AngularVelocity = 0;
				rigidBody.LinearVelocity = parentCharacter != null ? parentCharacter.Velocity : parentRigid != null ? parentRigid.LinearVelocity : Vector2.Zero;
			}

			MagneticComponent magneticComponent = (MagneticComponent) attachedObject.GetNode("MagneticComponent");
			MagneticCharacterComponent magCharComp = magneticComponent.GetMagneticCharacterComponent();

			if (blast) {
				magneticComponent.ForceObject(attachedObject.GlobalPosition, GlobalPosition, beamLength, pullMode, false, true, delta, false);

				// Starts a ragdoll timer if the object is a character so it doesnt swap from rigid to char
				// over and over again while being blasted
				if (magCharComp != null) {
					magCharComp.StartRagDollTimer();
				}
				
				OnBodyExitedBeam(attachedObject);

				blast = false;
			}
			attachedObject = null;
		}

		if (_magnetBeam.Position != new Vector2(32, 0)) {
			_magnetBeam.Position = new Vector2(32, 0);
		}
	}

	public override void _PhysicsProcess(double delta) {
		
		collision.Rotation = Rotation;
		collision.GlobalPosition = _physicsObject.GlobalPosition;

		// Positioning and rotating the duplicate collisions of held objects
		if (heldObjectCollisions.Count > 0) {
			for (int i = 0; i < heldObjectCollisions.Count; i++) {
				heldObjectCollisions[i].Rotation = Rotation + heldObjectInitRotations[i];
				heldObjectCollisions[i].GlobalPosition = objectCollisions[i].GlobalPosition;
			}
		}

		if (attachedObject != null) {
			// Disabling beam sprite if object attached
			if (strongMagnet) {
				_beamSpriteStrong.Visible = false;
			} else {
				_beamSpriteWeak.Visible = false;
			}
			if (!activated || !canJoin) {
				if (dropItemOnDeactivate || !isItem) {
					Detach();
				}
			}
		}

		if (attachedObject == null) {
			// Reenabling beam sprite if no object attached
			if (strongMagnet) {
				_beamSpriteStrong.Visible = activated;
			} else {
				_beamSpriteWeak.Visible = activated;
			}
			// Checking if there are tiles in the beam
			if (_tileBeamCast.IsColliding()) {
				if (_tileBeamCast.GetCollider() is TileMapLayer tileMap) {
					Vector2 collisionPoint = _tileBeamCast.GetCollisionPoint();

					// Adjust the collision by 1 depending on whether the magnet is above or below the collision point
					collisionPoint.Y -= GlobalPosition.Y > collisionPoint.Y ? 1 : -1;
					collisionPoint.X -= GlobalPosition.X > collisionPoint.X ? 1 : -1;

					Vector2 localCollision = tileMap.ToLocal(collisionPoint);

					// Convert local position to map cell coordinates using floor division
					Vector2 tileSize = tileMap.TileSet.TileSize;
					int cellX = Mathf.FloorToInt(localCollision.X / tileSize.X);
					int cellY = Mathf.FloorToInt(localCollision.Y / tileSize.Y);
					Vector2I collisionCoords = new Vector2I(cellX, cellY);

					TileData data = tileMap.GetCellTileData(collisionCoords);

					// Moving magnet holder if terrain is magnetic
					if (data != null && (bool) data.GetCustomData("Magnetic")) {
						ForceObject(_tileBeamCast.GetCollisionPoint(), delta);
					}
				}
			}

			// Iterate through all attracted objects to process attraction physics
			if (activated) {
				foreach (PhysicsBody2D body in attractedObjects.Keys) {
					
					if (!attractedObjects.ContainsKey(body)) {
						break;
					}
					// Attach object that reaches the magnet
					MagneticComponent magComp = attractedObjects[body];
					
					if (EnteredBody == body && attachedObject != body && canJoin) {
						// Detaching object from any magnet that is already holding it
						if (magComp.IsBeingHeld()) {
							magComp.GetMagnetParent().Detach();
						}
						magComp.SetMagnetParent(this);
						AttachObject(body, magComp);
					}

					if (magComp.GetMagneticCharacterComponent() == null || magComp.GetIsRigidPhysics()) {
						// Ensures walls block magnet beam
						if (_beamCheck1.GetCollider() == body || _beamCheck2.GetCollider() == body || _beamCheck3.GetCollider() == body) {

							// Fire two raycasts along both edges of the magnet beam
							var spaceState = GetWorld2D().DirectSpaceState;

							// Get the outer edges of the beam as two vectors represented by start and end
							var start1 = ToGlobal(_beamArea.Polygon[1]);
							var end1 = ToGlobal(_beamArea.Polygon[2]);
							var start2 = ToGlobal(_beamArea.Polygon[0]);
							var end2 = ToGlobal(_beamArea.Polygon[3]);

							var query1 = PhysicsRayQueryParameters2D.Create(start1, end1, _magnetBeam.CollisionMask);
							var query2 = PhysicsRayQueryParameters2D.Create(start2, end2, _magnetBeam.CollisionMask);
						
							query1.Exclude = new Array<Rid>();
							query2.Exclude = new Array<Rid>();

							Dictionary finalResult1 = null;
							Dictionary finalResult2 = null;

							bool breakCheck1 = true;
							bool breakCheck2 = true;

							Array<Rid> exclusionArray1 = new Array<Rid>{};
							Array<Rid> exclusionArray2 = new Array<Rid>{};

							const int maxIterations = 40;
							int iterationCount = 0;

							while (breakCheck1 || breakCheck2) {
								
								if (breakCheck1) {
									// Add list of objects found that aren't the target to exclusion list
									query1.Exclude = exclusionArray1;

									// Generate new query result
									var result1 = spaceState.IntersectRay(query1);

									// Check if the result contains a valid collider
									if (result1.Count != 0 && breakCheck1) {
										Rid currentRid1 = (Rid)result1["rid"];

										// If the current collider is the target body, close off query track
										if (currentRid1 == body.GetRid())
										{
											finalResult1 = result1;
											breakCheck1 = false;
										}
										
										// Exclude the current collider from the next query
										exclusionArray1.Add(currentRid1);
									} else {
										// Target not found
										breakCheck1 = false;
									}
								}

								if (breakCheck2) {
									// Add list of objects found that aren't the target to exclusion list
									query2.Exclude = exclusionArray2;
									
									// Generate new query result
									var result2 = spaceState.IntersectRay(query2);

									// Check if the result contains a valid collider
									if (result2.Count != 0 && breakCheck2) {
										Rid currentRid2 = (Rid)result2["rid"];
										
										// If the current collider is the target body, close off query track
										if (currentRid2 == body.GetRid())
										{
											finalResult2 = result2;
											breakCheck2 = false;
										}
										
										// Exclude the current collider from the next query
										exclusionArray2.Add(currentRid2);
									} else {
										// Target not found
										breakCheck2 = false;
									}
								}
								// If maximum interations reached, exit the loop
								iterationCount++;
								if (iterationCount >= maxIterations) {
									break; 
								}
							}

							Vector2 collisionPoint = Vector2.Zero;

							// If only one query found target, get the position of that query
							if (finalResult1 != null && finalResult2 == null) {
								collisionPoint = (Vector2)finalResult1["position"];

							} else if (finalResult1 == null && finalResult2 != null) {
								collisionPoint = (Vector2)finalResult2["position"];
								
							// If both queries found target, get position between both points
							} else if (finalResult1 != null && finalResult2 != null) {
								Vector2 position1 = (Vector2)finalResult1["position"];
								Vector2 position2 = (Vector2)finalResult2["position"];
								collisionPoint = position1.Lerp(position2, 0.5f);
							}

							if (magComp.GetRagDollOnAnyForce()) {
								MagneticCharacterComponent magCharComp = magComp.GetMagneticCharacterComponent();
								
								if (magCharComp != null) {
									if (magCharComp.GetRagDollOnAnyForce() && !magCharComp.GetIsRagDoll()) {
										magCharComp.StartAnyForceRagDollTimer();
									}
								}
							}
							magComp.ForceObject(collisionPoint, GlobalPosition, beamLength, pullMode, strongMagnet, false, delta, false);
						}
					} else {
						// No forces applied if object is part of a large character, only magnet data is shared
						magComp.ForceObject(Vector2.Zero, GlobalPosition, beamLength, pullMode, strongMagnet, false, delta, true);
					}
				}
			}
		}

		// Failsafe for if object is not in beam but is still included in the attractedObjects Dict
		// TODO: Make this never actually occur 
		if (!_beamCheck1.IsColliding() && !_beamCheck2.IsColliding() && !_beamCheck3.IsColliding() && attractedObjects.Count > 0 && !isObjectAttached) {
			DetachAll();
		} 

		// Failsafe for if OnBodyEnteredBeam isnt triggered correctly. Helps the magnet push enemies more consistently.
		if (activated && (_beamCheck1.IsColliding() || _beamCheck2.IsColliding() || _beamCheck3.IsColliding())) {
			if (_beamCheck1.IsColliding() && _beamCheck1.GetCollider() is PhysicsBody2D body1) {
				if (body1.IsInGroup("Magnetic")) {
					if (!attractedObjects.ContainsKey(body1)) {
						OnBodyEnteredBeam(body1);
					}
				}
			} else if (_beamCheck2.IsColliding() && _beamCheck2.GetCollider() is PhysicsBody2D body2) {
				if (body2.IsInGroup("Magnetic")) {
					if (!attractedObjects.ContainsKey(body2)) {
						OnBodyEnteredBeam(body2);
					}
				}
			} else if (_beamCheck3.IsColliding() && _beamCheck3.GetCollider() is PhysicsBody2D body3) {
				if (body3.IsInGroup("Magnetic")) {
					if (!attractedObjects.ContainsKey(body3)) {
						OnBodyEnteredBeam(body3);
					}
				}
			}
		}


		if (!activated) {
			foreach (var item in affectedDuplicates.Keys) {
				affectedDuplicates[item].StopExitCase();
				affectedDuplicates.Remove(item);
			}
		}

		foreach (var item in affectedDuplicates.Keys) {
			affectedDuplicates[item].TriggerExitCase();
		}

		QueueRedraw();
	}

	private void OnAreaEnteredBeam(Area2D area) {
		if (area.IsInGroup("DuplicateMagnetChild")) {
			string[] namePath = area.Name.ToString().Split('-')[2].Split('_');
			string path = "";
			for (int i = 0; i < namePath.Length; i++) {
				path += namePath[i];
				if (i != namePath.Length - 1) {
					path += "/";
				}
			} 
			MagneticComponent magComp = area.GetParent().GetNode<MagneticComponent>(path);
			
			affectedDuplicates.Add(area, magComp);
		}
	}

	private void OnAreaExitedBeam(Area2D area) {
		if (affectedDuplicates.ContainsKey(area)) {
			affectedDuplicates[area].StopExitCase();
			affectedDuplicates.Remove(area);
		}
	}

	// Called when object touches the magnet beam
	// Adds object to dict of attracted objects
	private void OnBodyEnteredBeam(Node body) {
		if (!isObjectAttached) {
			// Only adds objects with Magnetic group
			if (body.IsInGroup("Magnetic")) {
				
				// Magnetic rigidbodies and characterbodies are treated differently
				if (body.IsInGroup("MagneticCharacter")) {
					MagneticCharacterComponent magCharComp = null;

					foreach (var child in body.GetParent().GetChildren()) {
						if (child is MagneticCharacterComponent) {
							magCharComp = (MagneticCharacterComponent)child;
							break;
						}
					}

					// Uses the magcharcomp to get the bodycopy of the character before switching to rigid
					if (magCharComp != null) {
						
						RigidBody2D bodyCopy = magCharComp.GetBodyCopy();
						if (bodyCopy != null) {
							body.AddToGroup("Affected");
						
							MagneticComponent newObject = bodyCopy.GetNodeOrNull<MagneticComponent>("MagneticComponent");

							if (!attractedObjects.ContainsKey(bodyCopy)) {

								if (magCharComp.GetIsRigidPhysics()) {
									magCharComp.SwapToRigid();
									magCharComp.CanSwapToCharacter = false;
								}

								attractedObjects.Add(bodyCopy, newObject);
							}
						}
					}
				} else {
					// Just adds the rigidbody and its magnetic component to the list
					MagneticComponent newObject = (MagneticComponent) body.GetNode("MagneticComponent");
					
					if (!attractedObjects.ContainsKey((PhysicsBody2D)body)) {
						attractedObjects.Add((PhysicsBody2D)body, newObject);
					}
				}
			}
		}
	}

	// Called when object is not longer touching the magnet beam
	// Removes object from dict of attracted objects
	private void OnBodyExitedBeam(Node body) {
		// Body can only be detached if it's being pushed by the beam but not attached to the magnet
		if (body != EnteredBody) {
			if (body is PhysicsBody2D) {

				PhysicsBody2D itemToRemove = null;
				
				// Find item in dict with the exited body as the key
				foreach (PhysicsBody2D item in attractedObjects.Keys) {
					if (item == ((PhysicsBody2D)body)) {
						itemToRemove = item;
					}
				}

				// If body was in dict, remove from dict
				if (itemToRemove != null) {
					MagneticComponent magComp = attractedObjects[itemToRemove];

					MagneticCharacterComponent magCharComp = magComp.GetMagneticCharacterComponent();
					if (magCharComp != null) {
						magCharComp.GetCharacter().RemoveFromGroup("Affected");
						magCharComp.SwapToCharacter();
						magCharComp.CanSwapToCharacter = true;
					}

					attractedObjects.Remove(itemToRemove);
				}
			}
		}
	}
	
	// Called when object touches the magnet itself
	private void OnBodyEntered(Node2D body) {
		// Store body if it is magnetic, the magnet is activated and there is no other object attached
		if (body.IsInGroup("Magnetic") && activated && !isObjectAttached) {
			EnteredBody = (PhysicsBody2D) body;
		}
	}

	// Called when object is not longer touching the magnet itself
	private void OnBodyExited(Node2D body) {
		if (body == EnteredBody) {
			EnteredBody = null;
		}
	}

	private void AttachObject(PhysicsBody2D body, MagneticComponent bodyMagComp) {
		if (body.GetParent() != this && body is PhysicsBody2D && bodyMagComp.GetCanJoin()) {
			DetachAll();
			isObjectAttached = true;	
			attachedObject = body;
			
			MagneticCharacterComponent magCharComp = bodyMagComp.GetMagneticCharacterComponent();
			if (magCharComp != null) {
				magCharComp.CanSwapToCharacter = false;
			}

			isItem = attachedObject.IsInGroup("Item");
			if (isItem) {
				attachedObject.GetNode<ItemComponent>("ItemComponent").SetIsBeingHeld(true);
			}
			objectCollisionLayer = attachedObject.CollisionLayer;
			attachedObject.CollisionLayer = 1u << 3;
			
			// Remove object from original parent and add to this
			objectParent = attachedObject.GetParent();
			objectParent.RemoveChild(attachedObject);
			_anchor.AddChild(attachedObject);

			DamageComponent damageComponent = attachedObject.GetNodeOrNull<DamageComponent>("DamageComponent");

			if (damageComponent == null) {
				Array<Node> children = attachedObject.GetChildren();
				for (int i = 0; i < children.Count; i++) {
					damageComponent = children[i].GetNodeOrNull<DamageComponent>("DamageComponent");
				}
			}

			if (damageComponent != null) {
				damageComponent.AddException(parent);
			}

			// Get the collision shape from the attracted object
			CollisionShape2D mainObjectCollision = null;
			objectCollisions = new();

			// If a main collision shape is set, use that to create the anchor offset
			foreach (Node node in attachedObject.GetChildren()) {
				if (node is CollisionShape2D shape) {
					if (shape.IsInGroup("MainCollisionShape")) {
						mainObjectCollision = shape;
					}
					objectCollisions.Add(shape);
				}
			}

			if (mainObjectCollision == null) {
				mainObjectCollision = objectCollisions[0];
			}
			

			// Get the size of the object to offset the anchor point
			// This keeps the object sitting next to the magnet without overlapping
			if (mainObjectCollision != null) {
				Vector2 shapeSize = GetShapeSize(mainObjectCollision);
				anchorOffset = shapeSize.X >= shapeSize.Y ? shapeSize.X : shapeSize.Y;
			}

			
			attachedObject.Position = new Vector2(anchorOffset / 2, anchorPositionDefault.Y) - mainObjectCollision.Position;
			attachedObject.Rotation = _anchor.Rotation;

			// Adding a copy of the collisions of the object to the player
			for (int i = 0; i < objectCollisions.Count; i++) {
				
				CollisionShape2D currentCollision = (CollisionShape2D) objectCollisions[i].Duplicate();

				currentCollision.SetMeta("IgnoreCollision", true);
				currentCollision.Name = $"{attachedObject.Name}{currentCollision.Name}";
				
				heldObjectInitRotations.Add(currentCollision.Rotation);
				heldObjectCollisions.Add(currentCollision);

				parent.AddChild(currentCollision);
			}

			if (attachedObject is RigidBody2D rigidBody) {
				// Temporarily stop physics on the attached object
				rigidBody.Freeze = true;
				rigidBody.Sleeping = true;
				rigidBody.ContinuousCd = RigidBody2D.CcdMode.CastRay;
			}

			attachedObjectMagComp = bodyMagComp;
		}
	}

	// Detach any object from the magnet beam or magnet
	public void Detach() {
		if (isObjectAttached) {
			// Store object space data
			Vector2 objectPosition = attachedObject.GlobalPosition;
			float objectRotation = attachedObject.GlobalRotation;

			if (isItem) {
				attachedObject.GetNode<ItemComponent>("ItemComponent").SetIsBeingHeld(false);
			}

			attachedObject.Position = new Vector2(0, 0);
			for (int i = 0; i < heldObjectCollisions.Count; i++) {
				heldObjectCollisions[i].SetMeta("IgnoreCollision", false);
				parent.RemoveChild(heldObjectCollisions[i]);
			}
			heldObjectCollisions = new();
			heldObjectInitRotations = new();
			objectCollisions = new();

			// Return the child to it's original parent
			_anchor.RemoveChild(attachedObject);
			objectParent.AddChild(attachedObject);

			attachedObject.CollisionLayer = objectCollisionLayer;

			if (attachedObject is RigidBody2D rigidBody) {
				// Reenable physics on the attached object
				rigidBody.Sleeping = false;
				rigidBody.Freeze = false;
			}

			// Return object to it's original movement state
			// Adding slight offset from magnet object so it doesn't get put slightly inside magnet and then pushed out
			attachedObject.GlobalPosition = objectPosition + (GlobalPosition.DirectionTo(objectPosition) * 10); 
			attachedObject.GlobalRotation = objectRotation;

			MagneticCharacterComponent magCharComp = attachedObjectMagComp.GetMagneticCharacterComponent();
			if (magCharComp != null) {
				magCharComp.CanSwapToCharacter = false;
				magCharComp.GetCharacter().RemoveFromGroup("Affected");
				magCharComp.SwapToCharacter();
			}

			attachedObjectMagComp.SetMagnetParent(null);
			attachedObjectMagComp = null;


			isObjectAttached = false;
		}

		// Reset anchor position
		_anchor.Position = anchorPositionDefault;
		
		objectParent = null;
		RetriggerBeamDetection();

	}

	private void DetachAll() {
		foreach (var objectKey in attractedObjects.Keys) {
			OnBodyExitedBeam(objectKey);
		}
		attractedObjects = new Dictionary<PhysicsBody2D, MagneticComponent>{};
	}

	// Moves the magnet beam really far away for it to then be moved back in Process()
	// so that the beam entered signal retriggers
	private void RetriggerBeamDetection() {
		_magnetBeam.Position = new Vector2(99999, 0);
	}

	public void DropItem() {
		if (isObjectAttached && isItem) {
			Detach();
		}
	}

	public bool HasItem() {
		if (isItem && isObjectAttached) {
			return true;
		}
		return false;
	}

	public bool HasObject() {
		return  isObjectAttached;
	}

	public Array<CollisionShape2D> GetHeldObjectCollisions() {
		return heldObjectCollisions;
	}

	public RigidBody2D GetItem() {
		if (isItem && isObjectAttached) {
			return (RigidBody2D)attachedObject;
		} 
		return null;
	}

	public PhysicsBody2D GetAttachedObject() {
		if (isObjectAttached) {
			return attachedObject;
		} 
		return null;
	}

	public void ToggleActivation(bool weak, bool strong) {
		if (activated) {
			SetActivation(false, false);
		} else {
			SetActivation(weak, strong);
		}
	}

	public void SetActivation(bool weak, bool strong) {
		activated = weak || strong;
		strongMagnet = strong;
		_beamSpriteWeak.Visible = weak;
		_beamSpriteStrong.Visible = strong;

		// Retriggers the beam only when magnet is first activated
		if (activated && _magnetBeam.ProcessMode == ProcessModeEnum.Disabled) {
			RetriggerBeamDetection();
		}

		_magnetBeam.ProcessMode = activated ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
		_magnetBeam.SetBlockSignals(!activated);

		_tileBeamCast.Enabled = activated;
		
		if (!activated && attractedObjects.Count > 0) {
			DetachAll();
		}
	}

	public void SetPullMode(bool pullmodeInput) {
		if (pullMode && !pullmodeInput && isObjectAttached) {
			blast = true;
		}
		pullMode = pullmodeInput;
		canJoin = pullmodeInput;
	}

	public bool GetPullMode() {
		return pullMode;
	}

	public void ForceObject(Vector2 collisionPoint, double delta) {
		
		// Vector that is positive or negative depending on what pull mode the magnet is in
		Vector2 pushForce = pullMode ? collisionPoint - GlobalPosition : GlobalPosition - collisionPoint;
	
		// Vector that is larger the closer the Object is to the magnet
		float magnetStrength = Math.Clamp(beamLength - collisionPoint.DistanceTo(GlobalPosition), 1, beamLength);
		
		// Handle force if parent is CharacterBody2D
		if (parentCharacter != null) {
			float multiplier = strongMagnet ? 1.4f : 1;
			parentCharacter.Velocity += pushForce * multiplier * magnetStrength / 3f * (float)delta;

		// Handle force parent is RigidBody2D
		} else if (parentRigid != null) {
			float multiplier = strongMagnet ? 40 : 16;
			
			parentRigid.ApplyForce(pushForce * magnetStrength * multiplier * (float)delta, collisionPoint - GlobalPosition);
		}
	}

	public void SetRigidParent(RigidBody2D parent) {
		parentRigid = parent;
		parentCharacter = null;
	}

	public void SetCharacterParent(CharacterBody2D parent) {
		parentCharacter = parent;
		parentRigid = null;
	}

	/// <summary>
	/// Given a CollisionShape2D, return a Vector2 representing the size of the shape.
	/// <para> If the size of the shape can only be represented by a float, return a Vector2 
	/// with X being the value and Y being 0.</para>
	/// </summary>
	public Vector2 GetShapeSize(CollisionShape2D collisionShape) {
		// Rectangle
		if (collisionShape.Shape is RectangleShape2D rectangleShape) {
			Vector2 size = rectangleShape.Size;
			// GD.Print($"Rectangle Size: {size}");
			return size;
		}
		// Circle
		else if (collisionShape.Shape is CircleShape2D circleShape) {
			Vector2 diameter = new Vector2(circleShape.Radius * 2, 0);
			// GD.Print($"Circle Diameter: {diameter}");
			return diameter;
		}
		// Capsule
		else if (collisionShape.Shape is CapsuleShape2D capsuleShape) {
			float height = capsuleShape.Height;
			float width = capsuleShape.Radius * 2;
			Vector2 size = new Vector2(height, width);
			// GD.Print($"Capsule Size: Width = {width}, Height = {height}");
			return size;
		}
		// Polygon
		else if (collisionShape.Shape is ConvexPolygonShape2D polygonShape) {
			Vector2[] points = polygonShape.Points;
			if (points.Length > 0) {
				// Calculate the size by finding the bounds of the polygon
				Rect2 bounds = new Rect2(points[0], Vector2.Zero);
				for (int i = 1; i < points.Length; i++) {
					bounds = bounds.Merge(new Rect2(points[i], Vector2.Zero));
				}
				// GD.Print($"Polygon Size: {bounds.Size}");
				return bounds.Size; 
			}
			return Vector2.Zero;
		}
		// Any other shape
		if (true) {
			GD.PushError(collisionShape, " ", collisionShape.GetPath(), " Shape type not supported for size retrieval");
			return Vector2.Zero;
		}
	}
}

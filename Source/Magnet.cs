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

	private RigidBody2D parentRigid;
	private CharacterBody2D parentCharacter;
	private bool canJoin;
	private PhysicsBody2D EnteredBody;

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
	private Dictionary<PhysicsBody2D, MagneticComponent> attractedObjects = new Dictionary<PhysicsBody2D, MagneticComponent>{};
	private Sprite2D _beamSpriteWeak;
	private Sprite2D _beamSpriteStrong;

	private bool isObjectAttached = false;

	private Marker2D _anchor;
	private Vector2 anchorPositionDefault;
	private float anchorOffset = 0;

	private Node objectParent;

	private bool blast;
	
	private Vector2 draw1 = Vector2.Zero;
	private Vector2 draw2 = Vector2.Zero;
	private Vector2 draw3 = Vector2.Zero;
	private Vector2 draw4 = Vector2.Zero;
	private Vector2 draw5 = Vector2.Zero;
	private Vector2 draw6 = Vector2.Zero;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		_anchor = GetNode<Marker2D>("Anchor");
		anchorPositionDefault = _anchor.Position;
		_magnetBeam = GetNode<Area2D>("MagnetBeam");
		_beamSpriteWeak = GetNode<Sprite2D>("BeamSpriteWeak");
		_beamSpriteStrong = GetNode<Sprite2D>("BeamSpriteStrong");
		_tileBeamCast = GetNode<RayCast2D>("TileBeamCast");
		_objectCheck = GetNode<RayCast2D>("ObjectCheck");
		_physicsObject = GetNode<StaticBody2D>("PhysicsObject");

		_beamCheck1 = _magnetBeam.GetNode<RayCast2D>("BeamCheck1");
		_beamCheck2 = _magnetBeam.GetNode<RayCast2D>("BeamCheck2");
		_beamCheck3 = _magnetBeam.GetNode<RayCast2D>("BeamCheck3");

		_magnetBeam.Connect("body_entered", new Callable(this, MethodName.OnBodyEnteredBeam));
		_magnetBeam.Connect("body_exited", new Callable(this, MethodName.OnBodyExitedBeam));

		if (parent != null) {
			if (parent is RigidBody2D rigidBody) parentRigid = rigidBody;
			if (parent is CharacterBody2D characterBody) parentCharacter = characterBody;
			_physicsObject.AddCollisionExceptionWith(GetParent());
		}

		
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
	}

	public override void _Draw()
    {
        DrawLine(ToLocal(draw1), ToLocal(draw2), Colors.Green, 4.0f);
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
			// magneticComponent.ZeroVelocity();

			if (blast) {
				magneticComponent.ForceObject(attachedObject.GlobalPosition, GlobalPosition, beamLength, pullMode, false, true, delta);

				MagneticCharacterComponent magCharComp = magneticComponent.GetMagneticCharacterComponent();

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

		if (attachedObject != null) {
			// Disabling beam sprite if object attached
			if (strongMagnet) {
				_beamSpriteStrong.Visible = false;
			} else {
				_beamSpriteWeak.Visible = false;
			}
			if (!activated || !canJoin) {
				Dettach();
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

					Vector2I collisionCoords =  tileMap.LocalToMap(_tileBeamCast.GetCollisionPoint());

					// Offsetting the collision point to account for bad data when converting from float to int
					if (_tileBeamCast.GetCollisionPoint().X < GlobalPosition.X) collisionCoords.X = collisionCoords.X - 1;
					if (_tileBeamCast.GetCollisionPoint().Y < GlobalPosition.Y) collisionCoords.Y = collisionCoords.Y - 1;
					
					// TileData data = tileMap.GetCellTileData(0, collisionCoords);
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

					// Attach object that reaches the magnet
					MagneticComponent magComp = attractedObjects[body];
					if (EnteredBody == body && attachedObject != body && canJoin) {
						// Dettaching object from any magnet that is already holding it
						if (magComp.IsBeingHeld()) {
							magComp.GetMagnetParent().Dettach();
						}
						magComp.SetMagnetParent(this);

						AttachObject(body, magComp);
					}

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
					
					magComp.ForceObject(collisionPoint, GlobalPosition, beamLength, pullMode, strongMagnet, false, delta);
					
				}
			}
		}

		// Failsafe for if object is not in beam but is still included in the attractedObjects Dict
		// TODO: Make this never actually occur 
		if (!_beamCheck1.IsColliding() && !_beamCheck2.IsColliding() && !_beamCheck3.IsColliding() && attractedObjects.Count > 0 && !isObjectAttached) {
			DettachAll();
		}

		QueueRedraw();
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
						body.AddToGroup("Affected");
						MagneticComponent newObject = (MagneticComponent) bodyCopy.GetNode("MagneticComponent");

						if (!attractedObjects.ContainsKey(bodyCopy)) {
							magCharComp.SwapToRigid();

							attractedObjects.Add(bodyCopy, newObject);
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
		// Body can only be dettached if it's being pushed by the beam but not attached to the magnet
		if (body != attachedObject) {
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
		if (body.GetParent() != this && body is PhysicsBody2D) {
			isObjectAttached = true;	
			attachedObject = body;
			
			// Remove object from original parent and add to this
			objectParent = attachedObject.GetParent();
			objectParent.RemoveChild(attachedObject);
			_anchor.AddChild(attachedObject);

			// Get the collision shape from the attracted object
			CollisionShape2D ObjectCollision = null;
			foreach (Node node in attachedObject.GetChildren()) {
				if (node is CollisionShape2D shape) {
					ObjectCollision = shape;
				}
			}

			// Get the size of the object to offset the anchor point
			// This keeps the object sitting next to the magnet without overlapping
			if (ObjectCollision != null) {
				Vector2 shapeSize = GetShapeSize(ObjectCollision);
				anchorOffset = shapeSize.X >= shapeSize.Y ? shapeSize.X : shapeSize.Y;
			}
			
			attachedObject.Position = new Vector2(anchorOffset / 2, anchorPositionDefault.Y);
			attachedObject.Rotation = _anchor.Rotation;

			if (attachedObject is RigidBody2D rigidBody) {
				// Temporarily stop physics on the attached object
        		rigidBody.Freeze = true;
				rigidBody.Sleeping = true;
			}

			attachedObjectMagComp = bodyMagComp;
		}
	}

	// Detach any object from the magnet beam or magnet
	private void Dettach() {
		if (isObjectAttached) {
			// Store object space data
			Vector2 objectPosition = attachedObject.GlobalPosition;
			float objectRotation = attachedObject.GlobalRotation;

			attachedObject.Position = new Vector2(0, 0);

			// Return the child to it's original parent
			_anchor.RemoveChild(attachedObject);
			objectParent.AddChild(attachedObject);

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

	private void DettachAll() {
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
			DettachAll();
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

	// Given a CollisionShape2D, return a Vector2 representing the size of the shape
	/*
		If the size of the shape can only be represented by a float, return a Vector2 
		with X being the value and Y being 0
	*/
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

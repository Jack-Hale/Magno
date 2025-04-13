using Godot;
using Godot.Collections;
using System;

// [Tool]
public partial class PathFindingComponent : Node2D {

    [Export]
    public float veiwRadius = 500;
    private CharacterBody2D parent;
    private CharacterBody2D player;
    private RayCast2D[] rays = [
        new RayCast2D(),
        new RayCast2D(),
        new RayCast2D(),
        new RayCast2D(),
        new RayCast2D()
    ];
    private float rayOffset = 0;

    private Vector2 lastDetectionPoint;

    private float parentGreaterSize;
    private Vector2 parentShapeSize;
    private uint collision = (1u << 0) | (1u << 1) | (1u << 7);
    
	private float storedDistance = float.PositiveInfinity;
    private bool stopLooking = false;
    private uint tileCollisions = (1u << 0) | (1u << 7);
    private Vector2 draw1 = Vector2.Zero;
	private Vector2 draw2 = Vector2.Zero;
    private Vector2 draw3 = Vector2.Zero;
	private Vector2 draw4 = Vector2.Zero;
	private Vector2 draw5 = Vector2.Zero;
	private Vector2 draw6 = Vector2.Zero;
	private Vector2 draw7 = Vector2.Zero;
	private Vector2 draw8 = Vector2.Zero;

    public override void _Draw() {
		DrawLine(ToLocal(draw1), ToLocal(draw2), Colors.Green);
		DrawLine(ToLocal(draw3), ToLocal(draw4), Colors.Green);
		DrawLine(ToLocal(draw5), ToLocal(draw6), Colors.Green);
		DrawLine(ToLocal(draw7), ToLocal(draw8), Colors.Green);
    }

    public override void _Ready() {
        if (GetParent() is CharacterBody2D parent) {
            this.parent = parent;

            CollisionShape2D parentCollision = parent.GetNode<CollisionShape2D>("CollisionShape2D");
            parentShapeSize = GetShapeSize(parentCollision);
            parentGreaterSize = parentShapeSize.X >= parentShapeSize.Y ? parentShapeSize.X : parentShapeSize.Y;

            Array<Node> array = GetTree().Root.GetChildren();
            for (int i = 0; i < array.Count; i++) {
                player = array[i].GetNodeOrNull<CharacterBody2D>("Player");
            }
            
            CollisionShape2D playerCollision = player.GetNode<CollisionShape2D>("CollisionShape2D");
            Vector2 shapeSize = GetShapeSize(playerCollision);
            rayOffset = (shapeSize.X >= shapeSize.Y ? shapeSize.X : shapeSize.Y)/4;


            foreach (RayCast2D ray in rays) {
                ray.CollisionMask = collision;
               
                ray.AddException(parent);

                AddChild(ray);
            }
            
        } else {
            GD.PrintErr($"Parent ({GetParent()}) of PathFindingComponent {this} is not a CharacterBody2D. Type is {GetParent().GetType()}");
            GD.PushError($"Parent ({GetParent()}) of PathFindingComponent {this} is not a CharacterBody2D. Type is {GetParent().GetType()}");
        }
    }

    public override void _PhysicsProcess(double delta) {
        Vector2 playerPosition = ToLocal(player.GlobalPosition);
        bool isDetected = false;
        bool isLooking = false;
        foreach (RayCast2D ray in rays) {
            if (ray.GetCollider() == player) {
                isDetected = true;
                lastDetectionPoint = ToGlobal(ray.TargetPosition);
                stopLooking = false;
                storedDistance = GlobalPosition.DistanceTo(lastDetectionPoint);
            }

            // Ensures the enemy still considers the players last location it saw
            if (GlobalPosition.DistanceTo(lastDetectionPoint) > parentGreaterSize && !stopLooking) {
                isLooking = true;
            }
        }

        if (isDetected) {
            parent.AddToGroup("CanSeePlayer");
            lastDetectionPoint = player.GlobalPosition;
        } else {
            if (isLooking) {
                float currentDistance = GlobalPosition.DistanceTo(lastDetectionPoint);

                // If distance from last detection gets larger, stop looking
                if (currentDistance > storedDistance) {
                    stopLooking = true;
                }

                storedDistance = GlobalPosition.DistanceTo(lastDetectionPoint);
                
                // If can't see player directly, run a query to see if last detected position is through a wall
                var result = FireRayCast(GlobalPosition, lastDetectionPoint, collision);

                if (result.Count > 0) {
                    Node2D collider = (Node2D) result["collider"];
                    if (collider is TileMapLayer) {
                        // stopLooking = true;

                        // Following code checks up and down from the targets last location to see if
                        // there is an empty space it could potentially look for the player. I found that
                        // this never actually happened but there are potentially times where this could
                        // be needed in the future so I'll leave the code in case this happens.

                        /*
                        float angleOffset = Mathf.DegToRad(10);
                        Vector2 direction = GlobalPosition.DirectionTo(lastDetectionPoint);
                        float distance = GlobalPosition.DistanceTo(lastDetectionPoint);

                        Vector2 directionUp = direction.Rotated(angleOffset);
                        Vector2 directionDown = direction.Rotated(-angleOffset);

                        var upCheck = FireRayCast(GlobalPosition, directionUp * distance);
                        var downCheck = FireRayCast(GlobalPosition, directionDown * distance);

                        if (upCheck.Count > 0) {
                            Node2D colliderUp = (Node2D) upCheck["collider"];
                            if (!(colliderUp is TileMapLayer)) {
                                GD.Print(upCheck["collider"]);
                            }
                        }

                        if (downCheck.Count > 0) {
                            Node2D colliderDown = (Node2D) downCheck["collider"];
                            if (!(colliderDown is TileMapLayer)) {
                                GD.Print(downCheck["collider"]);
                            }
                        }
                        */
                    }
                }

                parent.AddToGroup("LookingForPlayer");
                parent.RemoveFromGroup("CanSeePlayer");
            } else {
                parent.RemoveFromGroup("CanSeePlayer");
                parent.RemoveFromGroup("LookingForPlayer");
            }
        }

        // Stop from looking if wall is between enemy and target location
        if (stopLooking) {
            parent.RemoveFromGroup("CanSeePlayer");
            parent.RemoveFromGroup("LookingForPlayer");
        }

        Vector2 baseDirection = (playerPosition - Position).Normalized();

        Vector2 perpendicular = new Vector2(-baseDirection.Y, baseDirection.X);

        Vector2 offset1 = perpendicular * rayOffset;
        Vector2 offset2 = perpendicular * (rayOffset * 2);

        // Creates 5 raycast directions that point to the player but fan out from the centre by offset
        Vector2[] rayDirections = [
            (playerPosition + offset2 - Position).Normalized(), // Left far
            (playerPosition + offset1 - Position).Normalized(), // Left close
            baseDirection,
            (playerPosition - offset1 - Position).Normalized(), // Right close
            (playerPosition - offset2 - Position).Normalized()  // Right far
        ];

        // Setting all the target positions of the rays to the directions
        for (int i = 0; i < 5; i++) {
            if (Position.DistanceTo(playerPosition) < veiwRadius) {
                rays[i].TargetPosition = Position + rayDirections[i] * Position.DistanceTo(playerPosition);
            } else {
                rays[i].TargetPosition = Position + rayDirections[i] * veiwRadius;
            }
        }
        QueueRedraw();
    }

    /// <summary>
    /// Applies a friction to the character's movement slowing them down
    /// </summary>
    /// <returns>Updated vector with friction applied.</returns>
    public Vector2 ApplyFriction(bool justX, Vector2 velocity, float friction, double delta) {
        Vector2 NewVelocity = Vector2.Zero;

		// if (justX) {
		// 	NewVelocity.X = velocity.X;
		// } else {
		// }
        NewVelocity = velocity;

        // Apply friction to reduce speed
        if (Math.Abs(NewVelocity.Length()) > (friction * (float)delta)) {
            Vector2 calc = NewVelocity.Normalized() * (justX ? (parent.IsOnFloor() ? friction : 1) : friction) * (float)delta;
            if (justX) {
                NewVelocity.X -= calc.X;
            } else {
                NewVelocity -= calc;
            }
        }

        else {
            NewVelocity = Vector2.Zero;
        }
		

        return NewVelocity;
    }
    /// <summary>
    /// <para>Moves the character in the direction specified.</para>
    /// </summary>
    /// <returns>Updated vector with movement applied.</returns>
    public Vector2 MoveCharacter(bool justX, Vector2 velocity, Vector2 direction, float maxSpeed, float acceleration, float airAcceleration, double delta) {
		Vector2 NewVelocity = Vector2.Zero;
        float friction = 100;
	
        NewVelocity = velocity;
		
		if (direction == Vector2.Zero) {
			// Apply friction to reduce speed
			if (Math.Abs(NewVelocity.Length()) > (friction * (float)delta)) {
                Vector2 calc = NewVelocity.Normalized() * (justX ? (parent.IsOnFloor() ? friction : 1) : friction) * (float)delta;
				NewVelocity -= calc;

                if (justX) {
                    NewVelocity.X -= calc.X;
                } else {
                    NewVelocity -= calc;
                }
            }

			else {
				NewVelocity = Vector2.Zero;
			}
		}

		// Input, Add acceleration
		if (direction != Vector2.Zero) {
		    // Apply friction to reduce ) {
            Vector2 calc = direction * (parent.IsOnFloor() ? acceleration : airAcceleration) * (float)delta;
            
            if (justX) {
			    NewVelocity.X += calc.X;
			    NewVelocity.X = NewVelocity.LimitLength(maxSpeed).X;
            } else {
                NewVelocity += calc;
			    NewVelocity = NewVelocity.LimitLength(maxSpeed);
            }
		}

		return NewVelocity;
	}

    /// <summary>
    /// When moving, if character would brush up against a wall, it instead moves along it by a distance
    /// </summary>
    /// <returns>Updated vector in the direction of the avoided path.</returns>
	public Vector2 AvoidWallsAir(Vector2 velocity, float avoidDistance, float checkAngle, float angleToTurn) {

		Vector2 start = GlobalPosition;
		Vector2 end = GlobalPosition + velocity.Normalized() * avoidDistance;

		float angleOffset = Mathf.DegToRad(checkAngle);
		Vector2 direction = start.DirectionTo(end);
		float distance = start.DistanceTo(end);

		Vector2 posDir = direction.Rotated(angleOffset);
		Vector2 negDir = direction.Rotated(-angleOffset);

		var posCheck = FireRayCast(start, start + posDir * distance, collision);
		var negCheck = FireRayCast(start, start + negDir * distance, collision);

		bool posTileFound = false;
		bool negTileFound = false;

		if (posCheck.Count > 0) {
			Node2D colliderPos = (Node2D) posCheck["collider"];
			if (colliderPos is TileMapLayer) {
				posTileFound = true;
			}
		} 

		if (negCheck.Count > 0) {
			Node2D colliderNeg = (Node2D) negCheck["collider"];
			if (colliderNeg is TileMapLayer) {
				negTileFound = true;
			}
		}

		if (posTileFound != negTileFound) {
			if (posTileFound) {
				velocity = velocity.Rotated(Mathf.DegToRad(-angleToTurn));
			}
			if (negTileFound) {
				velocity = velocity.Rotated(Mathf.DegToRad(angleToTurn));

			}
		}

		return velocity;
	}

    /// <summary>
    /// In the direction of velocity, will check in front at the parsed distance and angle if there is a wall.
    /// 
    /// <para>Angle checks up and down from the character's centre.</para>
    /// </summary>
    /// <returns>True if wall is found.</returns>
    public bool AvoidWallsGround(Vector2 velocity, float avoidDistance, float checkAngle) {

        float movementDir = Position.DirectionTo(velocity).X;

        movementDir = movementDir >= 0 ? 1 : -1;


        Vector2 start = parent.GlobalPosition;
        Vector2 end = new Vector2(parent.GlobalPosition.X + (avoidDistance * movementDir), parent.GlobalPosition.Y);

        float angleOffset = Mathf.DegToRad(checkAngle);
        Vector2 direction = start.DirectionTo(end);
        float distance = start.DistanceTo(end);

        Vector2 posDir = direction.Rotated(angleOffset);
        Vector2 negDir = direction.Rotated(-angleOffset);

        var posCheck = FireRayCast(start, start + posDir * distance, collision);
        var negCheck = FireRayCast(start, start + negDir * distance, collision);

        // draw1 = start;
        // draw2 = start + posDir * distance;
        // draw3 = start;
        // draw4 = start + negDir * distance;

        bool posTileFound = false;
        bool negTileFound = false;

        if (posCheck.Count > 0) {
            Node2D colliderPos = (Node2D) posCheck["collider"];
            if (colliderPos is TileMapLayer) {
                posTileFound = true;
            }
        } 

        if (negCheck.Count > 0) {
            Node2D colliderNeg = (Node2D) negCheck["collider"];
            if (colliderNeg is TileMapLayer) {
                negTileFound = true;
            }
        }

        if (posTileFound || negTileFound) {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if there is no floor in front of the character in the direction of velocity.
    /// 
    /// <para>distanceAcross defines how far ahead of the character to check.</para>
    /// <para>distanceBelow defines how far past the characters lowest point to check.</para>
    /// </summary>
    /// <returns>True if no floor is detected.</returns>
    public bool CheckNoFloor(Vector2 velocity, float distanceAcross, float distanceBelow) {
        if (velocity != Vector2.Zero) {
            float sizeX = parentShapeSize.X;
            float sizeY = parentShapeSize.Y;

            Vector2 checkFrom = parent.GlobalPosition;
            checkFrom.X = checkFrom.X + (sizeX/2 + distanceAcross) * (velocity.X >= 0 ? 1 : -1);
            
            Vector2 checkTo = checkFrom;
            checkTo.Y = checkTo.Y + (sizeY/2) + distanceBelow;

            // draw5 = checkFrom;
            // draw6 = checkTo;

            var check = FireRayCast(checkFrom, checkTo, tileCollisions);
			if (check.Count > 0) {
				return false;
			}
            return true;

        }
        return false;
    }
    
    /// <summary>
    /// If detection point is above character check above the character to see if there is a ledge it can jump to.
    /// <para>Allows a character to choose to jump onto a ledge that is above their head instead of running underneath it.</para>
    /// <para>distanceAbove defines how far above the character to check.</para>
    /// <para>distanceInFront defines how far in front of the character to check.</para>
    /// </summary>
    /// <returns>True if ground is detected.</returns>
    public bool CheckGroundAbove(Vector2 velocity, float distanceAbove, float distanceInFront) {
        if (lastDetectionPoint.Y < parent.GlobalPosition.Y - parentShapeSize.Y) {

            float movementDir = Mathf.Sign(velocity.X);

            float toX = parent.GlobalPosition.X + ((parentShapeSize.X/2) + distanceInFront) * movementDir;
            float toY = parent.GlobalPosition.Y - (parentShapeSize.Y/2) - distanceAbove;

            Vector2 checkFrom = parent.GlobalPosition;
            Vector2 checkTo = new Vector2(toX, toY);
            
            draw7 = checkFrom;
            draw8 = checkTo;
            
            var check = FireRayCast(checkFrom, checkTo, tileCollisions);

			if (check.Count > 0) {
				return true;
			}
            return false;
        }
        return false;
    }

    /// <summary>
    /// Gets the distance from parent to any collisionLayer in the path of direction at the specified maximum distance.
    /// </summary>
    /// <returns>
    /// The distance from parent to collision with collisionLayer.
    /// <para>-1 if no collision on collisionLayer is detected.</para>
    /// </returns>
    public float GetDistanceFromCollsionLayer(float checkDistance, Vector2 direction, uint collisionLayer) {
        direction = direction.Normalized();
        Vector2 from = parent.GlobalPosition;
        Vector2 to = from + (direction * checkDistance);
        var check = FireRayCast(from, to, collisionLayer);
        if (check.Count > 0) {
            return parent.GlobalPosition.DistanceTo((Vector2) check["position"]);
        }
        return -1;
    }

    /// <summary>
    /// Gets the angle from one vector to another normalised between 0 and 2π where 0 is right.
    /// </summary>
    /// <returns>Angle between 0 and 2π.</returns>
    public float GetAngle(Vector2 from, Vector2 to) {
        float angleToTarget = from.DirectionTo(to).Angle();
        return angleToTarget - 2 * Mathf.Pi * Mathf.Floor(angleToTarget / (2 * Mathf.Pi));
    }

    private Dictionary FireRayCast(Vector2 from, Vector2 to, uint collisions) {
        PhysicsDirectSpaceState2D spaceState = GetWorld2D().DirectSpaceState;
        PhysicsRayQueryParameters2D query = PhysicsRayQueryParameters2D.Create(from, to, collisions);
        Dictionary result = spaceState.IntersectRay(query);
        return result;
    }

    public CharacterBody2D GetPlayer() {
        return player;
    }

    public Vector2 GetLastDetectionPoint() {
        return lastDetectionPoint;
    }

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
			Vector2 size = new Vector2(width, height);
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

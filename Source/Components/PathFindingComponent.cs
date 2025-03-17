using Godot;
using Godot.Collections;
using System;

// [Tool]
public partial class PathFindingComponent : Node2D {

    [Export]
    public float veiwRadius = 200;
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

    private float parentShapeSize;
    private RigidBody2D collision = new();

    public override void _Ready() {
        if (GetParent() is CharacterBody2D parent) {
            this.parent = parent;

            CollisionShape2D parentCollision = parent.GetNode<CollisionShape2D>("CollisionShape2D");
            Vector2 t = GetShapeSize(parentCollision);
            parentShapeSize = t.X >= t.Y ? t.X : t.Y;

            Array<Node> array = GetTree().Root.GetChildren();
            for (int i = 0; i < array.Count; i++) {
                player = array[i].GetNodeOrNull<CharacterBody2D>("Player");
            }
            
            CollisionShape2D playerCollision = player.GetNode<CollisionShape2D>("CollisionShape2D");
            Vector2 shapeSize = GetShapeSize(playerCollision);
            rayOffset = (shapeSize.X >= shapeSize.Y ? shapeSize.X : shapeSize.Y)/4;


            // for (int i = 1; i <= 8; i++) {
            //     rayDefault.SetCollisionMaskValue(i, true);
            // }
            // AddChild(rayDefault);

            collision.SetCollisionMaskValue(1, true);
            collision.SetCollisionMaskValue(2, true);
            collision.SetCollisionMaskValue(8, true);

            foreach (RayCast2D ray in rays) {
                ray.CollisionMask = collision.CollisionMask;
               
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
        bool foundWall = false;
        foreach (RayCast2D ray in rays) {
            if (ray.GetCollider() == player) {
                isDetected = true;
                lastDetectionPoint = ToGlobal(ray.TargetPosition);
            }

            // if (!(ray.GetCollider() is TileMapLayer)) {
            //     foundWall = false;
            // }

            // Ensures the enemy still considers the players last location it saw
            if (GlobalPosition.DistanceTo(lastDetectionPoint) > parentShapeSize) {
                isLooking = true;
            }
        }

        if (isDetected) {
            parent.AddToGroup("CanSeePlayer");
        } else {
            if (isLooking) {
                
                // If can't see player directly, run a query to see if last detected position is through a wall
                var result = FireRayCast(GlobalPosition, lastDetectionPoint);

                if (result.Count > 0) {
                    Node2D collider = (Node2D) result["collider"];
                    if (collider is TileMapLayer) {
                        foundWall = true;

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
        if (foundWall) {
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
    }

    public Dictionary FireRayCast(Vector2 from, Vector2 to) {
        PhysicsDirectSpaceState2D spaceState = GetWorld2D().DirectSpaceState;
        PhysicsRayQueryParameters2D query = PhysicsRayQueryParameters2D.Create(from, to, collision.CollisionMask);
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

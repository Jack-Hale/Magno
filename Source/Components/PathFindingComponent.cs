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

    public override void _Ready() {
        if (GetParent() is CharacterBody2D parent) {
            this.parent = parent;

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

            foreach (RayCast2D ray in rays) {
                ray.SetCollisionMaskValue(1, true);
                ray.SetCollisionMaskValue(2, true);
                ray.SetCollisionMaskValue(8, true);
               
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
        foreach (RayCast2D ray in rays) {
            if (ray.GetCollider() == player) {
                isDetected = true;
            }
        }
        if (isDetected) {
            parent.AddToGroup("CanSeePlayer");
        } else {
            parent.RemoveFromGroup("CanSeePlayer");
        }

        // Base direction
        Vector2 baseDirection = (playerPosition - Position).Normalized();

        // Get perpendicular vector
        Vector2 perpendicular = new Vector2(-baseDirection.Y, baseDirection.X);

        // Offsets
        Vector2 offset1 = perpendicular * rayOffset;
        Vector2 offset2 = perpendicular * (rayOffset * 2);

        // Generate Ray Directions
        Vector2[] rayDirections = [
            (playerPosition + offset2 - Position).Normalized(), // Left far
            (playerPosition + offset1 - Position).Normalized(), // Left close
            baseDirection,
            (playerPosition - offset1 - Position).Normalized(), // Right close
            (playerPosition - offset2 - Position).Normalized()  // Right far
        ];


        for (int i = 0; i < 5; i++) {
            if (Position.DistanceTo(playerPosition) < veiwRadius) {
                rays[i].TargetPosition = Position + rayDirections[i] * Position.DistanceTo(playerPosition);
            } else {
                rays[i].TargetPosition = Position + rayDirections[i] * veiwRadius;
            }
        }
    }

    public CharacterBody2D GetPlayer() {
        return player;
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

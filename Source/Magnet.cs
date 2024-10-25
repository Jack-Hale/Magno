using Godot;
using System;

public partial class Magnet : Area2D
{
	[Export]
	private bool activated = false;
	[Export]
	private bool pullMode = true;
	private bool canJoin;
	private PhysicsBody2D EnteredBody;

	private RayCast2D _magnetBeam;
	
	private RigidBody2D attractedObject;
	private MagneticComponent objectMagComp;
	
	private Sprite2D _magnetBeamSprite;

	private bool ObjectAttached = false;

	private Marker2D _anchor;
	private Vector2 anchorPositionDefault;
	private float anchorOffset = 0;

	private Node ObjectParent;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		_anchor = GetNode<Marker2D>("Anchor");
		anchorPositionDefault = _anchor.Position;
		_magnetBeam = GetNode<RayCast2D>("MagnetBeam");
		_magnetBeamSprite = _magnetBeam.GetNode<Sprite2D>("Sprite2D");

		if (_magnetBeam == null) {
			GD.PrintErr("No Raycast found");
			GD.PushError("No Raycast found");
		}

		canJoin = pullMode;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {

	}

    public override void _PhysicsProcess(double delta) {
		if (activated) {
			if (_magnetBeam.IsColliding()) {

				// Check if the object in the beam is in the Magnetic group.
				Node Object = (Node) _magnetBeam.GetCollider();
				if (Object.IsInGroup("Magnetic")) {
					MagneticComponent newObject = (MagneticComponent) Object.FindChild("MagneticComponent");

					// Checking if the object is a different object to the previous call. 
					// If it is different, dettach the old one
					if (attractedObject != null && attractedObject != Object) {
						objectMagComp.Dettach();
						// GD.Print("Dettach (new object detected)");
						Dettach();
					}

					if (attractedObject == null) {
						attractedObject = (RigidBody2D)Object;
						objectMagComp = newObject;
					}
					if (EnteredBody == attractedObject && canJoin) {
						AttachObject();
					}
					
					objectMagComp.ForceObject(_magnetBeam.GetCollisionPoint(), GlobalPosition, _magnetBeam.TargetPosition.X, pullMode);
					
				} else {
					// GD.Print("Dettach (not magnetic)");
					Dettach();
				}
			} else {
				if (pullMode) {
					// Dettach();
				}
			}
		} else {
			// Dettach the object when deactivating magnet
			// GD.Print("Dettach (deactivate)");
			Dettach();
		}
		
		if (ObjectAttached) {
			attractedObject.Position = _anchor.Position;
		}
    }

	private void AttachObject() {
		if (attractedObject.GetParent() != this) {
			GD.Print("Attracted");
			// Store object space data
			Vector2 ObjectPosition = attractedObject.GlobalPosition;
			float ObjectRotation = attractedObject.GlobalRotation;
			Vector2 ObjectVelocity = attractedObject.LinearVelocity;

			ObjectParent = attractedObject.GetParent();
			ObjectParent.RemoveChild(attractedObject);
			AddChild(attractedObject);
			CollisionShape2D ObjectCollision = null;
			foreach (Node node in attractedObject.GetChildren()) {
				if (node is CollisionShape2D) {
					ObjectCollision = (CollisionShape2D)node;
				}
			}

			if (ObjectCollision != null) {
				GD.Print(ObjectCollision.Shape);
				Vector2 shapeSize = GetShapeSize(ObjectCollision);
				anchorOffset = shapeSize.X >= shapeSize.Y ? shapeSize.X : shapeSize.Y;
			}
			_anchor.Position = new Vector2(_anchor.Position.X + (anchorOffset / 2), _anchor.Position.Y);

			// Return object to it's original movement state
			attractedObject.GlobalPosition = ObjectPosition;
			attractedObject.GlobalRotation = ObjectRotation;
			// attractedObject.LinearVelocity = ObjectVelocity;

			ObjectAttached = true;
		}
	}

	private void Dettach() {
		if (attractedObject != null) {
		
			if (ObjectAttached) {
				// Store object space data
				Vector2 ObjectPosition = attractedObject.GlobalPosition;
				float ObjectRotation = attractedObject.GlobalRotation;
				Vector2 ObjectVelocity = attractedObject.LinearVelocity;

				RemoveChild(attractedObject);
				ObjectParent.AddChild(attractedObject);

				// Return object to it's original movement state
				attractedObject.GlobalPosition = ObjectPosition;
				attractedObject.GlobalRotation = ObjectRotation;
				attractedObject.LinearVelocity = ObjectVelocity;

				ObjectAttached = false;
			}
			
			_anchor.Position = anchorPositionDefault;

			objectMagComp.Dettach();

			ObjectParent = null;
			attractedObject = null;
			objectMagComp = null;
		}
	}

    private void OnBodyEntered(Node2D body) {
		if (body.IsInGroup("Magnetic") && activated && !ObjectAttached) {
			if (body.GetParent().IsInGroup("Magnetic")) {
				EnteredBody = (PhysicsBody2D) body.GetParent();
			} else {
				EnteredBody = (PhysicsBody2D) body;
			}
		}
	}

	private void OnBodyExited(Node2D body) {
		if (body == EnteredBody) {
			EnteredBody = null;
		}
	}

	public void SetActivation(bool status) {
		activated = status;
		_magnetBeamSprite.Visible = status;
	}

	public void SetPullMode(bool canJoin) {
		pullMode = canJoin;
		this.canJoin = canJoin;
	}

	public bool GetPullMode() {
		return pullMode;
	}

	public Vector2 GetShapeSize(CollisionShape2D collisionShape)
    {
        if (collisionShape.Shape is RectangleShape2D rectangleShape)
        {
            Vector2 size = rectangleShape.Size; // Full size from extents
            GD.Print($"Rectangle Size: {size}");
			return size;
        }
        else if (collisionShape.Shape is CircleShape2D circleShape)
        {
            Vector2 diameter = new Vector2(circleShape.Radius * 2, 0);
            GD.Print($"Circle Diameter: {diameter}");
			return diameter;
        }
        else if (collisionShape.Shape is CapsuleShape2D capsuleShape)
        {
            float height = capsuleShape.Height;
            float width = capsuleShape.Radius * 2;
			Vector2 size = new Vector2(height, width);
            GD.Print($"Capsule Size: Width = {width}, Height = {height}");
			return size;
        }
        else if (collisionShape.Shape is ConvexPolygonShape2D polygonShape)
        {
            Vector2[] points = polygonShape.Points;
            if (points.Length > 0)
            {
                // Calculate the size by finding the bounds of the polygon
                Rect2 bounds = new Rect2(points[0], Vector2.Zero);
                for (int i = 1; i < points.Length; i++)
                {
                    bounds = bounds.Merge(new Rect2(points[i], Vector2.Zero));
                }
                GD.Print($"Polygon Size: {bounds.Size}");
				return bounds.Size; 
            }
			return Vector2.Zero;
        }
        else
        {
            GD.Print("Shape type not supported for size retrieval.");
			return Vector2.Zero;
        }
    }
}

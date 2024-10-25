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

					/*
					I dont think I want to do this but I'm keeping it here just in case.

					// Checking if the object is a different object to the previous call. 
					// If it is different, dettach the old one
					if (attractedObject != null && attractedObject != Object) {
						objectMagComp.Dettach();
						// GD.Print("Dettach (new object detected)");
						Dettach();
					}
					*/

					// Accessing the object itself and the magnetic component of the attracted object
					if (attractedObject == null) {
						attractedObject = (RigidBody2D)Object;
						objectMagComp = newObject;
					}

					// If the attracted object has hit the magnet, attach the object
					if (EnteredBody == attractedObject && canJoin) {
						AttachObject();
					}
					
					objectMagComp.ForceObject(_magnetBeam.GetCollisionPoint(), GlobalPosition, _magnetBeam.TargetPosition.X, pullMode);
					
				} else {
					// GD.Print("Dettach (not magnetic)");
					Dettach();
				}
			} else {
				if (!ObjectAttached) {
					Dettach();
				}
			}
			if (ObjectAttached && !pullMode) {
				Dettach();
			}
		} else {
			// Dettach the object when deactivating magnet
			// GD.Print("Dettach (deactivate)");
			Dettach();
		}
		
		// Keeping the attached object to the anchor position
		if (ObjectAttached) {
			attractedObject.Position = _anchor.Position;
		}
    }

	private void AttachObject() {
		if (attractedObject.GetParent() != this) {

			// Store object space data
			Vector2 ObjectPosition = attractedObject.GlobalPosition;
			float ObjectRotation = attractedObject.GlobalRotation;
			Vector2 ObjectVelocity = attractedObject.LinearVelocity;

			// Remove object from original parent and add to this
			ObjectParent = attractedObject.GetParent();
			ObjectParent.RemoveChild(attractedObject);
			AddChild(attractedObject);

			// Get the collision shape from the attracted object
			CollisionShape2D ObjectCollision = null;
			foreach (Node node in attractedObject.GetChildren()) {
				if (node is CollisionShape2D) {
					ObjectCollision = (CollisionShape2D)node;
				}
			}

			// Get the size of the object to offset the achor point
			// This keeps the object sitting next to the magnet without overlapping
			if (ObjectCollision != null) {
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

	// Detach any object from the magnet beam or magnet
	private void Dettach() {
		if (attractedObject != null) {
			
			// Removes any object from the magnet
			if (ObjectAttached) {
				// Store object space data
				Vector2 ObjectPosition = attractedObject.GlobalPosition;
				float ObjectRotation = attractedObject.GlobalRotation;
				Vector2 ObjectVelocity = attractedObject.LinearVelocity;

				// Return the child to it's original parent
				RemoveChild(attractedObject);
				ObjectParent.AddChild(attractedObject);

				// Return object to it's original movement state
				attractedObject.GlobalPosition = ObjectPosition;
				attractedObject.GlobalRotation = ObjectRotation;
				attractedObject.LinearVelocity = ObjectVelocity;

				ObjectAttached = false;
			}
			
			// Reset anchor position
			_anchor.Position = anchorPositionDefault;

			objectMagComp.Dettach();

			ObjectParent = null;
			attractedObject = null;
			objectMagComp = null;
		}
	}

    private void OnBodyEntered(Node2D body) {
		// Store body if it is magnetic, the magnet is activated and there is no other object attached
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

	public void SetPullMode(bool pullmode) {
		pullMode = pullmode;
		canJoin = pullmode;
	}

	public bool GetPullMode() {
		return pullMode;
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

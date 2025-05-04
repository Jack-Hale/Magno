using Godot;
using Godot.Collections;
using System;

public partial class DamageComponent : Node2D {
	[Export]
	public float damageAmount = 10;

	[Export]
	public float damageCooldown = 0.5f;

	[Export]
	public bool velocityScaling = false;

	[Export]
	public float velocityScalingAmount = 0.5f;

	[Export]
	public float velocityScalingThreshold = 6;

	[Export]
	public float scaledDamageMax = 40;
	[Export]
	public bool onlyDamagePlayer = false;
	private bool canDamage = true;
	private CollisionObject2D parent = null;
	private CollisionObject2D greaterParent = null;
	private CharacterBody2D characterParent = null;
	private RigidBody2D rigidParent = null;
	private Vector2 previousPosition;
    private float deltaTime;
	private float parentSpeed;

	private float impactSpeed;
	private Array<Node2D> exceptions = new Array<Node2D>();
	private Dictionary<PhysicsBody2D, HealthComponent> damageArray = new Dictionary<PhysicsBody2D, HealthComponent>();
	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		if (GetParent() is CollisionObject2D parent) {
			this.parent = parent; 

			if (parent is RigidBody2D rigidNode) {
				parent.Connect("body_shape_entered", new Callable(this, MethodName.OnBodyShapeEntered));
				parent.Connect("body_shape_exited", new Callable(this, MethodName.OnBodyShapeExited));

				rigidParent = rigidNode;
				rigidNode.ContactMonitor = true;
				rigidNode.MaxContactsReported = 10;
			} else if (parent is CharacterBody2D charNode) {
				characterParent = charNode;
				CollisionShape2D parentCollision = characterParent.GetNode<CollisionShape2D>("CollisionShape2D");
				Area2D area2D = new();
				area2D.Name = "DamageArea";
				area2D.AddChild(parentCollision.Duplicate());
				characterParent.CallDeferred("add_child", area2D);
				area2D.CollisionLayer = characterParent.CollisionLayer;
				area2D.CollisionMask = (1u << 1) | (1u << 2) | (1u << 4) | (1u << 5);

				area2D.Connect("body_shape_entered", new Callable(this, MethodName.OnBodyShapeEntered));
				area2D.Connect("body_shape_exited", new Callable(this, MethodName.OnBodyShapeExited));
			} else if (parent is Area2D areaNode) {
				areaNode.Connect("body_shape_entered", new Callable(this, MethodName.OnBodyShapeEntered));
				areaNode.Connect("body_shape_exited", new Callable(this, MethodName.OnBodyShapeExited));
			} else {
				velocityScaling = false;
			}

			if (parent.GetParent() is CollisionObject2D greaterParent) {
				this.greaterParent = greaterParent;
			}
			
		} else {
			GD.PrintErr($"DamageComponent {this}, does not have parent of type CollisionObject2D. Parent is, {GetParent().GetType()}, {GetParent().Name}");
			GD.PushError($"DamageComponent {this}, does not have parent of type CollisionObject2D. Parent is, {GetParent().GetType()}, {GetParent().Name}");
		}
	}
    public override void _Process(double delta) {

    }

    public override void _PhysicsProcess(double delta) {
		// Calculating the speed of damage object based on position rather than velocity
		// This prevents it from damaging things when it has high velocity but is not moving (up against a wall, etc)
        float distance = parent.Position.DistanceTo(previousPosition);
        parentSpeed = distance / (float) delta;
        previousPosition = parent.Position;

		if (canDamage) {
			if (damageArray.Keys.Count > 0) {
				float damageAmountCalc = damageAmount;
				if (velocityScaling) {
					// Parent speed must exceed threshold to do damage
					if (impactSpeed >= velocityScalingThreshold) {

						damageAmountCalc += velocityScalingAmount * impactSpeed;

						if (damageAmountCalc > scaledDamageMax) {
							damageAmountCalc = scaledDamageMax;
						}

					} else {
						damageAmountCalc = 0;
					}
				}
				if (damageAmountCalc > 0) {
					foreach (var body in damageArray.Keys) {
						HealthComponent healthComponent = damageArray[body];
						healthComponent.TakeDamage(damageAmountCalc, damageCooldown, parent.GetInstanceId());
					}
				}
			}
		}
	}

	public void OnBodyShapeEntered(Rid bodyRid, Node body, int bodyShapeIndex, int localShapeIndex) {
		if (onlyDamagePlayer && body is not Player) {
			return;
		}
		
		bool isException = false;
		for (int i = 0; i < exceptions.Count; i++) {
			if (exceptions[i] == body) {
				isException = true;
			}
		}

		// Checking that the entered body is not an ignored collision, e.g. an object the player is holding
		if (body is CollisionObject2D collisionObject) {
        	uint shapeOwnerId = collisionObject.ShapeFindOwner(bodyShapeIndex);

			foreach (Node child in collisionObject.GetChildren()) {
				if (child is CollisionShape2D shape && collisionObject.ShapeOwnerGetOwner(shapeOwnerId) == shape) {
					if (shape.HasMeta("IgnoreCollision")) {
						return;
					}
				}
			}
		}


		if (body is PhysicsBody2D physicsBody && body != parent && body != greaterParent && !isException) {
			Array<Node> bodyChildren = physicsBody.GetChildren();
			for (int i = 0; i < bodyChildren.Count; i++) {
				if (bodyChildren[i] is HealthComponent healthComponent && !damageArray.ContainsKey(physicsBody)) {
					impactSpeed = parentSpeed / 100;
					damageArray.Add(physicsBody, healthComponent);
				}
			}
		}
	}

	public void OnBodyShapeExited(Rid bodyRid, Node body, int bodyShapeIndex, int localShapeIndex) {
		if (body is PhysicsBody2D physicsBody) {
			if (damageArray.ContainsKey(physicsBody)) {
				damageArray.Remove(physicsBody);
			}
		}
	}

	public void SetCanDamage(bool canDamage) {
		this.canDamage = canDamage;
	}

	public void SetDamage(float damage) {
		damageAmount = damage;
	}

	public void AddException(Node2D node) {
		exceptions.Add(node);
	}

	public void RemoveException(Node2D node) {
		exceptions.Remove(node);
	}
}

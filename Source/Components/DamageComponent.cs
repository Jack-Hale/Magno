using Godot;
using Godot.Collections;
using System;

public partial class DamageComponent : Node {
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
	private bool canDamage = true;
	private CollisionObject2D parent = null;
	private CharacterBody2D characterParent = null;
	private RigidBody2D rigidParent = null;
	private Vector2 previousPosition;
    private float deltaTime;
	private float parentSpeed;

	private float impactSpeed;
	private Dictionary<PhysicsBody2D, HealthComponent> damageArray = new Dictionary<PhysicsBody2D, HealthComponent>();
	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		if (GetParent() is CollisionObject2D parent) {
			this.parent = parent; 

			parent.Connect("body_entered", new Callable(this, MethodName.OnBodyEntered));
			parent.Connect("body_exited", new Callable(this, MethodName.OnBodyExited));

			if (parent is RigidBody2D rigidParent) {
				this.rigidParent = rigidParent;
				rigidParent.ContactMonitor = true;
				rigidParent.MaxContactsReported = 10;
			} else if (parent is CharacterBody2D characterParent) {
				this.characterParent = characterParent;
			} else {
				velocityScaling = false;
			}
			
		} else {
			GD.PrintErr($"DamageComponent {this}, does not have parent of type CollisionObject2D. Parent is, {GetParent().GetType()}, {GetParent().Name}");
			GD.PushError($"DamageComponent {this}, does not have parent of type CollisionObject2D. Parent is, {GetParent().GetType()}, {GetParent().Name}");
		}
	}
    public override void _Process(double delta) {
		// Calculating the speed of damage object based on position rather than velocity
		// This prevents it from damaging things when it has high velocity but is not moving (up against a wall, etc)
        float distance = parent.Position.DistanceTo(previousPosition);
        parentSpeed = distance / (float) delta;
        previousPosition = parent.Position;
    }

    public override void _PhysicsProcess(double delta) {
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

	public void OnBodyEntered(Node body) {
		if (body is PhysicsBody2D physicsBody) {
			Array<Node> bodyChildren = physicsBody.GetChildren();
			for (int i = 0; i < bodyChildren.Count; i++) {
				if (bodyChildren[i] is HealthComponent healthComponent && !damageArray.ContainsKey(physicsBody)) {
					impactSpeed = parentSpeed / 100;
					damageArray.Add(physicsBody, healthComponent);
				}
			}
		}
	}

	public void OnBodyExited(Node body) {
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
}

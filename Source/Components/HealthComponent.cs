using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.Marshalling;
using System.Xml;

public struct DamageCooldown {
    public ulong DamageSource;
    public float Timer;

    public DamageCooldown(ulong damageSource, float timer) {
        DamageSource = damageSource;
        Timer = timer;
    }
}

public struct DamageNumber {
    public ulong LabelID;
    public float Timer;

    public DamageNumber(ulong labelID, float timer) {
        LabelID = labelID;
        Timer = timer;
    }
}

[Tool]
public partial class HealthComponent : Node2D {
	[Export]
	private float maxHealth = 100;

	private float health;

	private ProgressBar _progressBar;
	private ProgressBar progressBarPassThrough; // Original character progress bar

	private CharacterBody2D character;
	private bool requirePassThrough = false; // If healthcomp is on a bodycopy it will parse the data to the original character
	private HealthComponent passThroughHC; // Original character healthcomp
	private bool sceneClass = false; // Prevents the scene in the tscn file from running code since it is [Tool]

	private List<DamageCooldown> activeCooldowns = new();
	private Queue<DamageNumber> activeDamageNumbers = new Queue<DamageNumber>(20);

	private bool hasDied = false;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		if (GetChildCount() == 0) {
			if (GetParent() is CharacterBody2D parent) {
				PackedScene scene = (PackedScene)GD.Load("res://Scenes/Components/health_component.tscn");
				Node healthComponent = scene.Instantiate();
				Array<Node> children = healthComponent.GetChildren();

				for (int i = 0; i < children.Count; i++) {
					AddChild(children[i].Duplicate());
				}

				health = maxHealth;
				_progressBar = GetNode<ProgressBar>("ProgressBar");
				_progressBar.MaxValue = maxHealth;
				_progressBar.Value = maxHealth;

				character = parent;


			} else {
				if (GetParent().IsInGroup("BodyCopy")) {
					requirePassThrough = true;
					Array<Node> parentChildren = GetParent().GetParent().GetChildren();
					for (int i = 0; i < parentChildren.Count; i++) {
						if (parentChildren[i] is CharacterBody2D character) {
							Array<Node> characterChildren = character.GetChildren();

							for (int j = 0; j < characterChildren.Count; j++) {
								if (characterChildren[j] is HealthComponent healthComponent) {
									passThroughHC = healthComponent;
									_progressBar = (ProgressBar) healthComponent.GetProgressBar().Duplicate();
									progressBarPassThrough = healthComponent.GetProgressBar();
									AddChild(_progressBar);
								}
							}
						}
					}


				} else {
					GD.PrintErr($"HealthComponent {this}, does not have parent of type CharacterBody2D. Parent is, {GetParent().GetType()}, {GetParent().Name}");
					GD.PushError($"HealthComponent {this}, does not have parent of type CharacterBody2D. Parent is, {GetParent().GetType()}, {GetParent().Name}");
				}
			}
		} else {
			sceneClass = true;
		}
	}

	public override void _PhysicsProcess(double delta) {
		if (!sceneClass) {

			UpdateDamageNumbers((float) delta);
			UpdateCooldowns((float) delta);

			if (requirePassThrough) {
				if (_progressBar.Value != progressBarPassThrough.Value) {
					_progressBar.Value = progressBarPassThrough.Value;
				}
			}

			if (health <= 0) {
				RunDeathSequence();
			}
		}
	}

	public void TakeDamage(float amount, float cooldown, ulong source) {
		if (!sceneClass) {
			if (requirePassThrough) {
				passThroughHC.TakeDamage(amount, cooldown, source);
			} else if (amount != 0) {

				for (int i = 0; i < activeCooldowns.Count; i++) {
					if (activeCooldowns[i].DamageSource == source) {
						return;
					}
				}

				health -= amount;

				if (activeCooldowns.Count < 30) {
					activeCooldowns.Add(new DamageCooldown(source, cooldown));
				}

				_progressBar.Value = health;
				CreateDamageNumber(amount);
			}
		}
	}

	public void UpdateCooldowns(float delta) {
		if (!sceneClass) {
			if (requirePassThrough) {
				passThroughHC.UpdateCooldowns(delta);
			} else {
				for (int i = 0; i < activeCooldowns.Count; i++) {
					activeCooldowns[i] = new DamageCooldown(activeCooldowns[i].DamageSource, activeCooldowns[i].Timer - delta);
					if (activeCooldowns[i].Timer <= 0) {
						activeCooldowns.RemoveAt(i);
					}
				}
			}	
		}		
	}

	/// <summary>
	/// Will kill the parent object.
	/// </summary>
	public void RunDeathSequence() {
		if (!sceneClass) {
			if (requirePassThrough) {
				passThroughHC.RunDeathSequence();
			} else {
				if (character.IsInGroup("MagneticCharacter")) {
					
				}
				if (!hasDied) {
					RigidBody2D body = CreateBodyCopy();
					body.ApplyTorqueImpulse(1000);
					hasDied = true;
				}
			}
		}
	}

	/// <summary>
	/// Creates a RigidBody2D copy of the character and replaces character with copy as a ragdoll.
	/// Doesn't work if parent is not CharacterBody2D
	/// </summary>
	public RigidBody2D CreateBodyCopy() {
		if (character != null) {
			RigidBody2D bodyCopy = new();
			bodyCopy.Name = $"BodyCopy";		

			Node characterParent = character.GetParent();

			bodyCopy.CollisionLayer = 1u << 2;
			bodyCopy.CollisionMask = (1u << 0) | (1u << 1) | (1u << 2) | (1u << 4) | (1u << 5) | (1u << 6) | (1u << 7);


			foreach (Node child in character.GetChildren()) {
				if (!child.IsInGroup("MagneticComponent")) {
					if (!child.IsInGroup("NoRagdollInclusion")) {
						if (child is Sprite2D || child is CollisionShape2D || child is CollisionObject2D || child is Camera2D) {
							if (child is CollisionObject2D colObj) {

								Area2D newObj = null;
								foreach (Node2D objChild in colObj.GetChildren()) {
									if (!objChild.IsInGroup("NoRagdollInclusion")) {
										if (objChild is Sprite2D || objChild is CollisionShape2D || objChild is CollisionObject2D || objChild is Camera2D) {
											if (newObj == null) {
												newObj = new();
											}
											newObj.Rotation = colObj.Rotation;
											newObj.Position = colObj.Position;
											newObj.AddChild(objChild.Duplicate());
										}
									}
								}
								if (newObj != null) {
									bodyCopy.AddChild(newObj);
								}
							} else {
								bodyCopy.AddChild(child.Duplicate());
							}
						}
					}
				}
			}
			bodyCopy.GlobalPosition = character.GlobalPosition;

			characterParent.AddChild(bodyCopy);
			characterParent.RemoveChild(character);
			bodyCopy.Inertia = 100;
			return bodyCopy;
		} else {
			return null;
		}
	}

	/// <summary>
	/// Given a sprite it will create a new RigidBody2D containing that sprite.
	/// </summary>
	/// <param name="originalBody">The body that the sprite originally came from.</param>
	/// <param name="sprite">The Sprite2D to base the RigidBody2D on.</param>
	/// <param name="collision">The CollisionShape2D to put on the new RigidBody2D</param>
	/// <returns></returns>
	public RigidBody2D CreateBodyOfSprite(RigidBody2D originalBody, Sprite2D sprite, CollisionShape2D collision) {
		RigidBody2D body = new();
		Sprite2D duplicateSprite;

		body.CollisionLayer = originalBody.CollisionLayer;
		body.CollisionMask = originalBody.CollisionMask;

		duplicateSprite = (Sprite2D) sprite.Duplicate();
		duplicateSprite.Name = $"{sprite.Name}";
		duplicateSprite.Position = body.Position + sprite.Position;
		duplicateSprite.Rotation = body.Rotation + sprite.Rotation;

		body.Name = $"{sprite.Name}Object";

		body.Position = sprite.Position;

		CollisionShape2D newCollision = (CollisionShape2D) collision.Duplicate();
		newCollision.Position = sprite.Position;
		body.AddChild(newCollision);
		body.AddChild(duplicateSprite);

		return body;
	}

	/// <summary>
	/// Generates a damage number that appears above the enemy that was hurt
	/// </summary>
	/// <param name="damageAmount">Amount of damage to take off of health.</param>
	public void CreateDamageNumber(float damageAmount) {
		if (requirePassThrough) {
			passThroughHC.CreateDamageNumber(damageAmount);
		} else {
			Label damageNumber = new();
			damageNumber.Text = ((int)damageAmount).ToString();

			damageNumber.SetPosition(new Vector2((_progressBar.GetRect().Size.X / 2) - (damageNumber.GetMinimumSize().X / 2), _progressBar.GetRect().Size.Y - 30));
			AddChild(damageNumber);
			
			activeDamageNumbers.Enqueue(new DamageNumber(damageNumber.GetInstanceId(), 3));
		}
	}

	public void UpdateDamageNumbers(float delta) {
		if (!sceneClass) {
			if (requirePassThrough) {
				passThroughHC.UpdateDamageNumbers(delta);
			} else {
				if (activeDamageNumbers.Count > 0) {
					if (activeDamageNumbers.Peek().Timer <= 0) {
						RemoveChild((Label) InstanceFromId(activeDamageNumbers.Dequeue().LabelID));
					}

					Queue<DamageNumber> queue = activeDamageNumbers;

					for (int i = 0; i < activeDamageNumbers.Count; i++) {

						DamageNumber damageNumber = queue.Dequeue();

						Label label = (Label) InstanceFromId(damageNumber.LabelID);
						label.SetPosition(new Vector2(label.Position.X, label.Position.Y - delta * 30f));

						damageNumber.Timer = damageNumber.Timer - delta;

						queue.Enqueue(damageNumber);
					}
					activeDamageNumbers = queue;
				}
			}
		}
	}

	public ProgressBar GetProgressBar() {
		return _progressBar;
	}
}

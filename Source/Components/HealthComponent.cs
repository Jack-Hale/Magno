using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

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
	float maxHealth = 100;

	float health;

	ProgressBar _progressBar;
	ProgressBar progressBarPassThrough;

	CharacterBody2D character;
	bool requirePassThrough = false;
	HealthComponent passThroughHC;
	bool sceneClass = false;

	List<DamageCooldown> activeCooldowns = new();
	Queue<DamageNumber> activeDamageNumbers = new Queue<DamageNumber>(20);

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		if (GetChildCount() == 0) {
			if (GetParent() is CharacterBody2D parent) {
				PackedScene scene = (PackedScene)GD.Load("res://Scenes/Components/health_component.tscn");
				Node healthComponent = scene.Instantiate();
				Array<Node> children = healthComponent.GetChildren();

				for (int i = 0; i < children.Count; i++) {
					// children[i].Owner = null;
					// healthComponent.RemoveChild(children[i]);
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
				_progressBar.Value = health;

				for (int i = 0; i < activeCooldowns.Count; i++) {
					if (activeCooldowns[i].DamageSource == source) {
						return;
					}
				}

				health -= amount;

				if (activeCooldowns.Count < 30) {
					activeCooldowns.Add(new DamageCooldown(source, cooldown));
				}

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

	public void RunDeathSequence() {
		if (!sceneClass) {
			if (requirePassThrough) {
				passThroughHC.RunDeathSequence();
			} else {
				if (character.IsInGroup("MagneticCharacter")) {
					
				}
				character.GlobalPosition = Vector2.Inf;
			}
		}
	}

	// Generates a damage number that appears above the enemy that was hurt
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

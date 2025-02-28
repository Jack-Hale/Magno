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

// [Tool]
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

	List<DamageCooldown> activeCooldowns = new List<DamageCooldown>();

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

			UpdateCooldowns((float) delta);

			if (requirePassThrough) {
				if (_progressBar.Value != progressBarPassThrough.Value) {
					_progressBar.Value = progressBarPassThrough.Value;
				}
			}

			if (health <= 0 && !requirePassThrough) {
				RunDeathSequence();
			}
		}
	}

	public void TakeDamage(float amount, float cooldown, ulong source) {
		if (!sceneClass) {
			if (requirePassThrough) {
				passThroughHC.TakeDamage(amount, cooldown, source);
			} else {
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
				character.GlobalPosition = Vector2.Inf;
			}
		}
	}

	public ProgressBar GetProgressBar() {
		return _progressBar;
	}
}

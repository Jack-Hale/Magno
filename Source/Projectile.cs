using Godot;
using Godot.Collections;
using System;

public partial class Projectile : CharacterBody2D {
	public float speed;
	private float direction;
	private float damage;
	private float waitTime;
	private Timer _life;
	private Area2D _area2D;
	private Sprite2D _defaultSprite2D;
	private CollisionShape2D _defaultCollisionShape2D;
	private DamageComponent _damageComponent;
	private bool needsReady = false;
	private bool hasReady = false;
    public override void _Ready() {
		hasReady = true;
		_life = GetNode<Timer>("Life");
		_life.Connect("timeout", new Callable(this, MethodName.OnLifeTimeout));
		_area2D = GetNode<Area2D>("Area2D");
		_area2D.Connect("body_entered", new Callable(this, MethodName.OnBodyEntered));
		_defaultSprite2D = GetNode<Sprite2D>("DefaultSprite2D");
		_defaultCollisionShape2D = GetNode<CollisionShape2D>("DefaultCollisionShape2D");

		_damageComponent = _area2D.GetNode<DamageComponent>("DamageComponent");

		bool replaceSprite = false;
		bool replaceCollider = false;
		
		if (needsReady) {
			_damageComponent.SetDamage(damage);
			_life.WaitTime = waitTime;
		}

		// Testing if there is a sprite and collider to use instead of the defaults
		Array<Node> children = GetChildren();
		for (int i = 0; i < children.Count; i++) {
			if (children[i] is Sprite2D sprite && sprite != _defaultSprite2D) {
				replaceSprite = true;
			}

			if (children[i] is CollisionShape2D collision && collision != _defaultCollisionShape2D) {
				replaceCollider = true;
			}
		}

		if (!replaceSprite) {
			_defaultSprite2D.Visible = true;
		}
		
		if (!replaceCollider) {
			_defaultCollisionShape2D.Visible = true;
		}
    }

    public override void _PhysicsProcess(double delta) {
        Velocity = new Vector2(speed, 0).Rotated(direction);
		MoveAndSlide();
    }

	public void SetToTemplate() {
		Visible = false;
		ProcessMode = ProcessModeEnum.Disabled;
	}

	public void OnLifeTimeout() {
		QueueFree();
	}

	public void OnBodyEntered(Node2D body) {
		QueueFree();
	}

	public void SetVariables(float speed, float direction, Vector2 spawnPos, float spawnRot, float damage, float projectileTimeout) {
		this.speed = speed;
		this.direction = direction;
		GlobalPosition = spawnPos;
		GlobalRotation = spawnRot;

		// Need to have different code run for if Ready has or hasn't been run 
		// since it is potential for both conditions to exist
		if (hasReady) {
			_life.WaitTime = projectileTimeout;
			_damageComponent.SetDamage(damage);
		} else {
			this.damage = damage;
			waitTime = projectileTimeout;
			needsReady = true;
		}
	}
}

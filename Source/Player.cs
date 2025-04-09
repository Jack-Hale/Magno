using Godot;
using Godot.Collections;
using System;
using System.ComponentModel;
using System.IO.IsolatedStorage;
using System.Linq;

public partial class Player : CharacterBody2D
{
	[Export]
	public float maxSpeed = 400;
	[Export]
	public float jumpVelocity = -180f;
	[Export]
	public float jumpHoldTime = 0.15f;
	[Export]
	public float friction = 2200f;
	[Export]
	public float airFriction = 1f;

	[Export]
	public float acceleration = 2200f;
	[Export]
	public float airAcceleration = 1800f;
	[Export]
	public float pushForce = 80f;

	public float currentJumpVelocity = 0f;
	public bool jumping = false;
	public float currentJumpTimer = 0f;
	public float jumpScalar = 1f;

	private const float coyoteTimerMax = 0.10f;
	private float coyoteTimer = 0f;

	private const float jumpBufferTimerMax = 0.05f;
	private float jumpBufferTimer = 0f;
	Vector2 Input = Vector2.Zero;

	private Magnet _magnet;
	private CollisionShape2D _collisionShape;
	private Vector2 drawVector1 = Vector2.Zero;
	private Vector2 drawVector2 = Vector2.Zero;

	private bool godMode = false;

	private MagneticComponent attachedObject;

	private bool pullMode;

	private bool wasOnFloor;

	private AnimationPlayer _animationPlayer;
	private Sprite2D _sprite2D;
	private Label _label;

	private Vector2 stickAimVector = Vector2.Zero;

	private bool mnkControl = true;

	private bool jumpAnimation = false;
	private Vector2 preFloorVelocity = Vector2.Zero;

	private RigidBody2D heldItem = null;
	private PhysicsBody2D heldObject = null;
	private ItemComponent itemComponent = null;

	private Vector2 force = Vector2.Zero;
	private float preFloorTimer = 0f;
	private float preFloorTimerMax = 0.3f;
	private bool isOnFloor = false;
	private bool isAnyOnFloor = false;
	private CapsuleShape2D capsuleCollision;
	PhysicsShapeQueryParameters2D query = new PhysicsShapeQueryParameters2D();


	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	public override void _Ready() {
		_magnet = GetNode<Magnet>("Magnet");
		_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		_sprite2D = GetNode<Sprite2D>("Sprite2D");
		_label = GetNode<Label>("Label");
		_label.AddToGroup("NoRagdollInclusion");
		_collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");

		if (_collisionShape.Shape is CapsuleShape2D capsuleShape) {
			capsuleCollision = capsuleShape;
		}

		pullMode = _magnet.GetPullMode();

		query.SetShape(capsuleCollision);
		query.Exclude.Add(GetRid());
		query.CollisionMask = (1u << 0) | (1u << 2) | (1u << 4) | (1u << 6) | (1u << 7);
	}

	public override void _Draw() {
        // DrawLine(drawVector1, drawVector2, Colors.Green, 1.0f);
    }

    public override void _Process(double delta) {
		// Activates Coyote timer if the player walks off an edge without jumping
		if (wasOnFloor && !isOnFloor && !jumping) {
			coyoteTimer = coyoteTimerMax;
		}
		if (coyoteTimer > 0) {
			coyoteTimer -= (float)delta;
		}

		if (jumpBufferTimer > 0) {
			jumpBufferTimer -= (float)delta;
		}


		if (preFloorTimer > 0) {
			preFloorTimer -= (float)delta;
		}

		// Gets the velocity from the frame before the player hit the ground
		if (!IsOnFloor()) {
			preFloorVelocity = Velocity;
			preFloorTimer = preFloorTimerMax;
		} else if (preFloorTimer <= 0) {
			// If player has been on ground for 0.3 seconds, set to Vector.Zero
			preFloorVelocity = Vector2.Zero;
		}

        wasOnFloor = isOnFloor;
    }

    public override void _PhysicsProcess(double delta) {

		if (pullMode) {
			_label.Text = "Pull";
		} else {
			_label.Text = "Push";
		}

		Vector2 NewVelocity = Velocity;

		HandleMagnet();

		if (Godot.Input.IsActionJustPressed("ToggleGodmode")) {
			godMode = !godMode;
		}


		// Flipping the sprite to face the way its moving
		if (Velocity.X != 0) {
			_sprite2D.FlipH = Velocity.X < 0;
		}

		if (_magnet.HasObject()) {
			if (_magnet.HasItem()) {
				RigidBody2D item = _magnet.GetItem();
				if (heldItem != item) {
					heldItem = item;
					itemComponent = heldItem.GetNode<ItemComponent>("ItemComponent");
				}
			} else {
				PhysicsBody2D item = _magnet.GetAttachedObject();
				if (heldObject != item) {
					heldObject = item;
					itemComponent = null;
				}
			}
		} else {
			if (heldItem != null) {
				heldItem = null;
				itemComponent = null;
			}
		}

		UpdateAnimations();

		Vector2 newAimVector = Godot.Input.GetVector("AimLeft", "AimRight", "AimUp", "AimDown");

		// Switches the aim to controller if new input is detected from the right thumbstick
		if (newAimVector != stickAimVector && newAimVector != Vector2.Zero) {
			mnkControl = false;
		}

		if (newAimVector != Vector2.Zero) {
			stickAimVector = newAimVector;
		}

		// Handles rotating the magnet to whatever input in active
		if (mnkControl) {
			Vector2 direction = (GetGlobalMousePosition() - _magnet.GlobalPosition).Normalized();
			float turnSpeed = 15f;
			
			// Will lower the turn speeed greatly if the item is going to collide with a surface
			if (heldItem != null || heldObject != null) {
				if (PhysicsTestCollision(_magnet.GetHeldObjectCollisions(), direction, 20f)) {
					turnSpeed = 1f;
				}
			}
			float targetRotation = direction.Angle();
			_magnet.Rotation = Mathf.LerpAngle(_magnet.Rotation, targetRotation, turnSpeed * (float)GetPhysicsProcessDeltaTime());
			
			// _magnet.LookAt(GetGlobalMousePosition());
		} else {
			_magnet.Rotation = stickAimVector.Angle();
		}
	
		if (Godot.Input.IsActionJustPressed("UseItemLeft")) {
			if (itemComponent != null) {
				itemComponent.UseItemLeft();
			}
		}

		if (Godot.Input.IsActionJustPressed("UseItemRight")) {
			if (itemComponent != null) {
				itemComponent.UseItemRight();
			}
		}

		if (Godot.Input.IsActionJustPressed("DropItem")) {
			_magnet.DropItem();
		}

		if (Godot.Input.IsActionJustPressed("ToggleMagnetMode")) {
			pullMode = !pullMode;
			_magnet.SetPullMode(pullMode);
		}

		if (!godMode) {
			// Add the gravity.
			if (!isOnFloor)
				NewVelocity.Y += gravity * (float)delta;

			NewVelocity.Y += HandleJump(delta);

			NewVelocity.X = MovePlayer(delta);
		} else {
			NewVelocity = GodmodeMove(delta);
		}
		if (force != Vector2.Zero) {
			NewVelocity += force;
			force = Vector2.Zero;
		}

		Velocity = NewVelocity;

		QueueRedraw();
		MoveAndSlide();

		// Replacing IsOnFloor call with a separate query that only checks the players collision
		Vector2 queryPosition = GlobalPosition + new Vector2(0, 1);
		query.Transform = new Transform2D(0, queryPosition);

		PhysicsDirectSpaceState2D spaceState = GetWorld2D().DirectSpaceState;
		var collisions = spaceState.IntersectShape(query);

		isOnFloor = false;
		if (collisions.Count > 0) {
			isOnFloor = true;
		}

		// Will be true if any part of the player is on floor
		isAnyOnFloor = isOnFloor || IsOnFloor();

		// Push RigidBody2D objects
		for (int i = 0; i < GetSlideCollisionCount(); i++) {
			KinematicCollision2D collision = GetSlideCollision(i);
			if (collision.GetCollider() is RigidBody2D) {
				RigidBody2D c = (RigidBody2D) collision.GetCollider();
				c.ApplyCentralImpulse(-collision.GetNormal() * pushForce);
			}
		}
	}

	// Tests if there is a collision with tilemaps where the magnet is rotating to
	private bool PhysicsTestCollision(Array<CollisionShape2D> collisions, Vector2 direction, float checkDistance) {
		var spaceState = GetWorld2D().DirectSpaceState;
		foreach (CollisionShape2D shape in collisions) {
			PhysicsShapeQueryParameters2D query = new PhysicsShapeQueryParameters2D();
			query.SetShape(shape.Shape);
			
			// Move the query forward in the direction the magnet is rotating
			Vector2 newPos = shape.GlobalPosition + (direction * checkDistance);
			
			query.Transform = new Transform2D(0, newPos);
			query.CollisionMask = (1u << 0) | (1u << 7);

			if (spaceState.IntersectShape(query).Count > 0)
				return true;
		}

		return false;
	}

    public override void _Input(InputEvent @event) {
		// If any mouse movement is detected, switch the aim control to mouse
        if (@event is InputEventMouseMotion) {
			mnkControl = true;
		}
    }

    public Vector2 GetXInput() {
		// Only X input is read because jump is handled separately
		Vector2 InputX = Input;
		InputX.X = Godot.Input.GetVector("MoveLeft", "MoveRight", "MoveUp", "MoveDown").X;
		return InputX.Normalized();
	}

	public Vector2 GetInput() {
		// Only X input is read because jump is handled separately
		Vector2 input = Input;
		input = Godot.Input.GetVector("MoveLeft", "MoveRight", "MoveUp", "MoveDown");
		return input.Normalized();
	}

	public void HandleMagnet() {
		_magnet.SetActivation(Godot.Input.IsActionPressed("ActivateWeakMagnet"), Godot.Input.IsActionPressed("ActivateStrongMagnet"));
	}

	public float HandleJump(double delta) {

		// Activates the jump buffer timer if jump is pressed not on the floor
		if (Godot.Input.IsActionJustPressed("Jump") && !isOnFloor) {
			jumpBufferTimer = jumpBufferTimerMax;
		}

		// Jump pressed while on the floor or the coyote timer is active, set jump velocity to max
		// Will jump when jump key is not pressed if the jump buffer is active
		if ((isOnFloor && jumpBufferTimer > 0) || Godot.Input.IsActionJustPressed("Jump") && (isOnFloor || coyoteTimer > 0)) {
			currentJumpVelocity = jumpVelocity;
			currentJumpTimer = jumpHoldTime;
			jumping = true;
		}

		// Jump is held down, decrease the timer
		if (Godot.Input.IsActionPressed("Jump") && jumping) {
			currentJumpTimer -= 1f * (float)delta;
			currentJumpVelocity += 140 * currentJumpTimer;
		}

		// Jump was released or timer ran out, stop jump sequence
		if (Godot.Input.IsActionJustReleased("Jump") || currentJumpTimer <= 0.0f) {
			jumping = false;
			currentJumpVelocity = 0;
		}

		if (Godot.Input.IsActionJustPressed("Jump") && isOnFloor) return currentJumpVelocity - 60f;
		else return currentJumpVelocity;
	}

	public void ApplyForce(Vector2 force) {
		this.force = force;
	}

	public float GetMagnetRotation() {
		return _magnet.Rotation;
	}

	public bool GetIsOnFloor() {
		return isOnFloor;
	}

	public bool GetIsAnyOnFloor() {
		return isAnyOnFloor;
	}

	public Vector2 GetPreFloorVelocity() {
		return preFloorVelocity;
	}

	public float MovePlayer(double delta) {
		Input = GetXInput();

		// Needs to be only on X otherwise LimitLength takes falling and jumping into account affecting speed
		float currentX = Velocity.X;

		float decelerationRate = 5f;
		
		// No Input
		if (Input == Vector2.Zero) {
			float frictionAmount = (isAnyOnFloor ? friction : airFriction) * (float)delta;

			if (Math.Abs(currentX) > frictionAmount) {
				currentX -= Mathf.Sign(currentX) * frictionAmount;
			} else {
				currentX = 0;
			}
		}
		// Input, Add acceleration
		else if (!Godot.Input.IsActionPressed("MoveDown") || isOnFloor) {
			// newVelocity += Input * (isOnFloor ? acceleration : airAcceleration) * (float)delta;
			// newVelocity = new Vector2(newVelocity.LimitLength(maxSpeed).X, 0);


			float accel = (isOnFloor ? acceleration : airAcceleration) * (float)delta;
        
			// Above max speed and the input is trying to accelerate further in the same direction
			if (Math.Abs(currentX) > maxSpeed && Mathf.Sign(Input.X) == Mathf.Sign(currentX)) {

				// Gradually reduce speed toward maxSpeed using Lerp
				currentX = Mathf.Lerp(currentX, Mathf.Sign(currentX) * maxSpeed, decelerationRate * (float)delta);

			} else {
				// Otherwise, apply normal acceleration
				currentX += Input.X * accel;
				
				// Reduce speed if currentX exceeds maxSpeed after acceleration is applied
				if (Math.Abs(currentX) > maxSpeed && Mathf.Sign(Input.X) == Mathf.Sign(currentX)) {
					currentX = Mathf.Lerp(currentX, Mathf.Sign(currentX) * maxSpeed, decelerationRate * (float)delta);
				}
			}
		}

		return currentX;
	}

	public Vector2 GodmodeMove(double delta) {
		Input = GetInput();
		
		return Input * maxSpeed*2;
	}

	public void UpdateAnimations() {
        string run = "run";

		float angle = (_magnet.Rotation % (2 * Mathf.Pi) + (2 * Mathf.Pi)) % (2 * Mathf.Pi);
		bool angleCheck = angle >= (3 * Mathf.Pi / 2) || angle <= (Mathf.Pi / 2);
		if (Velocity.X > 0) {
			if (!angleCheck) {
				run = "run_backwards";
			}
		} else if (Velocity.X < 0) {
			if (angleCheck) {
				run = "run_backwards";
			}
		}

		if (run == "run_backwards") {
			_sprite2D.FlipH = !_sprite2D.FlipH;
		}
		
		if (isOnFloor)  {
			jumpAnimation = true;
			if (Velocity.X == 0) {
				_animationPlayer.Play("idle");
			}
			else if (Mathf.Abs(Velocity.X) > maxSpeed) {
				_animationPlayer.Play(run);
			}
			else {
				if (Input.X > 0 && Velocity.X < 0 || Input.X < 0 && Velocity.X > 0) {
					_animationPlayer.Play(run);
				}
				else {
					_animationPlayer.Play(run);
				}
			}
		}
		else if (jumpAnimation) {
			_animationPlayer.Play("jump");
			jumpAnimation = false;
		}
	}
}



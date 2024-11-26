using Godot;
using System;
using System.ComponentModel;
using System.Linq;

public partial class Player : CharacterBody2D
{
	[Export]
	public float MAX_SPEED = 400;
	[Export]
	public float JUMP_VELOCITY = -180f;
	[Export]
	public float JUMP_HOLD_TIME = 0.1f;
	[Export]
	public float FRICTION = 2200f;
	[Export]
	public float AIR_FRICTION = 1f;

	[Export]
	public float ACCELERATION = 2200f;
	[Export]
	public float AIR_ACCELERATION = 1800f;
	[Export]
	public float PUSH_FORCE = 80f;

	public float CurrentJumpVelocity = 0f;
	public bool Jumping = false;
	public float CurrentJumpTimer = 0f;

	private const float coyoteTimerMax = 0.10f;
	private float coyoteTimer = 0f;

	private const float jumpBufferTimerMax = 0.05f;
	private float jumpBufferTimer = 0f;
	Vector2 Input = Vector2.Zero;

	private Magnet _magnet;

	private Vector2 DrawVector1 = Vector2.Zero;
	private Vector2 DrawVector2 = Vector2.Zero;

	private bool Godmode = false;

	private Magnet magnet;

	private MagneticComponent attachedObject;

	private bool pullMode;

	private bool wasOnFloor;

	private AnimationPlayer _animationPlayer;
	private Sprite2D _sprite2D;
	private Label _label;

	private Vector2 stickAimVector = Vector2.Zero;

	private bool mnkControl = true;


	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	public override void _Ready() {
		
		_magnet = GetNode<Magnet>("Magnet");
		_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		_sprite2D = GetNode<Sprite2D>("Sprite2D");
		_label = GetNode<Label>("Label");

		pullMode = _magnet.GetPullMode();
	}

	public override void _Draw()
    {
        DrawLine(DrawVector1, DrawVector2, Colors.Green, 1.0f);
    }

    public override void _Process(double delta)
    {
		// Activates Coyote timer if the player walks off an edge without jumping
		if (wasOnFloor && !IsOnFloor() && !Jumping) {
			coyoteTimer = coyoteTimerMax;
		}

		if (coyoteTimer > 0) {
			coyoteTimer -= (float)delta;
		}

		if (jumpBufferTimer > 0) {
			jumpBufferTimer -= (float)delta;
		}

        wasOnFloor = IsOnFloor();
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
			Godmode = !Godmode;

			// foreach (var item in GetParent().GetChildren())
			// {
			// 	if (item is RigidBody2D body) {
			// 		body.AngularVelocity = 0;
			// 		body.LinearVelocity = Vector2.Zero;
			// 	}
			// }

		}

		// Flipping the sprite to face the way its moving
		if (Velocity.X != 0) 
		{
			_sprite2D.FlipH = Velocity.X < 0;
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
			_magnet.LookAt(GetGlobalMousePosition());
		} else {
			_magnet.Rotation = stickAimVector.Angle();
		}
	
		if (Godot.Input.IsActionJustPressed("ToggleMagnetMode")) {
			pullMode = !pullMode;
			_magnet.SetPullMode(pullMode);
		}

		if (!Godmode) {
			// Add the gravity.
			if (!IsOnFloor())
				NewVelocity.Y += gravity * (float)delta;

			NewVelocity.Y += HandleJump(delta);

			NewVelocity.X = MovePlayer(delta);
		} else {
			NewVelocity = GodmodeMove(delta);
		}

		Velocity = NewVelocity;

		QueueRedraw();
		MoveAndSlide();

		// Push RigidBody2D objects
		for (int i = 0; i < GetSlideCollisionCount(); i++) {
			KinematicCollision2D collision = GetSlideCollision(i);
			if (collision.GetCollider() is RigidBody2D) {
				RigidBody2D c = (RigidBody2D) collision.GetCollider();
				c.ApplyCentralImpulse(-collision.GetNormal() * PUSH_FORCE);
			}
		}
	}

    public override void _Input(InputEvent @event)
    {
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
		Vector2 Input = this.Input;
		Input = Godot.Input.GetVector("MoveLeft", "MoveRight", "MoveUp", "MoveDown");
		return Input.Normalized();
	}

	public void HandleMagnet() {
		_magnet.SetActivation(Godot.Input.IsActionPressed("ActivateWeakMagnet"), Godot.Input.IsActionPressed("ActivateStrongMagnet"));
	}

	public float HandleJump(double delta) {

		// Activates the jump buffer timer if jump is pressed not on the floor
		if (Godot.Input.IsActionJustPressed("Jump") && !IsOnFloor()) {
			jumpBufferTimer = jumpBufferTimerMax;
		}

		// Jump pressed while on the floor or the coyote timer is active, set jump velocity to max
		// Will jump when jump key is not pressed if the jump buffer is active
		if ((IsOnFloor() && jumpBufferTimer > 0) || Godot.Input.IsActionJustPressed("Jump") && (IsOnFloor() || coyoteTimer > 0)) {
			CurrentJumpVelocity = JUMP_VELOCITY;
			CurrentJumpTimer = JUMP_HOLD_TIME;
			Jumping = true;
		}

		// Jump is held down, decrease the timer
		if (Godot.Input.IsActionPressed("Jump") && Jumping) {
			CurrentJumpTimer -= 1.0f * (float)delta;
			CurrentJumpVelocity += 200.0f * CurrentJumpTimer * (float)delta;
		} 

		// Jump was released or timer ran out, stop jump sequence
		if (!Godot.Input.IsActionPressed("Jump") || CurrentJumpTimer <= 0.0f) {
			Jumping = false;
			CurrentJumpVelocity = 0;
		}

		return CurrentJumpVelocity;
	}

	public float MovePlayer(double delta) {
		Input = GetXInput();
		Vector2 NewVelocity = Vector2.Zero;

		// Needs to be only on X otherwise LimitLength takes falling and jumping into account affecting speed
		NewVelocity.X = Velocity.X;
		
		// No Input
		if (Input == Vector2.Zero)
		{
			// Player is moving, Apply friction to reduce speed
			if (Math.Abs(NewVelocity.X) > (FRICTION * (float)delta))
			{
				// Friction is set based on land or air
				NewVelocity -= NewVelocity.Normalized() * (IsOnFloor() ? FRICTION : AIR_FRICTION) * (float)delta;
			}

			// Player is not moving
			else
			{
				NewVelocity = Vector2.Zero;
			}
		}
		// Input, Add acceleration
		else if (!Godot.Input.IsActionPressed("MoveDown") || IsOnFloor())
		{
			NewVelocity += Input * (IsOnFloor() ? ACCELERATION : AIR_ACCELERATION) * (float)delta;
			NewVelocity = NewVelocity.LimitLength(MAX_SPEED);
		}

		return NewVelocity.X;
	}

	public Vector2 GodmodeMove(double delta) {
		Input = GetInput();
		
		return Input * MAX_SPEED*2;
	}

	public void UpdateAnimations() {
		if (IsOnFloor())  {
			if (Velocity.X == 0) {
				_animationPlayer.Play("idle");
			}
			else if (Mathf.Abs(Velocity.X) > MAX_SPEED) {
				_animationPlayer.Play("run");
			}
			else {
				if (Input.X > 0 && Velocity.X < 0 || Input.X < 0 && Velocity.X > 0) {
					_animationPlayer.Play("run");
				}
				else {
					_animationPlayer.Play("run");
				}
			}
		}
		else {				
			_animationPlayer.Play("jump");
		}
	}
}



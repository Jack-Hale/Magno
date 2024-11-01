using Godot;
using System;
using System.Linq;

public partial class Player : CharacterBody2D
{
	[Export]
	public float MAX_SPEED = 400;
	[Export]
	public float JUMP_VELOCITY = -140.0f;
	[Export]
	public float JUMP_HOLD_TIME = 0.2f;	

	[Export]
	public float FRICTION = 2200.0f;
	[Export]
	public float AIR_FRICTION = 1.0f;

	[Export]
	public float ACCELERATION = 2200.0f;
	[Export]
	public float AIR_ACCELERATION = 1800.0f;
	[Export]
	public float PushForce = 80.0f;

	public float CurrentJumpVelocity = 0.0f;
	public bool Jumping = false;
	public float CurrentJumpTimer = 0.0f;
	Vector2 Input = Vector2.Zero;

	private Area2D _magnet;
	private Area2D _magnetA;

	private Vector2 DrawVector1 = Vector2.Zero;
	private Vector2 DrawVector2 = Vector2.Zero;

	private bool Godmode = false;

	private Magnet magnet;
	private MagnetA magnetA;
	private bool magnetMode = true;

	private MagneticComponent attachedObject;

	private bool pullMode;


	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	public override void _Ready() {
		
		_magnet = GetNode<Area2D>("Magnet");
		magnet = (Magnet) _magnet;

		_magnetA = GetNode<Area2D>("MagnetA");
		magnetA = (MagnetA) _magnetA;

		pullMode = magnet.GetPullMode();
	}

	public override void _Draw()
    {
        DrawLine(DrawVector1, DrawVector2, Colors.Green, 1.0f);
    }

	public override void _PhysicsProcess(double delta) {
		Vector2 NewVelocity = Velocity;

		_magnet.LookAt(GetGlobalMousePosition());
		_magnetA.LookAt(GetGlobalMousePosition());

		HandleMagnet();

		if (Godot.Input.IsActionJustPressed("ToggleGodmode")) {
			Godmode = !Godmode;
		}

		if (Godot.Input.IsActionJustPressed("SwitchMagnet")) {
			magnetMode = !magnetMode;
		}

		if (magnetMode) {
			_magnetA.ProcessMode = ProcessModeEnum.Disabled;
			_magnetA.Visible = false;

			_magnet.ProcessMode = ProcessModeEnum.Inherit;
			_magnet.Visible = true;
		} else {
			_magnet.ProcessMode = ProcessModeEnum.Disabled;
			_magnet.Visible = false;

			_magnetA.ProcessMode = ProcessModeEnum.Inherit;
			_magnetA.Visible = true;
		}

		if (Godot.Input.IsActionJustPressed("ToggleMagnetMode")) {
			pullMode = !pullMode;
			magnet.SetPullMode(pullMode);
			magnetA.SetPullMode(pullMode);
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
				c.ApplyCentralImpulse(-collision.GetNormal() * PushForce);
			}
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
		magnet.SetActivation(Godot.Input.IsActionPressed("ActivateWeakMagnet"), Godot.Input.IsActionPressed("ActivateStrongMagnet"));
		magnetA.SetActivation(Godot.Input.IsActionPressed("ActivateWeakMagnet"), Godot.Input.IsActionPressed("ActivateStrongMagnet"));
	}

	public float HandleJump(double delta) {

		// Jump pressed while on the floor, set jump velocity to max
		if (Godot.Input.IsActionJustPressed("Jump") && IsOnFloor()) {
			CurrentJumpVelocity = JUMP_VELOCITY;
			CurrentJumpTimer = JUMP_HOLD_TIME;
			Jumping = true;
		}

		// Jump is held down, decrease the timer
		if (Godot.Input.IsActionPressed("Jump") && Jumping) {
			CurrentJumpTimer -= 1.0f * (float)delta;
			CurrentJumpVelocity += 200.0f * (float)delta;
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
}

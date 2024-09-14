using Godot;
using System;

public partial class Player : CharacterBody2D
{
	public const float MAX_SPEED = 400.0f;
	public const float JUMP_VELOCITY = -140.0f;
	public const float JUMP_HOLD_TIME = 0.2f;	

	public const float FRICTION = 2200.0f;
	public const float AIR_FRICTION = 1.0f;

	public const float ACCELERATION = 2200.0f;
	public const float AIR_ACCELERATION = 1800.0f;

	public float CurrentJumpVelocity = 0.0f;
	public bool Jumping = false;
	public float CurrentJumpTimer = 0.0f;
	Vector2 Input = Vector2.Zero;


	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	public override void _PhysicsProcess(double delta) {
		Vector2 NewVelocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
			NewVelocity.Y += gravity * (float)delta;

		GD.Print(HandleJump(delta));
		NewVelocity.Y += HandleJump(delta);

		NewVelocity.X = MovePlayer(delta);

		Velocity = NewVelocity;
		MoveAndSlide();
	}

	public Vector2 GetInput() {
		// Only X input is read because jump is handled separately
		Vector2 InputX = Input;
		InputX.X = Godot.Input.GetVector("MoveLeft", "MoveRight", "MoveUp", "MoveDown").X;
		return InputX.Normalized();
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
			CurrentJumpVelocity += 100.0f * (float)delta;
		} 

		// Jump was released or timer ran out, stop jump sequence
		if (!Godot.Input.IsActionPressed("Jump") || CurrentJumpTimer <= 0.0f) {
			Jumping = false;
			CurrentJumpVelocity = 0;
		}

		return CurrentJumpVelocity;
	}

	public float MovePlayer(double delta) {
		Input = GetInput();
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
}

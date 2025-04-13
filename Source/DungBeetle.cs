using Godot;
using System;

public partial class DungBeetle : CharacterBody2D {
	public float maxSpeed = 100.0f;
	public float jumpVelocity = -800.0f;
	float friction = 2200;
	float acceleration = 2200;
	float airAcceleration = 1800;
	private Sprite2D _sprite2D;
	private PathFindingComponent _pathFinding;
	private CharacterBody2D player;
	private Bomb _bomb;
	private PinJoint2D _pinJoint;
	private Vector2 jointPosition;
	private bool bombThrown = false;
	MagneticComponent bombMag;

    public override void _Ready() {
        _pathFinding = GetNode<PathFindingComponent>("PathFindingComponent");
		_sprite2D = GetNode<Sprite2D>("Sprite2D");
		_bomb = GetNode<Bomb>("Bomb");
		_pinJoint = GetNode<PinJoint2D>("PinJoint2D");
		bombMag = _bomb.GetNode<MagneticComponent>("MagneticComponent");
		bombMag.DisableMagneticism(true);
		player = _pathFinding.GetPlayer();
		jointPosition = _pinJoint.Position;
    }



	public override void _PhysicsProcess(double delta) {
		Vector2 velocity = Velocity;
		Vector2 direction = Vector2.Zero;

		// Add the gravity.
		if (!IsOnFloor()) {
			velocity += GetGravity() * (float)delta;
		}

		if (IsInGroup("CanSeePlayer") || IsInGroup("LookingForPlayer")) {
			direction = GlobalPosition.DirectionTo(_pathFinding.GetLastDetectionPoint());
			if (bombThrown) {
				direction = new Vector2 (-direction.X, direction.Y);
			}

			float distance = _pathFinding.GetDistanceFromCollsionLayer(_pathFinding.veiwRadius, direction, 1u << 1);
			if (_bomb != null) {
				if (distance < 200 && distance != -1) {
					ThrowBomb(direction * 500);
					bombThrown = true;
				}
			}

			_sprite2D.FlipH = direction.X < 0;
		} else {

		}

		if (_bomb != null) {
			_bomb.ApplyTorque(1500 * Mathf.Sign(direction.X));
		}

		velocity.X = _pathFinding.MoveCharacter(true, velocity, direction, maxSpeed, acceleration, airAcceleration, delta).X;
		if (direction == Vector2.Zero) {
			velocity.X = _pathFinding.ApplyFriction(true, velocity, friction, delta).X;
		}

		_pinJoint.Position = new Vector2(jointPosition.X * Mathf.Sign(direction.X), jointPosition.Y);

		Velocity = velocity;
		MoveAndSlide();
	}

	public void ThrowBomb(Vector2 throwVector) {
		if (_bomb != null) {
			_pinJoint.NodeB = null;
			Vector2 globalPosition = _bomb.GlobalPosition;
			float rotation = _bomb.Rotation;
			RemoveChild(_bomb);
			GetParent().AddChild(_bomb);
			_bomb.GlobalPosition = globalPosition;
			_bomb.Rotation = rotation;
			bombMag.DisableMagneticism(false);

			_bomb.ApplyImpulse(throwVector);
			_bomb.StartTimer();
			_bomb = null;
		}
	}
}

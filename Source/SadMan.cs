using Godot;
using System;

public partial class SadMan : CharacterBody2D
{
	private float maxSpeed = 300.0f;
	private float acceleration = 1000;
	private float jumpVelocity = -400.0f;
	public float friction = 2200f;

	private bool affected = true;

	private MagneticCharacterComponent magCharComp = null;

	// The pull mode of the magnet affecting the enemy
	// Pulling = true, Pushing = false
	private bool magnetPull = false;

	// Position of the magnet affecting the enemy 
	private Vector2 magnetAttractionPoint = Vector2.Zero; 

	// Force applied to the enemy from one magnet
	private Vector2 magnetForce = Vector2.Zero;

	// Position on the enemy the force is being applied
	private Vector2 magnetForcePosition = Vector2.Zero;


	private float weakMultiplier;
	private float strongMultiplier;
	private float blastMultiplier;
	private bool canJoin;
	private SwapCondition swapCondition;

	private float swapTimeLimit;

	public bool isRigidPhysics;

	private bool ragDollOnAnyForce;

	private SwapCondition anyForceSwapCondition;
	private float anyForceSwapTimeLimit;


	private Sprite2D _Happy;
	private Sprite2D _Sad;	
	private PathFindingComponent _pathFinding;
	private RigidBody2D ball;
	private MagneticCharacterParent magneticCharacterParent = new();
	private bool lookingForBall = false;
	private Vector2 initialBallPosition;
	private bool inPickup = false;

	private float pickupCooldown = 2f;
	private float pickupCooldownTimer = 0;

	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
	public override void _Ready() {
		_pathFinding = GetNode<PathFindingComponent>("PathFindingComponent");
		_Happy = GetNode<Sprite2D>("Sprite2D");
		_Sad = GetNode<Sprite2D>("Sad");
		ball = GetNode<RigidBody2D>("Ball");
		initialBallPosition = ball.Position;

		MagneticComponent ballMagComp = GetNode<MagneticComponent>("Ball/MagneticComponent");
		ballMagComp.SetExitCondition(ExitCondition.TimeLimit);

		MagneticCharacterParent magCharPar = (MagneticCharacterParent) GetParent();

		foreach (var child in magCharPar.GetChildren()) {
			if (child is MagneticCharacterComponent) {
				magCharComp = (MagneticCharacterComponent) child;
			}
		}

		weakMultiplier = magCharPar.GetWeakMultiplier();
		strongMultiplier = magCharPar.GetStrongMultiplier();
		blastMultiplier = magCharPar.GetBlastMultiplier();
		canJoin = magCharPar.GetCanJoin();

		swapCondition = magCharComp.GetSwapCondition();
		swapTimeLimit = magCharComp.GetSwapTimeLimit();
		isRigidPhysics = magCharComp.GetIsRigidPhysics();
		ragDollOnAnyForce = magCharComp.GetRagDollOnAnyForce();
		anyForceSwapCondition = magCharComp.GetAnyForceSwapCondition();
		anyForceSwapTimeLimit = magCharComp.GetAnyForceSwapTimeLimit();
	}

	public override void _PhysicsProcess(double delta) {
		Vector2 velocity = Velocity;
		
		// Handles magnetic states
		if (IsInGroup("Magnetic")) {
			_Sad.Visible = false;
			_Happy.Visible = true;
			inPickup = false;
			lookingForBall = false;
			if (magCharComp == null) {
				GD.PrintErr($"No MagneticCharacterComponent found on magnetic character {this} {Name}");
				GD.PushError($"No MagneticCharacterComponent found on magnetic character {this} {Name}");
			}
			if (IsInGroup("Affected")) {
				affected = true;
				Tuple<bool, Vector2> magentData = magCharComp.GetBodyCopyMagnetData();
				magnetPull = magentData.Item1;
				magnetAttractionPoint = magentData.Item2;

				Tuple<Vector2, Vector2> forceData = magCharComp.GetBodyCopyForceData();
				magnetForce = forceData.Item1;
				magnetForcePosition = forceData.Item2;
			} else {
				affected = false;
				magnetPull = false;
				magnetAttractionPoint = Vector2.Zero;
			}
		} else {
			if (!lookingForBall) {
				pickupCooldownTimer = pickupCooldown;
			}
			lookingForBall = true;
			_Sad.Visible = true;
			_Happy.Visible = false;
			affected = false;
		}

		if (lookingForBall) {
			Tuple<Vector2, Vector2> search = new Tuple<Vector2, Vector2>(new Vector2(0, -72), ToLocal(ball.GlobalPosition).LimitLength(500));

			Vector2 ballPosition = _pathFinding.SearchForObject([search], "", ball);

			if (ballPosition != Vector2.Zero) {
				velocity = _pathFinding.MoveCharacter(true, velocity, ToLocal(ballPosition), maxSpeed, acceleration, 1, delta);
			}

			if (!inPickup && pickupCooldownTimer <= 0) {
				if (GlobalPosition.DistanceTo(ballPosition) < 175) {
					PickUpBall();
					inPickup = true;
					pickupCooldownTimer = pickupCooldown;
				}
			}
		}

		if (pickupCooldownTimer > 0) {
			pickupCooldownTimer -= (float) delta;
		}

		velocity = _pathFinding.ApplyFriction(true, velocity, friction, delta);

		// Add the gravity.
		if (!IsOnFloor())
			velocity.Y += gravity * (float)delta;

		if (affected) {
			// Handle behaviour when affected by a magnet
			
		} else {
			// Handle behaviour when unaffected by a magnet
			
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	public void PickUpBall() {
		if (lookingForBall) {
			MagneticParentStruct magneticParentStruct = new MagneticParentStruct(weakMultiplier, strongMultiplier, blastMultiplier, canJoin);
			MagneticComponentStruct magneticComponentStruct = new MagneticComponentStruct(swapCondition, swapTimeLimit, isRigidPhysics, ragDollOnAnyForce, anyForceSwapCondition, anyForceSwapTimeLimit);
			magneticCharacterParent.RemagnifyCharater(initialBallPosition, ball, this, magneticParentStruct, magneticComponentStruct);
		}
	}
}

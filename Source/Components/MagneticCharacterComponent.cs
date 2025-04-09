using Godot;
using Godot.Collections;
using System;
using System.IO;
using System.Security.Principal;

public enum SwapCondition {
	SwapWhenHitSurface,
	SwapWhenLetGo,
	SwapAfterTimeLimit
}

public enum ExitCondition {
	CannotExit,
	TimeLimit,
	StrongForce,
	Throw
}

public partial class MagneticCharacterComponent : Node2D {
	[Export]
	public ExitCondition exitCondition;

	[Export]
	public float exitTimer = 2;

	[Export]
	public SwapCondition swapCondition;

	[Export]
	public float swapTimeLimit = 0.5f;

	[Export]
	public bool isRigidPhysics = true;

	private Node2D parent;
	private MagneticCharacterParent magCharPar;
	private CharacterBody2D character;

	private RigidBody2D bodyCopy;

	public bool isCharacter = true;
	private uint collisionL;
	private uint collisionM;
	private MagneticComponent bodyCopyMagComp;

	private float ragdollTimer = 0;
	private bool ragdoll = false;

	// Variables for calculating swap rotation
	private float lastAngularVelocity = 0f;
	private float rotationDuration = 1.5f;
	private float rotationTime = 0f;
	private float startRotation;
	private float targetRotation;
	private bool isRotatingPostSwap = false;

	private Sprite2D magnetSprite = null;

	private Vector2 draw1 = Vector2.Zero;
	private Vector2 draw2 = Vector2.Zero;
	private Array<Node2D> physicsItems = new();
	private bool dettach = false;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		parent = (Node2D)GetParent();

		if (parent is MagneticCharacterParent mgp) {
			magCharPar = mgp;
		} else {
			GD.PrintErr($"MagneticCharacteComponent {this}, does not have a parent of type MagneticCharacterParent {GetParent()}");
			GD.PushError($"MagneticCharacteComponent {this}, does not have a parent of type MagneticCharacterParent {GetParent()}");
		}

		Array<Node> children = magCharPar.GetChildren();

		for (int i = 0; i < children.Count; i++) {
			if (children[i] is CharacterBody2D character) {
				this.character = character;
			}
		}
	}
	public override void _Draw() {
        // DrawLine(ToLocal(draw2 + new Vector2(2,0)), ToLocal(draw2 - new Vector2(2,0)), Colors.Red, 4.0f);
        // DrawLine(ToLocal(draw1), ToLocal(draw2), Colors.Green, 4.0f);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)	{

		if (swapCondition == SwapCondition.SwapWhenHitSurface) {
			bodyCopy.ContactMonitor = ragdoll;
		}

		// Disables swapping to character if the timer is active
		if (ragdoll) {

			if (ragdollTimer > 0) {
				ragdollTimer -= (float) delta;

			} else if (ragdollTimer == int.MinValue) {
				// GD.Print("ragdolling");

			} else {
				// Manually swaps to character once the timer has ended so it doesnt need to be triggered again
				ragdoll = false;
				SwapToCharacter();
			}
		} 

		QueueRedraw();
	}

	public override void _PhysicsProcess(double delta)	{

		// Rotates the character back to 0 gradually once switched to character from rigid
		if (isRotatingPostSwap) {
			rotationTime += (float) delta;

			float t = Mathf.Clamp(rotationTime / rotationDuration, 0, 1);

			character.Rotation = Mathf.LerpAngle(startRotation, targetRotation, t);

			// Stop rotation when finished
			if (t >= 1)
			{
				character.Rotation = 0;
				isRotatingPostSwap = false;
			}
		}
	}

	public Array<Node2D> GetPhysicsItems() {
		Array<Node> children = character.GetChildren();

		for (int i = 0; i < children.Count; i++) {
			if (children[i].IsInGroup("Magnetic")) {				
				Array<Node> childrenMag = children[i].GetChildren();
				for (int j = 0; j < childrenMag.Count; j++) {
					if (childrenMag[j].IsInGroup("HasPhysics")) {
						Node2D node = (Node2D) childrenMag[j].Duplicate();
						node.Position = ((Node2D) children[i]).Position;
						physicsItems.Add(node);
						character.AddChild(node);
					}
				}
			}
		}
		return physicsItems;
	}

	private void OnBodyEntered(Node body) {
		ragdollTimer = 0;
	}
	public bool Dettach() {
		return dettach;
	}
	// Swaps the CharacterBody2D with the Rigidbody2D bodyCopy
	public void SwapToRigid() {
		if (isCharacter && isRigidPhysics) {
			isCharacter = false;

			bodyCopy.ProcessMode = ProcessModeEnum.Inherit;
			bodyCopy.GlobalPosition = character.GlobalPosition;
			bodyCopy.LinearVelocity = Vector2.Zero;
			bodyCopy.AngularVelocity = 0;
			bodyCopy.Rotation = character.Rotation;

			ReplaceCollisions(character, true);
			bodyCopy.Visible = true;
        	bodyCopy.Sleeping = false;
        	character.Visible = false;
			ReplaceCollisions(bodyCopy, false);
		}
	}

	// Swaps back to the character from the bodycopy
	public void SwapToCharacter() {
		if (!isCharacter && !ragdoll && isRigidPhysics) {

			character.Velocity = bodyCopy.LinearVelocity;
			isCharacter = true;
			character.GlobalPosition = bodyCopy.GlobalPosition;
			character.Velocity = bodyCopy.LinearVelocity;

			ReplaceCollisions(character, false);
			bodyCopy.Visible = false;
        	bodyCopy.Sleeping = true;
        	character.Visible = true;
			ReplaceCollisions(bodyCopy, true);

			bodyCopy.ProcessMode = ProcessModeEnum.Disabled;

			lastAngularVelocity = bodyCopy.AngularVelocity;
			startRotation = bodyCopy.Rotation;
			character.Rotation = startRotation;
			rotationTime = 0f;
			isRotatingPostSwap = true;

			// Determine the target rotation based on the direction of spin
			if (lastAngularVelocity > 0) {
				// Counterclockwise
				targetRotation = (startRotation > 0) ? 0f : Mathf.Tau;
			} else {
				// Clockwise
				targetRotation = (startRotation < 0) ? 0f : -Mathf.Tau; 
			}

			rotationDuration = 0.1f;
		}
	}

	// Isolates the character object deleting the bodyCopy and this
	public void DettachMetalObject() {
		character.RemoveFromGroup("MagneticCharacter");
		character.RemoveFromGroup("Magnetic");
		SwapToCharacter();
		Vector2 position = character.GlobalPosition;
		
		for (int i = 0; i < physicsItems.Count; i++) {
			physicsItems[i].Position = new Vector2(0, 0);
			character.RemoveChild(physicsItems[i]);
		}
		if (magnetSprite != null) {
			character.RemoveChild(magnetSprite);
		}

		parent.RemoveChild(character);
		parent.GetParent().AddChild(character);
		character.GlobalPosition = position;
		dettach = true;

		QueueFree();
	}

	public bool IsLargeCharacter() {
		return isRigidPhysics;
	}

	// Sets the rag doll timer to the corresponding value for the swap condition
	public void StartRagDollTimer() {
		switch (swapCondition)
		{
			case SwapCondition.SwapAfterTimeLimit:
				ragdollTimer = swapTimeLimit;
				break;
			case SwapCondition.SwapWhenHitSurface:
				ragdollTimer = int.MinValue;
				break;
			case SwapCondition.SwapWhenLetGo:
				ragdollTimer = 0.5f;
				break;
		}
		ragdoll = true;
	}

	public RigidBody2D GetBodyCopy() {
		return bodyCopy;
	}

	public void SetBodyCopy(RigidBody2D body) {
		bodyCopy = body;
		parent.AddChild(bodyCopy);
		bodyCopy.Connect("body_entered", new Callable(this, MethodName.OnBodyEntered));

		bodyCopyMagComp = bodyCopy.GetNode<MagneticComponent>("MagneticComponent");
	}

	public CharacterBody2D GetCharacter() {
		return character;
	}
	public void SetCharacter(CharacterBody2D character, Sprite2D magnetSprite) {
		this.magnetSprite = magnetSprite;
		this.character = character;
		character.AddToGroup("MagneticCharacter");

		// Creating a copy the collision mask and layer of character
		collisionL = character.CollisionLayer;
		collisionM = character.CollisionMask;
	}

	public Vector2 GetCharacterVelocity() {
		return character.Velocity;
	}

	public Vector2 GetBodyVelocity() {
		return bodyCopy.LinearVelocity;
	}
	
	public bool GetIsRigidPhysics() {
		return isRigidPhysics;
	}

	public float GetExitTimerDefault() {
		return exitTimer;
	}

	// Replaces collision layer/mask with either no collisions or the original collisions
	private void ReplaceCollisions(PhysicsBody2D body, bool noCollisions) {
		if (noCollisions) {
			body.CollisionLayer = 0;
			body.CollisionMask = 0;
		} else {
			body.CollisionLayer = collisionL;
			body.CollisionMask = collisionM;
		}
	}

	public Tuple<bool, Vector2> GetBodyCopyMagnetData() {
		return bodyCopyMagComp.GetMagnetData();
	}

	public Tuple<Vector2, Vector2> GetBodyCopyForceData() {
		return bodyCopyMagComp.GetForceData();
	}

	public Tuple<bool, bool> GetBodyCopyStrengthData() {
		return bodyCopyMagComp.GetStrengthData();
	}

	public void ApplyForceBodyCopy(Vector2 force, Vector2 position) {
		bodyCopy.ApplyForce(force, position);
	}

	public void ApplyTorqueBodyCopy(float torque) {
		bodyCopy.ApplyTorque(torque);
	}
}

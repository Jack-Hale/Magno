using Godot;
using System;
using System.IO;

public partial class MagneticCharacterComponent : Node2D
{
	[Export]
	public SwapCondition swapCondition;

	[Export]
	public float swapTimeLimit = 0.5f;

	public enum SwapCondition
	{
		SwapWhenHitSurface,
		SwapWhenLetGo,
		SwapAfterTimeLimit
	}

	[Export]
	public bool largeCharacter = false;

	[Export]
	public float exitTimerDefault = 2;
	private Node2D parent;
	private CharacterBody2D character;

	private RigidBody2D bodyCopy;

	public bool isCharacter = true;
	private RigidBody2D collisionL;
	private RigidBody2D collisionM;
	private MagneticComponent bodyCopyMagComp;

	private float ragdollTimer = 0;
	private bool ragdoll = false;

	private Vector2 draw1 = Vector2.Zero;
	private Vector2 draw2 = Vector2.Zero;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		parent = (Node2D)GetParent();

		foreach (var child in parent.GetChildren()) {
			if (child is CharacterBody2D) {
				character = (CharacterBody2D)child;
				break;
			}
		}

		character.AddToGroup("MagneticCharacter");

		// Intialising a copy of the characterBody2D as a rigidBody2D
		bodyCopy = new RigidBody2D();
		parent.CallDeferred("add_child", bodyCopy);

		collisionL = new RigidBody2D();
		collisionM = new RigidBody2D();

		if (character != null) {
			
			// Creating a copy the collision mask and layer of character
			for (int i = 1; i <= 32; i++) {
				collisionL.SetCollisionLayerValue(i, character.GetCollisionLayerValue(i));
				collisionM.SetCollisionMaskValue(i, character.GetCollisionMaskValue(i));
			}

			bodyCopy.MaxContactsReported = 1;

			ReplaceCollisions(bodyCopy, true);

			bodyCopy.AddToGroup("Magnetic");
			bodyCopy.AddToGroup("BodyCopy");
			bodyCopy.Name = "BODYCOPY";

			bodyCopy.Connect("body_entered", new Callable(this, MethodName.OnBodyEntered));

			// Disabling BodyCopy
			bodyCopy.Visible = false;
        	bodyCopy.Sleeping = true;
        	character.Visible = true;

			bodyCopy.ProcessMode = ProcessModeEnum.Disabled;
		} else {
			GD.PrintErr("MagneticCharacteComponent ", this, ", does not have a CharacterBody2D next to it in Scene Tree", GetParent());
			GD.PushError("MagneticCharacteComponent ", this, ", does not have a CharacterBody2D next to it in Scene Tree", GetParent());
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

	}

	private void OnBodyEntered(Node body) {
		ragdollTimer = 0;
	}

	// Swaps the CharacterBody2D with the Rigidbody2D bodyCopy
	public void SwapToRigid() {
		if (isCharacter && !largeCharacter) {
			isCharacter = false;

			bodyCopy.ProcessMode = ProcessModeEnum.Inherit;
			bodyCopy.GlobalTransform = character.GetGlobalTransform();

			ReplaceCollisions(character, true);
			bodyCopy.Visible = true;
        	bodyCopy.Sleeping = false;
        	character.Visible = false;
			ReplaceCollisions(bodyCopy, false);

			character.ProcessMode = ProcessModeEnum.Disabled;
		}
	}

	// Swaps back to the character from the bodycopy
	public void SwapToCharacter() {
		if (!isCharacter && !ragdoll && !largeCharacter) {
			character.Velocity = bodyCopy.LinearVelocity;
			isCharacter = true;
			character.ProcessMode = ProcessModeEnum.Inherit;
			character.GlobalPosition = bodyCopy.GlobalPosition;

			ReplaceCollisions(character, false);
			bodyCopy.Visible = false;
        	bodyCopy.Sleeping = true;
        	character.Visible = true;
			ReplaceCollisions(bodyCopy, true);


			bodyCopy.ProcessMode = ProcessModeEnum.Disabled;

			character.Rotation = 0;
		}
	}

	// Isolates the character object deleting the bodyCopy and this
	public void DettachMetalObject() {
		character.RemoveFromGroup("MagneticCharacter");
		character.RemoveFromGroup("Magnetic");
		SwapToCharacter();
		Vector2 position = character.GlobalPosition;
		parent.RemoveChild(character);
		parent.GetParent().AddChild(character);
		character.GlobalPosition = position;
		QueueFree();
	}

	public bool IsLargeCharacter() {
		return largeCharacter;
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

	public CharacterBody2D GetCharacter() {
		return character;
	}

	public Vector2 GetCharacterVelocity() {
		return character.Velocity;
	}

	public Vector2 GetBodyVelocity() {
		return bodyCopy.LinearVelocity;
	}
	
	public bool GetLargeCharacter() {
		return largeCharacter;
	}

	public float GetExitTimerDefault() {
		return exitTimerDefault;
	}

	// Replaces collision layer/mask with either no collisions or the original collisions
	private void ReplaceCollisions(PhysicsBody2D body, bool noCollisions) {
		if (noCollisions) {
			for (int i = 1; i <= 32; i++) {
				body.SetCollisionLayerValue(i, false);
				body.SetCollisionMaskValue(i, false);
			}
		} else {
			for (int i = 1; i <= 32; i++) {
				body.SetCollisionLayerValue(i, collisionL.GetCollisionLayerValue(i));
				body.SetCollisionMaskValue(i, collisionM.GetCollisionMaskValue(i));
			}
		}
	}

	// Has to be in separate method outside of _Ready() in order to ensure it is called
	// after the MagComp has initialised the actual metal object in the character
	public RigidBody2D InitialiseBodyCopy() {
		foreach (var child in character.GetChildren()) {
			if (!child.IsInGroup("MagneticComponent")) {
				if (!child.IsInGroup("Magnetic")) {
					bodyCopy.AddChild(child.Duplicate());
				} else {
					bodyCopy.AddChild(child.GetNode("Sprite2D").Duplicate());
				}
			}
		}
		// Adds a magnetic component to the rigidbody so it can be moved with magnets
		MagneticComponent magComp = new MagneticComponent(this);
		bodyCopy.AddChild(magComp);	
		bodyCopyMagComp = magComp;

		return bodyCopy;
	}

	public Tuple<bool, Vector2> GetBodyCopyMagnetData() {
		return bodyCopyMagComp.GetMagnetData();
	}
}

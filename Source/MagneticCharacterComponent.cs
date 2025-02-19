using Godot;
using System;
using System.IO;

public partial class MagneticCharacterComponent : Node2D
{
	[Export]
	public SwapCondition swapCondition;

	[Export]
	public float swapTimeLimit;

	public enum SwapCondition
	{
		SwapWhenHitSurface,
		SwapWhenLetGo,
		SwapAfterTimeLimit
	}
	private Node2D parent;
	private CharacterBody2D character;

	private RigidBody2D bodyCopy;

	public bool isCharacter = true;
	private RigidBody2D collisionL;
	private RigidBody2D collisionM;

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

			ReplaceCollisions(bodyCopy, true);

			bodyCopy.AddToGroup("Magnetic");
			bodyCopy.AddToGroup("BodyCopy");
			bodyCopy.Name = "BODYCOPY";

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

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)	{
		// if (Godot.Input.IsActionJustPressed("ToggleGodmode")) {
		// 	SwapToRigid();
		// }

		// if (Godot.Input.IsActionJustPressed("ToggleMagnetMode")) {
		// 	SwapToCharacter();
		// }
	}

	// Swaps the CharacterBody2D with the Rigidbody2D bodyCopy
	public void SwapToRigid() {
		if (isCharacter) {
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

	public void SwapToCharacter() {
		if (!isCharacter) {
			isCharacter = true;
			character.ProcessMode = ProcessModeEnum.Inherit;
			character.GlobalTransform = bodyCopy.GetGlobalTransform();
			character.Velocity = bodyCopy.LinearVelocity;

			ReplaceCollisions(character, false);
			bodyCopy.Visible = false;
        	bodyCopy.Sleeping = true;
        	character.Visible = true;
			ReplaceCollisions(bodyCopy, true);


			bodyCopy.ProcessMode = ProcessModeEnum.Disabled;

			character.Rotation = 0;
		}
	}

	public RigidBody2D GetBodyCopy() {
		return bodyCopy;
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

		return bodyCopy;
	}
}

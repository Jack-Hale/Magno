using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

// using System.Collections.Generic;

using System.Linq;

public struct MagneticComponentStruct {
	private SwapCondition swapCondition;
	private float swapTimeLimit;
	private bool isRigidPhysics;
	private bool ragDollOnAnyForce;
	private SwapCondition anyForceSwapCondition;
	private float anyForceSwapTimeLimit;

	public MagneticComponentStruct(SwapCondition swapCondition, float swapTimeLimit, bool isRigidPhysics, bool ragDollOnAnyForce,
		SwapCondition anyForceSwapCondition, float anyForceSwapTimeLimit) {
		this.swapCondition = swapCondition;
		this.swapTimeLimit = swapTimeLimit;
		this.isRigidPhysics = isRigidPhysics;
		this.ragDollOnAnyForce = ragDollOnAnyForce;
		this.anyForceSwapCondition = anyForceSwapCondition;
		this.anyForceSwapTimeLimit = anyForceSwapTimeLimit;
	}

	public SwapCondition GetSwapCondition() {
		return swapCondition;
	}
	public float GetSwapTimeLimit() {
		return swapTimeLimit;
	}
	public bool GetIsRigidPhysics() {
		return isRigidPhysics;
	}
	public bool GetRagDollOnAnyForce() {
		return ragDollOnAnyForce;
	}
	public SwapCondition GetAnyForceSwapCondition() {
		return anyForceSwapCondition;
	}
	public float GetAnyForceSwapTimeLimit() {
		return anyForceSwapTimeLimit;
	}


	public void SetSwapCondition(SwapCondition swapCondition) {
		this.swapCondition = swapCondition;
	}
	public void SetSwapTimeLimit(float swapTimeLimit) {
		this.swapTimeLimit = swapTimeLimit;
	}
	public void SetIsRigidPhysics(bool isRigidPhysics) {
		this.isRigidPhysics = isRigidPhysics;
	}
	public void SetRagDollOnAnyForce(bool ragDollOnAnyForce) {
		this.ragDollOnAnyForce = ragDollOnAnyForce;
	}
	public void SetAnyForceSwapCondition(SwapCondition anyForceSwapCondition) {
		this.anyForceSwapCondition = anyForceSwapCondition;
	}
	public void SetAnyForceSwapTimeLimit(float anyForceSwapTimeLimit) {
		this.anyForceSwapTimeLimit = anyForceSwapTimeLimit;
	}
}
public enum SwapCondition {
	SwapWhenHitSurface,
	SwapWhenLetGo,
	SwapAfterTimeLimit
}

public partial class MagneticCharacterComponent : Node2D {

	/// <summary>
	/// The condition that allows the character to swap back into a CharacterBody2D from body copy.
	/// </summary>
	[Export]
	private SwapCondition swapCondition;

	[Export]
	private float swapTimeLimit = 0.5f;

	[Export]
	public bool isRigidPhysics = true;
	[Export]
	private bool ragDollOnAnyForce = false;
	[Export]
	private SwapCondition anyForceSwapCondition;
	[Export]
	private float anyForceSwapTimeLimit = 0.5f;
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

	private Godot.Collections.Dictionary<Sprite2D, Sprite2D> magnetSprites = new();
	private Godot.Collections.Dictionary<AnimationPlayer, AnimationPlayer> magnetPlayers = new();
	private Godot.Collections.Dictionary<Node2D, Node2D> physicsItems = new();

	private Vector2 draw1 = Vector2.Zero;
	private Vector2 draw2 = Vector2.Zero;
	private bool detach = false;
	public bool CanSwapToCharacter = true;
	private bool waitForSwap = false;
	private bool hitDetected = false;
	private bool detaching = false;
	private Queue<Node2D> detachQueue = new();

	public MagneticCharacterComponent() { }

	public MagneticCharacterComponent(string name, MagneticComponentStruct magneticComponentStruct) {
		Name = name;
		swapCondition = magneticComponentStruct.GetSwapCondition();
		swapTimeLimit = magneticComponentStruct.GetSwapTimeLimit();
		isRigidPhysics = magneticComponentStruct.GetIsRigidPhysics();
		ragDollOnAnyForce = magneticComponentStruct.GetRagDollOnAnyForce();
		anyForceSwapCondition = magneticComponentStruct.GetAnyForceSwapCondition();
		anyForceSwapTimeLimit = magneticComponentStruct.GetAnyForceSwapTimeLimit();
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		parent = (Node2D)GetParent();

		if (parent is MagneticCharacterParent mgp) {
			magCharPar = mgp;
		}
		else {
			GD.PrintErr($"MagneticCharacteComponent {this}, does not have a parent of type MagneticCharacterParent {GetParent()}");
			GD.PushError($"MagneticCharacteComponent {this}, does not have a parent of type MagneticCharacterParent {GetParent()}");
		}

		Array<Node> children = magCharPar.GetChildren();

		for (int i = 0; i < children.Count; i++) {
			if (children[i] is CharacterBody2D character) {
				this.character = character;
				character.AddToGroup("MagneticCharacter");

				// Creating a copy the collision mask and layer of character
				collisionL = character.CollisionLayer;
				collisionM = character.CollisionMask;
			}
		}
	}
	public override void _Draw() {
		// DrawLine(ToLocal(draw2 + new Vector2(2,0)), ToLocal(draw2 - new Vector2(2,0)), Colors.Red, 4.0f);
		// DrawLine(ToLocal(draw1), ToLocal(draw2), Colors.Green, 4.0f);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {

		if (swapCondition == SwapCondition.SwapWhenHitSurface || anyForceSwapCondition == SwapCondition.SwapWhenHitSurface) {
			bodyCopy.ContactMonitor = ragdoll;
		}

		// Disables swapping to character if the timer is active
		if (ragdoll) {

			if (ragdollTimer > 0) {
				ragdollTimer -= (float)delta;

			}
			else if (ragdollTimer == int.MinValue) {
				// GD.Print("ragdolling");

			}
			else {
				// Manually swaps to character once the timer has ended so it doesnt need to be triggered again
				if (CanSwapToCharacter) {
					ragdoll = false;
					SwapToCharacter();
				}
			}
		}

		QueueRedraw();
	}

	public override void _PhysicsProcess(double delta) {
		// Rotates the character back to 0 gradually once switched to character from rigid
		if (isRotatingPostSwap) {
			rotationTime += (float)delta;

			float t = Mathf.Clamp(rotationTime / rotationDuration, 0, 1);

			character.Rotation = Mathf.LerpAngle(startRotation, targetRotation, t);

			// Stop rotation when finished
			if (t >= 1) {
				character.Rotation = 0;
				isRotatingPostSwap = false;
			}
		}

		if (detachQueue.Count > 0) {
			if (!detaching) {
				DetachMetalObject(detachQueue.Dequeue());
			}
		}
	}

	/// <summary>
	/// Takes all physics based objects on character and creates a copy of just the physics node.
	/// </summary>
	/// <returns>Dictionary containing the copy of the physics node as the key and the original as the value.</returns>
	public Godot.Collections.Dictionary<Node2D, Node2D> GeneratePhysicsItems() {
		Array<Node> children = character.GetChildren();

		for (int i = 0; i < children.Count; i++) {
			if (children[i].IsInGroup("ChildHasPhysics")) {
				Array<Node> childrenMag = children[i].GetChildren();
				for (int j = 0; j < childrenMag.Count; j++) {
					if (childrenMag[j].IsInGroup("HasPhysics")) {
						Node2D node = (Node2D)childrenMag[j].Duplicate();
						node.Position = ((Node2D)children[i]).Position;
						node.Name = $"{childrenMag[j].Name}_Duplicate";
						node.AddToGroup($"path:{character.GetPathTo(childrenMag[j])}");

						physicsItems.Add(node, (Node2D)childrenMag[j]);
						character.AddChild(node);
					}
				}
			}
		}
		return physicsItems;
	}

	public Godot.Collections.Dictionary<Node2D, Node2D> GetPhysicsItems() {
		return physicsItems;
	}
	private void OnBodyEntered(Node body) {
		ragdollTimer = 0;
		hitDetected = true;
	}
	public bool Detach() {
		return detach;
	}
	// Swaps the CharacterBody2D with the Rigidbody2D bodyCopy
	public void SwapToRigid() {
		if (isCharacter && isRigidPhysics) {
			isCharacter = false;

			foreach (Node2D item in physicsItems.Keys) {
				item.ProcessMode = ProcessModeEnum.Disabled;
			}

			bodyCopy.ProcessMode = ProcessModeEnum.Inherit;
			bodyCopy.GlobalPosition = character.GlobalPosition;
			bodyCopy.LinearVelocity = Vector2.Zero;
			bodyCopy.AngularVelocity = 0;
			bodyCopy.Rotation = 0;
			// bodyCopy.Rotation = character.Rotation;

			ReplaceCollisions(character, true);
			bodyCopy.Sleeping = false;
			bodyCopy.Visible = true;
			character.Visible = false;
			ReplaceCollisions(bodyCopy, false);
		}
	}

	// Swaps back to the character from the bodycopy
	public bool SwapToCharacter() {
		if (!isCharacter && !ragdoll && !bodyCopy.IsInGroup("AttachedToMagnet")) {
			foreach (Node2D item in physicsItems.Keys) {
				item.ProcessMode = ProcessModeEnum.Inherit;
			}
			character.Velocity = bodyCopy.LinearVelocity;
			isCharacter = true;
			character.GlobalPosition = bodyCopy.GlobalPosition;
			character.Velocity = bodyCopy.LinearVelocity;

			ReplaceCollisions(character, false);
			bodyCopy.Sleeping = true;
			bodyCopy.Visible = false;
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
			}
			else {
				// Clockwise
				targetRotation = (startRotation < 0) ? 0f : -Mathf.Tau;
			}

			rotationDuration = 0.1f;

			// Ensures that if magneticism is being removed, it waits until swapped to character.
			if (waitForSwap) {
				StartRemoval();
			}
			return true;
		}
		else {
			return false;
		}
	}

	/// <summary>
	/// Given an object, remove all nodes that are duplicates from that object from character.
	/// <para>If object is the last magnetic object on character, remove all magneticism from character.</para>
	/// </summary>
	public void DetachMetalObject(Node2D objectRemove) {
		if (!detaching) {
			detaching = true;

			// Finding the copy of the object on the character
			Godot.Collections.Dictionary<Area2D, NodePath> duplicateObjects = magCharPar.GetDuplicateObjects();
			Area2D copyRemove = null;
			foreach (var item in duplicateObjects.Keys) {
				if (duplicateObjects[item] == magCharPar.GetPathTo(objectRemove)) {
					copyRemove = item;
					break;
				}
			}

			// Removing any duplicates from body copy.
			foreach (var item in bodyCopy.GetChildren()) {
				string[] name = item.Name.ToString().Split('-');
				if (name.Length == 3) {
					if (name[1] == objectRemove.Name) {
						bodyCopy.RemoveChild(item);
					}
				}
			}

			// Removing any duplicates from character.
			foreach (var item in character.GetChildren()) {
				string[] name = item.Name.ToString().Split('-');
				if (name.Length == 3) {
					if (name[1] == objectRemove.Name) {
						character.RemoveChild(item);
					}
				}
			}

			if (copyRemove != null) {
				character.RemoveChild(copyRemove);
			}

			// If object to remove has a physics item duplicated, remove it.
			if (objectRemove.IsInGroup("ChildHasPhysics")) {
				Array<Node> children = objectRemove.GetChildren();
				for (int i = 0; i < children.Count; i++) {
					if (children[i].IsInGroup("HasPhysics")) {
						foreach (var item in physicsItems.Keys) {
							if (children[i] == physicsItems[item]) {
								character.RemoveChild(item);
								physicsItems.Remove(item);
							}
						}
					}
				}
			}
		}
		else {
			detachQueue.Enqueue(objectRemove);
		}
		detaching = false;
	}

	/// <summary>
	/// Will attempt at removing all magnetic nodes from character. 
	/// <para>If magnetic object or component still exists in character tree, removal fails.</para>
	/// <para>Removal won't occur until the next time SwapToCharacter() is called.</para>
	/// </summary>
	public void TryRemoveMagnetism() {
		// If the character still has magnet objects to detach, don't remove magnetic abilities yet
		if (!isCharacter) {
			waitForSwap = true;
		}
		else if (!CharacterHasMagnet()) {
			StartRemoval();
		}
	}

	/// <summary>
	/// Starts the removal of all magneticism on character.
	/// </summary>
	public void StartRemoval() {
		ragdoll = false;
		SwapToCharacter();

		// Deleting all trace of magnetic based components from character.
		character.RemoveFromGroup("MagneticCharacter");
		character.RemoveFromGroup("Magnetic");
		Vector2 position = character.GlobalPosition;
		Vector2 velocity = character.Velocity;

		parent.RemoveChild(character);
		parent.GetParent().AddChild(character);
		character.GlobalPosition = position;
		character.Velocity = velocity;
		detach = true;
		QueueFree();
	}

	/// <summary>
	/// Returns if character has a magnetic object.
	/// </summary>
	public bool CharacterHasMagnet() {
		Array<Node> children = character.GetChildren();

		for (int i = 0; i < children.Count; i++) {
			if (children[i].IsInGroup("Magnetic")) {
				return true;
			}
		}
		return false;
	}

	public bool IsLargeCharacter() {
		return isRigidPhysics;
	}

	/// <summary>
	/// Sets the rag doll timer to the corresponding value for the swap condition.
	/// </summary>
	public void StartRagDollTimer() {
		switch (swapCondition) {
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

	/// <summary>
	/// Sets the rag doll timer to the corresponding value for the swap condition.
	/// </summary>
	public void StartAnyForceRagDollTimer() {
		switch (anyForceSwapCondition) {
			case SwapCondition.SwapAfterTimeLimit:
				ragdollTimer = anyForceSwapTimeLimit;
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
		foreach (var item in magCharPar.GetChildren()) {
			if (item.IsInGroup("BodyCopy")) {
				bodyCopy.Disconnect("body_entered", new Callable(this, MethodName.OnBodyEntered));
				magCharPar.RemoveChild(item);
			}
		}

		bodyCopy = body;
		magCharPar.AddChild(bodyCopy);
		bodyCopy.Connect("body_entered", new Callable(this, MethodName.OnBodyEntered));

		bodyCopyMagComp = bodyCopy.GetNode<MagneticComponent>("MagneticComponent");
	}

	public CharacterBody2D GetCharacter() {
		return character;
	}
	public void SetCharacterSprites(Godot.Collections.Dictionary<Sprite2D, Sprite2D> magnetSprites) {
		this.magnetSprites = magnetSprites;
	}

	public void SetCharacterPlayers(Godot.Collections.Dictionary<AnimationPlayer, AnimationPlayer> magnetPlayers) {
		this.magnetPlayers = magnetPlayers;
	}

	public Vector2 GetCharacterVelocity() {
		return character.Velocity;
	}

	public Vector2 GetBodyVelocity() {
		return bodyCopy.LinearVelocity;
	}

	// Replaces collision layer/mask with either no collisions or the original collisions
	private void ReplaceCollisions(PhysicsBody2D body, bool noCollisions) {
		if (noCollisions) {
			body.CollisionLayer = 0;
			body.CollisionMask = 0;
		}
		else {
			body.CollisionLayer = collisionL;
			body.CollisionMask = collisionM;
		}
	}

	/// <summary>
	/// Gets the data representing the parameters of a magnet affecting bodyCopy.
	/// <para>bool Item1 = pull mode of magnet (true = pull, false = push)</para>
	/// <para>Vector2 Item2 = position the magnet is affecting parent</para>
	/// </summary>
	/// <returns>Tuple containing both bool and vector.</returns>
	public Tuple<bool, Vector2> GetBodyCopyMagnetData() {
		return bodyCopyMagComp.GetMagnetData();
	}

	/// <summary>
	/// Gets the data representing the force of a magnet affecting bodyCopy.
	/// <para>Vector2 Item1 = force vector</para>
	/// <para>Vector2 Item2 = position force is being applied</para>
	/// </summary>
	/// <returns>Tuple containing both vectors.</returns>
	public Tuple<Vector2, Vector2> GetBodyCopyForceData() {
		return bodyCopyMagComp.GetForceData();
	}

	/// <summary>
	/// Gets the data representing the strength of a magnet affecting bodyCopy.
	/// <para>bool Item1 = if blast</para>
	/// <para>bool Item2 = if strong magnet</para>
	/// </summary>
	/// <returns>Tuple containing both booleans.</returns>
	public Tuple<bool, bool> GetBodyCopyStrengthData() {
		return bodyCopyMagComp.GetStrengthData();
	}

	public void ApplyForceBodyCopy(Vector2 force, Vector2 position) {
		bodyCopy.ApplyForce(force, position);
	}

	public void ApplyTorqueBodyCopy(float torque) {
		bodyCopy.ApplyTorque(torque);
	}
	public bool GetIsRagDoll() {
		return ragdoll;
	}

	public bool GetHitDetected() {
		return hitDetected;
	}

	public void ResetHitDetected() {
		hitDetected = false;
	}

	public Godot.Collections.Dictionary<Area2D, NodePath> GetDuplicateObjects() {
		return magCharPar.GetDuplicateObjects();
	}

	public SwapCondition GetSwapCondition() {
		return swapCondition;
	}
	public float GetSwapTimeLimit() {
		return swapTimeLimit;
	}
	public bool GetIsRigidPhysics() {
		return isRigidPhysics;
	}
	public bool GetRagDollOnAnyForce() {
		return ragDollOnAnyForce;
	}
	public SwapCondition GetAnyForceSwapCondition() {
		return anyForceSwapCondition;
	}
	public float GetAnyForceSwapTimeLimit() {
		return anyForceSwapTimeLimit;
	}
}

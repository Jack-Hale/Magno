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

public partial class MagneticCharacterComponent : Node2D {

	[Export]
	public SwapCondition swapCondition;

	[Export]
	public float swapTimeLimit = 0.5f;

	[Export]
	public bool isRigidPhysics = true;
	[Export]
	public bool ragDollOnAnyForce = false;
	[Export]
	public SwapCondition anyForceSwapCondition;
	[Export]
	public float anyForceSwapTimeLimit = 0.5f;
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

	private Dictionary<Sprite2D, Sprite2D> magnetSprites = new();
	private Dictionary<AnimationPlayer, AnimationPlayer> magnetPlayers = new();
	private Dictionary<Node2D, Node2D> physicsItems = new();

	private Vector2 draw1 = Vector2.Zero;
	private Vector2 draw2 = Vector2.Zero;
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
	public override void _Process(double delta)	{

		if (swapCondition == SwapCondition.SwapWhenHitSurface || anyForceSwapCondition == SwapCondition.SwapWhenHitSurface) {
			bodyCopy.ContactMonitor = ragdoll;
		}

		// GD.Print(ragdoll);
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



	public Dictionary<Node2D, Node2D> GetPhysicsItems() {
		Array<Node> children = character.GetChildren();

		for (int i = 0; i < children.Count; i++) {
			if (children[i].IsInGroup("ChildHasPhysics")) {				
				Array<Node> childrenMag = children[i].GetChildren();
				for (int j = 0; j < childrenMag.Count; j++) {
					if (childrenMag[j].IsInGroup("HasPhysics")) {
						Node2D node = (Node2D) childrenMag[j].Duplicate();
						node.Position = ((Node2D) children[i]).Position;
						node.Name = $"{childrenMag[j].Name}_Duplicate";
						node.AddToGroup($"path:{character.GetPathTo(childrenMag[j])}");

						physicsItems.Add(node, (Node2D) childrenMag[j]);
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

	/// <summary>
	/// Given an object, remove all nodes that are duplicates from that object from character.
	/// <para>If object is the last magnetic object on character, remove all magneticism from character.</para>
	/// </summary>
	public void DettachMetalObject(Node2D objectRemove) {
		// If the character still has magnet objects to dettach, don't remove magnetic abilities yet
		if (CharacterHasMagnet()) {
			
			// This is a nightmare. I think I could make this way faster by utilising Paths more but it works for now.
			int removeBodyIndex = int.MaxValue;
			Array<Node> children = character.GetChildren();
			Sprite2D objectSprite = null;
			AnimationPlayer objectPlayer = null;

			// Getting the object on character to remove by comparing magnetic components.
			for (int i = 0; i < children.Count; i++) {
				if (children[i].IsInGroup("Magnetic")) {
					MagneticComponent mc = children[i].GetNode<MagneticComponent>("MagneticComponent");
					if (mc == objectRemove.GetNode<MagneticComponent>("MagneticComponent")) {
						if (objectRemove.IsInGroup("ChildHasPhysics")) {
							removeBodyIndex = i;
							objectSprite = mc.GetRigidSprite();
							objectPlayer = mc.GetRigidPlayer();
						}
					}
				}
			}


			// Removing duplicate sprites from character and bodyCopy.
			for (int i = 0; i < children.Count; i++) {
				if (objectSprite != null) {
					if (children[i] is Sprite2D sprite) {
						if (magnetSprites.ContainsKey(sprite)) {
							if (magnetSprites[sprite] == objectSprite) {
								bodyCopy.RemoveChild(GetBodyCopyDupeNode(objectSprite));
								character.RemoveChild(sprite);
								RemoveMagnetSprite(sprite);
							}
						}
					}
				}
				
				// Removing duplicate animation players from character and bodyCopy.
				if (objectPlayer != null) {
					if (children[i] is AnimationPlayer player) {
						if (magnetPlayers.ContainsKey(player)) {
							if (magnetPlayers[player] == objectPlayer) {
								bodyCopy.RemoveChild(GetBodyCopyDupeNode(objectPlayer));
								character.RemoveChild(player);
								RemoveMagnetPlayer(player);
							}
						}
					}	
				}
			}

			// Removing duplicate physics items from character and bodyCopy.
			if (removeBodyIndex != int.MaxValue) {
				foreach (var key in physicsItems.Keys) {
					Node2D physicsNode = children[removeBodyIndex].GetNode<Node2D>(physicsItems[key].Name.ToString());

					if (physicsItems[key] == physicsNode) {
						physicsItems[key].Position = new Vector2(0, 0);
						bodyCopy.RemoveChild(GetBodyCopyDupeNode(physicsNode));
						character.RemoveChild(key);
						physicsItems.Remove(key);
					}
				}
			}
		// Deleting all trace of magnetic based components from character.
		} else {
			character.RemoveFromGroup("MagneticCharacter");
			character.RemoveFromGroup("Magnetic");
			SwapToCharacter();
			Vector2 position = character.GlobalPosition;

			parent.RemoveChild(character);
			parent.GetParent().AddChild(character);
			character.GlobalPosition = position;
			dettach = true;

			QueueFree();
		}
	}

	/// <summary>
	/// Given a node, find the node duplicated onto bodyCopy.
	/// </summary>
	/// <returns>Duplicated node</returns>
	public Node GetBodyCopyDupeNode(Node node) {
		Array<Node> children = bodyCopy.GetChildren();
		for (int i = 0; i < children.Count; i++) {
			if (children[i].IsInGroup(character.GetPathTo(node).ToString())) {
				return children[i];
			}
		}
		return null;
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
		bodyCopy = body;
		parent.AddChild(bodyCopy);
		bodyCopy.Connect("body_entered", new Callable(this, MethodName.OnBodyEntered));

		bodyCopyMagComp = bodyCopy.GetNode<MagneticComponent>("MagneticComponent");
	}

	public CharacterBody2D GetCharacter() {
		return character;
	}
	public void SetCharacterSprites(Dictionary<Sprite2D, Sprite2D> magnetSprites) {
		this.magnetSprites = magnetSprites;
	}

	public void SetCharacterPlayers(Dictionary<AnimationPlayer, AnimationPlayer> magnetPlayers) {
		this.magnetPlayers = magnetPlayers;
	}

	public void RemoveMagnetSprite(Sprite2D spriteKey) {
		magCharPar.RemoveDuplicateSprite(spriteKey);
		magnetSprites.Remove(spriteKey);
	}
	public void RemoveMagnetPlayer(AnimationPlayer playerKey) {
		magCharPar.RemoveDuplicatePlayer(playerKey);
		magnetPlayers.Remove(playerKey);
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

	public Dictionary<Sprite2D, Sprite2D> GetDuplicateSprites() {
		return magCharPar.GetDuplicateSprites();
	}

	public Dictionary<AnimationPlayer, AnimationPlayer> GetDuplicatePlayers() {
		return magCharPar.GetDuplicatePlayers();
	}

	public bool GetRagDollOnAnyForce() {
		return ragDollOnAnyForce;
	}
	public bool GetIsRagDoll() {
		return ragdoll;
	}
}

using Godot;
using Godot.Collections;
using System;

public partial class ItemComponent : Node
{
	private RigidBody2D parent;

	public event Action OnUseItemLeft;
	public event Action OnUseItemRight;
	private CharacterBody2D player;
	private CharacterBody2D itemParentChar;
	private RigidBody2D itemParentRig;
	private Magnet magnet;
	private uint collisionL = 0;
	private uint collisionM = 0;
	private bool isBeingHeld = false;
	private bool isPlayerMagnet = true;
	public override void _Ready() {
		Array<Node> array = GetTree().Root.GetChildren();
		for (int i = 0; i < array.Count; i++) {
			player = array[i].GetNodeOrNull<CharacterBody2D>("Player");
		}
		if (GetParent() is RigidBody2D node) {
			parent = node;

			parent.AddToGroup("Item");

			if (parent.GetParent() == player) {
				isPlayerMagnet = false;
				// Disabling the rigid object while it is within the larger object
				collisionL = parent.CollisionLayer;
				collisionM = parent.CollisionMask;

				parent.CollisionLayer = 0;
				parent.CollisionMask = 0;
				
				parent.Visible = false;
				parent.Sleeping = true;

				Array<Node> children = parent.GetChildren();
				for (int i = 0; i < children.Count; i++) {
					if (children[i] is Sprite2D sprite) {
						Sprite2D dupeSprite = (Sprite2D) sprite.Duplicate();
						dupeSprite.Position = parent.Position;
						player.CallDeferred("add_child", dupeSprite);
						player.CallDeferred("move_child", dupeSprite, 0);
					}
					if (children[i] is CollisionShape2D collision) {
						CollisionShape2D dupeCollision = (CollisionShape2D) collision.Duplicate();
						
						dupeCollision.Position = dupeCollision.Position + parent.Position;
						player.CallDeferred("add_child", dupeCollision);
					}
				}
			}
		} else {
			GD.PushError($"Item {GetParent()} {GetParent().Name} needs to be a RigidBody2D but is a {GetParent().GetType()}");
			GD.PrintErr($"Item {GetParent()} {GetParent().Name} needs to be a RigidBody2D but is a {GetParent().GetType()}");
		}
	}

	private PhysicsBody2D GetPhysicsParent(Node node) {
		while (true) {
			if (node.GetParent() is PhysicsBody2D physicsBody) {
				return physicsBody;
			} else {
				node = node.GetParent();
			}

			if (node == GetTree().Root) {
				return null;
			}
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)	{
		if (parent.GetParent() is Marker2D anchor) {
			magnet = (Magnet) anchor.GetParent();

			Node parent = GetPhysicsParent(magnet);

			if (parent != null) {
				if (parent is CharacterBody2D character) {
					itemParentChar = character;
					itemParentRig = null;
				} else if (parent is RigidBody2D rigid) {
					itemParentRig = rigid;
					itemParentChar = null;
				}
			} else {
				itemParentRig = null;
				itemParentChar = null;
			}
		} else {
			magnet = null;

			if (parent.GetParent() is CharacterBody2D character) {
				itemParentChar = character;
				itemParentRig = null;
			} else if (parent.GetParent() is RigidBody2D rigid) {
				itemParentRig = rigid;
				itemParentChar = null;
			} else {
				itemParentRig = null;
				itemParentChar = null;
			}
		}
	}

	public bool GetIsPlayerMagnet() {
		return isPlayerMagnet;
	}

	public void SetIsBeingHeld(bool isBeingHeld) {
		this.isBeingHeld = isBeingHeld;
	}

	public bool GetIsBeingHeld() {
		return isBeingHeld;
	}

	public void UseItemLeft() {
		OnUseItemLeft?.Invoke();
	}
	public void UseItemRight() {
		OnUseItemRight?.Invoke();
	}

	public Player GetPlayer() {
		return (Player) player;
	}

	public Magnet GetMagnet() {
		return magnet;
	}

	public PhysicsBody2D GetItemOwner() {
		if (itemParentChar != null) {
			return itemParentChar;
		} else if (itemParentRig != null) {
			return itemParentRig;
		} else {
			return null;
		}
	}
}

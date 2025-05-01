using Godot;
using Godot.Collections;
using System;
using System.Linq;

public partial class MagneticCharacterParent : Node2D {
	[Export]
	private float weakMultiplier = 7;	
	[Export]
	private float strongMultiplier = 10;
	[Export]
	private float blastMultiplier = 800;
	[Export]
	private bool canJoin = true;
	CharacterBody2D character;
	MagneticCharacterComponent component;
	Sprite2D originalSprite;
	Dictionary<Sprite2D, Sprite2D> duplicateSprites = new();
	Dictionary<AnimationPlayer, AnimationPlayer> duplicatePlayers = new();
	Dictionary<CollisionShape2D, NodePath> duplicateShapes = new();

	Dictionary<Area2D, NodePath> duplicateObjects = new();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		Array<Node> children = GetChildren();

		for (int i = 0; i < children.Count; i++) {
			if (children[i] is MagneticCharacterComponent mcc) {
				component = mcc;
			}
		}
		character = component.GetCharacter();

		component.SetBodyCopy(InitialiseBodyCopy());

		InitialiseCharacterChildren();
	}

	/// <summary>
	/// Creates the RigidBody2D copy of the character
	/// </summary>
	/// <returns>BodyCopy</returns>
	public RigidBody2D InitialiseBodyCopy() {
		RigidBody2D bodyCopy = new RigidBody2D();
		bodyCopy.MaxContactsReported = 1;

		for (int i = 1; i <= 32; i++) {
			bodyCopy.SetCollisionLayerValue(i, false);
			bodyCopy.SetCollisionMaskValue(i, false);
		}

		bodyCopy.AddToGroup("Magnetic");
		bodyCopy.AddToGroup("BodyCopy");
		bodyCopy.Name = $"{character.Name}_BODYCOPY";

		// Disabling BodyCopy
		bodyCopy.Visible = false;
		bodyCopy.Sleeping = true;
		character.Visible = true;

		bodyCopy.ProcessMode = ProcessModeEnum.Disabled;

		bool collisionTransfer = false;		

		foreach (Node child in character.GetChildren()) {
			Area2D duplicateObject = new();
			bool useDuplicate = false;

			MagneticComponent childMagComp = null;
			if (!child.IsInGroup("MagneticComponent")) {
				childMagComp = child.GetNodeOrNull<MagneticComponent>("MagneticComponent");
				
				duplicateObject.Name = $"Duplicate-{child.Name}-{character.GetPathTo(child)}-0";
				
				if (childMagComp != null && collisionTransfer) {
					duplicateObject.Name = $"Duplicate-{child.Name}-{character.GetPathTo(childMagComp)}-1";
					duplicateObject.CollisionLayer = 1u << 8;
					duplicateObject.AddToGroup("DuplicateMagnetChild");
				}
				
				if (childMagComp != null) {
					if (childMagComp.GetExitCondition() != ExitCondition.CannotExit) {
						collisionTransfer = true;
					}
				}

				// Duplicating all children of character into bodyCopy except the metal object(s)
				if (!child.IsInGroup("Magnetic")) {
					if (child is not PathFindingComponent) {
						if (!child.IsInGroup("HasPhysics")) {
							Node2D duplicateNode = (Node2D) child.Duplicate();
							duplicateNode.Name = $"{child.Name}_Duplicate";
							duplicateNode.AddToGroup(character.GetPathTo(child).ToString());
							bodyCopy.AddChild(duplicateNode);

						// If child is a physics item it would have been duplicated from a magnetic body
						} else {
							// Ensure duplication is off the original magnetic body and not off the duplicated physics item
							Node2D originalNode = null;
							foreach (string item in child.GetGroups()) {
								// When physics item is duplicated, the path is added to the duplicated node as a group
								if (item.Split(':').First() == "path") {
									originalNode = character.GetNode<Node2D>(item.Split(':').Last());
								}
							}
							if (originalNode != null) {
								Node2D duplicateNode = (Node2D) originalNode.Duplicate();
								
								duplicateNode.Position = ((Node2D) child).Position;
								duplicateNode.Name = $"{originalNode.Name}_Duplicate";
								duplicateNode.AddToGroup(character.GetPathTo(originalNode).ToString());
								bodyCopy.AddChild(duplicateNode);
							}
						}
					}
				} else {
					useDuplicate = true;
					// Extracting sprites and animation players from metal object(s)
					Array<Node> children = child.GetChildren();

					Sprite2D animationSprite = null;
					Sprite2D bodyCopyAnimSprite = null;
					Sprite2D characterCopyAnimSprite = null;
					AnimationPlayer animationPlayer = null;

					for (int i = 0; i < children.Count; i++) {
						// Checking if any AnimationPlayers exist
						if (children[i] is AnimationPlayer animPlayer) {
							animationPlayer = animPlayer;

							// Storing the sprite used in the animation player
							animationSprite = GetAnimatedSprite(animPlayer);
						}
					}

					for (int i = 0; i < children.Count; i++) {
						if (collisionTransfer) {
							if (children[i] is CollisionShape2D collShape) {
								CollisionShape2D characterCopyShape = (CollisionShape2D) collShape.Duplicate();
								duplicateObject.AddChild(characterCopyShape);
							}
						}

					
						// Extracting Sprite2Ds from metal object(s)
						if (children[i] is Sprite2D && child is PhysicsBody2D metalObject) {
							originalSprite = (Sprite2D) children[i];
							Sprite2D bodyCopySprite = (Sprite2D) originalSprite.Duplicate();
							Sprite2D characterSprite = (Sprite2D) originalSprite.Duplicate();
							// duplicateSprite.Position = bodyCopy.ToLocal(originalSprite.GlobalPosition);
							bodyCopySprite.Name = $"{originalSprite.Name}_{child.Name}_Duplicate";
							bodyCopySprite.AddToGroup(character.GetPathTo(children[i]).ToString());

							bodyCopySprite.Scale = originalSprite.Scale;
							bodyCopySprite.Rotation = metalObject.Rotation + originalSprite.Rotation;
							
							bodyCopySprite.Position = metalObject.Position.Rotated(metalObject.Rotation) + originalSprite.Position.Rotated(originalSprite.Rotation);
							bodyCopySprite.Position = bodyCopySprite.Position.Rotated(bodyCopySprite.Rotation);

							// Getting the duplicate of the sprite used in the animation player
							if (animationPlayer != null) {
								if (animationSprite == originalSprite) {
									bodyCopyAnimSprite = bodyCopySprite;
								}
							}

							characterSprite.AddToGroup(child.Name);
							characterCopyAnimSprite = characterSprite;

							duplicateObject.AddChild(characterCopyAnimSprite);
							
							bodyCopy.AddChild(bodyCopySprite);
						} 
					}
					
					// Doing a deep duplication of the animation player
					if (animationSprite != null && animationPlayer != null) {
						AnimationPlayer bodyCopyPlayer = DuplicateAnimationPlayer(animationPlayer, bodyCopyAnimSprite, animationSprite, bodyCopy.Name.ToString());
						bodyCopyPlayer.Name = $"{animationPlayer.Name}_{animationPlayer.GetParent().Name}_Duplicate";
						bodyCopyPlayer.AddToGroup(character.GetPathTo(animationPlayer).ToString());
						bodyCopy.AddChild(bodyCopyPlayer);

						AnimationPlayer characterCopyPlayer = DuplicateAnimationPlayer(animationPlayer, characterCopyAnimSprite, animationSprite, duplicateObject.Name.ToString());
						duplicateObject.AddChild(characterCopyPlayer);

						// Defaults to playing RESET. Will need to update if other animations need to be played
						characterCopyPlayer.Play("RESET");
						bodyCopyPlayer.Play("RESET");
					}
				}
			}

			if (useDuplicate && duplicateObject.GetChildCount() > 0) {
				duplicateObject.Position = ((Node2D)child).Position;
				duplicateObject.Rotation = ((Node2D)child).Rotation;
				

				duplicateObjects.Add(duplicateObject, GetPathTo(child));
			}
		}

		if (duplicateObjects.Count == 0) {
			duplicateObjects = null;
		}

		// Adds a magnetic component to the rigidbody so it can be moved with magnets
		MagneticComponent magComp = new MagneticComponent(component, weakMultiplier, strongMultiplier, blastMultiplier, canJoin);

		bodyCopy.AddChild(magComp);	

		return bodyCopy;
	}

	/// <summary>
	/// Duplicates an AnimationPlayer using newAnimationSprite for the texture key.
	/// <para>originalPlayer: The player being duplicated.</para>
	/// <para>newAnimationSprite: The sprite being used in the duplicated player.</para>
	/// <para>oldAnimationSprite: Used to get the key paths.</para>
	/// <para>recipient: The node that will have the new player added. Used to get proper pathing</para>
	/// </summary>
	/// <returns>The duplicated AnimationPlayer</returns>
	private AnimationPlayer DuplicateAnimationPlayer(AnimationPlayer originalPlayer, Sprite2D newAnimationSprite, Sprite2D oldAnimationSprite, NodePath recipientPath) {

		AnimationPlayer duplicatePlayer = (AnimationPlayer) originalPlayer.Duplicate((int) DuplicateFlags.Groups);
		
		// Doing a deep copy of the Animations themselves onto the AnimationPlayer
		foreach (var animLibName in originalPlayer.GetAnimationLibraryList()) {
			AnimationLibrary newLibrary = new();
			foreach (var animName in originalPlayer.GetAnimationLibrary(animLibName).GetAnimationList()) {
				newLibrary.AddAnimation(animName, (Animation) originalPlayer.GetAnimationLibrary(animLibName).GetAnimation(animName).Duplicate());
				duplicatePlayer.RemoveAnimationLibrary(animLibName);
				duplicatePlayer.AddAnimationLibrary(animLibName, newLibrary);
			}
		}

		// Remap animation tracks to point to the new sprite
		foreach (var animName in duplicatePlayer.GetAnimationList()) {
			var anim = duplicatePlayer.GetAnimation(animName);

			for (int j = 0; j < anim.GetTrackCount(); j++) {
				var path = anim.TrackGetPath(j);
				string nodePath = path.ToString().Split(':', StringSplitOptions.None)[0];
				// Only update tracks that point to the old Sprite2D
				if (nodePath == character.GetPathTo(oldAnimationSprite).ToString()) {

					NodePath relativePath = $"{recipientPath}/{newAnimationSprite.Name}";
					var property = path.ToString().Split(':', StringSplitOptions.None).Last(); // from the old track
					NodePath newPath = new NodePath($"{relativePath}:{property}");
					anim.TrackSetPath(j, newPath);
				}
			}
		}

		return duplicatePlayer;
	}

	/// <summary>
	/// Gets the Sprite2D used in the parsed animation player.
	/// </summary>
	/// <returns>The sprite found in the player.</returns>
	private Sprite2D GetAnimatedSprite(AnimationPlayer animPlayer) {

		foreach (var animName in animPlayer.GetAnimationList()) {
			var animation = animPlayer.GetAnimation(animName);
			for (int i = 0; i < animation.GetTrackCount(); i++) {
				if (animation.TrackGetType(i) != Animation.TrackType.Value)
					continue;

				NodePath path = animation.TrackGetPath(i);
				string property = path.ToString().Split(':', StringSplitOptions.None).Last();
				if (property == "texture" || property == "frame" || property == "hframes" || property == "vframes") {
					Node node = character.GetNode(path.GetConcatenatedNames());
					if (node is Sprite2D sprite)
						return sprite;
				}
			}
		}

		return null;
	}

	private Array<Sprite2D> GetAllAnimatedSprites(AnimationPlayer animPlayer) {
		var sprites = new Array<Sprite2D>();

		foreach (var animName in animPlayer.GetAnimationList()) {
			var animation = animPlayer.GetAnimation(animName);
			for (int i = 0; i < animation.GetTrackCount(); i++) {
				if (animation.TrackGetType(i) != Animation.TrackType.Value)
					continue;

				var path = animation.TrackGetPath(i);
				string property = path;
				if (property == "texture" || property == "frame" || property == "hframes" || property == "vframes") {
					Node node = animPlayer.GetNode(path.GetConcatenatedNames());
					if (node is Sprite2D sprite && !sprites.Contains(sprite))
						sprites.Add(sprite);
				}
			}
		}

		return sprites;
	}

	/// <summary>
	/// Initialises the character to have all the sprites and animation players from metal objects
	/// added to the character.
	/// </summary>
	public void InitialiseCharacterChildren() {
		if (duplicateObjects.Count > 0) {
			foreach (var item in duplicateObjects.Keys) {
				character.AddChild(item);
			}
		}
	}

	public void RemoveDuplicateShape(CollisionShape2D shapeKey) {
		duplicateShapes.Remove(shapeKey);
	}
	public Dictionary<Area2D, NodePath> GetDuplicateObjects() {
		return duplicateObjects;
	}

	public void RemoveDuplicateObject(Area2D objectKey) {
		duplicateObjects.Remove(objectKey);
	}
}

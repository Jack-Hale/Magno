using Godot;
using Godot.Collections;
using System;
using System.Linq;

public partial class MagneticCharacterParent : Node2D {
	CharacterBody2D character;
	MagneticCharacterComponent component;
	Node magnetObject;
	Sprite2D originalSprite;
	Dictionary<Sprite2D, Sprite2D> duplicateSprites = new();
	Dictionary<AnimationPlayer, AnimationPlayer> duplicatePlayers = new();

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
		component.SetCharacterSprites(duplicateSprites);
		component.SetCharacterPlayers(duplicatePlayers);

		InitialiseCharacterSprite();
	}

	// Creates the RigidBody2D copy of the character
	public RigidBody2D InitialiseBodyCopy() {
		RigidBody2D bodyCopy = new RigidBody2D();
		bodyCopy.MaxContactsReported = 1;

		for (int i = 1; i <= 32; i++) {
			bodyCopy.SetCollisionLayerValue(i, false);
			bodyCopy.SetCollisionMaskValue(i, false);
		}

		bodyCopy.AddToGroup("Magnetic");
		bodyCopy.AddToGroup("BodyCopy");
		bodyCopy.Name = "BODYCOPY";

		// Disabling BodyCopy
		bodyCopy.Visible = false;
		bodyCopy.Sleeping = true;
		character.Visible = true;

		bodyCopy.ProcessMode = ProcessModeEnum.Disabled;

		bool hasPlayers = false;
		bool hasSprites = false;

		foreach (Node child in character.GetChildren()) {
			if (!child.IsInGroup("MagneticComponent")) {

				// Duplicating all children of character into bodyCopy except the metal object(s)
				if (!child.IsInGroup("Magnetic")) {
					if (child is not PathFindingComponent) {
						bodyCopy.AddChild(child.Duplicate());
					}
				} else {
					// Extracting sprites and animation players from metal object(s)
					Array<Node> children = child.GetChildren();
					Sprite2D animationSprite = null;
					Sprite2D newAnimationSprite = null;
					Sprite2D newCharacterAnimationSprite = null;
					AnimationPlayer animationPlayer = null;

					// Checking if any AnimationPlayers exist
					for (int i = 0; i < children.Count; i++) {
						if (children[i] is AnimationPlayer animPlayer) {
							hasPlayers = true;
							animationPlayer = animPlayer;

							// Storing the sprite used in the animation player
							animationSprite = GetAnimatedSprite(animPlayer);
						}
					}

					// Extracting Sprite2Ds from metal object(s)
					for (int i = 0; i < children.Count; i++) {
						if (children[i] is Sprite2D && child is PhysicsBody2D metalObject) {
							hasSprites = true;

							originalSprite = (Sprite2D) children[i];
							Sprite2D duplicateSprite = (Sprite2D) originalSprite.Duplicate();
							// duplicateSprite.Position = bodyCopy.ToLocal(originalSprite.GlobalPosition);
							duplicateSprite.Name = $"{originalSprite.Name}_Duplicate";
							duplicateSprite.Rotation = metalObject.Rotation + originalSprite.Rotation;
							
							duplicateSprite.Position = metalObject.Position.Rotated(metalObject.Rotation) + originalSprite.Position.Rotated(originalSprite.Rotation);
							duplicateSprite.Position = duplicateSprite.Position.Rotated(duplicateSprite.Rotation);

							// Getting the duplicate of the sprite used in the animation player
							if (animationPlayer != null) {
								if (animationSprite == originalSprite) {
									newAnimationSprite = duplicateSprite;
								}
							}

							Sprite2D characterSprite = (Sprite2D) duplicateSprite.Duplicate();
							characterSprite.AddToGroup(child.Name);
							newCharacterAnimationSprite = characterSprite;
							
							duplicateSprites.Add(characterSprite, originalSprite);
							bodyCopy.AddChild(duplicateSprite);
						} 
					}
					
					// Doing a deep duplication of the animation player
					if (animationSprite != null && animationPlayer != null) {
						AnimationPlayer duplicatePlayer = DuplicatedAnimationPlayer(animationPlayer, newAnimationSprite, animationSprite, bodyCopy);
						bodyCopy.AddChild(duplicatePlayer);

						AnimationPlayer characterPlayer = DuplicatedAnimationPlayer(animationPlayer, newCharacterAnimationSprite, animationSprite, character);
						duplicatePlayers.Add(characterPlayer, animationPlayer);

						// Defaults to playing RESET. Will need to update if other animations need to be played
						duplicatePlayer.Play("RESET");
					}
				}
			}
		}

		// If no sprites or animation players are found setting the dictionaries to null
		if (!hasPlayers) duplicatePlayers = null;
		if (!hasSprites) duplicateSprites = null;


		// Adds a magnetic component to the rigidbody so it can be moved with magnets
		MagneticComponent magComp = new MagneticComponent(component);
		bodyCopy.AddChild(magComp);	

		return bodyCopy;
	}

	/// <summary>
	/// Duplicates an AnimationPlayer using newAnimationSprite for the texture key.
	/// <para>originalPlayer is the player being duplicated.</para>
	/// <para>newAnimationSprite is the sprite being used in the duplicated player.</para>
	/// <para>oldAnimationSprite used to get the key paths.</para>
	/// <para>recipient is the node that will have the new player added. Used to get proper pathing</para>
	/// </summary>
	/// <returns>The duplicated AnimationPlayer</returns>
	private AnimationPlayer DuplicatedAnimationPlayer(AnimationPlayer originalPlayer, Sprite2D newAnimationSprite, Sprite2D oldAnimationSprite, PhysicsBody2D recipient) {

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

					NodePath relativePath = $"{recipient.Name}/{newAnimationSprite.Name}";
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
	public void InitialiseCharacterSprite() {
		foreach (var child in character.GetChildren()) {
			if (child.IsInGroup("Magnetic") && duplicateSprites.Keys.Count > 0) {
				Array<Sprite2D> sprites = (Array<Sprite2D>) duplicateSprites.Keys;
				for (int i = 0; i < sprites.Count; i++) {
					if (sprites[i].IsInGroup(child.Name)) {
						character.AddChild(sprites[i]);
						character.MoveChild(sprites[i], child.GetIndex());
					}
				}
			}
		}
		
		if (duplicatePlayers.Keys.Count > 0) {
			Array<AnimationPlayer> players = (Array<AnimationPlayer>) duplicatePlayers.Keys;
			for (int i = 0; i < players.Count; i++) {
				character.AddChild(players[i]);
				players[i].Play("RESET");
			}
		}
	}

	public Dictionary<Sprite2D, Sprite2D> GetDuplicateSprites() {
		return duplicateSprites;
	}

	public Dictionary<AnimationPlayer, AnimationPlayer> GetDuplicatePlayers() {
		return duplicatePlayers;
	}
}

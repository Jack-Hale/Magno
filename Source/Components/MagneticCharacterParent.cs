using Godot;
using Godot.Collections;
using System;

public partial class MagneticCharacterParent : Node2D {
	[Export]
	public ExitCondition exitCondition;

	[Export]
	public float exitTimer = 2;

	[Export]
	public SwapCondition swapCondition;

	[Export]
	public float swapTimeLimit = 0.5f;

	[Export]
	public bool noRigidPhysics = false;

	CharacterBody2D character;
	MagneticCharacterComponent component;

	Sprite2D duplicateSprite;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		Array<Node> children = GetChildren();

		for (int i = 0; i < children.Count; i++) {
			if (children[i] is CharacterBody2D character) {
				this.character = character;
			}
			if (children[i] is MagneticCharacterComponent component) {
				this.component = component;
			}
		}
		
		component.SetBodyCopy(InitialiseBodyCopy());
		component.SetCharacter(InitialiseCharacterSprite(), duplicateSprite);
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

		foreach (Node child in character.GetChildren()) {
			if (!child.IsInGroup("MagneticComponent")) {

				// Duplicating all children of character into bodyCopy except the metal object
				if (!child.IsInGroup("Magnetic")) {
					if (child is not PathFindingComponent) {
						bodyCopy.AddChild(child.Duplicate());
					}
				} else {
					// Extracting just the sprite from the metal object to put in bodyCopy
					Array<Node> children = child.GetChildren();
					for (int i = 0; i < children.Count; i++) {
						if (children[i] is Sprite2D && child is PhysicsBody2D metalObject) {

							Sprite2D duplicateSprite = (Sprite2D) children[i].Duplicate();
							duplicateSprite.Position = metalObject.Position;
							duplicateSprite.Name = "ObjectSprite";
							duplicateSprite.Rotation = metalObject.Rotation;
							this.duplicateSprite = (Sprite2D) duplicateSprite.Duplicate();
							bodyCopy.AddChild(duplicateSprite);
						}
					}
				}
			}
		}

		// Adds a magnetic component to the rigidbody so it can be moved with magnets
		MagneticComponent magComp = new MagneticComponent(component);
		bodyCopy.AddChild(magComp);	

		return bodyCopy;
	}

	public CharacterBody2D InitialiseCharacterSprite() {
		foreach (var child in character.GetChildren()) {
			if (child.IsInGroup("Magnetic")) {
				character.AddChild(duplicateSprite);
				character.MoveChild(duplicateSprite, child.GetIndex());
			}
		}

		return character;
	}
}

using Godot;
using Godot.Collections;
using System;

public partial class MagneticReparentComponent : Node {
	Array<Node> reparentingNodes = new();

	/// <summary>
	/// Given a magnetic object, convert a non magnetic character into a magnetic character and make the given object the magnet source.
	/// </summary>
	public async void RemagnifyCharater(Vector2 position, RigidBody2D magnetObject, CharacterBody2D character,
		MagneticParentStruct parentStruct, MagneticComponentStruct componentStruct) {
		if (!reparentingNodes.Contains(magnetObject) && !magnetObject.IsInGroup("ReconnectionCooldown")) {
			reparentingNodes.Add(magnetObject);

			if (character.GetParent() is MagneticCharacterParent) {
				await ToSignal(character.GetTree(), SceneTree.SignalName.ProcessFrame); // Waits one frame
				AddMagnetToCharacter(position, magnetObject, character);
			}
			else {
				if (magnetObject.GetParent() is Marker2D marker && marker.IsInGroup("MagnetAnchor")) {
					Magnet magnet = (Magnet)marker.GetParent();
					magnet.Detach();
				}

				MagneticCharacterParent magneticCharacterParent = new(character.Name, parentStruct);
				MagneticCharacterComponent magneticCharacterComponent = new("MagneticCharacterComponent", componentStruct);

				await ToSignal(character.GetTree(), SceneTree.SignalName.ProcessFrame); // Waits one frame

				RemagnifyStep1(character.GetTree(), character, magnetObject, position, magneticCharacterParent, magneticCharacterComponent);
			}
		}
	}

	private async void RemagnifyStep1(SceneTree scene, CharacterBody2D character, RigidBody2D magnetObject, Vector2 position, MagneticCharacterParent magneticCharacterParent, MagneticCharacterComponent magneticCharacterComponent) {
		magnetObject.GetParent().RemoveChild(magnetObject);
		character.AddChild(magnetObject);

		await ToSignal(scene, SceneTree.SignalName.ProcessFrame); // Waits one frame

		magnetObject.Position = position;

		MagneticComponent magComp = null;
		if (magnetObject.IsInGroup("Magnetic")) {
			magComp = magnetObject.GetNode<MagneticComponent>("MagneticComponent");
		}

		if (magComp != null) {
			character.AddToGroup("MagneticCharacter");

			RemagnifyStep2(scene, magnetObject, character, magneticCharacterParent, magneticCharacterComponent, magComp);
		}
		else {
			GD.PrintErr($"Magnetic Object {magnetObject} {magnetObject.Name}, does not have a MagneticComponent.");
			GD.PushError($"Magnetic Object {magnetObject} {magnetObject.Name}, does not have a MagneticComponent.");
		}
	}

	private async void RemagnifyStep2(SceneTree scene, RigidBody2D magnetObject, CharacterBody2D character, MagneticCharacterParent magneticCharacterParent, MagneticCharacterComponent magneticCharacterComponent, MagneticComponent magneticComponent) {

		Node parent = character.GetParent();
		parent.RemoveChild(character);

		await ToSignal(scene, SceneTree.SignalName.ProcessFrame); // Waits one frame

		magneticCharacterParent.AddChild(magneticCharacterComponent);
		magneticCharacterParent.AddChild(character);
		magneticComponent.InitialiseForCharacterOwner(character, parent);

		parent.AddChild(magneticCharacterParent);

		reparentingNodes.Remove(magnetObject);
	}


	/// <summary>
	/// Adds a magnetic object to a character that is already magnetic.
	/// </summary>
	/// <param name="position"></param>
	/// <param name="magnetObject"></param>
	/// <param name="character"></param>
	private void AddMagnetToCharacter(Vector2 position, RigidBody2D magnetObject, CharacterBody2D character) {
		MagneticCharacterParent magneticCharacterParent = (MagneticCharacterParent)character.GetParent();

		magnetObject.GetParent().RemoveChild(magnetObject);
		character.AddChild(magnetObject);

		magnetObject.Position = position;

		MagneticComponent magComp = null;
		if (magnetObject.IsInGroup("Magnetic")) {
			magComp = magnetObject.GetNode<MagneticComponent>("MagneticComponent");
		}

		if (magComp != null) {
			magComp.InitialiseForCharacterOwner(character, magneticCharacterParent);
		}

		magneticCharacterParent.AddNewMagnetNode(magnetObject);
	}
}
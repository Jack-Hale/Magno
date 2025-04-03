using Godot;
using System;
using System.Runtime.CompilerServices;

public static class CharacterBody2DExtensions {
	public static void AddImpulse(this CharacterBody2D character, Vector2 impulse) {
        character.Velocity += impulse;
    }
}

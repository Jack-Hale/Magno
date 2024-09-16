using Godot;
using System;

public partial class MagneticComponent : Node2D
{
	[Export]
	public RigidBody2D Object;

	[Signal]
	public delegate void MagFieldDetectedEventHandler(Vector2 fieldSource);

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GetParent().AddToGroup("Magnetic");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}

	public void MoveObjectTowardsVector(Vector2 collisionPoint, Vector2 vector)
	{
		GD.Print(Object.LinearVelocity);
		Object.ApplyForce(collisionPoint, vector*2);
	}
}

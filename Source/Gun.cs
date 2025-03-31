using Godot;
using System;

public partial class Gun : RigidBody2D
{
	
	private ProjectileComponent _projectileComponent;
	private ItemComponent _itemComponent;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		
		_projectileComponent = GetNode<ProjectileComponent>("ProjectileComponent");
		_itemComponent = GetNode<ItemComponent>("ItemComponent");
		
		_itemComponent.OnUseItem += Shoot;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)	{

	}

	private void Shoot() {
		_projectileComponent.Shoot();
	}
}

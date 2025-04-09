using Godot;
using System;

public partial class WaspGun : RigidBody2D {
    private ProjectileComponent _projectileComponent;
    private ProjectileLauncher _projectileLauncher;
	private ItemComponent _itemComponent;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		_projectileLauncher = GetNode<ProjectileLauncher>("ProjectileLauncher");
		_projectileComponent = _projectileLauncher.GetProjectileComponent();
		_itemComponent = GetNode<ItemComponent>("ItemComponent");
		
		_itemComponent.OnUseItemLeft += Shoot;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)	{
        if (_itemComponent.GetIsBeingHeld()) {
            _projectileComponent.AddException(_itemComponent.GetItemOwner());
        }
	}

	private void Shoot() {
		_projectileComponent.Shoot();
	}
}

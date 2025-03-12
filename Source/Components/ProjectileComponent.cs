using Godot;
using System;

public partial class ProjectileComponent : Node2D {
	[Export]
	public float speed = 100;

	[Export]
	public float damage = 10;

	[Export]
	public float projectileTimeout = 3;

	[Export]
	public float shootCooldown = 1;

	private Node scene;
	private PackedScene projectileScene;
	private Node2D parent;

	private Projectile _projetile;
	private Projectile projetileTemplate;

	private bool useTemplate = false;

	private float cooldownTimer = 0;
	
	public override void _Ready() {
		parent = (Node2D)GetParent();
		scene = GetTree().Root;
		projectileScene = (PackedScene)GD.Load("res://Scenes/Perishable Objects/projectile.tscn");
		
		_projetile = GetNodeOrNull<Projectile>("Projectile");

		// Found a projectile to use as a template instead of default
		if (_projetile != null) {
			_projetile.SetToTemplate();
			useTemplate = true;

			projetileTemplate = (Projectile) _projetile.Duplicate();
			
			projetileTemplate.Visible = true;
			projetileTemplate.ProcessMode = ProcessModeEnum.Inherit;
		}
	}

	public override void _Process(double delta)	{
		if (cooldownTimer > 0) {
			cooldownTimer -= (float) delta;
		} else {
			cooldownTimer = 0;
		}
		// if (Godot.Input.IsActionJustPressed("ToggleGodmode")) {
		// }
		
	}

	public void Shoot() {
		if (cooldownTimer <= 0) {
			Projectile projectile;
			if (useTemplate) {
				projectile = (Projectile) projetileTemplate.Duplicate();
			} else {
				projectile = (Projectile) projectileScene.Instantiate();	
			}
			projectile.SetVariables(speed, GlobalRotation, GlobalPosition, GlobalRotation, damage, projectileTimeout);
			scene.AddChild(projectile);

			cooldownTimer = shootCooldown;
		}
	}
}

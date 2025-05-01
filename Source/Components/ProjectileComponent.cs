using Godot;
using Godot.Collections;
using System;

public partial class ProjectileComponent : Node2D {
	[Export]
	public float speed = 700;

	[Export]
	public float damage = 10;

	[Export]
	public float projectileTimeout = 3;

	[Export]
	public float shootCooldown = 1;

	public Array<Node2D> excludeArray = new();

	private Node scene;
	private PackedScene projectileScene;
	private Node2D parent;

	private Projectile _projetile;
	private Projectile projetileTemplate;

	private bool useTemplate = false;

	private float cooldownTimer = 0;
	public bool flipH = false;
	private bool flipping = false;

	private Vector2 originalPosition;
	private float originalRotation;
	
	public override void _Ready() {
		parent = (Node2D)GetParent();
		scene = GetTree().Root.GetChild(0);
		projectileScene = (PackedScene)GD.Load("res://Scenes/Perishable Objects/projectile.tscn");
		
		_projetile = GetNodeOrNull<Projectile>("Projectile");
		originalPosition = Position;
		// Found a projectile to use as a template instead of default
		if (_projetile != null) {
			_projetile.SetToTemplate();
			useTemplate = true;

			projetileTemplate = (Projectile) _projetile.Duplicate();
			
			projetileTemplate.Visible = true;
			projetileTemplate.ProcessMode = ProcessModeEnum.Inherit;
		}

		originalRotation = parent.Rotation;
		Rotation = -originalRotation;

		excludeArray.Add(parent);
		if (parent is ProjectileLauncher) {
			excludeArray.Add((Node2D) parent.GetParent());
		}

			if (flipH != flipping) {
			GD.Print("FLIP");
			Position = new Vector2(flipH ? -originalPosition.X : originalPosition.X, Position.Y);
			Rotation = flipH ? originalRotation : -originalRotation;
		}

		flipping = flipH;
	}

	public override void _PhysicsProcess(double delta)	{
	
	}
	public override void _Process(double delta)	{
		if (cooldownTimer > 0) {
			cooldownTimer -= (float) delta;
		} else {
			cooldownTimer = 0;
		}
	}

	public void AddException(Node2D exception) {
		if (!excludeArray.Contains(exception)) {
			excludeArray.Add(exception);
		}
	}

	public void RemoveException(Node2D exception) {
		excludeArray.Remove(exception);
	}

	public void Shoot() {
		if (cooldownTimer <= 0) {
			Projectile projectile;
			if (useTemplate) {
				projectile = (Projectile) projetileTemplate.Duplicate();
			} else {
				projectile = (Projectile) projectileScene.Instantiate();	
			}
			projectile.SetVariables(speed, GlobalRotation, GlobalPosition, GlobalRotation, damage, projectileTimeout, excludeArray);
			scene.AddChild(projectile);
			scene.MoveChild(projectile, 0);

			cooldownTimer = shootCooldown;
		}
	}
}

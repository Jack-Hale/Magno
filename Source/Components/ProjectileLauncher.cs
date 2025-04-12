using Godot;
using Godot.Collections;
using System;

public partial class ProjectileLauncher : Node2D {
	[Export]
	private bool flipOnParentSprite = false;
	private ProjectileComponent _projectileComponent;
	private Sprite2D _sprite;
	private bool flipH = false;
	private bool flipping = false;
	private Vector2 originalPosition;
	private float originalRotation;
	private Vector2 projectilePosition;
	private float spriteRotation;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		AddToGroup("HasPhysics");
		GetParent().AddToGroup("ChildHasPhysics");
		_projectileComponent = GetNodeOrNull<ProjectileComponent>("ProjectileComponent");
		_sprite = GetNodeOrNull<Sprite2D>("Sprite2D");

		if (_projectileComponent == null) {
			GD.PrintErr($"Projectile launcher on {GetParent().Name} {GetParent()} requires a projectile component but has none");
			GD.PushError($"Projectile launcher on {GetParent().Name} {GetParent()} requires a projectile component but has none");
		}

		if (_sprite == null) {
			GD.PrintErr($"Projectile launcher on {GetParent().Name} {GetParent()} requires a sprite2D but has none");
			GD.PushError($"Projectile launcher on {GetParent().Name} {GetParent()} requires a sprite2D but has none");
		}

		originalPosition = Position;
		originalRotation = Rotation;
		projectilePosition = _projectileComponent.Position;
		spriteRotation = _sprite.Rotation;
	}

	public override void _PhysicsProcess(double delta) {

	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {

		bool flip = flipOnParentSprite ? flipH : false;
		float angle = (Rotation % (2 * Mathf.Pi) + (2 * Mathf.Pi)) % (2 * Mathf.Pi);

		if (!flipOnParentSprite) {
			// Testing if the angle of the gun has it pointing on the left side of the character to flip its sprites
			if (angle >= (3 * Mathf.Pi / 2) || angle <= (Mathf.Pi / 2)) {
				_projectileComponent.Position = projectilePosition;
			} else {
				_projectileComponent.Position = new Vector2(projectilePosition.X, -projectilePosition.Y);
				flip = true;
			}
		}

		_sprite.FlipV = flip;
		_projectileComponent.flipH = flip;

		_sprite.Rotation = flip ? -spriteRotation : spriteRotation;
        
		Rotate(flip ? -originalRotation : originalRotation);
		Position = new Vector2(flip ? -originalPosition.X : originalPosition.X, originalPosition.Y); 
		flipping = flipH;

		if (flipOnParentSprite) {
			flipH = flip;
		}
	}

	public bool GetFlipH() {
		return flipH;
	}
	
	public void SetFlipH(bool flipH) {
		this.flipH = flipH;
	}

	public ProjectileComponent GetProjectileComponent() {
		return _projectileComponent;
	}
}

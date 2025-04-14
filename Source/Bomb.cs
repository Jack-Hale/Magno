using Godot;
using System;

public partial class Bomb : RigidBody2D {
    [Export]
    private float damageAmount = 50;
    [Export]
    private float timerMaxTime = 2;
    private float timer = 0;
	private ItemComponent _itemComponent;
    private Explosion _explosion;
    private PhysicsBody2D itemOwner = null;
	private CharacterBody2D charOwner = null;
	private RigidBody2D rigidOwner = null;
    private MagneticComponent _magneticComponent;
    private bool exploded = false;
	private Player player;
    private bool primed = false;
    public override void _Ready() {
        _explosion = GetNode<Explosion>("Explosion");
        _explosion.SetDamageAmount(damageAmount);

        _magneticComponent = GetNode<MagneticComponent>("MagneticComponent");

        _itemComponent = GetNode<ItemComponent>("ItemComponent");
		
		_itemComponent.OnUseItemLeft += Explode;
		_itemComponent.OnUseItemRight += Explode;

		itemOwner = _itemComponent.GetItemOwner();

		player = _itemComponent.GetPlayer();
    }

    public override void _PhysicsProcess(double delta) {
        if (exploded && !_magneticComponent.IsBeingHeld()) {
            Owner = null;
            QueueFree();
        }

        if (timer > 0) {
            timer -= (float)delta;
        }

        if (timer <= 0 && primed) {
            Explode();
        }
    }

    public void Explode() {
        if (!exploded) {
            exploded = true;
            _magneticComponent.DetachFromMagnet();
            Explosion explosion = (Explosion) _explosion.Duplicate();
            GetParent().AddChild(explosion);
            explosion.GlobalPosition = GlobalPosition;
            explosion.Visible = true;
            explosion.ProcessMode = ProcessModeEnum.Inherit;
        }
    }

    public void StartTimer() {
        primed = true;
        timer = timerMaxTime;
    }

    public float GetTimer() {
        return timerMaxTime;
    }
}

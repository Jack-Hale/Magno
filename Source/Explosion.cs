using Godot;
using System;

public partial class Explosion : Node2D {
    private DamageComponent _damageComponent;
    private AnimationPlayer _animationPlayer;
    private Sprite2D _sprite;

    public override void _Ready() {
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        _damageComponent = GetNode<Area2D>("Area2D").GetNode<DamageComponent>("DamageComponent");
        _sprite = GetNode<Sprite2D>("Sprite2D");
        Random random = new();
        _sprite.Rotation = Mathf.Pi * 2 * (float) random.NextDouble();
        
        _animationPlayer.Play("explode");
    }

    public override void _PhysicsProcess(double delta) {
        if (!_animationPlayer.IsPlaying()) {
            QueueFree();
        }
    }

    public void SetDamageAmount(float amount) {
        _damageComponent.SetDamage(amount);
    }
}

using Godot;
using System;

public partial class Explosion : Node2D {
    private DamageComponent _damageComponent;
    private AnimationPlayer _animationPlayer;

    public override void _Ready() {
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        _damageComponent = GetNode<Area2D>("Area2D").GetNode<DamageComponent>("DamageComponent");
        
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

using Godot;
using System;

public partial class Turret : CharacterBody2D
{
	private ProjectileComponent _projectileComponent;
	private PathFindingComponent _pathFinding;
	private CharacterBody2D player;
    public override void _Ready() {
		_projectileComponent = GetNode<ProjectileComponent>("ProjectileComponent");
		_pathFinding = GetNode<PathFindingComponent>("PathFindingComponent");
		player = _pathFinding.GetPlayer();
    }

	public override void _PhysicsProcess(double delta) {
		Vector2 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		if (IsInGroup("CanSeePlayer") || IsInGroup("LookingForPlayer")) {
			_projectileComponent.LookAt(_pathFinding.GetLastDetectionPoint());
			_projectileComponent.Shoot();
		}

		Velocity = velocity;
		MoveAndSlide();
	}
}

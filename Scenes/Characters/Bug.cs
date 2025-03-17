using Godot;
using System;

public partial class Bug : CharacterBody2D
{
	public float maxSpeed = 100.0f;
	public float acceleration = 60.0f;

	public float jumpVelocity = -400.0f;
	private PathFindingComponent _pathFinding;
	private Sprite2D _sprite;
	private CharacterBody2D player;
	private Vector2 draw1 = Vector2.Zero;
	private Vector2 draw2 = Vector2.Zero;
	private Vector2 draw3 = Vector2.Zero;
	private Vector2 draw4 = Vector2.Zero;
	private Vector2 draw5 = Vector2.Zero;
	private Vector2 draw6 = Vector2.Zero;
	public override void _Ready() {
		_sprite = GetNode<Sprite2D>("Sprite2D");
		_pathFinding = GetNode<PathFindingComponent>("PathFindingComponent");
		player = _pathFinding.GetPlayer();
	}

    public override void _Draw() {
		DrawLine(ToLocal(draw1), ToLocal(draw2), Colors.Green);
        DrawLine(ToLocal(draw3), ToLocal(draw4), Colors.Green);
        DrawLine(ToLocal(draw5), ToLocal(draw6), Colors.Red);
    }

    public override void _PhysicsProcess(double delta) {
		Vector2 velocity = Velocity;

		if (IsInGroup("CanSeePlayer") || IsInGroup("LookingForPlayer")) {
			velocity = MoveTowardsVector(velocity, _pathFinding.GetLastDetectionPoint(), maxSpeed, (float)delta);
		} else {
			velocity = IdleBehaviour();
		}

		if (IsInGroup("LookingForPlayer")) {

		}

		velocity = AvoidWalls(velocity);

		// draw5 = GlobalPosition;
		// draw6 = GlobalPosition + velocity.Normalized() * 40;

		_sprite.Rotation = Mathf.Atan2(velocity.Y, velocity.X);

		Velocity = velocity;
		MoveAndSlide();
		QueueRedraw();
	}

	private Vector2 MoveTowardsVector(Vector2 velocity, Vector2 vector, float speed, float delta) {
		
		velocity = (vector - GlobalPosition).Normalized() * speed*100 * delta;
		// velocity = velocity.LimitLength(speed);

		return velocity;
	}

	// When moving, if character would brush up against a wall, it instead moves along it by a distance
	private Vector2 AvoidWalls(Vector2 velocity) {

		float avoidDistance = 60;
		float checkAngle = 30;
		float angleToTurn = 40;

		Vector2 start = GlobalPosition;
		Vector2 end = GlobalPosition + velocity.Normalized() * avoidDistance;

		float angleOffset = Mathf.DegToRad(checkAngle);
		Vector2 direction = start.DirectionTo(end);
		float distance = start.DistanceTo(end);

		Vector2 posDir = direction.Rotated(angleOffset);
		Vector2 negDir = direction.Rotated(-angleOffset);

		// draw1 = GlobalPosition;
		// draw2 = start + posDir * distance;

		// draw3 = GlobalPosition;
		// draw4 = start + negDir * distance;

		var posCheck = _pathFinding.FireRayCast(start, start + posDir * distance);
		var negCheck = _pathFinding.FireRayCast(start, start + negDir * distance);

		bool posTileFound = false;
		bool negTileFound = false;

		if (posCheck.Count > 0) {
			Node2D colliderPos = (Node2D) posCheck["collider"];
			if (colliderPos is TileMapLayer) {
				posTileFound = true;
			}
		} 

		if (negCheck.Count > 0) {
			Node2D colliderNeg = (Node2D) negCheck["collider"];
			if (colliderNeg is TileMapLayer) {
				negTileFound = true;
			}
		}

		if (posTileFound != negTileFound) {
			if (posTileFound) {
				velocity = velocity.Rotated(Mathf.DegToRad(-angleToTurn));
			}
			if (negTileFound) {
				velocity = velocity.Rotated(Mathf.DegToRad(angleToTurn));

			}
		}

		return velocity;
	}

	private Vector2 IdleBehaviour() {
		return Vector2.Zero;
	}
}

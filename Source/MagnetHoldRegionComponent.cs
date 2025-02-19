using Godot;
using System;

public partial class MagnetHoldRegionComponent : Area2D
{
	[Export]
	public Shape2D collisionShape { get; set; }
	private CollisionShape2D _collisionShapeNode;

    public override void _Ready()
    {
        _collisionShapeNode = GetNode<CollisionShape2D>("CollisionShape2D");
        if (_collisionShapeNode != null && collisionShape != null)
        {
            _collisionShapeNode.Shape = collisionShape;
        }
    }
}

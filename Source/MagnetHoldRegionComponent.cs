using Godot;
using System;

public partial class MagnetHoldRegionComponent : Area2D
{
	[Export]
	public Shape2D CollisionShape { get; set; }
	private CollisionShape2D _collisionShapeNode;

    public override void _Ready()
    {
        _collisionShapeNode = GetNode<CollisionShape2D>("CollisionShape2D");
        if (_collisionShapeNode != null && CollisionShape != null)
        {
            _collisionShapeNode.Shape = CollisionShape;
        }
    }
}

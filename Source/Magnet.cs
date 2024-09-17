using Godot;
using System;

public partial class Magnet : Area2D
{
	private bool ActivationStatus = false;
	private PhysicsBody2D EnteredBody;

	private PinJoint2D _joint;
	private float rotationFix = 0;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		_joint = GetNode<PinJoint2D>("FixedJoint2D");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {
		// GD.Print(_joint.GetChildCount());
		if (EnteredBody != null && ActivationStatus) {
			EnteredBody.SetCollisionMaskValue(2, false);
			_joint.NodeB = EnteredBody.GetPath();
		}
		if (!ActivationStatus) {
			
			_joint.NodeB = null;
		}
	}

    public override void _PhysicsProcess(double delta)
    {
        if (EnteredBody != null)
        {
			var angleToBody = (GlobalPosition - EnteredBody.GlobalPosition).Angle();
			EnteredBody.SetDeferred("rotation", angleToBody + rotationFix);

			GD.Print(GlobalPosition - EnteredBody.GlobalPosition);
        }
    }

    private void OnBodyEntered(Node2D body) {
		if (body.IsInGroup("Magnetic")) {
			EnteredBody = (PhysicsBody2D) body;
			
			var angleToBody = (GlobalPosition - EnteredBody.GlobalPosition).Angle();
			Node2D parent = (Node2D) GetParent();
			rotationFix = parent.GlobalRotation - angleToBody;
		}
	}

	private void OnBodyExited(Node2D body) {
		if (body == EnteredBody) {
			EnteredBody.SetCollisionMaskValue(2, true);
			EnteredBody = null;
		}
	}

	public void SetActivation(bool status) {
		ActivationStatus = status;
	}
}

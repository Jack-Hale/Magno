using Godot;
using System;

public partial class Magnet : Area2D
{
	private bool ActivationStatus = false;
	private PhysicsBody2D EnteredBody;
	private PinJoint2D _joint1;
	private PinJoint2D _joint2;
	private float rotationFix = 0;
	private bool canJoin = false;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		_joint1 = GetNode<PinJoint2D>("PinJoint2D_1");
		_joint2 = GetNode<PinJoint2D>("PinJoint2D_2");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {
		// GD.Print(canJoin);


		if (EnteredBody != null && ActivationStatus && canJoin) {
			_joint1.NodeB = EnteredBody.GetPath();
			_joint2.NodeB = EnteredBody.GetPath();
		}
		if (!ActivationStatus) {
			_joint1.NodeB = null;
			_joint2.NodeB = null;
		}
	}

    public override void _PhysicsProcess(double delta)
    {
        // if (EnteredBody != null)
        // {
		// 	var angleToBody = (GlobalPosition - EnteredBody.GlobalPosition).Angle();
		// 	EnteredBody.SetDeferred("rotation", angleToBody + rotationFix);
        // }
    }

    private void OnBodyEntered(Node2D body) {
		if (body.IsInGroup("Magnetic") && ActivationStatus) {
			EnteredBody = (PhysicsBody2D) body;
			
			// var angleToBody = (GlobalPosition - EnteredBody.GlobalPosition).Angle();
			// rotationFix = GlobalRotation - angleToBody;
		}
	}

	private void OnBodyExited(Node2D body) {
		if (body == EnteredBody) {
			
			EnteredBody = null;
		}
	}

	public void SetActivation(bool status) {
		ActivationStatus = status;
	}

	public void SetCanJoin(bool canJoin) {
		this.canJoin = canJoin;
	}
}

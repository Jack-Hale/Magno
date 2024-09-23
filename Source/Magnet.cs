using Godot;
using System;

public partial class Magnet : Area2D
{
	private bool activated = false;
	private bool pullMode = true;
	private bool canJoin;
	private PhysicsBody2D EnteredBody;
	private PinJoint2D _joint1;
	private PinJoint2D _joint2;
	private float rotationFix = 0;

	private RayCast2D _magnetBeam;
	
	private MagneticComponent attachedObject;
	
	private Sprite2D _magnetBeamSprite;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		_joint1 = GetNode<PinJoint2D>("PinJoint2D_1");
		_joint2 = GetNode<PinJoint2D>("PinJoint2D_2");
		
		_magnetBeam = GetNode<RayCast2D>("MagnetBeam");
		_magnetBeamSprite = _magnetBeam.GetNode<Sprite2D>("Sprite2D");

		if (_magnetBeam == null) {
			GD.Print("No Raycast found");
		}

		canJoin = pullMode;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {

		if (EnteredBody != null && activated && canJoin) {
			_joint1.NodeB = EnteredBody.GetPath();
			_joint2.NodeB = EnteredBody.GetPath();
		}
		if (!activated) {
			_joint1.NodeB = null;
			_joint2.NodeB = null;
		}
	}

    public override void _PhysicsProcess(double delta) {
		if (activated) {
			if (_magnetBeam.IsColliding()) {

				// Check if the object in the beam is in the Magnetic group.
				Node Object = (Node) _magnetBeam.GetCollider();
				if (Object.IsInGroup("Magnetic")) {
					MagneticComponent newObject = (MagneticComponent) Object.FindChild("MagneticComponent");

					// Checking if the object is a different object to the previous call. 
					// If it is different, dettach the old one
					if (attachedObject != null && newObject != attachedObject) {
						attachedObject.Dettach();
					}
					attachedObject = newObject;
					attachedObject.ForceObject(_magnetBeam.GetCollisionPoint(), GlobalPosition, _magnetBeam.TargetPosition.X, pullMode);
				}
			} else {
				if (attachedObject != null) {
					attachedObject.Dettach();
					attachedObject = null;
				}
			}
		} else {
			// Dettach the object when deactivating magnet
			if (attachedObject != null) {
				attachedObject.Dettach();
				attachedObject = null;
			}
		}
    }

    private void OnBodyEntered(Node2D body) {
		if (body.IsInGroup("Magnetic") && activated) {
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
		activated = status;
		_magnetBeamSprite.Visible = status;
	}

	public void SetPullMode(bool canJoin) {
		pullMode = canJoin;
		this.canJoin = canJoin;
	}

	public bool GetPullMode() {
		return pullMode;
	}
}

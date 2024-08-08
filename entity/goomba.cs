using Godot;
using System;

public partial class goomba : CharacterBody2D
{
	public const float Speed = 1000.0f;
	public const float JumpVelocity = -400.0f;
	public float direction = 1;
	public poutre wisk_left,wisk_right;

	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	public override void _Ready() {
		wisk_left = GetNode<poutre>("wisk_left");
		wisk_right = GetNode<poutre>("wisk_right");
	}

	public override void _PhysicsProcess(double delta) {	
		GD.Print(wisk_left.colliding," ",wisk_right.colliding);
		Vector2 velocity = Velocity;
		if (!IsOnFloor()) {
			velocity.Y += gravity*(float)delta;
		} else {
			velocity.X = direction*Speed*(float)delta;
		}

		Velocity = velocity;

		bool collided = MoveAndSlide();
		if (collided) {
			float collision_angle = GetLastSlideCollision().GetAngle();
			if (Mathf.Abs(Mathf.Abs(collision_angle) - MathF.PI*0.5) < Mathf.Pi*0.2) {
				GD.Print(collision_angle);
				direction = -1*direction;
			}	
		}
		if (direction > 0 && !wisk_right.colliding) {
			direction = -1*direction;
		} else if (direction < 0 && !wisk_left.colliding) {
			direction = -1*direction;
		}
		//GD.Print(Position);
	}
}

using Godot;
using System;

public partial class goomba : CharacterBody2D
{
	public const float Speed = 1000.0f;
	public const float JumpVelocity = -400.0f;
	public float direction = 1;

	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	public override void _PhysicsProcess(double delta)
	{	
		Vector2 velocity = Velocity;
		if (!IsOnFloor()) {
			velocity.Y += gravity*0.25f;
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
		//GD.Print(Position);
	}
}

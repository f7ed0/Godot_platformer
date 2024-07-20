using Godot;
using System;

public partial class goomba : CharacterBody2D
{
	public const float Speed = 300.0f;
	public const float JumpVelocity = -400.0f;

	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	public override void _PhysicsProcess(double delta)
	{	
		if (!IsOnFloor()) {
			Velocity = new Vector2(0,gravity*0.25f);
		}
		
		MoveAndSlide();
		//GD.Print(Position);
	}
}

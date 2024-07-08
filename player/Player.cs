using Godot;
using System;
using System.Numerics;

public partial class Player : CharacterBody2D
{
	public const float Speed = 300.0f;
	public const float JumpVelocity = 400.0f;

	public float direction;

	public Camera2D cam;
	public AnimatedSprite2D sprite;


	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	public override void _Ready()
	{
		cam = GetNode<Camera2D>("PlayerCamera");
		sprite = GetNode<AnimatedSprite2D>("PlayerSprite");
	}

	public override void _Process(double delta) {
		if (Mathf.Abs(direction) > 0.01) {
			this.sprite.Play("default");
		} else {
			this.sprite.Stop();
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Godot.Vector2 velocity = Velocity;

		// Add the gravity.
		//if (!IsOnFloor())
		//	velocity.Y += gravity * (float)delta;

		// Handle Jump.
		//if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
		//	velocity.Y = -JumpVelocity;

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		this.direction = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
		if (direction != 0)
		{
			velocity.X = this.direction * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
		}

		Velocity = velocity;

		Godot.Vector2 velocity_normalized = velocity.Normalized();
		float x = velocity_normalized.X*160;
		float y = velocity_normalized.Y*90;

		
		float cam_pos_x = Mathf.MoveToward(this.cam.Position.X, x, Mathf.Sqrt(Math.Abs(this.cam.Position.X-x)));
		float cam_pos_y = Mathf.MoveToward(this.cam.Position.Y, -y, Mathf.Sqrt(Math.Abs(this.cam.Position.Y+y)));

		this.cam.Position = new Godot.Vector2(cam_pos_x,cam_pos_y);
		if (velocity.Length() > 160) {
			this.cam.Zoom = new Godot.Vector2(1.1f/MathF.Log10(velocity.Length()),1.2f/MathF.Log10(velocity.Length()));
		} else {
			this.cam.Zoom = new Godot.Vector2(0.5f,0.5f);
		}
		GD.Print(this.cam.Zoom);
		

		MoveAndSlide();
	}
}

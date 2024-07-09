using Godot;
using System;
using System.Reflection.Metadata;

public partial class Player : CharacterBody2D
{
	public const float Speed = 500.0f;
	public const float JumpVelocity = 550.0f;

	public float direction;
	public Camera2D cam;
	public AnimatedSprite2D sprite;
	public bool is_jumping;
	public double jump_timer;


	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	public override void _Ready()
	{
		cam = GetNode<Camera2D>("PlayerCamera");
		sprite = GetNode<AnimatedSprite2D>("PlayerSprite");
		is_jumping = false;
	}

	public override void _Process(double delta) {
		if (Mathf.Abs(direction) > 0.01) {
			if (direction < 0) {
				this.sprite.Play("walking");
				this.sprite.FlipH = true;
			} else {
				this.sprite.Play("walking");
				this.sprite.FlipH = false;
			}
		} else {
			this.sprite.Play("idle");
			this.sprite.Stop();
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Rect2 view = GetViewportRect();

		float new_aspect_ratio = 0.5f*(view.Size.Y/720);

		cam.Zoom = new Vector2(new_aspect_ratio,new_aspect_ratio);

		Vector2 velocity = Velocity;

		// Add the gravity.
		// Handle Jump.
		velocity.Y += HandleJump(delta);

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


		MoveAndSlide();

		// ------------CAMERA HANDLING-----------------

		Vector2 velocity_normalized = velocity.Normalized();

		float x = velocity_normalized.X*300;
		float y = Mathf.Min(0, -670-this.Position.Y);

		
		float cam_pos_x = Mathf.MoveToward(this.cam.Position.X, x, Mathf.Sqrt(Math.Abs(this.cam.Position.X-x))*(float) delta*20);
		float cam_pos_y = Mathf.MoveToward(this.cam.Position.Y, y, Mathf.Sqrt(Math.Abs(this.cam.Position.Y-y))*(float) delta*10);
		
		this.cam.Position = new Vector2(cam_pos_x,y);
	}

	public float HandleJump(double delta) {
		float velocity = 0;
		jump_timer -= delta;
		if (!IsOnFloor()) {
			velocity += gravity * (float)delta;
		}
		if (Input.IsActionJustPressed("jump") && IsOnFloor()) {
			GD.Print("IN");
			velocity = -JumpVelocity;
			is_jumping = true;
			this.jump_timer = 0.2;
		}
		if (Input.IsActionPressed("jump") && is_jumping && jump_timer > 0) {
			if (velocity > -0.005f*JumpVelocity) {
				velocity = -0.005f*JumpVelocity;
			}
		} else if(!Input.IsActionPressed("jump") && is_jumping) {
			GD.Print("OUT");
			is_jumping = false;
		}
		return velocity;
	}
}

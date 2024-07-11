using Godot;
using System;

public partial class Player : CharacterBody2D
{
	public const float Speed = 200.0f;
	public const float JumpVelocity = 250.0f;

	public float direction;
	public float default_zoom;
	public Camera2D cam;
	public AnimatedSprite2D sprite;
	public bool is_jumping;
	public double jump_timer;
	public double was_on_floor;
	public AnimationPlayer animation;

	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	public override void _Ready()
	{
		cam = GetNode<Camera2D>("PlayerCamera");
		sprite = GetNode<AnimatedSprite2D>("PlayerSprite");
		animation = GetNode<AnimationPlayer>("AnimationPlayer");
		default_zoom = cam.Zoom.X;
		is_jumping = false;
		this.sprite.Play();
	}

	public override void _Process(double delta) {
		if (!IsOnFloor()) {
			this.animation.Play("jump");
		} else if (Input.IsActionJustPressed("slide")) {
			this.animation.Play("slide_start");
			this.animation.Queue("slide");
		} else if(Input.IsActionJustReleased("slide")) {

		} else if (!Input.IsActionPressed("slide")){
			if(Mathf.Abs(direction) > 0.01) {
				if (direction < 0) {
					this.animation.Play("walking");
					this.sprite.FlipH = true;
				} else {
					this.animation.Play("walking");
					this.sprite.FlipH = false;
				}
			} else {
				this.animation.Play("idle");
			}
		} 
	}

	public override void _PhysicsProcess(double delta)
	{
		Rect2 view = GetViewportRect();

		//float new_aspect_ratio = default_zoom*(view.Size.Y/720);

		//cam.Zoom = new Vector2(new_aspect_ratio,new_aspect_ratio);

		Vector2 velocity = Velocity;

		// Add the gravity.
		// Handle Jump.
		velocity.Y += HandleJump(delta);

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		this.direction = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
		if(Input.IsActionPressed("slide")) {
			if(Input.IsActionJustPressed("slide")) {
				velocity.X = Mathf.Sign(velocity.X)*Speed*1.2f;
				GD.Print("LEZGONGUE");
			} else {
				velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed*0.4f);
			}
		} else {
			if (direction != 0) {
				velocity.X = this.direction * Speed;
			} else {
				velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			}
		}


		Velocity = velocity;


		MoveAndSlide();

		//GetNode<Label>("hud_position_label").Text = "X:"+Position.X.ToString()+" Y:"+Position.Y.ToString();

		// ------------CAMERA HANDLING-----------------

		Vector2 velocity_normalized = velocity.Normalized();

		float x = velocity_normalized.X*(250/default_zoom);
		float y = Mathf.Min(0, -100-Position.Y);

		
		float cam_pos_x = Mathf.MoveToward(this.cam.Position.X, x, Mathf.Sqrt(Math.Abs(this.cam.Position.X-x))*(float) delta*5);
		float cam_pos_y = Mathf.MoveToward(this.cam.Position.Y, y, Mathf.Sqrt(Math.Abs(this.cam.Position.Y-y))*(float) delta*5);
		
		this.cam.Position = new Vector2(cam_pos_x,cam_pos_y);

		// ----------------------------------------------
	}

	public float HandleJump(double delta) {
		float velocity = 0;
		jump_timer -= delta;
		if (!IsOnFloor()) {
			velocity += gravity * (float)delta;
			was_on_floor += delta;
		} else {
			was_on_floor = 0;
		}
		if (Input.IsActionJustPressed("jump") && (IsOnFloor() || was_on_floor < 0.2)) {
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

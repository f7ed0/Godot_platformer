using Godot;
using System;

public partial class Player : CharacterBody2D
{
	public const float Speed = 175.0f;
	public const float JumpVelocity = 400.0f;

	public float direction;
	public float default_zoom;
	public Camera2D cam;
	public AnimatedSprite2D sprite;
	public bool is_jumping;
	public double jump_timer;
	public double was_on_floor;
	public AnimationPlayer animation;
	public poutre ptre;
	public poutre[] wallgrab;
	public bool is_sliding;
	public double sliding_time;
	public bool launch_me;
	public int bonus_jump_count;

	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	public override void _Ready()
	{
		cam = GetNode<Camera2D>("PlayerCamera");
		sprite = GetNode<AnimatedSprite2D>("PlayerSprite");
		animation = GetNode<AnimationPlayer>("AnimationPlayer");
		wallgrab =  new poutre[2];
		wallgrab[0] = GetNode<poutre>("wallgrab_hitbox_l");
		wallgrab[1] = GetNode<poutre>("wallgrab_hitbox_r");
		ptre = GetNode<poutre>("poutre");
		default_zoom = cam.Zoom.X;
		is_jumping = false;
		this.sprite.Play();
	}

	public bool isCrouching() {
		return Input.IsActionPressed("crouch") || ptre.colliding;
	}

	public bool isWallHangingLeft() {
		return wallgrab[0].colliding && !IsOnFloor() && Velocity.Y >= 0;
	}

	public bool isWallHangingRight() {
		return wallgrab[1].colliding && !IsOnFloor() && Velocity.Y >= 0;
	}

	public bool isWallHanging() => isWallHangingLeft() || isWallHangingRight();

	public void correctSpritePosition() {
		if (this.is_sliding) {
			this.sprite.Position = this.sprite.FlipH ? new Vector2(4, this.sprite.Position.Y) : new Vector2(2, this.sprite.Position.Y);
		} else if (this.isWallHanging()) {
			this.sprite.Position = this.sprite.FlipH ? new Vector2(4, this.sprite.Position.Y) : new Vector2(-2, this.sprite.Position.Y);
		} else {
			this.sprite.Position = this.sprite.FlipH ? new Vector2(-8, this.sprite.Position.Y) : new Vector2(2, this.sprite.Position.Y);
		}
	}

	public override void _Process(double delta) {
		sliding_time -= delta;
		if (isWallHanging()) {
			this.animation.Play("wall_hang");
			if (isWallHangingRight()) {
				this.sprite.FlipH = false;
			} else {
				this.sprite.FlipH = true;
			}
		} else if (!IsOnFloor()) {
			if( is_jumping ){
				this.animation.Play("jump");
			}else {
				this.animation.Play("drop");
			}
			is_sliding = false;
		} else {
			if (Input.IsActionJustPressed("slide") && !is_sliding && Mathf.Abs(Velocity.X) > Speed*0.5f ) {
				
				this.animation.Play("slide_start");
				this.animation.Queue("slide");
				launch_me = true;
				is_sliding = true;
				sliding_time = 0.35f;
				
			}
			if (!is_sliding){
				if (Mathf.Abs(direction) > 0.01) {
					if (isCrouching()) {
						this.animation.Play("crouch_walk");
					} else {
						this.animation.Play("walking");
					}
					if (direction < 0) {
						this.sprite.FlipH = true;
					} else {
						this.sprite.FlipH = false;
					}
				} else {
					if (isCrouching()) {
						this.animation.Play("crouch");
					} else {
						this.animation.Play("idle");
					}
				}
			} else if((!Input.IsActionPressed("slide") && sliding_time <= 0) || Mathf.Abs(Velocity.X) < Speed*0.2f) {
				is_sliding = false;
			} 
		} 
		correctSpritePosition();
	}

	public override void _PhysicsProcess(double delta) {
		Rect2 view = GetViewportRect();

		//float new_aspect_ratio = default_zoom*(view.Size.Y/720);

		//cam.Zoom = new Vector2(new_aspect_ratio,new_aspect_ratio);

		Vector2 velocity = Velocity;

		// Add the gravity.
		// Handle Jump.
		velocity = HandleJump(delta,Velocity);

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		this.direction = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");

		if(launch_me) {
			velocity.X += (this.sprite.FlipH ? -1 : 1)*Speed*0.5f;
			launch_me = false;
		} 

		if(is_sliding) {
			velocity.X = Mathf.MoveToward(velocity.X, 0, Speed*0.9f*((float) delta));
		} else {
			float real_speed = Speed;
			float speed_cap = Speed;
			if (!IsOnFloor()) {
				real_speed = Speed*0.1f;
			}
			if (isCrouching()) {
				speed_cap = Speed*0.7f;
			}
			if (direction != 0) {
				velocity.X =  Mathf.MoveToward(velocity.X, this.direction * speed_cap, real_speed*(float) delta*60f);
			} else {
				velocity.X = Mathf.MoveToward(velocity.X, 0, real_speed*(float) delta*60f);
			}
		}

		Velocity = velocity;

		MoveAndSlide();

		//GetNode<Label>("hud_position_label").Text = "X:"+Position.X.ToString()+" Y:"+Position.Y.ToString();

		// ------------CAMERA HANDLING-----------------

		Vector2 velocity_normalized = velocity.Normalized();

		float x = velocity_normalized.X*(250/default_zoom);
		float y = Mathf.Min(-velocity_normalized.Y*(350/default_zoom), -100-Position.Y);

		
		float cam_pos_x = Mathf.MoveToward(this.cam.Position.X, x, Mathf.Sqrt(Math.Abs(this.cam.Position.X-x))*(float) delta*5);
		float cam_pos_y = Mathf.MoveToward(this.cam.Position.Y, y, Mathf.Sqrt(Math.Abs(this.cam.Position.Y-y))*(float) delta*7);
		
		this.cam.Position = new Vector2(cam_pos_x,cam_pos_y);

		// ----------------------------------------------
	}

	public Vector2 HandleJump(double delta, Vector2 velocity) {
		jump_timer -= delta;
		if (isWallHanging()) {
			velocity.Y = 0;
			was_on_floor += delta;
		} else if (!IsOnFloor()) {
			velocity.Y += gravity * (float)delta;
			was_on_floor += delta;
		} else {
			was_on_floor = 0;
			bonus_jump_count = 0;
		}
		if(isCrouching()) return velocity;
		if (Input.IsActionJustPressed("jump") && (IsOnFloor() || was_on_floor < 0.1)) {
			if(is_sliding) {
				velocity.Y = -0.8f*JumpVelocity;
				velocity.X = (this.sprite.FlipH ? -1 : 1)*Speed*3.75f;
				this.jump_timer = 0;
			} else {
				velocity.Y = -JumpVelocity;
				this.jump_timer = 0.2;
			}
			is_jumping = true;

		} else if (Input.IsActionJustPressed("jump") && bonus_jump_count > 0 && was_on_floor > 0.2) {
			velocity.Y = -0.7f*JumpVelocity;
			this.jump_timer = 0;
			is_jumping = true;
			bonus_jump_count --;
		}
		if (Input.IsActionPressed("jump") && is_jumping && jump_timer > 0) {
			if (velocity.Y > -0.5f*JumpVelocity) {
				velocity.Y = -0.5f*JumpVelocity;
			}
		} else if(is_jumping && jump_timer < -0.2) {
			is_jumping = false;
		}
		return velocity;
	}
}

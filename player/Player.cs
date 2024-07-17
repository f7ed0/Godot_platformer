using Godot;
using System;

public enum PlayerState
{
	Idle,Walking,Jumping,Crouched,CrouchWalk,Sliding,Falling,WallHanging
}

public partial class Player : CharacterBody2D
{
	public const float Speed = 175.0f;
	public const float JumpVelocity = 350.0f;
	public PlayerState playerState = PlayerState.Idle;
	public float direction;
	public float default_zoom;
	public Camera2D cam;
	public AnimatedSprite2D sprite;
	public double was_on_floor;
	public AnimationPlayer animation;
	public poutre ptre;
	public poutre[] wallgrab;
	public bool is_sliding;
	public double sliding_time;
	public bool launch_me;
	public int bonus_jump_count;
	public CanvasLayer hud;

	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float ground_gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
	public float jump_gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle()*0.25f;
	
	public Vector2 HandleGroundedPhysics(Vector2 velocity, double delta) {
		velocity.Y += ground_gravity * (float) delta;
		return velocity;
	}

	public Vector2 HandleAerialPhysics(Vector2 velocity,double delta) {
		velocity.Y += (Input.IsActionPressed("jump") ? 0.9f : 1f ) * jump_gravity * (float) delta;
		return velocity;
	}

	public float getDirection() {
		return Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");;
	}

	public void updateWasOnFloor(double delta) {
		if (IsOnFloor()) {
			was_on_floor = 0;
		} else {
			was_on_floor += delta;
		}
	}

	// ------------------ FALLING ------------------------------
	public Vector2 HandleFalling_Physics(Vector2 velocity, double delta) {
		if (IsOnFloor()) {
			playerState = PlayerState.Idle;
			velocity = HandleIdling_Pysics(velocity, delta);
			return velocity;
		}
		if (was_on_floor < 0.2 && Input.IsActionJustPressed("jump")) {
			playerState = PlayerState.Jumping;
			velocity.Y = -JumpVelocity;
			return velocity;
		}
		velocity = HandleAerialPhysics(velocity, delta);
		return velocity;
	}

	public void HandleFalling() {
		animation.Play("drop");
	}
	// ---------------------------------------------------------

	// ------------------ JUMPING ------------------------------
	public Vector2 HandleJumping_Physics(Vector2 velocity, double delta) {
		// Player Falling
		if (velocity.Y > 0) {
			playerState = PlayerState.Falling;
			velocity = HandleFalling_Physics(velocity, delta);
			return velocity;
		}
		if (IsOnFloor()) {
			playerState = PlayerState.Idle;
			velocity = HandleIdling_Pysics(velocity, delta);
			return velocity;
		}
		// 
		velocity.X = Mathf.MoveToward(velocity.X, getDirection()*Speed, Speed*0.1f*(float) delta*30f);
		velocity = HandleAerialPhysics(velocity, delta);
		return velocity;
	}

	public void HandleJumping() {
		animation.Play("jump");
	}
	// ----------------------------------------------------------

	// -------------------- IDLE --------------------------------
	public Vector2 HandleIdling_Pysics(Vector2 velocity, double delta) {
		// Player use left or right
		if ( getDirection() != 0 ) {
			playerState = PlayerState.Walking;
			return HandleWalking_Pysics(velocity, delta);
		}
		// Player use jump
		else if( Input.IsActionJustPressed("jump")) {
			playerState = PlayerState.Jumping;
			velocity.Y = -JumpVelocity;
			return velocity;
		}
		// Player crouch
		else if ( Input.IsActionJustPressed("crouch") ) {
			// TODO
		}
		// Player fall
		else if ( !IsOnFloor() ) {
			// TODO
		}
		velocity.X =  Mathf.MoveToward(velocity.X, 0, Speed*(float) delta*30f);
		return HandleGroundedPhysics(velocity, delta);
	}

	public void HandleIdling() {
		animation.Play("idle");
	}
	// ------------------------------------------------------------------------

	// -------------------- WALKING -------------------------------------------
	public Vector2 HandleWalking_Pysics(Vector2 velocity, double delta) {
		float direction = getDirection();
		if (direction == 0) {
			playerState = PlayerState.Idle;
			return HandleIdling_Pysics(velocity,delta);
		}
		if ( Input.IsActionJustPressed("jump") ) {
			playerState = PlayerState.Jumping;
			velocity.Y = -JumpVelocity;
			velocity.X = Mathf.MoveToward(velocity.X, direction*Speed, Speed*(float) delta*30f);
			return velocity;
		}
		if ( !IsOnFloor() ) {
			playerState = PlayerState.Falling;
			return HandleFalling_Physics(velocity,delta);
		}
		velocity.X = Mathf.MoveToward(velocity.X, direction*Speed, Speed*(float) delta*30f);
		return velocity;
	}

	public void HandleWalking() {
		if (Math.Abs(Velocity.X) > 0) {
			animation.Play("walking");
		} else {
			animation.Play("idle");
		}
		
		sprite.FlipH = getDirection() < 0;
	}
	// ------------------------------------------------------------------------

	public override void _Ready() {
		cam = GetNode<Camera2D>("PlayerCamera");
		sprite = GetNode<AnimatedSprite2D>("PlayerSprite");
		animation = GetNode<AnimationPlayer>("AnimationPlayer");
		wallgrab =  new poutre[2];
		wallgrab[0] = GetNode<poutre>("wallgrab_hitbox_l");
		wallgrab[1] = GetNode<poutre>("wallgrab_hitbox_r");
		//hud = GetNode<CanvasLayer>("/root/autoload/hud");
		ptre = GetNode<poutre>("poutre");
		default_zoom = cam.Zoom.X;
		sprite.Play();
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

	public bool isJumping() {
		return !IsOnFloor() && Input.IsActionPressed("jump");
	}

	public bool isWallHanging() => isWallHangingLeft() || isWallHangingRight();

	public void correctSpritePosition() {
		if (is_sliding) {
			sprite.Position = sprite.FlipH ? new Vector2(4, sprite.Position.Y) : new Vector2(2, sprite.Position.Y);
		} else if (isWallHanging()) {
			sprite.Position = sprite.FlipH ? new Vector2(-4, sprite.Position.Y) : new Vector2(-2, sprite.Position.Y);
		} else {
			sprite.Position = sprite.FlipH ? new Vector2(-8, sprite.Position.Y) : new Vector2(2, sprite.Position.Y);
		}
	}

	public override void _Process(double delta) {


		switch (playerState) {
			case PlayerState.Idle :
				HandleIdling();
				break;
			case PlayerState.Jumping :
				HandleJumping();
				break;
			case PlayerState.Falling :
				HandleFalling();
				break;
			case PlayerState.Walking :
				HandleWalking();
				break;
		}
		
		return;
		sliding_time -= delta;
		if (isWallHanging()) {
			animation.Play("wall_hang");
			if (isWallHangingRight()) {
				sprite.FlipH = false;
			} else {
				sprite.FlipH = true;
			}
		} else if (!IsOnFloor()) {
			if( isJumping() ){
				animation.Play("jump");
			}else {
				animation.Play("drop");
			}
			if (was_on_floor > 0.2) {
				is_sliding = false;
			}
		} else {
			if (Input.IsActionJustPressed("slide") && !is_sliding && Mathf.Abs(Velocity.X) > Speed*0.5f ) {
				
				animation.Play("slide_start");
				animation.Queue("slide");
				launch_me = true;
				is_sliding = true;
				sliding_time = 0.35f;
				
			}
			if (!is_sliding){
				if (Mathf.Abs(direction) > 0.01) {
					if (isCrouching()) {
						animation.Play("crouch_walk");
					} else {
						animation.Play("walking");
					}
					if (direction < 0) {
						sprite.FlipH = true;
					} else {
						sprite.FlipH = false;
					}
				} else {
					if (isCrouching()) {
						animation.Play("crouch");
					} else {
						animation.Play("idle");
					}
				}
			}

		} 
		if((!Input.IsActionPressed("slide") && sliding_time <= 0) || Mathf.Abs(Velocity.X) < Speed*0.2f) {
			is_sliding = false;
		}  
		if (isJumping()) {
			is_sliding = false;
		}
		correctSpritePosition();
	}

	public override void _PhysicsProcess(double delta) {
		Rect2 view = GetViewportRect();

		updateWasOnFloor(delta);
		//float new_aspect_ratio = default_zoom*(view.Size.Y/720);

		//cam.Zoom = new Vector2(new_aspect_ratio,new_aspect_ratio);

		Vector2 velocity = Velocity;

		//velocity += GetGravityAction(delta);

		switch(playerState) {
			case PlayerState.Idle :
				velocity = HandleIdling_Pysics(velocity, delta);
				break;
			case PlayerState.Jumping :
				velocity = HandleJumping_Physics(velocity, delta);
				break;
			case PlayerState.Falling :
				velocity = HandleFalling_Physics(velocity, delta);
				break;
			case PlayerState.Walking :
				velocity = HandleWalking_Pysics(velocity, delta);
				break;
		}

		Velocity = velocity;

		MoveAndSlide();

		return;
		// Add the gravity.
		// Handle Jump.
		velocity = HandleJump(delta,Velocity);

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		direction = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");

		if(launch_me) {
			velocity.X += (sprite.FlipH ? -1 : 1)*Speed*0.5f;
			launch_me = false;
		} 

		if(is_sliding) {
			velocity.X = Mathf.MoveToward(velocity.X, 0, Speed*0.9f*((float) delta));
		} else if (!isWallHanging()) {
			float real_speed = Speed;
			float speed_cap = Speed;
			if (!IsOnFloor()) {
				real_speed = Speed*0.1f;
			}
			if (isCrouching()) {
				speed_cap = Speed*0.7f;
			}
			if (direction != 0) {
				velocity.X =  Mathf.MoveToward(velocity.X, direction * speed_cap, real_speed*(float) delta*60f);
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
		float y = Mathf.Min(0, -100-Position.Y);

		
		float cam_pos_x = Mathf.MoveToward(cam.Position.X, x, Mathf.Sqrt(Math.Abs(cam.Position.X-x))*(float) delta*5);
		float cam_pos_y = y;
		
		cam.Position = new Vector2(cam_pos_x,cam_pos_y);

		// ----------------------------------------------
	}

	public Vector2 GetGravityAction(double delta) {
		Vector2 velocity =  new Vector2();
		if (isWallHanging()) {
			velocity.Y = 0;
			was_on_floor += delta;
		} else if (!IsOnFloor()) {
			velocity.Y += ( Input.IsActionPressed("jump") ? jump_gravity*0.9f : jump_gravity) * (float)delta;
			was_on_floor += delta;
		} else {
			velocity.Y += ground_gravity * (float)delta;
			was_on_floor = 0;
			bonus_jump_count = 0;
		}
		return velocity;
	}

	public Vector2 HandleJump(double delta, Vector2 velocity) {
		if (isWallHanging()) {
			velocity.Y = 0;
			was_on_floor += delta;
		} else if (!IsOnFloor()) {
			velocity.Y += ( Input.IsActionPressed("jump") ? jump_gravity*0.9f : jump_gravity) * (float)delta;
			was_on_floor += delta;
		} else {
			velocity.Y += ground_gravity * (float)delta;
			was_on_floor = 0;
			bonus_jump_count = 0;
		}
		if(isCrouching()) return velocity;
		if (Input.IsActionJustPressed("jump") && (IsOnFloor() || was_on_floor < 0.1)) {
			if(is_sliding) {
				velocity.Y = -0.6f*JumpVelocity;
				velocity.X = (sprite.FlipH ? -1 : 1)*Speed*3.75f;
			} else {
				velocity.Y = -JumpVelocity;
			}
		} else if (Input.IsActionJustPressed("jump") && bonus_jump_count > 0 && was_on_floor > 0.2) {
			velocity.Y = -0.7f*JumpVelocity;
			bonus_jump_count --;
		} else if (Input.IsActionJustPressed("jump") && isWallHanging()) {
			
			if ((direction > 0 && isWallHangingRight()) || (direction < 0 && isWallHangingLeft())) {
				velocity.Y = -1.2f*JumpVelocity;
				velocity.X = (isWallHangingLeft() ? 1 : -1)*Speed*0.75f;
			} else {
				velocity.Y = -JumpVelocity;
				velocity.X = (isWallHangingLeft() ? 1 : -1)*Speed*2f;
				sprite.FlipH = !sprite.FlipH;
			}
		}
		return velocity;
	}

	private void _on_hurtbox_area_entered(Area2D area)
	{
		if (area.GetCollisionLayerValue(9)) {
			Position = new Vector2(0,-100);
		}
	}
}




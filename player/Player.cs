using Godot;
using System;

public enum PlayerState
{
	Idle,Walking,Jumping,Crouched,CrouchWalk,Sliding,Falling,WallHanging,NULL
}

public partial class Player : CharacterBody2D
{
	public const float Speed = 175.0f;
	public const float JumpVelocity = 350.0f;

	public PlayerState playerState = PlayerState.Idle;
	public PlayerState oldPlayerState = PlayerState.NULL;

	public float default_zoom;
	public Camera2D cam;
	public AnimatedSprite2D sprite;
	public double was_on_floor;
	public AnimationPlayer animation;
	public poutre ptre;
	public poutre[] wallgrab;
	public double sliding_time;

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
		if ((CanWallHangLeft() && Input.IsActionPressed("move_left")) || (CanWallHangRight() && Input.IsActionPressed("move_right"))) {
			playerState = PlayerState.WallHanging;
			return HandleWallHanging_Pysics(velocity,delta);
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
		else if ( Input.IsActionJustPressed("crouch") || ptre.colliding) {
			playerState = PlayerState.Crouched;
			return HandleCrouching_Pysics(velocity,delta);
		}
		// Player fall
		else if ( !IsOnFloor() ) {
			playerState = PlayerState.Falling;
			return HandleFalling_Physics(velocity,delta);
		}
		velocity.X =  Mathf.MoveToward(velocity.X, 0, Speed*(float) delta*30f);
		return HandleGroundedPhysics(velocity, delta);
	}

	public void HandleIdling() {
		animation.Play("idle");
	}
	// ------------------------------------------------------------------------

	// -------------------- SLIDING -------------------------------------------
	public Vector2 HandleSliding_Pysics(Vector2 velocity, double delta) {
		sliding_time += delta;
		if ( (!Input.IsActionPressed("slide") && sliding_time > 0.4f)|| Mathf.Abs(velocity.X) < 0.3*Speed ) {
			playerState = PlayerState.Idle;
			return HandleIdling_Pysics(velocity, delta);
		}
		if ( Input.IsActionJustPressed("jump") ) {
			playerState = PlayerState.Jumping;
			velocity.Y = -0.6f*JumpVelocity;
			velocity.X = (sprite.FlipH ? -1 : 1)*Speed*3f;
			return velocity;
		}
		if (was_on_floor > 0.1f) {
			playerState = PlayerState.Falling;
			return HandleFalling_Physics(velocity,delta);
		} 
		velocity.X = Mathf.MoveToward(velocity.X, 0, Speed*0.9f*((float) delta));
		if ( !IsOnFloor() ) {
			return HandleAerialPhysics(velocity,delta);
		}
		return HandleGroundedPhysics(velocity,delta);
	}

	public void HandleSliding() {
		animation.Play("slide_start");
		animation.Queue("slide");
	}
	// ------------------------------------------------------------------------

	// -------------------- CROUCHING -----------------------------------------
	public Vector2 HandleCrouching_Pysics(Vector2 velocity, double delta) {
		if (!Input.IsActionPressed("crouch") && !ptre.colliding) {
			playerState = PlayerState.Idle;
			return HandleIdling_Pysics(velocity,delta);
		}
		if (getDirection() != 0) {
			playerState = PlayerState.CrouchWalk;
			return HandleCrouchWalk_Pysics(velocity,delta);
		}
		velocity.X =  Mathf.MoveToward(velocity.X, 0, Speed*(float) delta*30f);
		return HandleGroundedPhysics(velocity,delta);
	}

	public void HandleCrouching() {
		animation.Play("crouch");
	}
	// ------------------------------------------------------------------------

	// -------------------- CROUCHWALK ----------------------------------------
	public Vector2 HandleCrouchWalk_Pysics(Vector2 velocity, double delta) {
		float direction = getDirection();
		if (!Input.IsActionPressed("crouch") && !ptre.colliding) {
			playerState = PlayerState.Idle;
			return HandleIdling_Pysics(velocity,delta);
		}
		if (direction == 0) {
			playerState = PlayerState.Crouched;
			HandleCrouching_Pysics(velocity, delta);
		}
		velocity.X = Mathf.MoveToward(velocity.X, direction*Speed*0.7f, Speed*0.7f*(float) delta*30f);
		return HandleGroundedPhysics(velocity,delta);
	}

	public void HandleCrouchWalk() {
		if (oldPlayerState == PlayerState.CrouchWalk) {
			oldPlayerState = PlayerState.NULL;
		}
		if (Math.Abs(Velocity.X) > 0) {
			animation.Play("crouch_walk");
		} else {
			animation.Play("crouch");
		}
		sprite.FlipH = getDirection() < 0;
	}
	// ------------------------------------------------------------------------

	// -------------------- WALKING -------------------------------------------
	public Vector2 HandleWalking_Pysics(Vector2 velocity, double delta) {
		if (oldPlayerState == PlayerState.Walking) {
			oldPlayerState = PlayerState.NULL;
		}
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
		if ( Input.IsActionJustPressed("slide") && Math.Abs(velocity.X) > Speed*0.4f ) {
			playerState = PlayerState.Sliding;
			sliding_time = 0;
			velocity.X += (sprite.FlipH ? -1 : 1)*Speed*0.5f;
			return velocity;
		}
		if ( Input.IsActionJustPressed("crouch") || ptre.colliding) {
			playerState = PlayerState.CrouchWalk;
			return HandleCrouchWalk_Pysics(velocity,delta);
		}
		velocity.X = Mathf.MoveToward(velocity.X, direction*Speed, Speed*(float) delta*30f);
		return HandleGroundedPhysics(velocity,delta);
	}

	public void HandleWalking() {
		if (oldPlayerState == PlayerState.Walking) {
			oldPlayerState = PlayerState.NULL;
		}
		if (Math.Abs(Velocity.X) > 0) {
			animation.Play("walking");
		} else {
			animation.Play("idle");
		}
		sprite.FlipH = getDirection() < 0;
	}
	// ------------------------------------------------------------------------

	// --------------- WALLHANGING --------------------------------------------
	public Vector2 HandleWallHanging_Pysics(Vector2 velocity, double delta) {
		float direction = getDirection();
		if (Input.IsActionJustPressed("jump")) {
			playerState = PlayerState.Jumping;
			if ((direction > 0 && CanWallHangRight()) || (direction < 0 && CanWallHangLeft())) {
				velocity.Y = -1.4f*JumpVelocity;
				velocity.X = (CanWallHangLeft() ? 1 : -1)*Speed*0.5f;
			} else {
				velocity.Y = -1.2f*JumpVelocity;
				velocity.X = (CanWallHangLeft() ? 1 : -1)*Speed*1.8f;
				sprite.FlipH = !sprite.FlipH;
			}
			return velocity;
		}
		if (Input.IsActionJustPressed("crouch")) {
			playerState = PlayerState.Falling;
			velocity.X = (CanWallHangLeft() ? 1 : -1)*Speed*0.5f;
			return HandleFalling_Physics(velocity, delta);
		}
		return new Vector2();
	}

	public void HandleWallHanging() {
		animation.Play("wall_hang");
		sprite.FlipH = CanWallHangLeft();
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

	public bool CanWallHangLeft() {
		return wallgrab[0].colliding && !IsOnFloor();
	}

	public bool CanWallHangRight() {
		return wallgrab[1].colliding && !IsOnFloor();
	}

	public bool CanWallHang() => CanWallHangLeft() || CanWallHangRight();

	public void correctSpritePosition() {
		if (playerState == PlayerState.Sliding) {
			sprite.Position = sprite.FlipH ? new Vector2(4, sprite.Position.Y) : new Vector2(2, sprite.Position.Y);
		} else if (playerState == PlayerState.WallHanging) {
			sprite.Position = sprite.FlipH ? new Vector2(-4, sprite.Position.Y) : new Vector2(-2, sprite.Position.Y);
		} else {
			sprite.Position = sprite.FlipH ? new Vector2(-8, sprite.Position.Y) : new Vector2(2, sprite.Position.Y);
		}
	}

	public void HandleCamera(double delta) {
		Vector2 velocity_normalized = Velocity.Normalized();

		float x = velocity_normalized.X*(250/default_zoom);
		float y = Mathf.Min(0, -100-Position.Y);

		float cam_pos_x = Mathf.MoveToward(cam.Position.X, x, Mathf.Sqrt(Math.Abs(cam.Position.X-x))*(float) delta*5);
		float cam_pos_y = y;
		
		cam.Position = new Vector2(cam_pos_x,cam_pos_y);
	}

	public override void _Process(double delta) {
		if (oldPlayerState != playerState) {
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
				case PlayerState.Sliding :
					HandleSliding();
					break;
				case PlayerState.Crouched :
					HandleCrouching();
					break;
				case PlayerState.CrouchWalk :
					HandleCrouchWalk();
					break;
				case PlayerState.WallHanging:
					HandleWallHanging();
					break;
			}
			oldPlayerState = playerState;
		}

		correctSpritePosition();

		HandleCamera(delta);

		return;
	}

	public override void _PhysicsProcess(double delta) {
		Rect2 view = GetViewportRect();

		updateWasOnFloor(delta);
		//float new_aspect_ratio = default_zoom*(view.Size.Y/720);
		//cam.Zoom = new Vector2(new_aspect_ratio,new_aspect_ratio);

		Vector2 velocity = Velocity;

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
			case PlayerState.Sliding :
				velocity = HandleSliding_Pysics(velocity,delta);
				break;
			case PlayerState.Crouched :
				velocity = HandleCrouching_Pysics(velocity,delta);
				break;
			case PlayerState.CrouchWalk:
				velocity = HandleCrouchWalk_Pysics(velocity,delta);
				break;
			case PlayerState.WallHanging:
				velocity = HandleWallHanging_Pysics(velocity,delta);
				break;
		}

		Velocity = velocity;

		MoveAndSlide();

		return;
	}

	// TODO death by falling in void
	private void _on_hurtbox_area_entered(Area2D area)
	{
		if (area.GetCollisionLayerValue(9)) {
			Position = new Vector2(0,-100);
		}
	}
}




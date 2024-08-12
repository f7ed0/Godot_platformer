using Godot;
using System;

// TODO : FIX jump when sliding under a roof

public enum PlayerState
{
	Idle,Walking,Jumping,Crouched,CrouchWalk,Sliding,Falling,WallHanging,NULL
}

public partial class Player : CharacterBody2D
{
	public SceneTree tree = (SceneTree) Engine.GetMainLoop();
	public Hud HUD;

	public const float Speed = 175.0f;
	public const float SprintSpeed = 250.0f;
	public const float JumpVelocity = 350.0f;
	public const int WallHangBase = 2;

	public const float standart_width = 1280;
	public const float standart_height = 720;
	public Vector2 standart_zoom = new Vector2();

	public PlayerState playerState = PlayerState.Idle;
	public PlayerState oldPlayerState = PlayerState.NULL;

	public float hp_max = 50;
	public float hp = 50;

	public Camera2D cam;
	public AnimatedSprite2D sprite;
	public AnimationPlayer animation;
	public poutre ptre;
	public poutre[] wallgrab;

	public float default_zoom;

	public double was_on_floor;
	public double sliding_time;
	public int wall_hang_count;
	public double hurt_count;

	public double was_hurt = 1000;

	public double blink_variable = 0;

	public bool got_hurt = false;

	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float ground_gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
	public float jump_gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle()*0.25f;

	public override void _Ready() {
		cam = GetNode<Camera2D>("PlayerCamera");
		sprite = GetNode<AnimatedSprite2D>("PlayerSprite");
		animation = GetNode<AnimationPlayer>("AnimationPlayer");
		wallgrab =  new poutre[2];
		wallgrab[0] = GetNode<poutre>("wallgrab_hitbox_l");
		wallgrab[1] = GetNode<poutre>("wallgrab_hitbox_r");
		HUD = (Hud) tree.Root.GetNode("/root/Hud");
		ptre = GetNode<poutre>("poutre");
		default_zoom = cam.Zoom.X;
		standart_zoom = cam.Zoom;
		sprite.Play();
	}
	
	public Vector2 HandleGroundedPhysics(Vector2 velocity, double delta) {
		velocity.Y += ground_gravity * (float) delta;
		return velocity;
	}

	public Vector2 HandleAerialPhysics(Vector2 velocity,double delta) {
		velocity.Y += (Input.IsActionPressed("jump") ? 0.8f : 1f ) * jump_gravity * (float) delta;
		return velocity;
	}

	public float getDirection() {
		return Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");;
	}

	public bool isHurt() {
		return was_hurt < 3f;
	}

	public void Hurt(float amount) {
		hp -= amount;
		was_hurt = 0;
		got_hurt = true;
	}

	public void updateWasOnFloor(double delta) {
		if (IsOnFloor()) {
			was_on_floor = 0;
		} else {
			was_on_floor += delta;
		}
	}

	public void ResetCountersOnGround() {
		wall_hang_count = WallHangBase;
	}

	// ------------------ FALLING ------------------------------
	public Vector2 HandleFalling_Physics(Vector2 velocity, double delta) {
		if (IsOnFloor()) {
			playerState = PlayerState.Idle;
			velocity = HandleIdling_Pysics(velocity, delta);
			return velocity;
		}
		if (was_on_floor < 0.1 && Input.IsActionJustPressed("jump")) {
			playerState = PlayerState.Jumping;
			velocity.Y = -JumpVelocity;
			return velocity;
		}
		if (CanWallHang() && Input.IsActionPressed("wall_hang") && wall_hang_count > 0) {
			wall_hang_count --;
			playerState = PlayerState.WallHanging;
			velocity.Y = 0;
			return HandleWallHanging_Pysics(velocity,delta);
		}
		velocity.X = Mathf.MoveToward(velocity.X, getDirection()*Speed, Speed*0.1f*(float) delta*30f);
		velocity = HandleAerialPhysics(velocity, delta);
		return velocity;
	}

	public void HandleFalling() {
		animation.Queue("falling");
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
		if (CanWallHang() && Input.IsActionJustPressed("wall_hang") && wall_hang_count > 0) {
			wall_hang_count --;
			playerState = PlayerState.WallHanging;
			velocity.Y = 0;
			return HandleWallHanging_Pysics(velocity,delta);
		}
		// 
		velocity.X = Mathf.MoveToward(velocity.X, getDirection()*Speed, Speed*0.1f*(float) delta*30f);
		velocity = HandleAerialPhysics(velocity, delta);
		return velocity;
	}

	public void HandleJumping() {
		animation.Queue("jumping");
	}
	// ----------------------------------------------------------

	// -------------------- IDLE --------------------------------
	public Vector2 HandleIdling_Pysics(Vector2 velocity, double delta) {
		// Player use left or right
		ResetCountersOnGround();
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
		animation.Queue("idle");
	}
	// ------------------------------------------------------------------------

	// -------------------- SLIDING -------------------------------------------
	public Vector2 HandleSliding_Pysics(Vector2 velocity, double delta) {
		sliding_time += delta;
		if ( (!Input.IsActionPressed("slide") && sliding_time > 0.4f)|| Mathf.Abs(velocity.X) < 0.3*Speed ) {
			playerState = PlayerState.Idle;
			return HandleIdling_Pysics(velocity, delta);
		}
		if ( Input.IsActionJustPressed("jump") && !ptre.colliding) {
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
		animation.Queue("sliding_start");
		animation.Queue("sliding");
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
		if (!IsOnFloor()) {
			playerState = PlayerState.Falling;
			return HandleFalling_Physics(velocity,delta);
		}
		velocity.X =  Mathf.MoveToward(velocity.X, 0, Speed*(float) delta*30f);
		return HandleGroundedPhysics(velocity,delta);
	}

	public void HandleCrouching() {
		animation.Queue("crouching");
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
		if (!IsOnFloor()) {
			playerState = PlayerState.Falling;
			return HandleFalling_Physics(velocity,delta);
		}
		velocity.X = Mathf.MoveToward(velocity.X, direction*Speed*0.7f, Speed*0.7f*(float) delta*30f);
		return HandleGroundedPhysics(velocity,delta);
	}

	public void HandleCrouchWalk() {
		if (oldPlayerState == PlayerState.CrouchWalk) {
			oldPlayerState = PlayerState.NULL;
		}
		if (Math.Abs(Velocity.X) > 0) {
			animation.Queue("crouch_walking");
		} else {
			animation.Queue("crouching");
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
		float smothed_direction = Mathf.Sign(direction)*direction*direction*1.5f;
		velocity.X = Mathf.MoveToward(velocity.X, smothed_direction*Speed, Speed*(float) delta*30f);
		return HandleGroundedPhysics(velocity,delta);
	}

	public void HandleWalking() { 
		if (oldPlayerState == PlayerState.Walking) {
			oldPlayerState = PlayerState.NULL;
		}
		if (Math.Abs(Velocity.X) > 0) {
			if ( Velocity.X > Speed*1.2f ) {
				animation.Play("sprinting");
			} else {
				animation.Play("walking");
			}
		} else {
			animation.Queue("idle");
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
				velocity.Y = -0.9f*JumpVelocity;
				velocity.X = (CanWallHangLeft() ? 1 : -1)*Speed*0.3f;
			} else {
				velocity.Y = -1.2f*JumpVelocity;
				velocity.X = (CanWallHangLeft() ? 1 : -1)*Speed*1.8f;
				sprite.FlipH = !sprite.FlipH;
			}
			return velocity;
		}
		if (!Input.IsActionPressed("wall_hang")) {
			playerState = PlayerState.Idle;
			velocity.X = (CanWallHangLeft() ? 1 : -1)*Speed*0.5f;
			return HandleIdling_Pysics(velocity,delta);
		}
		if ( !CanWallHang() ) {
			playerState = PlayerState.Idle;
			return HandleIdling_Pysics(velocity,delta);
		}
		velocity.X = (CanWallHangRight() ? 1 : -1) * 100f;
		velocity.Y = Mathf.MoveToward(velocity.Y, 60f, jump_gravity*0.05f*(float) delta);
		return velocity;
	}

	public void HandleWallHanging() {
		animation.Queue("wall_hanging");
		sprite.FlipH = CanWallHangLeft();
	}
	// ------------------------------------------------------------------------

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
		float current_height_ratio = DisplayServer.WindowGetSize().Y / standart_height;
		float current_width_ratio = DisplayServer.WindowGetSize().X / standart_width;
		float coefzoom = Mathf.Max(current_height_ratio,current_width_ratio);

		cam.Zoom = new Vector2(standart_zoom.X * coefzoom, standart_zoom.Y * coefzoom);

		Vector2 velocity_normalized = Velocity.Normalized();

		float x = velocity_normalized.X*(250/default_zoom);
		float y = Math.Max(-Position.Y-105,Mathf.Min(0, -Position.Y));

		float cam_pos_x = Mathf.MoveToward(cam.Position.X, x, Mathf.Sqrt(Math.Abs(cam.Position.X-x))*(float) delta*5);
		float cam_pos_y = y;
		
		cam.Position = new Vector2(cam_pos_x,cam_pos_y);
	}

	public override void _Process(double delta) {
		was_hurt += delta;
		if (isHurt()) {
			blink_variable += delta;
			while (blink_variable > 0.2f) {
				blink_variable =- 0.2f;
			}
			if (blink_variable > 0.1f) {
				sprite.Modulate = new Color(1,1,1,0.3f);
			} else {
				sprite.Modulate = new Color(1,1,1,1);
			}
		} else {
			sprite.Modulate = new Color(1,1,1,1);
		}
		if (got_hurt) {
			got_hurt = false;

			// TODO FINISH PLAYER STATES
		}
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

		}
		if (hp <= 0) {
			Position = new Vector2(0,-10);
			hp = hp_max;
		}
		HUD.UpdatePosition(Position);
		HUD.updateHP(hp,hp_max);
		oldPlayerState = playerState;

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

	private void _on_head_touch(Area2D area) {
		// Replace with function body.
	}


	private void _on_body_touch(Area2D area) {
		try {
			HitBox hitBox = (HitBox) area;
			if (hitBox.Type == HitBoxType.Hit || hitBox.Type == HitBoxType.Both && hitBox.Ownership != "player") {
				Hurt(hitBox.DamageAmout);
			}
		} catch {
			GD.Print("UNDEFINED TYPE OF HITBOX");
		}
		// Replace with function body.
	}


	private void _on_feet_touched(Area2D area) {
		if (area.GetCollisionLayerValue(9)) {
			hp -= 15;
			Position = new Vector2(0,-10);
			playerState = PlayerState.Idle;
			return;
		}
		try {
			HitBox box = (HitBox) area;  
			
			if ( box.Type == HitBoxType.Hurt || box.Type == HitBoxType.Both ) {
				if ( box.Ownership == "goomba" && Velocity.Y > 0 && was_hurt > 1.0f) {
					Velocity = new Vector2(Velocity.X, Velocity.Y < -400.0f ? Velocity.Y : -400.0f );
					area.GetParent<goomba>().Die();
				}
			} 
		} catch {
			GD.Print("UNDEFINED TYPE OF HITBOX");
		}

	}

}

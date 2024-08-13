using Godot;
using System;

public partial class entity : CharacterBody2D
{
	public Vector2 OriginalPosition {get; set;} = new Vector2();
	public double respawn_timer = 0;
	public bool dead = false;
	[Export]
	public double respawn_time = 10.0f;
	[Export]
	public float default_hp = 10.0f;

	public float hp;

	public override void _Ready()
	{
		OriginalPosition = Position;
		GD.Print(OriginalPosition);
		hp = default_hp;
	}

	public void Die() {
		Position = new Vector2(0,1000);
		respawn_timer = 0;
		dead = true;
	}

	public void Resurect() {
		hp = default_hp;
		Position = OriginalPosition;
		dead = false;

	}

	public override void _Process(double delta)
	{	
		if (hp <= 0 && !dead) {
			Die();
		}
		if (dead) {
			respawn_timer += delta;
			if (respawn_timer > respawn_time) {
				Resurect();
			}
		}
	}
}




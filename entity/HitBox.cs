using Godot;
using System;

public enum HitBoxType {
	Hit, Hurt
}

public partial class HitBox : Area2D
{
	[Export]
	public HitBoxType Type {set; get;} = HitBoxType.Hit;
	[Export]
	public float DamageAmout = 10;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

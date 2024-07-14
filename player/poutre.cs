using Godot;
using System;

public partial class poutre : StaticBody2D
{

	public bool colliding;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public override void _PhysicsProcess(double delta)
	{
 		colliding = MoveAndCollide(new Vector2(0,0), true) != null;
		Position = new Vector2();
	}
}

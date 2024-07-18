using Godot;
using System;

public partial class Hud : CanvasLayer
{
	public Label position_label;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		position_label = GetNode<Label>("hud_position_label");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {

	}
	
	public void UpdatePosition(Vector2 position) {
		position_label.Text = "X : "+ (int) position.X +" Y : "+ (int) position.Y;
	}	
}

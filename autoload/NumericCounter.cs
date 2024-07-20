using Godot;
using System;

public partial class NumericCounter : Node2D
{

	public override void _Ready()
	{
	}


	public void UpdateValue(float current, float max, Color color) {
		Label label = GetNode<Label>("Label");
		Label label2 = GetNode<Label>("Label2");
		label.LabelSettings.FontColor = color;
		label.Text = ""+(int)current;
		label2.Text = "/"+(int)max;
		float size = label.LabelSettings.Font.GetStringSize(label.Text,fontSize:200).X;
		label2.Position = new Vector2(size,0);
	}
}

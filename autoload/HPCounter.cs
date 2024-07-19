using Godot;
using System;

public partial class HPCounter : Node2D
{
	[ExportCategory("Colors")]
	[Export]
	public Color FullHPColor {get; set;} = new Color("00ff00");
	[Export]
	public Color MidHPColor {get; set;} = new Color("ffff00");
	[Export]
	public Color LowHPColor {get; set;} = new Color("ff0000");

	private float min = 0;
	private float max = 100;
	private float current = 100;
	private Color current_tint;

	private TextureProgressBar HpBar;
	private NumericCounter numcounter;

	public void updateValues(float min, float current, float max) {
		this.min = min;
		this.max = max;
		this.current = current;
		current_tint = GenerateTint();
		HpBar.MaxValue = max;
		HpBar.MinValue = min;
		HpBar.Value = current;
		HpBar.TintProgress = current_tint;
		numcounter.UpdateValue(current,max,current_tint);
	}   

	public override void _Ready() {
		HpBar = GetNode<TextureProgressBar>("TextureProgressBar");
		numcounter = GetNode<NumericCounter>("NumericCounter");
		current_tint = GenerateTint();
		HpBar.MaxValue = max;
		HpBar.MinValue = min;
		HpBar.Value = current;
		HpBar.TintProgress = current_tint;
	}

	public override void _Process(double delta) {

	}

	public float GetPercentage() {
		return (current-min)/(max-min);
	}

	public Color GenerateTint() {
		float percentage = GetPercentage();
		GD.Print(percentage);
		float r,g,b;
		if (percentage >= 0.5f) {
			float amount = (percentage-0.5f)*2f;
			r = FullHPColor.R*amount + MidHPColor.R*(1-amount);
			g = FullHPColor.G*amount + MidHPColor.G*(1-amount);
			b = FullHPColor.B*amount + MidHPColor.B*(1-amount);
		} else {
			float amount = (percentage)*2f;
			r = LowHPColor.R*(1-amount) + MidHPColor.R*amount;
			g = LowHPColor.G*(1-amount) + MidHPColor.G*amount;
			b = LowHPColor.B*(1-amount) + MidHPColor.B*amount;
		}
		GD.Print(r," ",g," ",b);
		return new Color(r,g,b);
	}

}

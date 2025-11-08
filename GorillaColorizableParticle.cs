using UnityEngine;

public class GorillaColorizableParticle : GorillaColorizableBase
{
	public ParticleSystem particleSystem;

	public float gradientColorPower = 2f;

	public bool useLinearColor = true;

	public override void SetColor(Color color)
	{
		ParticleSystem.MainModule main = particleSystem.main;
		Color color2 = new Color(Mathf.Pow(color.r, gradientColorPower), Mathf.Pow(color.g, gradientColorPower), Mathf.Pow(color.b, gradientColorPower), color.a);
		main.startColor = new ParticleSystem.MinMaxGradient(useLinearColor ? color.linear : color, useLinearColor ? color2.linear : color2);
	}
}

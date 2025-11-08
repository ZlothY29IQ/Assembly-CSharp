using UnityEngine;

public static class GradientHelper
{
	public static Gradient FromColor(Color color)
	{
		float a = color.a;
		Color col = color;
		col.a = 1f;
		Gradient gradient = new Gradient();
		gradient.colorKeys = new GradientColorKey[1]
		{
			new GradientColorKey(col, 1f)
		};
		gradient.alphaKeys = new GradientAlphaKey[1]
		{
			new GradientAlphaKey(a, 1f)
		};
		return gradient;
	}
}

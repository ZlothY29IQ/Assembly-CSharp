using System;
using UnityEngine;

public static class ColorUtils
{
	private const byte kMaxByteForOverexposedColor = 191;

	public static Color WithAlpha(this Color c, float alpha)
	{
		c.a = Math.Clamp(alpha, 0f, 1f);
		return c;
	}

	public static Color32 WithAlpha(this Color32 c, byte alpha)
	{
		c.a = alpha;
		return c;
	}

	public static Color ComposeHDR(Color baseColor, float intensity)
	{
		intensity = Mathf.Clamp(intensity, -10f, 10f);
		Color result = baseColor;
		if (baseColor.maxColorComponent > 1f)
		{
			result = DecomposeHDR(baseColor).baseColor;
		}
		float num = Mathf.Pow(2f, intensity);
		if (QualitySettings.activeColorSpace == ColorSpace.Linear)
		{
			num = Mathf.GammaToLinearSpace(intensity);
		}
		result *= num;
		result.a = baseColor.a;
		return result;
	}

	public static (Color baseColor, float intensity) DecomposeHDR(Color hdrColor)
	{
		Color32 color = default(Color32);
		float item = 0f;
		float maxColorComponent = hdrColor.maxColorComponent;
		if (maxColorComponent == 0f || (maxColorComponent <= 1f && maxColorComponent >= 0.003921569f))
		{
			color.r = (byte)Mathf.RoundToInt(hdrColor.r * 255f);
			color.g = (byte)Mathf.RoundToInt(hdrColor.g * 255f);
			color.b = (byte)Mathf.RoundToInt(hdrColor.b * 255f);
		}
		else
		{
			float num = 191f / maxColorComponent;
			item = Mathf.Log(255f / num) / Mathf.Log(2f);
			color.r = Math.Min((byte)191, (byte)Mathf.CeilToInt(num * hdrColor.r));
			color.g = Math.Min((byte)191, (byte)Mathf.CeilToInt(num * hdrColor.g));
			color.b = Math.Min((byte)191, (byte)Mathf.CeilToInt(num * hdrColor.b));
		}
		return (baseColor: color, intensity: item);
	}
}

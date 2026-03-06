using System;
using UnityEngine;

namespace GorillaTag;

public static class GTColor
{
	[Serializable]
	public struct HSVRanges(float hMin = 0f, float hMax = 1f, float sMin = 0f, float sMax = 1f, float vMin = 0f, float vMax = 1f)
	{
		public Vector2 h = new Vector2(hMin, hMax);

		public Vector2 s = new Vector2(sMin, sMax);

		public Vector2 v = new Vector2(vMin, vMax);
	}

	public static Color RandomHSV(HSVRanges ranges)
	{
		return Color.HSVToRGB(UnityEngine.Random.Range(ranges.h.x, ranges.h.y), UnityEngine.Random.Range(ranges.s.x, ranges.s.y), UnityEngine.Random.Range(ranges.v.x, ranges.v.y));
	}
}

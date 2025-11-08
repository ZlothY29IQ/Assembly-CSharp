using System;
using UnityEngine;

namespace GorillaTag;

public static class GTColor
{
	[Serializable]
	public struct HSVRanges
	{
		public Vector2 h;

		public Vector2 s;

		public Vector2 v;

		public HSVRanges(float hMin = 0f, float hMax = 1f, float sMin = 0f, float sMax = 1f, float vMin = 0f, float vMax = 1f)
		{
			h = new Vector2(hMin, hMax);
			s = new Vector2(sMin, sMax);
			v = new Vector2(vMin, vMax);
		}
	}

	public static Color RandomHSV(HSVRanges ranges)
	{
		return Color.HSVToRGB(UnityEngine.Random.Range(ranges.h.x, ranges.h.y), UnityEngine.Random.Range(ranges.s.x, ranges.s.y), UnityEngine.Random.Range(ranges.v.x, ranges.v.y));
	}
}

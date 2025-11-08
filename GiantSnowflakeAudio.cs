using System;
using System.Collections.Generic;
using UnityEngine;

public class GiantSnowflakeAudio : MonoBehaviour
{
	[Serializable]
	public struct SnowflakeScaleOverride
	{
		public float scaleMax;

		[GorillaSoundLookup]
		public int newOverrideIndex;
	}

	public List<SnowflakeScaleOverride> audioOverrides;

	private void Start()
	{
		foreach (SnowflakeScaleOverride audioOverride in audioOverrides)
		{
			if (base.transform.lossyScale.x < audioOverride.scaleMax)
			{
				GetComponent<GorillaSurfaceOverride>().overrideIndex = audioOverride.newOverrideIndex;
			}
		}
	}
}

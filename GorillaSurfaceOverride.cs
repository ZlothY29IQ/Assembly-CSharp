using System;
using UnityEngine;

public class GorillaSurfaceOverride : MonoBehaviour
{
	[GorillaSoundLookup]
	public int overrideIndex;

	public float extraVelMultiplier = 1f;

	public float extraVelMaxMultiplier = 1f;

	[NonSerialized]
	[HideInInspector]
	public float slidePercentageOverride = -1f;

	public bool sendOnTapEvent;

	public bool disablePushBackEffect;
}

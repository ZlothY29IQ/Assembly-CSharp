using System;
using UnityEngine;

[Serializable]
public class VoiceLoudnessReactorTransformRotationTarget
{
	public Transform transform;

	private Quaternion initial;

	public Quaternion Max = Quaternion.identity;

	public float Scale = 1f;

	public bool UseSmoothedLoudness;

	public Quaternion Initial
	{
		get
		{
			return initial;
		}
		set
		{
			initial = value;
		}
	}
}

using System;
using UnityEngine;

[Serializable]
public class VoiceLoudnessReactorParticleSystemTarget
{
	public ParticleSystem particleSystem;

	public bool UseSmoothedLoudness;

	public float Scale = 1f;

	private float initialSpeed;

	private float initialRate;

	private float initialSize;

	public AnimationCurve speed;

	public AnimationCurve rate;

	public AnimationCurve size;

	[HideInInspector]
	public ParticleSystem.MainModule Main;

	[HideInInspector]
	public ParticleSystem.EmissionModule Emission;

	public float InitialSpeed
	{
		get
		{
			return initialSpeed;
		}
		set
		{
			initialSpeed = value;
		}
	}

	public float InitialRate
	{
		get
		{
			return initialRate;
		}
		set
		{
			initialRate = value;
		}
	}

	public float InitialSize
	{
		get
		{
			return initialSize;
		}
		set
		{
			initialSize = value;
		}
	}
}

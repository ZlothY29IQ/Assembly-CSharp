using System;
using UnityEngine;

public class GameLightPulse : GameLight, IGorillaSliceableSimple
{
	private float startingIntensity;

	public float frequency;

	private float offsetTime;

	public new void Awake()
	{
		base.Awake();
		startingIntensity = light.intensity;
		offsetTime = UnityEngine.Random.value / frequency;
	}

	protected new void OnEnable()
	{
		base.OnEnable();
		GorillaSlicerSimpleManager.RegisterSliceable(this);
	}

	protected new void OnDisable()
	{
		base.OnDisable();
		GorillaSlicerSimpleManager.UnregisterSliceable(this);
	}

	public void SliceUpdate()
	{
		light.intensity = startingIntensity / 2f * Mathf.Sin((Time.time + offsetTime) * frequency * 2f * MathF.PI % (MathF.PI * 2f)) + startingIntensity / 2f;
	}
}

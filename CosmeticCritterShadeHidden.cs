using System;
using UnityEngine;

public class CosmeticCritterShadeHidden : CosmeticCritter
{
	[Space]
	[Tooltip("How quickly the Shade orbits around the point where it spawned (the spawner's position).")]
	[SerializeField]
	private float orbitDegreesPerSecond;

	[Tooltip("The strength of additional up-and-down motion while orbiting.")]
	[SerializeField]
	private float verticalBobMagnitude;

	[Tooltip("The frequency of additional up-and-down motion while orbiting.")]
	[SerializeField]
	private float verticalBobFrequency;

	private Vector3 orbitCenter;

	private float initialAngle;

	private float orbitRadius;

	private float orbitDirection;

	public void SetCenterAndRadius(Vector3 center, float radius)
	{
		orbitCenter = center;
		orbitRadius = radius;
	}

	public override void SetRandomVariables()
	{
		initialAngle = UnityEngine.Random.Range(0f, MathF.PI * 2f);
		orbitDirection = ((UnityEngine.Random.value > 0.5f) ? 1f : (-1f));
	}

	public override void Tick()
	{
		float num = (float)GetAliveTime();
		float f = initialAngle + orbitDegreesPerSecond * num * orbitDirection;
		float y = verticalBobMagnitude * Mathf.Sin(num * verticalBobFrequency);
		base.transform.position = orbitCenter + new Vector3(orbitRadius * Mathf.Cos(f), y, orbitRadius * Mathf.Sin(f));
	}
}

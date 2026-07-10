using GT_CustomMapSupportRuntime;
using UnityEngine;

namespace GorillaTag.Gravity;

public class PlanetZone : BasicGravityZone
{
	[Tooltip("how close to the center of the zone to enable rotating the player")]
	[SerializeField]
	protected float rotationDistance;

	[Tooltip("if enabled, always rotates the player")]
	[SerializeField]
	protected bool alwaysRotate = true;

	[Tooltip("if enabled, gravity strength is read from the curve below using distance from the zone's center, instead of the constant gravityStrength")]
	[SerializeField]
	private bool useGravityCurve;

	[Tooltip("Maps distance from the zone's center (x) to gravity strength (y). Negative y pulls toward center, positive y expels.")]
	[SerializeField]
	private AnimationCurve gravityCurve = AnimationCurve.Constant(0f, 1f, -9.81f);

	private float sqrDistance;

	protected override void Awake()
	{
		base.Awake();
		CalculateDependentVars();
	}

	private void CalculateDependentVars()
	{
		sqrDistance = rotationDistance * rotationDistance;
	}

	protected override Vector3 GetGravityVectorAtPoint(in Vector3 worldPosition, in MonkeGravityController controller)
	{
		return worldPosition - base.transform.position;
	}

	protected override float GetGravityStrength(in Vector3 offsetFromGravity)
	{
		if (!useGravityCurve)
		{
			return gravityStrength;
		}
		return gravityCurve.Evaluate(offsetFromGravity.magnitude);
	}

	protected override bool GetRotationIntent(in Vector3 offsetFromGravity)
	{
		if (!alwaysRotate)
		{
			if (rotateTarget)
			{
				return offsetFromGravity.sqrMagnitude < sqrDistance;
			}
			return false;
		}
		return true;
	}

	public void CopyProperties(PlanetZoneSettings settings)
	{
		CopyProperties((BasicGravityZoneSettings)settings);
		rotationDistance = settings.rotationDistance;
		alwaysRotate = settings.alwaysRotate;
		useGravityCurve = settings.useGravityCurve;
		gravityCurve = settings.gravityCurve;
		CalculateDependentVars();
	}
}

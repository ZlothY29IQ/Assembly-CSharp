using System;
using GorillaTag.Cosmetics;
using UnityEngine;

public class ArcingProjectileLauncher : ElfLauncher
{
	[SerializeField]
	private Vector2 fireAngleLimits = new Vector3(-75f, 75f);

	[SerializeField]
	private AnimationCurve angleVelocityMultiplier;

	protected override void ShootShared(Vector3 origin, Vector3 direction)
	{
		shootAudio.Play();
		Vector3 lossyScale = base.transform.lossyScale;
		float num = Vector3.Dot(direction, Vector3.up);
		Vector3 vector = ((!((double)num > 0.99999) && !((double)num < -0.99999)) ? Vector3.ProjectOnPlane(direction, Vector3.up) : ((!(parentHoldable.myRig != null)) ? Vector3.forward : parentHoldable.myRig.transform.forward));
		vector.Normalize();
		Vector3 axis = Vector3.Cross(vector, Vector3.up);
		float num2 = Vector3.SignedAngle(vector, direction, axis);
		float num3 = angleVelocityMultiplier.Evaluate(num2);
		float num4 = Mathf.Clamp(num2, fireAngleLimits.x, fireAngleLimits.y);
		float num5 = Mathf.Sin(num4 * (MathF.PI / 180f));
		float num6 = Mathf.Cos(num4 * (MathF.PI / 180f));
		Vector3 vector2 = num5 * Vector3.up + num6 * vector;
		vector2.Normalize();
		Vector3 vector3 = vector2 * (muzzleVelocity * lossyScale.x * num3);
		GameObject gameObject = ObjectPools.instance.Instantiate(elfProjectileHash);
		IProjectile component = gameObject.GetComponent<IProjectile>();
		if (component != null)
		{
			component.Launch(origin, Quaternion.LookRotation(vector, Vector3.up), vector3, 1f, parentHoldable.myRig);
			return;
		}
		gameObject.transform.position = origin;
		gameObject.transform.rotation = Quaternion.LookRotation(direction);
		gameObject.transform.localScale = lossyScale;
		gameObject.GetComponent<Rigidbody>().linearVelocity = vector3;
	}
}

using System;
using UnityEngine;

namespace GorillaTag.Gravity;

public class ConsensusGravityZone : BasicGravityZone
{
	private Collider zoneCollider;

	private float currentRot;

	private float idealRot;

	private float rotSpeed;

	[SerializeField]
	private float weightForce;

	[SerializeField]
	private float centeringForce;

	[SerializeField]
	private float drag;

	[SerializeField]
	private float rotMin = -45f;

	[SerializeField]
	private float rotMax = 45f;

	protected override void Awake()
	{
		base.Awake();
		zoneCollider = GetComponent<Collider>();
	}

	protected override Vector3 GetGravityVectorAtPoint(in Vector3 worldPosition, in MonkeGravityController controller)
	{
		return base.transform.TransformVector(new Vector3(Mathf.Sin(currentRot * (MathF.PI / 180f)), Mathf.Cos(currentRot * (MathF.PI / 180f)), 0f));
	}

	private void FixedUpdate()
	{
		Vector3 zero = Vector3.zero;
		int num = 0;
		foreach (RigContainer activeRigContainer in VRRigCache.ActiveRigContainers)
		{
			Vector3 position = activeRigContainer.Rig.transform.position;
			if (zoneCollider.bounds.Contains(position))
			{
				zero += position;
				num++;
			}
		}
		if (num > 0)
		{
			Vector3 position2 = zero / num;
			Vector3 vector = base.transform.InverseTransformPoint(position2);
			idealRot = Mathf.Atan2(vector.x, vector.y) * 57.29578f;
		}
		float num2 = (idealRot - currentRot) * weightForce - currentRot * centeringForce;
		rotSpeed += num2 * Time.fixedDeltaTime;
		rotSpeed *= drag;
		currentRot += rotSpeed * Time.fixedDeltaTime;
		if (currentRot < rotMin)
		{
			rotSpeed = 0f;
			currentRot = rotMin;
		}
		else if (currentRot > rotMax)
		{
			rotSpeed = 0f;
			currentRot = rotMax;
		}
	}

	protected override bool GetRotationIntent(in Vector3 offsetFromGravity)
	{
		return true;
	}
}

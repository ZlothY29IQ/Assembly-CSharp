using System;
using UnityEngine;

public class YorickLook : MonoBehaviour
{
	public Transform leftEye;

	public Transform rightEye;

	public Transform lookTarget;

	public float lookRadius = 0.5f;

	public VRRig[] rigs = new VRRig[20];

	public VRRig[] overlapRigs;

	public float rotSpeed = 1f;

	public float lookAtAngleDegrees = 60f;

	private void Awake()
	{
		overlapRigs = new VRRig[20];
	}

	private void LateUpdate()
	{
		if (NetworkSystem.Instance.InRoom)
		{
			if (rigs.Length != NetworkSystem.Instance.RoomPlayerCount)
			{
				rigs = VRRigCache.Instance.GetAllRigs();
			}
		}
		else if (rigs.Length != 1)
		{
			rigs = new VRRig[1];
			rigs[0] = VRRig.LocalRig;
		}
		float num = -1f;
		float num2 = Mathf.Cos(lookAtAngleDegrees / 180f * MathF.PI);
		int num3 = 0;
		for (int i = 0; i < rigs.Length; i++)
		{
			Vector3 vector = rigs[i].tagSound.transform.position - base.transform.position;
			if (!(vector.magnitude > lookRadius))
			{
				float num4 = Vector3.Dot(-base.transform.up, vector.normalized);
				if (num4 > num2)
				{
					overlapRigs[num3++] = rigs[i];
				}
			}
		}
		lookTarget = null;
		for (int j = 0; j < num3; j++)
		{
			Vector3 vector = (overlapRigs[j].tagSound.transform.position - base.transform.position).normalized;
			float num4 = Vector3.Dot(base.transform.forward, vector);
			if (num4 > num)
			{
				num = num4;
				lookTarget = overlapRigs[j].tagSound.transform;
			}
		}
		Vector3 target = -base.transform.up;
		Vector3 target2 = -base.transform.up;
		if (lookTarget != null)
		{
			target = (lookTarget.position - leftEye.position).normalized;
			target2 = (lookTarget.position - rightEye.position).normalized;
		}
		Vector3 forward = Vector3.RotateTowards(leftEye.rotation * Vector3.forward, target, rotSpeed * MathF.PI, 0f);
		Vector3 forward2 = Vector3.RotateTowards(rightEye.rotation * Vector3.forward, target2, rotSpeed * MathF.PI, 0f);
		leftEye.rotation = Quaternion.LookRotation(forward);
		rightEye.rotation = Quaternion.LookRotation(forward2);
	}
}

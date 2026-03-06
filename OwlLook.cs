using System;
using UnityEngine;

public class OwlLook : MonoBehaviour
{
	public Transform head;

	public Transform lookTarget;

	public Transform neck;

	public float lookRadius = 0.5f;

	public Collider[] overlapColliders;

	public VRRig[] rigs = new VRRig[20];

	public VRRig[] overlapRigs;

	public float rotSpeed = 1f;

	public float lookAtAngleDegrees = 60f;

	public float maxNeckY;

	public float minNeckY;

	public VRRig myRig;

	private void Awake()
	{
		overlapRigs = new VRRig[20];
		if (myRig == null)
		{
			myRig = GetComponentInParent<VRRig>();
		}
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
			if (rigs[i] == myRig)
			{
				continue;
			}
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
		Vector3 direction = neck.forward;
		if (lookTarget != null)
		{
			direction = (lookTarget.position - head.position).normalized;
		}
		Vector3 vector2 = neck.InverseTransformDirection(direction);
		vector2.y = Mathf.Clamp(vector2.y, minNeckY, maxNeckY);
		direction = neck.TransformDirection(vector2.normalized);
		Vector3 forward = Vector3.RotateTowards(head.forward, direction, rotSpeed * (MathF.PI / 180f) * Time.deltaTime, 0f);
		head.rotation = Quaternion.LookRotation(forward, neck.up);
	}
}

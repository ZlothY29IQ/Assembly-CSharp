using System;
using GorillaExtensions;
using GorillaLocomotion;
using UnityEngine;
using UnityEngine.Events;

public class TransferrableObjectHoldablePart_Crank : TransferrableObjectHoldablePart
{
	[Serializable]
	private struct CrankThreshold
	{
		public float angleThreshold;

		public UnityEvent onReached;

		[HideInInspector]
		public float currentAngle;

		public void OnCranked(float deltaAngle)
		{
			currentAngle += deltaAngle;
			if (Mathf.Abs(currentAngle) > angleThreshold)
			{
				currentAngle = 0f;
				onReached.Invoke();
			}
		}
	}

	[SerializeField]
	private float crankHandleX;

	[SerializeField]
	private float crankHandleY;

	[SerializeField]
	private float crankHandleMinZ;

	[SerializeField]
	private float crankHandleMaxZ;

	[SerializeField]
	private float maxHandSnapDistance;

	private float crankAngleOffset;

	private float crankRadius;

	[SerializeField]
	private Transform rotatingPart;

	private float lastAngle;

	private Quaternion baseLocalAngle;

	private Quaternion baseLocalAngleInverse;

	private Action<float> onCrankedCallback;

	[SerializeField]
	private CrankThreshold[] thresholds;

	public void SetOnCrankedCallback(Action<float> onCrankedCallback)
	{
		this.onCrankedCallback = onCrankedCallback;
	}

	private void Awake()
	{
		if (rotatingPart == null)
		{
			rotatingPart = base.transform;
		}
		Vector3 vector = rotatingPart.parent.InverseTransformPoint(rotatingPart.TransformPoint(Vector3.right));
		lastAngle = Mathf.Atan2(vector.y, vector.x);
		baseLocalAngle = rotatingPart.localRotation;
		baseLocalAngleInverse = Quaternion.Inverse(baseLocalAngle);
		crankRadius = new Vector2(crankHandleX, crankHandleY).magnitude;
		crankAngleOffset = Mathf.Atan2(crankHandleY, crankHandleX) * 57.29578f;
		if (crankHandleMaxZ < crankHandleMinZ)
		{
			float num = crankHandleMaxZ;
			float num2 = crankHandleMinZ;
			crankHandleMinZ = num;
			crankHandleMaxZ = num2;
		}
	}

	protected override void UpdateHeld(VRRig rig, bool isHeldLeftHand)
	{
		Vector3 vector2;
		if (rig.isOfflineVRRig)
		{
			Transform controllerTransform = GTPlayer.Instance.GetControllerTransform(isHeldLeftHand);
			Vector3 v = rotatingPart.InverseTransformPoint(controllerTransform.position);
			Vector3 position = (v.xy().normalized * crankRadius).WithZ(Mathf.Clamp(v.z, crankHandleMinZ, crankHandleMaxZ));
			Vector3 vector = rotatingPart.TransformPoint(position);
			if (maxHandSnapDistance > 0f && (controllerTransform.position - vector).IsLongerThan(maxHandSnapDistance))
			{
				OnRelease(null, isHeldLeftHand ? EquipmentInteractor.instance.leftHand : EquipmentInteractor.instance.rightHand);
				return;
			}
			controllerTransform.position = vector;
			vector2 = controllerTransform.position;
		}
		else
		{
			VRMap vRMap = (isHeldLeftHand ? rig.leftHand : rig.rightHand);
			vector2 = vRMap.GetExtrapolatedControllerPosition();
			vector2 -= vRMap.rigTarget.rotation * GTPlayer.Instance.GetHandOffset(isHeldLeftHand) * rig.scaleFactor;
		}
		Vector3 vector3 = baseLocalAngleInverse * Quaternion.Inverse(rotatingPart.parent.rotation) * (vector2 - rotatingPart.position);
		float num = Mathf.Atan2(vector3.y, vector3.x) * 57.29578f;
		float num2 = Mathf.DeltaAngle(lastAngle, num);
		lastAngle = num;
		if (num2 != 0f)
		{
			if (onCrankedCallback != null)
			{
				onCrankedCallback(num2);
			}
			for (int i = 0; i < thresholds.Length; i++)
			{
				thresholds[i].OnCranked(num2);
			}
		}
		rotatingPart.localRotation = baseLocalAngle * Quaternion.AngleAxis(num - crankAngleOffset, Vector3.forward);
	}

	private void OnDrawGizmosSelected()
	{
		Transform transform = ((rotatingPart != null) ? rotatingPart : base.transform);
		Gizmos.color = Color.green;
		Gizmos.DrawLine(transform.TransformPoint(new Vector3(crankHandleX, crankHandleY, crankHandleMinZ)), transform.TransformPoint(new Vector3(crankHandleX, crankHandleY, crankHandleMaxZ)));
	}
}

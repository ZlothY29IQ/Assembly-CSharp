using GorillaExtensions;
using GorillaLocomotion;
using UnityEngine;

public class ArtilleryCrank : HoldableObject
{
	[SerializeField]
	private ArtilleryCannon cannon;

	[SerializeField]
	private ArtilleryCrankType crankType;

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

	[SerializeField]
	private Transform rotatingPart;

	private float crankAngleOffset;

	private float crankRadius;

	private float lastAngle;

	private float currentAngle;

	private Quaternion baseLocalAngle;

	private Quaternion baseLocalAngleInverse;

	private bool isHeld;

	private bool isHeldLeftHand;

	public bool IsHeld => isHeld;

	public bool IsHeldLeftHand => isHeldLeftHand;

	public float CurrentAngle => currentAngle;

	private int CrankIndex
	{
		get
		{
			if (crankType != ArtilleryCrankType.Pitch)
			{
				return 1;
			}
			return 0;
		}
	}

	private void Awake()
	{
		if (rotatingPart == null)
		{
			rotatingPart = base.transform;
		}
		Vector3 vector = rotatingPart.parent.InverseTransformPoint(rotatingPart.TransformPoint(Vector3.right));
		lastAngle = Mathf.Atan2(vector.y, vector.x) * 57.29578f;
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

	private void LateUpdate()
	{
		if (!isHeld)
		{
			return;
		}
		if (!cannon.IsCrankHeldLocally(CrankIndex))
		{
			DropItemCleanup();
			return;
		}
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
		float num = ComputeAngleFromWorldPos(controllerTransform.position);
		float num2 = Mathf.DeltaAngle(lastAngle, num);
		lastAngle = num;
		currentAngle = num;
		if (num2 != 0f)
		{
			cannon.OnCrankInput(CrankIndex, num2);
		}
		ApplyVisualAngle(num);
	}

	public void UpdateFromRemoteHand(VRRig rig, bool leftHand)
	{
		VRMap vRMap = (leftHand ? rig.leftHand : rig.rightHand);
		Vector3 extrapolatedControllerPosition = vRMap.GetExtrapolatedControllerPosition();
		extrapolatedControllerPosition -= vRMap.rigTarget.rotation * GTPlayer.Instance.GetHandOffset(leftHand) * rig.scaleFactor;
		ApplyVisualAngle(currentAngle = ComputeAngleFromWorldPos(extrapolatedControllerPosition));
	}

	public void SetVisualAngle(float angle)
	{
		if (rotatingPart != null)
		{
			currentAngle = angle;
			ApplyVisualAngle(angle);
		}
	}

	private float ComputeAngleFromWorldPos(Vector3 worldPos)
	{
		Vector3 vector = baseLocalAngleInverse * Quaternion.Inverse(rotatingPart.parent.rotation) * (worldPos - rotatingPart.position);
		return Mathf.Atan2(vector.y, vector.x) * 57.29578f;
	}

	private void ApplyVisualAngle(float angle)
	{
		rotatingPart.localRotation = baseLocalAngle * Quaternion.AngleAxis(angle - crankAngleOffset, Vector3.forward);
	}

	public override void OnHover(InteractionPoint pointHovered, GameObject hoveringHand)
	{
	}

	public override void OnGrab(InteractionPoint pointGrabbed, GameObject grabbingHand)
	{
		isHeldLeftHand = grabbingHand == EquipmentInteractor.instance.leftHand;
		if (cannon.OnCrankGrabbed(CrankIndex, isHeldLeftHand))
		{
			isHeld = true;
			EquipmentInteractor.instance.UpdateHandEquipment(this, isHeldLeftHand);
			Transform controllerTransform = GTPlayer.Instance.GetControllerTransform(isHeldLeftHand);
			Vector3 vector = baseLocalAngleInverse * Quaternion.Inverse(rotatingPart.parent.rotation) * (controllerTransform.position - rotatingPart.position);
			lastAngle = Mathf.Atan2(vector.y, vector.x) * 57.29578f;
		}
	}

	public override void DropItemCleanup()
	{
		if (isHeld)
		{
			isHeld = false;
			cannon.OnCrankReleased(CrankIndex, currentAngle);
		}
	}

	public override bool OnRelease(DropZone zoneReleased, GameObject releasingHand)
	{
		if (!base.OnRelease(zoneReleased, releasingHand))
		{
			return false;
		}
		EquipmentInteractor.instance.UpdateHandEquipment(null, isHeldLeftHand);
		if (isHeld)
		{
			isHeld = false;
			cannon.OnCrankReleased(CrankIndex, currentAngle);
		}
		return true;
	}

	private void OnDrawGizmosSelected()
	{
		Transform transform = ((rotatingPart != null) ? rotatingPart : base.transform);
		Gizmos.color = Color.green;
		Gizmos.DrawLine(transform.TransformPoint(new Vector3(crankHandleX, crankHandleY, crankHandleMinZ)), transform.TransformPoint(new Vector3(crankHandleX, crankHandleY, crankHandleMaxZ)));
	}
}

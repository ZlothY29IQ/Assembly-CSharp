using UnityEngine;

public class GorillaIK : MonoBehaviour
{
	public Transform headBone;

	public Transform leftUpperArm;

	public Transform leftLowerArm;

	public Transform leftHand;

	public Transform rightUpperArm;

	public Transform rightLowerArm;

	public Transform rightHand;

	public Transform targetLeft;

	public Transform targetRight;

	public Transform targetHead;

	public Quaternion initialUpperLeft;

	public Quaternion initialLowerLeft;

	public Quaternion initialUpperRight;

	public Quaternion initialLowerRight;

	public Quaternion newRotationUpper;

	public Quaternion newRotationLower;

	public float dU;

	public float dL;

	public float dMax;

	public bool testInEditor;

	public bool reset;

	public bool testDefineRot;

	public bool moveOnce;

	public float eps;

	public float upperArmAngle;

	public float elbowAngle;

	private bool hasLeftOverride;

	private Vector3 leftOverrideWorldPos;

	private bool hasRightOverride;

	private Vector3 rightOverrideWorldPos;

	private void Awake()
	{
		if (Application.isPlaying && !testInEditor)
		{
			dU = (leftUpperArm.position - leftLowerArm.position).magnitude;
			dL = (leftLowerArm.position - leftHand.position).magnitude;
			dMax = dU + dL - eps;
			initialUpperLeft = leftUpperArm.localRotation;
			initialLowerLeft = leftLowerArm.localRotation;
			initialUpperRight = rightUpperArm.localRotation;
			initialLowerRight = rightLowerArm.localRotation;
		}
	}

	private void OnEnable()
	{
		GorillaIKMgr.Instance.RegisterIK(this);
	}

	private void OnDisable()
	{
		GorillaIKMgr.Instance.DeregisterIK(this);
	}

	public void OverrideTargetPos(bool isLeftHand, Vector3 targetWorldPos)
	{
		if (isLeftHand)
		{
			hasLeftOverride = true;
			leftOverrideWorldPos = targetWorldPos;
		}
		else
		{
			hasRightOverride = true;
			rightOverrideWorldPos = targetWorldPos;
		}
	}

	public Vector3 GetShoulderLocalTargetPos_Left()
	{
		return leftUpperArm.parent.InverseTransformPoint(hasLeftOverride ? leftOverrideWorldPos : targetLeft.position);
	}

	public Vector3 GetShoulderLocalTargetPos_Right()
	{
		return rightUpperArm.parent.InverseTransformPoint(hasRightOverride ? rightOverrideWorldPos : targetRight.position);
	}

	public void ClearOverrides()
	{
		hasLeftOverride = false;
		hasRightOverride = false;
	}

	private void ArmIK(ref Transform upperArm, ref Transform lowerArm, ref Transform hand, Quaternion initRotUpper, Quaternion initRotLower, Transform target)
	{
		upperArm.localRotation = initRotUpper;
		lowerArm.localRotation = initRotLower;
		float num = Mathf.Clamp((target.position - upperArm.position).magnitude, eps, dMax);
		float num2 = Mathf.Acos(Mathf.Clamp(Vector3.Dot((hand.position - upperArm.position).normalized, (lowerArm.position - upperArm.position).normalized), -1f, 1f));
		float num3 = Mathf.Acos(Mathf.Clamp(Vector3.Dot((upperArm.position - lowerArm.position).normalized, (hand.position - lowerArm.position).normalized), -1f, 1f));
		float num4 = Mathf.Acos(Mathf.Clamp(Vector3.Dot((hand.position - upperArm.position).normalized, (target.position - upperArm.position).normalized), -1f, 1f));
		float num5 = Mathf.Acos(Mathf.Clamp((dL * dL - dU * dU - num * num) / (-2f * dU * num), -1f, 1f));
		float num6 = Mathf.Acos(Mathf.Clamp((num * num - dU * dU - dL * dL) / (-2f * dU * dL), -1f, 1f));
		Vector3 normalized = Vector3.Cross(hand.position - upperArm.position, lowerArm.position - upperArm.position).normalized;
		Vector3 normalized2 = Vector3.Cross(hand.position - upperArm.position, target.position - upperArm.position).normalized;
		Quaternion quaternion = Quaternion.AngleAxis((num5 - num2) * 57.29578f, Quaternion.Inverse(upperArm.rotation) * normalized);
		Quaternion quaternion2 = Quaternion.AngleAxis((num6 - num3) * 57.29578f, Quaternion.Inverse(lowerArm.rotation) * normalized);
		Quaternion quaternion3 = Quaternion.AngleAxis(num4 * 57.29578f, Quaternion.Inverse(upperArm.rotation) * normalized2);
		newRotationUpper = upperArm.localRotation * quaternion3 * quaternion;
		newRotationLower = lowerArm.localRotation * quaternion2;
		upperArm.localRotation = newRotationUpper;
		lowerArm.localRotation = newRotationLower;
		hand.rotation = target.rotation;
	}
}

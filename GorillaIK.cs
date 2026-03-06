using System;
using GorillaTagScripts;
using UnityEngine;
using UnityEngine.Android;

public class GorillaIK : MonoBehaviour
{
	public static GorillaIK playerIK;

	public Transform headBone;

	public Transform bodyBone;

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

	[NonSerialized]
	public Quaternion targetBodyRot;

	[NonSerialized]
	public Quaternion lerpBodyRot;

	[NonSerialized]
	public Vector3 leftElbowDirection;

	[NonSerialized]
	public Vector3 lerpLeftElbowDirection;

	[NonSerialized]
	public Vector3 rightElbowDirection;

	[NonSerialized]
	public Vector3 lerpRightElbowDirection;

	public bool usingUpdatedIK;

	public bool canUseUpdatedIK;

	public Quaternion bodyOffsetRotation;

	public OVRSkeleton skeleton;

	private Transform[] boneXforms;

	[NonSerialized]
	public Quaternion bodyInitialRot;

	public Transform projectedBodyRotation;

	public Transform projectedLeftShoulderPosition;

	public Transform projectedRightShoulderPosition;

	[NonSerialized]
	public VRRig myRig;

	public float biasDistance = 0.2f;

	private bool hasLeftOverride;

	private Vector3 leftOverrideWorldPos;

	private bool hasRightOverride;

	private Vector3 rightOverrideWorldPos;

	private Transform body;

	private Transform leftArmUpper;

	private Transform leftArmLower;

	private Transform rightArmUpper;

	private Transform rightArmLower;

	public bool TickRunning { get; set; }

	private void Awake()
	{
		bodyInitialRot = bodyBone.localRotation;
		myRig = GetComponent<VRRig>();
		ResetIKData();
	}

	private void OnEnable()
	{
		GorillaIKMgr.Instance.RegisterIK(this);
		if (!(skeleton == null))
		{
			playerIK = this;
		}
	}

	private void OnDisable()
	{
		GorillaIKMgr.Instance.DeregisterIK(this);
		ResetIKData();
	}

	public void ResetIKData()
	{
		leftElbowDirection = Vector3.zero;
		lerpLeftElbowDirection = Vector3.zero;
		rightElbowDirection = Vector3.zero;
		lerpRightElbowDirection = Vector3.zero;
		targetBodyRot = bodyInitialRot;
		lerpBodyRot = targetBodyRot;
		if (projectedBodyRotation != null)
		{
			projectedBodyRotation.localRotation = targetBodyRot;
		}
		usingUpdatedIK = false;
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

	public Vector3 GetShoulderLocalTargetPos_Left(bool updatedIK)
	{
		if (projectedBodyRotation != null && updatedIK)
		{
			return projectedLeftShoulderPosition.InverseTransformPoint(hasLeftOverride ? leftOverrideWorldPos : targetLeft.position);
		}
		return leftUpperArm.parent.InverseTransformPoint(hasLeftOverride ? leftOverrideWorldPos : targetLeft.position);
	}

	public Vector3 GetShoulderLocalTargetPos_Right(bool updatedIK)
	{
		if (projectedBodyRotation != null && updatedIK)
		{
			return projectedRightShoulderPosition.InverseTransformPoint(hasRightOverride ? rightOverrideWorldPos : targetRight.position);
		}
		return rightUpperArm.parent.InverseTransformPoint(hasRightOverride ? rightOverrideWorldPos : targetRight.position);
	}

	public void ClearOverrides()
	{
		hasLeftOverride = false;
		hasRightOverride = false;
	}

	public void SkeletonUpdate()
	{
		if (!canUseUpdatedIK || !SubscriptionManager.IsLocalSubscribed())
		{
			return;
		}
		bool subscriptionSettingBool = SubscriptionManager.GetSubscriptionSettingBool(SubscriptionManager.SubscriptionFeatures.IOBT);
		if (subscriptionSettingBool != skeleton.gameObject.activeSelf)
		{
			skeleton.gameObject.SetActive(subscriptionSettingBool);
			usingUpdatedIK = subscriptionSettingBool;
			if (!subscriptionSettingBool)
			{
				ResetIKData();
			}
		}
		else
		{
			if (!subscriptionSettingBool || skeleton == null || skeleton.Bones == null || skeleton.Bones.Count == 0)
			{
				return;
			}
			if (boneXforms[0] == null || body == null || leftArmUpper == null || leftArmLower == null || rightArmUpper == null || rightArmLower == null)
			{
				foreach (OVRBone bone in skeleton.Bones)
				{
					boneXforms[(int)bone.Id] = bone.Transform;
				}
				body = boneXforms[5];
				leftArmUpper = boneXforms[10];
				leftArmLower = boneXforms[11];
				rightArmUpper = boneXforms[15];
				rightArmLower = boneXforms[16];
			}
			else
			{
				usingUpdatedIK = true;
				targetBodyRot = Quaternion.Inverse(bodyBone.parent.rotation) * skeleton.transform.rotation * body.localRotation * bodyOffsetRotation;
				projectedBodyRotation.localRotation = targetBodyRot;
				leftElbowDirection = projectedLeftShoulderPosition.InverseTransformDirection((leftArmLower.position - leftArmLower.up * biasDistance - targetLeft.position).normalized).normalized;
				rightElbowDirection = projectedRightShoulderPosition.InverseTransformDirection((rightArmLower.position + rightArmLower.up * biasDistance - targetRight.position).normalized).normalized;
			}
		}
	}

	private void CheckPermissions()
	{
		if (!Permission.HasUserAuthorizedPermission("com.oculus.permission.BODY_TRACKING"))
		{
			PermissionCallbacks permissionCallbacks = new PermissionCallbacks();
			permissionCallbacks.PermissionGranted += PermissionGranted;
			Permission.RequestUserPermission("com.oculus.permission.BODY_TRACKING", permissionCallbacks);
		}
		else
		{
			PermissionGranted("");
		}
	}

	private void PermissionGranted(string permissionName)
	{
		GorillaIKMgr.AddPlayerIK(this);
		boneXforms = new Transform[84];
		leftElbowDirection = Vector3.zero;
		rightElbowDirection = Vector3.zero;
		targetBodyRot = bodyInitialRot;
		canUseUpdatedIK = true;
	}
}

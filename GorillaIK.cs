using System;
using System.Collections;
using System.Collections.Generic;
using GorillaTagScripts;
using UnityEngine;
using UnityEngine.Android;

public class GorillaIK : MonoBehaviour
{
	private const string LeanOffsetSavePrefsKey = "_GorillaIKLeanOffset";

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

	private Coroutine useUpdatedIKCoroutine;

	public Quaternion bodyOffsetRotation;

	public Quaternion leanOffsetRotation = Quaternion.identity;

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

	private VRRigAnchorOverrides anchorOverrides;

	private Coroutine calibrateCoroutine;

	private bool calibrating;

	private bool hasLeftOverride;

	private Vector3 leftOverrideWorldPos;

	private bool hasRightOverride;

	private Vector3 rightOverrideWorldPos;

	[NonSerialized]
	public Vector3 renderDisplacement;

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
		anchorOverrides = GetComponentInChildren<VRRigAnchorOverrides>(includeInactive: true);
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
		renderDisplacement = Vector3.zero;
		usingUpdatedIK = false;
	}

	public bool CanUpdateIK()
	{
		return useUpdatedIKCoroutine == null;
	}

	public void DelayedUpdateIK(bool usingIK)
	{
		if (CanUpdateIK())
		{
			useUpdatedIKCoroutine = StartCoroutine(DoDelayedUpdateIK(usingIK));
		}
	}

	private IEnumerator DoDelayedUpdateIK(bool usingIK)
	{
		yield return new WaitForSeconds(0.5f);
		ResetIKData();
		usingUpdatedIK = usingIK;
		yield return new WaitForSeconds(0.5f);
		useUpdatedIKCoroutine = null;
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
		Vector3 position = (hasLeftOverride ? leftOverrideWorldPos : targetLeft.position) + renderDisplacement;
		if (projectedBodyRotation != null && updatedIK)
		{
			return projectedLeftShoulderPosition.InverseTransformPoint(position);
		}
		return leftUpperArm.parent.InverseTransformPoint(position);
	}

	public Vector3 GetShoulderLocalTargetPos_Right(bool updatedIK)
	{
		Vector3 position = (hasRightOverride ? rightOverrideWorldPos : targetRight.position) + renderDisplacement;
		if (projectedBodyRotation != null && updatedIK)
		{
			return projectedRightShoulderPosition.InverseTransformPoint(position);
		}
		return rightUpperArm.parent.InverseTransformPoint(position);
	}

	public void ClearOverrides()
	{
		hasLeftOverride = false;
		hasRightOverride = false;
	}

	public void SkeletonUpdate()
	{
		if (!canUseUpdatedIK)
		{
			return;
		}
		bool subscriptionSettingBool = SubscriptionManager.GetSubscriptionSettingBool(SubscriptionManager.SubscriptionFeatures.IOBT);
		if (subscriptionSettingBool != skeleton.gameObject.activeSelf)
		{
			skeleton.gameObject.SetActive(subscriptionSettingBool);
			usingUpdatedIK = subscriptionSettingBool;
			if (anchorOverrides != null)
			{
				anchorOverrides.EnableChestBodyTracking(subscriptionSettingBool);
			}
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
				if (!calibrating)
				{
					targetBodyRot = Quaternion.Inverse(bodyBone.parent.rotation) * skeleton.transform.rotation * body.localRotation * bodyOffsetRotation * leanOffsetRotation;
				}
				else
				{
					targetBodyRot = Quaternion.Inverse(bodyBone.parent.rotation) * skeleton.transform.rotation * body.localRotation * bodyOffsetRotation;
				}
				projectedBodyRotation.localRotation = targetBodyRot;
				leftElbowDirection = projectedLeftShoulderPosition.InverseTransformDirection((leftArmLower.position - leftArmLower.up * biasDistance - targetLeft.position).normalized).normalized;
				rightElbowDirection = projectedRightShoulderPosition.InverseTransformDirection((rightArmLower.position + rightArmLower.up * biasDistance - targetRight.position).normalized).normalized;
			}
		}
	}

	[ContextMenu("Calibrate Lean Offset")]
	public void CalibrateLeanOffset()
	{
		if (calibrateCoroutine == null)
		{
			calibrateCoroutine = StartCoroutine(CalibrateLeanOffsetCoroutine());
		}
	}

	private IEnumerator CalibrateLeanOffsetCoroutine()
	{
		calibrating = true;
		yield return new WaitForSeconds(1f);
		List<Vector3> vecs = new List<Vector3>();
		int maxTries = 5;
		for (int tries = 0; tries < maxTries; tries++)
		{
			yield return new WaitForSeconds(0.2f);
			vecs.Add(projectedBodyRotation.InverseTransformDirection(Vector3.up).normalized);
		}
		leanOffsetRotation = Quaternion.FromToRotation(Vector3.up, CalculateAverage(vecs));
		SaveLeanOffset();
		calibrating = false;
		calibrateCoroutine = null;
	}

	[ContextMenu("Reset Lean Offset")]
	public void ResetLeanOffset()
	{
		if (calibrateCoroutine == null)
		{
			leanOffsetRotation = Quaternion.identity;
			SaveLeanOffset();
		}
	}

	private void LoadLeanOffset()
	{
		if (PlayerPrefs.HasKey("_GorillaIKLeanOffset_X"))
		{
			float x = PlayerPrefs.GetFloat("_GorillaIKLeanOffset_X");
			float y = PlayerPrefs.GetFloat("_GorillaIKLeanOffset_Y");
			float z = PlayerPrefs.GetFloat("_GorillaIKLeanOffset_Z");
			float w = PlayerPrefs.GetFloat("_GorillaIKLeanOffset_W");
			leanOffsetRotation = new Quaternion(x, y, z, w);
		}
	}

	private void SaveLeanOffset()
	{
		PlayerPrefs.SetFloat("_GorillaIKLeanOffset_X", leanOffsetRotation.x);
		PlayerPrefs.SetFloat("_GorillaIKLeanOffset_Y", leanOffsetRotation.y);
		PlayerPrefs.SetFloat("_GorillaIKLeanOffset_Z", leanOffsetRotation.z);
		PlayerPrefs.SetFloat("_GorillaIKLeanOffset_W", leanOffsetRotation.w);
		PlayerPrefs.Save();
	}

	private Vector3 CalculateAverage(List<Vector3> vecs)
	{
		if (vecs == null || vecs.Count == 0)
		{
			return Vector3.zero;
		}
		Vector3 zero = Vector3.zero;
		for (int i = 0; i < vecs.Count; i++)
		{
			zero += vecs[i];
		}
		return zero / vecs.Count;
	}

	private Quaternion CalculateAverage(List<Quaternion> quats, int index = 0)
	{
		if (index >= quats.Count - 1)
		{
			return quats[index];
		}
		return Quaternion.Lerp(quats[index], CalculateAverage(quats, index + 1), 0.5f);
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

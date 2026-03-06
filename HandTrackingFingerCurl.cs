using GorillaTagScripts;
using UnityEngine;

public class HandTrackingFingerCurl : MonoBehaviour
{
	[SerializeField]
	private OVRSkeleton skeleton;

	public float ActivationStart = 5f;

	public float ActivationEnd = 95f;

	public float CurlMultiplier = 1.2f;

	private Transform[] boneXforms;

	public static HandTrackingFingerCurl leftCurl;

	public static HandTrackingFingerCurl rightCurl;

	[SerializeField]
	private bool isLeft;

	public float ThumbCurl { get; private set; }

	public float TriggerCurl { get; private set; }

	public float GripCurl { get; private set; }

	private void Awake()
	{
		if (isLeft)
		{
			leftCurl = this;
		}
		else
		{
			rightCurl = this;
		}
		if (skeleton == null)
		{
			skeleton = GetComponent<OVRSkeleton>();
		}
		boneXforms = new Transform[84];
	}

	private void LateUpdate()
	{
		if (skeleton == null || skeleton.Bones == null || skeleton.Bones.Count == 0 || !SubscriptionManager.IsLocalSubscribed() || !SubscriptionManager.GetSubscriptionSettingBool(SubscriptionManager.SubscriptionFeatures.HandTracking))
		{
			return;
		}
		if (boneXforms[0] == null)
		{
			foreach (OVRBone bone in skeleton.Bones)
			{
				boneXforms[(int)bone.Id] = bone.Transform;
			}
		}
		ThumbCurl = CalcFingerCurl(OVRSkeleton.BoneId.Hand_Thumb3, OVRSkeleton.BoneId.Hand_Thumb2, OVRSkeleton.BoneId.Hand_Thumb1, OVRSkeleton.BoneId.Hand_Thumb0);
		TriggerCurl = CalcFingerCurl(OVRSkeleton.BoneId.Hand_Middle1, OVRSkeleton.BoneId.Hand_Index3, OVRSkeleton.BoneId.Hand_Index2, OVRSkeleton.BoneId.Hand_Index1);
		GripCurl = CalcFingerCurl(OVRSkeleton.BoneId.Hand_Ring3, OVRSkeleton.BoneId.Hand_Ring2, OVRSkeleton.BoneId.Hand_Ring1, OVRSkeleton.BoneId.Hand_Middle3);
	}

	private float CalcFingerCurl(OVRSkeleton.BoneId distal, OVRSkeleton.BoneId intermediate, OVRSkeleton.BoneId proximal, OVRSkeleton.BoneId metacarpal)
	{
		Transform transform = boneXforms[(int)distal];
		Transform transform2 = boneXforms[(int)intermediate];
		Transform transform3 = boneXforms[(int)proximal];
		Transform transform4 = boneXforms[(int)metacarpal];
		if (transform == null || transform2 == null || transform3 == null || transform4 == null)
		{
			return 0f;
		}
		Vector3 vector = transform.position - transform2.position;
		Vector3 vector2 = transform2.position - transform3.position;
		Vector3 to = transform3.position - transform4.position;
		float num = Vector3.Angle(vector, vector2);
		float num2 = Vector3.Angle(vector2, to);
		float num3 = (num + num2) * 0.5f;
		num3 *= CurlMultiplier;
		num3 = Mathf.InverseLerp(ActivationStart, ActivationEnd, num3);
		return Mathf.Clamp01(num3);
	}
}

using System;
using GorillaExtensions;
using Photon.Pun;
using UnityEngine;

namespace GorillaTag.Cosmetics;

public class EdibleWearable : MonoBehaviour
{
	[Serializable]
	public struct EdibleStateInfo
	{
		[Tooltip("Will be activated when this stage is reached.")]
		public GameObject gameObject;

		[Tooltip("Will be played when this stage is reached.")]
		public AudioClip sound;
	}

	[Tooltip("Check when using non cosmetic edible items like honeycomb")]
	public bool isNonRespawnable;

	[Tooltip("Eating sounds are played through this AudioSource using PlayOneShot.")]
	public AudioSource audioSource;

	[Tooltip("Volume each bite should play at.")]
	public float volume = 0.08f;

	[Tooltip("The slot this cosmetic resides.")]
	public VRRig.WearablePackedStateSlots wearablePackedStateSlot = VRRig.WearablePackedStateSlots.LeftHand;

	[Tooltip("Time between bites.")]
	public float biteCooldown = 1f;

	[Tooltip("How long it takes to pop back to the uneaten state after being fully eaten.")]
	public float respawnTime = 7f;

	[Tooltip("Distance from mouth to item required to trigger a bite.")]
	public float biteDistance = 0.5f;

	[Tooltip("Offset from Gorilla's head to mouth.")]
	public Vector3 gorillaHeadMouthOffset = new Vector3(0f, 0.0208f, 0.171f);

	[Tooltip("Offset from edible's transform to the bite point.")]
	public Vector3 edibleBiteOffset = new Vector3(0f, 0f, 0f);

	public EdibleStateInfo[] edibleStateInfos;

	private VRRig ownerRig;

	private bool isLocal;

	private bool isHandSlot;

	private bool isLeftHand;

	private GTBitOps.BitWriteInfo stateBitsWriteInfo;

	private int edibleState;

	private int previousEdibleState;

	private float lastEatTime;

	private float lastFullyEatenTime;

	private bool wasInBiteZoneLastFrame;

	protected void Awake()
	{
		edibleState = 0;
		previousEdibleState = 0;
		ownerRig = GetComponentInParent<VRRig>();
		isLocal = ownerRig != null && ownerRig.isOfflineVRRig;
		isHandSlot = wearablePackedStateSlot == VRRig.WearablePackedStateSlots.LeftHand || wearablePackedStateSlot == VRRig.WearablePackedStateSlots.RightHand;
		isLeftHand = wearablePackedStateSlot == VRRig.WearablePackedStateSlots.LeftHand;
		stateBitsWriteInfo = VRRig.WearablePackedStatesBitWriteInfos[(int)wearablePackedStateSlot];
	}

	protected void OnEnable()
	{
		if (ownerRig == null)
		{
			Debug.LogError("EdibleWearable \"" + base.transform.GetPath() + "\": Deactivating because ownerRig is null.", this);
			base.gameObject.SetActive(value: false);
			return;
		}
		for (int i = 0; i < edibleStateInfos.Length; i++)
		{
			edibleStateInfos[i].gameObject.SetActive(i == edibleState);
		}
	}

	protected virtual void LateUpdate()
	{
		if (isLocal)
		{
			LateUpdateLocal();
		}
		else
		{
			LateUpdateReplicated();
		}
		LateUpdateShared();
	}

	protected virtual void LateUpdateLocal()
	{
		if (edibleState == edibleStateInfos.Length - 1)
		{
			if (!isNonRespawnable && Time.time > lastFullyEatenTime + respawnTime)
			{
				edibleState = 0;
				previousEdibleState = 0;
				OnEdibleHoldableStateChange();
			}
			if (isNonRespawnable && Time.time > lastFullyEatenTime)
			{
				edibleState = 0;
				previousEdibleState = 0;
				OnEdibleHoldableStateChange();
				GorillaGameManager.instance.FindPlayerVRRig(NetworkSystem.Instance.LocalPlayer).netView.SendRPC("EnableNonCosmeticHandItemRPC", RpcTarget.All, false, isLeftHand);
			}
		}
		else if (Time.time > lastEatTime + biteCooldown)
		{
			Vector3 vector = base.transform.TransformPoint(edibleBiteOffset);
			bool flag = false;
			float num = biteDistance * biteDistance;
			if (!GorillaParent.hasInstance)
			{
				return;
			}
			if ((GorillaTagger.Instance.offlineVRRig.head.rigTarget.transform.TransformPoint(gorillaHeadMouthOffset) - vector).sqrMagnitude < num)
			{
				flag = true;
			}
			foreach (RigContainer activeRigContainer in VRRigCache.ActiveRigContainers)
			{
				VRRig rig = activeRigContainer.Rig;
				if (!flag)
				{
					if (rig.head == null || rig.head.rigTarget.IsNull())
					{
						break;
					}
					if ((rig.head.rigTarget.transform.TransformPoint(gorillaHeadMouthOffset) - vector).sqrMagnitude < num)
					{
						flag = true;
					}
				}
			}
			if (flag && !wasInBiteZoneLastFrame && edibleState < edibleStateInfos.Length)
			{
				edibleState++;
				lastEatTime = Time.time;
				lastFullyEatenTime = Time.time;
			}
			wasInBiteZoneLastFrame = flag;
		}
		ownerRig.WearablePackedStates = GTBitOps.WriteBits(ownerRig.WearablePackedStates, stateBitsWriteInfo, edibleState);
	}

	protected virtual void LateUpdateReplicated()
	{
		edibleState = GTBitOps.ReadBits(ownerRig.WearablePackedStates, stateBitsWriteInfo.index, stateBitsWriteInfo.valueMask);
	}

	protected virtual void LateUpdateShared()
	{
		int num = edibleState;
		if (num != previousEdibleState)
		{
			OnEdibleHoldableStateChange();
		}
		previousEdibleState = num;
	}

	protected virtual void OnEdibleHoldableStateChange()
	{
		if (previousEdibleState >= 0 && previousEdibleState < edibleStateInfos.Length)
		{
			edibleStateInfos[previousEdibleState].gameObject.SetActive(value: false);
		}
		if (edibleState >= 0 && edibleState < edibleStateInfos.Length)
		{
			edibleStateInfos[edibleState].gameObject.SetActive(value: true);
		}
		if (edibleState > 0 && edibleState < edibleStateInfos.Length && audioSource != null)
		{
			audioSource.GTPlayOneShot(edibleStateInfos[edibleState].sound, volume);
		}
		if (edibleState == edibleStateInfos.Length && audioSource != null)
		{
			audioSource.GTPlayOneShot(edibleStateInfos[edibleState - 1].sound, volume);
		}
		float amplitude = GorillaTagger.Instance.tapHapticStrength / 4f;
		float fixedDeltaTime = Time.fixedDeltaTime;
		if (isLocal && isHandSlot)
		{
			GorillaTagger.Instance.StartVibration(isLeftHand, amplitude, fixedDeltaTime);
		}
	}
}

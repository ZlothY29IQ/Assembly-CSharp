using GorillaTag;
using GorillaTag.CosmeticSystem;
using Photon.Pun;
using UnityEngine;
using UnityEngine.XR;

public class FingerFlagWearable : MonoBehaviour, ISpawnable
{
	[Header("Wearable Settings")]
	public bool attachedToLeftHand = true;

	[Header("Bones")]
	public Transform pinkyRingBone;

	public Transform thumbRingBone;

	public Transform[] clothBones;

	public Transform[] clothRigidbodies;

	[Header("Animation")]
	public Animator animator;

	public float extendSpeed = 1.5f;

	public float retractSpeed = 2.25f;

	[Header("Audio")]
	public AudioSource audioSource;

	public AudioClip extendAudioClip;

	public AudioClip retractAudioClip;

	[Header("Vibration")]
	public float extendVibrationDuration = 0.05f;

	public float extendVibrationStrength = 0.2f;

	public float retractVibrationDuration = 0.05f;

	public float retractVibrationStrength = 0.2f;

	private readonly int retractExtendTimeAnimParam = Animator.StringToHash("retractExtendTime");

	private bool networkedExtended;

	private bool extended;

	private bool fullyRetracted;

	private float retractExtendTime;

	private InputDevice inputDevice;

	private VRRig myRig;

	private int stateBitIndex;

	bool ISpawnable.IsSpawned { get; set; }

	ECosmeticSelectSide ISpawnable.CosmeticSelectedSide { get; set; }

	void ISpawnable.OnSpawn(VRRig rig)
	{
		myRig = GetComponentInParent<VRRig>(includeInactive: true);
		if (!myRig)
		{
			base.gameObject.SetActive(value: false);
		}
	}

	void ISpawnable.OnDespawn()
	{
	}

	protected void OnEnable()
	{
		int num = (attachedToLeftHand ? 1 : 2);
		stateBitIndex = VRRig.WearablePackedStatesBitWriteInfos[num].index;
		OnExtendStateChanged(playAudio: false);
	}

	private void UpdateLocal()
	{
		int node = (attachedToLeftHand ? 4 : 5);
		bool flag = ControllerInputPoller.GripFloat((XRNode)node) > 0.25f;
		bool flag2 = ControllerInputPoller.PrimaryButtonPress((XRNode)node);
		bool flag3 = ControllerInputPoller.SecondaryButtonPress((XRNode)node);
		bool flag4 = flag && (flag2 || flag3);
		networkedExtended = flag4;
		if (PhotonNetwork.InRoom && (bool)myRig)
		{
			myRig.WearablePackedStates = GTBitOps.WriteBit(myRig.WearablePackedStates, stateBitIndex, networkedExtended);
		}
	}

	private void UpdateShared()
	{
		if (extended != networkedExtended)
		{
			extended = networkedExtended;
			OnExtendStateChanged(playAudio: true);
		}
		bool num = fullyRetracted;
		fullyRetracted = extended && retractExtendTime <= 0f;
		if (num != fullyRetracted)
		{
			Transform[] array = clothRigidbodies;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].gameObject.SetActive(!fullyRetracted);
			}
		}
		UpdateAnimation();
	}

	private void UpdateReplicated()
	{
		if (myRig != null && !myRig.isOfflineVRRig)
		{
			networkedExtended = GTBitOps.ReadBit(myRig.WearablePackedStates, stateBitIndex);
		}
	}

	public bool IsMyItem()
	{
		if (myRig != null)
		{
			return myRig.isOfflineVRRig;
		}
		return false;
	}

	protected void LateUpdate()
	{
		if (IsMyItem())
		{
			UpdateLocal();
		}
		else
		{
			UpdateReplicated();
		}
		UpdateShared();
	}

	private void UpdateAnimation()
	{
		float num = (extended ? extendSpeed : (0f - retractSpeed));
		retractExtendTime = Mathf.Clamp01(retractExtendTime + Time.deltaTime * num);
		animator.SetFloat(retractExtendTimeAnimParam, retractExtendTime);
	}

	private void OnExtendStateChanged(bool playAudio)
	{
		audioSource.clip = (extended ? extendAudioClip : retractAudioClip);
		if (playAudio)
		{
			audioSource.GTPlay();
		}
		if (IsMyItem() && (bool)GorillaTagger.Instance)
		{
			GorillaTagger.Instance.StartVibration(attachedToLeftHand, extended ? extendVibrationDuration : retractVibrationDuration, extended ? extendVibrationStrength : retractVibrationStrength);
		}
	}
}

using GorillaTag;
using GorillaTag.CosmeticSystem;
using Photon.Pun;
using UnityEngine;
using UnityEngine.XR;

public class FingerTorch : MonoBehaviour, ISpawnable
{
	[Header("Wearable Settings")]
	public bool attachedToLeftHand = true;

	[Header("Bones")]
	public Transform pinkyRingBone;

	public Transform thumbRingBone;

	[Header("Audio")]
	public AudioSource audioSource;

	public AudioClip extendAudioClip;

	public AudioClip retractAudioClip;

	[Header("Vibration")]
	public float extendVibrationDuration = 0.05f;

	public float extendVibrationStrength = 0.2f;

	public float retractVibrationDuration = 0.05f;

	public float retractVibrationStrength = 0.2f;

	[Header("Particle FX")]
	public GameObject particleFX;

	private bool networkedExtended;

	private bool extended;

	private InputDevice inputDevice;

	private VRRig myRig;

	private int stateBitIndex;

	bool ISpawnable.IsSpawned { get; set; }

	ECosmeticSelectSide ISpawnable.CosmeticSelectedSide { get; set; }

	void ISpawnable.OnSpawn(VRRig rig)
	{
		myRig = rig;
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

	protected void OnDisable()
	{
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
			particleFX.SetActive(extended);
		}
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

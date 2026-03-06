using System;
using GorillaExtensions;
using GorillaLocomotion;
using GorillaLocomotion.Climbing;
using Photon.Pun;
using UnityEngine;

public class CrankableToyCarHoldable : TransferrableObject
{
	[SerializeField]
	private TransferrableObjectHoldablePart_Crank crank;

	[SerializeField]
	private CrankableToyCarDeployed deployedCar;

	[SerializeField]
	private GameObject deployablePart;

	[SerializeField]
	private GameObject disabledWhileDeployed;

	[SerializeField]
	private float crankAnglePerClick;

	[SerializeField]
	private float maxCrankStrength;

	[SerializeField]
	private float minClickPitch;

	[SerializeField]
	private float maxClickPitch;

	[SerializeField]
	private float minLifetime;

	[SerializeField]
	private float maxLifetime;

	[SerializeField]
	private SoundBankPlayer clickSound;

	[SerializeField]
	private SoundBankPlayer overCrankedSound;

	[SerializeField]
	private float crankHapticStrength = 0.1f;

	[SerializeField]
	private float crankHapticDuration = 0.05f;

	[SerializeField]
	private float overcrankHapticStrength = 0.8f;

	[SerializeField]
	private float overcrankHapticDuration = 0.05f;

	private float currentCrankStrength;

	private float currentCrankClickAmount;

	private RubberDuckEvents _events;

	protected override void Start()
	{
		base.Start();
		crank.SetOnCrankedCallback(OnCranked);
	}

	internal override void OnEnable()
	{
		base.OnEnable();
		if (base.gameObject.activeInHierarchy)
		{
			if (_events == null)
			{
				_events = base.gameObject.GetOrAddComponent<RubberDuckEvents>();
			}
			NetPlayer netPlayer = ((base.myOnlineRig != null) ? base.myOnlineRig.creator : ((!(base.myRig != null)) ? null : ((base.myRig.creator != null) ? base.myRig.creator : NetworkSystem.Instance.LocalPlayer)));
			if (netPlayer != null && _events != null)
			{
				_events.Init(netPlayer);
				_events.Activate += new Action<int, int, object[], PhotonMessageInfoWrapped>(OnDeployRPC);
			}
			else
			{
				Debug.LogError("Failed to get a reference to the Photon Player needed to hook up the cosmetic event");
			}
			itemState &= (ItemStates)(-2);
		}
	}

	internal override void OnDisable()
	{
		base.OnDisable();
		if (_events != null)
		{
			_events.Dispose();
		}
	}

	protected override void LateUpdateReplicated()
	{
		base.LateUpdateReplicated();
		if (itemState.HasFlag(ItemStates.State0))
		{
			if (!deployablePart.activeSelf)
			{
				OnCarDeployed();
			}
		}
		else if (deployablePart.activeSelf)
		{
			OnCarReturned();
		}
	}

	private void OnCranked(float deltaAngle)
	{
		currentCrankStrength += Mathf.Abs(deltaAngle);
		currentCrankClickAmount += deltaAngle;
		if (!(Mathf.Abs(currentCrankClickAmount) > crankAnglePerClick))
		{
			return;
		}
		if (currentCrankStrength >= maxCrankStrength)
		{
			overCrankedSound.Play();
			VRRig vRRig = ownerRig;
			if ((object)vRRig != null && vRRig.isLocal)
			{
				GorillaTagger.Instance.StartVibration(InRightHand(), overcrankHapticStrength, overcrankHapticDuration);
			}
		}
		else
		{
			float value = Mathf.Lerp(minClickPitch, maxClickPitch, Mathf.InverseLerp(0f, maxCrankStrength, currentCrankStrength));
			SoundBankPlayer soundBankPlayer = clickSound;
			float? pitchOverride = value;
			soundBankPlayer.Play(null, pitchOverride);
			VRRig vRRig2 = ownerRig;
			if ((object)vRRig2 != null && vRRig2.isLocal)
			{
				GorillaTagger.Instance.StartVibration(InRightHand(), crankHapticStrength, crankHapticDuration);
			}
		}
		currentCrankClickAmount = 0f;
	}

	public override bool OnRelease(DropZone zoneReleased, GameObject releasingHand)
	{
		if (!base.OnRelease(zoneReleased, releasingHand))
		{
			return false;
		}
		if (VRRigCache.Instance.localRig.Rig != ownerRig)
		{
			return false;
		}
		if (currentCrankStrength == 0f)
		{
			return true;
		}
		bool isLeftHand = releasingHand == EquipmentInteractor.instance.leftHand;
		GorillaVelocityTracker interactPointVelocityTracker = GTPlayer.Instance.GetInteractPointVelocityTracker(isLeftHand);
		Vector3 vector = base.transform.TransformPoint(Vector3.zero);
		Quaternion rotation = base.transform.rotation;
		Vector3 averageVelocity = interactPointVelocityTracker.GetAverageVelocity(worldSpace: true);
		float num = Mathf.Lerp(minLifetime, maxLifetime, Mathf.Clamp01(Mathf.InverseLerp(0f, maxCrankStrength, currentCrankStrength)));
		DeployCarLocal(vector, rotation, averageVelocity, num);
		if (PhotonNetwork.InRoom)
		{
			_events.Activate.RaiseOthers(BitPackUtils.PackWorldPosForNetwork(vector), BitPackUtils.PackQuaternionForNetwork(rotation), BitPackUtils.PackWorldPosForNetwork(averageVelocity * 100f), num);
		}
		currentCrankStrength = 0f;
		return true;
	}

	private void DeployCarLocal(Vector3 launchPos, Quaternion launchRot, Vector3 releaseVel, float lifetime, bool isRemote = false)
	{
		if (disabledWhileDeployed.activeSelf)
		{
			deployedCar.Deploy(this, launchPos, launchRot, releaseVel, lifetime, isRemote);
		}
	}

	private void OnDeployRPC(int sender, int receiver, object[] args, PhotonMessageInfoWrapped info)
	{
		if ((bool)this && sender == receiver && info.senderID == ownerRig.creator.ActorNumber)
		{
			MonkeAgent.IncrementRPCCall(info, "OnDeployRPC");
			Vector3 v = BitPackUtils.UnpackWorldPosFromNetwork((long)args[0]);
			Quaternion q = BitPackUtils.UnpackQuaternionFromNetwork((int)args[1]);
			Vector3 v2 = BitPackUtils.UnpackWorldPosFromNetwork((long)args[2]) / 100f;
			float lifetime = (float)args[3];
			if (v.IsValid(10000f) && q.IsValid() && v2.IsValid(10000f))
			{
				DeployCarLocal(v, q, v2, lifetime, isRemote: true);
			}
		}
	}

	public void OnCarDeployed()
	{
		itemState |= ItemStates.State0;
		deployablePart.SetActive(value: true);
		disabledWhileDeployed.SetActive(value: false);
	}

	public void OnCarReturned()
	{
		itemState &= (ItemStates)(-2);
		deployablePart.SetActive(value: false);
		disabledWhileDeployed.SetActive(value: true);
		clickSound.RestartSequence();
	}
}

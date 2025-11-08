using System;
using GorillaNetworking;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

public class HoldableLighterCosmetic : MonoBehaviour
{
	public enum LighterResult
	{
		Flicker,
		Light,
		Explode
	}

	private int OwnerID;

	[Header("Weights (0 to 1 total)")]
	[Range(0f, 1f)]
	public float flickerWeight = 0.5f;

	[Range(0f, 1f)]
	public float lightWeight = 0.3f;

	[Range(0f, 1f)]
	public float explodeWeight = 0.2f;

	[Header("Unity Events")]
	public UnityEvent OnFlicker;

	public UnityEvent OnLight;

	public UnityEvent OnExplode;

	public UnityEvent OnTriggerRelease;

	private LighterResult[] resultTimeline;

	private bool triggerHeld;

	private float lastCheckTime;

	private VRRig rig;

	private TransferrableObject parentTransferable;

	private void OnEnable()
	{
	}

	private void Awake()
	{
		rig = GetComponentInParent<VRRig>();
		parentTransferable = GetComponentInParent<TransferrableObject>();
	}

	private bool IsMyItem()
	{
		if (rig != null)
		{
			return rig.isOfflineVRRig;
		}
		return false;
	}

	private void DebugPull()
	{
		TriggerPulled();
	}

	private void DebugRelease()
	{
		TriggerReleased();
	}

	public void TriggerPulled()
	{
		triggerHeld = true;
		if (OwnerID == 0)
		{
			TrySetID();
		}
		double time = PhotonNetwork.Time;
		switch (GetResultAtTime(time, OwnerID))
		{
		case LighterResult.Flicker:
			OnFlicker?.Invoke();
			if (parentTransferable.IsMyItem())
			{
				GorillaTagger.Instance.StartVibration(parentTransferable.InLeftHand(), 0.1f, 0.1f);
			}
			break;
		case LighterResult.Light:
			OnLight?.Invoke();
			if (parentTransferable.IsMyItem())
			{
				GorillaTagger.Instance.StartVibration(parentTransferable.InLeftHand(), 0.1f, 0.1f);
			}
			break;
		case LighterResult.Explode:
			OnExplode?.Invoke();
			if (parentTransferable.IsMyItem())
			{
				GorillaTagger.Instance.StartVibration(parentTransferable.InLeftHand(), 0.75f, 0.5f);
			}
			break;
		}
	}

	private LighterResult GetResultAtTime(double photonTime, int seed)
	{
		int num = (int)Math.Floor(photonTime);
		float num2 = (float)new System.Random(seed ^ num).NextDouble();
		if (num2 < explodeWeight)
		{
			return LighterResult.Explode;
		}
		if (num2 < explodeWeight + lightWeight)
		{
			return LighterResult.Light;
		}
		return LighterResult.Flicker;
	}

	public void TriggerReleased()
	{
		triggerHeld = false;
		OnTriggerRelease?.Invoke();
	}

	private void TrySetID()
	{
		if (parentTransferable.IsLocalObject())
		{
			PlayFabAuthenticator instance = PlayFabAuthenticator.instance;
			if (instance != null)
			{
				OwnerID = (instance.GetPlayFabPlayerId() + GetType()).GetStaticHash();
			}
		}
		else if (parentTransferable.targetRig != null && parentTransferable.targetRig.creator != null)
		{
			OwnerID = (parentTransferable.targetRig.creator.UserId + GetType()).GetStaticHash();
		}
	}
}

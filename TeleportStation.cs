using UnityEngine;
using UnityEngine.Events;

public class TeleportStation : Tappable
{
	[SerializeField]
	private Transform target;

	[SerializeField]
	private Vector3 targetPos;

	[SerializeField]
	private float targetRot;

	[SerializeField]
	private Vector3 targetSlop;

	[SerializeField]
	private GTZone teleportToZone;

	[SerializeField]
	private GameObject firstPersonEffect;

	[SerializeField]
	private GameObject thirdPersonEffectStart;

	[SerializeField]
	private GameObject thirdPersonEffectEnd;

	[SerializeField]
	private int effectTime;

	[SerializeField]
	private UnityEvent on3PTeleport;

	[SerializeField]
	private UnityEvent on1PTeleport;

	private void Start()
	{
		firstPersonEffect.SetActive(value: false);
		thirdPersonEffectStart.SetActive(value: false);
		thirdPersonEffectEnd.SetActive(value: false);
		TeleportStationManager.Initialize(firstPersonEffect, thirdPersonEffectStart, thirdPersonEffectEnd);
	}

	public override void OnTapLocal(float tapStrength, float tapTime, PhotonMessageInfoWrapped sender)
	{
		RigContainer playerRig;
		if (sender.Sender == null || VRRig.LocalRig.Creator == sender.Sender)
		{
			TeleportStationManager.Instance.FirstPersonTeleport(targetPos, targetRot, targetSlop, teleportToZone, effectTime);
			on1PTeleport?.Invoke();
		}
		else if (VRRigCache.Instance.TryGetVrrig(sender.Sender, out playerRig) && FXSystem.CheckCallSpam(playerRig.Rig.fxSettings, 13, sender.SentServerTime))
		{
			TeleportStationManager.Instance.ThirdPersonTeleport(playerRig.Rig, effectTime);
			on3PTeleport?.Invoke();
		}
	}

	public void Test3pTeleport()
	{
		TeleportStationManager.Instance.ThirdPersonTeleport(VRRig.LocalRig, effectTime);
	}
}

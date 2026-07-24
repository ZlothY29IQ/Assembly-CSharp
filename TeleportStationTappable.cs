using UnityEngine;
using UnityEngine.Events;

public class TeleportStationTappable : Tappable
{
	[SerializeField]
	private XSceneRef _teleportStationRef;

	private TeleportStation _teleportStation;

	[Space]
	[SerializeField]
	private GameObject _firstPersonEffect;

	[SerializeField]
	private GameObject _thirdPersonEffectStart;

	[SerializeField]
	private GameObject _thirdPersonEffectEnd;

	[SerializeField]
	private UnityEvent _on3PTeleport;

	[SerializeField]
	private UnityEvent _on1PTeleport;

	private void Start()
	{
		_teleportStationRef.TryResolve(out _teleportStation);
		_firstPersonEffect.SetActive(value: false);
		_thirdPersonEffectStart.SetActive(value: false);
		_thirdPersonEffectEnd.SetActive(value: false);
		TeleportStationManager.Initialize(_firstPersonEffect, _thirdPersonEffectStart, _thirdPersonEffectEnd);
	}

	public override void OnTapLocal(float tapStrength, float tapTime, PhotonMessageInfoWrapped sender)
	{
		RigContainer playerRig;
		if (_teleportStation == null)
		{
			GTDev.LogWarning("TeleportStation is null!");
		}
		else if (sender.Sender == null || VRRig.LocalRig.Creator == sender.Sender)
		{
			_teleportStation.Attempt1PTeleport(sender);
			_on1PTeleport?.Invoke();
		}
		else if (VRRigCache.Instance.TryGetVrrig(sender.Sender, out playerRig) && FXSystem.CheckCallSpam(playerRig.Rig.fxSettings, 13, sender.SentServerTime))
		{
			_teleportStation.Attempt3PTeleport(sender);
			_on3PTeleport?.Invoke();
		}
	}
}

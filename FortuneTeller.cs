using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;

public class FortuneTeller : MonoBehaviourPunCallbacks
{
	[Serializable]
	public struct FortuneTellerResultFanfare
	{
		public FortuneResults.FortuneCategoryType type;

		public PlayableAsset fanfare;
	}

	[SerializeField]
	private FXType limiterType;

	[SerializeField]
	private FortuneTellerButton button;

	[SerializeField]
	private TextMeshPro text;

	[SerializeField]
	private FortuneResults results;

	[SerializeField]
	private PlayableDirector playable;

	[SerializeField]
	private Animator animator;

	[SerializeField]
	private float waitDurationBeforeAttractAnim;

	[SerializeField]
	private FortuneTellerResultFanfare[] resultFanfares;

	[Header("Grey Zone Visuals")]
	[SerializeField]
	private bool changeMaterialsInGreyZone;

	[SerializeField]
	private MeshRenderer boothRenderer;

	[SerializeField]
	private Material boothDefaultMaterial;

	[SerializeField]
	private Material boothGreyZoneMaterial;

	[SerializeField]
	private MeshRenderer beardRenderer;

	[SerializeField]
	private Material beardDefaultMaterial;

	[SerializeField]
	private Material beardGreyZoneMaterial;

	[SerializeField]
	private SkinnedMeshRenderer tellerRenderer;

	[SerializeField]
	private List<Material> tellerDefaultMaterials;

	[SerializeField]
	private List<Material> tellerGreyZoneMaterials;

	private FortuneResults.FortuneResult latestFortune;

	private CallLimiter triggerNewFortuneLimiter = new CallLimiter(10, 1f);

	private CallLimiter triggerUpdateFortuneLimiter = new CallLimiter(10, 1f);

	private AnimHashId trigger_attract = "Attract";

	private AnimHashId trigger_prediction = "Prediction";

	private float nextAttractAnimTimestamp;

	private Coroutine attractModeMonitor;

	private void Awake()
	{
		if (changeMaterialsInGreyZone && GreyZoneManager.Instance != null)
		{
			GreyZoneManager instance = GreyZoneManager.Instance;
			instance.OnGreyZoneActivated = (Action)Delegate.Combine(instance.OnGreyZoneActivated, new Action(GreyZoneActivated));
			GreyZoneManager instance2 = GreyZoneManager.Instance;
			instance2.OnGreyZoneDeactivated = (Action)Delegate.Combine(instance2.OnGreyZoneDeactivated, new Action(GreyZoneDeactivated));
		}
	}

	private void OnDestroy()
	{
		if (GreyZoneManager.Instance != null)
		{
			GreyZoneManager instance = GreyZoneManager.Instance;
			instance.OnGreyZoneActivated = (Action)Delegate.Remove(instance.OnGreyZoneActivated, new Action(GreyZoneActivated));
			GreyZoneManager instance2 = GreyZoneManager.Instance;
			instance2.OnGreyZoneDeactivated = (Action)Delegate.Remove(instance2.OnGreyZoneDeactivated, new Action(GreyZoneDeactivated));
		}
	}

	public override void OnEnable()
	{
		base.OnEnable();
		nextAttractAnimTimestamp = Time.time + waitDurationBeforeAttractAnim;
		if ((bool)button)
		{
			button.onPressed += HandlePressedButton;
		}
	}

	public override void OnDisable()
	{
		base.OnDisable();
		if ((bool)button)
		{
			button.onPressed -= HandlePressedButton;
		}
	}

	private void GreyZoneActivated()
	{
		boothRenderer.material = boothGreyZoneMaterial;
		beardRenderer.material = beardGreyZoneMaterial;
		tellerRenderer.SetMaterials(tellerGreyZoneMaterials);
	}

	private void GreyZoneDeactivated()
	{
		boothRenderer.material = boothDefaultMaterial;
		beardRenderer.material = beardDefaultMaterial;
		tellerRenderer.SetMaterials(tellerDefaultMaterials);
	}

	public override void OnPlayerEnteredRoom(Player newPlayer)
	{
		base.OnPlayerEnteredRoom(newPlayer);
		if (PhotonNetwork.InRoom && PhotonNetwork.LocalPlayer.IsMasterClient)
		{
			base.photonView.RPC("TriggerUpdateFortuneRPC", newPlayer, (int)latestFortune.fortuneType, latestFortune.resultIndex);
		}
	}

	public override void OnMasterClientSwitched(Player newMasterClient)
	{
		if (PhotonNetwork.IsMasterClient)
		{
			StartAttractModeMonitor();
		}
	}

	public override void OnJoinedRoom()
	{
		if (PhotonNetwork.IsMasterClient)
		{
			StartAttractModeMonitor();
		}
	}

	private void HandlePressedButton(GorillaPressableButton button, bool isLeft)
	{
		if (base.photonView.IsMine)
		{
			SendNewFortune();
		}
		else if (PhotonNetwork.InRoom)
		{
			base.photonView.RPC("RequestFortuneRPC", RpcTarget.MasterClient);
		}
	}

	[PunRPC]
	private void RequestFortuneRPC(PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "RequestFortune");
		if (NetworkSystem.Instance.IsMasterClient && info.Sender != null && VRRigCache.Instance.TryGetVrrig(info.Sender, out var playerRig))
		{
			CallLimitType<CallLimiter> callLimitType = playerRig.Rig.fxSettings.callSettings[(int)limiterType];
			if (callLimitType.UseNetWorkTime ? callLimitType.CallLimitSettings.CheckCallServerTime(info.SentServerTime) : callLimitType.CallLimitSettings.CheckCallTime(Time.time))
			{
				SendNewFortune();
			}
		}
	}

	private void SendNewFortune()
	{
		if (!(playable.time > 0.0) || !(playable.time < playable.duration))
		{
			latestFortune = results.GetResult();
			UpdateFortune(latestFortune, newFortune: true);
			if (PhotonNetwork.InRoom)
			{
				base.photonView.RPC("TriggerNewFortuneRPC", RpcTarget.Others, (int)latestFortune.fortuneType, latestFortune.resultIndex);
			}
		}
	}

	[PunRPC]
	private void TriggerUpdateFortuneRPC(int fortuneType, int resultIndex, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "TriggerUpdateFortune");
		if (info.Sender != PhotonNetwork.MasterClient)
		{
			MonkeAgent.instance.SendReport("Sent TriggerUpdateFortune when they weren't the master client", info.Sender.UserId, info.Sender.NickName);
		}
		else if (triggerUpdateFortuneLimiter.CheckCallTime(Time.time))
		{
			latestFortune = new FortuneResults.FortuneResult((FortuneResults.FortuneCategoryType)fortuneType, resultIndex);
			UpdateFortune(latestFortune, newFortune: false);
		}
	}

	[PunRPC]
	private void TriggerNewFortuneRPC(int fortuneType, int resultIndex, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "TriggerNewFortune");
		if (info.Sender != PhotonNetwork.MasterClient)
		{
			MonkeAgent.instance.SendReport("Sent TriggerNewFortune when they weren't the master client", info.Sender.UserId, info.Sender.NickName);
		}
		else if (triggerNewFortuneLimiter.CheckCallTime(Time.time))
		{
			latestFortune = new FortuneResults.FortuneResult((FortuneResults.FortuneCategoryType)fortuneType, resultIndex);
			nextAttractAnimTimestamp = Time.time + waitDurationBeforeAttractAnim;
			UpdateFortune(latestFortune, newFortune: true);
		}
	}

	private void StartAttractModeMonitor()
	{
		if (attractModeMonitor == null)
		{
			attractModeMonitor = StartCoroutine(AttractModeMonitor());
		}
	}

	private IEnumerator AttractModeMonitor()
	{
		while (PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
		{
			if (Time.time >= nextAttractAnimTimestamp)
			{
				SendAttractAnim();
			}
			yield return new WaitForSeconds(nextAttractAnimTimestamp - Time.time);
		}
		attractModeMonitor = null;
	}

	private void SendAttractAnim()
	{
		if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
		{
			base.photonView.RPC("TriggerAttractAnimRPC", RpcTarget.All);
		}
	}

	[PunRPC]
	private void TriggerAttractAnimRPC(PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "TriggerAttractAnim");
		if (info.Sender != PhotonNetwork.MasterClient)
		{
			MonkeAgent.instance.SendReport("Sent TriggerAttractAnim when they weren't the master client", info.Sender.UserId, info.Sender.NickName);
			return;
		}
		animator.SetTrigger(trigger_attract);
		nextAttractAnimTimestamp = Time.time + waitDurationBeforeAttractAnim;
	}

	private void UpdateFortune(FortuneResults.FortuneResult result, bool newFortune)
	{
		if ((bool)results)
		{
			PlayableAsset resultFanfare = GetResultFanfare(result.fortuneType);
			if ((bool)resultFanfare)
			{
				playable.initialTime = (newFortune ? 0.0 : resultFanfare.duration);
				playable.Play(resultFanfare, DirectorWrapMode.Hold);
				animator.SetTrigger(trigger_prediction);
				nextAttractAnimTimestamp = Time.time + waitDurationBeforeAttractAnim;
			}
		}
	}

	public void ApplyFortuneText()
	{
		text.text = results.GetResultText(latestFortune).ToUpper();
	}

	private PlayableAsset GetResultFanfare(FortuneResults.FortuneCategoryType fortuneType)
	{
		FortuneTellerResultFanfare[] array = resultFanfares;
		for (int i = 0; i < array.Length; i++)
		{
			FortuneTellerResultFanfare fortuneTellerResultFanfare = array[i];
			if (fortuneTellerResultFanfare.type == fortuneType)
			{
				return fortuneTellerResultFanfare.fanfare;
			}
		}
		return null;
	}
}

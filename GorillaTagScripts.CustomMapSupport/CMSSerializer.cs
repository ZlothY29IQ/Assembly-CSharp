using System;
using System.Collections.Generic;
using GorillaTagScripts.VirtualStumpCustomMaps;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTagScripts.CustomMapSupport;

internal class CMSSerializer : GorillaSerializer
{
	[OnEnterPlay_SetNull]
	private static volatile CMSSerializer instance;

	[OnEnterPlay_Set(false)]
	private static bool hasInstance;

	private static Dictionary<string, Dictionary<byte, CMSTrigger>> registeredTriggersPerScene = new Dictionary<string, Dictionary<byte, CMSTrigger>>();

	private static List<byte> triggerHistory = new List<byte>();

	private static Dictionary<byte, byte> triggerCounts = new Dictionary<byte, byte>();

	private static bool waitingForTriggerHistory;

	private static List<string> scenesWaitingForTriggerHistory = new List<string>();

	private static bool waitingForTriggerCounts;

	private static List<string> scenesWaitingForTriggerCounts = new List<string>();

	private static CallLimiter ActivateTriggerCallLimiter = new CallLimiter(50, 1f);

	public static UnityEvent<string> OnTriggerHistoryProcessedForScene = new UnityEvent<string>();

	public void Awake()
	{
		if (instance != null)
		{
			UnityEngine.Object.Destroy(this);
		}
		instance = this;
		hasInstance = true;
	}

	public void OnEnable()
	{
		CustomMapManager.OnMapLoadComplete.RemoveListener(OnCustomMapLoaded);
		CustomMapManager.OnMapLoadComplete.AddListener(OnCustomMapLoaded);
	}

	public void OnDisable()
	{
		CustomMapManager.OnMapLoadComplete.RemoveListener(OnCustomMapLoaded);
	}

	private void OnCustomMapLoaded(bool success)
	{
		if (success)
		{
			RequestSyncTriggerHistory();
		}
	}

	public static void ResetSyncedMapObjects()
	{
		triggerHistory.Clear();
		triggerCounts.Clear();
		registeredTriggersPerScene.Clear();
		waitingForTriggerHistory = false;
		waitingForTriggerCounts = false;
	}

	public static void RegisterTrigger(string sceneName, CMSTrigger trigger)
	{
		if (registeredTriggersPerScene.TryGetValue(sceneName, out var value))
		{
			if (!value.ContainsKey(trigger.GetID()))
			{
				value.Add(trigger.GetID(), trigger);
			}
		}
		else
		{
			registeredTriggersPerScene.Add(sceneName, new Dictionary<byte, CMSTrigger> { 
			{
				trigger.GetID(),
				trigger
			} });
		}
	}

	private static bool TryGetRegisteredTrigger(byte triggerID, out CMSTrigger trigger)
	{
		trigger = null;
		foreach (KeyValuePair<string, Dictionary<byte, CMSTrigger>> item in registeredTriggersPerScene)
		{
			if (item.Value.TryGetValue(triggerID, out trigger))
			{
				return true;
			}
		}
		return false;
	}

	public static void UnregisterTriggers(string forScene)
	{
		registeredTriggersPerScene.Remove(forScene);
	}

	public static void ResetTrigger(byte triggerID)
	{
		triggerCounts.Remove(triggerID);
	}

	private static void RequestSyncTriggerHistory()
	{
		if (hasInstance && NetworkSystem.Instance.InRoom && !NetworkSystem.Instance.IsMasterClient)
		{
			waitingForTriggerHistory = true;
			waitingForTriggerCounts = true;
			instance.SendRPC("RequestSyncTriggerHistory_RPC", false);
		}
	}

	[PunRPC]
	private void RequestSyncTriggerHistory_RPC(PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "RequestSyncTriggerHistory_RPC");
		if (NetworkSystem.Instance.InRoom && NetworkSystem.Instance.IsMasterClient)
		{
			NetPlayer player = NetworkSystem.Instance.GetPlayer(info.Sender);
			if (!player.CheckSingleCallRPC(NetPlayer.SingleCallRPC.CMS_RequestTriggerHistory))
			{
				player.ReceivedSingleCallRPC(NetPlayer.SingleCallRPC.CMS_RequestTriggerHistory);
				byte[] array = triggerHistory.ToArray();
				SendRPC("SyncTriggerHistory_RPC", info.Sender, array);
				SendRPC("SyncTriggerCounts_RPC", info.Sender, triggerCounts);
			}
		}
	}

	[PunRPC]
	private void SyncTriggerHistory_RPC(byte[] syncedTriggerHistory, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "SyncTriggerHistory_RPC");
		if (!NetworkSystem.Instance.InRoom || !info.Sender.IsMasterClient)
		{
			return;
		}
		NetPlayer player = NetworkSystem.Instance.GetPlayer(info.Sender);
		if (player.CheckSingleCallRPC(NetPlayer.SingleCallRPC.CMS_SyncTriggerHistory))
		{
			return;
		}
		player.ReceivedSingleCallRPC(NetPlayer.SingleCallRPC.CMS_SyncTriggerHistory);
		if (!waitingForTriggerHistory)
		{
			return;
		}
		triggerHistory.Clear();
		if (!syncedTriggerHistory.IsNullOrEmpty())
		{
			triggerHistory.AddRange(syncedTriggerHistory);
		}
		waitingForTriggerHistory = false;
		foreach (string item in scenesWaitingForTriggerHistory)
		{
			ProcessTriggerHistory(item);
		}
		scenesWaitingForTriggerHistory.Clear();
	}

	[PunRPC]
	private void SyncTriggerCounts_RPC(Dictionary<byte, byte> syncedTriggerCounts, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "SyncTriggerCounts_RPC");
		if (!NetworkSystem.Instance.InRoom || !info.Sender.IsMasterClient)
		{
			return;
		}
		NetPlayer player = NetworkSystem.Instance.GetPlayer(info.Sender);
		if (player.CheckSingleCallRPC(NetPlayer.SingleCallRPC.CMS_SyncTriggerCounts))
		{
			return;
		}
		player.ReceivedSingleCallRPC(NetPlayer.SingleCallRPC.CMS_SyncTriggerCounts);
		if (!waitingForTriggerCounts)
		{
			return;
		}
		triggerCounts.Clear();
		if (syncedTriggerCounts != null && syncedTriggerCounts.Count > 0)
		{
			triggerCounts = syncedTriggerCounts;
		}
		waitingForTriggerCounts = false;
		foreach (string scenesWaitingForTriggerCount in scenesWaitingForTriggerCounts)
		{
			ProcessTriggerCounts(scenesWaitingForTriggerCount);
		}
		scenesWaitingForTriggerCounts.Clear();
	}

	public static void ProcessSceneLoad(string sceneName)
	{
		if (waitingForTriggerHistory)
		{
			scenesWaitingForTriggerHistory.Add(sceneName);
		}
		else
		{
			ProcessTriggerHistory(sceneName);
		}
		if (waitingForTriggerCounts)
		{
			scenesWaitingForTriggerCounts.Add(sceneName);
		}
		else
		{
			ProcessTriggerCounts(sceneName);
		}
	}

	private static void ProcessTriggerHistory(string forScene)
	{
		if (registeredTriggersPerScene.TryGetValue(forScene, out var value))
		{
			foreach (byte item in triggerHistory)
			{
				if (value.TryGetValue(item, out var value2))
				{
					value2.Trigger(1.0, originatedLocally: false, ignoreTriggerCount: true);
				}
			}
		}
		OnTriggerHistoryProcessedForScene?.Invoke(forScene);
	}

	private static void ProcessTriggerCounts(string forScene)
	{
		if (!registeredTriggersPerScene.TryGetValue(forScene, out var value))
		{
			return;
		}
		List<byte> list = new List<byte>();
		foreach (KeyValuePair<byte, byte> triggerCount in triggerCounts)
		{
			if (value.TryGetValue(triggerCount.Key, out var value2))
			{
				if (value2.numAllowedTriggers > 0)
				{
					value2.SetTriggerCount(triggerCount.Value);
				}
				else
				{
					list.Add(triggerCount.Key);
				}
			}
		}
		foreach (byte item in list)
		{
			triggerCounts.Remove(item);
		}
	}

	public static void RequestTrigger(byte triggerID)
	{
		if (!hasInstance)
		{
			return;
		}
		if (!NetworkSystem.Instance.InRoom || NetworkSystem.Instance.IsMasterClient)
		{
			double triggerTime = Time.time;
			if (NetworkSystem.Instance.InRoom)
			{
				triggerTime = PhotonNetwork.Time;
				instance.SendRPC("ActivateTrigger_RPC", true, triggerID, NetworkSystem.Instance.LocalPlayer.ActorNumber);
			}
			instance.ActivateTrigger(triggerID, triggerTime, originatedLocally: true);
		}
		else
		{
			instance.SendRPC("RequestTrigger_RPC", false, triggerID);
		}
	}

	[PunRPC]
	private void RequestTrigger_RPC(byte triggerID, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "RequestTrigger_RPC");
		if (!NetworkSystem.Instance.InRoom || !NetworkSystem.Instance.IsMasterClient)
		{
			return;
		}
		NetPlayer player = NetworkSystem.Instance.GetPlayer(info.Sender);
		if (!VRRigCache.Instance.TryGetVrrig(player, out var playerRig) || !playerRig.Rig.fxSettings.callSettings[11].CallLimitSettings.CheckCallTime(Time.unscaledTime))
		{
			return;
		}
		if (TryGetRegisteredTrigger(triggerID, out var trigger))
		{
			if (!trigger.CanTrigger())
			{
				return;
			}
			Vector3 position = trigger.gameObject.transform.position;
			if (!VRRigCache.Instance.TryGetVrrig(info.Sender, out var playerRig2) || (playerRig2.Rig.bodyTransform.position - position).sqrMagnitude > trigger.validationDistanceSquared)
			{
				return;
			}
		}
		SendRPC("ActivateTrigger_RPC", true, triggerID, info.Sender.ActorNumber);
		ActivateTrigger(triggerID, info.SentServerTime);
	}

	[PunRPC]
	private void ActivateTrigger_RPC(byte triggerID, int originatingPlayer, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "ActivateTrigger_RPC");
		if (NetworkSystem.Instance.InRoom && info.Sender.IsMasterClient && !(info.SentServerTime < 0.0) && !(info.SentServerTime > 4294967.295))
		{
			double num = (double)PhotonNetwork.GetPing() / 1000.0;
			if (Utils.ValidateServerTime(info.SentServerTime, Math.Max(10.0, num * 2.0)) && ActivateTriggerCallLimiter.CheckCallTime(Time.unscaledTime))
			{
				ActivateTrigger(triggerID, info.SentServerTime, NetworkSystem.Instance.LocalPlayer.ActorNumber == originatingPlayer);
			}
		}
	}

	private void ActivateTrigger(byte triggerID, double triggerTime = -1.0, bool originatedLocally = false)
	{
		CMSTrigger trigger;
		bool flag = TryGetRegisteredTrigger(triggerID, out trigger);
		if (!double.IsFinite(triggerTime))
		{
			triggerTime = -1.0;
		}
		byte value;
		bool num = triggerCounts.TryGetValue(triggerID, out value);
		bool flag2 = !flag || trigger.numAllowedTriggers > 0;
		if (num)
		{
			triggerCounts[triggerID] = ((value == byte.MaxValue) ? byte.MaxValue : (++value));
		}
		else if (flag2)
		{
			triggerCounts.Add(triggerID, 1);
		}
		triggerHistory.Remove(triggerID);
		triggerHistory.Add(triggerID);
		if (flag)
		{
			trigger.Trigger(triggerTime, originatedLocally);
		}
	}
}

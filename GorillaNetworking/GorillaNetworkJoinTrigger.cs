using System;
using System.Collections.Generic;
using GorillaGameModes;
using GorillaTagScripts;
using UnityEngine;
using UnityEngine.Serialization;

namespace GorillaNetworking;

public class GorillaNetworkJoinTrigger : GorillaTriggerBox
{
	public GameObject[] makeSureThisIsDisabled;

	public GameObject[] makeSureThisIsEnabled;

	public GTZone zone;

	public GroupJoinZoneA groupJoinRequiredZones;

	public GroupJoinZoneB groupJoinRequiredZonesB;

	[FormerlySerializedAs("gameModeName")]
	public string networkZone;

	public GorillaFriendCollider myCollider;

	public GorillaNetworkJoinTrigger primaryTriggerForMyZone;

	public bool ignoredIfInParty;

	public bool isSubsOnly;

	private JoinTriggerUI ui;

	private bool didRegisterForCallbacks;

	public AdditionalCustomProperty[] additionalJoinCustomProperties;

	private static bool triggerJoinsDisabled;

	public GroupJoinZoneAB groupJoinRequiredZonesAB => new GroupJoinZoneAB
	{
		a = groupJoinRequiredZones,
		b = groupJoinRequiredZonesB
	};

	private void Start()
	{
		if (primaryTriggerForMyZone == null)
		{
			primaryTriggerForMyZone = this;
		}
		if (primaryTriggerForMyZone == this)
		{
			GorillaComputer.instance.RegisterPrimaryJoinTrigger(this);
		}
		PhotonNetworkController.Instance.RegisterJoinTrigger(this);
		if (!didRegisterForCallbacks && ui != null)
		{
			didRegisterForCallbacks = true;
			FriendshipGroupDetection.Instance.AddGroupZoneCallback(OnGroupPositionsChanged);
		}
	}

	public void RegisterUI(JoinTriggerUI ui)
	{
		this.ui = ui;
		if (!didRegisterForCallbacks && FriendshipGroupDetection.Instance != null)
		{
			didRegisterForCallbacks = true;
			FriendshipGroupDetection.Instance.AddGroupZoneCallback(OnGroupPositionsChanged);
		}
		UpdateUI();
	}

	public void UnregisterUI(JoinTriggerUI ui)
	{
		this.ui = null;
	}

	private void OnDestroy()
	{
		if (didRegisterForCallbacks)
		{
			FriendshipGroupDetection.Instance.RemoveGroupZoneCallback(OnGroupPositionsChanged);
		}
	}

	private void OnGroupPositionsChanged(GroupJoinZoneAB groupZone)
	{
		UpdateUI();
	}

	public void UpdateUI()
	{
		if (!(ui == null) && !(NetworkSystem.Instance == null))
		{
			if (GorillaScoreboardTotalUpdater.instance.offlineTextErrorString != null)
			{
				ui.SetState(JoinTriggerVisualState.ConnectionError, GetActiveNetworkZone, GetDesiredNetworkZone, GetActiveGameType, GetDesiredGameTypeLocalized);
			}
			else if (NetworkSystem.Instance.SessionIsPrivate)
			{
				ui.SetState(JoinTriggerVisualState.InPrivateRoom, GetActiveNetworkZone, GetDesiredNetworkZone, GetActiveGameType, GetDesiredGameTypeLocalized);
			}
			else if (NetworkSystem.Instance.InRoom && NetworkSystem.Instance.GameModeString == GetFullDesiredGameModeString())
			{
				ui.SetState(JoinTriggerVisualState.AlreadyInRoom, GetActiveNetworkZone, GetDesiredNetworkZone, GetActiveGameType, GetDesiredGameTypeLocalized);
			}
			else if (FriendshipGroupDetection.Instance.IsInParty)
			{
				ui.SetState(CanPartyJoin() ? JoinTriggerVisualState.LeaveRoomAndPartyJoin : JoinTriggerVisualState.AbandonPartyAndSoloJoin, GetActiveNetworkZone, GetDesiredNetworkZone, GetActiveGameType, GetDesiredGameTypeLocalized);
			}
			else if (!NetworkSystem.Instance.InRoom)
			{
				ui.SetState(JoinTriggerVisualState.NotConnectedSoloJoin, GetActiveNetworkZone, GetDesiredNetworkZone, GetActiveGameType, GetDesiredGameTypeLocalized);
			}
			else if (PhotonNetworkController.Instance.currentJoinTrigger == primaryTriggerForMyZone)
			{
				ui.SetState(JoinTriggerVisualState.ChangingGameModeSoloJoin, GetActiveNetworkZone, GetDesiredNetworkZone, GetActiveGameType, GetDesiredGameTypeLocalized);
			}
			else
			{
				ui.SetState(JoinTriggerVisualState.LeaveRoomAndSoloJoin, GetActiveNetworkZone, GetDesiredNetworkZone, GetActiveGameType, GetDesiredGameTypeLocalized);
			}
		}
	}

	private string GetActiveNetworkZone()
	{
		return PhotonNetworkController.Instance.currentJoinTrigger.networkZone.ToUpper();
	}

	private string GetDesiredNetworkZone()
	{
		return networkZone.ToUpper();
	}

	public static string GetActiveGameType()
	{
		return GameMode.ActiveGameMode?.GameModeName() ?? "";
	}

	public string GetDesiredGameType()
	{
		GameModeType result;
		return GameMode.GameModeZoneMapping.VerifyModeForZone(zone, Enum.TryParse<GameModeType>(GorillaComputer.instance.currentGameMode.Value, ignoreCase: true, out result) ? result : GameModeType.Casual, NetworkSystem.Instance.SessionIsPrivate).ToString();
	}

	public GameModeType GetDesiredGameModeType()
	{
		GameModeType result;
		return GameMode.GameModeZoneMapping.VerifyModeForZone(zone, Enum.TryParse<GameModeType>(GorillaComputer.instance.currentGameMode.Value, ignoreCase: true, out result) ? result : GameModeType.Casual, NetworkSystem.Instance.SessionIsPrivate);
	}

	public string GetDesiredGameTypeLocalized()
	{
		GameModeType result;
		return GorillaGameManager.GameModeEnumToName(GameMode.GameModeZoneMapping.VerifyModeForZone(zone, Enum.TryParse<GameModeType>(GorillaComputer.instance.currentGameMode.Value, ignoreCase: true, out result) ? result : GameModeType.Casual, NetworkSystem.Instance.SessionIsPrivate));
	}

	public virtual string GetFullDesiredGameModeString()
	{
		return new GameModeString
		{
			zone = networkZone,
			queue = GorillaComputer.instance.currentQueue,
			gameType = GetDesiredGameType()
		}.ToString();
	}

	public virtual bool SameZoneAsOverride()
	{
		return NetworkSystem.Instance.groupJoinOverrideGameMode.StartsWith(networkZone);
	}

	public virtual byte GetRoomSize(bool subscribed)
	{
		return RoomSystem.GetRoomSizeForCreate(zone, GetDesiredGameModeType(), privateRoom: false, subscribed);
	}

	public bool CanPartyJoin()
	{
		return CanPartyJoin(FriendshipGroupDetection.Instance.partyZone);
	}

	public bool CanPartyJoin(GroupJoinZoneAB zone)
	{
		return (groupJoinRequiredZonesAB & zone) == zone;
	}

	public override void OnBoxTriggered()
	{
		base.OnBoxTriggered();
		if (isSubsOnly)
		{
			if (SubscriptionManager.IsLocalSubscribed())
			{
				SubsPublicJoin();
			}
			return;
		}
		if (triggerJoinsDisabled)
		{
			Debug.Log("GorillaNetworkJoinTrigger::OnBoxTriggered - blocking join call");
			return;
		}
		GorillaComputer.instance.allowedMapsToJoin = myCollider.myAllowedMapsToJoin;
		if (NetworkSystem.Instance.groupJoinInProgress)
		{
			return;
		}
		List<(string, string)> list = new List<(string, string)>();
		AdditionalCustomProperty[] array = additionalJoinCustomProperties;
		for (int i = 0; i < array.Length; i++)
		{
			AdditionalCustomProperty additionalCustomProperty = array[i];
			list.Add((additionalCustomProperty.key, additionalCustomProperty.value));
		}
		if (FriendshipGroupDetection.Instance.IsInParty)
		{
			if (ignoredIfInParty || NetworkSystem.Instance.netState == NetSystemState.Connecting || NetworkSystem.Instance.netState == NetSystemState.Disconnecting || NetworkSystem.Instance.netState == NetSystemState.Initialization || NetworkSystem.Instance.netState == NetSystemState.PingRecon)
			{
				return;
			}
			if (NetworkSystem.Instance.InRoom)
			{
				if (NetworkSystem.Instance.GameModeString == GetFullDesiredGameModeString())
				{
					GTDev.Log("JoinTrigger: Ignoring party join/leave because " + networkZone + " is already the game mode");
					return;
				}
				if (NetworkSystem.Instance.SessionIsPrivate)
				{
					GTDev.Log("JoinTrigger: Ignoring party join/leave because we're in a private room");
					return;
				}
				if (SameZoneAsOverride())
				{
					GTDev.Log("JoinTrigger: Ignoring party join/leave because we joined as a group, and this trigger matches the zone for the override, so there's no reason to attempt to leave");
					return;
				}
			}
			if (CanPartyJoin())
			{
				Debug.Log($"JoinTrigger: Attempting party join in 1 second! <{groupJoinRequiredZones}> accepts <{FriendshipGroupDetection.Instance.partyZone}>");
				PhotonNetworkController.Instance.DeferJoining(1f);
				FriendshipGroupDetection.Instance.SendAboutToGroupJoin();
				PhotonNetworkController.Instance.AttemptToJoinPublicRoom(this, JoinType.JoinWithParty, list);
				return;
			}
			Debug.Log($"JoinTrigger: LeaveGroup: Leaving party and will solo join, wanted <{groupJoinRequiredZones}> but got <{FriendshipGroupDetection.Instance.partyZone}>");
			FriendshipGroupDetection.Instance.LeaveParty();
			PhotonNetworkController.Instance.DeferJoining(1f);
		}
		else
		{
			Debug.Log("JoinTrigger: Solo join (not in a group)");
			PhotonNetworkController.Instance.ClearDeferredJoin();
		}
		PhotonNetworkController.Instance.AttemptToJoinPublicRoom(this, JoinType.Solo, list);
	}

	public void SubsPublicJoin()
	{
		if (triggerJoinsDisabled)
		{
			Debug.Log("GorillaNetworkJoinTrigger::SubsPublicJoin - blocking join call");
			return;
		}
		GorillaComputer.instance.allowedMapsToJoin = myCollider.myAllowedMapsToJoin;
		PhotonNetworkController.Instance.ClearDeferredJoin();
		PhotonNetworkController.Instance.AttemptToJoinPublicRoom(this, JoinType.Solo, null, SubscriptionManager.IsLocalSubscribed());
	}

	public static void DisableTriggerJoins()
	{
		Debug.Log("[GorillaNetworkJoinTrigger::DisableTriggerJoins] Disabling Trigger-based Room Joins...");
		triggerJoinsDisabled = true;
	}

	public static void EnableTriggerJoins()
	{
		Debug.Log("[GorillaNetworkJoinTrigger::EnableTriggerJoins] Enabling Trigger-based Room Joins...");
		triggerJoinsDisabled = false;
	}
}

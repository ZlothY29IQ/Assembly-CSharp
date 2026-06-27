using System;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using GorillaNetworking;
using GorillaTag;
using GorillaTagScripts;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public static class RoomControls
{
	private class PunCallbacks : IInRoomCallbacks
	{
		void IInRoomCallbacks.OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
		{
			ReconcileRoomProperties(propertiesThatChanged);
		}

		void IInRoomCallbacks.OnPlayerEnteredRoom(Player newPlayer)
		{
		}

		void IInRoomCallbacks.OnPlayerLeftRoom(Player otherPlayer)
		{
		}

		void IInRoomCallbacks.OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
		{
		}

		void IInRoomCallbacks.OnMasterClientSwitched(Player newMasterClient)
		{
		}
	}

	private const byte BlockPlayerEventCode = 100;

	private const byte UnblockPlayerEventCode = 101;

	private const byte MutePlayerEventCode = 102;

	private const byte UnmutePlayerEventCode = 103;

	private const string RoomControlsEnabledRoomPropertyKey = "roomControlsEnabled";

	private const string BlockedPlayersRoomPropertyKey = "blockedUsers";

	private const string MutedPlayersRoomPropertyKey = "mutedUsers";

	[OnEnterPlay_Set(false)]
	private static bool roomControlsEnabled = false;

	[OnEnterPlay_SetNew]
	public static DelegateListProcessor<bool> OnRoomControlsEnabledChanged = new DelegateListProcessor<bool>();

	[OnEnterPlay_Clear]
	private static readonly Dictionary<string, long> blockedPlayers = new Dictionary<string, long>();

	[OnEnterPlay_SetNew]
	public static DelegateListProcessor<string, bool> OnPlayerBlockChanged = new DelegateListProcessor<string, bool>();

	[OnEnterPlay_Clear]
	private static readonly Dictionary<string, long> mutedPlayers = new Dictionary<string, long>();

	[OnEnterPlay_SetNew]
	public static DelegateListProcessor<string, bool> OnPlayerMuteChanged = new DelegateListProcessor<string, bool>();

	[OnEnterPlay_SetNew]
	public static DelegateListProcessor OnRoomStateLoaded = new DelegateListProcessor();

	private static readonly PunCallbacks punCallbacks = new PunCallbacks();

	private static readonly RaiseEventOptions RaiseToMasterClient = new RaiseEventOptions
	{
		Receivers = ReceiverGroup.MasterClient
	};

	public static IReadOnlyDictionary<string, long> BlockedPlayers => blockedPlayers;

	public static IReadOnlyDictionary<string, long> MutedPlayers => mutedPlayers;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void SubscribeToRoomEvents()
	{
		RoomSystem.JoinedRoomEvent += (Action)delegate
		{
			if (RoomSystem.WasRoomSubscription)
			{
				PhotonNetwork.AddCallbackTarget(punCallbacks);
				ApplyRoomProperties(PhotonNetwork.CurrentRoom.CustomProperties);
				OnRoomStateLoaded.InvokeSafe();
			}
		};
		RoomSystem.LeftRoomEvent += (Action)delegate
		{
			PhotonNetwork.RemoveCallbackTarget(punCallbacks);
			roomControlsEnabled = false;
			blockedPlayers.Clear();
			mutedPlayers.Clear();
		};
	}

	[OnExitPlay_Run]
	private static void RemovePunCallbacks()
	{
		PhotonNetwork.RemoveCallbackTarget(punCallbacks);
	}

	private static void ApplyRoomProperties(ExitGames.Client.Photon.Hashtable properties)
	{
		if (IsRoomControlsTrusted())
		{
			if (properties.TryGetValue("roomControlsEnabled", out var value))
			{
				roomControlsEnabled = value is bool && (bool)value;
			}
			if (properties.TryGetValue("blockedUsers", out var value2))
			{
				ApplyRoomProperty(value2 as ExitGames.Client.Photon.Hashtable, blockedPlayers);
			}
			if (properties.TryGetValue("mutedUsers", out var value3))
			{
				ApplyRoomProperty(value3 as ExitGames.Client.Photon.Hashtable, mutedPlayers);
			}
		}
	}

	private static void ReconcileRoomProperties(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
	{
		if (!IsRoomControlsTrusted())
		{
			return;
		}
		if (propertiesThatChanged.TryGetValue("roomControlsEnabled", out var value))
		{
			bool data = value is bool && (bool)value;
			if (data != roomControlsEnabled)
			{
				roomControlsEnabled = data;
				OnRoomControlsEnabledChanged.InvokeSafe(in data);
			}
		}
		if (propertiesThatChanged.TryGetValue("blockedUsers", out var value2))
		{
			ReconcileRoomProperty(value2 as ExitGames.Client.Photon.Hashtable, blockedPlayers, OnPlayerBlockChanged);
		}
		if (propertiesThatChanged.TryGetValue("mutedUsers", out var value3))
		{
			ReconcileRoomProperty(value3 as ExitGames.Client.Photon.Hashtable, mutedPlayers, OnPlayerMuteChanged);
		}
	}

	private static void ApplyRoomProperty(ExitGames.Client.Photon.Hashtable source, Dictionary<string, long> destination)
	{
		destination.Clear();
		if (source == null)
		{
			return;
		}
		foreach (DictionaryEntry item in source)
		{
			if (item.Key is string key && item.Value is long value)
			{
				destination[key] = value;
			}
		}
	}

	private static void ReconcileRoomProperty(ExitGames.Client.Photon.Hashtable source, Dictionary<string, long> destination, DelegateListProcessor<string, bool> onPropertyChanged)
	{
		HashSet<string> hashSet = new HashSet<string>(destination.Keys);
		ApplyRoomProperty(source, destination);
		foreach (string key in destination.Keys)
		{
			string data = key;
			if (!hashSet.Remove(data))
			{
				onPropertyChanged.InvokeSafe(in data, true);
			}
		}
		foreach (string item in hashSet)
		{
			onPropertyChanged.InvokeSafe(item, false);
		}
	}

	private static bool IsRoomControlsTrusted()
	{
		return GorillaServer.Instance?.CheckRoomControlsEnabledForAnyone() ?? false;
	}

	public static bool CanModerate()
	{
		string cannotReason;
		return CanModerate(out cannotReason);
	}

	public static bool CanModerate(out string cannotReason)
	{
		if (!PhotonNetwork.InRoom)
		{
			cannotReason = "Not in a room";
			return false;
		}
		if (!PhotonNetwork.IsMasterClient)
		{
			cannotReason = "The local player is not the master client";
			return false;
		}
		if (PhotonNetwork.CurrentRoom.IsVisible)
		{
			cannotReason = "The room is not a private room";
			return false;
		}
		if (!RoomSystem.WasRoomSubscription)
		{
			cannotReason = "The room was not a subscription room";
			return false;
		}
		if (!SubscriptionManager.IsLocalSubscribed())
		{
			cannotReason = "The local player is not a subscriber";
			return false;
		}
		if (!roomControlsEnabled)
		{
			cannotReason = "Room controls are disabled";
			return false;
		}
		cannotReason = null;
		return true;
	}

	public static void KickPlayer(int targetActorNumber)
	{
		if (CanModerate())
		{
			PhotonNetwork.RaiseEvent(100, new object[2] { targetActorNumber, 0 }, RaiseToMasterClient, SendOptions.SendReliable);
		}
	}

	public static void KickAndBlockPlayer(int targetActorNumber, int howManySeconds = -1)
	{
		if (CanModerate())
		{
			object[] eventContent = ((howManySeconds >= 0) ? new object[2] { targetActorNumber, howManySeconds } : new object[1] { targetActorNumber });
			PhotonNetwork.RaiseEvent(100, eventContent, RaiseToMasterClient, SendOptions.SendReliable);
		}
	}

	public static void UnblockPlayer(string targetUserId)
	{
		if (CanModerate())
		{
			PhotonNetwork.RaiseEvent(101, new object[1] { targetUserId }, RaiseToMasterClient, SendOptions.SendReliable);
		}
	}

	public static void MutePlayer(int targetActorNumber, int howManySeconds = -1)
	{
		if (CanModerate())
		{
			object[] eventContent = ((howManySeconds >= 0) ? new object[2] { targetActorNumber, howManySeconds } : new object[1] { targetActorNumber });
			PhotonNetwork.RaiseEvent(102, eventContent, RaiseToMasterClient, SendOptions.SendReliable);
		}
	}

	public static void UnmutePlayer(string targetUserId)
	{
		if (CanModerate())
		{
			PhotonNetwork.RaiseEvent(103, new object[1] { targetUserId }, RaiseToMasterClient, SendOptions.SendReliable);
		}
	}
}

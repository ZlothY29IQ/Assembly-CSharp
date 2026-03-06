using System;
using System.Collections;
using GorillaTagScripts.VirtualStumpCustomMaps;
using Modio.Mods;
using Photon.Pun;
using UnityEngine;

internal class VirtualStumpSerializer : GorillaSerializer
{
	[SerializeField]
	private VirtualStumpBarrierSFX barrierSFX;

	[SerializeField]
	private CustomMapsDisplayScreen detailsScreen;

	private static bool waitingForRoomInitialization;

	private static bool roomInitialized;

	private bool sendModList;

	private bool forceNewSearch;

	private bool waitToSendStatus;

	private bool sendNewStatus;

	private const float STATUS_UPDATE_INTERVAL = 0.5f;

	private Coroutine statusUpdateCoroutine;

	internal bool HasAuthority => photonView.IsMine;

	protected void Start()
	{
		NetworkSystem.Instance.OnMultiplayerStarted += new Action(OnJoinedRoom);
		NetworkSystem.Instance.OnReturnedToSinglePlayer += new Action(OnLeftRoom);
		NetworkSystem.Instance.OnPlayerLeft += new Action<NetPlayer>(OnPlayerLeftRoom);
	}

	private void OnPlayerLeftRoom(NetPlayer leavingPlayer)
	{
		if (NetworkSystem.Instance.IsMasterClient)
		{
			int driverID = CustomMapsTerminal.GetDriverID();
			if (leavingPlayer.ActorNumber == driverID)
			{
				CustomMapsTerminal.SetTerminalControlStatus(isLocked: false, -2, sendRPC: true);
			}
		}
	}

	private void OnJoinedRoom()
	{
		if (NetworkSystem.Instance.IsMasterClient)
		{
			roomInitialized = true;
			return;
		}
		Debug.Log("[VirtualStumpSerializer::OnJoinedRoom] Requesting Room Initialization...");
		waitingForRoomInitialization = true;
		SendRPC("RequestRoomInitialization_RPC", false);
	}

	private void OnLeftRoom()
	{
		Debug.Log("[VirtualStumpSerializer::OnLeftRoom]...");
		roomInitialized = false;
	}

	public static bool IsWaitingForRoomInit()
	{
		if (!waitingForRoomInitialization)
		{
			return !roomInitialized;
		}
		return true;
	}

	[PunRPC]
	private void RequestRoomInitialization_RPC(PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "RequestRoomInitialization_RPC");
		if (NetworkSystem.Instance.IsMasterClient)
		{
			NetPlayer player = NetworkSystem.Instance.GetPlayer(info.Sender);
			if (!player.CheckSingleCallRPC(NetPlayer.SingleCallRPC.CMS_RequestRoomInitialization))
			{
				player.ReceivedSingleCallRPC(NetPlayer.SingleCallRPC.CMS_RequestRoomInitialization);
				long id = CustomMapManager.GetRoomMapId()._id;
				SendRPC("InitializeRoom_RPC", info.Sender, CustomMapsTerminal.CurrentScreen, CustomMapsTerminal.GetDriverID(), CustomMapsTerminal.LocalModDetailsID, id);
			}
		}
	}

	[PunRPC]
	private void InitializeRoom_RPC(int currentScreen, int driverID, long modDetailsID, long loadedMapModID, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "InitializeRoom_RPC");
		if (info.Sender.IsMasterClient && waitingForRoomInitialization && (driverID == -2 || NetworkSystem.Instance.GetPlayer(driverID) != null))
		{
			CustomMapsTerminal.UpdateFromDriver(currentScreen, modDetailsID, driverID);
			if (loadedMapModID > 0)
			{
				CustomMapManager.SetRoomMap(loadedMapModID);
			}
			roomInitialized = true;
			waitingForRoomInitialization = false;
			Debug.Log("[VStumpSerializer.InitializeRPC] Room initialization finished.");
		}
	}

	public void LoadMapSynced(long modId)
	{
		CustomMapManager.SetRoomMap(modId);
		CustomMapManager.LoadMap(new ModId(modId));
		if (NetworkSystem.Instance.InRoom && NetworkSystem.Instance.SessionIsPrivate)
		{
			SendRPC("SetRoomMap_RPC", true, modId);
		}
	}

	public void UnloadMapSynced()
	{
		CustomMapManager.UnloadMap();
		if (NetworkSystem.Instance.InRoom && NetworkSystem.Instance.SessionIsPrivate)
		{
			SendRPC("UnloadMap_RPC", true);
		}
	}

	[PunRPC]
	private void SetRoomMap_RPC(long modId, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "SetRoomMap_RPC");
		if (modId > 0 && (info.Sender.ActorNumber == photonView.OwnerActorNr || info.Sender.ActorNumber == CustomMapsTerminal.GetDriverID()) && modId == detailsScreen.currentMapMod.Id._id)
		{
			CustomMapManager.SetRoomMap(modId);
		}
	}

	[PunRPC]
	private void UnloadMap_RPC(PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "UnloadMap_RPC");
		if (info.Sender.ActorNumber == CustomMapsTerminal.GetDriverID() && CustomMapManager.AreAllPlayersInVirtualStump())
		{
			CustomMapManager.UnloadMap();
		}
	}

	public void RequestTerminalControlStatusChange(bool lockedStatus)
	{
		if (NetworkSystem.Instance.InRoom)
		{
			SendRPC("RequestTerminalControlStatusChange_RPC", false, lockedStatus);
		}
	}

	[PunRPC]
	private void RequestTerminalControlStatusChange_RPC(bool lockedStatus, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "RequestTerminalControlStatusChange_RPC");
		if (NetworkSystem.Instance.IsMasterClient)
		{
			NetPlayer player = NetworkSystem.Instance.GetPlayer(info.Sender);
			if (VRRigCache.Instance.TryGetVrrig(player, out var playerRig) && playerRig.Rig.fxSettings.callSettings[19].CallLimitSettings.CheckCallTime(Time.unscaledTime) && !player.IsNull && CustomMapManager.IsRemotePlayerInVirtualStump(info.Sender.UserId))
			{
				CustomMapsTerminal.HandleTerminalControlStatusChangeRequest(lockedStatus, info.Sender.ActorNumber);
			}
		}
	}

	public void SetTerminalControlStatus(bool locked, int playerID)
	{
		if (NetworkSystem.Instance.InRoom && NetworkSystem.Instance.IsMasterClient)
		{
			SendRPC("SetTerminalControlStatus_RPC", true, locked, playerID);
		}
	}

	[PunRPC]
	private void SetTerminalControlStatus_RPC(bool locked, int driverID, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "SetTerminalControlStatus_RPC");
		if (info.Sender.IsMasterClient && (driverID == -2 || NetworkSystem.Instance.GetPlayer(driverID) != null))
		{
			NetPlayer player = NetworkSystem.Instance.GetPlayer(info.Sender);
			if (VRRigCache.Instance.TryGetVrrig(player, out var playerRig) && playerRig.Rig.fxSettings.callSettings[16].CallLimitSettings.CheckCallTime(Time.unscaledTime))
			{
				CustomMapsTerminal.SetTerminalControlStatus(locked, driverID);
			}
		}
	}

	public void SendTerminalStatus()
	{
		if (NetworkSystem.Instance.InRoom && CustomMapsTerminal.IsDriver)
		{
			if (statusUpdateCoroutine != null)
			{
				StopCoroutine(statusUpdateCoroutine);
			}
			statusUpdateCoroutine = StartCoroutine(WaitToSendStatus());
		}
	}

	private IEnumerator WaitToSendStatus()
	{
		yield return new WaitForSeconds(0.5f);
		SendRPC("UpdateScreen_RPC", true, CustomMapsTerminal.CurrentScreen, CustomMapsTerminal.LocalModDetailsID, CustomMapsTerminal.GetDriverID());
	}

	[PunRPC]
	private void UpdateScreen_RPC(int currentScreen, long modDetailsID, int driverID, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "UpdateScreen_RPC");
		if (info.Sender.ActorNumber == CustomMapsTerminal.GetDriverID() && CustomMapManager.IsRemotePlayerInVirtualStump(info.Sender.UserId) && currentScreen >= -1 && currentScreen <= 6 && NetworkSystem.Instance.GetPlayer(driverID) != null)
		{
			NetPlayer player = NetworkSystem.Instance.GetPlayer(info.Sender);
			if (VRRigCache.Instance.TryGetVrrig(player, out var playerRig) && playerRig.Rig.fxSettings.callSettings[17].CallLimitSettings.CheckCallTime(Time.unscaledTime))
			{
				CustomMapsTerminal.UpdateFromDriver(currentScreen, modDetailsID, driverID);
			}
		}
	}

	public void RefreshDriverNickName()
	{
		if (NetworkSystem.Instance.InRoom)
		{
			SendRPC("RefreshDriverNickName_RPC", true);
		}
	}

	[PunRPC]
	private void RefreshDriverNickName_RPC(PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "RefreshDriverNickName_RPC");
		if (info.Sender.ActorNumber == CustomMapsTerminal.GetDriverID())
		{
			NetPlayer player = NetworkSystem.Instance.GetPlayer(info.Sender);
			if (VRRigCache.Instance.TryGetVrrig(player, out var playerRig) && playerRig.Rig.fxSettings.callSettings[18].CallLimitSettings.CheckCallTime(Time.unscaledTime))
			{
				CustomMapsTerminal.RefreshDriverNickName();
			}
		}
	}
}

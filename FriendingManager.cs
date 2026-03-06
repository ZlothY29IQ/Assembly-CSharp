using System;
using System.Collections.Generic;
using GorillaLocomotion;
using Photon.Pun;
using UnityEngine;

public class FriendingManager : MonoBehaviourPun, IPunObservable, IGorillaSliceableSimple
{
	public enum FriendStationState
	{
		NotInRoom,
		WaitingForPlayers,
		WaitingOnFriendStatusBoth,
		WaitingOnFriendStatusPlayerA,
		WaitingOnFriendStatusPlayerB,
		WaitingOnButtonBoth,
		WaitingOnButtonPlayerA,
		WaitingOnButtonPlayerB,
		ButtonConfirmationTimer0,
		ButtonConfirmationTimer1,
		ButtonConfirmationTimer2,
		ButtonConfirmationTimer3,
		ButtonConfirmationTimer4,
		WaitingOnRequestBoth,
		WaitingOnRequestPlayerA,
		WaitingOnRequestPlayerB,
		RequestFailed,
		Friends,
		AlreadyFriends
	}

	public struct FriendStationData
	{
		public GTZone zone;

		public int actorNumberA;

		public int actorNumberB;

		public FriendStationState state;

		public float progressBarStartTime;
	}

	[OnEnterPlay_SetNull]
	public static volatile FriendingManager Instance;

	[SerializeField]
	private float progressBarDuration = 3f;

	[SerializeField]
	private float requiredProximityToStation = 3f;

	private List<FriendStationData> activeFriendStationData = new List<FriendStationData>(10);

	private Dictionary<GTZone, FriendingStation> friendingStations = new Dictionary<GTZone, FriendingStation>();

	private GTZone localPlayerZone = GTZone.none;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			PhotonNetwork.AddCallbackTarget(this);
		}
		else
		{
			UnityEngine.Object.Destroy(this);
		}
	}

	private void Start()
	{
		NetworkSystem.Instance.OnPlayerLeft += new Action<NetPlayer>(OnPlayerLeftRoom);
		NetworkSystem.Instance.OnMultiplayerStarted += new Action(ValidateState);
		NetworkSystem.Instance.OnReturnedToSinglePlayer += new Action(ValidateState);
	}

	private void OnDestroy()
	{
		if (NetworkSystem.Instance != null)
		{
			NetworkSystem.Instance.OnPlayerLeft -= new Action<NetPlayer>(OnPlayerLeftRoom);
			NetworkSystem.Instance.OnMultiplayerStarted -= new Action(ValidateState);
			NetworkSystem.Instance.OnReturnedToSinglePlayer -= new Action(ValidateState);
		}
	}

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void SliceUpdate()
	{
		AuthorityUpdate();
	}

	private void AuthorityUpdate()
	{
		if (!PhotonNetwork.InRoom || !base.photonView.IsMine)
		{
			return;
		}
		for (int i = 0; i < activeFriendStationData.Count; i++)
		{
			if (activeFriendStationData[i].state >= FriendStationState.ButtonConfirmationTimer0 && activeFriendStationData[i].state <= FriendStationState.ButtonConfirmationTimer4)
			{
				FriendStationData value = activeFriendStationData[i];
				int num = 4;
				float num2 = (Time.time - value.progressBarStartTime) / progressBarDuration;
				if (num2 < 1f)
				{
					int num3 = Mathf.RoundToInt(num2 * (float)num);
					value.state = (FriendStationState)(num3 + 8);
				}
				else
				{
					base.photonView.RPC("NotifyClientsFriendRequestReadyRPC", RpcTarget.All, value.zone);
					value.state = FriendStationState.WaitingOnRequestBoth;
				}
				activeFriendStationData[i] = value;
			}
		}
	}

	private void OnPlayerLeftRoom(NetPlayer player)
	{
		ValidateState();
	}

	private void ValidateState()
	{
		for (int i = 0; i < activeFriendStationData.Count; i++)
		{
			FriendStationData value = activeFriendStationData[i];
			if (value.actorNumberA != -1 && NetworkSystem.Instance.GetNetPlayerByID(value.actorNumberA) == null)
			{
				value.actorNumberA = -1;
			}
			if (value.actorNumberB != -1 && NetworkSystem.Instance.GetNetPlayerByID(value.actorNumberB) == null)
			{
				value.actorNumberB = -1;
			}
			if (value.actorNumberA == -1 || value.actorNumberB == -1)
			{
				value.state = FriendStationState.WaitingForPlayers;
			}
			activeFriendStationData[i] = value;
		}
		UpdateFriendingStations();
	}

	private void UpdateFriendingStations()
	{
		for (int i = 0; i < activeFriendStationData.Count; i++)
		{
			if (friendingStations.TryGetValue(activeFriendStationData[i].zone, out var value))
			{
				value.UpdateState(activeFriendStationData[i]);
			}
		}
	}

	public void RegisterFriendingStation(FriendingStation friendingStation)
	{
		if (!friendingStations.ContainsKey(friendingStation.Zone))
		{
			friendingStations.Add(friendingStation.Zone, friendingStation);
		}
	}

	public void UnregisterFriendingStation(FriendingStation friendingStation)
	{
		friendingStations.Remove(friendingStation.Zone);
	}

	private void DebugLogFriendingStations()
	{
		string text = $"Friending Stations: Count: {friendingStations.Count} ";
		foreach (KeyValuePair<GTZone, FriendingStation> friendingStation in friendingStations)
		{
			text += $"Station Zone {friendingStation.Key}";
		}
		Debug.Log(text);
	}

	public void PlayerEnteredStation(GTZone zone, NetPlayer netPlayer)
	{
		if (netPlayer != null && netPlayer.ActorNumber == NetworkSystem.Instance.LocalPlayer.ActorNumber)
		{
			localPlayerZone = zone;
		}
		if (!PhotonNetwork.InRoom || !base.photonView.IsMine)
		{
			return;
		}
		int num = -1;
		for (int i = 0; i < activeFriendStationData.Count; i++)
		{
			if (activeFriendStationData[i].zone != zone)
			{
				continue;
			}
			num = i;
			if (activeFriendStationData[i].actorNumberA == -1 && activeFriendStationData[i].actorNumberB != netPlayer.ActorNumber)
			{
				FriendStationData value = activeFriendStationData[i];
				value.actorNumberA = netPlayer.ActorNumber;
				if (value.actorNumberA != -1 && value.actorNumberB != -1)
				{
					value.state = FriendStationState.WaitingOnFriendStatusBoth;
				}
				else
				{
					value.state = FriendStationState.WaitingForPlayers;
				}
				activeFriendStationData[i] = value;
			}
			else if (activeFriendStationData[i].actorNumberA != -1 && activeFriendStationData[i].actorNumberA != netPlayer.ActorNumber && activeFriendStationData[i].actorNumberB == -1)
			{
				FriendStationData value2 = activeFriendStationData[i];
				value2.actorNumberB = netPlayer.ActorNumber;
				if (value2.actorNumberA != -1 && value2.actorNumberB != -1)
				{
					value2.state = FriendStationState.WaitingOnFriendStatusBoth;
				}
				else
				{
					value2.state = FriendStationState.WaitingForPlayers;
				}
				activeFriendStationData[i] = value2;
			}
			if (activeFriendStationData[i].state == FriendStationState.WaitingOnFriendStatusBoth)
			{
				base.photonView.RPC("CheckFriendStatusRequestRPC", RpcTarget.All, activeFriendStationData[i].zone, activeFriendStationData[i].actorNumberA, activeFriendStationData[i].actorNumberB);
			}
			break;
		}
		if (num < 0)
		{
			activeFriendStationData.Add(new FriendStationData
			{
				zone = zone,
				actorNumberA = netPlayer.ActorNumber,
				actorNumberB = -1,
				state = FriendStationState.WaitingForPlayers
			});
		}
		UpdateFriendingStations();
	}

	public void PlayerExitedStation(GTZone zone, NetPlayer netPlayer)
	{
		if (netPlayer != null && netPlayer.ActorNumber == NetworkSystem.Instance.LocalPlayer.ActorNumber)
		{
			localPlayerZone = GTZone.none;
		}
		if (!PhotonNetwork.InRoom || !base.photonView.IsMine)
		{
			return;
		}
		int num = -1;
		for (int i = 0; i < activeFriendStationData.Count; i++)
		{
			if (activeFriendStationData[i].zone == zone)
			{
				if ((activeFriendStationData[i].actorNumberA == netPlayer.ActorNumber && activeFriendStationData[i].actorNumberB == -1) || (activeFriendStationData[i].actorNumberA == -1 && activeFriendStationData[i].actorNumberB == netPlayer.ActorNumber))
				{
					FriendStationData value = activeFriendStationData[i];
					value.actorNumberA = -1;
					value.actorNumberB = -1;
					value.state = FriendStationState.WaitingForPlayers;
					activeFriendStationData[i] = value;
					num = i;
				}
				else if (activeFriendStationData[i].actorNumberA != -1 && activeFriendStationData[i].actorNumberA != netPlayer.ActorNumber && activeFriendStationData[i].actorNumberB == netPlayer.ActorNumber)
				{
					FriendStationData value2 = activeFriendStationData[i];
					value2.actorNumberB = -1;
					value2.state = FriendStationState.WaitingForPlayers;
					activeFriendStationData[i] = value2;
				}
				else if (activeFriendStationData[i].actorNumberB != -1 && activeFriendStationData[i].actorNumberB != netPlayer.ActorNumber && activeFriendStationData[i].actorNumberA == netPlayer.ActorNumber)
				{
					FriendStationData value3 = activeFriendStationData[i];
					value3.actorNumberA = -1;
					value3.state = FriendStationState.WaitingForPlayers;
					activeFriendStationData[i] = value3;
				}
				break;
			}
		}
		UpdateFriendingStations();
		if (num >= 0)
		{
			base.photonView.RPC("StationNoLongerActiveRPC", RpcTarget.Others, activeFriendStationData[num].zone);
			activeFriendStationData.RemoveAt(num);
		}
	}

	private void PlayerPressedButton(GTZone zone, int playerId)
	{
		if (!PhotonNetwork.InRoom || !base.photonView.IsMine)
		{
			return;
		}
		for (int i = 0; i < activeFriendStationData.Count; i++)
		{
			if (activeFriendStationData[i].zone != zone)
			{
				continue;
			}
			if (activeFriendStationData[i].actorNumberA != -1 && activeFriendStationData[i].actorNumberB != -1)
			{
				if ((activeFriendStationData[i].state == FriendStationState.WaitingOnButtonPlayerA && activeFriendStationData[i].actorNumberA == playerId) || (activeFriendStationData[i].state == FriendStationState.WaitingOnButtonPlayerB && activeFriendStationData[i].actorNumberB == playerId))
				{
					FriendStationData value = activeFriendStationData[i];
					value.state = FriendStationState.ButtonConfirmationTimer0;
					value.progressBarStartTime = Time.time;
					activeFriendStationData[i] = value;
				}
				else if (activeFriendStationData[i].state == FriendStationState.WaitingOnButtonBoth && activeFriendStationData[i].actorNumberA == playerId)
				{
					FriendStationData value2 = activeFriendStationData[i];
					value2.state = FriendStationState.WaitingOnButtonPlayerB;
					activeFriendStationData[i] = value2;
				}
				else if (activeFriendStationData[i].state == FriendStationState.WaitingOnButtonBoth && activeFriendStationData[i].actorNumberB == playerId)
				{
					FriendStationData value3 = activeFriendStationData[i];
					value3.state = FriendStationState.WaitingOnButtonPlayerA;
					activeFriendStationData[i] = value3;
				}
			}
			break;
		}
	}

	private void PlayerUnpressedButton(GTZone zone, int playerId)
	{
		if (!PhotonNetwork.InRoom || !base.photonView.IsMine)
		{
			return;
		}
		for (int i = 0; i < activeFriendStationData.Count; i++)
		{
			if (activeFriendStationData[i].zone != zone)
			{
				continue;
			}
			if (activeFriendStationData[i].actorNumberA != -1 && activeFriendStationData[i].actorNumberB != -1)
			{
				bool flag = activeFriendStationData[i].state >= FriendStationState.ButtonConfirmationTimer0 && activeFriendStationData[i].state <= FriendStationState.ButtonConfirmationTimer4;
				if (flag && activeFriendStationData[i].actorNumberA == playerId)
				{
					FriendStationData value = activeFriendStationData[i];
					value.state = FriendStationState.WaitingOnButtonPlayerA;
					activeFriendStationData[i] = value;
				}
				else if (flag && activeFriendStationData[i].actorNumberB == playerId)
				{
					FriendStationData value2 = activeFriendStationData[i];
					value2.state = FriendStationState.WaitingOnButtonPlayerB;
					activeFriendStationData[i] = value2;
				}
				else if ((activeFriendStationData[i].state == FriendStationState.WaitingOnButtonPlayerA && activeFriendStationData[i].actorNumberB == playerId) || (activeFriendStationData[i].state == FriendStationState.WaitingOnButtonPlayerB && activeFriendStationData[i].actorNumberA == playerId))
				{
					FriendStationData value3 = activeFriendStationData[i];
					value3.state = FriendStationState.WaitingOnButtonBoth;
					activeFriendStationData[i] = value3;
				}
			}
			break;
		}
	}

	private void CheckFriendStatusRequest(GTZone zone, int actorNumberA, int actorNumberB)
	{
		FriendSystem.Instance.OnFriendListRefresh -= CheckFriendStatusOnFriendListRefresh;
		FriendSystem.Instance.OnFriendListRefresh += CheckFriendStatusOnFriendListRefresh;
		FriendSystem.Instance.RefreshFriendsList();
	}

	private void CheckFriendStatusOnFriendListRefresh(List<FriendBackendController.Friend> friendList)
	{
		FriendSystem.Instance.OnFriendListRefresh -= CheckFriendStatusOnFriendListRefresh;
		for (int i = 0; i < activeFriendStationData.Count; i++)
		{
			if (activeFriendStationData[i].zone == localPlayerZone)
			{
				int actorNumber = NetworkSystem.Instance.LocalPlayer.ActorNumber;
				int num = -1;
				if (activeFriendStationData[i].actorNumberA == actorNumber)
				{
					num = activeFriendStationData[i].actorNumberB;
				}
				else if (activeFriendStationData[i].actorNumberB == actorNumber)
				{
					num = activeFriendStationData[i].actorNumberA;
				}
				if (num != -1 && FriendSystem.Instance.CheckFriendshipWithPlayer(num))
				{
					base.photonView.RPC("CheckFriendStatusResponseRPC", RpcTarget.MasterClient, localPlayerZone, num, true);
				}
				else
				{
					base.photonView.RPC("CheckFriendStatusResponseRPC", RpcTarget.MasterClient, localPlayerZone, num, false);
				}
				break;
			}
		}
	}

	private void CheckFriendStatusResponse(GTZone zone, int responderActorNumber, int friendTargetActorNumber, bool friends)
	{
		if (!PhotonNetwork.InRoom || !base.photonView.IsMine)
		{
			return;
		}
		for (int i = 0; i < activeFriendStationData.Count; i++)
		{
			if (activeFriendStationData[i].zone != zone)
			{
				continue;
			}
			if (activeFriendStationData[i].actorNumberA == -1 || activeFriendStationData[i].actorNumberB == -1)
			{
				break;
			}
			if ((activeFriendStationData[i].state == FriendStationState.WaitingOnFriendStatusPlayerA && activeFriendStationData[i].actorNumberA == responderActorNumber) || (activeFriendStationData[i].state == FriendStationState.WaitingOnFriendStatusPlayerB && activeFriendStationData[i].actorNumberB == responderActorNumber))
			{
				FriendStationData value = activeFriendStationData[i];
				if (friends)
				{
					value.state = FriendStationState.AlreadyFriends;
				}
				else
				{
					value.state = FriendStationState.WaitingOnButtonBoth;
				}
				activeFriendStationData[i] = value;
			}
			else if (activeFriendStationData[i].state == FriendStationState.WaitingOnFriendStatusBoth && activeFriendStationData[i].actorNumberA == responderActorNumber)
			{
				FriendStationData value2 = activeFriendStationData[i];
				if (friends)
				{
					value2.state = FriendStationState.WaitingOnFriendStatusPlayerB;
				}
				else
				{
					value2.state = FriendStationState.WaitingOnButtonBoth;
				}
				activeFriendStationData[i] = value2;
			}
			else if (activeFriendStationData[i].state == FriendStationState.WaitingOnFriendStatusBoth && activeFriendStationData[i].actorNumberB == responderActorNumber)
			{
				FriendStationData value3 = activeFriendStationData[i];
				if (friends)
				{
					value3.state = FriendStationState.WaitingOnFriendStatusPlayerA;
				}
				else
				{
					value3.state = FriendStationState.WaitingOnButtonBoth;
				}
				activeFriendStationData[i] = value3;
			}
			break;
		}
	}

	private void SendFriendRequestIfApplicable(GTZone zone)
	{
		for (int i = 0; i < activeFriendStationData.Count; i++)
		{
			if (activeFriendStationData[i].zone == zone)
			{
				FriendStationData friendStationData = activeFriendStationData[i];
				int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
				NetPlayer netPlayer = null;
				if (friendStationData.actorNumberA == actorNumber)
				{
					netPlayer = NetworkSystem.Instance.GetNetPlayerByID(friendStationData.actorNumberB);
				}
				else if (friendStationData.actorNumberB == actorNumber)
				{
					netPlayer = NetworkSystem.Instance.GetNetPlayerByID(friendStationData.actorNumberA);
				}
				if (netPlayer != null && friendingStations.TryGetValue(friendStationData.zone, out var value) && (GTPlayer.Instance.HeadCenterPosition - value.transform.position).sqrMagnitude < requiredProximityToStation * requiredProximityToStation)
				{
					FriendSystem.Instance.SendFriendRequest(netPlayer, friendStationData.zone, FriendRequestCallback);
				}
				break;
			}
		}
	}

	private void FriendRequestCompletedAuthority(GTZone zone, int playerId, bool succeeded)
	{
		if (!PhotonNetwork.InRoom || !base.photonView.IsMine)
		{
			return;
		}
		for (int i = 0; i < activeFriendStationData.Count; i++)
		{
			if (activeFriendStationData[i].zone != zone)
			{
				continue;
			}
			if (succeeded)
			{
				if ((activeFriendStationData[i].state == FriendStationState.WaitingOnRequestPlayerA && activeFriendStationData[i].actorNumberA == playerId) || (activeFriendStationData[i].state == FriendStationState.WaitingOnRequestPlayerB && activeFriendStationData[i].actorNumberB == playerId))
				{
					FriendStationData value = activeFriendStationData[i];
					value.state = FriendStationState.Friends;
					activeFriendStationData[i] = value;
				}
				else if (activeFriendStationData[i].state == FriendStationState.WaitingOnRequestBoth && activeFriendStationData[i].actorNumberA == playerId)
				{
					FriendStationData value2 = activeFriendStationData[i];
					value2.state = FriendStationState.WaitingOnRequestPlayerB;
					activeFriendStationData[i] = value2;
				}
				else if (activeFriendStationData[i].state == FriendStationState.WaitingOnRequestBoth && activeFriendStationData[i].actorNumberB == playerId)
				{
					FriendStationData value3 = activeFriendStationData[i];
					value3.state = FriendStationState.WaitingOnRequestPlayerA;
					activeFriendStationData[i] = value3;
				}
			}
			else
			{
				FriendStationData value4 = activeFriendStationData[i];
				value4.state = FriendStationState.RequestFailed;
				activeFriendStationData[i] = value4;
			}
			break;
		}
	}

	private void FriendRequestCallback(GTZone zone, int localId, int friendId, bool success)
	{
		if (base.photonView.IsMine)
		{
			FriendRequestCompletedAuthority(zone, PhotonNetwork.LocalPlayer.ActorNumber, success);
			return;
		}
		base.photonView.RPC("FriendRequestCompletedRPC", RpcTarget.MasterClient, zone, success);
	}

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			stream.SendNext(activeFriendStationData.Count);
			for (int i = 0; i < activeFriendStationData.Count; i++)
			{
				SendFriendStationData(stream, activeFriendStationData[i]);
			}
		}
		else if (stream.IsReading && info.Sender.IsMasterClient)
		{
			int num = (int)stream.ReceiveNext();
			if (num >= 0 && num <= 10)
			{
				activeFriendStationData.Clear();
				for (int j = 0; j < num; j++)
				{
					activeFriendStationData.Add(ReceiveFriendStationData(stream));
				}
			}
		}
		UpdateFriendingStations();
		static FriendStationData ReceiveFriendStationData(PhotonStream photonStream)
		{
			return new FriendStationData
			{
				zone = (GTZone)(int)photonStream.ReceiveNext(),
				actorNumberA = (int)photonStream.ReceiveNext(),
				actorNumberB = (int)photonStream.ReceiveNext(),
				state = (FriendStationState)(int)photonStream.ReceiveNext()
			};
		}
		static void SendFriendStationData(PhotonStream photonStream, FriendStationData data)
		{
			photonStream.SendNext((int)data.zone);
			photonStream.SendNext(data.actorNumberA);
			photonStream.SendNext(data.actorNumberB);
			photonStream.SendNext((int)data.state);
		}
	}

	[PunRPC]
	public void CheckFriendStatusRequestRPC(GTZone zone, int actorNumberA, int actorNumberB, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "CheckFriendStatusRequestRPC");
		if (VRRigCache.Instance.TryGetVrrig(NetworkSystem.Instance.GetPlayer(info.Sender), out var playerRig) && playerRig.Rig.fxSettings.callSettings[12].CallLimitSettings.CheckCallTime(Time.unscaledTime))
		{
			CheckFriendStatusRequest(zone, actorNumberA, actorNumberB);
		}
	}

	[PunRPC]
	public void CheckFriendStatusResponseRPC(GTZone zone, int friendTargetActorNumber, bool friends, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "CheckFriendStatusRequestRPC");
		if (VRRigCache.Instance.TryGetVrrig(NetworkSystem.Instance.GetPlayer(info.Sender), out var playerRig) && playerRig.Rig.fxSettings.callSettings[12].CallLimitSettings.CheckCallTime(Time.unscaledTime))
		{
			CheckFriendStatusResponse(zone, info.Sender.ActorNumber, friendTargetActorNumber, friends);
		}
	}

	[PunRPC]
	public void FriendButtonPressedRPC(GTZone zone, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "FriendButtonPressedRPC");
		if (VRRigCache.Instance.TryGetVrrig(NetworkSystem.Instance.GetPlayer(info.Sender), out var playerRig) && playerRig.Rig.fxSettings.callSettings[12].CallLimitSettings.CheckCallTime(Time.unscaledTime))
		{
			PlayerPressedButton(zone, info.Sender.ActorNumber);
		}
	}

	[PunRPC]
	public void FriendButtonUnpressedRPC(GTZone zone, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "FriendButtonUnpressedRPC");
		if (VRRigCache.Instance.TryGetVrrig(NetworkSystem.Instance.GetPlayer(info.Sender), out var playerRig) && playerRig.Rig.fxSettings.callSettings[12].CallLimitSettings.CheckCallTime(Time.unscaledTime))
		{
			PlayerUnpressedButton(zone, info.Sender.ActorNumber);
		}
	}

	[PunRPC]
	public void StationNoLongerActiveRPC(GTZone zone, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "StationNoLongerActiveRPC");
		if (VRRigCache.Instance.TryGetVrrig(NetworkSystem.Instance.GetPlayer(info.Sender), out var playerRig) && playerRig.Rig.fxSettings.callSettings[12].CallLimitSettings.CheckCallTime(Time.unscaledTime) && info.Sender.IsMasterClient && friendingStations.TryGetValue(zone, out var value))
		{
			value.UpdateState(new FriendStationData
			{
				zone = zone,
				actorNumberA = -1,
				actorNumberB = -1,
				state = FriendStationState.WaitingForPlayers
			});
		}
	}

	[PunRPC]
	public void NotifyClientsFriendRequestReadyRPC(GTZone zone, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "NotifyClientsFriendRequestReadyRPC");
		if (VRRigCache.Instance.TryGetVrrig(NetworkSystem.Instance.GetPlayer(info.Sender), out var playerRig) && playerRig.Rig.fxSettings.callSettings[12].CallLimitSettings.CheckCallTime(Time.unscaledTime))
		{
			SendFriendRequestIfApplicable(zone);
		}
	}

	[PunRPC]
	public void FriendRequestCompletedRPC(GTZone zone, bool succeeded, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "FriendRequestCompletedRPC");
		if (VRRigCache.Instance.TryGetVrrig(NetworkSystem.Instance.GetPlayer(info.Sender), out var playerRig) && playerRig.Rig.fxSettings.callSettings[12].CallLimitSettings.CheckCallTime(Time.unscaledTime))
		{
			FriendRequestCompletedAuthority(zone, info.Sender.ActorNumber, succeeded);
		}
	}
}

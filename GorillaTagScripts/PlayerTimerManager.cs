using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GorillaNetworking;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTagScripts;

public class PlayerTimerManager : MonoBehaviourPunCallbacks
{
	private enum RPC
	{
		InitTimersMaster,
		ToggleTimerMaster,
		Count
	}

	public struct PlayerTimerData
	{
		public int startTimeStamp;

		public int endTimeStamp;

		public bool isStarted;

		public uint lastTimerDuration;
	}

	public static PlayerTimerManager instance;

	public PhotonView timerPV;

	public UnityEvent OnLocalTimerStarted;

	public UnityEvent<int> OnTimerStartedForPlayer;

	public UnityEvent<int, int> OnTimerStopped;

	public const float MAX_DURATION_SECONDS = 3599.99f;

	private float requestSendTime;

	private bool localPlayerRequestedStart;

	private CallLimiter[] callLimiters;

	private Dictionary<int, CallLimiter> timerToggleLimiters;

	private List<CallLimiter> limiterPool;

	private bool areTimersInitialized;

	private Dictionary<int, PlayerTimerData> playerTimerData;

	private const int MAX_TIMER_INIT_BYTES = 256;

	private byte[] serializedTimerData;

	private static List<PlayerTimerBoard> timerBoards = new List<PlayerTimerBoard>(10);

	private bool joinedRoom;

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
		else if (instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		callLimiters = new CallLimiter[2];
		callLimiters[0] = new CallLimiter(10, 1f);
		callLimiters[1] = new CallLimiter(30, 1f);
		playerTimerData = new Dictionary<int, PlayerTimerData>(10);
		timerToggleLimiters = new Dictionary<int, CallLimiter>(10);
		limiterPool = new List<CallLimiter>(10);
		serializedTimerData = new byte[256];
	}

	private CallLimiter CreateLimiterFromPool()
	{
		if (limiterPool.Count > 0)
		{
			CallLimiter result = limiterPool[limiterPool.Count - 1];
			limiterPool.RemoveAt(limiterPool.Count - 1);
			return result;
		}
		return new CallLimiter(5, 1f);
	}

	private void ReturnCallLimiterToPool(CallLimiter limiter)
	{
		if (limiter != null)
		{
			limiter.Reset();
			limiterPool.Add(limiter);
		}
	}

	public void RegisterTimerBoard(PlayerTimerBoard board)
	{
		if (!timerBoards.Contains(board))
		{
			timerBoards.Add(board);
			UpdateTimerBoard(board);
		}
	}

	public void UnregisterTimerBoard(PlayerTimerBoard board)
	{
		timerBoards.Remove(board);
	}

	public bool IsLocalTimerStarted()
	{
		if (playerTimerData.TryGetValue(NetworkSystem.Instance.LocalPlayer.ActorNumber, out var value))
		{
			return value.isStarted;
		}
		return false;
	}

	public float GetTimeForPlayer(int actorNumber)
	{
		if (playerTimerData.TryGetValue(actorNumber, out var value))
		{
			if (value.isStarted)
			{
				return Mathf.Clamp((float)(uint)(PhotonNetwork.ServerTimestamp - value.startTimeStamp) / 1000f, 0f, 3599.99f);
			}
			return Mathf.Clamp((float)value.lastTimerDuration / 1000f, 0f, 3599.99f);
		}
		return 0f;
	}

	public float GetLastDurationForPlayer(int actorNumber)
	{
		if (playerTimerData.TryGetValue(actorNumber, out var value))
		{
			return Mathf.Clamp((float)value.lastTimerDuration / 1000f, 0f, 3599.99f);
		}
		return -1f;
	}

	[PunRPC]
	private void InitTimersMasterRPC(int numBytes, byte[] bytes, PhotonMessageInfo info)
	{
		if (info.Sender.IsMasterClient)
		{
			MonkeAgent.IncrementRPCCall(info, "InitTimersMasterRPC");
			if (ValidateCallLimits(RPC.InitTimersMaster, info) && !areTimersInitialized)
			{
				DeserializeTimerState(bytes.Length, bytes);
				areTimersInitialized = true;
				UpdateAllTimerBoards();
			}
		}
	}

	private int SerializeTimerState()
	{
		Array.Clear(serializedTimerData, 0, serializedTimerData.Length);
		MemoryStream memoryStream = new MemoryStream(serializedTimerData);
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		if (playerTimerData.Count > 10)
		{
			ClearOldPlayerData();
		}
		binaryWriter.Write(playerTimerData.Count);
		foreach (KeyValuePair<int, PlayerTimerData> playerTimerDatum in playerTimerData)
		{
			binaryWriter.Write(playerTimerDatum.Key);
			binaryWriter.Write(playerTimerDatum.Value.startTimeStamp);
			binaryWriter.Write(playerTimerDatum.Value.endTimeStamp);
			binaryWriter.Write((byte)(playerTimerDatum.Value.isStarted ? 1u : 0u));
			binaryWriter.Write(playerTimerDatum.Value.lastTimerDuration);
		}
		return (int)memoryStream.Position;
	}

	private void DeserializeTimerState(int numBytes, byte[] bytes)
	{
		if (numBytes <= 0 || numBytes > 256 || bytes == null || bytes.Length < numBytes)
		{
			return;
		}
		MemoryStream memoryStream = new MemoryStream(bytes);
		BinaryReader binaryReader = new BinaryReader(memoryStream);
		playerTimerData.Clear();
		try
		{
			List<Player> list = PhotonNetwork.PlayerList.ToList();
			if (bytes.Length < 4)
			{
				playerTimerData.Clear();
				return;
			}
			int num = binaryReader.ReadInt32();
			if (num < 0 || num > 10)
			{
				playerTimerData.Clear();
				return;
			}
			int num2 = 17;
			if (memoryStream.Position + num2 * num > bytes.Length)
			{
				playerTimerData.Clear();
				return;
			}
			for (int i = 0; i < num; i++)
			{
				int actorNum = binaryReader.ReadInt32();
				int startTimeStamp = binaryReader.ReadInt32();
				int endTimeStamp = binaryReader.ReadInt32();
				bool isStarted = binaryReader.ReadByte() != 0;
				uint lastTimerDuration = binaryReader.ReadUInt32();
				if (list.FindIndex((Player x) => x.ActorNumber == actorNum) >= 0)
				{
					PlayerTimerData value = new PlayerTimerData
					{
						startTimeStamp = startTimeStamp,
						endTimeStamp = endTimeStamp,
						isStarted = isStarted,
						lastTimerDuration = lastTimerDuration
					};
					playerTimerData.TryAdd(actorNum, value);
				}
			}
		}
		catch (Exception value2)
		{
			Console.WriteLine(value2);
			playerTimerData.Clear();
		}
		if (Time.time - requestSendTime < 5f && IsLocalTimerStarted() != localPlayerRequestedStart)
		{
			timerPV.RPC("RequestTimerToggleRPC", RpcTarget.MasterClient, localPlayerRequestedStart);
		}
	}

	private void ClearOldPlayerData()
	{
		List<int> list = new List<int>(playerTimerData.Count);
		List<Player> list2 = PhotonNetwork.PlayerList.ToList();
		foreach (int actorNum in playerTimerData.Keys)
		{
			if (list2.FindIndex((Player x) => x.ActorNumber == actorNum) < 0)
			{
				list.Add(actorNum);
			}
		}
		foreach (int item in list)
		{
			playerTimerData.Remove(item);
		}
	}

	public void RequestTimerToggle(bool startTimer)
	{
		requestSendTime = Time.time;
		localPlayerRequestedStart = startTimer;
		timerPV.RPC("RequestTimerToggleRPC", RpcTarget.MasterClient, startTimer);
	}

	[PunRPC]
	private void RequestTimerToggleRPC(bool startTimer, PhotonMessageInfo info)
	{
		if (!PhotonNetwork.IsMasterClient)
		{
			return;
		}
		MonkeAgent.IncrementRPCCall(info, "RequestTimerToggleRPC");
		if (timerToggleLimiters.TryGetValue(info.Sender.ActorNumber, out var value))
		{
			if (!value.CheckCallTime(Time.time))
			{
				return;
			}
		}
		else
		{
			CallLimiter callLimiter = CreateLimiterFromPool();
			timerToggleLimiters.Add(info.Sender.ActorNumber, callLimiter);
			callLimiter.CheckCallTime(Time.time);
		}
		if (info.Sender == null)
		{
			return;
		}
		PlayerTimerData value2;
		bool flag = playerTimerData.TryGetValue(info.Sender.ActorNumber, out value2);
		if ((startTimer || flag) && (!flag || startTimer || value2.isStarted))
		{
			int num = info.SentServerTimestamp;
			if (PhotonNetwork.ServerTimestamp - num > PhotonNetwork.NetworkingClient.LoadBalancingPeer.DisconnectTimeout)
			{
				num = PhotonNetwork.ServerTimestamp - PhotonNetwork.NetworkingClient.LoadBalancingPeer.DisconnectTimeout;
			}
			timerPV.RPC("TimerToggledMasterRPC", RpcTarget.All, startTimer, num, info.Sender);
		}
	}

	[PunRPC]
	private void TimerToggledMasterRPC(bool startTimer, int toggleTimeStamp, Player player, PhotonMessageInfo info)
	{
		if (!info.Sender.IsMasterClient)
		{
			return;
		}
		MonkeAgent.IncrementRPCCall(info, "TimerToggledMasterRPC");
		if (ValidateCallLimits(RPC.ToggleTimerMaster, info) && player != null && areTimersInitialized)
		{
			int num = toggleTimeStamp;
			int num2 = info.SentServerTimestamp;
			if (PhotonNetwork.ServerTimestamp - num2 > PhotonNetwork.NetworkingClient.LoadBalancingPeer.DisconnectTimeout)
			{
				num2 = PhotonNetwork.ServerTimestamp - PhotonNetwork.NetworkingClient.LoadBalancingPeer.DisconnectTimeout;
			}
			if (num2 - num > PhotonNetwork.NetworkingClient.LoadBalancingPeer.DisconnectTimeout)
			{
				num = num2 - PhotonNetwork.NetworkingClient.LoadBalancingPeer.DisconnectTimeout;
			}
			OnToggleTimerForPlayer(startTimer, player, num);
		}
	}

	private void OnToggleTimerForPlayer(bool startTimer, Player player, int toggleTime)
	{
		if (playerTimerData.TryGetValue(player.ActorNumber, out var value))
		{
			if (startTimer && !value.isStarted)
			{
				value.startTimeStamp = toggleTime;
				value.isStarted = true;
				OnTimerStartedForPlayer?.Invoke(player.ActorNumber);
				if (player.IsLocal)
				{
					OnLocalTimerStarted?.Invoke();
				}
			}
			else if (!startTimer && value.isStarted)
			{
				value.endTimeStamp = toggleTime;
				value.isStarted = false;
				value.lastTimerDuration = (uint)(value.endTimeStamp - value.startTimeStamp);
				OnTimerStopped?.Invoke(player.ActorNumber, value.endTimeStamp - value.startTimeStamp);
			}
			playerTimerData[player.ActorNumber] = value;
		}
		else
		{
			PlayerTimerData value2 = new PlayerTimerData
			{
				startTimeStamp = (startTimer ? toggleTime : 0),
				endTimeStamp = ((!startTimer) ? toggleTime : 0),
				isStarted = startTimer,
				lastTimerDuration = 0u
			};
			playerTimerData.TryAdd(player.ActorNumber, value2);
			OnTimerStartedForPlayer?.Invoke(player.ActorNumber);
			if (player.IsLocal)
			{
				OnLocalTimerStarted?.Invoke();
			}
		}
		UpdateAllTimerBoards();
	}

	private bool ValidateCallLimits(RPC rpcCall, PhotonMessageInfo info)
	{
		if (rpcCall < RPC.InitTimersMaster || rpcCall >= RPC.Count)
		{
			return false;
		}
		return callLimiters[(int)rpcCall].CheckCallTime(Time.time);
	}

	public override void OnMasterClientSwitched(Player newMasterClient)
	{
		base.OnMasterClientSwitched(newMasterClient);
		if (newMasterClient.IsLocal)
		{
			int num = SerializeTimerState();
			timerPV.RPC("InitTimersMasterRPC", RpcTarget.Others, num, serializedTimerData);
		}
		else
		{
			playerTimerData.Clear();
			areTimersInitialized = false;
		}
	}

	public override void OnPlayerEnteredRoom(Player newPlayer)
	{
		base.OnPlayerEnteredRoom(newPlayer);
		if (PhotonNetwork.IsMasterClient && !newPlayer.IsLocal)
		{
			int num = SerializeTimerState();
			timerPV.RPC("InitTimersMasterRPC", newPlayer, num, serializedTimerData);
		}
		UpdateAllTimerBoards();
	}

	public override void OnPlayerLeftRoom(Player otherPlayer)
	{
		base.OnPlayerLeftRoom(otherPlayer);
		playerTimerData.Remove(otherPlayer.ActorNumber);
		if (timerToggleLimiters.TryGetValue(otherPlayer.ActorNumber, out var value))
		{
			ReturnCallLimiterToPool(value);
			timerToggleLimiters.Remove(otherPlayer.ActorNumber);
		}
		UpdateAllTimerBoards();
	}

	public override void OnJoinedRoom()
	{
		base.OnJoinedRoom();
		joinedRoom = true;
		if (PhotonNetwork.IsMasterClient)
		{
			playerTimerData.Clear();
			foreach (CallLimiter value in timerToggleLimiters.Values)
			{
				ReturnCallLimiterToPool(value);
			}
			timerToggleLimiters.Clear();
			areTimersInitialized = true;
			UpdateAllTimerBoards();
		}
		else
		{
			requestSendTime = 0f;
			areTimersInitialized = false;
		}
	}

	public override void OnLeftRoom()
	{
		base.OnLeftRoom();
		joinedRoom = false;
		playerTimerData.Clear();
		foreach (CallLimiter value in timerToggleLimiters.Values)
		{
			ReturnCallLimiterToPool(value);
		}
		timerToggleLimiters.Clear();
		areTimersInitialized = false;
		requestSendTime = 0f;
		localPlayerRequestedStart = false;
		UpdateAllTimerBoards();
	}

	private void UpdateAllTimerBoards()
	{
		foreach (PlayerTimerBoard timerBoard in timerBoards)
		{
			UpdateTimerBoard(timerBoard);
		}
	}

	private void UpdateTimerBoard(PlayerTimerBoard board)
	{
		board.SetSleepState(joinedRoom);
		if (GorillaComputer.instance == null)
		{
			return;
		}
		if (!joinedRoom)
		{
			if (board.notInRoomText != null)
			{
				board.notInRoomText.gameObject.SetActive(value: true);
				board.notInRoomText.text = GorillaComputer.instance.offlineTextInitialString;
			}
			for (int i = 0; i < board.lines.Count; i++)
			{
				board.lines[i].ResetData();
			}
			return;
		}
		if (board.notInRoomText != null)
		{
			board.notInRoomText.gameObject.SetActive(value: false);
		}
		for (int j = 0; j < board.lines.Count; j++)
		{
			PlayerTimerBoardLine playerTimerBoardLine = board.lines[j];
			if (j < PhotonNetwork.PlayerList.Length)
			{
				playerTimerBoardLine.gameObject.SetActive(value: true);
				playerTimerBoardLine.SetLineData(NetworkSystem.Instance.GetPlayer(PhotonNetwork.PlayerList[j]));
				playerTimerBoardLine.UpdateLine();
			}
			else
			{
				playerTimerBoardLine.ResetData();
				playerTimerBoardLine.gameObject.SetActive(value: false);
			}
		}
		board.RedrawPlayerLines();
	}
}

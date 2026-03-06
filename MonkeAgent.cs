using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ExitGames.Client.Photon;
using Fusion;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class MonkeAgent : MonoBehaviour, IGorillaSliceableSimple
{
	private class RPCCallTracker
	{
		public int RPCCalls;

		public int RPCCallsMax;
	}

	[OnEnterPlay_SetNull]
	public static volatile MonkeAgent instance;

	private bool _sendReport;

	private string _suspiciousPlayerId = "";

	private string _suspiciousPlayerName = "";

	private string _suspiciousReason = "";

	internal List<string> reportedPlayers = new List<string>();

	public byte roomSize;

	public float lastCheck;

	public float userDecayTime = 15f;

	public NetPlayer currentMasterClient;

	public bool testAssault;

	private const byte ReportAssault = 8;

	private int lowestActorNumber;

	private int calls;

	public int rpcCallLimit = 50;

	public int logErrorMax = 50;

	public int rpcErrorMax = 10;

	private object outObj;

	private NetPlayer tempPlayer;

	private int logErrorCount;

	private int stringIndex;

	private string playerID;

	private string playerNick;

	private int lastServerTimestamp;

	private const string InvalidRPC = "invalid RPC stuff";

	public NetPlayer[] cachedPlayerList;

	private float lastReportChecked;

	private float reportCheckCooldown = 1f;

	private static int[] targetActors = new int[1] { -1 };

	private Dictionary<string, Dictionary<string, RPCCallTracker>> userRPCCalls = new Dictionary<string, Dictionary<string, RPCCallTracker>>();

	private ExitGames.Client.Photon.Hashtable hashTable;

	private NetworkRunner runner => ((NetworkSystemFusion)NetworkSystem.Instance).runner;

	private bool sendReport
	{
		get
		{
			return _sendReport;
		}
		set
		{
			if (!_sendReport)
			{
				_sendReport = true;
			}
		}
	}

	private string suspiciousPlayerId
	{
		get
		{
			return _suspiciousPlayerId;
		}
		set
		{
			if (_suspiciousPlayerId == "")
			{
				_suspiciousPlayerId = value;
			}
		}
	}

	private string suspiciousPlayerName
	{
		get
		{
			return _suspiciousPlayerName;
		}
		set
		{
			if (_suspiciousPlayerName == "")
			{
				_suspiciousPlayerName = value;
			}
		}
	}

	private string suspiciousReason
	{
		get
		{
			return _suspiciousReason;
		}
		set
		{
			if (_suspiciousReason == "")
			{
				_suspiciousReason = value;
			}
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
		CheckReports();
	}

	private void Start()
	{
		if (instance == null)
		{
			instance = this;
		}
		else if (instance != this)
		{
			UnityEngine.Object.Destroy(this);
		}
		RoomSystem.PlayerJoinedEvent += new Action<NetPlayer>(OnPlayerEnteredRoom);
		RoomSystem.PlayerLeftEvent += new Action<NetPlayer>(OnPlayerLeftRoom);
		RoomSystem.JoinedRoomEvent += (Action)delegate
		{
			cachedPlayerList = NetworkSystem.Instance.AllNetPlayers ?? new NetPlayer[0];
		};
		logErrorCount = 0;
		Application.logMessageReceived += LogErrorCount;
	}

	private void OnApplicationPause(bool paused)
	{
		if (!paused && RoomSystem.JoinedRoom)
		{
			lastServerTimestamp = NetworkSystem.Instance.SimTick;
			RefreshRPCs();
		}
	}

	public void LogErrorCount(string logString, string stackTrace, LogType type)
	{
		if (type != LogType.Error)
		{
			return;
		}
		logErrorCount++;
		stringIndex = logString.LastIndexOf("Sender is ");
		if (logString.Contains("RPC") && stringIndex >= 0)
		{
			playerID = logString.Substring(stringIndex + 10);
			tempPlayer = null;
			for (int i = 0; i < cachedPlayerList.Length; i++)
			{
				if (cachedPlayerList[i].UserId == playerID)
				{
					tempPlayer = cachedPlayerList[i];
					break;
				}
			}
			if (!IncrementRPCTracker(in tempPlayer, "invalid RPC stuff", in rpcErrorMax))
			{
				SendReport("invalid RPC stuff", tempPlayer.UserId, tempPlayer.NickName);
			}
			tempPlayer = null;
		}
		if (logErrorCount > logErrorMax)
		{
			Debug.unityLogger.logEnabled = false;
		}
	}

	public void SendReport(string susReason, string susId, string susNick)
	{
		suspiciousReason = susReason;
		suspiciousPlayerId = susId;
		suspiciousPlayerName = susNick;
		sendReport = true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void DispatchReport()
	{
		if ((sendReport || testAssault) && suspiciousPlayerId != "" && reportedPlayers.IndexOf(suspiciousPlayerId) == -1)
		{
			if (_suspiciousPlayerName.Length > 12)
			{
				_suspiciousPlayerName = _suspiciousPlayerName.Remove(12);
			}
			reportedPlayers.Add(suspiciousPlayerId);
			testAssault = false;
			WebFlags flags = new WebFlags(3);
			NetEventOptions options = new NetEventOptions
			{
				TargetActors = targetActors,
				Reciever = NetEventOptions.RecieverTarget.master,
				Flags = flags
			};
			string[] array = new string[cachedPlayerList.Length];
			int num = 0;
			NetPlayer[] array2 = cachedPlayerList;
			foreach (NetPlayer netPlayer in array2)
			{
				array[num] = netPlayer.UserId;
				num++;
			}
			object[] data = new object[7]
			{
				NetworkSystem.Instance.RoomStringStripped(),
				array,
				NetworkSystem.Instance.MasterClient.UserId,
				suspiciousPlayerId,
				suspiciousPlayerName,
				suspiciousReason,
				NetworkSystemConfig.AppVersion
			};
			NetworkSystemRaiseEvent.RaiseEvent(8, data, options, reliable: true);
			if (ShouldDisconnectFromRoom())
			{
				StartCoroutine(QuitDelay());
			}
		}
		_sendReport = false;
		_suspiciousPlayerId = "";
		_suspiciousPlayerName = "";
		_suspiciousReason = "";
	}

	private void CheckReports()
	{
		if (Time.time < lastCheck + reportCheckCooldown)
		{
			return;
		}
		lastCheck = Time.time;
		try
		{
			logErrorCount = 0;
			if (!RoomSystem.JoinedRoom)
			{
				return;
			}
			lastCheck = Time.time;
			lastServerTimestamp = NetworkSystem.Instance.SimTick;
			if (!PhotonNetwork.CurrentRoom.PublishUserId)
			{
				sendReport = true;
				suspiciousReason = "missing player ids";
				SetToRoomCreatorIfHere();
				CloseInvalidRoom();
			}
			if ((!RoomSystem.WasRoomSubscription && cachedPlayerList.Length > RoomSystem.GetCurrentRoomExpectedSize()) || cachedPlayerList.Length > 20)
			{
				sendReport = true;
				suspiciousReason = "too many players";
				SetToRoomCreatorIfHere();
				CloseInvalidRoom();
			}
			if (currentMasterClient != NetworkSystem.Instance.MasterClient || LowestActorNumber() != NetworkSystem.Instance.MasterClient.ActorNumber)
			{
				NetPlayer[] array = cachedPlayerList;
				foreach (NetPlayer netPlayer in array)
				{
					if (currentMasterClient == netPlayer)
					{
						sendReport = true;
						suspiciousReason = "room host force changed";
						suspiciousPlayerId = NetworkSystem.Instance.MasterClient.UserId;
						suspiciousPlayerName = NetworkSystem.Instance.MasterClient.NickName;
					}
				}
				currentMasterClient = NetworkSystem.Instance.MasterClient;
			}
			RefreshRPCs();
			DispatchReport();
		}
		catch
		{
		}
	}

	private void RefreshRPCs()
	{
		foreach (Dictionary<string, RPCCallTracker> value in userRPCCalls.Values)
		{
			foreach (RPCCallTracker value2 in value.Values)
			{
				value2.RPCCalls = 0;
			}
		}
	}

	private int LowestActorNumber()
	{
		lowestActorNumber = NetworkSystem.Instance.LocalPlayer.ActorNumber;
		NetPlayer[] array = cachedPlayerList;
		foreach (NetPlayer netPlayer in array)
		{
			if (netPlayer.ActorNumber < lowestActorNumber)
			{
				lowestActorNumber = netPlayer.ActorNumber;
			}
		}
		return lowestActorNumber;
	}

	public void OnPlayerEnteredRoom(NetPlayer newPlayer)
	{
		cachedPlayerList = NetworkSystem.Instance.AllNetPlayers ?? new NetPlayer[0];
	}

	public void OnPlayerLeftRoom(NetPlayer otherPlayer)
	{
		cachedPlayerList = NetworkSystem.Instance.AllNetPlayers ?? new NetPlayer[0];
		if (userRPCCalls.TryGetValue(otherPlayer.UserId, out var _))
		{
			userRPCCalls.Remove(otherPlayer.UserId);
		}
	}

	public static void IncrementRPCCall(PhotonMessageInfo info, [CallerMemberName] string callingMethod = "")
	{
		IncrementRPCCall(new PhotonMessageInfoWrapped(info), callingMethod);
	}

	public static void IncrementRPCCall(PhotonMessageInfoWrapped infoWrapped, [CallerMemberName] string callingMethod = "")
	{
		instance.IncrementRPCCallLocal(infoWrapped, callingMethod);
	}

	private void IncrementRPCCallLocal(PhotonMessageInfoWrapped infoWrapped, string rpcFunction)
	{
		if (infoWrapped.sentTick >= lastServerTimestamp)
		{
			NetPlayer player = NetworkSystem.Instance.GetPlayer(infoWrapped.senderID);
			if (player != null && !IncrementRPCTracker(player.UserId, in rpcFunction, in rpcCallLimit))
			{
				SendReport("too many rpc calls! " + rpcFunction, player.UserId, player.NickName);
			}
		}
	}

	private bool IncrementRPCTracker(in NetPlayer sender, in string rpcFunction, in int callLimit)
	{
		return IncrementRPCTracker(sender.UserId, in rpcFunction, in callLimit);
	}

	private bool IncrementRPCTracker(in Player sender, in string rpcFunction, in int callLimit)
	{
		return IncrementRPCTracker(sender.UserId, in rpcFunction, in callLimit);
	}

	private bool IncrementRPCTracker(in string userId, in string rpcFunction, in int callLimit)
	{
		RPCCallTracker rPCCallTracker = GetRPCCallTracker(userId, rpcFunction);
		if (rPCCallTracker == null)
		{
			return true;
		}
		rPCCallTracker.RPCCalls++;
		if (rPCCallTracker.RPCCalls > rPCCallTracker.RPCCallsMax)
		{
			rPCCallTracker.RPCCallsMax = rPCCallTracker.RPCCalls;
		}
		if (rPCCallTracker.RPCCalls > callLimit)
		{
			return false;
		}
		return true;
	}

	private RPCCallTracker GetRPCCallTracker(string userID, string rpcFunction)
	{
		if (userID == null)
		{
			return null;
		}
		RPCCallTracker value = null;
		if (!userRPCCalls.TryGetValue(userID, out var value2))
		{
			value = new RPCCallTracker
			{
				RPCCalls = 0,
				RPCCallsMax = 0
			};
			Dictionary<string, RPCCallTracker> dictionary = new Dictionary<string, RPCCallTracker>();
			dictionary.Add(rpcFunction, value);
			userRPCCalls.Add(userID, dictionary);
		}
		else if (!value2.TryGetValue(rpcFunction, out value))
		{
			value = new RPCCallTracker
			{
				RPCCalls = 0,
				RPCCallsMax = 0
			};
			value2.Add(rpcFunction, value);
		}
		return value;
	}

	private IEnumerator QuitDelay(float time = 1f)
	{
		yield return new WaitForSeconds(1f);
		NetworkSystem.Instance.ReturnToSinglePlayer();
	}

	private void SetToRoomCreatorIfHere()
	{
		tempPlayer = PhotonNetwork.CurrentRoom.GetPlayer(1);
		if (tempPlayer != null)
		{
			suspiciousPlayerId = tempPlayer.UserId;
			suspiciousPlayerName = tempPlayer.NickName;
		}
		else
		{
			suspiciousPlayerId = "n/a";
			suspiciousPlayerName = "n/a";
		}
	}

	private bool ShouldDisconnectFromRoom()
	{
		if (!_suspiciousReason.Contains("too many players") && !_suspiciousReason.Contains("invalid room name") && !_suspiciousReason.Contains("invalid game mode"))
		{
			return _suspiciousReason.Contains("missing player ids");
		}
		return true;
	}

	private void CloseInvalidRoom()
	{
		PhotonNetwork.CurrentRoom.IsOpen = false;
		PhotonNetwork.CurrentRoom.IsVisible = false;
		PhotonNetwork.CurrentRoom.MaxPlayers = RoomSystem.GetCurrentRoomExpectedSize();
	}
}

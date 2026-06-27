using System;
using System.Collections.Generic;
using GorillaNetworking;
using GorillaTag;
using UnityEngine;

public class GorillaScoreboardTotalUpdater : MonoBehaviour, IGorillaSliceableSimple
{
	public struct PlayerReports
	{
		public bool cheating;

		public bool toxicity;

		public bool hateSpeech;

		public bool pressedReport;

		public PlayerReports(PlayerReports reportToUpdate, GorillaPlayerScoreboardLine lineToUpdate)
		{
			cheating = reportToUpdate.cheating || lineToUpdate.reportedCheating;
			toxicity = reportToUpdate.toxicity || lineToUpdate.reportedToxicity;
			hateSpeech = reportToUpdate.hateSpeech || lineToUpdate.reportedHateSpeech;
			pressedReport = lineToUpdate.reportInProgress;
		}

		public PlayerReports(GorillaPlayerScoreboardLine lineToUpdate)
		{
			cheating = lineToUpdate.reportedCheating;
			toxicity = lineToUpdate.reportedToxicity;
			hateSpeech = lineToUpdate.reportedHateSpeech;
			pressedReport = lineToUpdate.reportInProgress;
		}
	}

	private static GorillaScoreboardTotalUpdater _instance = null;

	public static readonly List<GorillaPlayerScoreboardLine> allScoreboardLines = new List<GorillaPlayerScoreboardLine>();

	public static int lineIndex = 0;

	private const int linesPerFrame = 2;

	public static List<GorillaScoreBoard> allScoreboards = new List<GorillaScoreBoard>();

	private List<NetPlayer> playersInRoom = new List<NetPlayer>();

	private bool joinedRoom;

	private bool wasGameManagerNull;

	public string offlineTextErrorString;

	public Dictionary<int, PlayerReports> reportDict = new Dictionary<int, PlayerReports>();

	private static readonly Dictionary<int, ReportMuteTimer> m_reportMuteTimerDict = new Dictionary<int, ReportMuteTimer>(20);

	private static readonly ObjectPool<ReportMuteTimer> m_reportMuteTimerPool = new ObjectPool<ReportMuteTimer>(20);

	public static GorillaScoreboardTotalUpdater instance => _instance ?? (_instance = CreateManager());

	public static bool hasInstance => instance != null;

	public void UpdateLineState(GorillaPlayerScoreboardLine line)
	{
		if (line.playerActorNumber != -1)
		{
			if (reportDict.ContainsKey(line.playerActorNumber))
			{
				reportDict[line.playerActorNumber] = new PlayerReports(reportDict[line.playerActorNumber], line);
			}
			else
			{
				reportDict.Add(line.playerActorNumber, new PlayerReports(line));
			}
		}
	}

	protected void Awake()
	{
		if (_instance == this)
		{
			return;
		}
		if (_instance == null)
		{
			_instance = this;
			if (Application.isPlaying)
			{
				UnityEngine.Object.DontDestroyOnLoad(this);
			}
		}
		else
		{
			UnityEngine.Object.Destroy(this);
		}
	}

	private void Start()
	{
		RoomSystem.JoinedRoomEvent += new Action(JoinedRoom);
		RoomSystem.LeftRoomEvent += new Action(OnLeftRoom);
		RoomSystem.PlayerJoinedEvent += new Action<NetPlayer>(OnPlayerEnteredRoom);
		RoomSystem.PlayerLeftEvent += new Action<NetPlayer>(OnPlayerLeftRoom);
	}

	private static GorillaScoreboardTotalUpdater CreateManager()
	{
		GorillaScoreboardTotalUpdater gorillaScoreboardTotalUpdater = new GameObject("GorillaScoreboardTotalUpdater").AddComponent<GorillaScoreboardTotalUpdater>();
		if (Application.isPlaying)
		{
			UnityEngine.Object.DontDestroyOnLoad(gorillaScoreboardTotalUpdater);
		}
		return gorillaScoreboardTotalUpdater;
	}

	public static void RegisterSL(GorillaPlayerScoreboardLine sL)
	{
		if (!allScoreboardLines.Contains(sL))
		{
			allScoreboardLines.Add(sL);
		}
	}

	public static void UnregisterSL(GorillaPlayerScoreboardLine sL)
	{
		if (allScoreboardLines.Contains(sL))
		{
			allScoreboardLines.Remove(sL);
		}
	}

	public static void RegisterScoreboard(GorillaScoreBoard sB)
	{
		if (!allScoreboards.Contains(sB))
		{
			allScoreboards.Add(sB);
			instance.UpdateScoreboard(sB);
		}
	}

	public static void UnregisterScoreboard(GorillaScoreBoard sB)
	{
		if (allScoreboards.Contains(sB))
		{
			allScoreboards.Remove(sB);
		}
	}

	public void UpdateActiveScoreboards()
	{
		for (int i = 0; i < allScoreboards.Count; i++)
		{
			UpdateScoreboard(allScoreboards[i]);
		}
	}

	public void SetOfflineFailureText(string failureText)
	{
		offlineTextErrorString = failureText;
		UpdateActiveScoreboards();
	}

	public void ClearOfflineFailureText()
	{
		offlineTextErrorString = null;
		UpdateActiveScoreboards();
	}

	public void UpdateScoreboard(GorillaScoreBoard sB)
	{
		sB.SetSleepState(joinedRoom);
		if (GorillaComputer.instance == null)
		{
			return;
		}
		if (!joinedRoom)
		{
			if (sB.notInRoomText != null)
			{
				sB.notInRoomText.gameObject.SetActive(value: true);
				sB.notInRoomText.text = ((offlineTextErrorString != null) ? offlineTextErrorString : GorillaComputer.instance.offlineTextInitialString);
			}
			for (int i = 0; i < sB.lines.Count; i++)
			{
				sB.lines[i].ResetData();
			}
			sB.CleanupRoomControls();
			return;
		}
		if (sB.notInRoomText != null)
		{
			sB.notInRoomText.gameObject.SetActive(value: false);
		}
		for (int j = 0; j < sB.lines.Count; j++)
		{
			GorillaPlayerScoreboardLine gorillaPlayerScoreboardLine = sB.lines[j];
			if (j < playersInRoom.Count)
			{
				gorillaPlayerScoreboardLine.gameObject.SetActive(value: true);
				gorillaPlayerScoreboardLine.SetLineData(playersInRoom[j]);
			}
			else
			{
				gorillaPlayerScoreboardLine.ResetData();
				gorillaPlayerScoreboardLine.gameObject.SetActive(value: false);
			}
		}
		sB.RedrawPlayerLines();
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
		if (allScoreboardLines.Count == 0)
		{
			return;
		}
		for (int i = 0; i < 2; i++)
		{
			if (lineIndex >= allScoreboardLines.Count)
			{
				lineIndex = 0;
			}
			allScoreboardLines[lineIndex].UpdateLine();
			lineIndex++;
		}
		for (int j = 0; j < allScoreboards.Count; j++)
		{
			if (allScoreboards[j].IsDirty)
			{
				UpdateScoreboard(allScoreboards[j]);
			}
		}
	}

	private void OnPlayerEnteredRoom(NetPlayer netPlayer)
	{
		if (netPlayer == null)
		{
			Debug.LogError("Null netplayer");
		}
		if (!playersInRoom.Contains(netPlayer))
		{
			playersInRoom.Add(netPlayer);
		}
		UpdateActiveScoreboards();
	}

	private void OnPlayerLeftRoom(NetPlayer netPlayer)
	{
		if (netPlayer == null)
		{
			Debug.LogError("Null NetPlayer.");
		}
		playersInRoom.Remove(netPlayer);
		UpdateActiveScoreboards();
		if (m_reportMuteTimerDict.TryGetValue(netPlayer.ActorNumber, out var value))
		{
			m_reportMuteTimerDict.Remove(netPlayer.ActorNumber);
			m_reportMuteTimerPool.Return(value);
		}
	}

	internal void JoinedRoom()
	{
		joinedRoom = true;
		NetPlayer[] allNetPlayers = NetworkSystem.Instance.AllNetPlayers;
		foreach (NetPlayer item in allNetPlayers)
		{
			playersInRoom.Add(item);
		}
		playersInRoom.Sort((NetPlayer x, NetPlayer y) => x.ActorNumber.CompareTo(y.ActorNumber));
		foreach (GorillaScoreBoard allScoreboard in allScoreboards)
		{
			UpdateScoreboard(allScoreboard);
		}
	}

	private void OnLeftRoom()
	{
		joinedRoom = false;
		playersInRoom.Clear();
		reportDict.Clear();
		foreach (GorillaScoreBoard allScoreboard in allScoreboards)
		{
			UpdateScoreboard(allScoreboard);
		}
		foreach (KeyValuePair<int, ReportMuteTimer> item in m_reportMuteTimerDict)
		{
			m_reportMuteTimerPool.Return(item.Value);
		}
		m_reportMuteTimerDict.Clear();
	}

	public static void ReportMute(NetPlayer player, int muted)
	{
		if (m_reportMuteTimerDict.TryGetValue(player.ActorNumber, out var value))
		{
			value.Muted = muted;
			if (!value.Running)
			{
				value.Start();
			}
		}
		else
		{
			value = m_reportMuteTimerPool.Take();
			value.SetReportData(player.UserId, player.NickName, muted);
			value.coolDown = 5f;
			value.Start();
			m_reportMuteTimerDict[player.ActorNumber] = value;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Text;
using Fusion;
using GorillaExtensions;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class RacingManager : NetworkSceneObject, ITickSystemTick
{
	[Serializable]
	private struct RaceSetup
	{
		public BoxCollider startVolume;

		public int numCheckpoints;

		public float dqBaseDuration;

		public float dqInterval;
	}

	private struct RacerData
	{
		public int actorNumber;

		public string playerName;

		public int numCheckpointsPassed;

		public double latestCheckpointTime;

		public bool isDisqualified;
	}

	private class RacerComparer : IComparer<RacerData>
	{
		public static RacerComparer instance = new RacerComparer();

		public int Compare(RacerData a, RacerData b)
		{
			int num = a.isDisqualified.CompareTo(b.isDisqualified);
			if (num != 0)
			{
				return num;
			}
			int num2 = a.numCheckpointsPassed.CompareTo(b.numCheckpointsPassed);
			if (num2 != 0)
			{
				return -num2;
			}
			if (a.numCheckpointsPassed > 0)
			{
				return a.latestCheckpointTime.CompareTo(b.latestCheckpointTime);
			}
			return a.actorNumber.CompareTo(b.actorNumber);
		}
	}

	public enum RacingState
	{
		Inactive,
		Countdown,
		InProgress,
		Results
	}

	private class Race
	{
		private int raceIndex;

		private int numCheckpoints;

		private float dqBaseDuration;

		private float dqInterval;

		private BoxCollider raceStartZone;

		private PhotonView photonView;

		private List<RacerData> racers = new List<RacerData>(20);

		private Dictionary<NetPlayer, int> playerLookup = new Dictionary<NetPlayer, int>();

		private List<int> actorsInStartZone = new List<int>();

		private List<int> actorsInStartZone2 = new List<int>();

		private Dictionary<int, string> playerNamesInStartZone = new Dictionary<int, string>();

		private int numLapsSelected = 1;

		private double raceStartTime;

		private double abortRaceAtTimestamp;

		private float resultsEndTimestamp;

		private bool isInstanceLoaded;

		private int numCheckpointsToWin;

		private RaceVisual raceVisual;

		private bool hasLockedInParticipants;

		private float nextTickTimestamp;

		private static StringBuilder stringBuilder = new StringBuilder();

		private static StringBuilder timesStringBuilder = new StringBuilder();

		private static Collider[] overlapColliders = new Collider[20];

		private static int playerLayerMask = UnityLayer.GorillaBodyCollider.ToLayerMask() | UnityLayer.GorillaTagCollider.ToLayerMask();

		private float nextStartZoneUpdateTimestamp;

		public RacingState racingState { get; private set; }

		public Race(int raceIndex, RaceSetup setup, HashSet<int> actorsInAnyRace, PhotonView photonView)
		{
			this.raceIndex = raceIndex;
			numCheckpoints = setup.numCheckpoints;
			raceStartZone = setup.startVolume;
			dqBaseDuration = setup.dqBaseDuration;
			dqInterval = setup.dqInterval;
			this.photonView = photonView;
		}

		public void RegisterVisual(RaceVisual visual)
		{
			raceVisual = visual;
		}

		public void Clear()
		{
			hasLockedInParticipants = false;
			racers.Clear();
			playerLookup.Clear();
			racingState = RacingState.Inactive;
		}

		public bool IsActorLockedIntoRace(int actorNumber)
		{
			if (racingState != RacingState.InProgress || !hasLockedInParticipants)
			{
				return false;
			}
			for (int i = 0; i < racers.Count; i++)
			{
				if (racers[i].actorNumber == actorNumber)
				{
					return true;
				}
			}
			return false;
		}

		public void SendStateToNewPlayer(NetPlayer newPlayer)
		{
			switch (racingState)
			{
			case RacingState.Inactive:
			case RacingState.Results:
				break;
			case RacingState.Countdown:
				photonView.RPC("RaceBeginCountdown_RPC", RpcTarget.All, (byte)raceIndex, (byte)numLapsSelected, raceStartTime);
				break;
			case RacingState.InProgress:
				break;
			}
		}

		public void Tick()
		{
			if (Time.time >= nextTickTimestamp)
			{
				nextTickTimestamp = Time.time + TickWithNextDelay();
			}
		}

		public float TickWithNextDelay()
		{
			bool flag = raceVisual != null;
			if (flag)
			{
				raceVisual.ActivateStartingWall(racingState == RacingState.Countdown);
			}
			switch (racingState)
			{
			case RacingState.Inactive:
				if (flag)
				{
					RefreshStartingPlayerList();
				}
				return 1f;
			case RacingState.Countdown:
				if (raceStartTime > PhotonNetwork.Time)
				{
					if (flag)
					{
						RefreshStartingPlayerList();
						raceVisual.UpdateCountdown(Mathf.CeilToInt((float)(raceStartTime - PhotonNetwork.Time)));
					}
				}
				else
				{
					RaceCountdownEnds();
				}
				return 0.1f;
			case RacingState.InProgress:
				if (PhotonNetwork.IsMasterClient)
				{
					if (PhotonNetwork.Time > abortRaceAtTimestamp)
					{
						photonView.RPC("RaceEnded_RPC", RpcTarget.All, (byte)raceIndex);
					}
					else
					{
						int num = 0;
						for (int i = 0; i < racers.Count; i++)
						{
							if (racers[i].numCheckpointsPassed < numCheckpointsToWin)
							{
								num++;
							}
						}
						if (num == 0)
						{
							photonView.RPC("RaceEnded_RPC", RpcTarget.All, (byte)raceIndex);
						}
					}
				}
				return 1f;
			case RacingState.Results:
				if (Time.time >= resultsEndTimestamp)
				{
					if (flag)
					{
						raceVisual.OnRaceReset();
					}
					racingState = RacingState.Inactive;
				}
				return 1f;
			default:
				return 1f;
			}
		}

		public void RaceEnded()
		{
			if (racingState != RacingState.InProgress)
			{
				return;
			}
			racingState = RacingState.Results;
			resultsEndTimestamp = Time.time + 10f;
			if (raceVisual != null)
			{
				raceVisual.OnRaceEnded();
			}
			for (int i = 0; i < racers.Count; i++)
			{
				RacerData value = racers[i];
				if (value.numCheckpointsPassed < numCheckpointsToWin)
				{
					value.isDisqualified = true;
					racers[i] = value;
				}
			}
			racers.Sort(RacerComparer.instance);
			OnRacerOrderChanged();
			for (int j = 0; j < racers.Count; j++)
			{
				if (racers[j].actorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
				{
					VRRig.LocalRig.hoverboardVisual.SetRaceDisplay("");
					VRRig.LocalRig.hoverboardVisual.SetRaceLapsDisplay("");
					break;
				}
			}
		}

		private void RefreshStartingPlayerList()
		{
			if (raceVisual != null && UpdateActorsInStartZone())
			{
				stringBuilder.Clear();
				stringBuilder.AppendLine("NEXT RACE LINEUP");
				for (int i = 0; i < actorsInStartZone.Count; i++)
				{
					stringBuilder.Append("    ");
					stringBuilder.AppendLine(playerNamesInStartZone[actorsInStartZone[i]]);
				}
				raceVisual.SetRaceStartScoreboardText(stringBuilder.ToString(), "");
			}
		}

		public void Button_StartRace(int laps)
		{
			if (racingState == RacingState.Inactive)
			{
				photonView.RPC("RequestRaceStart_RPC", RpcTarget.MasterClient, raceIndex, laps);
			}
		}

		public void Host_RequestRaceStart(int laps, int requestedByActorNumber)
		{
			if (racingState == RacingState.Inactive)
			{
				UpdateActorsInStartZone();
				if (actorsInStartZone.Contains(requestedByActorNumber))
				{
					photonView.RPC("RaceBeginCountdown_RPC", RpcTarget.All, (byte)raceIndex, (byte)laps, PhotonNetwork.Time + 4.0);
				}
			}
		}

		public void BeginCountdown(double startTime, int laps)
		{
			if (racingState == RacingState.Inactive)
			{
				racingState = RacingState.Countdown;
				raceStartTime = startTime;
				abortRaceAtTimestamp = startTime + (double)dqBaseDuration;
				numLapsSelected = laps;
				numCheckpointsToWin = numCheckpoints * laps + 1;
				hasLockedInParticipants = false;
				if (raceVisual != null)
				{
					raceVisual.OnCountdownStart(laps, (float)(startTime - PhotonNetwork.Time));
				}
			}
		}

		public void RaceCountdownEnds()
		{
			if (racingState == RacingState.Countdown)
			{
				racingState = RacingState.InProgress;
				if (raceVisual != null)
				{
					raceVisual.OnRaceStart();
				}
				UpdateActorsInStartZone();
				if (PhotonNetwork.IsMasterClient)
				{
					photonView.RPC("RaceLockInParticipants_RPC", RpcTarget.All, (byte)raceIndex, actorsInStartZone.ToArray());
				}
				else if (actorsInStartZone.Count >= 1)
				{
					LockInParticipants(actorsInStartZone.ToArray(), isProvisional: true);
				}
			}
		}

		public void LockInParticipants(int[] participantActorNumbers, bool isProvisional = false)
		{
			if (hasLockedInParticipants)
			{
				return;
			}
			if (!isProvisional && participantActorNumbers.Length < 1)
			{
				racingState = RacingState.Inactive;
				return;
			}
			racers.Clear();
			if (participantActorNumbers.Length != 0)
			{
				foreach (VRRig activeRig in VRRigCache.ActiveRigs)
				{
					int actorNumber = activeRig.OwningNetPlayer.ActorNumber;
					if (participantActorNumbers.BinarySearch(actorNumber) >= 0 && !instance.IsActorLockedIntoAnyRace(actorNumber))
					{
						racers.Add(new RacerData
						{
							actorNumber = actorNumber,
							playerName = activeRig.OwningNetPlayer.SanitizedNickName,
							latestCheckpointTime = raceStartTime
						});
					}
				}
			}
			if (!isProvisional)
			{
				if (racers.Count < 1)
				{
					racingState = RacingState.Inactive;
					return;
				}
				hasLockedInParticipants = true;
			}
			racers.Sort(RacerComparer.instance);
			OnRacerOrderChanged();
		}

		public void PassCheckpoint(Player player, int checkpointIndex, double time)
		{
			if (racingState == RacingState.Inactive || time < raceStartTime || time < PhotonNetwork.Time - 5.0 || time > PhotonNetwork.Time + 0.10000000149011612)
			{
				return;
			}
			if (abortRaceAtTimestamp < time + (double)dqInterval)
			{
				abortRaceAtTimestamp = time + (double)dqInterval;
			}
			RacerData racerData = default(RacerData);
			int i;
			for (i = 0; i < racers.Count; i++)
			{
				racerData = racers[i];
				if (racerData.actorNumber == player.ActorNumber)
				{
					if (racerData.numCheckpointsPassed < numCheckpointsToWin && !racerData.isDisqualified && checkpointIndex == racerData.numCheckpointsPassed % numCheckpoints && (!(raceVisual != null) || !VRRigCache.Instance.TryGetVrrig(player, out var playerRig) || raceVisual.IsPlayerNearCheckpoint(playerRig.Rig, checkpointIndex)))
					{
						break;
					}
					return;
				}
			}
			if (racerData.actorNumber != player.ActorNumber)
			{
				return;
			}
			racerData.numCheckpointsPassed++;
			racerData.latestCheckpointTime = time;
			racers[i] = racerData;
			if (racerData.numCheckpointsPassed >= numCheckpointsToWin || (i > 0 && RacerComparer.instance.Compare(racers[i - 1], racerData) > 0))
			{
				racers.Sort(RacerComparer.instance);
				OnRacerOrderChanged();
			}
			if (!player.IsLocal)
			{
				return;
			}
			if (checkpointIndex == numCheckpoints - 1)
			{
				int num = racerData.numCheckpointsPassed / numCheckpoints + 1;
				if (num > numLapsSelected)
				{
					raceVisual.ShowFinishLineText("FINISH");
					raceVisual.EnableRaceEndSound();
				}
				else if (num == numLapsSelected)
				{
					raceVisual.ShowFinishLineText("FINAL LAP");
				}
				else
				{
					raceVisual.ShowFinishLineText("NEXT LAP");
				}
			}
			else if (checkpointIndex == 0)
			{
				int num2 = racerData.numCheckpointsPassed / numCheckpoints + 1;
				if (num2 > numLapsSelected)
				{
					VRRig.LocalRig.hoverboardVisual.SetRaceLapsDisplay("");
				}
				else
				{
					VRRig.LocalRig.hoverboardVisual.SetRaceLapsDisplay($"LAP {num2}/{numLapsSelected}");
				}
			}
		}

		private void OnRacerOrderChanged()
		{
			if (!(raceVisual != null))
			{
				return;
			}
			stringBuilder.Clear();
			timesStringBuilder.Clear();
			timesStringBuilder.AppendLine("");
			bool flag = false;
			switch (racingState)
			{
			case RacingState.Inactive:
				return;
			case RacingState.Countdown:
				stringBuilder.AppendLine("STARTING LINEUP");
				flag = true;
				break;
			case RacingState.InProgress:
				stringBuilder.AppendLine("RACE LEADERBOARD");
				break;
			case RacingState.Results:
				stringBuilder.AppendLine("RACE RESULTS");
				break;
			}
			for (int i = 0; i < racers.Count; i++)
			{
				RacerData racerData = racers[i];
				if (racerData.actorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
				{
					VRRig.LocalRig.hoverboardVisual.SetRaceDisplay(racerData.isDisqualified ? "DQ" : (i + 1).ToString());
				}
				string text = (racerData.isDisqualified ? "DQ. " : (flag ? "    " : (i + 1 + ". ")));
				stringBuilder.Append(text);
				if (text.Length <= 3)
				{
					stringBuilder.Append(" ");
				}
				stringBuilder.AppendLine(racerData.playerName);
				if (racerData.isDisqualified)
				{
					timesStringBuilder.AppendLine("--.--");
				}
				else if (racerData.numCheckpointsPassed < numCheckpointsToWin)
				{
					timesStringBuilder.AppendLine("");
				}
				else
				{
					timesStringBuilder.AppendLine($"{racerData.latestCheckpointTime - raceStartTime:0.00}");
				}
			}
			string mainText = stringBuilder.ToString();
			string timesText = timesStringBuilder.ToString();
			raceVisual.SetScoreboardText(mainText, timesText);
			raceVisual.SetRaceStartScoreboardText(mainText, timesText);
		}

		private bool UpdateActorsInStartZone()
		{
			if (Time.time < nextStartZoneUpdateTimestamp)
			{
				return false;
			}
			nextStartZoneUpdateTimestamp = Time.time + 0.1f;
			List<int> list = actorsInStartZone2;
			List<int> list2 = actorsInStartZone;
			actorsInStartZone = list;
			actorsInStartZone2 = list2;
			actorsInStartZone.Clear();
			playerNamesInStartZone.Clear();
			int a = Physics.OverlapBoxNonAlloc(raceStartZone.transform.position, raceStartZone.size / 2f, overlapColliders, raceStartZone.transform.rotation, playerLayerMask);
			a = Mathf.Min(a, overlapColliders.Length);
			for (int i = 0; i < a; i++)
			{
				Collider collider = overlapColliders[i];
				if (collider == null)
				{
					continue;
				}
				VRRig component = collider.attachedRigidbody.gameObject.GetComponent<VRRig>();
				int count = actorsInStartZone.Count;
				if (component == null)
				{
					continue;
				}
				if (component.isLocal)
				{
					if (NetworkSystem.Instance.LocalPlayer == null)
					{
						overlapColliders[i] = null;
						continue;
					}
					if (instance.IsActorLockedIntoAnyRace(NetworkSystem.Instance.LocalPlayer.ActorNumber))
					{
						continue;
					}
					actorsInStartZone.AddSortedUnique(NetworkSystem.Instance.LocalPlayer.ActorNumber);
					if (actorsInStartZone.Count > count)
					{
						playerNamesInStartZone.Add(NetworkSystem.Instance.LocalPlayer.ActorNumber, component.playerNameVisible);
					}
				}
				else
				{
					if (instance.IsActorLockedIntoAnyRace(component.OwningNetPlayer.ActorNumber))
					{
						continue;
					}
					actorsInStartZone.AddSortedUnique(component.OwningNetPlayer.ActorNumber);
					if (actorsInStartZone.Count > count)
					{
						playerNamesInStartZone.Add(component.OwningNetPlayer.ActorNumber, component.playerNameVisible);
					}
				}
				overlapColliders[i] = null;
			}
			if (actorsInStartZone2.Count != actorsInStartZone.Count)
			{
				return true;
			}
			for (int j = 0; j < actorsInStartZone.Count; j++)
			{
				if (actorsInStartZone[j] != actorsInStartZone2[j])
				{
					return true;
				}
			}
			return false;
		}
	}

	[SerializeField]
	private RaceSetup[] raceSetups;

	private const int MinPlayersInRace = 1;

	private const float ResultsDuration = 10f;

	private Race[] races;

	public static RacingManager instance { get; private set; }

	public bool TickRunning { get; set; }

	private void Awake()
	{
		instance = this;
		HashSet<int> actorsInAnyRace = new HashSet<int>();
		races = new Race[raceSetups.Length];
		for (int i = 0; i < raceSetups.Length; i++)
		{
			races[i] = new Race(i, raceSetups[i], actorsInAnyRace, photonView);
		}
		RoomSystem.JoinedRoomEvent += new Action(OnRoomJoin);
		RoomSystem.PlayerJoinedEvent += new Action<NetPlayer>(OnPlayerJoined);
	}

	protected override void OnEnable()
	{
		NetworkBehaviourUtils.InternalOnEnable(this);
		TickSystem<object>.AddTickCallback(this);
		base.OnEnable();
	}

	protected override void OnDisable()
	{
		NetworkBehaviourUtils.InternalOnDisable(this);
		TickSystem<object>.RemoveTickCallback(this);
		base.OnDisable();
	}

	private void OnRoomJoin()
	{
		for (int i = 0; i < races.Length; i++)
		{
			races[i].Clear();
		}
	}

	private void OnPlayerJoined(NetPlayer player)
	{
		if (NetworkSystem.Instance.IsMasterClient)
		{
			for (int i = 0; i < races.Length; i++)
			{
				races[i].SendStateToNewPlayer(player);
			}
		}
	}

	public void RegisterVisual(RaceVisual visual)
	{
		int raceId = visual.raceId;
		if (raceId >= 0 && raceId < races.Length)
		{
			races[raceId].RegisterVisual(visual);
		}
	}

	public void Button_StartRace(int raceId, int laps)
	{
		if (raceId >= 0 && raceId < races.Length)
		{
			races[raceId].Button_StartRace(laps);
		}
	}

	[PunRPC]
	private void RequestRaceStart_RPC(int raceId, int laps, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "RequestRaceStart_RPC");
		if (PhotonNetwork.IsMasterClient && (laps == 1 || laps == 3 || laps == 5) && raceId >= 0 && raceId < races.Length)
		{
			races[raceId].Host_RequestRaceStart(laps, info.Sender.ActorNumber);
		}
	}

	[PunRPC]
	private void RaceBeginCountdown_RPC(byte raceId, byte laps, double startTime, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "RaceBeginCountdown_RPC");
		if (info.Sender.IsMasterClient && (laps == 1 || laps == 3 || laps == 5) && double.IsFinite(startTime) && !(startTime < PhotonNetwork.Time) && !(startTime > PhotonNetwork.Time + 4.0) && raceId >= 0 && raceId < races.Length)
		{
			races[raceId].BeginCountdown(startTime, laps);
		}
	}

	[PunRPC]
	private void RaceLockInParticipants_RPC(byte raceId, int[] participantActorNumbers, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "RaceLockInParticipants_RPC");
		if (!info.Sender.IsMasterClient || participantActorNumbers.Length > 20)
		{
			return;
		}
		for (int i = 1; i < participantActorNumbers.Length; i++)
		{
			if (participantActorNumbers[i] <= participantActorNumbers[i - 1])
			{
				return;
			}
		}
		if (raceId >= 0 && raceId < races.Length)
		{
			races[raceId].LockInParticipants(participantActorNumbers);
		}
	}

	public void OnCheckpointPassed(int raceId, int checkpointIndex)
	{
		photonView.RPC("PassCheckpoint_RPC", RpcTarget.All, (byte)raceId, (byte)checkpointIndex);
	}

	[PunRPC]
	private void PassCheckpoint_RPC(byte raceId, byte checkpointIndex, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "PassCheckpoint_RPC");
		if (raceId >= 0 && raceId < races.Length)
		{
			races[raceId].PassCheckpoint(info.Sender, checkpointIndex, info.SentServerTime);
		}
	}

	[PunRPC]
	private void RaceEnded_RPC(byte raceId, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "RaceEnded_RPC");
		if (info.Sender.IsMasterClient && raceId >= 0 && raceId < races.Length)
		{
			races[raceId].RaceEnded();
		}
	}

	void ITickSystemTick.Tick()
	{
		for (int i = 0; i < races.Length; i++)
		{
			races[i].Tick();
		}
	}

	public bool IsActorLockedIntoAnyRace(int actorNumber)
	{
		for (int i = 0; i < races.Length; i++)
		{
			if (races[i].IsActorLockedIntoRace(actorNumber))
			{
				return true;
			}
		}
		return false;
	}
}

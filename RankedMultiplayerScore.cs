using System;
using System.Collections.Generic;
using System.Linq;
using GorillaGameModes;
using Photon.Pun;
using UnityEngine;

public class RankedMultiplayerScore : MonoBehaviourTick
{
	public struct PlayerScore
	{
		public int PlayerId;

		public float GameScore;

		public float EloScore;

		public int NumTags;

		public float TimeUntagged;

		public float PointsOnDefense;
	}

	public struct PlayerScoreInRound(int id, bool initInfected = false)
	{
		public int PlayerId = id;

		public int NumTags = 0;

		public float PointsOnDefense = 0f;

		public float JoinTime = Time.time;

		public float TaggedTime = (initInfected ? Time.time : 0f);

		public bool Infected = initInfected;
	}

	public struct ResultData
	{
		public float Elo;

		public int Rank;

		public int MostTags;

		public float LongestUntagged;

		public int MostTagsPlayerId;

		public int LongestUntaggedPlayerId;

		public bool IsMostTagsTied()
		{
			return MostTagsPlayerId == RESULT_TIE;
		}

		public bool IsLongestUntaggedTied()
		{
			return LongestUntaggedPlayerId == RESULT_TIE;
		}
	}

	public struct RecordHolder<T>
	{
		public int PlayerId;

		public T Value;
	}

	public static float LongestUntaggedTieEpsilon = 0.2f;

	public static int RESULT_TIE = -1;

	[SerializeField]
	private int PointsPerTag = 30;

	[SerializeField]
	private float PointsPerUninfectedSecMin = 0.5f;

	[SerializeField]
	private float PointsPerUninfectedSecMax = 2f;

	private float PerSecondTimer = -1f;

	private bool WasInfectedInitially;

	private GorillaTagCompetitiveManager CompetitiveManager;

	protected Dictionary<int, PlayerScoreInRound> AllPlayerInRoundScores = new Dictionary<int, PlayerScoreInRound>();

	protected List<PlayerScore> AllFinalPlayerScores = new List<PlayerScore>();

	protected Dictionary<int, bool> VisitedScoreCombintations = new Dictionary<int, bool>();

	protected Dictionary<int, float> InProgressEloDeltaPerPlayer = new Dictionary<int, float>();

	protected Dictionary<int, int> PlayerRankedTierIndices = new Dictionary<int, int>();

	protected Dictionary<int, float> PlayerRankedElos = new Dictionary<int, float>();

	private ResultData PendingResults;

	private RecordHolder<int> ResultsMostTags;

	private RecordHolder<float> ResultsLongestUntagged;

	private bool IsLateJoiner;

	public RankedProgressionManager Progression { get; private set; }

	public Dictionary<int, int> PlayerRankedTiers
	{
		get
		{
			return PlayerRankedTierIndices;
		}
		set
		{
			PlayerRankedTierIndices = value;
		}
	}

	public Dictionary<int, float> PlayerRankedEloScores
	{
		get
		{
			return PlayerRankedElos;
		}
		set
		{
			PlayerRankedElos = value;
		}
	}

	public Dictionary<int, float> ProjectedEloDeltas
	{
		get
		{
			return InProgressEloDeltaPerPlayer;
		}
		set
		{
			InProgressEloDeltaPerPlayer = value;
		}
	}

	public void Initialize()
	{
		GorillaTagCompetitiveManager.onStateChanged += OnStateChanged;
		GorillaTagCompetitiveManager.onRoundStart += OnGameStarted;
		GorillaTagCompetitiveManager.onRoundEnd += OnGameEnded;
		GorillaTagCompetitiveManager.onPlayerJoined += OnPlayerJoined;
		GorillaTagCompetitiveManager.onPlayerLeft += OnPlayerLeft;
		GorillaTagCompetitiveManager.onTagOccurred += OnTagReported;
		GorillaGameManager instance = GorillaGameManager.instance;
		if (instance != null)
		{
			CompetitiveManager = instance as GorillaTagCompetitiveManager;
		}
		Progression = RankedProgressionManager.Instance;
		RankedProgressionManager progression = Progression;
		progression.OnPlayerEloAcquired = (Action<int, float, int>)Delegate.Combine(progression.OnPlayerEloAcquired, new Action<int, float, int>(HandlePlayerEloAcquired));
	}

	private void HandlePlayerEloAcquired(int playerId, float elo, int tier)
	{
		CachePlayerRankedProgressionData(playerId, tier, elo);
	}

	private void OnDestroy()
	{
		Unsubscribe();
	}

	public void Unsubscribe()
	{
		GorillaTagCompetitiveManager.onStateChanged -= OnStateChanged;
		GorillaTagCompetitiveManager.onRoundStart -= OnGameStarted;
		GorillaTagCompetitiveManager.onRoundEnd -= OnGameEnded;
		GorillaTagCompetitiveManager.onPlayerJoined -= OnPlayerJoined;
		GorillaTagCompetitiveManager.onPlayerLeft -= OnPlayerLeft;
		GorillaTagCompetitiveManager.onTagOccurred -= OnTagReported;
		if (Progression != null)
		{
			RankedProgressionManager progression = Progression;
			progression.OnPlayerEloAcquired = (Action<int, float, int>)Delegate.Remove(progression.OnPlayerEloAcquired, new Action<int, float, int>(HandlePlayerEloAcquired));
		}
	}

	public override void Tick()
	{
		if (PerSecondTimer > 0f && Time.time >= PerSecondTimer + 1f && !(CompetitiveManager == null))
		{
			OnPerSecondTimerElapsed(NetworkSystem.Instance.AllNetPlayers.Length, CompetitiveManager.currentInfected.Count);
			PerSecondTimer = Time.time;
		}
	}

	private void OnPerSecondTimerElapsed(int playersInGame, int infectedPlayers)
	{
		foreach (int item in AllPlayerInRoundScores.Keys.ToList())
		{
			PlayerScoreInRound value = AllPlayerInRoundScores[item];
			value.Infected = CompetitiveManager.IsInfected(NetworkSystem.Instance.GetPlayer(item));
			if (!value.Infected)
			{
				float t = (float)infectedPlayers / (float)playersInGame;
				value.PointsOnDefense += Mathf.Lerp(PointsPerUninfectedSecMin, PointsPerUninfectedSecMax, t);
			}
			AllPlayerInRoundScores[item] = value;
		}
	}

	public void ResetMatch()
	{
		AllFinalPlayerScores.Clear();
		AllPlayerInRoundScores.Clear();
	}

	private void OnStateChanged(GorillaTagCompetitiveManager.GameState state)
	{
		if (state == GorillaTagCompetitiveManager.GameState.StartingCountdown)
		{
			OnGameStarted();
			Progression.AcquireRoomRankInformation();
		}
	}

	public void OnGameStarted()
	{
		PerSecondTimer = Time.time;
		if (!IsLateJoiner)
		{
			ResetMatch();
			for (int i = 0; i < NetworkSystem.Instance.AllNetPlayers.Length; i++)
			{
				StartTrackingPlayer(NetworkSystem.Instance.AllNetPlayers[i], lateJoin: false);
			}
		}
	}

	public void OnGameEnded()
	{
		foreach (int item in AllPlayerInRoundScores.Keys.ToList())
		{
			PlayerScoreInRound value = AllPlayerInRoundScores[item];
			if (!value.Infected)
			{
				value.TaggedTime = Time.time;
			}
			AllPlayerInRoundScores[item] = value;
		}
		PerSecondTimer = -1f;
		ReportScore();
		WasInfectedInitially = false;
		IsLateJoiner = false;
	}

	private void OnPlayerJoined(NetPlayer player)
	{
		if (NetworkSystem.Instance.IsMasterClient && CompetitiveManager.IsMatchActive())
		{
			List<int> list = new List<int>();
			List<int> list2 = new List<int>();
			List<float> list3 = new List<float>();
			List<float> list4 = new List<float>();
			List<bool> list5 = new List<bool>();
			List<float> list6 = new List<float>();
			foreach (KeyValuePair<int, PlayerScoreInRound> allPlayerInRoundScore in AllPlayerInRoundScores)
			{
				list.Add(allPlayerInRoundScore.Value.PlayerId);
				list2.Add(allPlayerInRoundScore.Value.NumTags);
				list3.Add(allPlayerInRoundScore.Value.PointsOnDefense);
				list4.Add(Time.time - allPlayerInRoundScore.Value.JoinTime);
				list5.Add(allPlayerInRoundScore.Value.Infected);
				if (!allPlayerInRoundScore.Value.Infected)
				{
					list6.Add(0f);
				}
				else
				{
					list6.Add(Time.time - allPlayerInRoundScore.Value.TaggedTime);
				}
			}
			GameMode.ActiveNetworkHandler.SendRPC("SendScoresToLateJoinerRPC", player, list.ToArray(), list2.ToArray(), list3.ToArray(), list4.ToArray(), list5.ToArray(), list6.ToArray());
		}
		StartTrackingPlayer(player, lateJoin: true);
	}

	public void ReceivedScoresForLateJoiner(int[] playerIds, int[] numTags, float[] pointsOnDefense, float[] joinTime, bool[] infected, float[] taggedTime)
	{
		if (NetworkSystem.Instance.IsMasterClient)
		{
			return;
		}
		IsLateJoiner = true;
		for (int i = 0; i < playerIds.Length; i++)
		{
			int num = playerIds[i];
			PlayerScoreInRound value = new PlayerScoreInRound(num, infected[i]);
			value.NumTags = numTags[i];
			value.PointsOnDefense = pointsOnDefense[i];
			value.JoinTime = Time.time - joinTime[i];
			if (!infected[i])
			{
				value.TaggedTime = 0f;
			}
			else
			{
				value.TaggedTime = Time.time - taggedTime[i];
			}
			AllPlayerInRoundScores.TryAdd(num, value);
		}
	}

	private void OnPlayerLeft(NetPlayer player)
	{
		AllPlayerInRoundScores.Remove(player.ActorNumber);
	}

	private void StartTrackingPlayer(NetPlayer player, bool lateJoin)
	{
		bool initInfected = lateJoin;
		if (!lateJoin && CompetitiveManager != null)
		{
			initInfected = CompetitiveManager.IsInfected(player);
			if (player.ActorNumber == NetworkSystem.Instance.LocalPlayerID)
			{
				WasInfectedInitially = true;
			}
		}
		if (player == NetworkSystem.Instance.LocalPlayer)
		{
			CachePlayerRankedProgressionData(player.ActorNumber, Progression.GetProgressionRankIndex(), Progression.GetEloScore());
		}
		AllPlayerInRoundScores.TryAdd(player.ActorNumber, new PlayerScoreInRound(player.ActorNumber, initInfected));
	}

	public PlayerScoreInRound GetInGameScoreForSelf()
	{
		if (AllPlayerInRoundScores.TryGetValue(NetworkSystem.Instance.LocalPlayerID, out var value))
		{
			return value;
		}
		return default(PlayerScoreInRound);
	}

	public void OnTagReported(NetPlayer taggedPlayer, NetPlayer taggingPlayer)
	{
		if (AllPlayerInRoundScores.TryGetValue(taggingPlayer.ActorNumber, out var value))
		{
			value.NumTags++;
			AllPlayerInRoundScores[taggingPlayer.ActorNumber] = value;
		}
		if (AllPlayerInRoundScores.TryGetValue(taggedPlayer.ActorNumber, out var value2))
		{
			value2.Infected = true;
			value2.TaggedTime = Time.time;
			AllPlayerInRoundScores[taggedPlayer.ActorNumber] = value2;
		}
	}

	private void ReportScore()
	{
		if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("matchId", out var value))
		{
			foreach (KeyValuePair<int, PlayerScoreInRound> allPlayerInRoundScore in AllPlayerInRoundScores)
			{
				AllFinalPlayerScores.Add(new PlayerScore
				{
					PlayerId = allPlayerInRoundScore.Key,
					GameScore = ComputeGameScore(allPlayerInRoundScore.Value.NumTags, allPlayerInRoundScore.Value.PointsOnDefense),
					EloScore = (PlayerRankedElos.ContainsKey(allPlayerInRoundScore.Key) ? PlayerRankedElos[allPlayerInRoundScore.Key] : 0f),
					NumTags = allPlayerInRoundScore.Value.NumTags,
					TimeUntagged = allPlayerInRoundScore.Value.TaggedTime - allPlayerInRoundScore.Value.JoinTime,
					PointsOnDefense = allPlayerInRoundScore.Value.PointsOnDefense
				});
			}
			GorillaTagCompetitiveServerApi.Instance.RequestSubmitMatchScores((string)value, AllFinalPlayerScores);
		}
		PredictPlayerEloChanges();
	}

	public float ComputeGameScore(int tags, float pointsOnDefense)
	{
		return (float)(tags * PointsPerTag) + pointsOnDefense;
	}

	private void PredictPlayerEloChanges()
	{
		VisitedScoreCombintations.Clear();
		AllFinalPlayerScores = AllFinalPlayerScores.OrderByDescending((PlayerScore s) => s.GameScore).ToList();
		float k = Progression.MaxEloConstant / (float)(AllFinalPlayerScores.Count - 1);
		InProgressEloDeltaPerPlayer.Clear();
		for (int num = 0; num < AllFinalPlayerScores.Count; num++)
		{
			InProgressEloDeltaPerPlayer.Add(AllFinalPlayerScores[num].PlayerId, 0f);
		}
		for (int num2 = 0; num2 < AllFinalPlayerScores.Count; num2++)
		{
			for (int num3 = 0; num3 < AllFinalPlayerScores.Count; num3++)
			{
				if (num2 != num3)
				{
					bool flag = AllFinalPlayerScores[num2].GameScore.Approx(AllFinalPlayerScores[num3].GameScore);
					float num4 = 0f;
					float eloWinProbability = RankedProgressionManager.GetEloWinProbability(AllFinalPlayerScores[num3].EloScore, AllFinalPlayerScores[num2].EloScore);
					float eloWinProbability2 = RankedProgressionManager.GetEloWinProbability(AllFinalPlayerScores[num2].EloScore, AllFinalPlayerScores[num3].EloScore);
					int key = num2 * AllFinalPlayerScores.Count + num3;
					if (!VisitedScoreCombintations.ContainsKey(key))
					{
						PlayerScore playerScore = AllFinalPlayerScores[num2];
						num4 = ((!flag) ? ((float)((num2 < num3) ? 1 : 0)) : 0.5f);
						float eloScore = playerScore.EloScore;
						float num5 = RankedProgressionManager.UpdateEloScore(eloScore, eloWinProbability, num4, k);
						InProgressEloDeltaPerPlayer[playerScore.PlayerId] += num5 - eloScore;
						VisitedScoreCombintations.Add(key, value: true);
					}
					int key2 = num3 * AllFinalPlayerScores.Count + num2;
					if (!VisitedScoreCombintations.ContainsKey(key2))
					{
						PlayerScore playerScore2 = AllFinalPlayerScores[num3];
						num4 = ((!flag) ? ((float)((num3 < num2) ? 1 : 0)) : 0.5f);
						float eloScore2 = playerScore2.EloScore;
						float num6 = RankedProgressionManager.UpdateEloScore(eloScore2, eloWinProbability2, num4, k);
						InProgressEloDeltaPerPlayer[playerScore2.PlayerId] += num6 - eloScore2;
						VisitedScoreCombintations.Add(key2, value: true);
					}
				}
			}
		}
	}

	public void CachePlayerRankedProgressionData(int playerId, int tierIdx, float elo)
	{
		if (PlayerRankedTierIndices.ContainsKey(playerId))
		{
			PlayerRankedTierIndices[playerId] = tierIdx;
		}
		else
		{
			PlayerRankedTierIndices.Add(playerId, tierIdx);
		}
		if (PlayerRankedElos.ContainsKey(playerId))
		{
			PlayerRankedElos[playerId] = elo;
		}
		else
		{
			PlayerRankedElos.Add(playerId, elo);
		}
	}

	public List<PlayerScoreInRound> GetSortedScores()
	{
		List<PlayerScoreInRound> list = new List<PlayerScoreInRound>();
		foreach (KeyValuePair<int, PlayerScoreInRound> allPlayerInRoundScore in AllPlayerInRoundScores)
		{
			list.Add(allPlayerInRoundScore.Value);
		}
		list.Sort((PlayerScoreInRound s1, PlayerScoreInRound s2) => ComputeGameScore(s2.NumTags, s2.PointsOnDefense).CompareTo(ComputeGameScore(s1.NumTags, s1.PointsOnDefense)));
		return list;
	}
}

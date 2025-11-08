using System;
using System.Collections;
using System.Collections.Generic;
using GorillaGameModes;
using GorillaNetworking;
using UnityEngine;

public class RankedProgressionManager : MonoBehaviour
{
	public enum ERankedMatchmakingTier
	{
		Low,
		Medium,
		High
	}

	public enum ERankedProgressionEventType
	{
		None,
		Progress,
		Promotion,
		Relegation
	}

	public class RankedProgressionEvent
	{
		public ERankedProgressionEventType evtType;

		public Sprite progressIconLeft;

		public Sprite progressIconRight;

		public Sprite newTierIcon;

		public string leftName;

		public string rightName;

		public string newTierName;

		public float minVal;

		public float maxVal;

		public float delta;

		public override string ToString()
		{
			string text = "Progression Info\n";
			text += $"Event Type: {evtType.ToString()}\n";
			text += $"Left Tier: {leftName}\n";
			text += $"Right Tier: {rightName}\n";
			text += string.Format("Left Value: {0}\n", minVal.ToString("N0"));
			text += string.Format("Right Value: {0}\n", maxVal.ToString("N0"));
			text += string.Format("Elo Delta: {0}\n", delta.ToString("N0"));
			if (evtType == ERankedProgressionEventType.Promotion || evtType == ERankedProgressionEventType.Relegation)
			{
				text += $"Fanfare Tier: {newTierName}\n";
			}
			return text;
		}
	}

	public abstract class RankedProgressionTierBase
	{
		public string name;

		public Color color = Color.white;

		public float thresholdMax;

		private float thresholdMin = -1f;

		public void SetMinThreshold(float val)
		{
			thresholdMin = val;
		}

		public float GetMinThreshold()
		{
			if (thresholdMin < 0f)
			{
				GTDev.LogError("Tier min threshold not initialized. Can only be used at runtime.");
			}
			return thresholdMin;
		}
	}

	[Serializable]
	public class RankedProgressionSubTier : RankedProgressionTierBase
	{
		public Sprite icon;
	}

	[Serializable]
	public class RankedProgressionTier : RankedProgressionTierBase
	{
		public List<RankedProgressionSubTier> subTiers = new List<RankedProgressionSubTier>();

		public void InsertSubTierAt(int idx, float tierMin)
		{
			RankedProgressionSubTier item = new RankedProgressionSubTier
			{
				name = "NewTier"
			};
			subTiers.Insert(idx, item);
			EnforceSubTierValidity(tierMin);
		}

		public void EnforceSubTierValidity(float thresholdMin)
		{
			float num = (((thresholdMax == 0f) ? 4000f : thresholdMax) - thresholdMin) / (float)subTiers.Count;
			for (int i = 0; i < subTiers.Count - 1; i++)
			{
				float num2 = thresholdMin + (float)(i + 1) * num;
				num2 = Mathf.Round(num2 / 10f);
				subTiers[i].thresholdMax = num2 * 10f;
			}
		}
	}

	public static RankedProgressionManager Instance;

	public const float DEFAULT_ELO = 100f;

	public const float MIN_ELO = 100f;

	public const float MAX_ELO = 4000f;

	public const float MAJOR_TIER_MIN_RANGE = 200f;

	public const float SUB_TIER_MIN_RANGE = 20f;

	public static string RANKED_ELO_KEY = "RankedElo";

	public static string RANKED_PROGRESSION_GRACE_PERIOD_KEY = "RankedProgGracePeriod";

	public static string RANKED_ELO_PC_KEY = "RankedEloPC";

	public static string RANKED_PROGRESSION_GRACE_PERIOD_PC_KEY = "RankedProgGracePeriodPC";

	private RankedMultiplayerStatisticFloat EloScorePC;

	private RankedMultiplayerStatisticFloat EloScoreQuest;

	private RankedMultiplayerStatisticInt NewTierGracePeriodIdxPC;

	private RankedMultiplayerStatisticInt NewTierGracePeriodIdxQuest;

	private GorillaTagCompetitiveServerApi.RankedModePlayerProgressionData ProgressionData;

	[SerializeField]
	private List<RankedProgressionTier> majorTiers = new List<RankedProgressionTier>();

	[SerializeField]
	private int newTierGracePeriod = 3;

	public float MaxEloConstant = 90f;

	private RankedProgressionEvent ProgressionEvent;

	public Action<int, float, int> OnPlayerEloAcquired;

	[Space]
	[ContextMenuItem("Set ELO", "DebugSetELO")]
	public int debugEloPoints = 100;

	public int MaxRank { get; private set; }

	public float LowTierThreshold { get; set; }

	public float HighTierThreshold { get; set; }

	public List<RankedProgressionTier> MajorTiers
	{
		get
		{
			return majorTiers;
		}
		private set
		{
		}
	}

	public float CompetitiveQueueEloFloor => LowTierThreshold;

	private void DebugSetELO()
	{
	}

	[ContextMenu("Reset ELO")]
	private void DebugResetELO()
	{
	}

	private void Awake()
	{
		if ((bool)Instance)
		{
			GTDev.LogError("Duplicate RankedProgressionManager detected. Destroying self.", base.gameObject);
			UnityEngine.Object.Destroy(this);
		}
		else
		{
			Instance = this;
		}
	}

	private void Start()
	{
		if (majorTiers.Count < 3)
		{
			GTDev.LogWarning("At least 3 MMR tiers must be defined.");
			return;
		}
		GameMode.OnStartGameMode += OnJoinedRoom;
		RoomSystem.PlayerJoinedEvent += new Action<NetPlayer>(OnPlayerJoined);
		float minThreshold = 100f;
		int num = 0;
		for (int i = 0; i < majorTiers.Count; i++)
		{
			majorTiers[i].SetMinThreshold((i == 0) ? 100f : majorTiers[i - 1].thresholdMax);
			for (int j = 0; j < majorTiers[i].subTiers.Count; j++)
			{
				num++;
				majorTiers[i].subTiers[j].SetMinThreshold(minThreshold);
				minThreshold = majorTiers[i].subTiers[j].thresholdMax;
			}
		}
		MaxRank = num - 1;
		LowTierThreshold = majorTiers[0].thresholdMax;
		List<RankedProgressionTier> list = majorTiers;
		HighTierThreshold = list[list.Count - 1].GetMinThreshold();
		EloScorePC = new RankedMultiplayerStatisticFloat(RANKED_ELO_PC_KEY, 100f, 100f, 4000f, RankedMultiplayerStatistic.SerializationType.PlayerPrefs);
		EloScoreQuest = new RankedMultiplayerStatisticFloat(RANKED_ELO_KEY, 100f, 100f, 4000f, RankedMultiplayerStatistic.SerializationType.PlayerPrefs);
		NewTierGracePeriodIdxPC = new RankedMultiplayerStatisticInt(RANKED_PROGRESSION_GRACE_PERIOD_KEY, 0, -1, int.MaxValue, RankedMultiplayerStatistic.SerializationType.PlayerPrefs);
		NewTierGracePeriodIdxQuest = new RankedMultiplayerStatisticInt(RANKED_PROGRESSION_GRACE_PERIOD_PC_KEY, 0, -1, int.MaxValue, RankedMultiplayerStatistic.SerializationType.PlayerPrefs);
	}

	private void OnDestroy()
	{
		GameMode.OnStartGameMode += OnJoinedRoom;
		RoomSystem.PlayerJoinedEvent -= new Action<NetPlayer>(OnPlayerJoined);
	}

	public void RequestUnlockCompetitiveQueue(bool unlock)
	{
		GorillaTagCompetitiveServerApi.Instance.RequestUnlockCompetitiveQueue(unlock, delegate
		{
			AcquireLocalPlayerRankInformation();
		});
	}

	public IEnumerator LoadStatsWhenReady()
	{
		yield return new WaitUntil(() => NetworkSystem.Instance.LocalPlayer.UserId != null);
		if (HasUnlockedCompetitiveQueue())
		{
			RequestUnlockCompetitiveQueue(unlock: true);
		}
		else
		{
			AcquireLocalPlayerRankInformation();
		}
	}

	private void OnJoinedRoom(GameModeType newGameModeType)
	{
		if (newGameModeType == GameModeType.InfectionCompetitive)
		{
			AcquireRoomRankInformation(includeLocalPlayer: false);
		}
	}

	private void OnPlayerJoined(NetPlayer player)
	{
		if (GorillaGameManager.instance != null && GorillaGameManager.instance.GameType() == GameModeType.InfectionCompetitive)
		{
			AcquireSinglePlayerRankInformation(player);
		}
	}

	private void AcquireLocalPlayerRankInformation()
	{
		List<string> list = new List<string>();
		list.Add(NetworkSystem.Instance.LocalPlayer.UserId);
		GorillaTagCompetitiveServerApi.Instance.RequestGetRankInformation(list, OnLocalPlayerRankedInformationAcquired);
	}

	private void AcquireSinglePlayerRankInformation(NetPlayer player)
	{
		if (player != null)
		{
			List<string> list = new List<string>();
			list.Add(player.UserId);
			GorillaTagCompetitiveServerApi.Instance.RequestGetRankInformation(list, OnPlayersRankedInformationAcquired);
		}
	}

	public void AcquireRoomRankInformation(bool includeLocalPlayer = true)
	{
		List<string> list = new List<string>();
		foreach (NetPlayer item in RoomSystem.PlayersInRoom)
		{
			if (includeLocalPlayer || !item.IsLocal)
			{
				list.Add(item.UserId);
			}
		}
		if (list.Count > 0)
		{
			GorillaTagCompetitiveServerApi.Instance.RequestGetRankInformation(list, OnPlayersRankedInformationAcquired);
		}
	}

	private void OnPlayersRankedInformationAcquired(GorillaTagCompetitiveServerApi.RankedModeProgressionData rankedModeProgressionData)
	{
		foreach (GorillaTagCompetitiveServerApi.RankedModePlayerProgressionData playerDatum in rankedModeProgressionData.playerData)
		{
			if (playerDatum == null || playerDatum.platformData == null || playerDatum.platformData.Length < 2)
			{
				continue;
			}
			int num = -1;
			NetPlayer[] allNetPlayers = NetworkSystem.Instance.AllNetPlayers;
			foreach (NetPlayer netPlayer in allNetPlayers)
			{
				if (netPlayer.UserId == playerDatum.playfabID)
				{
					num = netPlayer.ActorNumber;
					break;
				}
			}
			if (num < 0)
			{
				continue;
			}
			GorillaTagCompetitiveServerApi.RankedModeProgressionPlatformData rankedModeProgressionPlatformData = playerDatum.platformData[1];
			GorillaTagCompetitiveServerApi.RankedModeProgressionPlatformData rankedModeProgressionPlatformData2 = playerDatum.platformData[0];
			GorillaTagCompetitiveServerApi.RankedModeProgressionPlatformData rankedModeProgressionPlatformData3 = rankedModeProgressionPlatformData;
			rankedModeProgressionPlatformData3 = rankedModeProgressionPlatformData2;
			int rankFromTiers = Instance.GetRankFromTiers(rankedModeProgressionPlatformData3.majorTier, rankedModeProgressionPlatformData3.minorTier);
			OnPlayerEloAcquired?.Invoke(num, rankedModeProgressionPlatformData3.elo, rankFromTiers);
			if (num == NetworkSystem.Instance.LocalPlayerID)
			{
				SetLocalProgressionData(playerDatum);
			}
			if (VRRigCache.Instance.TryGetVrrig(num, out var playerRig))
			{
				VRRig rig = playerRig.Rig;
				if (rig != null)
				{
					int rankFromTiers2 = GetRankFromTiers(rankedModeProgressionPlatformData.majorTier, rankedModeProgressionPlatformData.minorTier);
					int rankFromTiers3 = Instance.GetRankFromTiers(rankedModeProgressionPlatformData2.majorTier, rankedModeProgressionPlatformData2.minorTier);
					rig.SetRankedInfo(rankedModeProgressionPlatformData3.elo, rankFromTiers2, rankFromTiers3, broadcastToOtherClients: false);
				}
			}
		}
	}

	private void OnLocalPlayerRankedInformationAcquired(GorillaTagCompetitiveServerApi.RankedModeProgressionData rankedModeProgressionData)
	{
		if (rankedModeProgressionData.playerData.Count > 0)
		{
			SetLocalProgressionData(rankedModeProgressionData.playerData[0]);
			float eloScore = GetEloScore();
			int progressionRankIndexQuest = GetProgressionRankIndexQuest();
			int progressionRankIndexPC = GetProgressionRankIndexPC();
			int num = progressionRankIndexQuest;
			num = progressionRankIndexPC;
			HandlePlayerRankedInfoReceived(NetworkSystem.Instance.LocalPlayer.ActorNumber, eloScore, num);
			VRRig.LocalRig.SetRankedInfo(eloScore, progressionRankIndexQuest, progressionRankIndexPC);
		}
	}

	public bool AreValuesValid(float elo, int questTier, int pcTier)
	{
		if (elo >= 100f && elo <= 4000f && questTier >= 0 && questTier <= MaxRank && pcTier >= 0 && pcTier <= MaxRank)
		{
			return true;
		}
		return false;
	}

	public void HandlePlayerRankedInfoReceived(int actorNum, float elo, int tier)
	{
		OnPlayerEloAcquired?.Invoke(actorNum, elo, tier);
	}

	public void SetLocalProgressionData(GorillaTagCompetitiveServerApi.RankedModePlayerProgressionData data)
	{
		ProgressionData = data;
	}

	public void LoadStats()
	{
		StartCoroutine(LoadStatsWhenReady());
	}

	public float GetEloScore()
	{
		return GetEloScorePC();
	}

	public void SetEloScore(float val)
	{
		GorillaTagCompetitiveServerApi.Instance.RequestSetEloValue(val, delegate
		{
			AcquireLocalPlayerRankInformation();
		});
	}

	public float GetEloScorePC()
	{
		if (ProgressionData == null || ProgressionData.platformData == null || ProgressionData.platformData.Length < 2)
		{
			return 100f;
		}
		return ProgressionData.platformData[0].elo;
	}

	public float GetEloScoreQuest()
	{
		if (ProgressionData == null || ProgressionData.platformData == null || ProgressionData.platformData.Length < 2)
		{
			return 100f;
		}
		return ProgressionData.platformData[1].elo;
	}

	private int GetNewTierGracePeriodIdx()
	{
		return NewTierGracePeriodIdxPC;
	}

	private void SetNewTierGracePeriodIdx(int val)
	{
		NewTierGracePeriodIdxPC.Set(val);
	}

	private void IncrementNewTierGracePeriodIdx()
	{
		NewTierGracePeriodIdxPC.Increment();
	}

	public bool TryGetProgressionSubTier(out RankedProgressionSubTier subTier, out int index)
	{
		subTier = null;
		index = -1;
		return TryGetProgressionSubTier(GetEloScore(), out subTier, out index);
	}

	public bool TryGetProgressionSubTier(float elo, out RankedProgressionSubTier subTier, out int index)
	{
		int num = 0;
		subTier = null;
		index = -1;
		for (int i = 0; i < majorTiers.Count; i++)
		{
			float num2 = ((i < majorTiers.Count - 1) ? majorTiers[i].thresholdMax : 4000.1f);
			if (elo < num2)
			{
				int num3 = 0;
				while (num3 < majorTiers[i].subTiers.Count)
				{
					float num4 = ((num3 < majorTiers[i].subTiers.Count - 1) ? majorTiers[i].subTiers[num3].thresholdMax : num2);
					if (elo < num4)
					{
						subTier = majorTiers[i].subTiers[num3];
						index = num;
						return true;
					}
					num3++;
					num++;
				}
			}
			else
			{
				num += majorTiers[i].subTiers.Count;
			}
		}
		return false;
	}

	private RankedProgressionTier GetProgressionMajorTierBySubTierIndex(int idx)
	{
		int num = 0;
		for (int i = 0; i < majorTiers.Count; i++)
		{
			int num2 = 0;
			while (num2 < majorTiers[i].subTiers.Count)
			{
				if (num == idx)
				{
					return majorTiers[i];
				}
				num2++;
				num++;
			}
		}
		return null;
	}

	private RankedProgressionSubTier GetProgressionSubTierByIndex(int idx)
	{
		int num = 0;
		for (int i = 0; i < majorTiers.Count; i++)
		{
			int num2 = 0;
			while (num2 < majorTiers[i].subTiers.Count)
			{
				if (num == idx)
				{
					return majorTiers[i].subTiers[num2];
				}
				num2++;
				num++;
			}
		}
		return null;
	}

	private RankedProgressionSubTier GetNextProgressionSubTierByIndex(int idx)
	{
		RankedProgressionSubTier progressionSubTierByIndex = GetProgressionSubTierByIndex(idx + 1);
		if (progressionSubTierByIndex != null)
		{
			return progressionSubTierByIndex;
		}
		return GetProgressionSubTierByIndex(idx);
	}

	private RankedProgressionSubTier GetPrevProgressionSubTierByIndex(int idx)
	{
		if (idx > 0)
		{
			RankedProgressionSubTier progressionSubTierByIndex = GetProgressionSubTierByIndex(idx - 1);
			if (progressionSubTierByIndex != null)
			{
				return progressionSubTierByIndex;
			}
		}
		return GetProgressionSubTierByIndex(idx);
	}

	public string GetProgressionRankName()
	{
		return GetProgressionRankName(GetEloScore());
	}

	public string GetProgressionRankName(float elo)
	{
		if (TryGetProgressionSubTier(elo, out var subTier, out var _))
		{
			return subTier.name;
		}
		return string.Empty;
	}

	public string GetNextProgressionRankName(int subTierIdx)
	{
		return GetNextProgressionSubTierByIndex(subTierIdx)?.name;
	}

	public string GetPrevProgressionRankName(int subTierIdx)
	{
		return GetPrevProgressionSubTierByIndex(subTierIdx)?.name;
	}

	public int GetProgressionRankIndex()
	{
		return GetProgressionRankIndexPC();
	}

	public RankedProgressionSubTier GetProgressionSubTier()
	{
		return GetProgressionSubTierByIndex(GetProgressionRankIndex());
	}

	public int GetProgressionRankIndexQuest()
	{
		if (ProgressionData == null || ProgressionData.platformData == null || ProgressionData.platformData.Length < 2)
		{
			return 0;
		}
		GorillaTagCompetitiveServerApi.RankedModeProgressionPlatformData rankedModeProgressionPlatformData = ProgressionData.platformData[1];
		return GetRankFromTiers(rankedModeProgressionPlatformData.majorTier, rankedModeProgressionPlatformData.minorTier);
	}

	public int GetProgressionRankIndexPC()
	{
		if (ProgressionData == null || ProgressionData.platformData == null || ProgressionData.platformData.Length < 2)
		{
			return 0;
		}
		GorillaTagCompetitiveServerApi.RankedModeProgressionPlatformData rankedModeProgressionPlatformData = ProgressionData.platformData[0];
		return GetRankFromTiers(rankedModeProgressionPlatformData.majorTier, rankedModeProgressionPlatformData.minorTier);
	}

	public int GetRankFromTiers(int majorTier, int minorTier)
	{
		int num = 0;
		for (int i = 0; i < majorTiers.Count; i++)
		{
			for (int j = 0; j < majorTiers[i].subTiers.Count; j++)
			{
				if (i == majorTier && j == minorTier)
				{
					return num;
				}
				num++;
			}
		}
		return -1;
	}

	public int GetProgressionRankIndex(float elo)
	{
		if (TryGetProgressionSubTier(elo, out var _, out var index))
		{
			return index;
		}
		return -1;
	}

	public float GetProgressionRankProgress()
	{
		return GetProgressionRankProgressPC();
	}

	public float GetProgressionRankProgressQuest()
	{
		if (ProgressionData == null || ProgressionData.platformData == null || ProgressionData.platformData.Length < 2)
		{
			return 0f;
		}
		return ProgressionData.platformData[1].rankProgress;
	}

	public float GetProgressionRankProgressPC()
	{
		if (ProgressionData == null || ProgressionData.platformData == null || ProgressionData.platformData.Length < 2)
		{
			return 0f;
		}
		return ProgressionData.platformData[0].rankProgress;
	}

	public int ClampProgressionRankIndex(int subTierIdx)
	{
		if (subTierIdx < 0)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < majorTiers.Count; i++)
		{
			int num2 = 0;
			while (num2 < majorTiers[i].subTiers.Count)
			{
				if (num == subTierIdx)
				{
					return subTierIdx;
				}
				num2++;
				num++;
			}
		}
		return num - 1;
	}

	public Sprite GetProgressionRankIcon()
	{
		if (ProgressionData == null || ProgressionData.platformData == null || ProgressionData.platformData.Length < 2)
		{
			return null;
		}
		int num = 0;
		int num2 = 0;
		num = ((ProgressionData != null) ? ProgressionData.platformData[0].minorTier : 0);
		num2 = ((ProgressionData != null) ? ProgressionData.platformData[0].majorTier : 0);
		return majorTiers[num2].subTiers[num]?.icon;
	}

	public string GetRankedProgressionTierName()
	{
		if (ProgressionData == null || ProgressionData.platformData == null || ProgressionData.platformData.Length < 2)
		{
			return "None";
		}
		int num = 0;
		int num2 = 0;
		num = ProgressionData.platformData[0].minorTier;
		num2 = ProgressionData.platformData[0].majorTier;
		RankedProgressionSubTier rankedProgressionSubTier = majorTiers[num2].subTiers[num];
		if (rankedProgressionSubTier != null)
		{
			return rankedProgressionSubTier.name;
		}
		return "None";
	}

	public Sprite GetProgressionRankIcon(float elo)
	{
		if (TryGetProgressionSubTier(elo, out var subTier, out var _))
		{
			return subTier.icon;
		}
		return null;
	}

	public Sprite GetProgressionRankIcon(int subTierIdx)
	{
		return GetProgressionSubTierByIndex(subTierIdx)?.icon;
	}

	public Sprite GetNextProgressionRankIcon(int subTierIdx)
	{
		return GetNextProgressionSubTierByIndex(subTierIdx)?.icon;
	}

	public Sprite GetPrevProgressionRankIcon(int subTierIdx)
	{
		return GetPrevProgressionSubTierByIndex(subTierIdx)?.icon;
	}

	public float GetCurrentELO()
	{
		return GetEloScore();
	}

	public void GetSubtierRankThresholds(int subTierIdx, out float minThreshold, out float maxThreshold)
	{
		minThreshold = 0f;
		maxThreshold = 1f;
		RankedProgressionSubTier progressionSubTierByIndex = GetProgressionSubTierByIndex(subTierIdx);
		if (progressionSubTierByIndex == null)
		{
			return;
		}
		maxThreshold = progressionSubTierByIndex.thresholdMax;
		if (maxThreshold <= 0f)
		{
			RankedProgressionTier progressionMajorTierBySubTierIndex = GetProgressionMajorTierBySubTierIndex(subTierIdx);
			if (progressionMajorTierBySubTierIndex != null)
			{
				maxThreshold = progressionMajorTierBySubTierIndex.thresholdMax;
				if (maxThreshold <= 0f)
				{
					maxThreshold = 4000f;
				}
			}
		}
		minThreshold = progressionSubTierByIndex.GetMinThreshold();
		if (!(minThreshold <= 0f))
		{
			return;
		}
		RankedProgressionTier progressionMajorTierBySubTierIndex2 = GetProgressionMajorTierBySubTierIndex(subTierIdx);
		if (progressionMajorTierBySubTierIndex2 != null)
		{
			minThreshold = progressionMajorTierBySubTierIndex2.GetMinThreshold();
			if (minThreshold <= 0f)
			{
				minThreshold = 100f;
			}
		}
	}

	public static float GetEloWinProbability(float ratingPlayer1, float ratingPlayer2)
	{
		return 1f / (1f + Mathf.Pow(10f, (ratingPlayer1 - ratingPlayer2) / 400f));
	}

	public static float UpdateEloScore(float eloScore, float expectedResult, float actualResult, float k)
	{
		return Mathf.Clamp(eloScore + k * (actualResult - expectedResult), 100f, 4000f);
	}

	public ERankedMatchmakingTier GetRankedMatchmakingTier()
	{
		if (ProgressionData == null || ProgressionData.platformData == null || ProgressionData.platformData.Length < 2)
		{
			return ERankedMatchmakingTier.Low;
		}
		return (ERankedMatchmakingTier)ProgressionData.platformData[0].majorTier;
	}

	private bool HasUnlockedCompetitiveQueue()
	{
		return GorillaComputer.instance.allowedInCompetitive;
	}
}

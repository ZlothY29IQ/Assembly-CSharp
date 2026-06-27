using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GorillaGameModes;
using Newtonsoft.Json;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class SIProgression : MonoBehaviour, IGorillaSliceableSimple, GorillaQuestManager
{
	public struct SINode
	{
		public string id;

		public bool unlocked;

		public Dictionary<SIResource.ResourceType, int> costs;

		public List<SINode> parents;

		public SIUpgradeType upgradeType;
	}

	[Serializable]
	public class SIQuestsList
	{
		public List<RotatingQuest> quests;

		public RotatingQuest GetQuestById(int questID)
		{
			foreach (RotatingQuest quest in quests)
			{
				if (quest.questID == questID)
				{
					return quest.disable ? null : quest;
				}
			}
			return null;
		}
	}

	[Serializable]
	public struct SIProgressionResourceCap
	{
		public SIResource.ResourceType resourceType;

		public int resourceMax;
	}

	[SerializeField]
	private SITechTreeSO techTreeSO;

	[SerializeField]
	private int perCategoryQuestLimit = 1;

	public Action OnTreeReady;

	public Action OnInventoryReady;

	public Action<SIUpgradeType> OnNodeUnlocked;

	public bool ClientReady;

	private static Dictionary<SIResource.ResourceType, string> _resourceToString;

	private const string TREE_NAME = "SI_Gadgets";

	private Dictionary<SIUpgradeType, SINode> siNodes;

	internal bool _treeReady;

	private bool _inventoryReady;

	public Dictionary<SITechTreePageId, int> heldOrSnappedByGadgetPageType = new Dictionary<SITechTreePageId, int>();

	public int heldOrSnappedOwnGadgets;

	public int heldOrSnappedOthersGadgets;

	public float timeTelemetryLastChecked;

	public float lastTelemetrySent;

	private float telemetryCooldown = 600f;

	private float totalPlayTime;

	private float roomPlayTime;

	private float intervalPlayTime;

	[NonSerialized]
	public float activeTerminalTimeTotal;

	[NonSerialized]
	public float activeTerminalTimeInterval;

	private Dictionary<SITechTreePageId, float> timeUsingGadgetTypeTotal = new Dictionary<SITechTreePageId, float>();

	private Dictionary<SITechTreePageId, float> timeUsingGadgetTypeInterval = new Dictionary<SITechTreePageId, float>();

	private float timeUsingOthersGadgetsTotal;

	private float timeUsingOthersGadgetsInterval;

	private float timeUsingOwnGadgetsTotal;

	private float timeUsingOwnGadgetsInterval;

	private Dictionary<SITechTreePageId, int> tagsUsingGadgetTypeTotal = new Dictionary<SITechTreePageId, int>();

	private Dictionary<SITechTreePageId, int> tagsUsingGadgetTypeInterval = new Dictionary<SITechTreePageId, int>();

	private int tagsHoldingOthersGadgetTotal;

	private int tagsHoldingOthersGadgetInterval;

	private int tagsHoldingOwnGadgetTotal;

	private int tagsHoldingOwnGadgetInterval;

	private Dictionary<SIResource.ResourceType, int> resourcesCollectedTotal = new Dictionary<SIResource.ResourceType, int>();

	private Dictionary<SIResource.ResourceType, int> resourcesCollectedInterval = new Dictionary<SIResource.ResourceType, int>();

	private int roundsPlayedTotal;

	private int roundsPlayedInterval;

	private SINode emptyNode;

	public SIQuestsList questSourceList;

	private const int STARTING_STASHED_QUESTS = 0;

	private const int STARTING_STASHED_BONUS_POINTS = 0;

	public const int SHARED_QUEST_TURNINS_FOR_POINT = 4;

	public const int NEW_QUESTS_PER_DAY = 3;

	public const int NEW_BONUS_POINTS_PER_DAY = 1;

	public const int MAX_STASHED_QUESTS = 6;

	public const int MAX_STASHED_BONUS_POINTS = 2;

	public const int MAX_RESOURCE_COUNT = 30;

	private const int ACTIVE_QUEST_COUNT = 3;

	private const string kLocalQuestPath = "TestingSuperInfectionQuests";

	private const string kVersion = "v1_";

	private const string kLastQuestGrantTime = "v1_SIProgression:lastSharedGrantTime";

	private const string kBonusProgress = "v1_SIProgression:bonusProgress";

	private const string kDailyQuestId = "v1_Rotating_Quest_Daily_ID_Key";

	private const string kDailyQuestProgress = "v1_Rotating_Quest_Daily_Progress_Key";

	private const string kStashedQuests = "v1_SIProgression:stashedQuests";

	private const string kStashedBonusPoints = "v1_SIProgression:stashedBonusPoints";

	private const string kTechTree = "v1_SITechTree:";

	private const string kLimitedDeposit = "v1_SIResource:LimitedDeposit:";

	private const string kTechPoints = "v1_SIResource:techPoints";

	private const string kStrangeWood = "v1_SIResource:strangeWood";

	private const string kWeirdGear = "v1_SIResource:weirdGear";

	private const string kVibratingSpring = "v1_SIResource:vibratingSpring";

	private const string kBouncySand = "v1_SIResource:bouncySand";

	private const string kFloppyMetal = "v1_SIResource:floppyMetal";

	private const string kStartingPackageGranted = "v1_SIProgression:startingPackageGranted";

	public TimeSpan CROSSOVER_TIME_OF_DAY = new TimeSpan(1, 0, 0);

	public DateTime lastQuestGrantTime;

	public int stashedQuests;

	public int completedQuests;

	public int stashedBonusPoints;

	public int completedBonusPoints;

	public int bonusProgress;

	public int questGrantRefreshCooldown = 28800;

	public Dictionary<SIResource.ResourceType, int> resourceDict;

	public int[] limitedDepositTimeArray;

	public bool[][] unlockedTechTreeData;

	[SerializeField]
	private int[] activeQuestIds = new int[3];

	[SerializeField]
	private int[] activeQuestProgresses = new int[3];

	[SerializeField]
	private QuestCategory[] activeQuestCategories = new QuestCategory[3];

	private bool dailyLimitedTurnedIn;

	public SIProgressionResourceCap[] resourceCaps;

	public int[] resourceCapsArray;

	private DateTime lastQuestGrantTimeDiff;

	private int stashedQuestsDiff;

	private int stashedBonusPointsDiff;

	private int bonusProgressDiff;

	private int[] resourceArrayDiff;

	private int[] limitedDepositTimeDiff;

	private bool[][] unlockedTechTreeDataDiff;

	private int[] activeQuestIdsDiff;

	private int[] activeQuestProgressesDiff;

	private bool questsInitialized;

	private bool _startingPackageGranted;

	private float lastStartingPackageAttemptStarted;

	private int startingPackageBackupAttempts;

	private const int STARTING_PACKAGE_MAX_ATTEMPTS = 10;

	private bool[] redeemingQuestInProgress = new bool[3];

	private float lastDisconnectTelemetrySent;

	private float minDisconnectTelemetryCooldown = 60f;

	public static SIProgression Instance { get; private set; }

	public int[] ActiveQuestIds => activeQuestIds;

	public int[] ActiveQuestProgresses => activeQuestProgresses;

	public bool DailyLimitedTurnedIn => dailyLimitedTurnedIn;

	public event Action OnClientReady;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		emptyNode = default(SINode);
		InitResourceToStringDictionary();
		resourceCapsArray = Enumerable.Repeat(int.MaxValue, 6).ToArray();
		for (int i = 0; i < resourceCaps.Length; i++)
		{
			resourceCapsArray[(int)resourceCaps[i].resourceType] = resourceCaps[i].resourceMax;
		}
		foreach (SITechTreePageId value in Enum.GetValues(typeof(SITechTreePageId)))
		{
			heldOrSnappedByGadgetPageType.Add(value, 0);
		}
		EnsureInitialized();
		InitializeQuests();
		ResetTelemetryIntervalData();
		LoadSavedTelemetryData();
	}

	public void OnEnable()
	{
		if (ProgressionManager.Instance != null)
		{
			ProgressionManager.Instance.OnTreeUpdated += HandleTreeUpdated;
			ProgressionManager.Instance.OnInventoryUpdated += HandleInventoryUpdated;
			ProgressionManager.Instance.OnNodeUnlocked += HandleNodeUnlocked;
			ProgressionManager.Instance.RefreshProgressionTree();
			ProgressionManager.Instance.RefreshUserInventory();
		}
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void OnDisable()
	{
		if (ProgressionManager.Instance != null)
		{
			ProgressionManager.Instance.OnTreeUpdated -= HandleTreeUpdated;
			ProgressionManager.Instance.OnInventoryUpdated -= HandleInventoryUpdated;
			ProgressionManager.Instance.OnNodeUnlocked -= HandleNodeUnlocked;
		}
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public static string GetResourceString(SIResource.ResourceType resourceType)
	{
		if (_resourceToString == null)
		{
			InitResourceToStringDictionary();
		}
		return _resourceToString[resourceType];
	}

	private static void InitResourceToStringDictionary()
	{
		_resourceToString = new Dictionary<SIResource.ResourceType, string>();
		_resourceToString[SIResource.ResourceType.TechPoint] = "SI_TechPoints";
		_resourceToString[SIResource.ResourceType.StrangeWood] = "SI_StrangeWood";
		_resourceToString[SIResource.ResourceType.WeirdGear] = "SI_WeirdGear";
		_resourceToString[SIResource.ResourceType.VibratingSpring] = "SI_VibratingSpring";
		_resourceToString[SIResource.ResourceType.BouncySand] = "SI_BouncySand";
		_resourceToString[SIResource.ResourceType.FloppyMetal] = "SI_FloppyMetal";
	}

	public void Init()
	{
		SIPlayer.LocalPlayer.SetProgressionLocal();
		if (ProgressionManager.Instance != null)
		{
			ProgressionManager.Instance.RefreshProgressionTree();
			ProgressionManager.Instance.RefreshUserInventory();
		}
		ClearAllQuestEventListeners();
		SetupAllQuestEventListeners();
	}

	public void EnsureInitialized()
	{
		if (techTreeSO != null)
		{
			techTreeSO.EnsureInitialized();
		}
		if (SIPlayer.progressionSO == null)
		{
			SIPlayer.progressionSO = techTreeSO;
		}
		int num = 6;
		if (resourceDict == null || resourceDict.Count != num)
		{
			resourceDict = new Dictionary<SIResource.ResourceType, int>();
			resourceDict[SIResource.ResourceType.TechPoint] = 0;
			resourceDict[SIResource.ResourceType.StrangeWood] = 0;
			resourceDict[SIResource.ResourceType.VibratingSpring] = 0;
			resourceDict[SIResource.ResourceType.BouncySand] = 0;
			resourceDict[SIResource.ResourceType.FloppyMetal] = 0;
			resourceDict[SIResource.ResourceType.WeirdGear] = 0;
		}
		int num2 = 2;
		if (limitedDepositTimeArray == null || limitedDepositTimeArray.Length != num2)
		{
			limitedDepositTimeArray = new int[num2];
		}
		int treePageCount = SIPlayer.progressionSO.TreePageCount;
		if (unlockedTechTreeData == null || unlockedTechTreeData.Length != treePageCount)
		{
			unlockedTechTreeData = new bool[treePageCount][];
		}
		for (int i = 0; i < treePageCount; i++)
		{
			int num3 = SIPlayer.progressionSO.TreeNodeCounts[i];
			if (unlockedTechTreeData[i] == null || unlockedTechTreeData[i].Length != num3)
			{
				unlockedTechTreeData[i] = new bool[num3];
			}
		}
		if (activeQuestIds == null || activeQuestIds.Length != 3)
		{
			activeQuestIds = new int[3];
		}
		if (activeQuestProgresses == null || activeQuestProgresses.Length != 3)
		{
			activeQuestProgresses = new int[3];
		}
		CopySaveDataToDiff();
	}

	private void ApplyServerQuestsStatus(ProgressionManager.UserQuestsStatusResponse userQuestsStatus)
	{
		if (userQuestsStatus != null)
		{
			stashedQuests = userQuestsStatus.TodayClaimableQuests;
			stashedBonusPoints = userQuestsStatus.TodayClaimableBonus;
			dailyLimitedTurnedIn = userQuestsStatus.TodayClaimableIdol <= 0;
			lastQuestGrantTime = DateTime.UtcNow;
			RefreshActiveQuests();
			SIPlayer.SetAndBroadcastProgression();
			if (!questsInitialized)
			{
				questsInitialized = true;
				ClientReady = true;
				this.OnClientReady?.Invoke();
			}
		}
	}

	public int GetCurrencyAmount(SIResource.ResourceType currencyType)
	{
		if (!ProgressionManager.Instance.GetInventoryItem(_resourceToString[currencyType], out var item))
		{
			return 0;
		}
		return item.Quantity;
	}

	public bool IsNodeUnlocked(SIUpgradeType upgradeType)
	{
		if (siNodes != null)
		{
			if (siNodes.TryGetValue(upgradeType, out var value))
			{
				return value.unlocked;
			}
			return false;
		}
		UserHydratedProgressionTreeResponse userHydratedProgressionTreeResponse = ProgressionManager.Instance?.GetTree("SI_Gadgets");
		if (userHydratedProgressionTreeResponse != null)
		{
			foreach (UserHydratedNodeDefinition node in userHydratedProgressionTreeResponse.Nodes)
			{
				if (node.name == upgradeType.ToString())
				{
					return node.unlocked;
				}
			}
		}
		return false;
	}

	public void UnlockNode(SIUpgradeType upgradeType)
	{
		if (_treeReady && _inventoryReady)
		{
			UserHydratedProgressionTreeResponse userHydratedProgressionTreeResponse = ProgressionManager.Instance?.GetTree("SI_Gadgets");
			if (siNodes != null && siNodes.TryGetValue(upgradeType, out var value) && !value.unlocked)
			{
				ProgressionManager.Instance.UnlockNode(userHydratedProgressionTreeResponse.Tree.id, value.id);
			}
		}
	}

	private void HandleTreeUpdated()
	{
		_treeReady = true;
		UpdateTree();
		UpdateUnlockOnPlayer();
		if (!_startingPackageGranted)
		{
			if (IsNodeUnlocked(SIUpgradeType.Initialize))
			{
				_startingPackageGranted = true;
			}
			else
			{
				startingPackageBackupAttempts++;
				if (startingPackageBackupAttempts > 10)
				{
					_startingPackageGranted = true;
				}
				else
				{
					StartCoroutine(TryClaimNewPlayerPackage());
				}
			}
		}
		GTDev.Log("[SIProgression] Updating local tech tree costs from remote tree");
		foreach (GraphNode<SITechTreeNode> allNode in techTreeSO.AllNodes)
		{
			SITechTreeNode value = allNode.Value;
			if (siNodes.TryGetValue(value.upgradeType, out var value2))
			{
				SIResource.ResourceCost[] array = SIResource.GenerateCostsFrom(value2.costs);
				if (array.IsValid_AllowZero() && !SIResource.CostsAreEqual(value.nodeCost, array))
				{
					GTDev.Log($"[SIProgression] Changing {value.upgradeType} costs from {SIResource.PrintCost(value.nodeCost)} to {SIResource.PrintCost(array)}");
					value.nodeCost = array;
				}
			}
		}
		OnTreeReady?.Invoke();
	}

	private void HandleInventoryUpdated()
	{
		_inventoryReady = true;
		UpdateCurrencyOnPlayer();
		OnInventoryReady?.Invoke();
	}

	private IEnumerator TryClaimNewPlayerPackage()
	{
		yield return new WaitForSecondsRealtime(Mathf.Pow(startingPackageBackupAttempts, 2f));
		if (!_startingPackageGranted)
		{
			TryUnlock(SIUpgradeType.Initialize);
		}
	}

	private void HandleNodeUnlocked(string treeId, string nodeId)
	{
		UpdateTree();
		UpdateUnlockOnPlayer();
		SINode nodeFromID = GetNodeFromID(nodeId);
		if (!string.IsNullOrEmpty(nodeFromID.id))
		{
			OnNodeUnlocked?.Invoke(nodeFromID.upgradeType);
		}
	}

	private void UpdateTree()
	{
		UserHydratedProgressionTreeResponse obj = ProgressionManager.Instance?.GetTree("SI_Gadgets");
		siNodes = new Dictionary<SIUpgradeType, SINode>();
		foreach (UserHydratedNodeDefinition node in obj.Nodes)
		{
			if (!Enum.TryParse<SIUpgradeType>(node.name, out var result))
			{
				result = SIUpgradeType.InvalidNode;
			}
			Dictionary<SIResource.ResourceType, int> dictionary = new Dictionary<SIResource.ResourceType, int>();
			if (node.cost?.items != null)
			{
				foreach (KeyValuePair<string, MothershipHydratedInventoryChange> item in node.cost.items)
				{
					foreach (KeyValuePair<SIResource.ResourceType, string> item2 in _resourceToString)
					{
						if (item2.Value == item.Key)
						{
							dictionary[item2.Key] = item.Value.Delta;
							break;
						}
					}
				}
			}
			SINode value = new SINode
			{
				id = node.id,
				unlocked = node.unlocked,
				costs = dictionary,
				parents = new List<SINode>(),
				upgradeType = result
			};
			siNodes[result] = value;
		}
	}

	public bool TryUnlock(SIUpgradeType upgrade)
	{
		if (upgrade == SIUpgradeType.Initialize)
		{
			if (!_startingPackageGranted)
			{
				UnlockNode(upgrade);
				return true;
			}
			return false;
		}
		techTreeSO.EnsureInitialized();
		if (!techTreeSO.TryGetNode(upgrade, out var node))
		{
			return false;
		}
		SIPlayer localPlayer = SIPlayer.LocalPlayer;
		SITechTreeNode value = node.Value;
		if (localPlayer.NodeResearched(upgrade))
		{
			return false;
		}
		if (!_treeReady)
		{
			ProgressionManager.Instance.RefreshProgressionTree();
		}
		if (!_inventoryReady)
		{
			ProgressionManager.Instance.RefreshUserInventory();
		}
		if (!localPlayer.NodeParentsUnlocked(upgrade))
		{
			return false;
		}
		SIResource.ResourceCost[] nodeCost = value.nodeCost;
		for (int i = 0; i < nodeCost.Length; i++)
		{
			SIResource.ResourceCost resourceCost = nodeCost[i];
			if (resourceCost.amount > GetCurrencyAmount(resourceCost.type))
			{
				return false;
			}
		}
		UnlockNode(upgrade);
		return true;
	}

	private SINode GetNodeFromID(string id)
	{
		foreach (KeyValuePair<SIUpgradeType, SINode> siNode in siNodes)
		{
			if (siNode.Value.id == id)
			{
				return siNode.Value;
			}
		}
		return default(SINode);
	}

	private void UpdateCurrencyOnPlayer()
	{
		foreach (SIResource.ResourceType item in resourceDict.Keys.ToList())
		{
			int value = 0;
			try
			{
				value = GetCurrencyAmount(item);
			}
			catch
			{
			}
			resourceDict[item] = value;
		}
		SIPlayer.SetAndBroadcastProgression();
		if (!ClientReady && questSourceList != null)
		{
			ClientReady = true;
			this.OnClientReady?.Invoke();
		}
	}

	private void UpdateUnlockOnPlayer()
	{
		_ = SIPlayer.LocalPlayer;
		techTreeSO.EnsureInitialized();
		int num = 0;
		foreach (KeyValuePair<SIUpgradeType, SINode> siNode in siNodes)
		{
			SIUpgradeType key = siNode.Key;
			if (key >= SIUpgradeType.Thruster_Unlock)
			{
				unlockedTechTreeData[key.GetPageId()][key.GetNodeId()] = siNode.Value.unlocked;
				if (siNode.Value.unlocked)
				{
					num++;
				}
			}
		}
		SIPlayer.SetAndBroadcastProgression();
		if (!ClientReady && questSourceList != null)
		{
			ClientReady = true;
			this.OnClientReady?.Invoke();
		}
	}

	public static void InitializeQuests()
	{
		Instance._InitializeQuests();
	}

	private void ProcessAllQuests(Action<RotatingQuest> action)
	{
		foreach (RotatingQuest quest in questSourceList.quests)
		{
			action(quest);
		}
	}

	private void QuestLoadPostProcess(RotatingQuest quest)
	{
		quest.SetRequiredZone();
		if (quest.requiredZones.Count == 1 && quest.requiredZones[0] == GTZone.none)
		{
			quest.requiredZones.Clear();
		}
		quest.isQuestActive = true;
	}

	private void QuestSavePreProcess(RotatingQuest quest)
	{
		if (quest.requiredZones.Count == 0)
		{
			quest.requiredZones.Add(GTZone.none);
		}
	}

	private void _InitializeQuests()
	{
		ProgressionManager.Instance.GetActiveSIQuests(LoadQuestsFromServer);
	}

	public void LoadQuestsFromServer(List<RotatingQuest> serverQuests)
	{
		if (serverQuests == null || serverQuests.Count == 0)
		{
			Debug.LogError("[SIProgression] Server returned no quests");
			LoadQuestsFromLocalJson();
		}
		else
		{
			questSourceList = new SIQuestsList
			{
				quests = serverQuests
			};
			ProcessAllQuests(QuestLoadPostProcess);
		}
		LoadQuestProgress();
		if (!questsInitialized)
		{
			ProgressionManager.Instance.GetSIQuestStatus(ApplyServerQuestsStatus);
		}
	}

	private void LoadQuestsFromLocalJson()
	{
		TextAsset textAsset = Resources.Load<TextAsset>("TestingSuperInfectionQuests");
		LoadQuestsFromJson(textAsset.text);
		ProcessAllQuests(QuestLoadPostProcess);
	}

	public void SliceUpdate()
	{
		SuperInfectionManager activeSuperInfectionManager = SuperInfectionManager.activeSuperInfectionManager;
		if ((object)activeSuperInfectionManager != null && activeSuperInfectionManager.IsZoneReady() && questsInitialized)
		{
			CheckTimeCrossover();
			SaveQuestProgress();
			CheckTelemetry();
		}
	}

	private void CheckTimeCrossover()
	{
		CheckTimeCrossoverServer();
	}

	private void CheckTimeCrossoverServer()
	{
		DateTime utcNow = DateTime.UtcNow;
		DateTime dateTime = utcNow.Date + CROSSOVER_TIME_OF_DAY;
		if (dateTime > utcNow)
		{
			dateTime = dateTime.AddDays(-1.0);
		}
		if ((dateTime - lastQuestGrantTime).Ticks > 0)
		{
			lastQuestGrantTime = utcNow.Date + CROSSOVER_TIME_OF_DAY;
			ProgressionManager.Instance.GetSIQuestStatus(ApplyServerQuestsStatus);
		}
	}

	public static void StaticSaveQuestProgress()
	{
		Instance.SaveQuestProgress();
	}

	public void LoadQuestProgress()
	{
		LoadQuestProgressServer();
	}

	public void SaveQuestProgress()
	{
		SaveQuestProgressServer();
	}

	public void LoadQuestProgressServer()
	{
		int num = 0;
		for (int i = 0; i < activeQuestIds.Length; i++)
		{
			int num2 = PlayerPrefs.GetInt(string.Format("{0}{1}", "v1_Rotating_Quest_Daily_ID_Key", i), -1);
			int num3 = PlayerPrefs.GetInt(string.Format("{0}{1}", "v1_Rotating_Quest_Daily_Progress_Key", i), -1);
			activeQuestIds[i] = num2;
			activeQuestProgresses[i] = num3;
			if (num2 != -1)
			{
				RotatingQuest questById = questSourceList.GetQuestById(num2);
				if (questById == null || !questById.isQuestActive)
				{
					activeQuestIds[i] = -1;
					activeQuestProgresses[i] = -1;
				}
				else
				{
					num++;
					questById.ApplySavedProgress(num3);
				}
			}
		}
		bonusProgress = PlayerPrefs.GetInt("v1_SIProgression:bonusProgress", 0);
		CopySaveDataToDiff();
	}

	public void SaveQuestProgressServer()
	{
		int num = 0;
		for (int i = 0; i < activeQuestIds.Length; i++)
		{
			if (num >= stashedQuests)
			{
				activeQuestIds[i] = -1;
				activeQuestProgresses[i] = 0;
			}
			RotatingQuest questById = questSourceList.GetQuestById(activeQuestIds[i]);
			if (questById == null || !questById.isQuestActive)
			{
				activeQuestIds[i] = -1;
				activeQuestProgresses[i] = 0;
			}
			else
			{
				num++;
			}
			int num2 = -1;
			int num3 = 0;
			if (questById != null)
			{
				num2 = questById.questID;
				num3 = questById.GetProgress();
			}
			activeQuestProgresses[i] = num3;
			if (num2 != activeQuestIdsDiff[i])
			{
				PlayerPrefs.SetInt(string.Format("{0}{1}", "v1_Rotating_Quest_Daily_ID_Key", i), num2);
			}
			if (num3 != activeQuestProgressesDiff[i])
			{
				PlayerPrefs.SetInt(string.Format("{0}{1}", "v1_Rotating_Quest_Daily_Progress_Key", i), num3);
			}
		}
		if (bonusProgress != bonusProgressDiff)
		{
			PlayerPrefs.SetInt("v1_SIProgression:bonusProgress", bonusProgress);
		}
		PlayerPrefs.Save();
		CopySaveDataToDiff();
	}

	public void CopySaveDataToDiff()
	{
		lastQuestGrantTimeDiff = lastQuestGrantTime;
		stashedQuestsDiff = stashedQuests;
		stashedBonusPointsDiff = stashedBonusPoints;
		bonusProgressDiff = bonusProgress;
		int[] array = new int[6];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = resourceDict[(SIResource.ResourceType)i];
		}
		_SafeShallowCopyArray(array, ref resourceArrayDiff);
		_SafeShallowCopyArray(limitedDepositTimeArray, ref limitedDepositTimeDiff);
		_SafeShallowCopyArray(activeQuestIds, ref activeQuestIdsDiff);
		_SafeShallowCopyArray(activeQuestProgresses, ref activeQuestProgressesDiff);
		if (unlockedTechTreeDataDiff == null || unlockedTechTreeDataDiff.Length != unlockedTechTreeData.Length)
		{
			unlockedTechTreeDataDiff = new bool[unlockedTechTreeData.Length][];
		}
		for (int j = 0; j < unlockedTechTreeData.Length; j++)
		{
			_SafeShallowCopyArray(unlockedTechTreeData[j], ref unlockedTechTreeDataDiff[j]);
		}
	}

	private static void _SafeShallowCopyArray<T>(T[] sourceArray, ref T[] ref_destinationArray)
	{
		if (ref_destinationArray == null || ref_destinationArray.Length != sourceArray.Length)
		{
			ref_destinationArray = new T[sourceArray.Length];
		}
		Array.Copy(sourceArray, ref_destinationArray, sourceArray.Length);
	}

	public int[] GetResourceArray()
	{
		int[] array = new int[resourceDict.Count];
		for (int i = 0; i < resourceDict.Count; i++)
		{
			array[i] = resourceDict[(SIResource.ResourceType)i];
		}
		return array;
	}

	public void SetResourceArray(int[] resourceArray)
	{
		for (int i = 0; i < resourceArray.Length; i++)
		{
			resourceDict[(SIResource.ResourceType)i] = resourceArray[i];
		}
	}

	public void HandleQuestCompleted(int questID)
	{
		UpdateQuestProgresses();
		SIPlayer.SetAndBroadcastProgression();
		SIPlayer.LocalPlayer.questCompleteCelebrate.SetActive(value: true);
	}

	public void HandleQuestProgressChanged(bool initialLoad)
	{
		if (UpdateQuestProgresses())
		{
			SIPlayer.SetAndBroadcastProgression();
		}
	}

	private bool UpdateQuestProgresses()
	{
		bool result = false;
		for (int i = 0; i < activeQuestIds.Length; i++)
		{
			RotatingQuest questById = questSourceList.GetQuestById(activeQuestIds[i]);
			int num = 0;
			if (questById != null)
			{
				num = questById.GetProgress();
				if (questById.questType != QuestType.moveDistance || activeQuestProgresses[i] / 100 != num / 100)
				{
					result = true;
				}
			}
			activeQuestProgresses[i] = num;
		}
		SaveQuestProgress();
		return result;
	}

	public void AttemptIncrementResource(SIResource.ResourceType resource)
	{
		ProgressionManager.Instance.IncrementSIResource(resource.ToString(), OnSuccessfulIncrementResource, delegate(string err)
		{
			Debug.LogError(err);
		});
	}

	private void OnSuccessfulIncrementResource(string resourceStr)
	{
		if (Enum.Parse<SIResource.ResourceType>(resourceStr) == SIResource.ResourceType.TechPoint)
		{
			SIPlayer.LocalPlayer.TechPointGrantedCelebrate();
		}
		ProgressionManager.Instance.RefreshUserInventory();
	}

	public void AttemptRedeemCompletedQuest(int questIndex)
	{
		RotatingQuest quest = questSourceList.GetQuestById(activeQuestIds[questIndex]);
		if (quest == null || activeQuestIds[questIndex] == -1 || !quest.isQuestComplete || redeemingQuestInProgress[questIndex])
		{
			return;
		}
		redeemingQuestInProgress[questIndex] = true;
		ProgressionManager.Instance.CompleteSIQuest(quest.questID, delegate(ProgressionManager.UserQuestsStatusResponse status)
		{
			OnSuccessfulQuestRedeem(questIndex, quest, status);
		}, delegate(string err)
		{
			if (err.Contains("409") || err.Contains("404"))
			{
				OnInvalidQuestRedeemAttempt(questIndex, quest);
			}
			redeemingQuestInProgress[questIndex] = false;
			Debug.LogError(err);
		});
	}

	private void OnSuccessfulQuestRedeem(int questIndex, RotatingQuest quest, ProgressionManager.UserQuestsStatusResponse userQuestsStatus)
	{
		activeQuestIds[questIndex] = -1;
		activeQuestProgresses[questIndex] = 0;
		quest.ApplySavedProgress(0);
		redeemingQuestInProgress[questIndex] = false;
		resourceDict[SIResource.ResourceType.TechPoint]++;
		ApplyServerQuestsStatus(userQuestsStatus);
		SIPlayer.LocalPlayer.TechPointGrantedCelebrate();
		ProgressionManager.Instance.RefreshUserInventory();
	}

	private void OnInvalidQuestRedeemAttempt(int questIndex, RotatingQuest quest)
	{
		activeQuestIds[questIndex] = -1;
		activeQuestProgresses[questIndex] = 0;
		quest.ApplySavedProgress(0);
		ProgressionManager.Instance.GetSIQuestStatus(ApplyServerQuestsStatus);
	}

	public void AttemptRedeemBonusPoint()
	{
		ProgressionManager.Instance.CompleteSIBonus(delegate(ProgressionManager.UserQuestsStatusResponse userQuestsStatus)
		{
			OnSuccessfulBonusRedeem(userQuestsStatus);
		}, delegate(string err)
		{
			Debug.LogError(err);
		});
	}

	private void OnSuccessfulBonusRedeem(ProgressionManager.UserQuestsStatusResponse userQuestsStatus)
	{
		bonusProgress = 0;
		ApplyServerQuestsStatus(userQuestsStatus);
		SIPlayer.LocalPlayer.TechPointGrantedCelebrate();
		ProgressionManager.Instance.RefreshUserInventory();
	}

	public void AttemptCollectMonkeIdol()
	{
		ProgressionManager.Instance.CollectSIIdol(OnSuccessfulMonkeIdolRedeem, delegate(string err)
		{
			Debug.LogError(err);
		});
	}

	private void OnSuccessfulMonkeIdolRedeem(ProgressionManager.UserQuestsStatusResponse userQuestsStatus)
	{
		ApplyServerQuestsStatus(userQuestsStatus);
		limitedDepositTimeArray[1] = 1;
		SIPlayer.LocalPlayer.TechPointGrantedCelebrate();
		ProgressionManager.Instance.RefreshUserInventory();
	}

	public void GetBonusProgress()
	{
		bonusProgress++;
	}

	public void SetupAllQuestEventListeners()
	{
		for (int i = 0; i < activeQuestIds.Length; i++)
		{
			RotatingQuest questById = questSourceList.GetQuestById(activeQuestIds[i]);
			if (questById != null && activeQuestIds[i] != -1)
			{
				questById.questManager = this;
				if (!questById.isQuestComplete)
				{
					questById.AddEventListener();
				}
			}
		}
	}

	public static void StaticClearAllQuestEventListeners()
	{
		Instance.ClearAllQuestEventListeners();
	}

	public void ClearAllQuestEventListeners()
	{
		for (int i = 0; i < activeQuestIds.Length; i++)
		{
			questSourceList.GetQuestById(activeQuestIds[i])?.RemoveEventListener();
		}
	}

	public void LoadQuestsFromJson(string jsonString)
	{
		questSourceList = JsonConvert.DeserializeObject<SIQuestsList>(jsonString);
		ProcessAllQuests(QuestLoadPostProcess);
	}

	public void RefreshActiveQuests()
	{
		ClearAllQuestEventListeners();
		SelectActiveQuests();
		HandleQuestProgressChanged(initialLoad: true);
		SetupAllQuestEventListeners();
	}

	private void SelectActiveQuests()
	{
		int num = 0;
		for (int i = 0; i < activeQuestIds.Length; i++)
		{
			RotatingQuest questById = questSourceList.GetQuestById(activeQuestIds[i]);
			if (questById != null && questById.isQuestActive && num < stashedQuests)
			{
				activeQuestCategories[i] = questById.category;
				num++;
				continue;
			}
			activeQuestIds[i] = -1;
			activeQuestProgresses[i] = 0;
			activeQuestCategories[i] = QuestCategory.NONE;
			questById?.ApplySavedProgress(0);
		}
		int num2 = Mathf.Max(0, stashedQuests);
		for (int j = 0; j < activeQuestIds.Length; j++)
		{
			if (num >= num2)
			{
				break;
			}
			RotatingQuest questById2 = questSourceList.GetQuestById(activeQuestIds[j]);
			if (questById2 != null && questById2.isQuestActive)
			{
				continue;
			}
			int num3 = -1;
			int num4 = UnityEngine.Random.Range(0, questSourceList.quests.Count);
			for (int k = 0; k < questSourceList.quests.Count; k++)
			{
				num3 = (num4 + k) % questSourceList.quests.Count;
				RotatingQuest questById3 = questSourceList.GetQuestById(num3);
				if (questById3 == null || !questById3.isQuestActive || GetMatchingCategoryCount(questById3) >= perCategoryQuestLimit)
				{
					continue;
				}
				bool flag = false;
				for (int l = 0; l < activeQuestIds.Length; l++)
				{
					if (num3 == activeQuestIds[l])
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					activeQuestIds[j] = num3;
					activeQuestCategories[j] = questById3.category;
					questById3.ApplySavedProgress(0);
					activeQuestProgresses[j] = 0;
					num++;
					break;
				}
			}
		}
		SaveQuestProgress();
		int GetMatchingCategoryCount(RotatingQuest quest)
		{
			if (quest.category == QuestCategory.NONE)
			{
				return 0;
			}
			int num5 = 0;
			QuestCategory[] array = activeQuestCategories;
			for (int m = 0; m < array.Length; m++)
			{
				if (array[m] == quest.category)
				{
					num5++;
				}
			}
			return num5;
		}
	}

	private void SelectCurrentTurnInDate()
	{
		DateTime dateTime = new DateTime(2025, 1, 10, 18, 0, 0, DateTimeKind.Utc);
		TimeSpan timeSpan = TimeSpan.FromHours(-8.0);
		DateTime dateStart = new DateTime(1, 1, 1, 0, 0, 0);
		DateTime dateEnd = new DateTime(2006, 12, 31, 0, 0, 0);
		TimeSpan daylightDelta = TimeSpan.FromHours(1.0);
		TimeZoneInfo.TransitionTime daylightTransitionStart = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 4, 1, DayOfWeek.Sunday);
		TimeZoneInfo.TransitionTime daylightTransitionEnd = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 10, 5, DayOfWeek.Sunday);
		DateTime dateStart2 = new DateTime(2007, 1, 1, 0, 0, 0);
		DateTime dateEnd2 = new DateTime(9999, 12, 31, 0, 0, 0);
		TimeSpan daylightDelta2 = TimeSpan.FromHours(1.0);
		TimeZoneInfo.TransitionTime daylightTransitionStart2 = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 2, DayOfWeek.Sunday);
		TimeZoneInfo.TransitionTime daylightTransitionEnd2 = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 11, 1, DayOfWeek.Sunday);
		TimeZoneInfo timeZoneInfo = TimeZoneInfo.CreateCustomTimeZone("Pacific Standard Time", timeSpan, "Pacific Standard Time", "Pacific Standard Time", "Pacific Standard Time", new TimeZoneInfo.AdjustmentRule[2]
		{
			TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(dateStart, dateEnd, daylightDelta, daylightTransitionStart, daylightTransitionEnd),
			TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(dateStart2, dateEnd2, daylightDelta2, daylightTransitionStart2, daylightTransitionEnd2)
		});
		if (timeZoneInfo != null && timeZoneInfo.IsDaylightSavingTime(DateTime.UtcNow - timeSpan))
		{
			dateTime -= TimeSpan.FromHours(1.0);
		}
		_ = (DateTime.UtcNow - dateTime).Days;
	}

	public bool TryDepositResources(SIResource.ResourceType type, int count)
	{
		int resourceMaxCap = GetResourceMaxCap(type);
		int num = resourceDict[type];
		if (resourceMaxCap == num)
		{
			return false;
		}
		count = Math.Min(count, resourceMaxCap - num);
		resourceDict[type] += count;
		AttemptIncrementResource(type);
		return true;
	}

	public int GetResourceMaxCap(SIResource.ResourceType type)
	{
		return resourceCapsArray[(int)type];
	}

	public bool IsLimitedDepositAvailable(SIResource.LimitedDepositType limitedDepositType)
	{
		return !dailyLimitedTurnedIn;
	}

	public void ApplyLimitedDepositTime(SIResource.LimitedDepositType limitedDepositType)
	{
		if (limitedDepositType != SIResource.LimitedDepositType.None)
		{
			AttemptCollectMonkeIdol();
		}
	}

	private void OnDestroy()
	{
		SaveQuestProgress();
	}

	public bool GetOnlineNode(SIUpgradeType type, out SINode node)
	{
		if (!_treeReady)
		{
			node = emptyNode;
			return false;
		}
		return siNodes.TryGetValue(type, out node);
	}

	public static bool ResourcesMaxed()
	{
		return Instance._ResourcesMaxed();
	}

	public bool _ResourcesMaxed()
	{
		foreach (KeyValuePair<SIResource.ResourceType, int> item in resourceDict)
		{
			if (item.Key != SIResource.ResourceType.TechPoint && item.Value < GetResourceMaxCap(item.Key))
			{
				return false;
			}
		}
		return true;
	}

	public void CheckTelemetry()
	{
		GorillaGameManager activeGameMode = GameMode.ActiveGameMode;
		if (activeGameMode == null)
		{
			return;
		}
		GameModeType gameModeType = activeGameMode.GameType();
		if (gameModeType != GameModeType.SuperInfect && gameModeType != GameModeType.SuperCasual)
		{
			return;
		}
		if (!activeGameMode.ValidGameMode())
		{
			timeTelemetryLastChecked = Time.realtimeSinceStartup;
			return;
		}
		float num = Time.realtimeSinceStartup - timeTelemetryLastChecked;
		timeTelemetryLastChecked = Time.realtimeSinceStartup;
		totalPlayTime += num;
		if (NetworkSystem.Instance.InRoom)
		{
			roomPlayTime += num;
		}
		intervalPlayTime += num;
		for (int i = 0; i < 11; i++)
		{
			SITechTreePageId key = (SITechTreePageId)i;
			if (Instance.heldOrSnappedByGadgetPageType[key] > 0)
			{
				timeUsingGadgetTypeInterval[key] += num;
				timeUsingGadgetTypeTotal[key] += num;
			}
		}
		if (Instance.heldOrSnappedOwnGadgets > 0)
		{
			timeUsingOwnGadgetsInterval += num;
			timeUsingOwnGadgetsTotal += num;
		}
		if (Instance.heldOrSnappedOthersGadgets > 0)
		{
			timeUsingOthersGadgetsInterval += num;
			timeUsingOthersGadgetsTotal += num;
		}
		if (lastTelemetrySent + telemetryCooldown < Time.realtimeSinceStartup)
		{
			lastTelemetrySent = Time.realtimeSinceStartup;
			SaveTelemetryData();
			GorillaTelemetry.SuperInfectionEvent(roomDisconnect: false, totalPlayTime, roomPlayTime, Time.realtimeSinceStartup, intervalPlayTime, activeTerminalTimeTotal, activeTerminalTimeInterval, timeUsingGadgetTypeTotal, timeUsingGadgetTypeInterval, timeUsingOwnGadgetsTotal, timeUsingOwnGadgetsInterval, timeUsingOthersGadgetsTotal, timeUsingOthersGadgetsInterval, tagsUsingGadgetTypeTotal, tagsUsingGadgetTypeInterval, tagsHoldingOwnGadgetTotal, tagsHoldingOwnGadgetInterval, tagsHoldingOthersGadgetTotal, tagsHoldingOthersGadgetInterval, resourcesCollectedTotal, resourcesCollectedInterval, roundsPlayedTotal, roundsPlayedInterval, Instance.unlockedTechTreeData, NetworkSystem.Instance.RoomPlayerCount);
			ResetTelemetryIntervalData();
		}
	}

	public void SendTelemetryData()
	{
		if (!(Time.realtimeSinceStartup < lastDisconnectTelemetrySent + minDisconnectTelemetryCooldown))
		{
			lastDisconnectTelemetrySent = Time.realtimeSinceStartup;
			SaveTelemetryData();
			GorillaTelemetry.SuperInfectionEvent(roomDisconnect: true, totalPlayTime, roomPlayTime, Time.realtimeSinceStartup, intervalPlayTime, activeTerminalTimeTotal, activeTerminalTimeInterval, timeUsingGadgetTypeTotal, timeUsingGadgetTypeInterval, timeUsingOwnGadgetsTotal, timeUsingOwnGadgetsInterval, timeUsingOthersGadgetsTotal, timeUsingOthersGadgetsInterval, tagsUsingGadgetTypeTotal, tagsUsingGadgetTypeInterval, tagsHoldingOwnGadgetTotal, tagsHoldingOwnGadgetInterval, tagsHoldingOthersGadgetTotal, tagsHoldingOthersGadgetInterval, resourcesCollectedTotal, resourcesCollectedInterval, roundsPlayedTotal, roundsPlayedInterval, Instance.unlockedTechTreeData, NetworkSystem.Instance.InRoom ? NetworkSystem.Instance.RoomPlayerCount : (-1));
			ResetTelemetryIntervalData();
			roomPlayTime = 0f;
		}
	}

	public void SendPurchaseResourcesData()
	{
		SaveTelemetryData();
		GorillaTelemetry.SuperInfectionEvent("si_fill_resources", 500, -1, totalPlayTime, roomPlayTime, Time.realtimeSinceStartup);
	}

	public void SendPurchaseTechPointsData(int techPointsPurchased)
	{
		SaveTelemetryData();
		GorillaTelemetry.SuperInfectionEvent("si_purchase_tech_points", techPointsPurchased * 100, techPointsPurchased, totalPlayTime, roomPlayTime, Time.realtimeSinceStartup);
	}

	public void LoadSavedTelemetryData()
	{
		totalPlayTime = PlayerPrefs.GetFloat("super_infection_total_play_time", 0f);
		for (int i = 0; i < 11; i++)
		{
			SITechTreePageId sITechTreePageId = (SITechTreePageId)i;
			timeUsingGadgetTypeTotal[sITechTreePageId] = PlayerPrefs.GetFloat("super_infection_time_holding_gadget_type_total" + sITechTreePageId.GetName(), 0f);
			tagsUsingGadgetTypeTotal[sITechTreePageId] = PlayerPrefs.GetInt("super_infection_tags_holding_gadget_type_total" + sITechTreePageId.GetName(), 0);
		}
		activeTerminalTimeTotal = PlayerPrefs.GetFloat("super_infection_terminal_total_time", 0f);
		tagsHoldingOthersGadgetTotal = PlayerPrefs.GetInt("super_infection_tags_holding_others_gadgets_total", 0);
		tagsHoldingOwnGadgetTotal = PlayerPrefs.GetInt("super_infection_tags_holding_own_gadgets_total", 0);
		for (int j = 0; j < 6; j++)
		{
			SIResource.ResourceType resourceType = (SIResource.ResourceType)j;
			resourcesCollectedTotal[resourceType] = PlayerPrefs.GetInt("super_infection_resource_type_collected_total" + resourceType.GetName(), 0);
		}
		roundsPlayedTotal = PlayerPrefs.GetInt("super_infection_rounds_played_total", 0);
	}

	private void SaveTelemetryData()
	{
		PlayerPrefs.SetFloat("super_infection_total_play_time", totalPlayTime);
		for (int i = 0; i < 11; i++)
		{
			SITechTreePageId sITechTreePageId = (SITechTreePageId)i;
			PlayerPrefs.SetFloat("super_infection_time_holding_gadget_type_total" + sITechTreePageId.GetName(), timeUsingGadgetTypeTotal[sITechTreePageId]);
			PlayerPrefs.SetInt("super_infection_tags_holding_gadget_type_total" + sITechTreePageId.GetName(), tagsUsingGadgetTypeTotal[sITechTreePageId]);
		}
		PlayerPrefs.SetFloat("super_infection_terminal_total_time", activeTerminalTimeTotal);
		PlayerPrefs.SetInt("super_infection_tags_holding_others_gadgets_total", tagsHoldingOthersGadgetTotal);
		PlayerPrefs.SetInt("super_infection_tags_holding_own_gadgets_total", tagsHoldingOwnGadgetTotal);
		for (int j = 0; j < 6; j++)
		{
			SIResource.ResourceType resourceType = (SIResource.ResourceType)j;
			PlayerPrefs.SetInt("super_infection_resource_type_collected_total" + resourceType.GetName(), resourcesCollectedTotal[resourceType]);
		}
		PlayerPrefs.SetInt("super_infection_rounds_played_total", roundsPlayedTotal);
		PlayerPrefs.Save();
	}

	public void ResetTelemetryIntervalData()
	{
		lastTelemetrySent = Time.realtimeSinceStartup;
		intervalPlayTime = 0f;
		activeTerminalTimeInterval = 0f;
		for (int i = 0; i < 11; i++)
		{
			SITechTreePageId key = (SITechTreePageId)i;
			timeUsingGadgetTypeInterval[key] = 0f;
			tagsUsingGadgetTypeInterval[key] = 0;
		}
		timeUsingOwnGadgetsInterval = 0f;
		timeUsingOthersGadgetsInterval = 0f;
		tagsHoldingOthersGadgetInterval = 0;
		tagsHoldingOwnGadgetInterval = 0;
		for (int j = 0; j < 6; j++)
		{
			SIResource.ResourceType key2 = (SIResource.ResourceType)j;
			resourcesCollectedInterval[key2] = 0;
		}
		roundsPlayedInterval = 0;
	}

	public void HandleTagTelemetry(NetPlayer taggedPlayer, NetPlayer taggingPlayer)
	{
		if (taggingPlayer.ActorNumber != SIPlayer.LocalPlayer.ActorNr)
		{
			return;
		}
		for (int i = 0; i < 11; i++)
		{
			SITechTreePageId key = (SITechTreePageId)i;
			if (Instance.heldOrSnappedByGadgetPageType[key] > 0)
			{
				tagsUsingGadgetTypeTotal[key]++;
				tagsUsingGadgetTypeInterval[key]++;
			}
		}
		if (Instance.heldOrSnappedOwnGadgets > 0)
		{
			tagsHoldingOwnGadgetInterval++;
			tagsHoldingOwnGadgetTotal++;
		}
		if (Instance.heldOrSnappedOthersGadgets > 0)
		{
			tagsHoldingOthersGadgetInterval++;
			tagsHoldingOthersGadgetTotal++;
		}
	}

	public void UpdateHeldGadgetsTelemetry(SITechTreePageId id, bool isMine, int changeAmount)
	{
		Instance.heldOrSnappedByGadgetPageType[id] += changeAmount;
		if (isMine)
		{
			Instance.heldOrSnappedOwnGadgets += changeAmount;
		}
		else
		{
			Instance.heldOrSnappedOthersGadgets += changeAmount;
		}
	}

	public void CollectResourceTelemetry(SIResource.ResourceType type, int count)
	{
		resourcesCollectedTotal[type] += count;
		resourcesCollectedInterval[type] += count;
	}

	public void AddRoundTelemetry()
	{
		roundsPlayedInterval++;
		roundsPlayedTotal++;
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using GorillaLocomotion;
using UnityEngine;
using UnityEngine.Serialization;

public class SIPlayer : MonoBehaviour, ITickSystemTick
{
	[Serializable]
	public struct ProgressionData
	{
		public int[] resourceArray;

		public int[] limitedDepositTimeArray;

		public bool[][] techTreeData;

		public int stashedQuests;

		public int stashedBonusPoints;

		public int bonusProgress;

		public int[] currentQuestIds;

		public int[] currentQuestProgresses;

		public ProgressionData(bool itsNullLol)
		{
			resourceArray = new int[6];
			limitedDepositTimeArray = new int[2];
			techTreeData = new bool[progressionSO.TreePageCount][];
			for (int i = 0; i < progressionSO.TreePageCount; i++)
			{
				techTreeData[i] = new bool[progressionSO.TreeNodeCounts[i]];
			}
			stashedQuests = 0;
			stashedBonusPoints = 0;
			bonusProgress = 0;
			currentQuestIds = new int[3];
			currentQuestProgresses = new int[3];
		}

		public ProgressionData(int[] _resourceArray, int[] _limitedDepositTimeArray, bool[][] _techTreeData, int _stashedQuests, int _stashedBonusPoints, int _bonusProgress, int[] _currentQuestIds, int[] _currentQuestProgresses)
		{
			resourceArray = _resourceArray;
			limitedDepositTimeArray = _limitedDepositTimeArray;
			techTreeData = _techTreeData;
			stashedQuests = _stashedQuests;
			stashedBonusPoints = _stashedBonusPoints;
			bonusProgress = _bonusProgress;
			currentQuestIds = _currentQuestIds;
			currentQuestProgresses = _currentQuestProgresses;
		}

		public ProgressionData(SIProgression siProgression)
		{
			resourceArray = siProgression.GetResourceArray();
			limitedDepositTimeArray = siProgression.limitedDepositTimeArray;
			techTreeData = siProgression.unlockedTechTreeData;
			stashedQuests = siProgression.stashedQuests;
			stashedBonusPoints = siProgression.stashedBonusPoints;
			bonusProgress = siProgression.bonusProgress;
			currentQuestIds = siProgression.ActiveQuestIds;
			currentQuestProgresses = siProgression.ActiveQuestProgresses;
		}

		public bool IsUnlocked(SIUpgradeType upgradeType)
		{
			return techTreeData[upgradeType.GetPageId()][upgradeType.GetNodeId()];
		}
	}

	private const string preLog = "[SIPlayer]  ";

	private const string preErr = "[SIPlayer]  ERROR!!!  ";

	public GamePlayer gamePlayer;

	private static Dictionary<int, SIPlayer> siPlayerByActorNr = new Dictionary<int, SIPlayer>();

	public CallLimiter clientToAuthorityRPCLimiter;

	public CallLimiter clientToClientRPCLimiter;

	public CallLimiter authorityToClientRPCLimiter;

	public static SITechTreeSO progressionSO;

	public SITechTreeSO progressionSORef;

	public ParticleSystem tpParticleSystem;

	public GameObject bonusProgressionCelebrate;

	[FormerlySerializedAs("testPointGainedCelebrate")]
	public GameObject techPointGainedCelebrate;

	public GameObject monkeIdolDepositCelebrate;

	public GameObject questCompleteCelebrate;

	private int lastQuestsAvailableToClaim;

	private const int STANDARD_GADGET_LIMIT = 3;

	private const int SUBSCRIBER_GADGET_LIMIT = 6;

	[NonSerialized]
	public int exclusionZoneCount;

	public bool netInitialized;

	private ProgressionData currentProgression;

	public List<int> activePlayerGadgets = new List<int>();

	private static float _debug_lastStaleSlotLogTime;

	public static SIPlayer LocalPlayer => Get(NetworkSystem.Instance.LocalPlayer.ActorNumber);

	public int TotalGadgetLimit
	{
		get
		{
			if (!gamePlayer.IsSubscribed)
			{
				return 3;
			}
			return 6;
		}
	}

	public bool TickRunning { get; set; }

	public int ActorNr
	{
		get
		{
			if (!gamePlayer.rig.isOfflineVRRig)
			{
				return gamePlayer.rig.OwningNetPlayer.ActorNumber;
			}
			return NetworkSystem.Instance.LocalPlayerID;
		}
	}

	public ProgressionData CurrentProgression => currentProgression;

	public event Action<Vector3> OnKnockback;

	public event Action OnBlasterHit;

	public event Action OnBlasterSplashHit;

	private void Awake()
	{
		activePlayerGadgets = new List<int>();
		progressionSO = progressionSORef;
		clientToAuthorityRPCLimiter = new CallLimiter(25, 1f);
		clientToClientRPCLimiter = new CallLimiter(25, 1f);
		authorityToClientRPCLimiter = new CallLimiter(25, 1f);
		currentProgression = new ProgressionData(itsNullLol: true);
		GamePlayer obj = gamePlayer;
		obj.OnPlayerLeftZone = (Action)Delegate.Combine(obj.OnPlayerLeftZone, new Action(ClearGadgetsOnLeaveZone));
	}

	private void OnEnable()
	{
		if (this == LocalPlayer)
		{
			TickSystem<object>.AddTickCallback(this);
		}
	}

	private void OnDisable()
	{
		if (!ApplicationQuittingState.IsQuitting)
		{
			TickSystem<object>.RemoveTickCallback(this);
			Reset();
		}
	}

	public void Reset()
	{
		if (LocalPlayer == this)
		{
			SIProgression.StaticSaveQuestProgress();
			SIProgression.StaticClearAllQuestEventListeners();
		}
		lastQuestsAvailableToClaim = 999;
		tpParticleSystem.Stop();
		netInitialized = false;
	}

	public static SIPlayer Get(int actorNumber)
	{
		if (siPlayerByActorNr.TryGetValue(actorNumber, out var value))
		{
			return value;
		}
		if (!GamePlayer.TryGetGamePlayer(actorNumber, out var out_gamePlayer))
		{
			return null;
		}
		siPlayerByActorNr.Add(actorNumber, out_gamePlayer.GetComponent<SIPlayer>());
		return siPlayerByActorNr[actorNumber];
	}

	public static void ClearPlayerCache()
	{
		siPlayerByActorNr.Clear();
	}

	public static SIPlayer Get(VRRig vrRig)
	{
		if (!vrRig)
		{
			return null;
		}
		return vrRig.GetComponent<SIPlayer>();
	}

	public void SerializeNetworkState(BinaryWriter writer, NetPlayer player)
	{
		for (int i = 0; i < 6; i++)
		{
			writer.Write(CurrentProgression.resourceArray[i]);
		}
		for (int j = 0; j < 2; j++)
		{
			writer.Write(CurrentProgression.limitedDepositTimeArray[j]);
		}
		for (int k = 0; k < progressionSO.TreePageCount; k++)
		{
			for (int l = 0; l < progressionSO.TreeNodeCounts[k]; l++)
			{
				writer.Write(CurrentProgression.techTreeData[k][l]);
			}
		}
		writer.Write((byte)CurrentProgression.stashedQuests);
		writer.Write((byte)CurrentProgression.stashedBonusPoints);
		writer.Write((byte)CurrentProgression.bonusProgress);
		for (int m = 0; m < CurrentProgression.currentQuestIds.Length; m++)
		{
			writer.Write(CurrentProgression.currentQuestIds[m]);
			writer.Write(CurrentProgression.currentQuestProgresses[m]);
		}
	}

	public static void DeserializeNetworkStateAndBurn(BinaryReader reader, SIPlayer player, SuperInfectionManager siManager)
	{
		if (player == null || player == LocalPlayer)
		{
			for (int i = 0; i < 6; i++)
			{
				reader.ReadInt32();
			}
			for (int j = 0; j < 2; j++)
			{
				reader.ReadInt32();
			}
			for (int k = 0; k < progressionSO.TreePageCount; k++)
			{
				for (int l = 0; l < progressionSO.TreeNodeCounts[k]; l++)
				{
					reader.ReadBoolean();
				}
			}
			reader.ReadByte();
			reader.ReadByte();
			reader.ReadByte();
			reader.ReadInt32();
			reader.ReadInt32();
			reader.ReadInt32();
			reader.ReadInt32();
			reader.ReadInt32();
			reader.ReadInt32();
			return;
		}
		int[] array = new int[6];
		int[] array2 = new int[2];
		bool[][] array3 = new bool[progressionSO.TreePageCount][];
		for (int m = 0; m < 6; m++)
		{
			array[m] = reader.ReadInt32();
		}
		for (int n = 0; n < 2; n++)
		{
			array2[n] = reader.ReadInt32();
		}
		for (int num = 0; num < progressionSO.TreePageCount; num++)
		{
			array3[num] = new bool[progressionSO.TreeNodeCounts[num]];
			for (int num2 = 0; num2 < progressionSO.TreeNodeCounts[num]; num2++)
			{
				array3[num][num2] = reader.ReadBoolean();
			}
		}
		int stashedQuests = reader.ReadByte();
		int stashedBonusPoints = reader.ReadByte();
		int bonusProgress = reader.ReadByte();
		int[] array4 = new int[3];
		int[] array5 = new int[3];
		for (int num3 = 0; num3 < 3; num3++)
		{
			array4[num3] = reader.ReadInt32();
			array5[num3] = reader.ReadInt32();
		}
		player.UpdateProgression(array, array2, array3, stashedQuests, stashedBonusPoints, bonusProgress, array4, array5);
	}

	public bool HasLimitedResourceBeenDeposited(SIResource.LimitedDepositType limitedDepositType)
	{
		if (limitedDepositType == SIResource.LimitedDepositType.None)
		{
			return false;
		}
		if (this == LocalPlayer)
		{
			return SIProgression.Instance.DailyLimitedTurnedIn;
		}
		return CurrentProgression.limitedDepositTimeArray[(int)limitedDepositType] > 0;
	}

	public bool CanLimitedResourceBeDeposited(SIResource.LimitedDepositType limitedDepositType)
	{
		if (limitedDepositType == SIResource.LimitedDepositType.None)
		{
			return true;
		}
		if ((bool)SIProgression.Instance)
		{
			return SIProgression.Instance.IsLimitedDepositAvailable(limitedDepositType);
		}
		return false;
	}

	public void GatherResource(SIResource.ResourceType type, SIResource.LimitedDepositType limitedDepositType, int count)
	{
		SuperInfectionManager activeSuperInfectionManager = SuperInfectionManager.activeSuperInfectionManager;
		SIProgression.Instance.resourceDict[type] += count;
		SIProgression.Instance.ApplyLimitedDepositTime(limitedDepositType);
		bool flag = type == SIResource.ResourceType.TechPoint || (limitedDepositType == SIResource.LimitedDepositType.MonkeIdol && SIProgression.Instance.limitedDepositTimeArray[(int)limitedDepositType] != 1) || SIProgression.Instance.TryDepositResources(type, count);
		switch (type)
		{
		case SIResource.ResourceType.StrangeWood:
			PlayerGameEvents.MiscEvent("SIStrangeWoodCollect");
			break;
		case SIResource.ResourceType.WeirdGear:
			PlayerGameEvents.MiscEvent("SISWeirdGearCollect");
			break;
		case SIResource.ResourceType.FloppyMetal:
			PlayerGameEvents.MiscEvent("SIFloppyMetalCollect");
			break;
		case SIResource.ResourceType.BouncySand:
			PlayerGameEvents.MiscEvent("SIBouncySandCollect");
			break;
		case SIResource.ResourceType.VibratingSpring:
			PlayerGameEvents.MiscEvent("SIVibratingSpringCollect");
			break;
		}
		if (activeSuperInfectionManager != null && activeSuperInfectionManager.zoneSuperInfection != null && flag)
		{
			SIProgression.Instance.CollectResourceTelemetry(type, count);
		}
	}

	public void GetBonusProgress(SuperInfectionManager manager)
	{
		if (SIProgression.Instance.stashedBonusPoints > 0)
		{
			SIProgression.Instance.GetBonusProgress();
			BonusProgressCelebrate();
			SetAndBroadcastProgression();
		}
	}

	public int GetResourceAmount(SIResource.ResourceType type)
	{
		return CurrentProgression.resourceArray[(int)type];
	}

	public void SetProgressionLocal()
	{
		currentProgression = new ProgressionData(SIProgression.Instance);
		int num = 0;
		if (currentProgression.techTreeData != null)
		{
			for (int i = 0; i < currentProgression.techTreeData.Length; i++)
			{
				if (currentProgression.techTreeData[i] == null)
				{
					continue;
				}
				for (int j = 0; j < currentProgression.techTreeData[i].Length; j++)
				{
					if (currentProgression.techTreeData[i][j])
					{
						num++;
					}
				}
			}
		}
		gamePlayer.SetInitializePlayer(initialized: true);
		UpdateVisualsForAvailableQuestRedemption();
	}

	public void UpdateProgression(int[] resourceArray, int[] limitedDepositTimeArray, bool[][] techTreeData, int _stashedQuests, int _stashedBonusPoints, int _bonusProgress, int[] _currentQuestIds, int[] _currentQuestProgresses)
	{
		ProgressionData newProgression = new ProgressionData(resourceArray, limitedDepositTimeArray, techTreeData, _stashedQuests, _stashedBonusPoints, _bonusProgress, _currentQuestIds, _currentQuestProgresses);
		if (netInitialized)
		{
			CelebrateIfQuestProgressMade(newProgression);
		}
		else
		{
			netInitialized = true;
			currentProgression = newProgression;
			gamePlayer.SetInitializePlayer(initialized: true);
		}
		currentProgression = newProgression;
		UpdateVisualsForAvailableQuestRedemption();
	}

	public void CelebrateIfQuestProgressMade(ProgressionData newProgression)
	{
		int num = QuestsAvailableToClaim();
		if (currentProgression.bonusProgress < newProgression.bonusProgress && currentProgression.stashedBonusPoints == newProgression.stashedBonusPoints && currentProgression.stashedBonusPoints > 0)
		{
			BonusProgressCelebrate();
		}
		bool num2 = num > 0 && currentProgression.stashedQuests > newProgression.stashedQuests;
		bool flag = currentProgression.bonusProgress >= 4 && currentProgression.stashedBonusPoints > newProgression.stashedBonusPoints;
		bool flag2 = currentProgression.limitedDepositTimeArray[1] == 0 && newProgression.limitedDepositTimeArray[1] == 1;
		if ((num2 || flag || flag2) && currentProgression.resourceArray[0] < newProgression.resourceArray[0])
		{
			TechPointGrantedCelebrate();
		}
		if (num > lastQuestsAvailableToClaim)
		{
			questCompleteCelebrate.SetActive(value: true);
		}
		lastQuestsAvailableToClaim = num;
	}

	public void TechPointGrantedCelebrate()
	{
		SIQuestBoard questBoard = SuperInfectionManager.activeSuperInfectionManager.zoneSuperInfection.questBoard;
		if (this != LocalPlayer)
		{
			questBoard.GrantBonusPointProgress();
		}
		if (techPointGainedCelebrate != null)
		{
			techPointGainedCelebrate.SetActive(value: false);
			techPointGainedCelebrate.SetActive(value: true);
		}
		else
		{
			Debug.LogError("[SIPlayer]  ERROR!!!  Null reference: `techPointGainedCelebrate`.");
		}
		if (!questBoard.celebrateParticle)
		{
			Debug.LogError("[SIPlayer]  ERROR!!!  SuperInfectionManager.zoneSuperInfection.questBoard.celebrateParticle != null");
		}
		questBoard.celebrateParticle.Play();
	}

	public void BonusProgressCelebrate()
	{
		bonusProgressionCelebrate.SetActive(value: false);
		bonusProgressionCelebrate.SetActive(value: true);
	}

	public bool AttemptUnlockNode(SIUpgradeType upgrade, SuperInfectionManager manager)
	{
		if (CurrentProgression.techTreeData[upgrade.GetPageId()][upgrade.GetNodeId()])
		{
			return false;
		}
		SITechTreeNode treeNode = progressionSO.GetTreeNode(upgrade);
		if (!PlayerCanAffordNode(treeNode))
		{
			return false;
		}
		PurchaseNode(treeNode);
		return true;
	}

	public bool PlayerCanAffordNode(SITechTreeNode node)
	{
		if (SIProgression.Instance.GetOnlineNode(node.upgradeType, out var node2))
		{
			foreach (KeyValuePair<SIResource.ResourceType, int> cost in node2.costs)
			{
				if (CurrentProgression.resourceArray[(int)cost.Key] < cost.Value)
				{
					return false;
				}
			}
		}
		else
		{
			SIResource.ResourceCost[] nodeCost = node.nodeCost;
			for (int i = 0; i < nodeCost.Length; i++)
			{
				SIResource.ResourceCost resourceCost = nodeCost[i];
				if (CurrentProgression.resourceArray[(int)resourceCost.type] < resourceCost.amount)
				{
					return false;
				}
			}
		}
		return true;
	}

	public void PurchaseNode(SITechTreeNode node)
	{
		if (SIProgression.Instance.GetOnlineNode(node.upgradeType, out var node2))
		{
			foreach (KeyValuePair<SIResource.ResourceType, int> cost in node2.costs)
			{
				SIProgression.Instance.GetResourceArray()[(int)cost.Key] -= cost.Value;
			}
		}
		else
		{
			SIResource.ResourceCost[] nodeCost = node.nodeCost;
			for (int i = 0; i < nodeCost.Length; i++)
			{
				SIResource.ResourceCost resourceCost = nodeCost[i];
				SIProgression.Instance.GetResourceArray()[(int)resourceCost.type] -= resourceCost.amount;
			}
		}
		SIProgression.Instance.unlockedTechTreeData[node.upgradeType.GetPageId()][node.upgradeType.GetNodeId()] = true;
		SetAndBroadcastProgression();
	}

	public bool NodeResearched(SIUpgradeType upgrade)
	{
		return CurrentProgression.techTreeData[upgrade.GetPageId()][upgrade.GetNodeId()];
	}

	public SIUpgradeSet GetUpgrades(SITechTreePageId pageId)
	{
		SIUpgradeSet result = default(SIUpgradeSet);
		bool[] array = CurrentProgression.techTreeData[(int)pageId];
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i])
			{
				result.Add(i);
			}
		}
		return result;
	}

	public bool NodeParentsUnlocked(SIUpgradeType upgrade)
	{
		SITechTreeNode treeNode = progressionSO.GetTreeNode(upgrade);
		for (int i = 0; i < treeNode.parentUpgrades.Length; i++)
		{
			if (!NodeResearched(treeNode.parentUpgrades[i]))
			{
				return false;
			}
		}
		return true;
	}

	public void ResetTechTree()
	{
		SIProgression.Instance.unlockedTechTreeData = new bool[progressionSO.TreePageCount][];
		for (int i = 0; i < progressionSO.TreePageCount; i++)
		{
			SIProgression.Instance.unlockedTechTreeData[i] = new bool[progressionSO.TreeNodeCounts[i]];
		}
		SetAndBroadcastProgression();
	}

	public void ResetResources()
	{
		SIProgression.Instance.resourceDict = null;
		SIProgression.Instance.EnsureInitialized();
		SIProgression.Instance.limitedDepositTimeArray = new int[2];
		SetAndBroadcastProgression();
	}

	public static void SetAndBroadcastProgression()
	{
		LocalPlayer.SetAndBroadcastProgressionLocal();
	}

	public void SetAndBroadcastProgressionLocal()
	{
		SuperInfectionManager activeSuperInfectionManager = SuperInfectionManager.activeSuperInfectionManager;
		SetProgressionLocal();
		if (NetworkSystem.Instance.InRoom && !(activeSuperInfectionManager == null))
		{
			activeSuperInfectionManager.CallRPC(SuperInfectionManager.ClientToClientRPC.BroadcastProgression, new object[8]
			{
				LocalPlayer.CurrentProgression.resourceArray,
				LocalPlayer.currentProgression.limitedDepositTimeArray,
				LocalPlayer.currentProgression.techTreeData,
				LocalPlayer.currentProgression.stashedQuests,
				LocalPlayer.currentProgression.stashedBonusPoints,
				LocalPlayer.currentProgression.bonusProgress,
				LocalPlayer.currentProgression.currentQuestIds,
				LocalPlayer.currentProgression.currentQuestProgresses
			});
			if (activeSuperInfectionManager.zoneSuperInfection != null)
			{
				activeSuperInfectionManager.zoneSuperInfection.RefreshStations(LocalPlayer.ActorNr);
			}
		}
	}

	public void UpdateVisualsForAvailableQuestRedemption()
	{
		bool flag = SuperInfectionManager.activeSuperInfectionManager != null && SuperInfectionManager.activeSuperInfectionManager.IsZoneReady() && (QuestsAvailableToClaim() > 0 || (currentProgression.bonusProgress >= 4 && currentProgression.stashedBonusPoints > 0));
		if (tpParticleSystem.isPlaying && !flag)
		{
			tpParticleSystem.Stop();
		}
		else if (!tpParticleSystem.isPlaying && flag)
		{
			tpParticleSystem.Play();
		}
	}

	public int QuestsAvailableToClaim()
	{
		int num = 0;
		for (int i = 0; i < currentProgression.currentQuestIds.Length; i++)
		{
			if (SIProgression.Instance.questSourceList.GetQuestById(currentProgression.currentQuestIds[i]) != null && currentProgression.currentQuestProgresses[i] >= SIProgression.Instance.questSourceList.GetQuestById(currentProgression.currentQuestIds[i]).requiredOccurenceCount)
			{
				num++;
			}
		}
		return num;
	}

	public bool QuestAvailableToClaim(int questIndex)
	{
		if (SIProgression.Instance.questSourceList.GetQuestById(currentProgression.currentQuestIds[questIndex]) == null)
		{
			return false;
		}
		return currentProgression.currentQuestProgresses[questIndex] >= SIProgression.Instance.questSourceList.GetQuestById(currentProgression.currentQuestIds[questIndex]).requiredOccurenceCount;
	}

	public void TriggerIdolDepositedCelebration(Vector3 position)
	{
		SuperInfectionManager activeSuperInfectionManager = SuperInfectionManager.activeSuperInfectionManager;
		if (activeSuperInfectionManager.gameEntityManager.IsAuthority())
		{
			activeSuperInfectionManager.CallRPC(SuperInfectionManager.AuthorityToClientRPC.TriggerMonkeIdolDepositCelebration, new object[1] { position });
		}
		monkeIdolDepositCelebrate.transform.position = position;
		monkeIdolDepositCelebrate.SetActive(value: false);
		monkeIdolDepositCelebrate.SetActive(value: true);
	}

	public void ClearGadgetsOnLeaveZone()
	{
		if (SuperInfectionManager.activeSuperInfectionManager.gameEntityManager.IsAuthority())
		{
			SuperInfectionManager.activeSuperInfectionManager.ClearPlayerGadgets(this);
		}
	}

	public void NotifyBlasterHit()
	{
		this.OnBlasterHit?.Invoke();
	}

	public void NotifyBlasterSplashHit()
	{
		this.OnBlasterSplashHit?.Invoke();
	}

	public void PlayerKnockback(Vector3 directionAndMagnitude, bool forceOffGround = true, bool applyExclusionZone = true)
	{
		if (!applyExclusionZone || exclusionZoneCount <= 0)
		{
			this.OnKnockback?.Invoke(directionAndMagnitude);
			GTPlayer.Instance.ApplyClampedKnockback(directionAndMagnitude.normalized, directionAndMagnitude.magnitude, 1.5f, forceOffGround);
		}
	}

	public void PlayerHandHaptic(bool isLeft, float hapticStrength, float hapticDuration, bool applyExclusionZone = true)
	{
		if (!applyExclusionZone || exclusionZoneCount <= 0)
		{
			GorillaTagger.Instance.StartVibration(isLeft, hapticStrength, hapticDuration);
		}
	}

	public void Tick()
	{
		bool isSupercharged = SuperInfectionManager.activeSuperInfectionManager?.IsSupercharged ?? false;
		if (!_TryUpdateSlotEntityCharge(gamePlayer, 0, isSupercharged))
		{
			_TryUpdateSlotEntityCharge(gamePlayer, 2, isSupercharged);
		}
		if (!_TryUpdateSlotEntityCharge(gamePlayer, 1, isSupercharged))
		{
			_TryUpdateSlotEntityCharge(gamePlayer, 3, isSupercharged);
		}
	}

	private static bool _TryUpdateSlotEntityCharge(GamePlayer gamePlayer, int slotIndex, bool isSupercharged)
	{
		if (!gamePlayer.TryGetSlotEntity(slotIndex, out var out_entity))
		{
			if (gamePlayer.TryGetSlotData(slotIndex, out var _) && Time.time - _debug_lastStaleSlotLogTime > 5f)
			{
				_debug_lastStaleSlotLogTime = Time.time;
			}
			return false;
		}
		IEnergyGadget component = out_entity.GetComponent<IEnergyGadget>();
		if (component == null || !component.UsesEnergy || component.IsFull)
		{
			return false;
		}
		float dt = Time.deltaTime * (isSupercharged ? 5f : 1f);
		component.UpdateRecharge(dt);
		return true;
	}
}

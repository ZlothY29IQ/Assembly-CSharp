using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using GorillaNetworking;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class ProgressionManager : MonoBehaviour
{
	public struct MothershipItemSummary
	{
		public string Name;

		public string EntitlementId;

		public string InGameId;

		public int Quantity;
	}

	private enum RequestType
	{
		GetProgression,
		SetProgression,
		UnlockProgressionTreeNode,
		IncrementSIResource,
		CompleteSIQuest,
		CompleteSIBonus,
		CollectSIIdol,
		GetActiveSIQuests,
		GetSIQuestsStatus,
		ResetSIQuestsStatus,
		PurchaseTechPoints,
		PurchaseResources,
		PurchaseShiftCreditCapIncrease,
		PurchaseShiftCredit,
		RegisterToGRShift,
		GetJuicerStatus,
		DepositCore,
		PurchaseOverdrive,
		GetShiftCredit,
		SubtractShiftCredit,
		AdvanceDockWristUpgrade,
		GetDockWristUpgradeStatus,
		PurchaseDrillUpgrade,
		RecycleTool,
		StartOfShift,
		EndOfShiftReward,
		GetGhostReactorStats,
		GetGhostReactorInventory,
		SetGhostReactorInventory
	}

	public enum WristDockUpgradeType
	{
		None,
		Upgrade1,
		Upgrade2,
		Upgrade3
	}

	public enum DrillUpgradeLevel
	{
		None,
		Base,
		Upgrade1,
		Upgrade2,
		Upgrade3
	}

	public enum CoreType
	{
		None,
		Core,
		SuperCore,
		ChaosSeed
	}

	[Serializable]
	private class GetProgressionRequest : MothershipRequest
	{
		public string TrackId;
	}

	[Serializable]
	private class GetProgressionResponse
	{
		public string Track;

		public int Progress;

		public int StatusCode;

		public string Error;
	}

	[Serializable]
	private class SetProgressionRequest : MothershipRequest
	{
		public string TrackId;

		public int Progress;
	}

	[Serializable]
	private class SetProgressionResponse
	{
		public string Track;

		public int Progress;

		public int StatusCode;

		public string Error;
	}

	[Serializable]
	private class UnlockNodeRequest : MothershipRequest
	{
		public string TreeId;

		public string NodeId;
	}

	[Serializable]
	private class UnlockNodeResponse
	{
		public UserHydratedProgressionTreeResponse Tree;

		public int StatusCode;

		public string Error;
	}

	[Serializable]
	private class IncrementSIResourceRequest : MothershipRequest
	{
		public string ResourceType;
	}

	[Serializable]
	private class IncrementSIResourceResponse : UserInventoryResponse
	{
		public string ResourceType;
	}

	[Serializable]
	private class GetActiveSIQuestsRequest : MothershipRequest
	{
	}

	[Serializable]
	private class GetActiveSIQuestsResponse
	{
		public GetActiveSIQuestsResult Result;

		public int StatusCode;

		public string Error;
	}

	[Serializable]
	public class GetActiveSIQuestsResult
	{
		public List<RotatingQuest> Quests;
	}

	[Serializable]
	private class GetSIQuestsStatusRequest : MothershipRequest
	{
	}

	[Serializable]
	private class ResetSIQuestsStatusRequest : MothershipRequest
	{
	}

	[Serializable]
	private class PurchaseTechPointsRequest : MothershipRequest
	{
		public int TechPointsAmount;
	}

	private class PurchaseResourcesRequest : MothershipRequest
	{
	}

	[Serializable]
	private class GetSIQuestsStatusResponse
	{
		public UserQuestsStatusResponse Result;
	}

	[Serializable]
	private class UserInventoryResponse
	{
		public UserInventory Result;
	}

	[Serializable]
	public class UserInventory
	{
		public Dictionary<string, int> Inventory;
	}

	[Serializable]
	private class SetSIQuestCompleteRequest : RewardRequest
	{
		public int QuestID;
	}

	[Serializable]
	private class SetSIBonusCompleteRequest : RewardRequest
	{
	}

	[Serializable]
	private class SetSIIdolCollectRequest : RewardRequest
	{
	}

	[Serializable]
	private class RewardRequest : MothershipRequest
	{
	}

	[Serializable]
	private class MothershipRequest
	{
		public string MothershipId;

		public string MothershipToken;

		public string MothershipEnvId;

		public string MothershipDeploymentId;
	}

	[Serializable]
	private class MothershipUserDataWriteRequest : MothershipRequest
	{
		public bool SkipUserDataCache;
	}

	[Serializable]
	public class UserQuestsStatusResponse
	{
		public int TodayClaimableQuests;

		public int TodayClaimableBonus;

		public int TodayClaimableIdol;
	}

	[Serializable]
	private class PurchaseShiftCreditCapIncreaseRequest : MothershipUserDataWriteRequest
	{
	}

	[Serializable]
	private class PurchaseShiftCreditCapIncreaseResponse
	{
		public int StatusCode;

		public string Error;

		public int CurrentShiftCreditCapIncreases;

		public int CurrentShiftCreditCapIncreasesMax;

		public string TargetMothershipId;
	}

	[Serializable]
	private class PurchaseShiftCreditRequest : MothershipUserDataWriteRequest
	{
	}

	[Serializable]
	private class PurchaseShiftCreditResponse
	{
		public int StatusCode;

		public string Error;

		public int CurrentShiftCredits;

		public string TargetMothershipId;
	}

	[Serializable]
	private class GetShiftCreditRequest : MothershipRequest
	{
		public string TargetMothershipId;
	}

	[Serializable]
	public class ShiftCreditResponse
	{
		public int StatusCode;

		public string Error;

		public int CurrentShiftCredits;

		public int CurrentShiftCreditCapIncreases;

		public int CurrentShiftCreditCapIncreasesMax;

		public string TargetMothershipId;
	}

	[Serializable]
	private class GetJuicerStatusRequest : MothershipUserDataWriteRequest
	{
	}

	[Serializable]
	private class DepositCoreRequest : MothershipUserDataWriteRequest
	{
		public CoreType CoreBeingDeposited;
	}

	[Serializable]
	private class DepositCoreResponse
	{
		public int StatusCode;

		public string Error;

		public int CurrentShiftCredits;
	}

	[Serializable]
	private class PurchaseOverdriveRequest : MothershipUserDataWriteRequest
	{
	}

	[Serializable]
	public class JuicerStatusResponse
	{
		public string MothershipId;

		public int StatusCode;

		public string Error;

		public int CurrentCoreCount;

		public int CoreProcessingTimeSec;

		public float CoreProcessingPercent;

		public int OverdriveSupply;

		public int OverdriveCap;

		public int CoresProcessedByOverdrive;

		public bool RefreshJuice;
	}

	[Serializable]
	private class SubtractShiftCreditRequest : MothershipUserDataWriteRequest
	{
		public int ShiftCreditToRemove;
	}

	[Serializable]
	private class AdvanceDockWristUpgradeRequest : MothershipUserDataWriteRequest
	{
		public WristDockUpgradeType Upgrade;
	}

	[Serializable]
	private class DockWristUpgradeStatusRequest : MothershipRequest
	{
	}

	[Serializable]
	public class DockWristStatusResponse
	{
		public int CurrentUpgrade1Level;

		public int CurrentUpgrade2Level;

		public int CurrentUpgrade3Level;

		public int Upgrade1LevelMax;

		public int Upgrade2LevelMax;

		public int Upgrade3LevelMax;
	}

	[Serializable]
	private class PurchaseDrillUpgradeRequest : MothershipRequest
	{
		public DrillUpgradeLevel Upgrade;
	}

	[Serializable]
	private class PurchaseDrillUpgradeResponse
	{
		public int StatusCode;

		public string Error;
	}

	[Serializable]
	private class RecycleToolRequest : MothershipRequest
	{
		public GRTool.GRToolType ToolBeingRecycled;

		public int NumberOfPlayers;
	}

	[Serializable]
	private class StartOfShiftRequest : MothershipRequest
	{
		public string ShiftId;

		public int CoresRequired;

		public int NumberOfPlayers;

		public int Depth;
	}

	[Serializable]
	private class EndOfShiftRewardRequest : MothershipUserDataWriteRequest
	{
		public string ShiftId;
	}

	[Serializable]
	private class GhostReactorStatsRequest : MothershipRequest
	{
	}

	[Serializable]
	public class GhostReactorStatsResponse
	{
		public string MothershipId;

		public int MaxDepthReached;
	}

	[Serializable]
	private class GhostReactorInventoryRequest : MothershipRequest
	{
	}

	[Serializable]
	public class GhostReactorInventoryResponse
	{
		public string MothershipId;

		public string InventoryJson;
	}

	[Serializable]
	private class SetGhostReactorInventoryRequest : MothershipUserDataWriteRequest
	{
		public string InventoryJson;
	}

	[Serializable]
	public class SetGhostReactorInventoryResponse
	{
		public string MothershipId;
	}

	private readonly Dictionary<string, UserHydratedProgressionTreeResponse> _trees = new Dictionary<string, UserHydratedProgressionTreeResponse>();

	private readonly Dictionary<string, MothershipItemSummary> _inventory = new Dictionary<string, MothershipItemSummary>();

	private readonly Dictionary<string, int> _tracks = new Dictionary<string, int>();

	private Dictionary<RequestType, int> retryCounters = new Dictionary<RequestType, int>();

	private int maxRetriesOnFail = 4;

	private const double k_minRefreshIntervalSeconds = 2.0;

	private double _lastTreeRefreshTime = double.NegativeInfinity;

	private double _lastInventoryRefreshTime = double.NegativeInfinity;

	private bool _treeRefreshInFlight;

	private bool _inventoryRefreshInFlight;

	public static int debug_refreshTreeCount;

	public static int debug_refreshInventoryCount;

	public static int debug_refreshTreeDroppedByThrottle;

	public static int debug_refreshInventoryDroppedByThrottle;

	public static double debug_lastRefreshTreeAttemptTime;

	public static double debug_lastRefreshInventoryAttemptTime;

	public static ProgressionManager Instance { get; private set; }

	public event Action OnTreeUpdated;

	public event Action OnInventoryUpdated;

	public event Action<string, int> OnTrackRead;

	public event Action<string, int> OnTrackSet;

	public event Action<string, string> OnNodeUnlocked;

	public event Action<string, int> OnGetShiftCredit;

	public event Action<string, int, int> OnGetShiftCreditCapData;

	public event Action<bool> OnPurchaseShiftCreditCapIncrease;

	public event Action<bool> OnPurchaseShiftCredit;

	public event Action<bool> OnChaosDepositSuccess;

	public event Action<JuicerStatusResponse> OnJucierStatusUpdated;

	public event Action<bool> OnPurchaseOverdrive;

	public event Action<DockWristStatusResponse> OnDockWristStatusUpdated;

	public event Action<GhostReactorStatsResponse> OnGhostReactorStatsUpdated;

	public event Action<GhostReactorInventoryResponse> OnGhostReactorInventoryUpdated;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
	}

	public async void RefreshProgressionTree()
	{
		double num = (debug_lastRefreshTreeAttemptTime = Time.unscaledTimeAsDouble);
		if (_treeRefreshInFlight)
		{
			debug_refreshTreeDroppedByThrottle++;
			return;
		}
		if (num - _lastTreeRefreshTime < 2.0)
		{
			debug_refreshTreeDroppedByThrottle++;
			return;
		}
		_lastTreeRefreshTime = num;
		_treeRefreshInFlight = true;
		debug_refreshTreeCount++;
		await ProgressionUtil.WaitForMothershipSessionToken();
		MothershipClientApiUnity.GetPlayerProgressionTreesData(delegate(GetProgressionTreesForPlayerResponse response)
		{
			_treeRefreshInFlight = false;
			OnGetTrees(response);
		}, delegate(MothershipError err, int code)
		{
			_treeRefreshInFlight = false;
			GetMothershipFailure(err, code);
		});
	}

	public async void RefreshUserInventory()
	{
		double num = (debug_lastRefreshInventoryAttemptTime = Time.unscaledTimeAsDouble);
		if (_inventoryRefreshInFlight)
		{
			debug_refreshInventoryDroppedByThrottle++;
			return;
		}
		if (num - _lastInventoryRefreshTime < 2.0)
		{
			debug_refreshInventoryDroppedByThrottle++;
			return;
		}
		_lastInventoryRefreshTime = num;
		_inventoryRefreshInFlight = true;
		debug_refreshInventoryCount++;
		await ProgressionUtil.WaitForMothershipSessionToken();
		MothershipClientApiUnity.GetUserInventory(delegate(MothershipGetInventoryResponse response)
		{
			_inventoryRefreshInFlight = false;
			OnGetInventory(response);
		}, delegate(MothershipError err, int code)
		{
			_inventoryRefreshInFlight = false;
			GetMothershipFailure(err, code);
		});
		await ProgressionUtil.WaitForPlayFabSessionTicket();
		RefreshShinyRocksTotal();
	}

	public UserHydratedProgressionTreeResponse GetTree(string treeName)
	{
		_trees.TryGetValue(treeName, out var value);
		return value;
	}

	public bool GetInventoryItem(string inventoryKey, out MothershipItemSummary item)
	{
		return _inventory.TryGetValue(inventoryKey?.Trim(), out item);
	}

	public int GetNodeCost(string treeName, string nodeId, string currencyKey)
	{
		if (!_trees.TryGetValue(treeName, out var value) || value == null || string.IsNullOrEmpty(nodeId) || string.IsNullOrEmpty(currencyKey))
		{
			return 0;
		}
		foreach (UserHydratedNodeDefinition node in value.Nodes)
		{
			if (!(node.id == nodeId) || node.cost == null || node.cost.items == null)
			{
				continue;
			}
			foreach (KeyValuePair<string, MothershipHydratedInventoryChange> item in node.cost.items)
			{
				if (string.Equals(item.Key?.Trim(), currencyKey.Trim(), StringComparison.Ordinal))
				{
					return item.Value.Delta;
				}
			}
			break;
		}
		return 0;
	}

	public async void GetProgression(string trackId)
	{
		await ProgressionUtil.WaitForMothershipSessionToken();
		StartCoroutine(DoGetProgression(new GetProgressionRequest
		{
			MothershipId = MothershipClientContext.MothershipId,
			MothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			MothershipDeploymentId = MothershipClientApiUnity.DeploymentId,
			MothershipToken = MothershipClientContext.Token,
			TrackId = trackId
		}));
	}

	public async void SetProgression(string trackId, int progress)
	{
		await ProgressionUtil.WaitForMothershipSessionToken();
		StartCoroutine(DoSetProgression(new SetProgressionRequest
		{
			MothershipId = MothershipClientContext.MothershipId,
			MothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			MothershipDeploymentId = MothershipClientApiUnity.DeploymentId,
			MothershipToken = MothershipClientContext.Token,
			TrackId = trackId,
			Progress = progress
		}));
	}

	public async void UnlockNode(string treeId, string nodeId)
	{
		await ProgressionUtil.WaitForMothershipSessionToken();
		StartCoroutine(DoUnlockNode(new UnlockNodeRequest
		{
			MothershipId = MothershipClientContext.MothershipId,
			MothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			MothershipDeploymentId = MothershipClientApiUnity.DeploymentId,
			MothershipToken = MothershipClientContext.Token,
			TreeId = treeId,
			NodeId = nodeId
		}));
	}

	public async void IncrementSIResource(string resourceName, Action<string> OnSuccess = null, Action<string> OnFailure = null)
	{
		await ProgressionUtil.WaitForMothershipSessionToken();
		StartCoroutine(DoIncrementSIResource(new IncrementSIResourceRequest
		{
			MothershipId = MothershipClientContext.MothershipId,
			MothershipToken = MothershipClientContext.Token,
			MothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			MothershipDeploymentId = MothershipClientApiUnity.DeploymentId,
			ResourceType = resourceName
		}, OnSuccess, OnFailure));
	}

	public async void CompleteSIQuest(int questID, Action<UserQuestsStatusResponse> OnSuccess = null, Action<string> OnFailure = null)
	{
		await ProgressionUtil.WaitForMothershipSessionToken();
		StartCoroutine(DoQuestCompleteReward(new SetSIQuestCompleteRequest
		{
			MothershipId = MothershipClientContext.MothershipId,
			MothershipToken = MothershipClientContext.Token,
			MothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			MothershipDeploymentId = MothershipClientApiUnity.DeploymentId,
			QuestID = questID
		}, OnSuccess, OnFailure));
	}

	public async void CompleteSIBonus(Action<UserQuestsStatusResponse> OnSuccess = null, Action<string> OnFailure = null)
	{
		await ProgressionUtil.WaitForMothershipSessionToken();
		StartCoroutine(DoBonusCompleteReward(new SetSIBonusCompleteRequest
		{
			MothershipId = MothershipClientContext.MothershipId,
			MothershipToken = MothershipClientContext.Token,
			MothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			MothershipDeploymentId = MothershipClientApiUnity.DeploymentId
		}, OnSuccess, OnFailure));
	}

	public async void CollectSIIdol(Action<UserQuestsStatusResponse> OnSuccess = null, Action<string> OnFailure = null)
	{
		await ProgressionUtil.WaitForMothershipSessionToken();
		StartCoroutine(DoIdolCollectReward(new SetSIIdolCollectRequest
		{
			MothershipId = MothershipClientContext.MothershipId,
			MothershipToken = MothershipClientContext.Token,
			MothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			MothershipDeploymentId = MothershipClientApiUnity.DeploymentId
		}, OnSuccess, OnFailure));
	}

	public async void GetActiveSIQuests(Action<List<RotatingQuest>> OnSuccess = null, Action<string> OnFailure = null)
	{
		await ProgressionUtil.WaitForMothershipSessionToken();
		StartCoroutine(DoGetActiveSIQuests(new GetActiveSIQuestsRequest
		{
			MothershipId = MothershipClientContext.MothershipId,
			MothershipToken = MothershipClientContext.Token,
			MothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			MothershipDeploymentId = MothershipClientApiUnity.DeploymentId
		}, OnSuccess, OnFailure));
	}

	public async void GetSIQuestStatus(Action<UserQuestsStatusResponse> OnSuccess = null, Action<string> OnFailure = null)
	{
		await ProgressionUtil.WaitForMothershipSessionToken();
		StartCoroutine(DoGetSIQuestsStatus(new GetSIQuestsStatusRequest
		{
			MothershipId = MothershipClientContext.MothershipId,
			MothershipToken = MothershipClientContext.Token,
			MothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			MothershipDeploymentId = MothershipClientApiUnity.DeploymentId
		}, OnSuccess, OnFailure));
	}

	public async void PurchaseTechPoints(int amount, Action OnSuccess = null, Action<string> OnFailure = null)
	{
		await ProgressionUtil.WaitForMothershipSessionToken();
		StartCoroutine(DoPurchaseTechPoints(new PurchaseTechPointsRequest
		{
			MothershipId = MothershipClientContext.MothershipId,
			MothershipToken = MothershipClientContext.Token,
			MothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			MothershipDeploymentId = MothershipClientApiUnity.DeploymentId,
			TechPointsAmount = amount
		}, OnSuccess, OnFailure));
	}

	public async void PurchaseResources(Action<UserInventory> OnSuccess = null, Action<string> OnFailure = null)
	{
		await ProgressionUtil.WaitForMothershipSessionToken();
		StartCoroutine(DoPurchaseResources(new PurchaseResourcesRequest
		{
			MothershipId = MothershipClientContext.MothershipId,
			MothershipToken = MothershipClientContext.Token,
			MothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			MothershipDeploymentId = MothershipClientApiUnity.DeploymentId
		}, OnSuccess, OnFailure));
	}

	public void PurchaseShiftCreditCapIncrease()
	{
		PurchaseShiftCreditCapIncreaseInternal();
	}

	private void PurchaseShiftCreditCapIncreaseInternal(bool skipUserDataCache = false)
	{
		StartCoroutine(DoPurchaseShiftCreditCapIncrease(new PurchaseShiftCreditCapIncreaseRequest
		{
			MothershipId = MothershipClientContext.MothershipId,
			MothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			MothershipDeploymentId = MothershipClientApiUnity.DeploymentId,
			MothershipToken = MothershipClientContext.Token,
			SkipUserDataCache = skipUserDataCache
		}));
	}

	public void PurchaseShiftCredit()
	{
		PurchaseShiftCreditInternal();
	}

	private void PurchaseShiftCreditInternal(bool skipUserDataCache = false)
	{
		StartCoroutine(DoPurchaseShiftCredit(new PurchaseShiftCreditRequest
		{
			MothershipId = MothershipClientContext.MothershipId,
			MothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			MothershipDeploymentId = MothershipClientApiUnity.DeploymentId,
			MothershipToken = MothershipClientContext.Token,
			SkipUserDataCache = skipUserDataCache
		}));
	}

	public void GetShiftCredit(string mothershipId)
	{
		StartCoroutine(DoGetShiftCredit(new GetShiftCreditRequest
		{
			MothershipId = MothershipClientContext.MothershipId,
			MothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			MothershipDeploymentId = MothershipClientApiUnity.DeploymentId,
			MothershipToken = MothershipClientContext.Token,
			TargetMothershipId = mothershipId
		}));
	}

	public void GetJuicerStatus()
	{
		GetJuicerStatusInternal();
	}

	private void GetJuicerStatusInternal(bool skipUserDataCache = false)
	{
		StartCoroutine(DoGetJuicerStatus(new GetJuicerStatusRequest
		{
			MothershipId = MothershipClientContext.MothershipId,
			MothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			MothershipDeploymentId = MothershipClientApiUnity.DeploymentId,
			MothershipToken = MothershipClientContext.Token,
			SkipUserDataCache = skipUserDataCache
		}));
	}

	public void DepositCore(CoreType coreType)
	{
		DepositCoreInternal(coreType);
	}

	private void DepositCoreInternal(CoreType coreType, bool skipUserDataCache = false)
	{
		StartCoroutine(DoDepositCore(new DepositCoreRequest
		{
			MothershipId = MothershipClientContext.MothershipId,
			MothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			MothershipDeploymentId = MothershipClientApiUnity.DeploymentId,
			MothershipToken = MothershipClientContext.Token,
			CoreBeingDeposited = coreType,
			SkipUserDataCache = skipUserDataCache
		}));
	}

	public void PurchaseOverdrive()
	{
		PurchaseOverdriveInternal();
	}

	private void PurchaseOverdriveInternal(bool skipUserDataCache = false)
	{
		StartCoroutine(DoPurchaseOverdrive(new PurchaseOverdriveRequest
		{
			MothershipId = MothershipClientContext.MothershipId,
			MothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			MothershipDeploymentId = MothershipClientApiUnity.DeploymentId,
			MothershipToken = MothershipClientContext.Token,
			SkipUserDataCache = skipUserDataCache
		}));
	}

	public void SubtractShiftCredit(int creditsToSubtract)
	{
		SubtractShiftCreditInternal(creditsToSubtract);
	}

	private void SubtractShiftCreditInternal(int creditsToSubtract, bool skipUserDataCache = false)
	{
		StartCoroutine(DoSubtractShiftCredit(new SubtractShiftCreditRequest
		{
			MothershipId = MothershipClientContext.MothershipId,
			MothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			MothershipDeploymentId = MothershipClientApiUnity.DeploymentId,
			MothershipToken = MothershipClientContext.Token,
			ShiftCreditToRemove = creditsToSubtract,
			SkipUserDataCache = skipUserDataCache
		}));
	}

	public void AdvanceDockWristUpgradeLevel(WristDockUpgradeType upgrade)
	{
		AdvanceDockWristUpgradeLevelInternal(upgrade);
	}

	private void AdvanceDockWristUpgradeLevelInternal(WristDockUpgradeType upgrade, bool skipUserDataCache = false)
	{
		StartCoroutine(DoAdvanceDockWristUpgradeLevel(new AdvanceDockWristUpgradeRequest
		{
			MothershipId = MothershipClientContext.MothershipId,
			MothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			MothershipDeploymentId = MothershipClientApiUnity.DeploymentId,
			MothershipToken = MothershipClientContext.Token,
			Upgrade = upgrade,
			SkipUserDataCache = skipUserDataCache
		}));
	}

	public void GetDockWristUpgradeStatus()
	{
		StartCoroutine(DoGetDockWristUpgradeStatus(new DockWristUpgradeStatusRequest
		{
			MothershipId = MothershipClientContext.MothershipId,
			MothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			MothershipDeploymentId = MothershipClientApiUnity.DeploymentId,
			MothershipToken = MothershipClientContext.Token
		}));
	}

	public void PurchaseDrillUpgrade(DrillUpgradeLevel upgrade)
	{
		StartCoroutine(DoPurchaseDrillUpgrade(new PurchaseDrillUpgradeRequest
		{
			MothershipId = MothershipClientContext.MothershipId,
			MothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			MothershipDeploymentId = MothershipClientApiUnity.DeploymentId,
			MothershipToken = MothershipClientContext.Token,
			Upgrade = upgrade
		}));
	}

	public void RecycleTool(GRTool.GRToolType toolBeingRecycled, int numberOfPlayers)
	{
		StartCoroutine(DoRecycleTool(new RecycleToolRequest
		{
			MothershipId = MothershipClientContext.MothershipId,
			MothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			MothershipDeploymentId = MothershipClientApiUnity.DeploymentId,
			MothershipToken = MothershipClientContext.Token,
			ToolBeingRecycled = toolBeingRecycled,
			NumberOfPlayers = numberOfPlayers
		}));
	}

	public void StartOfShift(string shiftId, int coresRequired, int numberOfPlayers, int depth)
	{
		StartCoroutine(DoStartOfShift(new StartOfShiftRequest
		{
			MothershipId = MothershipClientContext.MothershipId,
			MothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			MothershipDeploymentId = MothershipClientApiUnity.DeploymentId,
			MothershipToken = MothershipClientContext.Token,
			ShiftId = shiftId,
			CoresRequired = coresRequired,
			NumberOfPlayers = numberOfPlayers,
			Depth = depth
		}));
	}

	public void EndOfShiftReward(string shiftId)
	{
		EndOfShiftRewardInternal(shiftId);
	}

	private void EndOfShiftRewardInternal(string shiftId, bool skipUserDataCache = false)
	{
		StartCoroutine(DoEndOfShiftReward(new EndOfShiftRewardRequest
		{
			MothershipId = MothershipClientContext.MothershipId,
			MothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			MothershipDeploymentId = MothershipClientApiUnity.DeploymentId,
			MothershipToken = MothershipClientContext.Token,
			ShiftId = shiftId,
			SkipUserDataCache = skipUserDataCache
		}));
	}

	public void GetGhostReactorStats()
	{
		StartCoroutine(DoGetGhostReactorStats(new GhostReactorStatsRequest
		{
			MothershipId = MothershipClientContext.MothershipId,
			MothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			MothershipDeploymentId = MothershipClientApiUnity.DeploymentId,
			MothershipToken = MothershipClientContext.Token
		}));
	}

	public void GetGhostReactorInventory()
	{
		StartCoroutine(DoGetGhostReactorInventory(new GhostReactorInventoryRequest
		{
			MothershipId = MothershipClientContext.MothershipId,
			MothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			MothershipDeploymentId = MothershipClientApiUnity.DeploymentId,
			MothershipToken = MothershipClientContext.Token
		}));
	}

	public void SetGhostReactorInventory(string jsonInventory)
	{
		SetGhostReactorInventoryInternal(jsonInventory);
	}

	private void SetGhostReactorInventoryInternal(string jsonInventory, bool skipUserDataCache = false)
	{
		StartCoroutine(DoSetGhostReactorInventory(new SetGhostReactorInventoryRequest
		{
			MothershipId = MothershipClientContext.MothershipId,
			MothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			MothershipDeploymentId = MothershipClientApiUnity.DeploymentId,
			MothershipToken = MothershipClientContext.Token,
			InventoryJson = jsonInventory,
			SkipUserDataCache = skipUserDataCache
		}));
	}

	private IEnumerator HandleWebRequestRetries<T>(RequestType requestType, T data, Action<T> actionToTake, Action failureActionToTake = null)
	{
		if (!retryCounters.ContainsKey(requestType))
		{
			retryCounters[requestType] = 0;
		}
		if (retryCounters[requestType] < maxRetriesOnFail)
		{
			float num = UnityEngine.Random.Range(0.5f, Mathf.Pow(2f, retryCounters[requestType] + 1));
			Debug.LogWarning($"PM: Retrying ... attempt #{retryCounters[requestType] + 1}, waiting {num}s");
			retryCounters[requestType]++;
			yield return new WaitForSecondsRealtime(num);
			actionToTake(data);
		}
		else
		{
			Debug.LogError("PM: Maximum retries attempted.");
			retryCounters[requestType] = 0;
			failureActionToTake?.Invoke();
		}
	}

	private bool HandleWebRequestFailures(UnityWebRequest request, bool retryOnConflict = false)
	{
		bool result = false;
		Debug.LogError($"PM: HandleWebRequestFailures Error: {request.responseCode} -- raw response: " + request.downloadHandler.text);
		if (request.result != UnityWebRequest.Result.ProtocolError)
		{
			result = true;
			goto IL_00a2;
		}
		long responseCode = request.responseCode;
		if (responseCode >= 500)
		{
			if (responseCode < 600)
			{
				goto IL_0066;
			}
		}
		else if (responseCode == 408 || responseCode == 429)
		{
			goto IL_0066;
		}
		bool flag = false;
		goto IL_006c;
		IL_0066:
		flag = true;
		goto IL_006c;
		IL_006c:
		if (flag || (retryOnConflict && request.responseCode == 409))
		{
			result = true;
			Debug.LogError($"PM: HTTP {request.responseCode} error: {request.error}");
		}
		goto IL_00a2;
		IL_00a2:
		return result;
	}

	private IEnumerator DoGetProgression(GetProgressionRequest data)
	{
		UnityWebRequest request = FormatWebRequest(PlayFabAuthenticatorSettings.ProgressionApiBaseUrl, data, RequestType.GetProgression);
		yield return request.SendWebRequest();
		if (request.result == UnityWebRequest.Result.Success)
		{
			int num = int.Parse(request.downloadHandler.text);
			_tracks[data.TrackId] = num;
			Debug.Log("PM: GetProgression Success: track is " + data.TrackId + " and progress is " + num);
			retryCounters[RequestType.GetProgression] = 0;
			this.OnTrackRead?.Invoke(data.TrackId, num);
		}
		else if (HandleWebRequestFailures(request))
		{
			yield return HandleWebRequestRetries(RequestType.GetProgression, data.TrackId, delegate(string x)
			{
				GetProgression(x);
			});
		}
	}

	private IEnumerator DoSetProgression(SetProgressionRequest data)
	{
		UnityWebRequest request = FormatWebRequest(PlayFabAuthenticatorSettings.ProgressionApiBaseUrl, data, RequestType.SetProgression);
		yield return request.SendWebRequest();
		if (request.result == UnityWebRequest.Result.Success)
		{
			GetProgressionResponse getProgressionResponse = JsonConvert.DeserializeObject<GetProgressionResponse>(request.downloadHandler.text);
			_tracks[data.TrackId] = getProgressionResponse.Progress;
			retryCounters[RequestType.SetProgression] = 0;
			this.OnTrackSet?.Invoke(data.TrackId, getProgressionResponse.Progress);
		}
		else if (HandleWebRequestFailures(request))
		{
			yield return HandleWebRequestRetries(RequestType.SetProgression, (data.TrackId, data.Progress), delegate((string TrackId, int Progress) x)
			{
				SetProgression(x.TrackId, x.Progress);
			});
		}
	}

	private IEnumerator DoUnlockNode(UnlockNodeRequest data)
	{
		UnityWebRequest request = FormatWebRequest(PlayFabAuthenticatorSettings.ProgressionApiBaseUrl, data, RequestType.UnlockProgressionTreeNode);
		yield return request.SendWebRequest();
		if (request.result == UnityWebRequest.Result.Success)
		{
			retryCounters[RequestType.UnlockProgressionTreeNode] = 0;
			RefreshProgressionTree();
			RefreshUserInventory();
			this.OnNodeUnlocked?.Invoke(data.TreeId, data.NodeId);
		}
		else if (HandleWebRequestFailures(request))
		{
			yield return HandleWebRequestRetries(RequestType.UnlockProgressionTreeNode, (data.TreeId, data.NodeId), delegate((string TreeId, string NodeId) x)
			{
				UnlockNode(x.TreeId, x.NodeId);
			});
		}
	}

	private IEnumerator DoIncrementSIResource(IncrementSIResourceRequest data, Action<string> OnSuccess, Action<string> OnFailure)
	{
		UnityWebRequest request = FormatWebRequest(PlayFabAuthenticatorSettings.DailyQuestsApiBaseUrl, data, RequestType.IncrementSIResource);
		yield return request.SendWebRequest();
		if (IsSuccessResponse(request.responseCode))
		{
			IncrementSIResourceResponse incrementSIResourceResponse = JsonConvert.DeserializeObject<IncrementSIResourceResponse>(request.downloadHandler.text);
			OnSuccess?.Invoke(incrementSIResourceResponse.ResourceType);
			yield break;
		}
		if (!HandleWebRequestFailures(request))
		{
			OnFailure?.Invoke(request.error);
			yield break;
		}
		yield return HandleWebRequestRetries(RequestType.IncrementSIResource, data, delegate
		{
			IncrementSIResource(data.ResourceType, OnSuccess, OnFailure);
		}, delegate
		{
			OnFailure?.Invoke(request.error);
		});
	}

	private IEnumerator DoQuestCompleteReward(SetSIQuestCompleteRequest data, Action<UserQuestsStatusResponse> OnSuccess, Action<string> OnFailure)
	{
		UnityWebRequest request = FormatWebRequest(PlayFabAuthenticatorSettings.DailyQuestsApiBaseUrl, data, RequestType.CompleteSIQuest);
		yield return request.SendWebRequest();
		if (IsSuccessResponse(request.responseCode))
		{
			GetSIQuestsStatusResponse getSIQuestsStatusResponse = JsonConvert.DeserializeObject<GetSIQuestsStatusResponse>(request.downloadHandler.text);
			OnSuccess?.Invoke(getSIQuestsStatusResponse.Result);
			yield break;
		}
		if (!HandleWebRequestFailures(request))
		{
			OnFailure?.Invoke(request.error);
			yield break;
		}
		yield return HandleWebRequestRetries(RequestType.CompleteSIQuest, data, delegate
		{
			CompleteSIQuest(data.QuestID, OnSuccess, OnFailure);
		}, delegate
		{
			OnFailure?.Invoke(request.error);
		});
	}

	private IEnumerator DoBonusCompleteReward(SetSIBonusCompleteRequest data, Action<UserQuestsStatusResponse> OnSuccess, Action<string> OnFailure)
	{
		UnityWebRequest request = FormatWebRequest(PlayFabAuthenticatorSettings.DailyQuestsApiBaseUrl, data, RequestType.CompleteSIBonus);
		yield return request.SendWebRequest();
		if (IsSuccessResponse(request.responseCode))
		{
			GetSIQuestsStatusResponse getSIQuestsStatusResponse = JsonConvert.DeserializeObject<GetSIQuestsStatusResponse>(request.downloadHandler.text);
			OnSuccess?.Invoke(getSIQuestsStatusResponse.Result);
			yield break;
		}
		if (!HandleWebRequestFailures(request))
		{
			OnFailure?.Invoke(request.error);
			yield break;
		}
		yield return HandleWebRequestRetries(RequestType.CompleteSIBonus, data, delegate
		{
			CompleteSIBonus(OnSuccess, OnFailure);
		}, delegate
		{
			OnFailure?.Invoke(request.error);
		});
	}

	private IEnumerator DoIdolCollectReward(SetSIIdolCollectRequest data, Action<UserQuestsStatusResponse> OnSuccess, Action<string> OnFailure)
	{
		UnityWebRequest request = FormatWebRequest(PlayFabAuthenticatorSettings.DailyQuestsApiBaseUrl, data, RequestType.CollectSIIdol);
		yield return request.SendWebRequest();
		if (IsSuccessResponse(request.responseCode))
		{
			GetSIQuestsStatusResponse getSIQuestsStatusResponse = JsonConvert.DeserializeObject<GetSIQuestsStatusResponse>(request.downloadHandler.text);
			OnSuccess?.Invoke(getSIQuestsStatusResponse.Result);
			yield break;
		}
		if (!HandleWebRequestFailures(request))
		{
			OnFailure?.Invoke(request.error);
			yield break;
		}
		yield return HandleWebRequestRetries(RequestType.CollectSIIdol, data, delegate
		{
			CollectSIIdol(OnSuccess, OnFailure);
		}, delegate
		{
			OnFailure?.Invoke(request.error);
		});
	}

	private IEnumerator DoGetActiveSIQuests(GetActiveSIQuestsRequest data, Action<List<RotatingQuest>> OnSuccess, Action<string> OnFailure)
	{
		UnityWebRequest request = FormatWebRequest(PlayFabAuthenticatorSettings.DailyQuestsApiBaseUrl, data, RequestType.GetActiveSIQuests);
		yield return request.SendWebRequest();
		if (IsSuccessResponse(request.responseCode))
		{
			GetActiveSIQuestsResponse getActiveSIQuestsResponse = JsonConvert.DeserializeObject<GetActiveSIQuestsResponse>(request.downloadHandler.text);
			OnSuccess?.Invoke(getActiveSIQuestsResponse.Result.Quests);
			yield break;
		}
		if (!HandleWebRequestFailures(request))
		{
			OnFailure?.Invoke(request.error);
			yield break;
		}
		yield return HandleWebRequestRetries(RequestType.GetActiveSIQuests, data, delegate
		{
			GetActiveSIQuests(OnSuccess, OnFailure);
		}, delegate
		{
			OnFailure?.Invoke(request.error);
		});
	}

	private IEnumerator DoGetSIQuestsStatus(GetSIQuestsStatusRequest data, Action<UserQuestsStatusResponse> OnSuccess, Action<string> OnFailure)
	{
		UnityWebRequest request = FormatWebRequest(PlayFabAuthenticatorSettings.DailyQuestsApiBaseUrl, data, RequestType.GetSIQuestsStatus);
		yield return request.SendWebRequest();
		if (IsSuccessResponse(request.responseCode))
		{
			GetSIQuestsStatusResponse getSIQuestsStatusResponse = JsonConvert.DeserializeObject<GetSIQuestsStatusResponse>(request.downloadHandler.text);
			OnSuccess?.Invoke(getSIQuestsStatusResponse.Result);
			yield break;
		}
		if (!HandleWebRequestFailures(request))
		{
			OnFailure?.Invoke(request.error);
			yield break;
		}
		yield return HandleWebRequestRetries(RequestType.GetSIQuestsStatus, data, delegate
		{
			GetSIQuestStatus(OnSuccess, OnFailure);
		}, delegate
		{
			OnFailure?.Invoke(request.error);
		});
	}

	private IEnumerator DoPurchaseTechPoints(PurchaseTechPointsRequest data, Action OnSuccess, Action<string> OnFailure)
	{
		UnityWebRequest request = FormatWebRequest(PlayFabAuthenticatorSettings.DailyQuestsApiBaseUrl, data, RequestType.PurchaseTechPoints);
		yield return request.SendWebRequest();
		if (IsSuccessResponse(request.responseCode))
		{
			OnSuccess?.Invoke();
			yield break;
		}
		if (!HandleWebRequestFailures(request))
		{
			OnFailure?.Invoke(request.error);
			yield break;
		}
		yield return HandleWebRequestRetries(RequestType.PurchaseTechPoints, data, delegate
		{
			PurchaseTechPoints(data.TechPointsAmount, OnSuccess, OnFailure);
		}, delegate
		{
			OnFailure?.Invoke(request.error);
		});
	}

	private IEnumerator DoPurchaseResources(PurchaseResourcesRequest data, Action<UserInventory> OnSuccess, Action<string> OnFailure)
	{
		UnityWebRequest request = FormatWebRequest(PlayFabAuthenticatorSettings.DailyQuestsApiBaseUrl, data, RequestType.PurchaseResources);
		yield return request.SendWebRequest();
		if (IsSuccessResponse(request.responseCode))
		{
			UserInventoryResponse userInventoryResponse = JsonConvert.DeserializeObject<UserInventoryResponse>(request.downloadHandler.text);
			OnSuccess?.Invoke(userInventoryResponse.Result);
			yield break;
		}
		if (!HandleWebRequestFailures(request))
		{
			OnFailure?.Invoke(request.error);
			yield break;
		}
		yield return HandleWebRequestRetries(RequestType.PurchaseResources, data, delegate
		{
			PurchaseResources(OnSuccess, OnFailure);
		}, delegate
		{
			OnFailure?.Invoke(request.error);
		});
	}

	private IEnumerator DoPurchaseShiftCreditCapIncrease(PurchaseShiftCreditCapIncreaseRequest data)
	{
		UnityWebRequest request = FormatWebRequest(PlayFabAuthenticatorSettings.ProgressionApiBaseUrl, data, RequestType.PurchaseShiftCreditCapIncrease);
		yield return request.SendWebRequest();
		if (request.result == UnityWebRequest.Result.Success)
		{
			PurchaseShiftCreditCapIncreaseResponse purchaseShiftCreditCapIncreaseResponse = JsonConvert.DeserializeObject<PurchaseShiftCreditCapIncreaseResponse>(request.downloadHandler.text);
			retryCounters[RequestType.PurchaseShiftCreditCapIncrease] = 0;
			RefreshShinyRocksTotal();
			this.OnGetShiftCreditCapData?.Invoke(purchaseShiftCreditCapIncreaseResponse.TargetMothershipId, purchaseShiftCreditCapIncreaseResponse.CurrentShiftCreditCapIncreases, purchaseShiftCreditCapIncreaseResponse.CurrentShiftCreditCapIncreasesMax);
			this.OnPurchaseShiftCreditCapIncrease?.Invoke(obj: true);
		}
		else if (request.responseCode == 400 && request.downloadHandler.text == "User Already Has Purchased Max Shift Credit Cap")
		{
			this.OnPurchaseShiftCreditCapIncrease?.Invoke(obj: false);
		}
		else if (HandleWebRequestFailures(request, retryOnConflict: true))
		{
			yield return HandleWebRequestRetries(RequestType.PurchaseShiftCreditCapIncrease, data, delegate
			{
				PurchaseShiftCreditCapIncreaseInternal(request.responseCode == 409);
			});
		}
	}

	private IEnumerator DoPurchaseShiftCredit(PurchaseShiftCreditRequest data)
	{
		UnityWebRequest request = FormatWebRequest(PlayFabAuthenticatorSettings.ProgressionApiBaseUrl, data, RequestType.PurchaseShiftCredit);
		yield return request.SendWebRequest();
		if (request.result == UnityWebRequest.Result.Success)
		{
			PurchaseShiftCreditResponse purchaseShiftCreditResponse = JsonConvert.DeserializeObject<PurchaseShiftCreditResponse>(request.downloadHandler.text);
			retryCounters[RequestType.PurchaseShiftCredit] = 0;
			RefreshShinyRocksTotal();
			this.OnGetShiftCredit?.Invoke(purchaseShiftCreditResponse.TargetMothershipId, purchaseShiftCreditResponse.CurrentShiftCredits);
			this.OnPurchaseShiftCredit?.Invoke(obj: true);
			GRPlayer local = GRPlayer.GetLocal();
			if (local != null)
			{
				local.SendCreditsRefilledTelemetry(100, purchaseShiftCreditResponse.CurrentShiftCredits);
			}
		}
		else if (request.responseCode == 400 && request.downloadHandler.text == "User Already at Max Shift Credit")
		{
			this.OnPurchaseShiftCredit?.Invoke(obj: false);
		}
		else if (HandleWebRequestFailures(request, retryOnConflict: true))
		{
			yield return HandleWebRequestRetries(RequestType.PurchaseShiftCredit, data, delegate
			{
				PurchaseShiftCreditInternal(request.responseCode == 409);
			});
		}
	}

	private IEnumerator DoGetShiftCredit(GetShiftCreditRequest data)
	{
		UnityWebRequest request = FormatWebRequest(PlayFabAuthenticatorSettings.ProgressionApiBaseUrl, data, RequestType.GetShiftCredit);
		yield return request.SendWebRequest();
		if (request.result == UnityWebRequest.Result.Success)
		{
			ShiftCreditResponse shiftCreditResponse = JsonConvert.DeserializeObject<ShiftCreditResponse>(request.downloadHandler.text);
			retryCounters[RequestType.GetShiftCredit] = 0;
			this.OnGetShiftCredit?.Invoke(shiftCreditResponse.TargetMothershipId, shiftCreditResponse.CurrentShiftCredits);
			this.OnGetShiftCreditCapData?.Invoke(shiftCreditResponse.TargetMothershipId, shiftCreditResponse.CurrentShiftCreditCapIncreases, shiftCreditResponse.CurrentShiftCreditCapIncreasesMax);
		}
		else if (HandleWebRequestFailures(request))
		{
			yield return HandleWebRequestRetries(RequestType.GetShiftCredit, data, delegate(GetShiftCreditRequest x)
			{
				GetShiftCredit(x.TargetMothershipId);
			});
		}
	}

	private IEnumerator DoGetJuicerStatus(GetJuicerStatusRequest data)
	{
		UnityWebRequest request = FormatWebRequest(PlayFabAuthenticatorSettings.ProgressionApiBaseUrl, data, RequestType.GetJuicerStatus);
		yield return request.SendWebRequest();
		if (request.result == UnityWebRequest.Result.Success)
		{
			retryCounters[RequestType.GetJuicerStatus] = 0;
			JuicerStatusResponse obj = JsonConvert.DeserializeObject<JuicerStatusResponse>(request.downloadHandler.text);
			this.OnJucierStatusUpdated?.Invoke(obj);
		}
		else if (HandleWebRequestFailures(request, retryOnConflict: true))
		{
			yield return HandleWebRequestRetries(RequestType.GetJuicerStatus, data, delegate
			{
				GetJuicerStatusInternal(request.responseCode == 409);
			});
		}
	}

	private IEnumerator DoDepositCore(DepositCoreRequest data)
	{
		UnityWebRequest request = FormatWebRequest(PlayFabAuthenticatorSettings.ProgressionApiBaseUrl, data, RequestType.DepositCore);
		yield return request.SendWebRequest();
		if (request.result == UnityWebRequest.Result.Success)
		{
			retryCounters[RequestType.DepositCore] = 0;
			if (data.CoreBeingDeposited == CoreType.ChaosSeed)
			{
				this.OnChaosDepositSuccess?.Invoke(obj: true);
				GetJuicerStatus();
			}
			else
			{
				DepositCoreResponse depositCoreResponse = JsonConvert.DeserializeObject<DepositCoreResponse>(request.downloadHandler.text);
				this.OnGetShiftCredit?.Invoke(data.MothershipId, depositCoreResponse.CurrentShiftCredits);
			}
		}
		else if (request.responseCode == 400 && request.downloadHandler.text == "DepositGRCore already at seed cap")
		{
			if (data.CoreBeingDeposited == CoreType.ChaosSeed)
			{
				this.OnChaosDepositSuccess?.Invoke(obj: false);
				GetJuicerStatus();
			}
		}
		else if (HandleWebRequestFailures(request, retryOnConflict: true))
		{
			yield return HandleWebRequestRetries(RequestType.DepositCore, data, delegate(DepositCoreRequest x)
			{
				DepositCoreInternal(x.CoreBeingDeposited, request.responseCode == 409);
			});
		}
	}

	private IEnumerator DoPurchaseOverdrive(PurchaseOverdriveRequest data)
	{
		UnityWebRequest request = FormatWebRequest(PlayFabAuthenticatorSettings.ProgressionApiBaseUrl, data, RequestType.PurchaseOverdrive);
		yield return request.SendWebRequest();
		if (request.result == UnityWebRequest.Result.Success)
		{
			retryCounters[RequestType.PurchaseOverdrive] = 0;
			GetJuicerStatus();
			RefreshShinyRocksTotal();
			this.OnPurchaseOverdrive?.Invoke(obj: true);
		}
		else if (request.responseCode == 400 && (request.downloadHandler.text == "User Already At Overdrive Cap" || request.downloadHandler.text == "User would exceed Overdrive Cap"))
		{
			this.OnPurchaseOverdrive?.Invoke(obj: false);
		}
		else if (HandleWebRequestFailures(request, retryOnConflict: true))
		{
			yield return HandleWebRequestRetries(RequestType.PurchaseOverdrive, data, delegate
			{
				PurchaseOverdriveInternal(request.responseCode == 409);
			});
		}
	}

	private IEnumerator DoSubtractShiftCredit(SubtractShiftCreditRequest data)
	{
		UnityWebRequest request = FormatWebRequest(PlayFabAuthenticatorSettings.ProgressionApiBaseUrl, data, RequestType.SubtractShiftCredit);
		yield return request.SendWebRequest();
		if (request.result == UnityWebRequest.Result.Success)
		{
			ShiftCreditResponse shiftCreditResponse = JsonConvert.DeserializeObject<ShiftCreditResponse>(request.downloadHandler.text);
			retryCounters[RequestType.SubtractShiftCredit] = 0;
			this.OnGetShiftCredit?.Invoke(data.MothershipId, shiftCreditResponse.CurrentShiftCredits);
			this.OnGetShiftCreditCapData?.Invoke(shiftCreditResponse.TargetMothershipId, shiftCreditResponse.CurrentShiftCreditCapIncreases, shiftCreditResponse.CurrentShiftCreditCapIncreasesMax);
		}
		else if (HandleWebRequestFailures(request, retryOnConflict: true))
		{
			yield return HandleWebRequestRetries(RequestType.SubtractShiftCredit, data, delegate
			{
				SubtractShiftCreditInternal(data.ShiftCreditToRemove, request.responseCode == 409);
			});
		}
	}

	private IEnumerator DoAdvanceDockWristUpgradeLevel(AdvanceDockWristUpgradeRequest data)
	{
		UnityWebRequest request = FormatWebRequest(PlayFabAuthenticatorSettings.ProgressionApiBaseUrl, data, RequestType.AdvanceDockWristUpgrade);
		yield return request.SendWebRequest();
		if (request.result == UnityWebRequest.Result.Success)
		{
			DockWristStatusResponse obj = JsonConvert.DeserializeObject<DockWristStatusResponse>(request.downloadHandler.text);
			retryCounters[RequestType.AdvanceDockWristUpgrade] = 0;
			this.OnDockWristStatusUpdated?.Invoke(obj);
		}
		else if (HandleWebRequestFailures(request, retryOnConflict: true))
		{
			yield return HandleWebRequestRetries(RequestType.AdvanceDockWristUpgrade, data, delegate
			{
				AdvanceDockWristUpgradeLevelInternal(data.Upgrade, request.responseCode == 409);
			});
		}
	}

	private IEnumerator DoGetDockWristUpgradeStatus(DockWristUpgradeStatusRequest data)
	{
		UnityWebRequest request = FormatWebRequest(PlayFabAuthenticatorSettings.ProgressionApiBaseUrl, data, RequestType.GetDockWristUpgradeStatus);
		yield return request.SendWebRequest();
		if (request.result == UnityWebRequest.Result.Success)
		{
			DockWristStatusResponse obj = JsonConvert.DeserializeObject<DockWristStatusResponse>(request.downloadHandler.text);
			retryCounters[RequestType.GetDockWristUpgradeStatus] = 0;
			this.OnDockWristStatusUpdated?.Invoke(obj);
		}
		else if (HandleWebRequestFailures(request))
		{
			yield return HandleWebRequestRetries(RequestType.GetDockWristUpgradeStatus, data, delegate
			{
				GetDockWristUpgradeStatus();
			});
		}
	}

	private IEnumerator DoPurchaseDrillUpgrade(PurchaseDrillUpgradeRequest data)
	{
		UnityWebRequest request = FormatWebRequest(PlayFabAuthenticatorSettings.ProgressionApiBaseUrl, data, RequestType.PurchaseDrillUpgrade);
		yield return request.SendWebRequest();
		if (request.result == UnityWebRequest.Result.Success)
		{
			retryCounters[RequestType.PurchaseDrillUpgrade] = 0;
			RefreshUserInventory();
			this.OnNodeUnlocked?.Invoke("", "");
			if (data.Upgrade == DrillUpgradeLevel.Base)
			{
				GRPlayer local = GRPlayer.GetLocal();
				if (local != null)
				{
					local.SendPodUpgradeTelemetry(DrillUpgradeLevel.Base.ToString(), 0, 2500, 0);
				}
			}
		}
		else if (HandleWebRequestFailures(request))
		{
			yield return HandleWebRequestRetries(RequestType.PurchaseDrillUpgrade, data, delegate
			{
				PurchaseDrillUpgrade(data.Upgrade);
			});
		}
	}

	private IEnumerator DoRecycleTool(RecycleToolRequest data)
	{
		UnityWebRequest request = FormatWebRequest(PlayFabAuthenticatorSettings.ProgressionApiBaseUrl, data, RequestType.RecycleTool);
		yield return request.SendWebRequest();
		if (request.result == UnityWebRequest.Result.Success)
		{
			ShiftCreditResponse shiftCreditResponse = JsonConvert.DeserializeObject<ShiftCreditResponse>(request.downloadHandler.text);
			retryCounters[RequestType.RecycleTool] = 0;
			this.OnGetShiftCredit?.Invoke(data.MothershipId, shiftCreditResponse.CurrentShiftCredits);
			this.OnGetShiftCreditCapData?.Invoke(shiftCreditResponse.TargetMothershipId, shiftCreditResponse.CurrentShiftCreditCapIncreases, shiftCreditResponse.CurrentShiftCreditCapIncreasesMax);
		}
		else if (HandleWebRequestFailures(request))
		{
			yield return HandleWebRequestRetries(RequestType.RecycleTool, data, delegate
			{
				RecycleTool(data.ToolBeingRecycled, data.NumberOfPlayers);
			});
		}
	}

	private IEnumerator DoStartOfShift(StartOfShiftRequest data)
	{
		UnityWebRequest request = FormatWebRequest(PlayFabAuthenticatorSettings.ProgressionApiBaseUrl, data, RequestType.StartOfShift);
		yield return request.SendWebRequest();
		if (request.result == UnityWebRequest.Result.Success)
		{
			retryCounters[RequestType.StartOfShift] = 0;
		}
		else if (HandleWebRequestFailures(request))
		{
			yield return HandleWebRequestRetries(RequestType.StartOfShift, data, delegate
			{
				StartOfShift(data.ShiftId, data.CoresRequired, data.NumberOfPlayers, data.Depth);
			});
		}
	}

	private IEnumerator DoEndOfShiftReward(EndOfShiftRewardRequest data)
	{
		UnityWebRequest request = FormatWebRequest(PlayFabAuthenticatorSettings.ProgressionApiBaseUrl, data, RequestType.EndOfShiftReward);
		yield return request.SendWebRequest();
		if (request.result == UnityWebRequest.Result.Success)
		{
			ShiftCreditResponse shiftCreditResponse = JsonConvert.DeserializeObject<ShiftCreditResponse>(request.downloadHandler.text);
			retryCounters[RequestType.EndOfShiftReward] = 0;
			this.OnGetShiftCredit?.Invoke(data.MothershipId, shiftCreditResponse.CurrentShiftCredits);
			this.OnGetShiftCreditCapData?.Invoke(shiftCreditResponse.TargetMothershipId, shiftCreditResponse.CurrentShiftCreditCapIncreases, shiftCreditResponse.CurrentShiftCreditCapIncreasesMax);
		}
		else if ((request.responseCode != 400 || !(request.error == "EndOfShiftReward Unknown Shift or Mothership Failure.")) && HandleWebRequestFailures(request, retryOnConflict: true))
		{
			yield return HandleWebRequestRetries(RequestType.EndOfShiftReward, data, delegate
			{
				EndOfShiftRewardInternal(data.ShiftId, request.responseCode == 409);
			});
		}
	}

	private IEnumerator DoGetGhostReactorStats(GhostReactorStatsRequest data)
	{
		UnityWebRequest request = FormatWebRequest(PlayFabAuthenticatorSettings.ProgressionApiBaseUrl, data, RequestType.GetGhostReactorStats);
		yield return request.SendWebRequest();
		if (request.result == UnityWebRequest.Result.Success)
		{
			GhostReactorStatsResponse obj = JsonConvert.DeserializeObject<GhostReactorStatsResponse>(request.downloadHandler.text);
			retryCounters[RequestType.GetGhostReactorStats] = 0;
			this.OnGhostReactorStatsUpdated?.Invoke(obj);
		}
		else if (HandleWebRequestFailures(request))
		{
			yield return HandleWebRequestRetries(RequestType.GetGhostReactorStats, data, delegate
			{
				GetGhostReactorStats();
			});
		}
	}

	private IEnumerator DoGetGhostReactorInventory(GhostReactorInventoryRequest data)
	{
		UnityWebRequest request = FormatWebRequest(PlayFabAuthenticatorSettings.ProgressionApiBaseUrl, data, RequestType.GetGhostReactorInventory);
		yield return request.SendWebRequest();
		if (request.result == UnityWebRequest.Result.Success)
		{
			GhostReactorInventoryResponse obj = JsonConvert.DeserializeObject<GhostReactorInventoryResponse>(request.downloadHandler.text);
			retryCounters[RequestType.GetGhostReactorInventory] = 0;
			this.OnGhostReactorInventoryUpdated?.Invoke(obj);
		}
		else if (HandleWebRequestFailures(request))
		{
			yield return HandleWebRequestRetries(RequestType.GetGhostReactorInventory, data, delegate
			{
				GetGhostReactorInventory();
			});
		}
	}

	private IEnumerator DoSetGhostReactorInventory(SetGhostReactorInventoryRequest data)
	{
		UnityWebRequest request = FormatWebRequest(PlayFabAuthenticatorSettings.ProgressionApiBaseUrl, data, RequestType.SetGhostReactorInventory);
		yield return request.SendWebRequest();
		if (request.result == UnityWebRequest.Result.Success)
		{
			retryCounters[RequestType.SetGhostReactorInventory] = 0;
		}
		else if (HandleWebRequestFailures(request, retryOnConflict: true))
		{
			yield return HandleWebRequestRetries(RequestType.SetGhostReactorInventory, data, delegate
			{
				SetGhostReactorInventoryInternal(data.InventoryJson, request.responseCode == 409);
			});
		}
	}

	private bool IsSuccessResponse(long code)
	{
		if (code >= 200)
		{
			return code < 300;
		}
		return false;
	}

	private UnityWebRequest FormatWebRequest<T>(string url, T pendingRequest, RequestType type)
	{
		string text = "";
		byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(pendingRequest));
		switch (type)
		{
		case RequestType.GetProgression:
			text = "/api/GetProgression";
			break;
		case RequestType.SetProgression:
			text = "/api/SetProgression";
			break;
		case RequestType.UnlockProgressionTreeNode:
			text = "/api/UnlockProgressionTreeNode";
			break;
		case RequestType.GetSIQuestsStatus:
			text = "/api/GetSIQuestsStatus";
			break;
		case RequestType.GetActiveSIQuests:
			text = "/api/GetActiveSIQuests";
			break;
		case RequestType.IncrementSIResource:
			text = "/api/IncrementSIResource";
			break;
		case RequestType.CompleteSIQuest:
			text = "/api/SetSIQuestComplete";
			break;
		case RequestType.CompleteSIBonus:
			text = "/api/SetSIBonusComplete";
			break;
		case RequestType.CollectSIIdol:
			text = "/api/SetSIIdolCollect";
			break;
		case RequestType.ResetSIQuestsStatus:
			text = "/api/ResetSIQuestsStatus";
			break;
		case RequestType.PurchaseTechPoints:
			text = "/api/PurchaseTechPoints";
			break;
		case RequestType.PurchaseResources:
			text = "/api/PurchaseResources";
			break;
		case RequestType.PurchaseShiftCreditCapIncrease:
			text = "/api/PurchaseShiftCreditCapIncrease";
			break;
		case RequestType.PurchaseShiftCredit:
			text = "/api/PurchaseShiftCredit";
			break;
		case RequestType.GetJuicerStatus:
			text = "/api/GetJuicerStatus";
			break;
		case RequestType.DepositCore:
			text = "/api/DepositGRCore";
			break;
		case RequestType.PurchaseOverdrive:
			text = "/api/PurchaseOverdrive";
			break;
		case RequestType.GetShiftCredit:
			text = "/api/GetShiftCredit";
			break;
		case RequestType.SubtractShiftCredit:
			text = "/api/SubtractShiftCredit";
			break;
		case RequestType.AdvanceDockWristUpgrade:
			text = "/api/AdvanceDockWristUpgrade";
			break;
		case RequestType.GetDockWristUpgradeStatus:
			text = "/api/GetDockWristUpgradeStatus";
			break;
		case RequestType.PurchaseDrillUpgrade:
			text = "/api/PurchaseDrillUpgrade";
			break;
		case RequestType.RecycleTool:
			text = "/api/RecycleTool";
			break;
		case RequestType.StartOfShift:
			text = "/api/StartOfShift";
			break;
		case RequestType.EndOfShiftReward:
			text = "/api/EndOfShiftReward";
			break;
		case RequestType.GetGhostReactorStats:
			text = "/api/GetGhostReactorStats";
			break;
		case RequestType.GetGhostReactorInventory:
			text = "/api/GetGhostReactorInventory";
			break;
		case RequestType.SetGhostReactorInventory:
			text = "/api/SetGhostReactorInventory";
			break;
		}
		UnityWebRequest unityWebRequest = new UnityWebRequest(url + text, "POST");
		unityWebRequest.uploadHandler = new UploadHandlerRaw(bytes);
		unityWebRequest.downloadHandler = new DownloadHandlerBuffer();
		unityWebRequest.SetRequestHeader("Content-Type", "application/json");
		return unityWebRequest;
	}

	private void OnGetTrees(GetProgressionTreesForPlayerResponse response)
	{
		if (response?.Results == null)
		{
			return;
		}
		_trees.Clear();
		foreach (UserHydratedProgressionTreeResponse result in response.Results)
		{
			UserHydratedProgressionTreeResponse userHydratedProgressionTreeResponse = new UserHydratedProgressionTreeResponse();
			userHydratedProgressionTreeResponse.Tree = result.Tree;
			userHydratedProgressionTreeResponse.Track = result.Track;
			userHydratedProgressionTreeResponse.Nodes = result.Nodes;
			_trees[result.Tree.name] = userHydratedProgressionTreeResponse;
		}
		this.OnTreeUpdated?.Invoke();
	}

	private void OnGetInventory(MothershipGetInventoryResponse response)
	{
		if (response?.Results == null)
		{
			return;
		}
		_inventory.Clear();
		foreach (KeyValuePair<string, MothershipPlayerInventorySummary> result in response.Results)
		{
			if (result.Value?.entitlements == null)
			{
				continue;
			}
			foreach (MothershipInventoryItemSummary entitlement in result.Value.entitlements)
			{
				string key = entitlement.name?.Trim();
				_inventory[key] = new MothershipItemSummary
				{
					EntitlementId = entitlement.entitlement_id,
					InGameId = entitlement.in_game_id,
					Name = entitlement.name,
					Quantity = entitlement.quantity
				};
			}
		}
		this.OnInventoryUpdated?.Invoke();
	}

	public int GetShinyRocksTotal()
	{
		if (CosmeticsController.instance != null)
		{
			return CosmeticsController.instance.CurrencyBalance;
		}
		return 0;
	}

	public void RefreshShinyRocksTotal()
	{
		if (CosmeticsController.instance != null)
		{
			CosmeticsController.instance.GetCurrencyBalance();
		}
	}

	public static void GetMothershipFailure(MothershipError callError, int errorCode)
	{
		Debug.LogError("Progression: GetMothershipFailure: " + callError.MothershipErrorCode + ":" + callError.Message);
	}
}

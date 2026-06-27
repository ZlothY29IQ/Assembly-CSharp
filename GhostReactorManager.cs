using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Fusion;
using GorillaExtensions;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

[NetworkBehaviourWeaved(0)]
public class GhostReactorManager : NetworkComponent, IGameEntityZoneComponent
{
	public enum RPC
	{
		ApplyCollectItem,
		ApplyChargeTool,
		ApplyDepositCurrency,
		ApplyPlayerRevived,
		GrantPlayerShield,
		RequestFireProjectile,
		ApplyShiftStart,
		ApplyShiftEnd,
		ToolPurchaseResponse,
		ApplyBreakableBroken,
		EntityEnteredDropZone,
		PromotionBotResponse,
		DistillItem,
		ApplySentientCoreDestination,
		Handprint,
		ApplyRecycleItem,
		ApplRecycleScanItem,
		SeedExtractorAction,
		ToolUpgradeStationAction,
		SendMothershipId,
		RefreshShiftCredit
	}

	public enum GRPlayerAction
	{
		ButtonShiftStart,
		DelveDeeper,
		DelveState,
		ShuttleOpen,
		ShuttleClose,
		ShuttleLaunch,
		ShuttleArrive,
		ShuttleTargetLevelUp,
		ShuttleTargetLevelDown,
		SetPodLevel,
		SetPodChassisLevel,
		SeedExtractorOpenStation,
		SeedExtractorCloseStation,
		SeedExtractorCardSwipeFail,
		SeedExtractorTryDepositSeed,
		SeedExtractorDepositSeedSucceeded,
		SeedExtractorDepositSeedFailed,
		DEBUG_ResetDepth,
		DEBUG_DelveDeeper,
		DEBUG_DelveShallower
	}

	public enum ToolPurchaseActionV2
	{
		RequestPurchaseAuthority,
		SelectShelfAndItem,
		NotifyPurchaseFail,
		NotifyPurchaseSuccess,
		RequestStationExclusivityAuthority,
		SetToolStationActivePlayer,
		SetHandleAndSelectionWheelPosition,
		SetToolStationHackedDebug
	}

	public enum ToolPurchaseStationAction
	{
		ShiftLeft,
		ShiftRight,
		TryPurchase
	}

	public enum ToolPurchaseStationResponse
	{
		SelectionUpdate,
		PurchaseSucceeded,
		PurchaseFailed
	}

	private const string EVENT_CORE_COLLECTED = "GRCollectCore";

	private const string EVENT_ENEMY_KILLED = "GRKillEnemy";

	public const string EVENT_BREAKABLE_BROKEN = "GRSmashBreakable";

	public const string EVENT_ENEMY_ARMOR_BREAK = "GRArmorBreak";

	public const string NETWORK_ROOM_GR_DEPTH = "ghostReactorDepth";

	public const int GHOSTREACTOR_ZONE_ID = 5;

	public const GTZone GT_ZONE_GHOSTREACTOR = GTZone.ghostReactor;

	public GameEntityManager gameEntityManager;

	public GameAgentManager gameAgentManager;

	public GRNoiseEventManager noiseEventManager;

	public PhotonView photonView;

	public GhostReactor reactor;

	public CallLimitersList<CallLimiter, RPC> m_RpcSpamChecks = new CallLimitersList<CallLimiter, RPC>();

	private const float HandprintThrottleTime = 0.25f;

	private float LastHandprintTime;

	private Coroutine activeSpawnSectionEntitiesCoroutine;

	private WaitForSeconds spawnSectionEntitiesWait = new WaitForSeconds(0.1f);

	private static List<GameEntityId> tempEntitiesToDestroy = new List<GameEntityId>();

	private GameEntity cachedBossEntity;

	public GRToolUpgradeStation upgradeStation;

	public static bool entityDebugEnabled = false;

	public static bool noiseDebugEnabled = false;

	public static bool bayUnlockEnabled = false;

	public static bool AggroDisabled => false;

	protected override void Awake()
	{
		base.Awake();
		noiseEventManager = GetComponent<GRNoiseEventManager>();
	}

	internal override void OnEnable()
	{
		NetworkBehaviourUtils.InternalOnEnable(this);
		base.OnEnable();
	}

	internal override void OnDisable()
	{
		NetworkBehaviourUtils.InternalOnDisable(this);
		base.OnDisable();
	}

	public bool IsAuthority()
	{
		return gameEntityManager.IsAuthority();
	}

	private bool IsAuthorityPlayer(NetPlayer player)
	{
		return gameEntityManager.IsAuthorityPlayer(player);
	}

	private bool IsAuthorityPlayer(Player player)
	{
		return gameEntityManager.IsAuthorityPlayer(player);
	}

	private Player GetAuthorityPlayer()
	{
		return gameEntityManager.GetAuthorityPlayer();
	}

	public bool IsZoneActive()
	{
		return gameEntityManager.IsZoneActive();
	}

	public bool IsPositionInZone(Vector3 pos)
	{
		return gameEntityManager.IsPositionInManagerBounds(pos);
	}

	public bool IsValidClientRPC(Player sender)
	{
		return gameEntityManager.IsValidClientRPC(sender);
	}

	public bool IsValidClientRPC(Player sender, int entityNetId)
	{
		return gameEntityManager.IsValidClientRPC(sender, entityNetId);
	}

	public bool IsValidClientRPC(Player sender, int entityNetId, Vector3 pos)
	{
		return gameEntityManager.IsValidClientRPC(sender, entityNetId, pos);
	}

	public bool IsValidClientRPC(Player sender, Vector3 pos)
	{
		return gameEntityManager.IsValidClientRPC(sender, pos);
	}

	public bool IsValidAuthorityRPC(Player sender)
	{
		return gameEntityManager.IsValidAuthorityRPC(sender);
	}

	public bool IsValidAuthorityRPC(Player sender, int entityNetId)
	{
		return gameEntityManager.IsValidAuthorityRPC(sender, entityNetId);
	}

	public bool IsValidAuthorityRPC(Player sender, int entityNetId, Vector3 pos)
	{
		return gameEntityManager.IsValidAuthorityRPC(sender, entityNetId, pos);
	}

	public bool IsValidAuthorityRPC(Player sender, Vector3 pos)
	{
		return gameEntityManager.IsValidAuthorityRPC(sender, pos);
	}

	public static GhostReactorManager Get(GameEntity gameEntity)
	{
		if (gameEntity == null || gameEntity.manager == null)
		{
			return null;
		}
		return gameEntity.manager.ghostReactorManager;
	}

	public void RefreshShiftCredit()
	{
	}

	[PunRPC]
	public void RefreshShiftCreditRPC(PhotonMessageInfo info)
	{
		if (IsValidAuthorityRPC(info.Sender) && !m_RpcSpamChecks.IsSpamming(RPC.RefreshShiftCredit))
		{
			GRPlayer gRPlayer = GRPlayer.Get(info.Sender.ActorNumber);
			if (!gRPlayer.IsNull() && !gRPlayer.mothershipId.IsNullOrEmpty())
			{
				ProgressionManager.Instance.GetShiftCredit(gRPlayer.mothershipId);
			}
		}
	}

	public void SendMothershipId()
	{
		_ = MothershipClientContext.MothershipId;
	}

	[PunRPC]
	public void SendMothershipIdRPC(string mothershipId, PhotonMessageInfo info)
	{
		if (IsValidAuthorityRPC(info.Sender) && !m_RpcSpamChecks.IsSpamming(RPC.SendMothershipId) && mothershipId.Length <= 40)
		{
			GRPlayer gRPlayer = GRPlayer.Get(info.Sender.ActorNumber);
			if (!gRPlayer.IsNull() && gRPlayer.mothershipId.IsNullOrEmpty() && gRPlayer.mothershipId.IsNullOrEmpty())
			{
				gRPlayer.mothershipId = mothershipId.Trim();
				ProgressionManager.Instance.GetShiftCredit(gRPlayer.mothershipId);
			}
		}
	}

	public void RequestCollectItem(GameEntityId collectibleEntityId, GameEntityId collectorEntityId)
	{
		photonView.RPC("RequestCollectItemRPC", GetAuthorityPlayer(), gameEntityManager.GetNetIdFromEntityId(collectibleEntityId), gameEntityManager.GetNetIdFromEntityId(collectorEntityId));
	}

	public void RequestDepositCollectible(GameEntityId collectibleEntityId)
	{
		if (IsAuthority())
		{
			GameEntity gameEntity = gameEntityManager.GetGameEntity(collectibleEntityId);
			if (gameEntity != null)
			{
				photonView.RPC("ApplyCollectItemRPC", RpcTarget.All, gameEntityManager.GetNetIdFromEntityId(collectibleEntityId), -1, gameEntity.lastHeldByActorNumber);
			}
		}
	}

	[PunRPC]
	public void RequestCollectItemRPC(int collectibleEntityNetId, int collectorEntityNetId, PhotonMessageInfo info)
	{
		if (IsValidAuthorityRPC(info.Sender, collectibleEntityNetId))
		{
			GRPlayer gRPlayer = GRPlayer.Get(info.Sender.ActorNumber);
			if (!gRPlayer.IsNull() && gRPlayer.requestCollectItemLimiter.CheckCallTime(Time.unscaledTime) && gameEntityManager.IsValidNetId(collectorEntityNetId) && (gameEntityManager.IsEntityNearEntity(collectibleEntityNetId, collectorEntityNetId) ? true : false))
			{
				photonView.RPC("ApplyCollectItemRPC", RpcTarget.All, collectibleEntityNetId, collectorEntityNetId, info.Sender.ActorNumber);
			}
		}
	}

	[PunRPC]
	public void ApplyCollectItemRPC(int collectibleEntityNetId, int collectorEntityNetId, int collectingPlayerActorNumber, PhotonMessageInfo info)
	{
		if (!IsValidClientRPC(info.Sender, collectibleEntityNetId) || reactor == null || m_RpcSpamChecks.IsSpamming(RPC.ApplyCollectItem))
		{
			return;
		}
		GRPlayer gRPlayer = GRPlayer.Get(collectingPlayerActorNumber);
		if (gRPlayer == null)
		{
			return;
		}
		GameEntityId entityIdFromNetId = gameEntityManager.GetEntityIdFromNetId(collectibleEntityNetId);
		GameEntity gameEntity = gameEntityManager.GetGameEntity(entityIdFromNetId);
		if (gameEntity == null)
		{
			return;
		}
		GRCollectible component = gameEntity.GetComponent<GRCollectible>();
		if (component == null)
		{
			return;
		}
		GameEntityId entityIdFromNetId2 = gameEntityManager.GetEntityIdFromNetId(collectorEntityNetId);
		GameEntity gameEntity2 = gameEntityManager.GetGameEntity(entityIdFromNetId2);
		if (gameEntity2 != null)
		{
			GRToolCollector component2 = gameEntity2.GetComponent<GRToolCollector>();
			if (component2 != null && component2.tool != null)
			{
				component2.PerformCollection(component);
			}
		}
		else
		{
			ProgressionManager.Instance.DepositCore(component.type);
			ReportCoreCollection(gRPlayer, component.type);
			int count = reactor.vrRigs.Count;
			int coreValue = component.energyValue / 4;
			for (int i = 0; i < count; i++)
			{
				GRPlayer.Get(reactor.vrRigs[i]).IncrementCoresCollectedGroup(coreValue);
			}
			gRPlayer.IncrementCoresCollectedPlayer(coreValue);
		}
		if (gameEntity != null && component != null)
		{
			component.InvokeOnCollected();
		}
		gameEntityManager.DestroyItemLocal(entityIdFromNetId);
	}

	public void RequestApplySeedExtractorState(int coreCount, int coresProcessedByOverdrive, int researchPoints, float coreProcessingPercentage, float overdriveSupply)
	{
		photonView.RPC("RequestApplySeedExtractorStateRPC", GetAuthorityPlayer(), coreCount, coresProcessedByOverdrive, researchPoints, coreProcessingPercentage, overdriveSupply);
	}

	[PunRPC]
	public void RequestApplySeedExtractorStateRPC(int coreCount, int coresProcessedByOverdrive, int researchPoints, float coreProcessingPercentage, float overdriveSupply, PhotonMessageInfo info)
	{
		if (IsValidAuthorityRPC(info.Sender) && !m_RpcSpamChecks.IsSpamming(RPC.SeedExtractorAction) && coreCount >= 0 && coresProcessedByOverdrive >= 0 && researchPoints >= 0 && float.IsFinite(coreProcessingPercentage) && float.IsFinite(overdriveSupply) && info.Sender.ActorNumber == reactor.seedExtractor.CurrentPlayerActorNumber)
		{
			photonView.RPC("ApplySeedExtractorStateRPC", RpcTarget.All, info.Sender.ActorNumber, coreCount, coresProcessedByOverdrive, researchPoints, coreProcessingPercentage, overdriveSupply);
		}
	}

	[PunRPC]
	public void ApplySeedExtractorStateRPC(int playerActorNumber, int coreCount, int coresProcessedByOverdrive, int researchPoints, float coreProcessingPercentage, float overdriveSupply, PhotonMessageInfo info)
	{
		if (IsValidClientRPC(info.Sender) && !m_RpcSpamChecks.IsSpamming(RPC.SeedExtractorAction) && coreCount >= 0 && coresProcessedByOverdrive >= 0 && researchPoints >= 0 && float.IsFinite(coreProcessingPercentage) && float.IsFinite(overdriveSupply) && reactor != null && reactor.seedExtractor != null)
		{
			reactor.seedExtractor.ApplyState(playerActorNumber, coreCount, coresProcessedByOverdrive, researchPoints, coreProcessingPercentage, overdriveSupply);
		}
	}

	public void RequestDistillCollectible(GameEntityId collectibleEntityId, Player sender)
	{
		if (IsValidAuthorityRPC(sender))
		{
			GameEntity gameEntity = gameEntityManager.GetGameEntity(collectibleEntityId);
			if (gameEntity != null)
			{
				photonView.RPC("DistillItemRPC", RpcTarget.All, gameEntityManager.GetNetIdFromEntityId(collectibleEntityId), gameEntity.lastHeldByActorNumber);
			}
		}
	}

	[PunRPC]
	public void DistillItemRPC(int collectibleEntityNetId, int collectingPlayerActorNumber, PhotonMessageInfo info)
	{
		if (!IsValidClientRPC(info.Sender, collectibleEntityNetId) || reactor == null || m_RpcSpamChecks.IsSpamming(RPC.DistillItem) || GRPlayer.Get(collectingPlayerActorNumber) == null)
		{
			return;
		}
		GameEntityId entityIdFromNetId = gameEntityManager.GetEntityIdFromNetId(collectibleEntityNetId);
		GameEntity gameEntity = gameEntityManager.GetGameEntity(entityIdFromNetId);
		if (gameEntity == null)
		{
			return;
		}
		GRCollectible component = gameEntity.GetComponent<GRCollectible>();
		if (!(component == null))
		{
			Debug.LogWarning("Warning - NOT IMPLEMENTED - Return validating inserting core for distillery.");
			if (gameEntity != null && component != null)
			{
				component.InvokeOnCollected();
			}
			gameEntityManager.DestroyItemLocal(entityIdFromNetId);
		}
	}

	public void RequestChargeTool(GameEntityId collectorEntityId, GameEntityId targetToolId, int targetEnergyDelta = 0, bool useCollectorEnergy = true)
	{
		photonView.RPC("RequestChargeToolRPC", GetAuthorityPlayer(), gameEntityManager.GetNetIdFromEntityId(collectorEntityId), gameEntityManager.GetNetIdFromEntityId(targetToolId), targetEnergyDelta, useCollectorEnergy);
	}

	[PunRPC]
	public void RequestChargeToolRPC(int collectorEntityNetId, int targetToolNetId, int targetEnergyDelta, bool useCollectorEnergy, PhotonMessageInfo info)
	{
		if (IsValidAuthorityRPC(info.Sender) && gameEntityManager.IsValidNetId(collectorEntityNetId) && gameEntityManager.IsValidNetId(targetToolNetId) && gameEntityManager.IsEntityNearEntity(collectorEntityNetId, targetToolNetId) && GamePlayer.TryGetGamePlayer(info.Sender.ActorNumber, out var out_gamePlayer) && gameEntityManager.IsPlayerHandNearEntity(out_gamePlayer, collectorEntityNetId, isLeftHand: false, checkBothHands: true) && gameEntityManager.IsPlayerHandNearEntity(out_gamePlayer, targetToolNetId, isLeftHand: false, checkBothHands: true))
		{
			GRPlayer gRPlayer = GRPlayer.Get(info.Sender.ActorNumber);
			if (!gRPlayer.IsNull() && (gRPlayer.requestChargeToolLimiter.CheckCallTime(Time.unscaledTime) ? true : false))
			{
				photonView.RPC("ApplyChargeToolRPC", RpcTarget.All, collectorEntityNetId, targetToolNetId, targetEnergyDelta, useCollectorEnergy, info.Sender);
			}
		}
	}

	[PunRPC]
	public void ApplyChargeToolRPC(int collectorEntityNetId, int targetToolNetId, int targetEnergyDelta, bool useCollectorEnergy, Player collectingPlayer, PhotonMessageInfo info)
	{
		if (!IsValidClientRPC(info.Sender) || m_RpcSpamChecks.IsSpamming(RPC.ApplyChargeTool) || !gameEntityManager.IsValidNetId(collectorEntityNetId) || !gameEntityManager.IsValidNetId(targetToolNetId) || 1 == 0)
		{
			return;
		}
		GameEntityId entityIdFromNetId = gameEntityManager.GetEntityIdFromNetId(collectorEntityNetId);
		GameEntity gameEntity = gameEntityManager.GetGameEntity(entityIdFromNetId);
		GameEntityId entityIdFromNetId2 = gameEntityManager.GetEntityIdFromNetId(targetToolNetId);
		GameEntity gameEntity2 = gameEntityManager.GetGameEntity(entityIdFromNetId2);
		if (!(gameEntity != null) || !(gameEntity2 != null))
		{
			return;
		}
		GRToolCollector component = gameEntity.GetComponent<GRToolCollector>();
		GRTool component2 = gameEntity2.GetComponent<GRTool>();
		if (!(component != null) || !(component.tool != null) || !(component2 != null))
		{
			return;
		}
		int num = ((targetEnergyDelta != 0) ? targetEnergyDelta : 100);
		int b = Mathf.Max(component2.GetEnergyMax() - component2.energy, 0);
		int num2 = 0;
		if (!useCollectorEnergy)
		{
			num2 = Mathf.Min(num, b);
			Debug.Log($"Apply SelfCharge {num2}");
		}
		else
		{
			num2 = Mathf.Min(Mathf.Min(component.tool.energy, num), b);
		}
		if (num2 > 0)
		{
			if (useCollectorEnergy)
			{
				component.tool.SetEnergy(component.tool.energy - num2);
			}
			component2.RefillEnergy(num2, entityIdFromNetId);
			component.PlayChargeEffect(component2);
		}
	}

	public void RequestDepositCurrency(GameEntityId collectorEntityId)
	{
		photonView.RPC("RequestDepositCurrencyRPC", GetAuthorityPlayer(), gameEntityManager.GetNetIdFromEntityId(collectorEntityId));
	}

	[PunRPC]
	public void RequestDepositCurrencyRPC(int collectorEntityNetId, PhotonMessageInfo info)
	{
		if (!IsValidAuthorityRPC(info.Sender, collectorEntityNetId))
		{
			return;
		}
		GRPlayer gRPlayer = GRPlayer.Get(info.Sender.ActorNumber);
		if (!gRPlayer.IsNull() && gRPlayer.requestDepositCurrencyLimiter.CheckCallTime(Time.unscaledTime))
		{
			GameEntityId entityIdFromNetId = gameEntityManager.GetEntityIdFromNetId(collectorEntityNetId);
			gameEntityManager.GetGameEntity(entityIdFromNetId);
			if (GamePlayer.TryGetGamePlayer(info.Sender.ActorNumber, out var out_gamePlayer) && gameEntityManager.IsPlayerHandNearEntity(out_gamePlayer, collectorEntityNetId, isLeftHand: false, checkBothHands: true) && (gRPlayer.transform.position - reactor.currencyDepositor.transform.position).magnitude < 16f)
			{
				photonView.RPC("ApplyDepositCurrencyRPC", RpcTarget.All, collectorEntityNetId, info.Sender.ActorNumber);
			}
		}
	}

	[PunRPC]
	public void ApplyDepositCurrencyRPC(int collectorEntityNetId, int targetPlayerActorNumber, PhotonMessageInfo info)
	{
		if (!IsValidClientRPC(info.Sender, collectorEntityNetId) || reactor == null || m_RpcSpamChecks.IsSpamming(RPC.ApplyDepositCurrency))
		{
			return;
		}
		GRPlayer gRPlayer = GRPlayer.Get(targetPlayerActorNumber);
		if (gRPlayer == null)
		{
			return;
		}
		GameEntityId entityIdFromNetId = gameEntityManager.GetEntityIdFromNetId(collectorEntityNetId);
		GameEntity gameEntity = gameEntityManager.GetGameEntity(entityIdFromNetId);
		if (!(gameEntity != null))
		{
			return;
		}
		GRToolCollector component = gameEntity.GetComponent<GRToolCollector>();
		if (!(component != null) || !(component.tool != null))
		{
			return;
		}
		int energy = component.tool.energy;
		int energyDepositPerUse = component.energyDepositPerUse;
		if (energy >= energyDepositPerUse)
		{
			ReportCoreCollection(gRPlayer, ProgressionManager.CoreType.Core);
			int count = reactor.vrRigs.Count;
			int coreValue = energyDepositPerUse / 4;
			for (int i = 0; i < count; i++)
			{
				GRPlayer.Get(reactor.vrRigs[i]).IncrementCoresCollectedGroup(coreValue);
			}
			gRPlayer.IncrementCoresCollectedPlayer(coreValue);
			int energy2 = energy - energyDepositPerUse;
			component.tool.SetEnergy(energy2);
			reactor.RefreshScoreboards();
			ProgressionManager.Instance.DepositCore(ProgressionManager.CoreType.Core);
			component.PlayChargeEffect(reactor.currencyDepositor);
		}
	}

	public void RequestEnemyHitPlayer(GhostReactor.EnemyType type, GameEntityId hitByEntityId, GRPlayer player, Vector3 hitPosition)
	{
		photonView.RPC("ApplyEnemyHitPlayerRPC", RpcTarget.All, type, gameEntityManager.GetNetIdFromEntityId(hitByEntityId), hitPosition, Vector3.zero);
	}

	public void RequestEnemyHitPlayer(GhostReactor.EnemyType type, GameEntityId hitByEntityId, GRPlayer player, Vector3 hitPosition, Vector3 hitImpulse)
	{
		photonView.RPC("ApplyEnemyHitPlayerRPC", RpcTarget.All, type, gameEntityManager.GetNetIdFromEntityId(hitByEntityId), hitPosition, hitImpulse);
	}

	[PunRPC]
	private void ApplyEnemyHitPlayerRPC(GhostReactor.EnemyType type, int entityNetId, Vector3 hitPosition, Vector3 hitImpulse, PhotonMessageInfo info)
	{
		if (gameEntityManager.IsValidNetId(entityNetId) && hitPosition.IsValid(10000f) && hitImpulse.IsValid(10000f) && !(hitImpulse.magnitude > 50f))
		{
			GameEntityId entityIdFromNetId = gameEntityManager.GetEntityIdFromNetId(entityNetId);
			GRPlayer gRPlayer = GRPlayer.Get(info.Sender.ActorNumber);
			if (!(gRPlayer == null) && gRPlayer.applyEnemyHitLimiter.CheckCallTime(Time.unscaledTime))
			{
				OnEnemyHitPlayerInternal(type, entityIdFromNetId, gRPlayer, hitPosition, hitImpulse);
			}
		}
	}

	private void OnEnemyHitPlayerInternal(GhostReactor.EnemyType type, GameEntityId entityId, GRPlayer player, Vector3 hitPosition, Vector3 hitImpulse)
	{
		if (type == GhostReactor.EnemyType.Chaser || type == GhostReactor.EnemyType.Phantom || type == GhostReactor.EnemyType.Ranged || type == GhostReactor.EnemyType.CustomMapsEnemy)
		{
			player.OnPlayerHit(hitPosition, hitImpulse, this, entityId);
			GameHitter component = gameEntityManager.GetGameEntity(entityId).GetComponent<GameHitter>();
			if (component != null)
			{
				component.ApplyHitToPlayer(player, hitPosition);
			}
		}
	}

	public void ReportLocalPlayerHit()
	{
		base.GetView.RPC("ReportLocalPlayerHitRPC", RpcTarget.All);
	}

	[PunRPC]
	private void ReportLocalPlayerHitRPC(PhotonMessageInfo info)
	{
		GRPlayer gRPlayer = GRPlayer.Get(info.Sender.ActorNumber);
		if (!(gRPlayer == null) && gRPlayer.reportLocalHitLimiter.CheckCallTime(Time.unscaledTime))
		{
			gRPlayer.ChangePlayerState(GRPlayer.GRPlayerState.Ghost, this);
		}
	}

	public void RequestPlayerRevive(GRReviveStation reviveStation, GRPlayer player)
	{
		if ((NetworkSystem.Instance.InRoom && IsAuthority()) || !NetworkSystem.Instance.InRoom)
		{
			base.GetView.RPC("ApplyPlayerRevivedRPC", RpcTarget.All, reviveStation.Index, player.gamePlayer.rig.OwningNetPlayer.ActorNumber);
		}
	}

	[PunRPC]
	private void ApplyPlayerRevivedRPC(int reviveStationIndex, int playerActorNumber, PhotonMessageInfo info)
	{
		if (!IsValidClientRPC(info.Sender) || m_RpcSpamChecks.IsSpamming(RPC.ApplyPlayerRevived))
		{
			return;
		}
		GRPlayer gRPlayer = GRPlayer.Get(playerActorNumber);
		if (!(gRPlayer == null) && reviveStationIndex >= 0 && reviveStationIndex < reactor.reviveStations.Count)
		{
			GRReviveStation gRReviveStation = reactor.reviveStations[reviveStationIndex];
			if (!(gRReviveStation == null))
			{
				gRReviveStation.RevivePlayer(gRPlayer);
			}
		}
	}

	public void RequestPlayerStateChange(GRPlayer player, GRPlayer.GRPlayerState newState)
	{
		if (NetworkSystem.Instance.InRoom)
		{
			base.GetView.RPC("PlayerStateChangeRPC", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, player.gamePlayer.rig.OwningNetPlayer.ActorNumber, (int)newState);
		}
		else
		{
			player.ChangePlayerState(newState, this);
		}
	}

	[PunRPC]
	private void PlayerStateChangeRPC(int playerResponsibleNumber, int playerActorNumber, int newState, PhotonMessageInfo info)
	{
		bool flag = IsValidClientRPC(info.Sender);
		bool num = newState == 1 && info.Sender.ActorNumber == playerActorNumber;
		bool flag2 = newState == 0 && flag;
		if (!(num || flag2))
		{
			return;
		}
		GRPlayer gRPlayer = GRPlayer.Get(playerActorNumber);
		GRPlayer gRPlayer2 = GRPlayer.Get(info.Sender.ActorNumber);
		if (gRPlayer == null || gRPlayer2.IsNull() || !gRPlayer2.playerStateChangeLimiter.CheckCallTime(Time.unscaledTime))
		{
			return;
		}
		if (newState == 0 && playerResponsibleNumber != playerActorNumber)
		{
			GRPlayer gRPlayer3 = GRPlayer.Get(playerResponsibleNumber);
			if (gRPlayer3 != null && gRPlayer3 != gRPlayer)
			{
				gRPlayer3.IncrementSynchronizedSessionStat(GRPlayer.SynchronizedSessionStat.Assists, 1f);
			}
		}
		gRPlayer.ChangePlayerState((GRPlayer.GRPlayerState)newState, this);
	}

	public void RequestGrantPlayerShield(GRPlayer player, int shieldHp, int shieldFlags)
	{
		base.GetView.RPC("RequestGrantPlayerShieldRPC", GetAuthorityPlayer(), PhotonNetwork.LocalPlayer.ActorNumber, player.gamePlayer.rig.OwningNetPlayer.ActorNumber, shieldHp, shieldFlags);
	}

	[PunRPC]
	private void RequestGrantPlayerShieldRPC(int shieldingPlayer, int playerToGrantShieldActorNumber, int shieldHp, int shieldFlags, PhotonMessageInfo info)
	{
		GRPlayer gRPlayer = GRPlayer.Get(info.Sender.ActorNumber);
		GRPlayer gRPlayer2 = GRPlayer.Get(playerToGrantShieldActorNumber);
		if (IsValidAuthorityRPC(info.Sender) && !gRPlayer.IsNull() && gRPlayer.fireShieldLimiter.CheckCallTime(Time.unscaledTime) && !gRPlayer2.IsNull() && gRPlayer2.CanActivateShield(shieldHp))
		{
			base.GetView.RPC("ApplyGrantPlayerShieldRPC", RpcTarget.All, shieldingPlayer, playerToGrantShieldActorNumber, shieldHp, shieldFlags);
		}
	}

	[PunRPC]
	private void ApplyGrantPlayerShieldRPC(int shieldingPlayer, int playerToGrantShieldActorNumber, int shieldHp, int shieldFlags, PhotonMessageInfo info)
	{
		if (!IsValidClientRPC(info.Sender) || m_RpcSpamChecks.IsSpamming(RPC.GrantPlayerShield))
		{
			return;
		}
		GRPlayer gRPlayer = GRPlayer.Get(playerToGrantShieldActorNumber);
		if (!(gRPlayer == null) && gRPlayer.TryActivateShield(shieldHp, shieldFlags))
		{
			GRPlayer gRPlayer2 = GRPlayer.Get(shieldingPlayer);
			if (gRPlayer2 != null)
			{
				gRPlayer2.IncrementSynchronizedSessionStat(GRPlayer.SynchronizedSessionStat.Assists, 1f);
			}
		}
	}

	public void RequestFireProjectile(GameEntityId entityId, Vector3 firingPosition, Vector3 targetPosition, double networkTime)
	{
		if (IsAuthority() && ((NetworkSystem.Instance.InRoom && base.IsMine) || !NetworkSystem.Instance.InRoom))
		{
			base.GetView.RPC("RequestFireProjectileRPC", RpcTarget.All, gameEntityManager.GetNetIdFromEntityId(entityId), firingPosition, targetPosition, networkTime);
		}
	}

	[PunRPC]
	private void RequestFireProjectileRPC(int entityNetId, Vector3 firingPosition, Vector3 targetPosition, double networkTime, PhotonMessageInfo info)
	{
		if (IsValidClientRPC(info.Sender, entityNetId, targetPosition) && !m_RpcSpamChecks.IsSpamming(RPC.RequestFireProjectile) && gameEntityManager.IsEntityNearPosition(entityNetId, firingPosition))
		{
			GameEntityId entityIdFromNetId = gameEntityManager.GetEntityIdFromNetId(entityNetId);
			OnRequestFireProjectileInternal(entityIdFromNetId, firingPosition, targetPosition, networkTime);
		}
	}

	private void OnRequestFireProjectileInternal(GameEntityId entityId, Vector3 firingPosition, Vector3 targetPosition, double networkTime)
	{
		GREnemyRanged gameComponent = gameEntityManager.GetGameComponent<GREnemyRanged>(entityId);
		if (gameComponent != null)
		{
			gameComponent.RequestRangedAttack(firingPosition, targetPosition, networkTime);
		}
		GRHazardTower gameComponent2 = gameEntityManager.GetGameComponent<GRHazardTower>(entityId);
		if (gameComponent2 != null)
		{
			gameComponent2.OnFire(firingPosition, targetPosition, networkTime);
		}
	}

	[PunRPC]
	public void BroadcastHandprint(Vector3 pos, Quaternion orient, PhotonMessageInfo info)
	{
		if (!(reactor == null) && pos.IsValid(10000f) && orient.IsValid())
		{
			GRPlayer gRPlayer = GRPlayer.Get(info.Sender);
			if (!(gRPlayer == null) && GameEntityManager.IsPlayerHandNearPosition(gRPlayer.gamePlayer, pos, isLeftHand: false, checkBothHands: true, 3f) && (info.Sender.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber || !(Time.time - LastHandprintTime <= 0.25f)))
			{
				LastHandprintTime = Time.time;
				reactor.AddHandprint(pos, orient);
			}
		}
	}

	public void OnAbilityDie(GameEntity entity, float forcedRespawn = -1f)
	{
		if (!(reactor == null))
		{
			reactor.OnAbilityDie(entity, forcedRespawn);
		}
	}

	public void RequestShiftStartAuthority(bool isFirstShift)
	{
		if (IsAuthority())
		{
			GhostReactorShiftManager shiftManager = reactor.shiftManager;
			_ = reactor.levelGenerator;
			if (!shiftManager.ShiftActive)
			{
				double time = PhotonNetwork.Time;
				int num = new SRand(Mathf.FloorToInt(Time.time * 100f)).NextInt(0, int.MaxValue);
				string text = Guid.NewGuid().ToString();
				photonView.RPC("ApplyShiftStartRPC", RpcTarget.All, time, num, text, isFirstShift);
				shiftManager.RequestState(GhostReactorShiftManager.State.ShiftActive);
				ProgressionManager.Instance.StartOfShift(text, shiftManager.shiftRewardCoresForMothership, reactor.vrRigs.Count, reactor.GetDepthLevel());
			}
		}
	}

	[PunRPC]
	public void ApplyShiftStartRPC(double shiftStartTime, int randomSeed, string gameIdGuid, bool isFirstShift, PhotonMessageInfo info)
	{
		if (double.IsNaN(shiftStartTime) || !IsValidClientRPC(info.Sender) || m_RpcSpamChecks.IsSpamming(RPC.ApplyShiftStart) || reactor == null)
		{
			return;
		}
		GhostReactorShiftManager shiftManager = reactor.shiftManager;
		GhostReactorLevelGenerator levelGenerator = reactor.levelGenerator;
		int index = Math.Clamp(reactor.NumActivePlayers, 0, reactor.difficultyScalingPerPlayer.Count - 1);
		reactor.difficultyScalingForCurrentFloor = 1f;
		if (reactor.difficultyScalingPerPlayer.Count > 0)
		{
			reactor.difficultyScalingForCurrentFloor = reactor.difficultyScalingPerPlayer[index];
		}
		double num = PhotonNetwork.Time - shiftStartTime;
		if (num < 0.0 || num > 10.0)
		{
			return;
		}
		levelGenerator.Generate(randomSeed);
		if (gameEntityManager.IsAuthority())
		{
			if (activeSpawnSectionEntitiesCoroutine != null)
			{
				StopCoroutine(activeSpawnSectionEntitiesCoroutine);
			}
			activeSpawnSectionEntitiesCoroutine = StartCoroutine(SpawnSectionEntitiesCoroutine(reactor.difficultyScalingForCurrentFloor));
		}
		shiftManager.shiftStats.ResetShiftStats();
		shiftManager.ResetJudgment();
		shiftManager.RefreshShiftStatsDisplay();
		shiftManager.OnShiftStarted(gameIdGuid, shiftStartTime, wasPlayerInAtStart: true, isFirstShift);
		reactor.ClearAllHandprints();
		reactor.ClearAllRespawns();
	}

	private IEnumerator SpawnSectionEntitiesCoroutine(float respawnCount)
	{
		int initialFrameCount = Time.frameCount;
		while (initialFrameCount == Time.frameCount)
		{
			yield return spawnSectionEntitiesWait;
		}
		if (gameEntityManager.IsAuthority())
		{
			reactor.levelGenerator.SpawnEntitiesInEachSection(respawnCount);
		}
	}

	[PunRPC]
	public void RequestShiftEnd()
	{
		if (!IsAuthority() || reactor == null)
		{
			return;
		}
		GhostReactorShiftManager shiftManager = reactor.shiftManager;
		GhostReactorLevelGenerator levelGenerator = reactor.levelGenerator;
		if (shiftManager == null || !shiftManager.ShiftActive)
		{
			return;
		}
		tempEntitiesToDestroy.Clear();
		List<GameEntity> gameEntities = gameEntityManager.GetGameEntities();
		for (int i = 0; i < gameEntities.Count; i++)
		{
			GameEntity gameEntity = gameEntities[i];
			if (gameEntity != null && !ShouldEntitySurviveShift(gameEntity))
			{
				tempEntitiesToDestroy.Add(gameEntity.id);
			}
		}
		gameEntityManager.RequestDestroyItems(tempEntitiesToDestroy);
		photonView.RPC("ApplyShiftEndRPC", RpcTarget.Others, PhotonNetwork.Time);
		levelGenerator.ClearLevelSections();
		shiftManager.OnShiftEnded(PhotonNetwork.Time, isShiftActuallyEnding: true);
		shiftManager.CalculateShiftTotal();
		shiftManager.RevealJudgment(Mathf.FloorToInt((float)shiftManager.shiftStats.GetShiftStat(GRShiftStatType.EnemyDeaths) / 5f));
		shiftManager.RequestState(GhostReactorShiftManager.State.PostShift);
	}

	public void SendRequestShiftEndRPC()
	{
		photonView.RPC("RequestShiftEnd", gameEntityManager.GetAuthorityPlayer());
	}

	[PunRPC]
	public void ApplyShiftEndRPC(double networkedTime, PhotonMessageInfo info)
	{
		if (double.IsFinite(networkedTime) && IsValidClientRPC(info.Sender) && !m_RpcSpamChecks.IsSpamming(RPC.ApplyShiftEnd) && !(reactor == null))
		{
			GhostReactorShiftManager shiftManager = reactor.shiftManager;
			GhostReactorLevelGenerator levelGenerator = reactor.levelGenerator;
			if (shiftManager.ShiftActive)
			{
				reactor.ClearAllRespawns();
				levelGenerator.ClearLevelSections();
				shiftManager.OnShiftEnded(networkedTime, isShiftActuallyEnding: true);
				shiftManager.CalculateShiftTotal();
				shiftManager.RevealJudgment(Mathf.FloorToInt((float)shiftManager.shiftStats.GetShiftStat(GRShiftStatType.EnemyDeaths) / 5f));
			}
		}
	}

	private bool ShouldEntitySurviveShift(GameEntity gameEntity)
	{
		if (gameEntity == null)
		{
			return true;
		}
		if (reactor == null)
		{
			return false;
		}
		if (IsEnemy(gameEntity))
		{
			return false;
		}
		if (gameEntity.GetComponent<GRBreakable>() != null || gameEntity.GetComponent<GRCollectibleDispenser>() != null || gameEntity.GetComponent<GRMetalEnergyGate>() != null || gameEntity.GetComponent<GRBarrierSpectral>() != null || gameEntity.GetComponent<GRSconce>() != null)
		{
			return false;
		}
		BoxCollider safeZoneLimit = reactor.safeZoneLimit;
		Vector3 position = gameEntity.gameObject.transform.position;
		if (safeZoneLimit.bounds.Contains(position))
		{
			return true;
		}
		if (gameEntity.GetComponent<GRBadge>() != null)
		{
			return true;
		}
		return false;
	}

	private bool IsEnemy(GameEntity gameEntity)
	{
		if (!(gameEntity.GetComponent<GREnemyChaser>() != null) && !(gameEntity.GetComponent<GREnemyRanged>() != null) && !(gameEntity.GetComponent<GREnemyPhantom>() != null) && !(gameEntity.GetComponent<GREnemyPest>() != null) && !(gameEntity.GetComponent<GREnemySummoner>() != null) && !(gameEntity.GetComponent<GREnemyMonkeye>() != null))
		{
			return gameEntity.GetComponent<GREnemyBossMoon>() != null;
		}
		return true;
	}

	public void InstantDeathForCurrentEnemies()
	{
		int num = 0;
		List<GameEntity> gameEntities = gameEntityManager.GetGameEntities();
		for (int i = 0; i < gameEntities.Count; i++)
		{
			if (gameEntities[i] == null)
			{
				continue;
			}
			GameEntity gameEntity = gameEntities[i];
			if (gameEntity.GetComponent<GREnemyBossMoon>() != null)
			{
				continue;
			}
			GREnemyChaser component = gameEntity.GetComponent<GREnemyChaser>();
			if (component != null)
			{
				component.InstantDeath();
				num++;
				continue;
			}
			GREnemyRanged component2 = gameEntity.GetComponent<GREnemyRanged>();
			if (component2 != null)
			{
				component2.InstantDeath();
				num++;
				continue;
			}
			GREnemyPest component3 = gameEntity.GetComponent<GREnemyPest>();
			if (component3 != null)
			{
				component3.InstantDeath();
				num++;
				continue;
			}
			GREnemySummoner component4 = gameEntity.GetComponent<GREnemySummoner>();
			if (component4 != null)
			{
				component4.InstantDeath();
				num++;
				continue;
			}
			GREnemyMonkeye component5 = gameEntity.GetComponent<GREnemyMonkeye>();
			if (component5 != null)
			{
				component5.InstantDeath();
				num++;
			}
		}
		Debug.Log($"Instant death for {num} enemies.");
	}

	private void RequestRestoreBossHP()
	{
	}

	private void RequestHurtBossHP()
	{
	}

	private void RequestKillBossEyes()
	{
	}

	private void RequestKillBossSummoned()
	{
	}

	private void RequestGoBackBossPhase()
	{
	}

	private void RequestAdvanceBossPhase()
	{
	}

	private void RequestBossBehavior(GREnemyBossMoon.Behavior bossBehavior)
	{
	}

	public GameEntity GetBossEntity()
	{
		if (cachedBossEntity != null && cachedBossEntity.IsNotNull())
		{
			return cachedBossEntity;
		}
		if (gameEntityManager == null)
		{
			return null;
		}
		GameEntity result = null;
		List<GameEntity> gameEntities = gameEntityManager.GetGameEntities();
		for (int i = 0; i < gameEntities.Count; i++)
		{
			if (!(gameEntities[i] == null) && !(gameEntities[i].GetComponent<GREnemyBossMoon>() == null))
			{
				result = gameEntities[i];
				break;
			}
		}
		cachedBossEntity = result;
		return result;
	}

	public void ClearCachedBossEntity()
	{
		cachedBossEntity = null;
	}

	public void ReportEnemyDeath()
	{
		if (!(reactor == null))
		{
			GhostReactorShiftManager shiftManager = reactor.shiftManager;
			shiftManager.shiftStats.IncrementShiftStat(GRShiftStatType.EnemyDeaths);
			shiftManager.RefreshShiftStatsDisplay();
			PlayerGameEvents.MiscEvent("GRKillEnemy");
		}
	}

	public void ReportCoreCollection(GRPlayer player, ProgressionManager.CoreType type)
	{
		Debug.Log("GhostReactorManager ReportCoreCollection");
		if (player == null || reactor == null)
		{
			return;
		}
		GhostReactorShiftManager shiftManager = reactor.shiftManager;
		switch (type)
		{
		case ProgressionManager.CoreType.ChaosSeed:
			shiftManager.shiftStats.IncrementShiftStat(GRShiftStatType.SentientCoresCollected);
			break;
		case ProgressionManager.CoreType.SuperCore:
		{
			shiftManager.shiftStats.IncrementShiftStat(GRShiftStatType.CoresCollected);
			shiftManager.shiftStats.IncrementShiftStat(GRShiftStatType.CoresCollected);
			shiftManager.shiftStats.IncrementShiftStat(GRShiftStatType.CoresCollected);
			player.IncrementSynchronizedSessionStat(GRPlayer.SynchronizedSessionStat.CoresDeposited, 3f);
			int count2 = reactor.vrRigs.Count;
			for (int j = 0; j < count2; j++)
			{
				GRPlayer gRPlayer2 = GRPlayer.Get(reactor.vrRigs[j]);
				if (gRPlayer2 != null)
				{
					gRPlayer2.IncrementSynchronizedSessionStat(GRPlayer.SynchronizedSessionStat.EarnedCredits, 15f);
				}
			}
			break;
		}
		default:
		{
			shiftManager.shiftStats.IncrementShiftStat(GRShiftStatType.CoresCollected);
			player.IncrementSynchronizedSessionStat(GRPlayer.SynchronizedSessionStat.CoresDeposited, 1f);
			int count = reactor.vrRigs.Count;
			for (int i = 0; i < count; i++)
			{
				GRPlayer gRPlayer = GRPlayer.Get(reactor.vrRigs[i]);
				if (gRPlayer != null)
				{
					gRPlayer.IncrementSynchronizedSessionStat(GRPlayer.SynchronizedSessionStat.EarnedCredits, 5f);
				}
			}
			break;
		}
		}
		shiftManager.RefreshShiftStatsDisplay();
		PlayerGameEvents.MiscEvent("GRCollectCore");
	}

	public void ReportPlayerDeath(GRPlayer player)
	{
		if (!(reactor == null) && !(player == null) && reactor.zone != GTZone.customMaps)
		{
			GhostReactorShiftManager shiftManager = reactor.shiftManager;
			shiftManager.shiftStats.IncrementShiftStat(GRShiftStatType.PlayerDeaths);
			shiftManager.RefreshShiftStatsDisplay();
			player.IncrementSynchronizedSessionStat(GRPlayer.SynchronizedSessionStat.Deaths, 1f);
		}
	}

	public void PromotionBotActivePlayerRequest(int state)
	{
		photonView.RPC("PromotionBotActivePlayerRequestRPC", GetAuthorityPlayer(), state);
	}

	[PunRPC]
	public void PromotionBotActivePlayerRequestRPC(int state, PhotonMessageInfo info)
	{
		if (reactor == null || !IsValidAuthorityRPC(info.Sender))
		{
			return;
		}
		GRPlayer gRPlayer = GRPlayer.Get(info.Sender.ActorNumber);
		if (gRPlayer.IsNull() || !gRPlayer.promotionBotLimiter.CheckCallTime(Time.unscaledTime))
		{
			return;
		}
		GRUIPromotionBot promotionBot = reactor.promotionBot;
		if (promotionBot == null)
		{
			return;
		}
		if (state == 6)
		{
			if (promotionBot.currentPlayerActorNumber != -1)
			{
				return;
			}
			state = 1;
		}
		int actorNumber = info.Sender.ActorNumber;
		photonView.RPC("PromotionBotActivePlayerResponseRPC", RpcTarget.Others, actorNumber, state);
		promotionBot.SetActivePlayerStateChange(actorNumber, state);
	}

	[PunRPC]
	public void PromotionBotActivePlayerResponseRPC(int actorNumber, int state, PhotonMessageInfo info)
	{
		if (!(reactor == null))
		{
			GRUIPromotionBot promotionBot = reactor.promotionBot;
			if (!(GRPlayer.Get(info.Sender.ActorNumber) == null) && !(promotionBot == null) && IsValidClientRPC(info.Sender) && !m_RpcSpamChecks.IsSpamming(RPC.PromotionBotResponse))
			{
				promotionBot.SetActivePlayerStateChange(actorNumber, state);
			}
		}
	}

	[PunRPC]
	public void BroadcastScoreboardPage(int scoreboardPage, PhotonMessageInfo info)
	{
		if (!(reactor == null))
		{
			GRPlayer gRPlayer = GRPlayer.Get(info.Sender.ActorNumber);
			if (!(gRPlayer == null) && gRPlayer.scoreboardPageLimiter.CheckCallTime(Time.unscaledTime) && GRUIScoreboard.ValidPage((GRUIScoreboard.ScoreboardScreen)scoreboardPage))
			{
				GhostReactor.instance.UpdateScoreboardScreen((GRUIScoreboard.ScoreboardScreen)scoreboardPage);
			}
		}
	}

	[PunRPC]
	public void BroadcastStartingProgression(int points, int redeemedPoints, double shiftJoinedTime, PhotonMessageInfo info)
	{
		if (!double.IsNaN(shiftJoinedTime) && !double.IsInfinity(shiftJoinedTime) && !(reactor == null))
		{
			GRPlayer gRPlayer = GRPlayer.Get(info.Sender.ActorNumber);
			if (!(gRPlayer == null) && gRPlayer.progressionBroadcastLimiter.CheckCallTime(Time.unscaledTime))
			{
				gRPlayer.SetProgressionData(points, redeemedPoints);
				gRPlayer.shiftJoinTime = Math.Clamp(shiftJoinedTime, PhotonNetwork.Time - 10.0, PhotonNetwork.Time);
			}
		}
	}

	public void RequestPlayerAction(GRPlayerAction playerAction)
	{
		photonView.RPC("RequestPlayerActionRPC", GetAuthorityPlayer(), (int)playerAction, 0, 0);
	}

	public void RequestPlayerAction(GRPlayerAction playerAction, int param0)
	{
		photonView.RPC("RequestPlayerActionRPC", GetAuthorityPlayer(), (int)playerAction, param0, 0);
	}

	public void RequestPlayerAction(GRPlayerAction playerAction, int param0, int param1)
	{
		photonView.RPC("RequestPlayerActionRPC", GetAuthorityPlayer(), (int)playerAction, param0, param1);
	}

	public bool VerifyShuttleInteractability(GRPlayer player, int shuttleIdx, bool ignoreOwnership = false)
	{
		if (GRElevatorManager._instance == null)
		{
			return false;
		}
		GRShuttle shuttleById = GRElevatorManager._instance.GetShuttleById(shuttleIdx);
		if (shuttleById == null)
		{
			return false;
		}
		return shuttleById.IsShuttleInteractableByPlayer(player, ignoreOwnership);
	}

	[PunRPC]
	public void RequestPlayerActionRPC(int playerAction, int param0, int param1, PhotonMessageInfo info)
	{
		if (!IsValidAuthorityRPC(info.Sender) || reactor == null)
		{
			return;
		}
		GRPlayer gRPlayer = GRPlayer.Get(info.Sender.ActorNumber);
		if (gRPlayer.IsNull() || !gRPlayer.requestShiftStartLimiter.CheckCallTime(Time.unscaledTime))
		{
			return;
		}
		GhostReactorShiftManager shiftManager = reactor.shiftManager;
		_ = reactor.levelGenerator;
		bool flag = false;
		switch ((GRPlayerAction)playerAction)
		{
		case GRPlayerAction.DelveDeeper:
			flag = !shiftManager.ShiftActive && shiftManager.authorizedToDelveDeeper;
			if (flag)
			{
				int num = reactor.GetDepthLevel() + 1;
				reactor.depthConfigIndex = reactor.PickLevelConfigForDepth(num);
				param0 = num;
				param1 = reactor.depthConfigIndex;
			}
			break;
		case GRPlayerAction.DelveState:
			flag = true;
			break;
		case GRPlayerAction.ShuttleOpen:
			flag = VerifyShuttleInteractability(gRPlayer, param0, ignoreOwnership: true);
			param1 = info.Sender.ActorNumber;
			break;
		case GRPlayerAction.ShuttleClose:
			flag = VerifyShuttleInteractability(gRPlayer, param0);
			param1 = info.Sender.ActorNumber;
			break;
		case GRPlayerAction.ShuttleLaunch:
			flag = VerifyShuttleInteractability(gRPlayer, param0);
			param1 = info.Sender.ActorNumber;
			break;
		case GRPlayerAction.ShuttleArrive:
			flag = VerifyShuttleInteractability(gRPlayer, param0);
			param1 = info.Sender.ActorNumber;
			break;
		case GRPlayerAction.ShuttleTargetLevelUp:
			flag = VerifyShuttleInteractability(gRPlayer, param0);
			param1 = info.Sender.ActorNumber;
			break;
		case GRPlayerAction.ShuttleTargetLevelDown:
			flag = VerifyShuttleInteractability(gRPlayer, param0);
			param1 = info.Sender.ActorNumber;
			break;
		case GRPlayerAction.SetPodLevel:
			flag = true;
			param0 = Mathf.Clamp(param0, 0, 1);
			param1 = info.Sender.ActorNumber;
			break;
		case GRPlayerAction.SetPodChassisLevel:
			flag = true;
			param0 = Mathf.Clamp(param0, 0, 3);
			param1 = info.Sender.ActorNumber;
			break;
		case GRPlayerAction.SeedExtractorOpenStation:
			flag = param0 == info.Sender.ActorNumber || IsAuthorityPlayer(info.Sender);
			if (reactor.seedExtractor.StationOpen && reactor.seedExtractor.CurrentPlayerActorNumber != info.Sender.ActorNumber)
			{
				playerAction = 13;
			}
			break;
		case GRPlayerAction.SeedExtractorCloseStation:
			flag = IsAuthorityPlayer(info.Sender);
			break;
		case GRPlayerAction.SeedExtractorCardSwipeFail:
			flag = IsAuthorityPlayer(info.Sender);
			break;
		case GRPlayerAction.SeedExtractorTryDepositSeed:
		{
			GameEntity gameEntityFromNetId2 = gameEntityManager.GetGameEntityFromNetId(param1);
			if (IsAuthorityPlayer(info.Sender) && gameEntityFromNetId2 != null && gameEntityFromNetId2.lastHeldByActorNumber == param0)
			{
				flag = true;
			}
			break;
		}
		case GRPlayerAction.SeedExtractorDepositSeedSucceeded:
		{
			int netId = param1;
			GameEntity gameEntityFromNetId = gameEntityManager.GetGameEntityFromNetId(netId);
			if (gameEntityFromNetId != null && reactor.seedExtractor.ValidateSeedDepositSucceeded(param0, param1))
			{
				gameEntityManager.RequestDestroyItem(gameEntityFromNetId.id);
				flag = true;
			}
			break;
		}
		case GRPlayerAction.SeedExtractorDepositSeedFailed:
			flag = info.Sender.ActorNumber == param0;
			break;
		}
		if (flag)
		{
			photonView.RPC("ApplyPlayerActionRPC", RpcTarget.All, playerAction, param0, param1);
		}
	}

	[PunRPC]
	public void ApplyPlayerActionRPC(int playerAction, int param0, int param1, PhotonMessageInfo info)
	{
		if (!IsValidClientRPC(info.Sender) || m_RpcSpamChecks.IsSpamming(RPC.ApplyShiftStart) || reactor == null)
		{
			return;
		}
		_ = reactor.shiftManager;
		_ = reactor.levelGenerator;
		gameEntityManager.IsAuthorityPlayer(info.Sender);
		GRPlayer gRPlayer = GRPlayer.Get(info.Sender.ActorNumber);
		if (gRPlayer.IsNull() || !gRPlayer.requestShiftStartLimiter.CheckCallTime(Time.unscaledTime))
		{
			return;
		}
		switch ((GRPlayerAction)playerAction)
		{
		case GRPlayerAction.DelveDeeper:
			reactor.SetNextDelveDepth(param0, param1);
			break;
		case GRPlayerAction.DelveState:
			reactor.shiftManager.SetState((GhostReactorShiftManager.State)param0);
			break;
		case GRPlayerAction.ShuttleOpen:
		{
			GRPlayer gRPlayer5 = GRPlayer.Get(param1);
			if (!(gRPlayer5 == null) && VerifyShuttleInteractability(gRPlayer5, param0, ignoreOwnership: true))
			{
				GRShuttle shuttleById2 = GRElevatorManager._instance.GetShuttleById(param0);
				if (shuttleById2 != null)
				{
					shuttleById2.OnOpenDoor();
				}
			}
			break;
		}
		case GRPlayerAction.ShuttleClose:
		{
			GRPlayer gRPlayer9 = GRPlayer.Get(param1);
			if (!(gRPlayer9 == null) && VerifyShuttleInteractability(gRPlayer9, param0))
			{
				GRShuttle shuttleById6 = GRElevatorManager._instance.GetShuttleById(param0);
				if (shuttleById6 != null)
				{
					shuttleById6.OnCloseDoor();
				}
			}
			break;
		}
		case GRPlayerAction.ShuttleLaunch:
		{
			GRPlayer gRPlayer3 = GRPlayer.Get(param1);
			if (!(gRPlayer3 == null) && VerifyShuttleInteractability(gRPlayer3, param0))
			{
				GRShuttle shuttleById = GRElevatorManager._instance.GetShuttleById(param0);
				if (shuttleById != null)
				{
					shuttleById.OnLaunch();
				}
			}
			break;
		}
		case GRPlayerAction.ShuttleArrive:
		{
			GRPlayer gRPlayer7 = GRPlayer.Get(param1);
			if (!(gRPlayer7 == null) && VerifyShuttleInteractability(gRPlayer7, param0))
			{
				GRShuttle shuttleById4 = GRElevatorManager._instance.GetShuttleById(param0);
				if (shuttleById4 != null)
				{
					shuttleById4.OnArrive();
				}
			}
			break;
		}
		case GRPlayerAction.ShuttleTargetLevelUp:
		{
			GRPlayer gRPlayer8 = GRPlayer.Get(param1);
			if (!(gRPlayer8 == null) && VerifyShuttleInteractability(gRPlayer8, param0))
			{
				GRShuttle shuttleById5 = GRElevatorManager._instance.GetShuttleById(param0);
				if (shuttleById5 != null)
				{
					shuttleById5.OnTargetLevelUp();
				}
			}
			break;
		}
		case GRPlayerAction.ShuttleTargetLevelDown:
		{
			GRPlayer gRPlayer6 = GRPlayer.Get(param1);
			if (!(gRPlayer6 == null) && VerifyShuttleInteractability(gRPlayer6, param0))
			{
				GRShuttle shuttleById3 = GRElevatorManager._instance.GetShuttleById(param0);
				if (shuttleById3 != null)
				{
					shuttleById3.OnTargetLevelDown();
				}
			}
			break;
		}
		case GRPlayerAction.SetPodLevel:
		{
			GRPlayer gRPlayer4 = GRPlayer.Get(param1);
			if (gRPlayer4 != null)
			{
				param0 = Mathf.Clamp(param0, 0, 1);
				gRPlayer4.dropPodLevel = param0;
				reactor.RefreshBays();
				gRPlayer4.RefreshShuttles();
			}
			break;
		}
		case GRPlayerAction.SetPodChassisLevel:
		{
			GRPlayer gRPlayer2 = GRPlayer.Get(param1);
			if (gRPlayer2 != null)
			{
				param0 = Mathf.Clamp(param0, 0, 3);
				gRPlayer2.dropPodChasisLevel = param0;
				reactor.RefreshBays();
				gRPlayer2.RefreshShuttles();
			}
			break;
		}
		case GRPlayerAction.SeedExtractorOpenStation:
			reactor.seedExtractor.CardSwipeSuccess();
			reactor.seedExtractor.OpenStation(param0);
			break;
		case GRPlayerAction.SeedExtractorCloseStation:
			reactor.seedExtractor.CloseStation();
			break;
		case GRPlayerAction.SeedExtractorCardSwipeFail:
			reactor.seedExtractor.CardSwipeFail();
			break;
		case GRPlayerAction.SeedExtractorTryDepositSeed:
			reactor.seedExtractor.TryDepositSeed(param0, param1);
			break;
		case GRPlayerAction.SeedExtractorDepositSeedSucceeded:
			reactor.seedExtractor.SeedDepositSucceeded(param0, param1);
			break;
		case GRPlayerAction.SeedExtractorDepositSeedFailed:
			reactor.seedExtractor.SeedDepositFailed(param0, param1);
			break;
		}
	}

	public GRToolUpgradePurchaseStationFull GetToolUpgradeStationFullForIndex(int idx)
	{
		if (reactor == null || reactor.toolUpgradePurchaseStationsFull == null || idx < 0 || idx >= reactor.toolUpgradePurchaseStationsFull.Count)
		{
			return null;
		}
		return reactor.toolUpgradePurchaseStationsFull[idx];
	}

	public int GetIndexForToolUpgradeStationFull(GRToolUpgradePurchaseStationFull station)
	{
		if (reactor == null || reactor.toolUpgradePurchaseStationsFull == null)
		{
			return -1;
		}
		return reactor.toolUpgradePurchaseStationsFull.IndexOf(station);
	}

	public void RequestNetworkShelfAndItemChange(GRToolUpgradePurchaseStationFull station, int shelf, int item)
	{
		int indexForToolUpgradeStationFull = GetIndexForToolUpgradeStationFull(station);
		if (indexForToolUpgradeStationFull != -1)
		{
			photonView.RPC("ToolPurchaseV2_RPC", RpcTarget.Others, ToolPurchaseActionV2.SelectShelfAndItem, PhotonNetwork.LocalPlayer.ActorNumber, indexForToolUpgradeStationFull, shelf, item);
		}
	}

	private void SelectToolShelfAndItemRPCRouted(int stationIndex, int shelf, int item, PhotonMessageInfo info)
	{
		GRToolUpgradePurchaseStationFull toolUpgradeStationFullForIndex = GetToolUpgradeStationFullForIndex(stationIndex);
		if (!(toolUpgradeStationFullForIndex == null) && toolUpgradeStationFullForIndex.currentActivePlayerActorNumber == info.Sender.ActorNumber)
		{
			toolUpgradeStationFullForIndex.SetSelectedShelfAndItem(shelf, item, fromNetworkRPC: true);
		}
	}

	public void RequestPurchaseToolOrUpgrade(GRToolUpgradePurchaseStationFull station, int shelf, int item)
	{
		int indexForToolUpgradeStationFull = GetIndexForToolUpgradeStationFull(station);
		if (indexForToolUpgradeStationFull != -1)
		{
			photonView.RPC("ToolPurchaseV2_RPC", GetAuthorityPlayer(), ToolPurchaseActionV2.RequestPurchaseAuthority, PhotonNetwork.LocalPlayer.ActorNumber, indexForToolUpgradeStationFull, shelf, item);
		}
	}

	public void RequestPurchaseRPCRoutedAuthority(int stationIndex, int shelf, int item, PhotonMessageInfo info)
	{
		if (!IsValidAuthorityRPC(info.Sender))
		{
			return;
		}
		GRToolUpgradePurchaseStationFull toolUpgradeStationFullForIndex = GetToolUpgradeStationFullForIndex(stationIndex);
		if (toolUpgradeStationFullForIndex == null)
		{
			return;
		}
		GRPlayer gRPlayer = GRPlayer.Get(info.Sender.ActorNumber);
		if (gRPlayer.IsNull() || toolUpgradeStationFullForIndex.currentActivePlayerActorNumber != info.Sender.ActorNumber)
		{
			return;
		}
		(bool, bool) tuple = toolUpgradeStationFullForIndex.TryPurchaseAuthority(gRPlayer, shelf, item);
		var (flag, _) = tuple;
		if (tuple.Item2)
		{
			if (flag)
			{
				photonView.RPC("ToolPurchaseV2_RPC", RpcTarget.Others, ToolPurchaseActionV2.NotifyPurchaseSuccess, info.Sender.ActorNumber, stationIndex, shelf, item);
			}
			else
			{
				photonView.RPC("ToolPurchaseV2_RPC", RpcTarget.Others, ToolPurchaseActionV2.NotifyPurchaseFail, info.Sender.ActorNumber, stationIndex, shelf, item);
			}
			toolUpgradeStationFullForIndex.ToolPurchaseResponseLocal(gRPlayer, shelf, item, flag);
		}
	}

	public void NotifyPurchaseToolOrUpgradeRPCRouted(int actorNumber, int stationIndex, int shelf, int item, bool success, PhotonMessageInfo info)
	{
		if (!IsValidClientRPC(info.Sender))
		{
			return;
		}
		GRToolUpgradePurchaseStationFull toolUpgradeStationFullForIndex = GetToolUpgradeStationFullForIndex(stationIndex);
		if (!(toolUpgradeStationFullForIndex == null))
		{
			GRPlayer gRPlayer = GRPlayer.Get(actorNumber);
			if (gRPlayer != null)
			{
				toolUpgradeStationFullForIndex.ToolPurchaseResponseLocal(gRPlayer, shelf, item, success);
			}
		}
	}

	public void RequestStationExclusivity(GRToolUpgradePurchaseStationFull station)
	{
		int indexForToolUpgradeStationFull = GetIndexForToolUpgradeStationFull(station);
		if (indexForToolUpgradeStationFull != -1)
		{
			photonView.RPC("ToolPurchaseV2_RPC", GetAuthorityPlayer(), ToolPurchaseActionV2.RequestStationExclusivityAuthority, PhotonNetwork.LocalPlayer.ActorNumber, indexForToolUpgradeStationFull, 0, 0);
		}
	}

	public void SetActivePlayerAuthority(GRToolUpgradePurchaseStationFull station, int actorNumber)
	{
		if (IsAuthority())
		{
			int indexForToolUpgradeStationFull = GetIndexForToolUpgradeStationFull(station);
			if (indexForToolUpgradeStationFull != -1)
			{
				station.SetActivePlayer(actorNumber);
				photonView.RPC("ToolPurchaseV2_RPC", RpcTarget.Others, ToolPurchaseActionV2.SetToolStationActivePlayer, PhotonNetwork.LocalPlayer.ActorNumber, indexForToolUpgradeStationFull, station.currentActivePlayerActorNumber, 0);
			}
		}
	}

	public void RequestStationExclusivityRPCRoutedAuthority(int stationIndex, PhotonMessageInfo info)
	{
		if (IsValidAuthorityRPC(info.Sender))
		{
			GRToolUpgradePurchaseStationFull toolUpgradeStationFullForIndex = GetToolUpgradeStationFullForIndex(stationIndex);
			if (!(toolUpgradeStationFullForIndex == null) && toolUpgradeStationFullForIndex.currentActivePlayerActorNumber == -1)
			{
				SetActivePlayerAuthority(toolUpgradeStationFullForIndex, info.Sender.ActorNumber);
			}
		}
	}

	public void SetToolStationActivePlayerRPCRouted(int stationIndex, int activeOwner, PhotonMessageInfo info)
	{
		if (IsValidClientRPC(info.Sender))
		{
			GRToolUpgradePurchaseStationFull toolUpgradeStationFullForIndex = GetToolUpgradeStationFullForIndex(stationIndex);
			if (!(toolUpgradeStationFullForIndex == null))
			{
				toolUpgradeStationFullForIndex.SetActivePlayer(activeOwner);
			}
		}
	}

	public void BroadcastHandleAndSelectionWheelPosition(GRToolUpgradePurchaseStationFull station, int handlePos, int wheelPos)
	{
		int indexForToolUpgradeStationFull = GetIndexForToolUpgradeStationFull(station);
		if (indexForToolUpgradeStationFull != -1 && NetworkSystem.Instance.LocalPlayer.ActorNumber == station.currentActivePlayerActorNumber)
		{
			photonView.RPC("ToolPurchaseV2_RPC", RpcTarget.Others, ToolPurchaseActionV2.SetHandleAndSelectionWheelPosition, PhotonNetwork.LocalPlayer.ActorNumber, indexForToolUpgradeStationFull, handlePos, wheelPos);
		}
	}

	public void SetHandleAndSelectionWheelPositionRPCRouted(int stationIndex, int handlePos, int wheelPos, PhotonMessageInfo info)
	{
		GRToolUpgradePurchaseStationFull toolUpgradeStationFullForIndex = GetToolUpgradeStationFullForIndex(stationIndex);
		if (!(toolUpgradeStationFullForIndex == null) && info.Sender.ActorNumber == toolUpgradeStationFullForIndex.currentActivePlayerActorNumber)
		{
			toolUpgradeStationFullForIndex.SetHandleAndSelectionWheelPositionRemote(handlePos, wheelPos);
		}
	}

	public void RequestHackToolStation()
	{
	}

	[PunRPC]
	public void ToolPurchaseV2_RPC(ToolPurchaseActionV2 command, int initiatorID, int stationIndex, int param1, int param2, PhotonMessageInfo info)
	{
		if (!m_RpcSpamChecks.IsSpamming(RPC.ToolUpgradeStationAction))
		{
			switch (command)
			{
			case ToolPurchaseActionV2.RequestPurchaseAuthority:
				RequestPurchaseRPCRoutedAuthority(stationIndex, param1, param2, info);
				break;
			case ToolPurchaseActionV2.SelectShelfAndItem:
				SelectToolShelfAndItemRPCRouted(stationIndex, param1, param2, info);
				break;
			case ToolPurchaseActionV2.NotifyPurchaseSuccess:
				NotifyPurchaseToolOrUpgradeRPCRouted(initiatorID, stationIndex, param1, param2, success: true, info);
				break;
			case ToolPurchaseActionV2.NotifyPurchaseFail:
				NotifyPurchaseToolOrUpgradeRPCRouted(initiatorID, stationIndex, param1, param2, success: false, info);
				break;
			case ToolPurchaseActionV2.RequestStationExclusivityAuthority:
				RequestStationExclusivityRPCRoutedAuthority(stationIndex, info);
				break;
			case ToolPurchaseActionV2.SetToolStationActivePlayer:
				SetToolStationActivePlayerRPCRouted(stationIndex, param1, info);
				break;
			case ToolPurchaseActionV2.SetHandleAndSelectionWheelPosition:
				SetHandleAndSelectionWheelPositionRPCRouted(stationIndex, param1, param2, info);
				break;
			case ToolPurchaseActionV2.SetToolStationHackedDebug:
				break;
			}
		}
	}

	public void ToolPurchaseStationRequest(int stationIndex, ToolPurchaseStationAction action)
	{
		photonView.RPC("ToolPurchaseStationRequestRPC", GetAuthorityPlayer(), stationIndex, action);
	}

	[PunRPC]
	public void ToolPurchaseStationRequestRPC(int stationIndex, ToolPurchaseStationAction action, PhotonMessageInfo info)
	{
		if (reactor == null)
		{
			return;
		}
		List<GRToolPurchaseStation> toolPurchasingStations = reactor.toolPurchasingStations;
		if (!IsValidAuthorityRPC(info.Sender) || stationIndex < 0 || stationIndex >= toolPurchasingStations.Count)
		{
			return;
		}
		GRPlayer gRPlayer = GRPlayer.Get(info.Sender.ActorNumber);
		if (gRPlayer.IsNull() || !gRPlayer.requestToolPurchaseStationLimiter.CheckCallTime(Time.unscaledTime))
		{
			return;
		}
		GRToolPurchaseStation gRToolPurchaseStation = toolPurchasingStations[stationIndex];
		if (gRToolPurchaseStation == null)
		{
			return;
		}
		switch (action)
		{
		case ToolPurchaseStationAction.ShiftLeft:
			gRToolPurchaseStation.ShiftLeftAuthority();
			photonView.RPC("ToolPurchaseStationResponseRPC", RpcTarget.Others, stationIndex, ToolPurchaseStationResponse.SelectionUpdate, gRToolPurchaseStation.ActiveEntryIndex, 0);
			ToolPurchaseResponseLocal(stationIndex, ToolPurchaseStationResponse.SelectionUpdate, gRToolPurchaseStation.ActiveEntryIndex, 0);
			break;
		case ToolPurchaseStationAction.ShiftRight:
			gRToolPurchaseStation.ShiftRightAuthority();
			photonView.RPC("ToolPurchaseStationResponseRPC", RpcTarget.Others, stationIndex, ToolPurchaseStationResponse.SelectionUpdate, gRToolPurchaseStation.ActiveEntryIndex, 0);
			ToolPurchaseResponseLocal(stationIndex, ToolPurchaseStationResponse.SelectionUpdate, gRToolPurchaseStation.ActiveEntryIndex, 0);
			break;
		case ToolPurchaseStationAction.TryPurchase:
		{
			bool flag = false;
			if (VRRigCache.Instance.TryGetVrrig(NetworkSystem.Instance.GetNetPlayerByID(info.Sender.ActorNumber), out var playerRig))
			{
				GRPlayer component = playerRig.Rig.GetComponent<GRPlayer>();
				if (component != null && gRToolPurchaseStation.TryPurchaseAuthority(component, out var itemCost))
				{
					photonView.RPC("ToolPurchaseStationResponseRPC", RpcTarget.Others, stationIndex, ToolPurchaseStationResponse.PurchaseSucceeded, info.Sender.ActorNumber, itemCost);
					ToolPurchaseResponseLocal(stationIndex, ToolPurchaseStationResponse.PurchaseSucceeded, info.Sender.ActorNumber, itemCost);
					flag = true;
				}
			}
			if (!flag)
			{
				photonView.RPC("ToolPurchaseStationResponseRPC", RpcTarget.Others, stationIndex, ToolPurchaseStationResponse.PurchaseFailed, info.Sender.ActorNumber, 0);
				ToolPurchaseResponseLocal(stationIndex, ToolPurchaseStationResponse.PurchaseFailed, info.Sender.ActorNumber, 0);
			}
			break;
		}
		}
	}

	[PunRPC]
	public void ToolPurchaseStationResponseRPC(int stationIndex, ToolPurchaseStationResponse responseType, int dataA, int dataB, PhotonMessageInfo info)
	{
		if (!(reactor == null))
		{
			List<GRToolPurchaseStation> toolPurchasingStations = reactor.toolPurchasingStations;
			if (IsValidClientRPC(info.Sender) && stationIndex >= 0 && stationIndex < toolPurchasingStations.Count && !m_RpcSpamChecks.IsSpamming(RPC.ToolPurchaseResponse))
			{
				ToolPurchaseResponseLocal(stationIndex, responseType, dataA, dataB);
			}
		}
	}

	private void ToolPurchaseResponseLocal(int stationIndex, ToolPurchaseStationResponse responseType, int dataA, int dataB)
	{
		if (reactor == null)
		{
			return;
		}
		List<GRToolPurchaseStation> toolPurchasingStations = reactor.toolPurchasingStations;
		if (stationIndex < 0 || stationIndex >= toolPurchasingStations.Count)
		{
			return;
		}
		GRToolPurchaseStation gRToolPurchaseStation = toolPurchasingStations[stationIndex];
		if (gRToolPurchaseStation == null)
		{
			return;
		}
		switch (responseType)
		{
		case ToolPurchaseStationResponse.SelectionUpdate:
			gRToolPurchaseStation.OnSelectionUpdate(dataA);
			break;
		case ToolPurchaseStationResponse.PurchaseSucceeded:
		{
			gRToolPurchaseStation.OnPurchaseSucceeded();
			GRPlayer gRPlayer = GRPlayer.Get(dataA);
			if (gRPlayer != null)
			{
				gRPlayer.IncrementCoresSpentPlayer(dataB);
				gRPlayer.AddItemPurchased(gRToolPurchaseStation.GetCurrentToolName());
				gRPlayer.SubtractShiftCredit(dataB);
			}
			break;
		}
		case ToolPurchaseStationResponse.PurchaseFailed:
			gRToolPurchaseStation.OnPurchaseFailed();
			break;
		}
	}

	public void ToolUpgradeStationRequestUpgrade(GRToolProgressionManager.ToolParts UpgradeID, int entityNetId)
	{
		photonView.RPC("ToolUpgradeStationRequestUpgradeRPC", GetAuthorityPlayer(), UpgradeID, entityNetId);
	}

	public void ToolSnapRequestUpgrade(int upgradeNetID, GRToolProgressionManager.ToolParts UpgradeID, int entityNetId)
	{
		photonView.RPC("ToolSnapRequestUpgradeRPC", GetAuthorityPlayer(), upgradeNetID, UpgradeID, entityNetId);
	}

	[PunRPC]
	public void ToolSnapRequestUpgradeRPC(int upgradeNetID, GRToolProgressionManager.ToolParts UpgradeID, int entityNetId, PhotonMessageInfo info)
	{
		if (reactor == null)
		{
			return;
		}
		GRPlayer gRPlayer = GRPlayer.Get(info.Sender.ActorNumber);
		if (gRPlayer == null || m_RpcSpamChecks.IsSpamming(RPC.ToolUpgradeStationAction) || !IsValidAuthorityRPC(info.Sender))
		{
			return;
		}
		GameEntity gameEntity = gameEntityManager.GetGameEntity(gameEntityManager.GetEntityIdFromNetId(entityNetId));
		if (gameEntity != null)
		{
			GRTool component = gameEntity.GetComponent<GRTool>();
			GameEntity gameEntity2 = gameEntityManager.GetGameEntity(gameEntityManager.GetEntityIdFromNetId(upgradeNetID));
			if (component != null && gameEntity2 != null && GameEntityManager.IsPlayerHandNearPosition(gRPlayer.gamePlayer, gameEntity2.transform.position, isLeftHand: false, checkBothHands: true) && GameEntityManager.IsPlayerHandNearPosition(gRPlayer.gamePlayer, gameEntity2.transform.position, isLeftHand: false, checkBothHands: true))
			{
				photonView.RPC("UpgradeToolRemoteRPC", RpcTarget.All, UpgradeID, entityNetId, false, info.Sender.ActorNumber);
				gameEntityManager.RequestDestroyItem(gameEntity2.id);
			}
		}
	}

	public void ToolUpgradeStationRequestUpgradeRPC(GRToolProgressionManager.ToolParts UpgradeID, int entityNetId, PhotonMessageInfo info)
	{
	}

	[PunRPC]
	public void UpgradeToolRemoteRPC(GRToolProgressionManager.ToolParts UpgradeID, int entityNetId, bool applyCost, int playerNetId, PhotonMessageInfo info)
	{
		if (!IsValidClientRPC(info.Sender))
		{
			return;
		}
		if (applyCost)
		{
			GRPlayer gRPlayer = GRPlayer.Get(info.Sender.ActorNumber);
			if (gRPlayer != null && reactor.toolProgression.GetShiftCreditCost(UpgradeID, out var shiftCreditCost))
			{
				gRPlayer.SubtractShiftCredit(shiftCreditCost);
			}
		}
		GameEntity gameEntity = gameEntityManager.GetGameEntity(gameEntityManager.GetEntityIdFromNetId(entityNetId));
		if (gameEntity != null)
		{
			GRTool component = gameEntity.GetComponent<GRTool>();
			if (component != null)
			{
				component.UpgradeTool(UpgradeID);
			}
		}
	}

	private bool DoesUserHaveResearchUnlocked(int UserID, string ResearchID)
	{
		return true;
	}

	public void ToolPlacedInUpgradeStation(GameEntity entity)
	{
		photonView.RPC("PlacedToolInUpgradeStationRPC", RpcTarget.All, gameEntityManager.GetNetIdFromEntityId(entity.id));
	}

	public void PlacedToolInUpgradeStationRPC(int entityNetId, PhotonMessageInfo info)
	{
	}

	public void UpgradeToolAtToolStation()
	{
		photonView.RPC("UpgradeToolAtToolStationRPC", RpcTarget.All);
	}

	public void UpgradeToolAtToolStationRPC(PhotonMessageInfo info)
	{
	}

	public void LocalEjectToolInUpgradeStation()
	{
	}

	public void EntityEnteredDropZone(GameEntity entity)
	{
		if (!IsAuthority() || reactor == null)
		{
			return;
		}
		GRUIStationEmployeeBadges employeeBadges = reactor.employeeBadges;
		long num = BitPackUtils.PackWorldPosForNetwork(entity.transform.position);
		int num2 = BitPackUtils.PackQuaternionForNetwork(entity.transform.rotation);
		if (entity.gameObject.GetComponent<GRBadge>() != null)
		{
			GRUIEmployeeBadgeDispenser gRUIEmployeeBadgeDispenser = employeeBadges.badgeDispensers[entity.gameObject.GetComponent<GRBadge>().dispenserIndex];
			if (gRUIEmployeeBadgeDispenser != null)
			{
				num = BitPackUtils.PackWorldPosForNetwork(gRUIEmployeeBadgeDispenser.GetSpawnPosition());
				num2 = BitPackUtils.PackQuaternionForNetwork(gRUIEmployeeBadgeDispenser.GetSpawnRotation());
			}
		}
		photonView.RPC("EntityEnteredDropZoneRPC", RpcTarget.All, gameEntityManager.GetNetIdFromEntityId(entity.id), num, num2);
	}

	[PunRPC]
	public void EntityEnteredDropZoneRPC(int entityNetId, long position, int rotation, PhotonMessageInfo info)
	{
		if (!IsValidClientRPC(info.Sender, entityNetId) || m_RpcSpamChecks.IsSpamming(RPC.EntityEnteredDropZone))
		{
			return;
		}
		MonkeAgent.IncrementRPCCall(info, "EntityEnteredDropZoneRPC");
		Vector3 v = BitPackUtils.UnpackWorldPosFromNetwork(position);
		if (v.IsValid(10000f))
		{
			Quaternion q = BitPackUtils.UnpackQuaternionFromNetwork(rotation);
			if (q.IsValid() && IsPositionInZone(v) && !((v - reactor.dropZone.transform.position).magnitude > 5f))
			{
				LocalEntityEnteredDropZone(gameEntityManager.GetEntityIdFromNetId(entityNetId), v, q);
			}
		}
	}

	private void LocalEntityEnteredDropZone(GameEntityId entityId, Vector3 position, Quaternion rotation)
	{
		if (reactor == null)
		{
			return;
		}
		GRDropZone dropZone = reactor.dropZone;
		Vector3 linearVelocity = dropZone.GetRepelDirectionWorld() * GhostReactor.DROP_ZONE_REPEL;
		GameEntity gameEntity = gameEntityManager.GetGameEntity(entityId);
		if (gameEntity.heldByActorNumber >= 0 && GamePlayer.TryGetGamePlayer(gameEntity.heldByActorNumber, out var out_gamePlayer))
		{
			int handIndex = out_gamePlayer.FindHandIndex(entityId);
			out_gamePlayer.ClearGrabbedIfHeld(entityId, gameEntityManager);
			if (gameEntity.heldByActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
			{
				GamePlayerLocal.instance.gamePlayer.ClearGrabbed(handIndex);
				GamePlayerLocal.instance.ClearGrabbed(handIndex);
			}
			gameEntity.heldByActorNumber = -1;
			gameEntity.heldByHandIndex = -1;
			gameEntity.OnReleased?.Invoke();
		}
		gameEntity.transform.SetParent(null);
		gameEntity.transform.SetLocalPositionAndRotation(position, rotation);
		if (!(gameEntity.gameObject.GetComponent<GRBadge>() != null))
		{
			Rigidbody component = gameEntity.GetComponent<Rigidbody>();
			if (component != null)
			{
				component.isKinematic = false;
				component.position = position;
				component.rotation = rotation;
				component.linearVelocity = linearVelocity;
				component.angularVelocity = Vector3.zero;
			}
		}
		dropZone.PlayEffect();
	}

	public void RequestRecycleScanItem(GameEntityId gameEntityId)
	{
		if (IsAuthority())
		{
			int netIdFromEntityId = gameEntityManager.GetNetIdFromEntityId(gameEntityId);
			if (netIdFromEntityId != -1)
			{
				SendRPC("ApplyRecycleScanItemRPC", RpcTarget.All, netIdFromEntityId);
			}
		}
	}

	[PunRPC]
	public void ApplyRecycleScanItemRPC(int netId, PhotonMessageInfo info)
	{
		if (IsZoneActive() && IsValidClientRPC(info.Sender) && !m_RpcSpamChecks.IsSpamming(RPC.ApplRecycleScanItem))
		{
			GameEntityId entityIdFromNetId = gameEntityManager.GetEntityIdFromNetId(netId);
			reactor.recycler.ScanItem(entityIdFromNetId);
		}
	}

	public void RequestRecycleItem(int lastHeldActorNumber, GameEntityId toolId, GRTool.GRToolType toolType)
	{
		if (IsAuthority() && !(gameEntityManager == null))
		{
			int netIdFromEntityId = gameEntityManager.GetNetIdFromEntityId(toolId);
			if (netIdFromEntityId != -1)
			{
				SendRPC("ApplyRecycleItemRPC", RpcTarget.All, lastHeldActorNumber, netIdFromEntityId, toolType);
			}
		}
	}

	[PunRPC]
	public void ApplyRecycleItemRPC(int lastHeldActorNumber, int toolNetId, GRTool.GRToolType toolType, PhotonMessageInfo info)
	{
		if (IsZoneActive() && IsValidClientRPC(info.Sender) && !m_RpcSpamChecks.IsSpamming(RPC.ApplyRecycleItem) && gameEntityManager.IsEntityNearPosition(toolNetId, reactor.recycler.transform.position))
		{
			int count = reactor.vrRigs.Count;
			Mathf.FloorToInt((float)reactor.recycler.GetRecycleValue(toolType) / (float)count);
			ProgressionManager.Instance.RecycleTool(toolType, reactor.vrRigs.Count);
			reactor.RefreshScoreboards();
			reactor.recycler.RecycleItem();
			gameEntityManager.DestroyItemLocal(gameEntityManager.GetEntityIdFromNetId(toolNetId));
		}
	}

	public void RequestSentientCorePerformJump(GameEntity entity, Vector3 startPos, Vector3 normal, Vector3 direction, float waitTime)
	{
		if (IsAuthority())
		{
			int netIdFromEntityId = gameEntityManager.GetNetIdFromEntityId(entity.id);
			double num = PhotonNetwork.Time + (double)waitTime;
			SendRPC("SentientCorePerformJumpRPC", RpcTarget.All, netIdFromEntityId, startPos, normal, direction, num);
		}
	}

	[PunRPC]
	public void SentientCorePerformJumpRPC(int entityNetId, Vector3 startPosition, Vector3 surfaceNormal, Vector3 jumpDirection, double jumpStartTime, PhotonMessageInfo info)
	{
		if (!IsValidClientRPC(info.Sender, entityNetId, startPosition) || m_RpcSpamChecks.IsSpamming(RPC.ApplySentientCoreDestination) || !startPosition.IsValid(10000f) || !surfaceNormal.IsValid(10000f) || !jumpDirection.IsValid(10000f) || !double.IsFinite(jumpStartTime) || PhotonNetwork.Time - jumpStartTime > 5.0 || !gameEntityManager.IsEntityNearPosition(entityNetId, startPosition))
		{
			return;
		}
		GameEntity gameEntity = gameEntityManager.GetGameEntity(gameEntityManager.GetEntityIdFromNetId(entityNetId));
		if (!(gameEntity == null))
		{
			GRSentientCore component = gameEntity.GetComponent<GRSentientCore>();
			if (!(component == null))
			{
				component.PerformJump(startPosition, surfaceNormal, jumpDirection, jumpStartTime);
			}
		}
	}

	public override void WriteDataFusion()
	{
	}

	public override void ReadDataFusion()
	{
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
	}

	protected void OnNewPlayerEnteredGhostReactor()
	{
		if (!(reactor == null))
		{
			reactor.VRRigRefresh();
		}
	}

	public void OnEntityZoneClear(GTZone zoneId)
	{
	}

	public void OnZoneCreate()
	{
		if (reactor == null)
		{
			return;
		}
		GRPlayer gRPlayer = GRPlayer.Get(VRRig.LocalRig);
		if (reactor.zone != GTZone.customMaps)
		{
			int newDepthConfigIndex = reactor.PickLevelConfigForDepth(gRPlayer.shuttleData.targetLevel);
			reactor.SetNextDelveDepth(gRPlayer.shuttleData.targetLevel, newDepthConfigIndex);
			reactor.DelveToNextDepth();
			if (reactor.shiftManager != null)
			{
				reactor.shiftManager.SetState(GhostReactorShiftManager.State.WaitingForConnect, force: true);
			}
		}
	}

	public void OnZoneInit()
	{
		if (!(reactor == null) && reactor.zone != GTZone.customMaps)
		{
			reactor.VRRigRefresh();
			if (reactor.employeeTerminal != null)
			{
				reactor.employeeTerminal.Setup();
			}
			if (GRPlayer.Get(NetworkSystem.Instance.LocalPlayer.ActorNumber) != null)
			{
				RequestPlayerAction(GRPlayerAction.SetPodLevel, reactor.toolProgression.GetDropPodLevel());
				RequestPlayerAction(GRPlayerAction.SetPodChassisLevel, reactor.toolProgression.GetDropPodChasisLevel());
			}
		}
	}

	public void OnZoneClear(ZoneClearReason reason)
	{
		if (reactor == null)
		{
			return;
		}
		GRPlayer component = GamePlayerLocal.instance.gamePlayer.GetComponent<GRPlayer>();
		if (component != null)
		{
			GRBadge badge = component.badge;
			if (badge != null && badge.IsAttachedToPlayer())
			{
				component.lastLeftWithBadgeAttachedTime = Time.timeAsDouble;
			}
			component.SendGameEndedTelemetry(isShiftActuallyEnding: false, reason);
		}
		if (reactor.levelGenerator != null)
		{
			reactor.levelGenerator.ClearLevelSections();
		}
		if (reactor.shiftManager != null)
		{
			reactor.shiftManager.OnShiftEnded(0.0, isShiftActuallyEnding: false, reason);
		}
		GRPlayer gRPlayer = GRPlayer.Get(NetworkSystem.Instance.LocalPlayer.ActorNumber);
		if (gRPlayer != null)
		{
			gRPlayer.SetGooParticleSystemEnabled(bIsLeftHand: false, newEnableState: false);
			gRPlayer.SetGooParticleSystemEnabled(bIsLeftHand: true, newEnableState: false);
		}
	}

	public bool IsZoneReady()
	{
		return reactor != null;
	}

	public bool ShouldClearZone()
	{
		return true;
	}

	public void OnCreateGameEntity(GameEntity entity)
	{
	}

	public void SerializeZoneData(BinaryWriter writer)
	{
		GhostReactorShiftManager shiftManager = reactor.shiftManager;
		GhostReactorLevelGenerator levelGenerator = reactor.levelGenerator;
		GRUIPromotionBot promotionBot = reactor.promotionBot;
		GRUIScoreboard[] array = reactor.scoreboards.ToArray();
		writer.Write(reactor.depthLevel);
		writer.Write(reactor.depthConfigIndex);
		writer.Write(reactor.difficultyScalingForCurrentFloor);
		if (shiftManager != null)
		{
			writer.Write(shiftManager.ShiftActive);
			writer.Write(shiftManager.ShiftStartNetworkTime);
			shiftManager.shiftStats.Serialize(writer);
			writer.Write(shiftManager.ShiftId);
			writer.Write(shiftManager.stateStartTime);
			writer.Write((byte)shiftManager.GetState());
			writer.Write(levelGenerator.seed);
		}
		if (promotionBot != null)
		{
			writer.Write(promotionBot.GetCurrentPlayerActorNumber());
			writer.Write((int)promotionBot.currentState);
		}
		for (int i = 0; i < array.Length; i++)
		{
			writer.Write((int)array[i].currentScreen);
		}
		List<GRToolPurchaseStation> toolPurchasingStations = reactor.toolPurchasingStations;
		writer.Write(toolPurchasingStations.Count);
		for (int j = 0; j < toolPurchasingStations.Count; j++)
		{
			writer.Write(toolPurchasingStations[j].ActiveEntryIndex);
		}
		List<GRToolUpgradePurchaseStationFull> toolUpgradePurchaseStationsFull = reactor.toolUpgradePurchaseStationsFull;
		writer.Write(toolUpgradePurchaseStationsFull.Count);
		for (int k = 0; k < toolUpgradePurchaseStationsFull.Count; k++)
		{
			writer.Write(toolUpgradePurchaseStationsFull[k].SelectedShelf);
			writer.Write(toolUpgradePurchaseStationsFull[k].SelectedItem);
			writer.Write(toolUpgradePurchaseStationsFull[k].currentActivePlayerActorNumber);
		}
		List<GhostReactor.EntityTypeRespawnTracker> respawnQueue = reactor.respawnQueue;
		writer.Write(reactor.respawnQueue.Count);
		for (int l = 0; l < respawnQueue.Count; l++)
		{
			writer.Write(respawnQueue[l].entityTypeID);
			writer.Write(respawnQueue[l].entityCreateData);
			writer.Write(respawnQueue[l].entityNextRespawnTime);
		}
		bool value = false;
		writer.Write(value);
	}

	public void DeserializeZoneData(BinaryReader reader)
	{
		GhostReactorShiftManager shiftManager = reactor.shiftManager;
		GhostReactorLevelGenerator levelGenerator = reactor.levelGenerator;
		GRUIPromotionBot promotionBot = reactor.promotionBot;
		GRUIScoreboard[] array = reactor.scoreboards.ToArray();
		int depthLevel = reader.ReadInt32();
		reactor.depthLevel = depthLevel;
		int depthConfigIndex = reader.ReadInt32();
		reactor.depthConfigIndex = depthConfigIndex;
		float difficultyScalingForCurrentFloor = reader.ReadSingle();
		reactor.difficultyScalingForCurrentFloor = difficultyScalingForCurrentFloor;
		if (shiftManager != null)
		{
			bool num = reader.ReadBoolean();
			double shiftStartTime = reader.ReadDouble();
			shiftManager.shiftStats.Deserialize(reader);
			shiftManager.RefreshShiftStatsDisplay();
			string text = reader.ReadString();
			shiftManager.SetShiftId(text);
			shiftManager.stateStartTime = reader.ReadDouble();
			GhostReactorShiftManager.State newState = (GhostReactorShiftManager.State)reader.ReadByte();
			shiftManager.SetState(newState, force: true);
			int inputSeed = reader.ReadInt32();
			if (num)
			{
				levelGenerator.Generate(inputSeed);
				shiftManager.OnShiftStarted(text, shiftStartTime, wasPlayerInAtStart: false, isFirstShift: true);
				reactor.ClearAllHandprints();
			}
		}
		if (promotionBot != null)
		{
			int actorNumber = reader.ReadInt32();
			int state = reader.ReadInt32();
			promotionBot.SetActivePlayerStateChange(actorNumber, state);
		}
		for (int i = 0; i < array.Length; i++)
		{
			array[i].currentScreen = (GRUIScoreboard.ScoreboardScreen)reader.ReadInt32();
		}
		reactor.RefreshScoreboards();
		reactor.RefreshDepth();
		List<GRToolPurchaseStation> toolPurchasingStations = reactor.toolPurchasingStations;
		int num2 = reader.ReadInt32();
		for (int j = 0; j < num2; j++)
		{
			int newSelectedIndex = reader.ReadInt32();
			if (j < toolPurchasingStations.Count && toolPurchasingStations[j] != null)
			{
				toolPurchasingStations[j].OnSelectionUpdate(newSelectedIndex);
			}
		}
		List<GRToolUpgradePurchaseStationFull> toolUpgradePurchaseStationsFull = reactor.toolUpgradePurchaseStationsFull;
		int num3 = reader.ReadInt32();
		for (int k = 0; k < num3; k++)
		{
			int shelf = reader.ReadInt32();
			int item = reader.ReadInt32();
			int activePlayer = reader.ReadInt32();
			if (k < toolUpgradePurchaseStationsFull.Count && toolUpgradePurchaseStationsFull[k] != null)
			{
				toolUpgradePurchaseStationsFull[k].SetSelectedShelfAndItem(shelf, item, fromNetworkRPC: true);
				toolUpgradePurchaseStationsFull[k].SetActivePlayer(activePlayer);
			}
		}
		List<GhostReactor.EntityTypeRespawnTracker> respawnQueue = reactor.respawnQueue;
		respawnQueue.Clear();
		int num4 = reader.ReadInt32();
		for (int l = 0; l < num4; l++)
		{
			GhostReactor.EntityTypeRespawnTracker entityTypeRespawnTracker = new GhostReactor.EntityTypeRespawnTracker();
			entityTypeRespawnTracker.entityTypeID = reader.ReadInt32();
			entityTypeRespawnTracker.entityCreateData = reader.ReadInt64();
			entityTypeRespawnTracker.entityNextRespawnTime = reader.ReadSingle();
			respawnQueue.Add(entityTypeRespawnTracker);
		}
		reader.ReadBoolean();
		reactor.VRRigRefresh();
	}

	public long ProcessMigratedGameEntityCreateData(GameEntity entity, long createData)
	{
		return createData;
	}

	public bool ValidateMigratedGameEntity(int netId, int entityTypeId, Vector3 position, Quaternion rotation, long createData, int actorNr)
	{
		return false;
	}

	public bool ValidateCreateMultipleItems(int zoneId, byte[] compressedStateData, int EntityCount)
	{
		if (EntityCount > 128)
		{
			return false;
		}
		return true;
	}

	public bool ValidateCreateItem(int nedId, int entityTypeId, Vector3 position, Quaternion rotation, long createData, int createdByEntityNetId)
	{
		return true;
	}

	public bool ValidateCreateItemBatchSize(int size)
	{
		return true;
	}

	public void SerializeZoneEntityData(BinaryWriter writer, GameEntity entity)
	{
	}

	public void DeserializeZoneEntityData(BinaryReader reader, GameEntity entity)
	{
	}

	public void OnTapLocal(bool isLeftHand, Vector3 pos, Quaternion rot, GorillaSurfaceOverride surfaceOverride, Vector3 handVelocity)
	{
		if (reactor != null)
		{
			reactor.OnTapLocal(isLeftHand, pos, rot, surfaceOverride);
		}
		if (IsAuthority())
		{
			float num = Math.Clamp(handVelocity.magnitude / 8f, 0f, 1f);
			if (num > 0.25f)
			{
				GRNoiseEventManager.instance.AddNoiseEvent(pos, num);
			}
		}
	}

	public void OnSharedTap(VRRig rig, Vector3 tapPos, float handTapSpeed)
	{
		if (IsAuthority())
		{
			float num = Math.Clamp(handTapSpeed / 8f, 0f, 1f);
			if (num > 0.25f)
			{
				GRNoiseEventManager.instance.AddNoiseEvent(tapPos, num);
			}
		}
	}

	public void SerializeZonePlayerData(BinaryWriter writer, int actorNumber)
	{
		GRPlayer gRPlayer = GRPlayer.Get(actorNumber);
		gRPlayer.SerializeNetworkState(writer, gRPlayer.gamePlayer.rig.OwningNetPlayer);
	}

	public void DeserializeZonePlayerData(BinaryReader reader, int actorNumber)
	{
		GRPlayer player = GRPlayer.Get(actorNumber);
		GRPlayer.DeserializeNetworkStateAndBurn(reader, player, this);
	}

	public bool DebugIsToolStationHacked()
	{
		return false;
	}

	[WeaverGenerated]
	public override void CopyBackingFieldsToState(bool P_0)
	{
		base.CopyBackingFieldsToState(P_0);
	}

	[WeaverGenerated]
	public override void CopyStateToBackingFields()
	{
		base.CopyStateToBackingFields();
	}
}

using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Fusion;
using GorillaTag.Rendering;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

public class GhostReactor : MonoBehaviourTick, IBuildValidation
{
	[Serializable]
	public class TempEnemySpawnInfo
	{
		public GameEntity prefab;

		public Transform spawnMarker;

		public int patrolPath;
	}

	public class EntityTypeRespawnTracker
	{
		public int entityTypeID;

		public long entityCreateData;

		public float entityNextRespawnTime;
	}

	public enum EntityGroupTypes
	{
		EnemyChaser,
		EnemyChaserArmored,
		EnemyRanged,
		EnemyRangedArmored,
		CollectibleFlower,
		BarrierEnergyCostGate,
		BarrierSpectralWall,
		HazardSpectralLiquid
	}

	public enum EnemyType
	{
		Chaser,
		Ranged,
		Phantom,
		Environment,
		CustomMapsEnemy
	}

	public struct EnemyEntityCreateData
	{
		public int respawnCount;

		public int sectionIndex;

		public int patrolIndex;

		private static long PackData(int value, int nbits, int shift)
		{
			return (long)(((ulong)value & (ulong)((1 << nbits) - 1)) << shift);
		}

		private static int UnpackData(long createData, int nbits, int shift)
		{
			return (int)((createData >> shift) & ((1 << nbits) - 1));
		}

		public static EnemyEntityCreateData Unpack(long bits)
		{
			return new EnemyEntityCreateData
			{
				respawnCount = UnpackData(bits, 8, 16),
				sectionIndex = UnpackData(bits, 8, 8),
				patrolIndex = UnpackData(bits, 8, 0)
			};
		}

		public long Pack()
		{
			return PackData(respawnCount, 8, 16) | PackData(sectionIndex, 8, 8) | PackData(patrolIndex, 8, 0);
		}
	}

	public struct ToolEntityCreateData
	{
		public int stationIndex;

		public float decayTime;

		private static long PackData(int value, int nbits, int shift)
		{
			return (long)(((ulong)value & (ulong)((1 << nbits) - 1)) << shift);
		}

		private static int UnpackData(long createData, int nbits, int shift)
		{
			return (int)((createData >> shift) & ((1 << nbits) - 1));
		}

		public static ToolEntityCreateData Unpack(long bits)
		{
			ToolEntityCreateData result = new ToolEntityCreateData
			{
				stationIndex = UnpackData(bits, 8, 0) - 1
			};
			int num = UnpackData(bits, 8, 8);
			result.decayTime = 5f * (float)num;
			return result;
		}

		public long Pack()
		{
			long result = PackData(stationIndex + 1, 8, 0);
			PackData((int)(decayTime / 5f), 8, 8);
			return result;
		}
	}

	public static GhostReactor instance;

	public GTZone zone;

	public Transform restartMarker;

	public PhotonView photonView;

	public AudioSource entryRoomAudio;

	public AudioClip entryRoomDeathSound;

	[FormerlySerializedAs("zoneLimit")]
	public BoxCollider boundsBoxCollider;

	public BoxCollider safeZoneLimit;

	public List<TempEnemySpawnInfo> tempSpawnEnemies;

	public GameEntity overrideEnemySpawn;

	public List<GameEntity> tempSpawnItems;

	public Transform tempSpawnItemsMarker;

	public List<GRUIBuyItem> itemPurchaseStands;

	public List<GRToolPurchaseStation> toolPurchasingStations;

	public GRDebugUpgradeKiosk debugUpgradeKiosk;

	public List<GRUIScoreboard> scoreboards;

	public List<GRCollectibleDispenser> collectibleDispensers = new List<GRCollectibleDispenser>();

	public List<IGRSleepableEntity> sleepableEntities = new List<IGRSleepableEntity>();

	private List<GRBay> bays;

	private List<GRUIStoreDisplay> storeDisplays;

	public GRUIStationEmployeeBadges employeeBadges;

	public GRUIEmployeeTerminal employeeTerminal;

	public GhostReactorShiftManager shiftManager;

	public GhostReactorLevelGenerator levelGenerator;

	public GRCurrencyDepositor currencyDepositor;

	public GRSeedExtractor seedExtractor;

	public GRDistillery distillery;

	public GRToolProgressionManager toolProgression;

	public GRToolUpgradeStation upgradeStation;

	public List<GRToolUpgradePurchaseStationFull> toolUpgradePurchaseStationsFull;

	public GRRecycler recycler;

	public List<EntityTypeRespawnTracker> respawnQueue = new List<EntityTypeRespawnTracker>();

	public List<float> difficultyScalingPerPlayer = new List<float>(10);

	public float respawnTime = 10f;

	public float respawnMinDistToPlayer = 8f;

	public float difficultyScalingForCurrentFloor = 1f;

	public LayerMask envLayerMask;

	public Material handPrintMaterial;

	public Mesh handPrintMesh;

	public float handPrintScale;

	public float handPrintInkTime = 30f;

	public float handPrintFadeTime = 600f;

	private const int handPrintMaxCount = 1000;

	private List<Matrix4x4> handPrintLocations = new List<Matrix4x4>(1000);

	private List<float> handPrintData = new List<float>(1000);

	private MaterialPropertyBlock handPrintMPB;

	[ReadOnly]
	public List<GRReviveStation> reviveStations;

	public List<GRVendingMachine> vendingMachines;

	public List<VRRig> vrRigs;

	private float collectibleDispenserUpdateFrequency = 3f;

	private double lastCollectibleDispenserUpdateTime = -10.0;

	private int sentientCoreUpdateIndex;

	private SRand randomGenerator;

	[ReadOnly]
	public int depthLevel;

	[ReadOnly]
	public int depthConfigIndex;

	public Dictionary<int, double> playerProgressionData;

	public GRDropZone dropZone;

	public static float DROP_ZONE_REPEL = 2.25f;

	public ZoneShaderSettings zoneShaderSettings;

	public GRUIPromotionBot promotionBot;

	private bool isRefreshing;

	public GhostReactorManager grManager;

	private float handPrintTimeLeft = -1000f;

	private float handPrintTimeRight = -1000f;

	private int handPrintCombineTestDelta = 1;

	private float lastBroadcastHandTapTime;

	private float broadcastHandTapDelay = 0.3f;

	public int NumActivePlayers => vrRigs.Count;

	public static GhostReactor Get(GameEntity gameEntity)
	{
		GhostReactorManager ghostReactorManager = GhostReactorManager.Get(gameEntity);
		if (ghostReactorManager == null)
		{
			return null;
		}
		return ghostReactorManager.reactor;
	}

	private void Awake()
	{
		instance = this;
		reviveStations = new List<GRReviveStation>();
		GetComponentsInChildren(reviveStations);
		for (int i = 0; i < reviveStations.Count; i++)
		{
			reviveStations[i].Init(this, i);
		}
		vrRigs = new List<VRRig>();
		for (int j = 0; j < itemPurchaseStands.Count; j++)
		{
			if (itemPurchaseStands[j] == null)
			{
				Debug.LogErrorFormat("Null Item Purchase Stand {0}", j);
			}
			else
			{
				itemPurchaseStands[j].Setup(j);
			}
		}
		for (int k = 0; k < toolPurchasingStations.Count; k++)
		{
			if (toolPurchasingStations[k] == null)
			{
				Debug.LogErrorFormat("Null Tool Purchasing Station {0}", k);
			}
			else
			{
				toolPurchasingStations[k].PurchaseStationId = k;
			}
		}
		if (promotionBot != null)
		{
			promotionBot.Init(this);
		}
		randomGenerator = new SRand(UnityEngine.Random.Range(0, int.MaxValue));
		handPrintMPB = new MaterialPropertyBlock();
		handPrintMPB.SetFloatArray("_HandPrintData", new float[1024]);
		bays = new List<GRBay>(32);
		GetComponentsInChildren(includeInactive: false, bays);
		storeDisplays = new List<GRUIStoreDisplay>();
		GetComponentsInChildren(includeInactive: false, storeDisplays);
	}

	private new void OnEnable()
	{
		base.OnEnable();
		if (zone == GTZone.customMaps)
		{
			return;
		}
		GTDev.Log($"GhostReactor::OnEnable getting manager for zone {zone}");
		GameEntityManager managerForZone = GameEntityManager.GetManagerForZone(zone);
		if (managerForZone == null)
		{
			Debug.LogErrorFormat("No GameEntityManager found for zone {0}", zone);
			return;
		}
		grManager = managerForZone.ghostReactorManager;
		if (grManager == null)
		{
			Debug.LogErrorFormat("No GhostReactorManager found for zone {0}", zone);
			return;
		}
		grManager.reactor = this;
		grManager.gameEntityManager.boundsBoxCollider = boundsBoxCollider;
		if (GameLightingManager.instance != null && zone != GTZone.customMaps)
		{
			GameLightingManager.instance.ZoneEnableCustomDynamicLighting(enable: true);
		}
		VRRigCache.OnRigActivated += OnVRRigsChanged;
		VRRigCache.OnRigDeactivated += OnVRRigsChanged;
		VRRigCache.OnRigNameChanged += OnVRRigsChanged;
		if (NetworkSystem.Instance != null)
		{
			NetworkSystem.Instance.OnMultiplayerStarted += new Action(OnLocalPlayerConnectedToRoom);
		}
		for (int i = 0; i < toolPurchasingStations.Count; i++)
		{
			toolPurchasingStations[i].Init(grManager, this);
		}
		if (debugUpgradeKiosk != null)
		{
			debugUpgradeKiosk.Init(grManager, this);
		}
		if (currencyDepositor != null)
		{
			currencyDepositor.Init(this);
		}
		if (distillery != null)
		{
			distillery.Init(this);
		}
		if (seedExtractor != null)
		{
			seedExtractor.Init(toolProgression, this);
		}
		if (levelGenerator != null)
		{
			levelGenerator.Init(this);
		}
		if (employeeBadges != null)
		{
			employeeBadges.Init(this);
		}
		if (toolProgression != null)
		{
			toolProgression.Init(this);
			toolProgression.OnProgressionUpdated += OnProgressionUpdated;
		}
		if (shiftManager != null)
		{
			shiftManager.Init(grManager);
		}
		for (int j = 0; j < toolUpgradePurchaseStationsFull.Count; j++)
		{
			toolUpgradePurchaseStationsFull[j].Init(toolProgression, this);
		}
		GRElevatorManager._instance.InitShuttles(this);
		if (recycler != null)
		{
			recycler.Init(this);
		}
		if (zoneShaderSettings != null)
		{
			zoneShaderSettings.BecomeActiveInstance(force: true);
		}
		for (int k = 0; k < bays.Count; k++)
		{
			bays[k].Setup(this);
		}
		for (int l = 0; l < storeDisplays.Count; l++)
		{
			storeDisplays[l].Setup(-1, this);
		}
		RefreshDepth();
	}

	public void EnableGhostReactorForVirtualStump()
	{
		instance = this;
		RefreshReviveStations();
		OnEnable();
	}

	public void RefreshReviveStations(bool searchScene = false)
	{
		reviveStations = new List<GRReviveStation>();
		GetComponentsInChildren(reviveStations);
		if (searchScene)
		{
			reviveStations.AddRange(UnityEngine.Object.FindObjectsByType<GRReviveStation>(FindObjectsInactive.Include, FindObjectsSortMode.None));
		}
		for (int i = 0; i < reviveStations.Count; i++)
		{
			reviveStations[i].Init(this, i);
		}
	}

	private new void OnDisable()
	{
		base.OnDisable();
		if (zone != GTZone.customMaps)
		{
			GameLightingManager.instance.ZoneEnableCustomDynamicLighting(enable: false);
			VRRigCache.OnRigActivated -= OnVRRigsChanged;
			VRRigCache.OnRigDeactivated -= OnVRRigsChanged;
			VRRigCache.OnRigNameChanged -= OnVRRigsChanged;
			if (toolProgression != null)
			{
				toolProgression.OnProgressionUpdated -= OnProgressionUpdated;
			}
			if (NetworkSystem.Instance != null)
			{
				NetworkSystem.Instance.OnMultiplayerStarted -= new Action(OnLocalPlayerConnectedToRoom);
			}
		}
	}

	private void OnProgressionUpdated()
	{
		if (toolProgression != null)
		{
			UpdateLocalPlayerFromProgression();
		}
	}

	public void UpdateLocalPlayerFromProgression()
	{
		GRPlayer local = GRPlayer.GetLocal();
		if (!(local != null))
		{
			return;
		}
		int dropPodLevel = toolProgression.GetDropPodLevel();
		if (local.dropPodLevel != dropPodLevel)
		{
			local.dropPodLevel = dropPodLevel;
			Debug.LogFormat("Drop Pod UpdateLocalPlayerFromProgression Level {0} {1} {2}", grManager.IsZoneActive(), local.dropPodLevel, local.dropPodChasisLevel);
			if (grManager.IsZoneActive())
			{
				grManager.RequestPlayerAction(GhostReactorManager.GRPlayerAction.SetPodLevel, dropPodLevel);
			}
		}
		int dropPodChasisLevel = toolProgression.GetDropPodChasisLevel();
		if (local.dropPodChasisLevel != dropPodChasisLevel)
		{
			local.dropPodChasisLevel = dropPodChasisLevel;
			Debug.LogFormat("Drop Pod UpdateLocalPlayerFromProgression Level {0} {1} {2}", grManager.IsZoneActive(), local.dropPodLevel, local.dropPodChasisLevel);
			if (grManager.IsZoneActive())
			{
				grManager.RequestPlayerAction(GhostReactorManager.GRPlayerAction.SetPodChassisLevel, dropPodChasisLevel);
			}
		}
		if ((bool)local.badge)
		{
			local.badge.RefreshText(PhotonNetwork.LocalPlayer);
		}
		RefreshStore();
	}

	public GRPatrolPath GetPatrolPath(long createData)
	{
		if (levelGenerator == null)
		{
			return null;
		}
		return levelGenerator.GetPatrolPath(createData);
	}

	public override void Tick()
	{
		if (grManager == null)
		{
			return;
		}
		_ = Time.deltaTime;
		if (grManager.gameEntityManager.IsAuthority())
		{
			if (Time.timeAsDouble - lastCollectibleDispenserUpdateTime > (double)collectibleDispenserUpdateFrequency)
			{
				lastCollectibleDispenserUpdateTime = Time.timeAsDouble;
				for (int i = 0; i < collectibleDispensers.Count; i++)
				{
					if (collectibleDispensers[i] != null && collectibleDispensers[i].ReadyToDispenseNewCollectible)
					{
						collectibleDispensers[i].RequestDispenseCollectible();
					}
				}
			}
			if (sleepableEntities.Count > 0)
			{
				sentientCoreUpdateIndex = Mathf.Max(0, sentientCoreUpdateIndex % sleepableEntities.Count);
				if (sentientCoreUpdateIndex < sleepableEntities.Count)
				{
					IGRSleepableEntity iGRSleepableEntity = sleepableEntities[sentientCoreUpdateIndex];
					float num = iGRSleepableEntity.WakeUpRadius * iGRSleepableEntity.WakeUpRadius;
					float num2 = (iGRSleepableEntity.WakeUpRadius + 0.5f) * (iGRSleepableEntity.WakeUpRadius + 0.5f);
					bool flag = false;
					bool flag2 = false;
					for (int j = 0; j < vrRigs.Count; j++)
					{
						GRPlayer component = vrRigs[j].GetComponent<GRPlayer>();
						if (!(component == null) && component.State != GRPlayer.GRPlayerState.Ghost)
						{
							float sqrMagnitude = (iGRSleepableEntity.Position - vrRigs[j].bodyTransform.position).sqrMagnitude;
							if (sqrMagnitude < num2)
							{
								flag = true;
							}
							if (sqrMagnitude < num)
							{
								flag2 = true;
								break;
							}
						}
					}
					bool flag3 = iGRSleepableEntity.IsSleeping();
					if (flag3 && flag2)
					{
						iGRSleepableEntity.WakeUp();
					}
					else if (!flag3 && !flag)
					{
						iGRSleepableEntity.Sleep();
					}
					sentientCoreUpdateIndex++;
				}
			}
		}
		bool flag4 = false;
		foreach (EntityTypeRespawnTracker item in respawnQueue)
		{
			item.entityNextRespawnTime -= Time.deltaTime;
			if (item.entityNextRespawnTime < 0f)
			{
				item.entityNextRespawnTime = 0f;
				flag4 = true;
				if (grManager.gameEntityManager.IsAuthority())
				{
					levelGenerator.RespawnEntity(item.entityTypeID, item.entityCreateData, GameEntityId.Invalid);
				}
			}
		}
		if (flag4)
		{
			respawnQueue.RemoveAll((EntityTypeRespawnTracker e) => e.entityNextRespawnTime <= 0f);
		}
		UpdateHandprints(Time.deltaTime);
	}

	private void OnLocalPlayerConnectedToRoom()
	{
		GRPlayer gRPlayer = GRPlayer.Get(VRRig.LocalRig);
		if (gRPlayer != null)
		{
			gRPlayer.Reset();
		}
		if (shiftManager != null)
		{
			shiftManager.shiftStats.ResetShiftStats();
			shiftManager.RefreshShiftStatsDisplay();
		}
	}

	private void OnVRRigsChanged(RigContainer container)
	{
		VRRigRefresh();
	}

	public void VRRigRefresh()
	{
		if (isRefreshing)
		{
			return;
		}
		isRefreshing = true;
		vrRigs.Clear();
		vrRigs.Add(VRRig.LocalRig);
		VRRigCache.Instance.GetAllUsedRigs(vrRigs);
		vrRigs.Sort(delegate(VRRig a, VRRig b)
		{
			if (a == null || a.OwningNetPlayer == null)
			{
				return 1;
			}
			return (b == null || b.OwningNetPlayer == null) ? (-1) : a.OwningNetPlayer.ActorNumber.CompareTo(b.OwningNetPlayer.ActorNumber);
		});
		if (promotionBot != null)
		{
			promotionBot.Refresh();
		}
		RefreshScoreboards();
		RefreshDepth();
		RefreshStore();
		GRPlayer gRPlayer = GRPlayer.Get(VRRig.LocalRig);
		if (gRPlayer != null && vrRigs.Count > gRPlayer.maxNumberOfPlayersInShift)
		{
			gRPlayer.maxNumberOfPlayersInShift = vrRigs.Count;
		}
		isRefreshing = false;
	}

	public void UpdateScoreboardScreen(GRUIScoreboard.ScoreboardScreen newScreen)
	{
		for (int i = 0; i < scoreboards.Count; i++)
		{
			scoreboards[i].SwitchToScreen(newScreen);
		}
		RefreshScoreboards();
	}

	public void RefreshScoreboards()
	{
		for (int i = 0; i < scoreboards.Count; i++)
		{
			if (scoreboards[i] == null)
			{
				continue;
			}
			scoreboards[i].Refresh(vrRigs);
			if (shiftManager != null)
			{
				if (shiftManager.ShiftActive)
				{
					scoreboards[i].total.text = "-AWAITING SHIFT END-";
				}
				else if (shiftManager.ShiftTotalEarned < 0)
				{
					scoreboards[i].total.text = "-SHIFT NOT ACTIVE-";
				}
				else
				{
					scoreboards[i].total.text = shiftManager.ShiftTotalEarned.ToString();
				}
			}
		}
	}

	public int GetItemCost(int entityTypeId)
	{
		if (!grManager.gameEntityManager.PriceLookup(entityTypeId, out var price))
		{
			return 100;
		}
		return price;
	}

	public void UpdateRemoteScoreboardScreen(GRUIScoreboard.ScoreboardScreen scoreboardPage)
	{
		GameEntityManager managerForZone = GameEntityManager.GetManagerForZone(zone);
		if (managerForZone != null && managerForZone.ghostReactorManager != null)
		{
			managerForZone.ghostReactorManager.photonView.RPC("BroadcastScoreboardPage", RpcTarget.Others, scoreboardPage);
		}
	}

	public void SetNextDelveDepth(int newLevel, int newDepthConfigIndex)
	{
		depthLevel = newLevel;
		depthLevel = Mathf.Clamp(depthLevel, 0, levelGenerator.depthConfigs.Count);
		if (depthLevel >= 0 && zone == GTZone.ghostReactorDrill && PhotonNetwork.InRoom && !NetworkSystem.Instance.SessionIsPrivate && grManager.IsAuthority())
		{
			int joinDepthSectionFromLevel = GetJoinDepthSectionFromLevel(depthLevel);
			Hashtable hashtable = new Hashtable { 
			{
				"ghostReactorDepth",
				joinDepthSectionFromLevel.ToString()
			} };
			Debug.LogFormat("GR Room Param Set {0} {1}", "ghostReactorDepth", hashtable["ghostReactorDepth"]);
			PhotonNetwork.CurrentRoom.SetCustomProperties(hashtable);
		}
		depthConfigIndex = newDepthConfigIndex;
	}

	public static int GetJoinDepthSectionFromLevel(int depthLevel)
	{
		if (depthLevel < 4)
		{
			return 0;
		}
		if (depthLevel < 10)
		{
			return 1;
		}
		if (depthLevel < 15)
		{
			return 2;
		}
		if (depthLevel < 20)
		{
			return 3;
		}
		if (depthLevel < 25)
		{
			return 5;
		}
		return 6;
	}

	public void DelveToNextDepth()
	{
		if (shiftManager != null)
		{
			shiftManager.authorizedToDelveDeeper = false;
		}
		RefreshDepth();
	}

	public int PickLevelConfigForDepth(int depthLevel)
	{
		if (zone == GTZone.customMaps)
		{
			return 0;
		}
		GhostReactorLevelDepthConfig depthLevelConfig = GetDepthLevelConfig(depthLevel);
		int num = 0;
		for (int i = 0; i < depthLevelConfig.options.Count; i++)
		{
			num += depthLevelConfig.options[i].weight;
		}
		int num2 = UnityEngine.Random.Range(0, num + 1);
		for (int j = 0; j < depthLevelConfig.options.Count; j++)
		{
			if (depthLevelConfig.options[j].weight >= num2)
			{
				return j;
			}
			num2 -= depthLevelConfig.options[j].weight;
		}
		return 0;
	}

	public void RefreshDepth()
	{
		if (shiftManager != null)
		{
			shiftManager.RefreshDepthDisplay();
		}
		RefreshBays();
	}

	public int GetDepthLevel()
	{
		return depthLevel;
	}

	public int GetDepthConfigIndex()
	{
		return depthConfigIndex;
	}

	public GhostReactorLevelDepthConfig GetDepthLevelConfig(int level)
	{
		if (levelGenerator == null)
		{
			return null;
		}
		level = Mathf.Clamp(level, 0, levelGenerator.depthConfigs.Count - 1);
		return levelGenerator.depthConfigs[level];
	}

	public GhostReactorLevelGenConfig GetCurrLevelGenConfig()
	{
		if (levelGenerator == null)
		{
			return null;
		}
		int value = GetDepthLevel();
		value = Mathf.Clamp(value, 0, levelGenerator.depthConfigs.Count - 1);
		depthConfigIndex = Mathf.Clamp(depthConfigIndex, 0, levelGenerator.depthConfigs[value].options.Count - 1);
		return levelGenerator.depthConfigs[value].options[depthConfigIndex].levelConfig;
	}

	public void RefreshStore()
	{
		for (int i = 0; i < storeDisplays.Count; i++)
		{
			storeDisplays[i].Setup(PhotonNetwork.LocalPlayer.ActorNumber, this);
		}
	}

	public void RefreshBays()
	{
		for (int i = 0; i < bays.Count; i++)
		{
			bays[i].Refresh();
		}
	}

	public void UpdateHandprints(float deltaTime)
	{
		int num = handPrintData.Count - 1000;
		if (num > 0)
		{
			handPrintData.RemoveRange(0, num);
			handPrintLocations.RemoveRange(0, num);
		}
		_ = Time.time;
		for (int num2 = handPrintData.Count - 1; num2 >= 0; num2--)
		{
			handPrintData[num2] -= deltaTime;
			if (num2 + handPrintCombineTestDelta < handPrintData.Count)
			{
				if (handPrintData[num2 + handPrintCombineTestDelta] > handPrintFadeTime - 3f)
				{
					continue;
				}
				Matrix4x4 matrix4x = handPrintLocations[num2];
				Matrix4x4 matrix4x2 = handPrintLocations[num2 + handPrintCombineTestDelta];
				if (new Vector3(matrix4x.m03 - matrix4x2.m03, matrix4x.m13 - matrix4x2.m13, matrix4x.m23 - matrix4x2.m23).sqrMagnitude < handPrintScale * handPrintScale)
				{
					handPrintData[num2] -= deltaTime * (float)handPrintData.Count * 50f;
				}
			}
			if (handPrintData[num2] < 0f)
			{
				handPrintData.RemoveAt(num2);
				handPrintLocations.RemoveAt(num2);
			}
		}
		if (handPrintData.Count > 0)
		{
			handPrintCombineTestDelta = (handPrintCombineTestDelta + 1) % handPrintData.Count;
			if (handPrintCombineTestDelta == 0)
			{
				handPrintCombineTestDelta = 1;
			}
		}
		else
		{
			handPrintCombineTestDelta = 1;
		}
		if (handPrintMaterial != null)
		{
			handPrintMaterial.SetFloat("_FadeDuration", handPrintFadeTime);
			handPrintMaterial.enableInstancing = true;
		}
		int num3 = Mathf.Min(Math.Min(1000, 1023), handPrintLocations.Count);
		if (num3 > 0)
		{
			handPrintMPB.Clear();
			handPrintMPB.SetFloatArray("_HandPrintData", handPrintData.GetRange(0, num3));
			handPrintMPB.SetFloat("_FadeDuration", handPrintFadeTime);
			RenderParams renderParams = new RenderParams(handPrintMaterial);
			renderParams.shadowCastingMode = ShadowCastingMode.Off;
			renderParams.receiveShadows = false;
			renderParams.layer = base.gameObject.layer;
			renderParams.matProps = handPrintMPB;
			renderParams.worldBounds = new Bounds(Vector3.zero, Vector3.one * 2000f);
			RenderParams rparams = renderParams;
			Graphics.RenderMeshInstanced(in rparams, handPrintMesh, 0, handPrintLocations.GetRange(0, num3));
		}
		GRPlayer gRPlayer = GRPlayer.Get(VRRig.LocalRig);
		if (gRPlayer != null)
		{
			if (Time.time - handPrintTimeLeft >= handPrintInkTime)
			{
				gRPlayer.SetGooParticleSystemEnabled(bIsLeftHand: true, newEnableState: false);
			}
			if (Time.time - handPrintTimeRight >= handPrintInkTime)
			{
				gRPlayer.SetGooParticleSystemEnabled(bIsLeftHand: false, newEnableState: false);
			}
		}
	}

	public void OnTapLocal(bool isLeftHand, Vector3 pos, Quaternion orient, GorillaSurfaceOverride surfaceOverride)
	{
		GRPlayer gRPlayer = GRPlayer.Get(VRRig.LocalRig);
		if (gRPlayer == null)
		{
			return;
		}
		if (surfaceOverride != null && surfaceOverride.overrideIndex == 79)
		{
			gRPlayer.SetGooParticleSystemEnabled(isLeftHand, newEnableState: true);
			if (isLeftHand)
			{
				handPrintTimeLeft = Time.time;
			}
			else
			{
				handPrintTimeRight = Time.time;
			}
			return;
		}
		float num = (isLeftHand ? handPrintTimeLeft : handPrintTimeRight);
		if (Time.time - num < handPrintInkTime && (Time.time < lastBroadcastHandTapTime || Time.time > lastBroadcastHandTapTime + broadcastHandTapDelay))
		{
			GameEntityManager managerForZone = GameEntityManager.GetManagerForZone(zone);
			if (managerForZone != null && managerForZone.ghostReactorManager != null)
			{
				managerForZone.ghostReactorManager.photonView.RPC("BroadcastHandprint", RpcTarget.All, pos, orient);
			}
			lastBroadcastHandTapTime = Time.time;
		}
	}

	public void AddHandprint(Vector3 pos, Quaternion orient)
	{
		Matrix4x4 item = default(Matrix4x4);
		item.SetTRS(pos, orient * Quaternion.Euler(90f, 0f, 180f), Vector3.one * handPrintScale);
		handPrintLocations.Add(item);
		handPrintData.Add(handPrintFadeTime);
	}

	public void ClearAllHandprints()
	{
		handPrintData.Clear();
		handPrintLocations.Clear();
	}

	public void OnAbilityDie(GameEntity entity, float forcedRespawn = -1f)
	{
		EnemyEntityCreateData enemyEntityCreateData = EnemyEntityCreateData.Unpack(entity.createData);
		if (enemyEntityCreateData.respawnCount == 0)
		{
			return;
		}
		if (grManager.GetBossEntity() != null)
		{
			GREnemyBossMoon component = grManager.GetBossEntity().GetComponent<GREnemyBossMoon>();
			if (component != null && component.BossHasRevealed)
			{
				return;
			}
		}
		EntityTypeRespawnTracker entityTypeRespawnTracker = new EntityTypeRespawnTracker();
		entityTypeRespawnTracker.entityTypeID = entity.typeId;
		entityTypeRespawnTracker.entityCreateData = enemyEntityCreateData.Pack();
		entityTypeRespawnTracker.entityNextRespawnTime = ((forcedRespawn < 0f) ? respawnTime : forcedRespawn);
		respawnQueue.Add(entityTypeRespawnTracker);
	}

	public void ClearAllRespawns()
	{
		respawnQueue.Clear();
	}

	bool IBuildValidation.BuildValidationCheck()
	{
		return true;
	}
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Critters.Scripts;
using Fusion;
using GorillaExtensions;
using GorillaNetworking;
using Photon.Pun;
using UnityEngine;
using Utilities;

[NetworkBehaviourWeaved(0)]
public class CrittersManager : NetworkComponent, IRequestableOwnershipGuardCallbacks, IBuildValidation, ITickSystemTick
{
	[Flags]
	public enum AllowGrabbingFlags
	{
		None = 0,
		OutOfHands = 1,
		FromBags = 2,
		EntireBag = 4
	}

	public enum CritterEvent
	{
		StunExplosion,
		NoiseMakerTriggered,
		StickyDeployed,
		StickyTriggered
	}

	public CritterIndex creatureIndex;

	public static volatile CrittersManager instance;

	public LayerMask movementLayers;

	public LayerMask objectLayers;

	public LayerMask containerLayer;

	[ReadOnly]
	public List<CrittersActor> crittersActors;

	[ReadOnly]
	public List<CrittersActor> allActors;

	[ReadOnly]
	public List<CrittersPawn> crittersPawns;

	[ReadOnly]
	public List<CrittersActor> despawnableActors;

	[ReadOnly]
	public List<CrittersActor> newlyDisabledActors;

	[ReadOnly]
	public List<CrittersRigActorSetup> rigActorSetups;

	[ReadOnly]
	public List<CrittersActorSpawner> actorSpawners;

	[NonSerialized]
	private List<CrittersActor> persistentActors = new List<CrittersActor>();

	public Dictionary<int, CrittersActor> actorById;

	public Dictionary<CrittersPawn, List<CrittersActor>> awareOfActors;

	public Dictionary<VRRig, CrittersRigActorSetup> rigSetupByRig;

	private int allActorsCount;

	public bool intialized;

	private List<int> updatesToSend;

	public int actorsPerInitializationCall = 5;

	public float actorsInitializationCallCooldown = 0.2f;

	public Transform poolParent;

	public List<object> objList;

	public double spawnDelay;

	private double lastSpawnTime;

	public float softJointGracePeriod = 0.1f;

	private List<CrittersRegion> _spawnRegions;

	private int _currentRegionIndex = -1;

	private static CrittersActorGrabber _rightGrabber;

	private static CrittersActorGrabber _leftGrabber;

	public float springForce = 1000f;

	public float springAngularForce = 100f;

	public float damperForce = 10f;

	public float damperAngularForce = 1f;

	public float lightMass = 0.05f;

	public float heavyMass = 2f;

	public float overlapDistanceMax = 0.01f;

	public float fastThrowThreshold = 3f;

	public float fastThrowMultiplier = 1.5f;

	private Dictionary<CrittersActor.CrittersActorType, int> poolIndexDict;

	public AllowGrabbingFlags privateRoomGrabbingFlags;

	public AllowGrabbingFlags publicRoomGrabbingFlags;

	public float MaxAttachSpeed = 0.04f;

	private float binDimensionXMin;

	private float binDimensionZMin;

	public Transform crittersRange;

	public int totalBinsApproximate = 400;

	private float xLength;

	private float zLength;

	private int binXCount;

	private int binZCount;

	private float individualBinSide;

	private List<CrittersActor>[] actorBins;

	private bool[] priorityBins;

	private Dictionary<CrittersActor, int> actorBinIndices;

	private List<CrittersActor> nearbyActors;

	private List<NetPlayer> playersToUpdate;

	public CrittersPool crittersPool;

	private int lowPriorityActorsPerFrame = 5;

	private int lowPriorityIndex;

	private int spawnerIndex;

	private int despawnIndex;

	private List<CrittersActor> lowPriorityPawnsToProcess;

	private Dictionary<CrittersActor.CrittersActorType, float> despawnDecayValue;

	public float decayRate = 60f;

	private CrittersActor.CrittersActorType[] actorTypes;

	public float maxGrabDistance = 25f;

	public RequestableOwnershipGuard guard;

	private List<VRRig> allRigs;

	private bool localInZone;

	private List<int> updatingPlayers;

	private bool hasNewlyInitialized;

	private float initRequestCooldown = 10f;

	private float lastRequest;

	public int poolCount = 100;

	public int despawnThreshold = 20;

	private Dictionary<CrittersActor.CrittersActorType, int> poolCounts;

	private Dictionary<CrittersActor.CrittersActorType, List<CrittersActor>> actorPools;

	public GameObject foodPrefab;

	public GameObject creaturePrefab;

	public GameObject noisePrefab;

	public GameObject grabberPrefab;

	public GameObject cagePrefab;

	public GameObject foodSpawnerPrefab;

	public GameObject stunBombPrefab;

	public GameObject bodyAttachPointPrefab;

	public GameObject bagPrefab;

	public GameObject noiseMakerPrefab;

	public GameObject stickyTrapPrefab;

	public GameObject stickyGooPrefab;

	public int universalActorId;

	public int rigActorId;

	private CallLimiter critterEventCallLimit = new CallLimiter(10, 0.5f);

	public static bool hasInstance { get; private set; }

	public bool TickRunning { get; set; }

	public bool allowGrabbingEntireBag
	{
		get
		{
			if (!NetworkSystem.Instance.SessionIsPrivate)
			{
				return (AllowGrabbingFlags.EntireBag & publicRoomGrabbingFlags) != 0;
			}
			return (AllowGrabbingFlags.EntireBag & privateRoomGrabbingFlags) != 0;
		}
	}

	public bool allowGrabbingOutOfHands
	{
		get
		{
			if (!NetworkSystem.Instance.SessionIsPrivate)
			{
				return (AllowGrabbingFlags.OutOfHands & publicRoomGrabbingFlags) != 0;
			}
			return (AllowGrabbingFlags.OutOfHands & privateRoomGrabbingFlags) != 0;
		}
	}

	public bool allowGrabbingFromBags
	{
		get
		{
			if (!NetworkSystem.Instance.SessionIsPrivate)
			{
				return (AllowGrabbingFlags.FromBags & publicRoomGrabbingFlags) != 0;
			}
			return (AllowGrabbingFlags.FromBags & privateRoomGrabbingFlags) != 0;
		}
	}

	public bool LocalInZone => localInZone;

	public event Action<CritterEvent, int, Vector3, Quaternion> OnCritterEventReceived;

	public void LoadGrabSettings()
	{
		PlayFabTitleDataCache.Instance.GetTitleData("PublicCrittersGrabSettings", delegate(string data)
		{
			if (int.TryParse(data, out var result))
			{
				publicRoomGrabbingFlags = (AllowGrabbingFlags)result;
			}
		}, delegate
		{
		});
		PlayFabTitleDataCache.Instance.GetTitleData("PrivateCrittersGrabSettings", delegate(string data)
		{
			if (int.TryParse(data, out var result))
			{
				privateRoomGrabbingFlags = (AllowGrabbingFlags)result;
			}
		}, delegate
		{
		});
	}

	public bool BuildValidationCheck()
	{
		if (guard == null)
		{
			Debug.LogError("requestable owner guard missing", base.gameObject);
			return false;
		}
		if (crittersPool == null)
		{
			Debug.LogError("critters pool missing", base.gameObject);
			return false;
		}
		return true;
	}

	protected override void Start()
	{
		base.Start();
		instance.LoadGrabSettings();
		CheckInitialize();
	}

	public static void InitializeCrittersManager()
	{
		if (hasInstance)
		{
			return;
		}
		hasInstance = true;
		instance = UnityEngine.Object.FindAnyObjectByType<CrittersManager>();
		instance.crittersActors = new List<CrittersActor>();
		instance.crittersPawns = new List<CrittersPawn>();
		instance.awareOfActors = new Dictionary<CrittersPawn, List<CrittersActor>>();
		instance.despawnableActors = new List<CrittersActor>();
		instance.newlyDisabledActors = new List<CrittersActor>();
		instance.rigActorSetups = new List<CrittersRigActorSetup>();
		instance.rigSetupByRig = new Dictionary<VRRig, CrittersRigActorSetup>();
		instance.updatesToSend = new List<int>();
		instance.objList = new List<object>();
		instance.lowPriorityPawnsToProcess = new List<CrittersActor>();
		instance.actorSpawners = UnityEngine.Object.FindObjectsByType<CrittersActorSpawner>(FindObjectsSortMode.None).ToList();
		instance._spawnRegions = CrittersRegion.Regions;
		instance.poolCounts = new Dictionary<CrittersActor.CrittersActorType, int>();
		instance.despawnDecayValue = new Dictionary<CrittersActor.CrittersActorType, float>();
		instance.actorTypes = (CrittersActor.CrittersActorType[])Enum.GetValues(typeof(CrittersActor.CrittersActorType));
		instance.poolIndexDict = new Dictionary<CrittersActor.CrittersActorType, int>();
		for (int i = 0; i < instance.actorTypes.Length; i++)
		{
			instance.poolCounts[instance.actorTypes[i]] = 0;
			instance.despawnDecayValue[instance.actorTypes[i]] = 0f;
		}
		instance.PopulatePools();
		List<CrittersRigActorSetup> list = UnityEngine.Object.FindObjectsByType<CrittersRigActorSetup>(FindObjectsSortMode.None).ToList();
		for (int j = 0; j < list.Count; j++)
		{
			if (list[j].enabled)
			{
				RegisterRigActorSetup(list[j]);
			}
		}
		CrittersActorGrabber[] array = UnityEngine.Object.FindObjectsByType<CrittersActorGrabber>(FindObjectsSortMode.None);
		for (int k = 0; k < array.Length; k++)
		{
			if (array[k].isLeft)
			{
				_leftGrabber = array[k];
			}
			else
			{
				_rightGrabber = array[k];
			}
		}
		if (instance.guard.IsNotNull())
		{
			instance.guard.AddCallbackTarget(instance);
		}
		RoomSystem.JoinedRoomEvent += new Action(instance.JoinedRoomEvent);
		RoomSystem.LeftRoomEvent += new Action(instance.LeftRoomEvent);
	}

	private new void OnEnable()
	{
		NetworkBehaviourUtils.InternalOnEnable(this);
		base.OnEnable();
		TickSystem<object>.AddTickCallback(this);
	}

	private new void OnDisable()
	{
		NetworkBehaviourUtils.InternalOnDisable(this);
		base.OnDisable();
		TickSystem<object>.RemoveTickCallback(this);
	}

	private void ResetRoom()
	{
		lastSpawnTime = 0.0;
		for (int i = 0; i < allActors.Count; i++)
		{
			CrittersActor crittersActor = allActors[i];
			if (crittersActor.gameObject.activeSelf)
			{
				if (persistentActors.Contains(allActors[i]))
				{
					allActors[i].Initialize();
				}
				else
				{
					crittersActor.gameObject.SetActive(value: false);
				}
			}
		}
		for (int j = 0; j < actorSpawners.Count; j++)
		{
			actorSpawners[j].DoReset();
		}
	}

	public void Tick()
	{
		HandleZonesAndOwnership();
		if (localInZone)
		{
			ProcessSpawning();
			ProcessActorBinLocations();
			ProcessRigSetups();
			ProcessCritterAwareness();
			ProcessDespawningIdles();
			ProcessActors();
		}
		ProcessNewlyDisabledActors();
	}

	public void ProcessRigSetups()
	{
		if (LocalAuthority())
		{
			objList.Clear();
			for (int i = 0; i < rigActorSetups.Count; i++)
			{
				rigActorSetups[i].CheckUpdate(ref objList);
			}
			if (objList.Count > 0 && NetworkSystem.Instance.InRoom)
			{
				instance.SendRPC("RemoteUpdatePlayerCrittersActorData", RpcTarget.Others, new object[1] { objList.ToArray() });
			}
		}
	}

	private void ProcessCritterAwareness()
	{
		if (!LocalAuthority())
		{
			return;
		}
		int num = 0;
		lowPriorityPawnsToProcess.Clear();
		for (int i = 0; i < crittersPawns.Count; i++)
		{
			CrittersPawn key = crittersPawns[i];
			if (!awareOfActors.ContainsKey(key))
			{
				awareOfActors[key] = new List<CrittersActor>();
			}
			else
			{
				awareOfActors[key].Clear();
			}
			nearbyActors.Clear();
			int num2 = actorBinIndices[key];
			if (!priorityBins[num2])
			{
				if (i < lowPriorityIndex || num >= lowPriorityActorsPerFrame)
				{
					continue;
				}
				lowPriorityPawnsToProcess.Add(crittersPawns[i]);
				num++;
				lowPriorityIndex++;
				if (lowPriorityIndex >= crittersPawns.Count)
				{
					lowPriorityIndex = 0;
				}
			}
			int num3 = Mathf.FloorToInt(num2 / binXCount);
			int num4 = num2 % binXCount;
			for (int j = -1; j <= 1; j++)
			{
				for (int k = -1; k <= 1; k++)
				{
					if (num3 + j < binXCount && num3 + j >= 0 && num4 + k < binZCount && num4 + k >= 0)
					{
						nearbyActors.AddRange(actorBins[num4 + k + (num3 + j) * binXCount]);
					}
				}
			}
			for (int l = 0; l < nearbyActors.Count; l++)
			{
				if (crittersPawns[i].AwareOfActor(nearbyActors[l]))
				{
					awareOfActors[crittersPawns[i]].Add(nearbyActors[l]);
				}
			}
		}
	}

	private void ProcessSpawning()
	{
		if (!LocalAuthority())
		{
			return;
		}
		if (lastSpawnTime + spawnDelay <= (NetworkSystem.Instance.InRoom ? PhotonNetwork.Time : ((double)Time.time)))
		{
			int nextSpawnRegion = GetNextSpawnRegion();
			if (nextSpawnRegion >= 0)
			{
				SpawnCritter(nextSpawnRegion);
			}
			else
			{
				lastSpawnTime = (NetworkSystem.Instance.InRoom ? PhotonNetwork.Time : ((double)Time.time));
			}
		}
		if (spawnerIndex >= actorSpawners.Count)
		{
			spawnerIndex = 0;
		}
		if (actorSpawners.Count != 0)
		{
			actorSpawners[spawnerIndex].ProcessLocal();
			spawnerIndex++;
		}
	}

	private int GetNextSpawnRegion()
	{
		for (int i = 1; i <= _spawnRegions.Count; i++)
		{
			int num = (_currentRegionIndex + i) % _spawnRegions.Count;
			CrittersRegion crittersRegion = _spawnRegions[num];
			if (crittersRegion.CritterCount < crittersRegion.maxCritters)
			{
				_currentRegionIndex = num;
				return _currentRegionIndex;
			}
		}
		return -1;
	}

	private void ProcessActorBinLocations()
	{
		if (!LocalAuthority())
		{
			return;
		}
		for (int i = 0; i < actorBins.Length; i++)
		{
			actorBins[i].Clear();
			priorityBins[i] = false;
		}
		for (int num = crittersActors.Count - 1; num >= 0; num--)
		{
			CrittersActor crittersActor = crittersActors[num];
			if (crittersActor == null)
			{
				crittersActors.RemoveAt(num);
			}
			else
			{
				Transform obj = crittersActor.transform;
				int num2 = Mathf.Clamp(Mathf.FloorToInt((obj.position.x - binDimensionXMin) / individualBinSide), 0, binXCount - 1);
				int num3 = Mathf.Clamp(Mathf.FloorToInt((obj.position.z - binDimensionZMin) / individualBinSide), 0, binZCount - 1);
				int num4 = num2 + num3 * binXCount;
				if (actorBinIndices.ContainsKey(crittersActor))
				{
					actorBinIndices[crittersActor] = num4;
				}
				else
				{
					actorBinIndices.Add(crittersActor, num4);
				}
				actorBins[num4].Add(crittersActor);
			}
		}
		for (int j = 0; j < RoomSystem.PlayersInRoom.Count; j++)
		{
			if (VRRigCache.Instance.TryGetVrrig(RoomSystem.PlayersInRoom[j], out var playerRig))
			{
				Transform obj2 = playerRig.Rig.transform;
				float num5 = (obj2.position.x - binDimensionXMin) / individualBinSide;
				float num6 = (obj2.position.z - binDimensionZMin) / individualBinSide;
				int num7 = Mathf.FloorToInt(num5);
				int num8 = Mathf.FloorToInt(num6);
				int num9 = ((num5 % 1f > 0.5f) ? 1 : (-1));
				int num10 = ((num6 % 1f > 0.5f) ? 1 : (-1));
				if (num7 < 0 || num7 >= binXCount || num8 < 0 || num8 >= binZCount)
				{
					break;
				}
				int num11 = num7 + num8 * binXCount;
				priorityBins[num11] = true;
				num9 = Mathf.Clamp(num7 + num9, 0, binXCount - 1);
				num10 = Mathf.Clamp(num8 + num10, 0, binZCount - 1);
				priorityBins[num9 + num8 * binXCount] = true;
				priorityBins[num7 + num10 * binXCount] = true;
				priorityBins[num9 + num10 * binXCount] = true;
			}
		}
	}

	private void ProcessDespawningIdles()
	{
		for (int i = 0; i < actorTypes.Length; i++)
		{
			despawnDecayValue[actorTypes[i]] = Mathf.Lerp(despawnDecayValue[actorTypes[i]], despawnThreshold, 1f - Mathf.Exp((0f - decayRate) * (Time.realtimeSinceStartup - Time.deltaTime)));
		}
		if (!LocalAuthority() || despawnableActors.Count == 0)
		{
			return;
		}
		int num = 0;
		while (num <= lowPriorityActorsPerFrame)
		{
			despawnIndex++;
			if (despawnIndex >= despawnableActors.Count)
			{
				despawnIndex = 0;
			}
			num++;
			CrittersActor crittersActor = despawnableActors[despawnIndex];
			if (!(despawnDecayValue[crittersActor.crittersActorType] < (float)despawnThreshold) && crittersActor.ShouldDespawn())
			{
				DespawnActor(crittersActor);
			}
		}
	}

	public void DespawnActor(CrittersActor actor)
	{
		int actorId = actor.actorId;
		if (!updatesToSend.Contains(actorId))
		{
			updatesToSend.Add(actorId);
		}
		actor.gameObject.SetActive(value: false);
	}

	public void IncrementPoolCount(CrittersActor.CrittersActorType type)
	{
		if (!poolCounts.TryGetValue(type, out var _))
		{
			poolCounts[type] = 1;
		}
		else
		{
			poolCounts[type] += 1;
		}
		if (!despawnDecayValue.TryGetValue(type, out var _))
		{
			despawnDecayValue[type] = 1f;
		}
		else
		{
			despawnDecayValue[type] += 1f;
		}
	}

	public void DecrementPoolCount(CrittersActor.CrittersActorType type)
	{
		if (poolCounts.TryGetValue(type, out var value))
		{
			poolCounts[type] = Mathf.Max(0, value - 1);
		}
		else
		{
			poolCounts[type] = 0;
		}
	}

	private void ProcessActors()
	{
		if (LocalAuthority())
		{
			for (int num = crittersActors.Count - 1; num >= 0; num--)
			{
				if (crittersActors[num].crittersActorType != CrittersActor.CrittersActorType.Creature || priorityBins[actorBinIndices[crittersActors[num]]] || lowPriorityPawnsToProcess.Contains(crittersActors[num]))
				{
					int actorId = crittersActors[num].actorId;
					if (crittersActors[num].ProcessLocal() && !updatesToSend.Contains(actorId))
					{
						updatesToSend.Add(actorId);
					}
				}
			}
		}
		else
		{
			for (int i = 0; i < crittersActors.Count; i++)
			{
				crittersActors[i].ProcessRemote();
			}
		}
	}

	private void ProcessNewlyDisabledActors()
	{
		for (int i = 0; i < newlyDisabledActors.Count; i++)
		{
			CrittersActor crittersActor = newlyDisabledActors[i];
			if (instance.crittersActors.Contains(crittersActor))
			{
				instance.crittersActors.Remove(crittersActor);
			}
			if (crittersActor.despawnWhenIdle && instance.despawnableActors.Contains(crittersActor))
			{
				instance.despawnableActors.Remove(crittersActor);
			}
			instance.DecrementPoolCount(crittersActor.crittersActorType);
			crittersActor.SetTransformToDefaultParent(resetOrigin: true);
		}
		newlyDisabledActors.Clear();
	}

	public static void RegisterCritter(CrittersPawn crittersPawn)
	{
		CheckInitialize();
		if (!instance.crittersPawns.Contains(crittersPawn))
		{
			instance.crittersPawns.Add(crittersPawn);
		}
	}

	public static void RegisterRigActorSetup(CrittersRigActorSetup setup)
	{
		CheckInitialize();
		if (!instance.rigActorSetups.Contains(setup))
		{
			instance.rigActorSetups.Add(setup);
		}
		instance.rigSetupByRig.AddOrUpdate(setup.myRig, setup);
	}

	public static void DeregisterCritter(CrittersPawn crittersPawn)
	{
		CheckInitialize();
		instance.SetCritterRegion(crittersPawn, 0);
		if (instance.crittersPawns.Contains(crittersPawn))
		{
			instance.crittersPawns.Remove(crittersPawn);
		}
	}

	public static void RegisterActor(CrittersActor actor)
	{
		CheckInitialize();
		if (!instance.crittersActors.Contains(actor))
		{
			instance.crittersActors.Add(actor);
		}
		if (actor.despawnWhenIdle && !instance.despawnableActors.Contains(actor))
		{
			instance.despawnableActors.Add(actor);
		}
		if (instance.newlyDisabledActors.Contains(actor))
		{
			instance.newlyDisabledActors.Remove(actor);
		}
		instance.IncrementPoolCount(actor.crittersActorType);
	}

	public static void DeregisterActor(CrittersActor actor)
	{
		CheckInitialize();
		if (!instance.newlyDisabledActors.Contains(actor))
		{
			instance.newlyDisabledActors.Add(actor);
		}
	}

	public static void CheckInitialize()
	{
		if (!hasInstance)
		{
			InitializeCrittersManager();
		}
	}

	public static bool CritterAwareOfAny(CrittersPawn creature)
	{
		return instance.awareOfActors[creature].Count > 0;
	}

	public static bool AnyFoodNearby(CrittersPawn creature)
	{
		List<CrittersActor> list = instance.awareOfActors[creature];
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].crittersActorType == CrittersActor.CrittersActorType.Food)
			{
				return true;
			}
		}
		return false;
	}

	public static CrittersFood ClosestFood(CrittersPawn creature)
	{
		float num = float.MaxValue;
		CrittersFood result = null;
		List<CrittersActor> list = instance.awareOfActors[creature];
		for (int i = 0; i < list.Count; i++)
		{
			CrittersActor crittersActor = list[i];
			if (crittersActor.crittersActorType == CrittersActor.CrittersActorType.Food)
			{
				CrittersFood crittersFood = (CrittersFood)crittersActor;
				float sqrMagnitude = (creature.transform.position - crittersFood.food.transform.position).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					result = crittersFood;
					num = sqrMagnitude;
				}
			}
		}
		return result;
	}

	public static void PlayHaptics(AudioClip clip, float strength, bool isLeftHand)
	{
		(isLeftHand ? _leftGrabber : _rightGrabber).PlayHaptics(clip, strength);
	}

	public static void StopHaptics(bool isLeftHand)
	{
		(isLeftHand ? _leftGrabber : _rightGrabber).StopHaptics();
	}

	public CrittersPawn SpawnCritter(int regionIndex = -1)
	{
		CrittersRegion crittersRegion = ((regionIndex >= 0 && regionIndex < _spawnRegions.Count) ? _spawnRegions[regionIndex] : null);
		int randomCritterType = creatureIndex.GetRandomCritterType(crittersRegion);
		if (randomCritterType < 0)
		{
			return null;
		}
		Vector3 position = (crittersRegion ? crittersRegion.GetSpawnPoint() : _spawnRegions[0].transform.position);
		Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);
		CrittersPawn crittersPawn = SpawnCritter(randomCritterType, position, rotation);
		SetCritterRegion(crittersPawn, crittersRegion);
		lastSpawnTime = (NetworkSystem.Instance.InRoom ? PhotonNetwork.Time : ((double)Time.time));
		return crittersPawn;
	}

	public CrittersPawn SpawnCritter(int critterType, Vector3 position, Quaternion rotation)
	{
		CrittersPawn crittersPawn = (CrittersPawn)SpawnActor(CrittersActor.CrittersActorType.Creature);
		crittersPawn.SetTemplate(critterType);
		crittersPawn.currentState = CrittersPawn.CreatureState.Idle;
		crittersPawn.MoveActor(position, Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f));
		crittersPawn.SetImpulseVelocity(Vector3.zero, Vector3.zero);
		crittersPawn.SetState(CrittersPawn.CreatureState.Spawning);
		if (NetworkSystem.Instance.InRoom && LocalAuthority())
		{
			SendRPC("RemoteSpawnCreature", RpcTarget.Others, crittersPawn.actorId, crittersPawn.regionId, crittersPawn.visuals.Appearance.WriteToRPCData());
		}
		return crittersPawn;
	}

	public void DespawnCritter(CrittersPawn crittersPawn)
	{
		DeactivateActor(crittersPawn);
	}

	public void QueueDespawnAllCritters()
	{
		if (!LocalAuthority())
		{
			return;
		}
		foreach (CrittersPawn crittersPawn in crittersPawns)
		{
			crittersPawn.SetState(CrittersPawn.CreatureState.Despawning);
		}
	}

	private void SetCritterRegion(CrittersPawn critter, CrittersRegion region)
	{
		SetCritterRegion(critter, region ? region.ID : 0);
	}

	private void SetCritterRegion(CrittersPawn critter, int regionId)
	{
		if (critter.regionId != 0)
		{
			CrittersRegion.RemoveCritterFromRegion(critter);
		}
		if (regionId != 0)
		{
			CrittersRegion.AddCritterToRegion(critter, regionId);
		}
		critter.regionId = regionId;
	}

	public void DeactivateActor(CrittersActor actor)
	{
		actor.gameObject.SetActive(value: false);
	}

	private void CamCapture()
	{
		Camera component = GetComponent<Camera>();
		RenderTexture active = RenderTexture.active;
		RenderTexture.active = component.targetTexture;
		component.Render();
		Texture2D texture2D = new Texture2D(component.targetTexture.width, component.targetTexture.height);
		texture2D.ReadPixels(new Rect(0f, 0f, component.targetTexture.width, component.targetTexture.height), 0, 0);
		texture2D.Apply();
		RenderTexture.active = active;
		texture2D.EncodeToPNG();
		UnityEngine.Object.Destroy(texture2D);
	}

	private IEnumerator RemoteDataInitialization(NetPlayer player, int actorNumber)
	{
		List<object> nonPlayerActorObjList = new List<object>();
		List<object> playerActorObjList = new List<object>();
		int worldActorDataCount = 0;
		int playerActorDataCount = 0;
		for (int i = 0; i < allActors.Count; i++)
		{
			if (!NetworkSystem.Instance.InRoom || !LocalAuthority())
			{
				RemoveInitializingPlayer(actorNumber);
				yield break;
			}
			if (allActors[i].isOnPlayer)
			{
				playerActorDataCount++;
				allActors[i].AddPlayerCrittersActorDataToList(ref playerActorObjList);
			}
			if (playerActorDataCount >= actorsPerInitializationCall || (i == allActors.Count - 1 && playerActorDataCount > 0))
			{
				if (!player.InRoom || player.ActorNumber != actorNumber)
				{
					RemoveInitializingPlayer(actorNumber);
					yield break;
				}
				if (NetworkSystem.Instance.InRoom && LocalAuthority())
				{
					SendRPC("RemoteUpdatePlayerCrittersActorData", player, new object[1] { playerActorObjList.ToArray() });
				}
				playerActorObjList.Clear();
				playerActorDataCount = 0;
				yield return new WaitForSeconds(actorsInitializationCallCooldown);
			}
		}
		if (!player.InRoom || player.ActorNumber != actorNumber)
		{
			RemoveInitializingPlayer(actorNumber);
			yield break;
		}
		if (NetworkSystem.Instance.InRoom && LocalAuthority() && playerActorDataCount > 0)
		{
			SendRPC("RemoteUpdatePlayerCrittersActorData", player, new object[1] { playerActorObjList.ToArray() });
		}
		for (int i = 0; i < allActors.Count; i++)
		{
			if (!player.InRoom || player.ActorNumber != actorNumber)
			{
				RemoveInitializingPlayer(actorNumber);
				yield break;
			}
			if (!NetworkSystem.Instance.InRoom || !LocalAuthority())
			{
				RemoveInitializingPlayer(actorNumber);
				yield break;
			}
			CrittersActor crittersActor = allActors[i];
			if (!crittersActor.gameObject.activeSelf)
			{
				continue;
			}
			worldActorDataCount++;
			if (crittersActor.parentActorId == -1)
			{
				crittersActor.UpdateImpulses();
				crittersActor.UpdateImpulseVelocity();
			}
			crittersActor.AddActorDataToList(ref nonPlayerActorObjList);
			if (worldActorDataCount >= actorsPerInitializationCall)
			{
				if (!player.InRoom || player.ActorNumber != actorNumber)
				{
					RemoveInitializingPlayer(actorNumber);
					yield break;
				}
				if (!NetworkSystem.Instance.InRoom || !LocalAuthority())
				{
					RemoveInitializingPlayer(actorNumber);
					yield break;
				}
				SendRPC("RemoteUpdateCritterData", player, new object[1] { nonPlayerActorObjList.ToArray() });
				nonPlayerActorObjList.Clear();
				worldActorDataCount = 0;
				yield return new WaitForSeconds(actorsInitializationCallCooldown);
			}
		}
		if (NetworkSystem.Instance.InRoom && LocalAuthority() && worldActorDataCount > 0)
		{
			SendRPC("RemoteUpdateCritterData", player, new object[1] { nonPlayerActorObjList.ToArray() });
		}
		RemoveInitializingPlayer(actorNumber);
	}

	private IEnumerator DelayedInitialization(NetPlayer player, List<object> nonPlayerActorObjList)
	{
		yield return new WaitForSeconds(30f);
		SendRPC("RemoteUpdateCritterData", player, new object[1] { nonPlayerActorObjList.ToArray() });
	}

	public void RemoveInitializingPlayer(int actorNumber)
	{
		if (updatingPlayers.Contains(actorNumber))
		{
			updatingPlayers.Remove(actorNumber);
		}
	}

	private void JoinedRoomEvent()
	{
		if (localInZone && !LocalAuthority())
		{
			ResetRoom();
		}
		hasNewlyInitialized = false;
	}

	private void LeftRoomEvent()
	{
		guard.TransferOwnership(NetworkSystem.Instance.LocalPlayer);
		if (LocalInZone)
		{
			ResetRoom();
		}
	}

	[PunRPC]
	public void RequestDataInitialization(PhotonMessageInfo info)
	{
		if (NetworkSystem.Instance.InRoom && LocalAuthority())
		{
			if (updatingPlayers == null)
			{
				updatingPlayers = new List<int>();
			}
			if (!updatingPlayers.Contains(info.Sender.ActorNumber))
			{
				updatingPlayers.Add(info.Sender.ActorNumber);
				StartCoroutine(RemoteDataInitialization(info.Sender, info.Sender.ActorNumber));
			}
		}
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (!SenderIsOwner(info))
		{
			OwnerSentError(info);
		}
		else
		{
			if (!localInZone || !ValidateDataType<int>(stream.ReceiveNext(), out var dataAsType) || dataAsType > actorsPerInitializationCall)
			{
				return;
			}
			for (int i = 0; i < dataAsType; i++)
			{
				if (!UpdateActorByType(stream))
				{
					break;
				}
			}
		}
	}

	public bool UpdateActorByType(PhotonStream stream)
	{
		if (!ValidateDataType<int>(stream.ReceiveNext(), out var dataAsType))
		{
			return false;
		}
		if (dataAsType < 0 || dataAsType >= universalActorId)
		{
			return false;
		}
		if (!actorById.TryGetValue(dataAsType, out var value))
		{
			return false;
		}
		return value.UpdateSpecificActor(stream);
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (!ZoneManagement.IsInZone(GTZone.critters))
		{
			return;
		}
		using (GTProfiler.BeginSample("WriteDataPUNCrittersManager"))
		{
			int num = Mathf.Min(updatesToSend.Count, actorsPerInitializationCall);
			stream.SendNext(num);
			for (int i = 0; i < num; i++)
			{
				allActors[updatesToSend[i]].SendDataByCrittersActorType(stream);
			}
			updatesToSend.RemoveRange(0, num);
		}
	}

	[PunRPC]
	public void RemoteCritterActorReleased(int releasedActorID, bool keepWorldPosition, Quaternion rotation, Vector3 position, Vector3 velocity, Vector3 angularVelocity, PhotonMessageInfo info)
	{
		if (LocalAuthority() && VRRigCache.Instance.TryGetVrrig(info.Sender, out var _) && rotation.IsValid() && position.IsValid(10000f) && velocity.IsValid(10000f) && angularVelocity.IsValid(10000f))
		{
			CheckValidRemoteActorRelease(releasedActorID, keepWorldPosition, rotation, position, velocity, angularVelocity, info);
		}
	}

	[PunRPC]
	public void RemoteSpawnCreature(int actorID, int regionId, object[] spawnData, PhotonMessageInfo info)
	{
		RigContainer playerRig;
		CrittersActor value;
		if (!SenderIsOwner(info))
		{
			OwnerSentError(info);
		}
		else if (localInZone && VRRigCache.Instance.TryGetVrrig(info.Sender, out playerRig) && CritterAppearance.ValidateData(spawnData) && actorById.TryGetValue(actorID, out value))
		{
			CrittersPawn crittersPawn = (CrittersPawn)value;
			SetCritterRegion(crittersPawn, regionId);
			crittersPawn.SetSpawnData(spawnData);
		}
	}

	[PunRPC]
	public void RemoteCrittersActorGrabbedby(int grabbedActorID, int grabberActorID, Quaternion offsetRotation, Vector3 offsetPosition, bool isGrabDisabled, PhotonMessageInfo info)
	{
		if (LocalAuthority() && VRRigCache.Instance.TryGetVrrig(info.Sender, out var _) && offsetRotation.IsValid() && offsetPosition.IsValid(10000f))
		{
			CheckValidRemoteActorGrab(grabbedActorID, grabberActorID, offsetRotation, offsetPosition, isGrabDisabled, info);
		}
	}

	[PunRPC]
	public void RemoteUpdatePlayerCrittersActorData(object[] data, PhotonMessageInfo info)
	{
		if (!SenderIsOwner(info))
		{
			OwnerSentError(info);
		}
		else
		{
			if (!localInZone || data == null)
			{
				return;
			}
			int dataAsType;
			CrittersActor value;
			for (int i = 0; i < data.Length && ValidateDataType<int>(data[i], out dataAsType); i += value.UpdatePlayerCrittersActorFromRPC(data, i))
			{
				if (!actorById.TryGetValue(dataAsType, out value))
				{
					break;
				}
			}
		}
	}

	[PunRPC]
	public void RemoteUpdateCritterData(object[] data, PhotonMessageInfo info)
	{
		if (!SenderIsOwner(info))
		{
			OwnerSentError(info);
		}
		else
		{
			if (!VRRigCache.Instance.TryGetVrrig(info.Sender, out var _) || !localInZone || data == null)
			{
				return;
			}
			int dataAsType;
			CrittersActor value;
			for (int i = 0; i < data.Length && ValidateDataType<int>(data[i], out dataAsType); i += value.UpdateFromRPC(data, i))
			{
				if (!actorById.TryGetValue(dataAsType, out value))
				{
					break;
				}
			}
		}
	}

	public CrittersActor SpawnActor(CrittersActor.CrittersActorType type, int subObjectIndex = -1)
	{
		if (!actorPools.TryGetValue(type, out var value))
		{
			return null;
		}
		int num = poolIndexDict[type];
		for (int i = 0; i < value.Count; i++)
		{
			if (!value[(i + num) % value.Count].gameObject.activeSelf)
			{
				num = (i + num) % value.Count;
				poolIndexDict[type] = num + 1;
				value[num].subObjectIndex = subObjectIndex;
				value[num].gameObject.SetActive(value: true);
				return value[num];
			}
		}
		for (int j = 0; j < value.Count; j++)
		{
			CrittersActor key = value[j];
			int num2 = actorBinIndices[key];
			if (!priorityBins[num2])
			{
				value[j].gameObject.SetActive(value: false);
				value[j].subObjectIndex = subObjectIndex;
				value[j].gameObject.SetActive(value: true);
				return value[j];
			}
		}
		return null;
	}

	public override void WriteDataFusion()
	{
	}

	public override void ReadDataFusion()
	{
	}

	public void PopulatePools()
	{
		binDimensionXMin = crittersRange.position.x - crittersRange.localScale.x / 2f;
		binDimensionZMin = crittersRange.position.z - crittersRange.localScale.z / 2f;
		xLength = crittersRange.localScale.x;
		zLength = crittersRange.localScale.z;
		float f = xLength * zLength / (float)totalBinsApproximate;
		individualBinSide = Mathf.Sqrt(f);
		binXCount = Mathf.CeilToInt(xLength / individualBinSide);
		binZCount = Mathf.CeilToInt(zLength / individualBinSide);
		int num = binXCount * binZCount;
		actorBins = new List<CrittersActor>[num];
		for (int i = 0; i < num; i++)
		{
			actorBins[i] = new List<CrittersActor>();
		}
		priorityBins = new bool[num];
		actorBinIndices = new Dictionary<CrittersActor, int>();
		nearbyActors = new List<CrittersActor>();
		allActors = new List<CrittersActor>();
		actorPools = new Dictionary<CrittersActor.CrittersActorType, List<CrittersActor>>();
		actorPools.Add(CrittersActor.CrittersActorType.Bag, new List<CrittersActor>());
		actorPools.Add(CrittersActor.CrittersActorType.Cage, new List<CrittersActor>());
		actorPools.Add(CrittersActor.CrittersActorType.Food, new List<CrittersActor>());
		actorPools.Add(CrittersActor.CrittersActorType.Creature, new List<CrittersActor>());
		actorPools.Add(CrittersActor.CrittersActorType.LoudNoise, new List<CrittersActor>());
		actorPools.Add(CrittersActor.CrittersActorType.Grabber, new List<CrittersActor>());
		actorPools.Add(CrittersActor.CrittersActorType.FoodSpawner, new List<CrittersActor>());
		actorPools.Add(CrittersActor.CrittersActorType.AttachPoint, new List<CrittersActor>());
		actorPools.Add(CrittersActor.CrittersActorType.StunBomb, new List<CrittersActor>());
		actorPools.Add(CrittersActor.CrittersActorType.BodyAttachPoint, new List<CrittersActor>());
		actorPools.Add(CrittersActor.CrittersActorType.NoiseMaker, new List<CrittersActor>());
		actorPools.Add(CrittersActor.CrittersActorType.StickyTrap, new List<CrittersActor>());
		actorPools.Add(CrittersActor.CrittersActorType.StickyGoo, new List<CrittersActor>());
		actorById = new Dictionary<int, CrittersActor>();
		universalActorId = 0;
		GameObject gameObject = new GameObject();
		gameObject.transform.parent = base.transform;
		poolParent = gameObject.transform;
		poolParent.name = "Critter Actors Pool Parent";
		actorPools.TryGetValue(CrittersActor.CrittersActorType.Food, out var _);
		persistentActors = UnityEngine.Object.FindObjectsByType<CrittersActor>(FindObjectsSortMode.InstanceID).ToList();
		persistentActors.Sort((CrittersActor x, CrittersActor y) => x.transform.position.magnitude.CompareTo(y.transform.position.magnitude));
		persistentActors.Sort((CrittersActor x, CrittersActor y) => x.gameObject.name.CompareTo(y.gameObject.name));
		UpdatePool(ref actorPools, bagPrefab, CrittersActor.CrittersActorType.Bag, gameObject.transform, 80, persistentActors);
		UpdatePool(ref actorPools, cagePrefab, CrittersActor.CrittersActorType.Cage, gameObject.transform, poolCount, persistentActors);
		UpdatePool(ref actorPools, foodPrefab, CrittersActor.CrittersActorType.Food, gameObject.transform, poolCount, persistentActors);
		UpdatePool(ref actorPools, creaturePrefab, CrittersActor.CrittersActorType.Creature, gameObject.transform, poolCount, persistentActors);
		UpdatePool(ref actorPools, noisePrefab, CrittersActor.CrittersActorType.LoudNoise, gameObject.transform, poolCount, persistentActors);
		UpdatePool(ref actorPools, grabberPrefab, CrittersActor.CrittersActorType.Grabber, gameObject.transform, poolCount, persistentActors);
		UpdatePool(ref actorPools, foodSpawnerPrefab, CrittersActor.CrittersActorType.FoodSpawner, gameObject.transform, poolCount, persistentActors);
		UpdatePool(ref actorPools, bodyAttachPointPrefab, CrittersActor.CrittersActorType.BodyAttachPoint, gameObject.transform, 40, persistentActors);
		UpdatePool(ref actorPools, null, CrittersActor.CrittersActorType.AttachPoint, gameObject.transform, 0, persistentActors);
		UpdatePool(ref actorPools, stunBombPrefab, CrittersActor.CrittersActorType.StunBomb, gameObject.transform, poolCount, persistentActors);
		UpdatePool(ref actorPools, noiseMakerPrefab, CrittersActor.CrittersActorType.NoiseMaker, gameObject.transform, poolCount, persistentActors);
		UpdatePool(ref actorPools, stickyTrapPrefab, CrittersActor.CrittersActorType.StickyTrap, gameObject.transform, poolCount, persistentActors);
		UpdatePool(ref actorPools, stickyGooPrefab, CrittersActor.CrittersActorType.StickyGoo, gameObject.transform, poolCount, persistentActors);
	}

	public void UpdatePool<T>(ref Dictionary<CrittersActor.CrittersActorType, List<T>> dict, GameObject prefab, CrittersActor.CrittersActorType crittersActorType, Transform parent, int poolAmount, List<CrittersActor> sceneActors) where T : CrittersActor
	{
		int num = 0;
		for (int i = 0; i < sceneActors.Count; i++)
		{
			if (sceneActors[i].crittersActorType != crittersActorType)
			{
				continue;
			}
			dict[crittersActorType].Add((T)sceneActors[i]);
			sceneActors[i].actorId = universalActorId;
			actorById.Add(universalActorId, sceneActors[i]);
			allActors.Add(sceneActors[i]);
			universalActorId++;
			num++;
			if (sceneActors[i].enabled)
			{
				if (crittersActorType == CrittersActor.CrittersActorType.Creature)
				{
					RegisterCritter(sceneActors[i] as CrittersPawn);
				}
				else
				{
					RegisterActor(sceneActors[i]);
				}
			}
		}
		for (int j = 0; j < poolAmount - num; j++)
		{
			GameObject obj = UnityEngine.Object.Instantiate(prefab);
			obj.transform.parent = parent;
			obj.name += j;
			obj.SetActive(value: false);
			T component = obj.GetComponent<T>();
			dict[crittersActorType].Add(component);
			component.actorId = universalActorId;
			component.SetDefaultParent(parent);
			actorById.Add(universalActorId, component);
			allActors.Add(component);
			universalActorId++;
		}
		poolIndexDict[crittersActorType] = 0;
	}

	public void TriggerEvent(CritterEvent eventType, int sourceActor, Vector3 position, Quaternion rotation)
	{
		this.OnCritterEventReceived?.Invoke(eventType, sourceActor, position, rotation);
		if (LocalAuthority() && NetworkSystem.Instance.InRoom)
		{
			SendRPC("RemoteReceivedCritterEvent", RpcTarget.Others, eventType, sourceActor, position, rotation);
		}
	}

	public void TriggerEvent(CritterEvent eventType, int sourceActor, Vector3 position)
	{
		TriggerEvent(eventType, sourceActor, position, Quaternion.identity);
	}

	[PunRPC]
	public void RemoteReceivedCritterEvent(CritterEvent eventType, int sourceActor, Vector3 position, Quaternion rotation, PhotonMessageInfo info)
	{
		if (localInZone)
		{
			RigContainer playerRig;
			if (!SenderIsOwner(info))
			{
				OwnerSentError(info);
			}
			else if (VRRigCache.Instance.TryGetVrrig(info.Sender, out playerRig) && position.IsValid(10000f) && rotation.IsValid() && critterEventCallLimit.CheckCallTime(Time.time))
			{
				this.OnCritterEventReceived?.Invoke(eventType, sourceActor, position, rotation);
			}
		}
	}

	public static bool ValidateDataType<T>(object obj, out T dataAsType)
	{
		if (obj is T)
		{
			dataAsType = (T)obj;
			return true;
		}
		dataAsType = default(T);
		return false;
	}

	public void CheckValidRemoteActorRelease(int releasedActorID, bool keepWorldPosition, Quaternion rotation, Vector3 position, Vector3 velocity, Vector3 angularVelocity, PhotonMessageInfo info)
	{
		if (actorById.TryGetValue(releasedActorID, out var value))
		{
			CrittersActor crittersActor = TopLevelCritterGrabber(value);
			rotation.SetValueSafe(in rotation);
			position.SetValueSafe(in position);
			velocity.SetValueSafe(in velocity);
			angularVelocity.SetValueSafe(in angularVelocity);
			if (crittersActor != null && crittersActor is CrittersGrabber && crittersActor.isOnPlayer && crittersActor.rigPlayerId == info.Sender.ActorNumber)
			{
				value.Released(keepWorldPosition, rotation, position, velocity, angularVelocity);
			}
		}
	}

	private void CheckValidRemoteActorGrab(int actorBeingGrabbedActorID, int grabbingActorID, Quaternion offsetRotation, Vector3 offsetPosition, bool isGrabDisabled, PhotonMessageInfo info)
	{
		if (actorById.TryGetValue(actorBeingGrabbedActorID, out var value) && actorById.TryGetValue(grabbingActorID, out var value2))
		{
			offsetRotation.SetValueSafe(in offsetRotation);
			offsetPosition.SetValueSafe(in offsetPosition);
			if (!((value.transform.position - value2.transform.position).magnitude > maxGrabDistance) && !(offsetPosition.magnitude > maxGrabDistance) && ((value2.crittersActorType == CrittersActor.CrittersActorType.Grabber && value2.isOnPlayer && value2.rigPlayerId == info.Sender.ActorNumber) || value2.crittersActorType != CrittersActor.CrittersActorType.Grabber) && value.AllowGrabbingActor(value2))
			{
				value.GrabbedBy(value2, positionOverride: true, offsetRotation, offsetPosition, isGrabDisabled);
			}
		}
	}

	private CrittersActor TopLevelCritterGrabber(CrittersActor baseActor)
	{
		CrittersActor value = null;
		actorById.TryGetValue(baseActor.parentActorId, out value);
		while (value != null && value.parentActorId != -1)
		{
			actorById.TryGetValue(value.parentActorId, out value);
		}
		return value;
	}

	public static CapsuleCollider DuplicateCapsuleCollider(Transform targetTransform, CapsuleCollider sourceCollider)
	{
		if (sourceCollider == null)
		{
			return null;
		}
		CapsuleCollider capsuleCollider = new GameObject().AddComponent<CapsuleCollider>();
		capsuleCollider.transform.rotation = sourceCollider.transform.rotation;
		capsuleCollider.transform.position = sourceCollider.transform.position;
		capsuleCollider.transform.localScale = sourceCollider.transform.lossyScale;
		capsuleCollider.radius = sourceCollider.radius;
		capsuleCollider.height = sourceCollider.height;
		capsuleCollider.center = sourceCollider.center;
		capsuleCollider.gameObject.layer = targetTransform.gameObject.layer;
		capsuleCollider.transform.SetParent(targetTransform.transform);
		return capsuleCollider;
	}

	private void HandleZonesAndOwnership()
	{
		bool flag = localInZone;
		localInZone = ZoneManagement.IsInZone(GTZone.critters);
		CheckOwnership();
		if (!LocalAuthority() && localInZone && NetworkSystem.Instance.InRoom && guard.actualOwner != null && (!hasNewlyInitialized || !flag) && Time.time > lastRequest + initRequestCooldown)
		{
			lastRequest = Time.time;
			hasNewlyInitialized = true;
			SendRPC("RequestDataInitialization", guard.actualOwner);
		}
		if (flag && !localInZone)
		{
			ResetRoom();
			poolParent.gameObject.SetActive(value: false);
			crittersPool.poolParent.gameObject.SetActive(value: false);
		}
		if (!flag && localInZone)
		{
			poolParent.gameObject.SetActive(value: true);
			crittersPool.poolParent.gameObject.SetActive(value: true);
		}
	}

	private void CheckOwnership()
	{
		if (!PhotonNetwork.InRoom && base.IsMine)
		{
			if (guard.actualOwner == null || !guard.actualOwner.Equals(NetworkSystem.Instance.LocalPlayer))
			{
				guard.SetOwnership(NetworkSystem.Instance.LocalPlayer);
			}
		}
		else
		{
			if (allRigs == null && !VRRigCache.isInitialized)
			{
				return;
			}
			if (allRigs == null)
			{
				allRigs = new List<VRRig>(VRRigCache.Instance.GetAllRigs());
			}
			if (!LocalAuthority() || localInZone)
			{
				return;
			}
			int num = int.MaxValue;
			NetPlayer netPlayer = null;
			for (int i = 0; i < allRigs.Count; i++)
			{
				NetPlayer creator = allRigs[i].creator;
				if (creator != null && allRigs[i].zoneEntity.currentZone == GTZone.critters && creator.ActorNumber < num)
				{
					netPlayer = creator;
					num = creator.ActorNumber;
				}
			}
			if (netPlayer != null)
			{
				guard.TransferOwnership(netPlayer);
			}
		}
	}

	public bool LocalAuthority()
	{
		if (!NetworkSystem.Instance.InRoom)
		{
			return true;
		}
		if (guard == null)
		{
			return false;
		}
		if ((guard.actualOwner == null || !guard.isTrulyMine) && (base.Owner == null || !base.Owner.IsLocal))
		{
			return guard.currentState == NetworkingState.IsOwner;
		}
		return true;
	}

	private bool SenderIsOwner(PhotonMessageInfo info)
	{
		if ((guard.actualOwner == null && base.Owner == null) || info.Sender == null)
		{
			return false;
		}
		if (!LocalAuthority())
		{
			if (guard.actualOwner == null || guard.actualOwner.ActorNumber != info.Sender.ActorNumber)
			{
				if (base.Owner != null)
				{
					return base.Owner.ActorNumber == info.Sender.ActorNumber;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	private void OwnerSentError(PhotonMessageInfo info)
	{
		_ = base.Owner;
	}

	public void OnOwnershipTransferred(NetPlayer toPlayer, NetPlayer fromPlayer)
	{
		_ = NetworkSystem.Instance.LocalPlayer;
	}

	public bool OnOwnershipRequest(NetPlayer fromPlayer)
	{
		return false;
	}

	public void OnMyOwnerLeft()
	{
	}

	public bool OnMasterClientAssistedTakeoverRequest(NetPlayer fromPlayer, NetPlayer toPlayer)
	{
		return false;
	}

	public void OnMyCreatorLeft()
	{
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

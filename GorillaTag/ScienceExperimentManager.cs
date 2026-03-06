using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CjLib;
using Fusion;
using Fusion.CodeGen;
using GorillaExtensions;
using GorillaGameModes;
using GorillaLocomotion;
using GorillaLocomotion.Swimming;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Scripting;

namespace GorillaTag;

[NetworkBehaviourWeaved(76)]
public class ScienceExperimentManager : NetworkComponent, ITickSystemTick
{
	public enum RisingLiquidState
	{
		Drained,
		Erupting,
		Rising,
		Full,
		PreDrainDelay,
		Draining
	}

	private enum RiseSpeed
	{
		Fast,
		Medium,
		Slow,
		ExtraSlow
	}

	private enum TagBehavior
	{
		None,
		Infect,
		Revive
	}

	[Serializable]
	public struct PlayerGameState
	{
		public int playerId;

		public bool touchedLiquid;

		public float touchedLiquidAtProgress;
	}

	private struct SyncData
	{
		public RisingLiquidState state;

		public double stateStartTime;

		public float stateStartLiquidProgressLinear;

		public double activationProgress;
	}

	private struct RotatingRingState
	{
		public Transform ringTransform;

		public float initialAngle;

		public float resultingAngle;
	}

	[Serializable]
	private struct DisableByLiquidData
	{
		public Transform target;

		public float heightOffset;
	}

	[StructLayout(LayoutKind.Explicit, Size = 304)]
	[NetworkStructWeaved(76)]
	private struct ScienceManagerData : INetworkStruct
	{
		[FieldOffset(0)]
		public int reliableState;

		[FieldOffset(4)]
		public double stateStartTime;

		[FieldOffset(12)]
		public float stateStartLiquidProgressLinear;

		[FieldOffset(16)]
		public double activationProgress;

		[FieldOffset(24)]
		public int nextRoundRiseSpeed;

		[FieldOffset(28)]
		public float riseTime;

		[FieldOffset(32)]
		public int lastWinnerId;

		[FieldOffset(36)]
		public int inGamePlayerCount;

		[FieldOffset(40)]
		[FixedBufferProperty(typeof(NetworkArray<int>), typeof(UnityArraySurrogate_0040ElementReaderWriterInt32), 10, order = -2147483647)]
		[WeaverGenerated]
		[SerializeField]
		private FixedStorage_004010 _playerIdArray;

		[FieldOffset(80)]
		[FixedBufferProperty(typeof(NetworkArray<bool>), typeof(UnityArraySurrogate_0040ElementReaderWriterBoolean), 10, order = -2147483647)]
		[WeaverGenerated]
		[SerializeField]
		private FixedStorage_004010 _touchedLiquidArray;

		[FieldOffset(120)]
		[FixedBufferProperty(typeof(NetworkArray<float>), typeof(UnityArraySurrogate_0040ElementReaderWriterSingle), 10, order = -2147483647)]
		[WeaverGenerated]
		[SerializeField]
		private FixedStorage_004010 _touchedLiquidAtProgressArray;

		[FieldOffset(160)]
		[FixedBufferProperty(typeof(NetworkLinkedList<float>), typeof(UnityLinkedListSurrogate_0040ElementReaderWriterSingle), 5, order = -2147483647)]
		[WeaverGenerated]
		[SerializeField]
		private FixedStorage_004018 _initialAngleArray;

		[FieldOffset(232)]
		[FixedBufferProperty(typeof(NetworkLinkedList<float>), typeof(UnityLinkedListSurrogate_0040ElementReaderWriterSingle), 5, order = -2147483647)]
		[WeaverGenerated]
		[SerializeField]
		private FixedStorage_004018 _resultingAngleArray;

		[Networked]
		[Capacity(10)]
		[NetworkedWeavedArray(10, 1, typeof(Fusion.ElementReaderWriterInt32))]
		[NetworkedWeaved(10, 10)]
		public unsafe NetworkArray<int> playerIdArray => new NetworkArray<int>(Native.ReferenceToPointer(ref _playerIdArray), 10, Fusion.ElementReaderWriterInt32.GetInstance());

		[Networked]
		[Capacity(10)]
		[NetworkedWeavedArray(10, 1, typeof(global::ElementReaderWriterBoolean))]
		[NetworkedWeaved(20, 10)]
		public unsafe NetworkArray<bool> touchedLiquidArray => new NetworkArray<bool>(Native.ReferenceToPointer(ref _touchedLiquidArray), 10, global::ElementReaderWriterBoolean.GetInstance());

		[Networked]
		[Capacity(10)]
		[NetworkedWeavedArray(10, 1, typeof(Fusion.ElementReaderWriterSingle))]
		[NetworkedWeaved(30, 10)]
		public unsafe NetworkArray<float> touchedLiquidAtProgressArray => new NetworkArray<float>(Native.ReferenceToPointer(ref _touchedLiquidAtProgressArray), 10, Fusion.ElementReaderWriterSingle.GetInstance());

		[Networked]
		[Capacity(5)]
		[NetworkedWeavedLinkedList(5, 1, typeof(Fusion.ElementReaderWriterSingle))]
		[NetworkedWeaved(40, 18)]
		public unsafe NetworkLinkedList<float> initialAngleArray => new NetworkLinkedList<float>(Native.ReferenceToPointer(ref _initialAngleArray), 5, Fusion.ElementReaderWriterSingle.GetInstance());

		[Networked]
		[Capacity(5)]
		[NetworkedWeavedLinkedList(5, 1, typeof(Fusion.ElementReaderWriterSingle))]
		[NetworkedWeaved(58, 18)]
		public unsafe NetworkLinkedList<float> resultingAngleArray => new NetworkLinkedList<float>(Native.ReferenceToPointer(ref _resultingAngleArray), 5, Fusion.ElementReaderWriterSingle.GetInstance());

		public ScienceManagerData(int reliableState, double stateStartTime, float stateStartLiquidProgressLinear, double activationProgress, int nextRoundRiseSpeed, float riseTime, int lastWinnerId, int inGamePlayerCount, PlayerGameState[] playerStates, RotatingRingState[] rings)
		{
			this.reliableState = reliableState;
			this.stateStartTime = stateStartTime;
			this.stateStartLiquidProgressLinear = stateStartLiquidProgressLinear;
			this.activationProgress = activationProgress;
			this.nextRoundRiseSpeed = nextRoundRiseSpeed;
			this.riseTime = riseTime;
			this.lastWinnerId = lastWinnerId;
			this.inGamePlayerCount = inGamePlayerCount;
			for (int i = 0; i < rings.Length; i++)
			{
				RotatingRingState rotatingRingState = rings[i];
				initialAngleArray.Add(rotatingRingState.initialAngle);
				resultingAngleArray.Add(rotatingRingState.resultingAngle);
			}
			int[] array = new int[10];
			bool[] array2 = new bool[10];
			float[] array3 = new float[10];
			for (int j = 0; j < 10; j++)
			{
				array[j] = playerStates[j].playerId;
				array2[j] = playerStates[j].touchedLiquid;
				array3[j] = playerStates[j].touchedLiquidAtProgress;
			}
			playerIdArray.CopyFrom(array, 0, array.Length);
			touchedLiquidArray.CopyFrom(array2, 0, array2.Length);
			touchedLiquidAtProgressArray.CopyFrom(array3, 0, array3.Length);
		}
	}

	public static volatile ScienceExperimentManager instance;

	[SerializeField]
	private TagBehavior tagBehavior = TagBehavior.Infect;

	[SerializeField]
	private float minScale = 1f;

	[SerializeField]
	private float maxScale = 10f;

	[SerializeField]
	private float riseTimeFast = 30f;

	[SerializeField]
	private float riseTimeMedium = 60f;

	[SerializeField]
	private float riseTimeSlow = 120f;

	[SerializeField]
	private float riseTimeExtraSlow = 240f;

	[SerializeField]
	private float preDrainWaitTime = 3f;

	[SerializeField]
	private float maxFullTime = 5f;

	[SerializeField]
	private float drainTime = 10f;

	[SerializeField]
	private float fullyDrainedWaitTime = 3f;

	[SerializeField]
	private float lagResolutionLavaProgressPerSecond = 0.2f;

	[SerializeField]
	private AnimationCurve animationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[SerializeField]
	private float lavaProgressToDisableRefreshWater = 0.18f;

	[SerializeField]
	private float lavaProgressToEnableRefreshWater = 0.08f;

	[SerializeField]
	private float entryLiquidMaxScale = 5f;

	[SerializeField]
	private Vector2 entryLiquidScaleSyncOpeningTop = Vector2.zero;

	[SerializeField]
	private Vector2 entryLiquidScaleSyncOpeningBottom = Vector2.zero;

	[SerializeField]
	private float entryBridgeQuadMaxScaleY = 0.0915f;

	[SerializeField]
	private Vector2 entryBridgeQuadMinMaxZHeight = new Vector2(0.245f, 0.337f);

	[SerializeField]
	private AnimationCurve lavaActivationRockProgressVsPlayerCount = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[SerializeField]
	private AnimationCurve lavaActivationDrainRateVsPlayerCount = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[SerializeField]
	public GameObject waterBalloonPrefab;

	[SerializeField]
	private Vector2 rotatingRingRandomAngleRange = Vector2.zero;

	[SerializeField]
	private bool rotatingRingQuantizeAngles;

	[SerializeField]
	private float rotatingRingAngleSnapDegrees = 9f;

	[SerializeField]
	private float drainBlockerSlideTime = 4f;

	[SerializeField]
	private Vector2 sodaFizzParticleEmissionMinMax = new Vector2(30f, 100f);

	[SerializeField]
	private float infrequentUpdatePeriod = 3f;

	[SerializeField]
	private bool optPlayersOutOfRoomGameMode;

	[SerializeField]
	private bool debugDrawPlayerGameState;

	private ScienceExperimentSceneElements elements;

	private NetPlayer[] allPlayersInRoom;

	private RotatingRingState[] rotatingRings = new RotatingRingState[0];

	private const int maxPlayerCount = 10;

	private PlayerGameState[] inGamePlayerStates = new PlayerGameState[10];

	private int inGamePlayerCount;

	private int lastWinnerId = -1;

	private string lastWinnerName = "None";

	private List<PlayerGameState> sortedPlayerStates = new List<PlayerGameState>();

	private SyncData reliableState;

	private RiseSpeed nextRoundRiseSpeed = RiseSpeed.Slow;

	private float riseTime = 120f;

	private float riseProgress;

	private float riseProgressLinear;

	private float localLagRiseProgressOffset;

	private double lastInfrequentUpdateTime = -10.0;

	private string mentoProjectileTag = "ScienceCandyProjectile";

	private double currentTime;

	private double prevTime;

	private float ringRotationProgress = 1f;

	private float drainBlockerSlideSpeed;

	private float[] riseTimeLookup;

	[Header("Scene References")]
	public Transform ringParent;

	public Transform liquidMeshTransform;

	public Transform liquidSurfacePlane;

	public Transform entryWayLiquidMeshTransform;

	public Transform entryWayBridgeQuadTransform;

	public Transform drainBlocker;

	public Transform drainBlockerClosedPosition;

	public Transform drainBlockerOpenPosition;

	public WaterVolume liquidVolume;

	public WaterVolume entryLiquidVolume;

	public WaterVolume bottleLiquidVolume;

	public WaterVolume refreshWaterVolume;

	public CompositeTriggerEvents gameAreaTriggerNotifier;

	public SlingshotProjectileHitNotifier sodaWaterProjectileTriggerNotifier;

	public AudioSource eruptionAudioSource;

	public AudioSource drainAudioSource;

	public AudioSource rotatingRingsAudioSource;

	private ParticleSystem.EmissionModule fizzParticleEmission;

	private bool hasPlayedEruptionEffects;

	private bool hasPlayedDrainEffects;

	[SerializeField]
	private float debugRotateRingsTime = 10f;

	private Coroutine rotateRingsCoroutine;

	private bool debugRandomizingRings;

	[WeaverGenerated]
	[DefaultForProperty("Data", 0, 76)]
	[DrawIf("IsEditorWritable", true, CompareOperator.Equal, DrawIfMode.ReadOnly)]
	private ScienceManagerData _Data;

	private bool RefreshWaterAvailable
	{
		get
		{
			if (reliableState.state != RisingLiquidState.Drained && reliableState.state != RisingLiquidState.Erupting && (reliableState.state != RisingLiquidState.Rising || !(riseProgress < lavaProgressToDisableRefreshWater)))
			{
				if (reliableState.state == RisingLiquidState.Draining)
				{
					return riseProgress < lavaProgressToEnableRefreshWater;
				}
				return false;
			}
			return true;
		}
	}

	public RisingLiquidState GameState => reliableState.state;

	public float RiseProgress => riseProgress;

	public float RiseProgressLinear => riseProgressLinear;

	private int PlayerCount
	{
		get
		{
			int result = 1;
			GorillaGameManager gorillaGameManager = GorillaGameManager.instance;
			if (gorillaGameManager != null && gorillaGameManager.currentNetPlayerArray != null)
			{
				result = gorillaGameManager.currentNetPlayerArray.Length;
			}
			return result;
		}
	}

	bool ITickSystemTick.TickRunning { get; set; }

	[Networked]
	[NetworkedWeaved(0, 76)]
	private unsafe ScienceManagerData Data
	{
		get
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing ScienceExperimentManager.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			return *(ScienceManagerData*)((byte*)((NetworkBehaviour)this).Ptr + 0);
		}
		set
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing ScienceExperimentManager.Data. Networked properties can only be accessed when Spawned() has been called.");
			}
			*(ScienceManagerData*)((byte*)((NetworkBehaviour)this).Ptr + 0) = value;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		if (instance == null)
		{
			instance = this;
			NetworkSystem.Instance.RegisterSceneNetworkItem(base.gameObject);
			riseTimeLookup = new float[4] { riseTimeFast, riseTimeMedium, riseTimeSlow, riseTimeExtraSlow };
			riseTime = riseTimeLookup[(int)nextRoundRiseSpeed];
			allPlayersInRoom = RoomSystem.PlayersInRoom.ToArray();
			GorillaGameManager.OnTouch += OnPlayerTagged;
			RoomSystem.PlayerLeftEvent += new Action<NetPlayer>(OnPlayerLeftRoom);
			RoomSystem.LeftRoomEvent += new Action(OnLeftRoom);
			rotatingRings = new RotatingRingState[ringParent.childCount];
			for (int i = 0; i < rotatingRings.Length; i++)
			{
				rotatingRings[i].ringTransform = ringParent.GetChild(i);
				rotatingRings[i].initialAngle = 0f;
				rotatingRings[i].resultingAngle = 0f;
			}
			gameAreaTriggerNotifier.CompositeTriggerEnter += OnColliderEnteredVolume;
			gameAreaTriggerNotifier.CompositeTriggerExit += OnColliderExitedVolume;
			liquidVolume.ColliderEnteredWater += OnColliderEnteredSoda;
			liquidVolume.ColliderExitedWater += OnColliderExitedSoda;
			entryLiquidVolume.ColliderEnteredWater += OnColliderEnteredSoda;
			entryLiquidVolume.ColliderExitedWater += OnColliderExitedSoda;
			if (bottleLiquidVolume != null)
			{
				bottleLiquidVolume.ColliderEnteredWater += OnColliderEnteredSoda;
				bottleLiquidVolume.ColliderExitedWater += OnColliderExitedSoda;
			}
			if (refreshWaterVolume != null)
			{
				refreshWaterVolume.ColliderEnteredWater += OnColliderEnteredRefreshWater;
				refreshWaterVolume.ColliderExitedWater += OnColliderExitedRefreshWater;
			}
			if (sodaWaterProjectileTriggerNotifier != null)
			{
				sodaWaterProjectileTriggerNotifier.OnProjectileTriggerEnter += OnProjectileEnteredSodaWater;
			}
			float num = Vector3.Distance(drainBlockerClosedPosition.position, drainBlockerOpenPosition.position);
			drainBlockerSlideSpeed = num / drainBlockerSlideTime;
		}
		else
		{
			UnityEngine.Object.Destroy(this);
		}
	}

	internal override void OnEnable()
	{
		NetworkBehaviourUtils.InternalOnEnable(this);
		base.OnEnable();
		TickSystem<object>.AddTickCallback(this);
	}

	internal override void OnDisable()
	{
		NetworkBehaviourUtils.InternalOnDisable(this);
		base.OnDisable();
		TickSystem<object>.RemoveTickCallback(this);
	}

	private void OnDestroy()
	{
		NetworkBehaviourUtils.InternalOnDestroy(this);
		GorillaGameManager.OnTouch -= OnPlayerTagged;
		if (gameAreaTriggerNotifier != null)
		{
			gameAreaTriggerNotifier.CompositeTriggerEnter -= OnColliderEnteredVolume;
			gameAreaTriggerNotifier.CompositeTriggerExit -= OnColliderExitedVolume;
		}
		if (liquidVolume != null)
		{
			liquidVolume.ColliderEnteredWater -= OnColliderEnteredSoda;
			liquidVolume.ColliderExitedWater -= OnColliderExitedSoda;
		}
		if (entryLiquidVolume != null)
		{
			entryLiquidVolume.ColliderEnteredWater -= OnColliderEnteredSoda;
			entryLiquidVolume.ColliderExitedWater -= OnColliderExitedSoda;
		}
		if (bottleLiquidVolume != null)
		{
			bottleLiquidVolume.ColliderEnteredWater -= OnColliderEnteredSoda;
			bottleLiquidVolume.ColliderExitedWater -= OnColliderExitedSoda;
		}
		if (refreshWaterVolume != null)
		{
			refreshWaterVolume.ColliderEnteredWater -= OnColliderEnteredRefreshWater;
			refreshWaterVolume.ColliderExitedWater -= OnColliderExitedRefreshWater;
		}
		if (sodaWaterProjectileTriggerNotifier != null)
		{
			sodaWaterProjectileTriggerNotifier.OnProjectileTriggerEnter -= OnProjectileEnteredSodaWater;
		}
	}

	public void InitElements(ScienceExperimentSceneElements elements)
	{
		this.elements = elements;
		fizzParticleEmission = elements.sodaFizzParticles.emission;
		elements.sodaFizzParticles.gameObject.SetActive(value: false);
		elements.sodaEruptionParticles.gameObject.SetActive(value: false);
		RoomSystem.LeftRoomEvent += new Action(OnLeftRoom);
	}

	public void DeInitElements()
	{
		elements = null;
	}

	public Transform GetElement(ScienceExperimentElementID elementID)
	{
		switch (elementID)
		{
		case ScienceExperimentElementID.Platform1:
			return rotatingRings[0].ringTransform;
		case ScienceExperimentElementID.Platform2:
			return rotatingRings[1].ringTransform;
		case ScienceExperimentElementID.Platform3:
			return rotatingRings[2].ringTransform;
		case ScienceExperimentElementID.Platform4:
			return rotatingRings[3].ringTransform;
		case ScienceExperimentElementID.Platform5:
			return rotatingRings[4].ringTransform;
		case ScienceExperimentElementID.LiquidMesh:
			return liquidMeshTransform;
		case ScienceExperimentElementID.EntryChamberLiquidMesh:
			return entryWayLiquidMeshTransform;
		case ScienceExperimentElementID.EntryChamberBridgeQuad:
			return entryWayBridgeQuadTransform;
		case ScienceExperimentElementID.DrainBlocker:
			return drainBlocker;
		default:
			Debug.LogError($"Unhandled ScienceExperiment element ID! {elementID}");
			return null;
		}
	}

	void ITickSystemTick.Tick()
	{
		prevTime = currentTime;
		currentTime = (NetworkSystem.Instance.InRoom ? NetworkSystem.Instance.SimTime : Time.unscaledTimeAsDouble);
		lastInfrequentUpdateTime = ((lastInfrequentUpdateTime > currentTime) ? currentTime : lastInfrequentUpdateTime);
		if (currentTime > lastInfrequentUpdateTime + (double)infrequentUpdatePeriod)
		{
			InfrequentUpdate();
			lastInfrequentUpdateTime = (float)currentTime;
		}
		if (base.IsMine)
		{
			UpdateReliableState(currentTime, ref reliableState);
		}
		UpdateLocalState(currentTime, reliableState);
		localLagRiseProgressOffset = Mathf.MoveTowards(localLagRiseProgressOffset, 0f, lagResolutionLavaProgressPerSecond * Time.deltaTime);
		UpdateLiquid(riseProgress + localLagRiseProgressOffset);
		UpdateRotatingRings(ringRotationProgress);
		UpdateRefreshWater();
		UpdateDrainBlocker(currentTime);
		DisableObjectsInContactWithLava(liquidMeshTransform.localScale.z);
		UpdateEffects();
		if (!debugDrawPlayerGameState)
		{
			return;
		}
		for (int i = 0; i < inGamePlayerCount; i++)
		{
			NetPlayer netPlayer = null;
			if (NetworkSystem.Instance.InRoom)
			{
				netPlayer = NetworkSystem.Instance.GetPlayer(inGamePlayerStates[i].playerId);
			}
			else if (inGamePlayerStates[i].playerId == NetworkSystem.Instance.LocalPlayer.ActorNumber)
			{
				netPlayer = NetworkSystem.Instance.LocalPlayer;
			}
			if (netPlayer != null && VRRigCache.Instance.TryGetVrrig(netPlayer, out var playerRig) && playerRig.Rig != null)
			{
				float num = 0.03f;
				DebugUtil.DrawSphere(playerRig.Rig.transform.position + Vector3.up * 0.5f * num, 0.16f * num, 12, 12, inGamePlayerStates[i].touchedLiquid ? Color.red : Color.green, depthTest: true, DebugUtil.Style.SolidColor);
			}
		}
	}

	private void InfrequentUpdate()
	{
		allPlayersInRoom = RoomSystem.PlayersInRoom.ToArray();
		if (base.IsMine)
		{
			for (int num = inGamePlayerCount - 1; num >= 0; num--)
			{
				int playerId = inGamePlayerStates[num].playerId;
				bool flag = false;
				for (int i = 0; i < allPlayersInRoom.Length; i++)
				{
					if (allPlayersInRoom[i].ActorNumber == playerId)
					{
						flag = true;
					}
				}
				if (!flag)
				{
					if (num < inGamePlayerCount - 1)
					{
						inGamePlayerStates[num] = inGamePlayerStates[inGamePlayerCount - 1];
					}
					inGamePlayerStates[inGamePlayerCount - 1] = default(PlayerGameState);
					inGamePlayerCount--;
				}
			}
		}
		if (!optPlayersOutOfRoomGameMode)
		{
			return;
		}
		for (int j = 0; j < allPlayersInRoom.Length; j++)
		{
			bool flag2 = false;
			for (int k = 0; k < inGamePlayerCount; k++)
			{
				if (allPlayersInRoom[j].ActorNumber == inGamePlayerStates[k].playerId)
				{
					flag2 = true;
				}
			}
			if (flag2)
			{
				GorillaGameModes.GameMode.OptOut(allPlayersInRoom[j]);
			}
			else
			{
				GorillaGameModes.GameMode.OptIn(allPlayersInRoom[j]);
			}
		}
	}

	private bool PlayerInGame(Player player)
	{
		for (int i = 0; i < inGamePlayerCount; i++)
		{
			if (inGamePlayerStates[i].playerId == player.ActorNumber)
			{
				return true;
			}
		}
		return false;
	}

	private void UpdateReliableState(double currentTime, ref SyncData syncData)
	{
		if (currentTime < syncData.stateStartTime)
		{
			syncData.stateStartTime = currentTime;
		}
		switch (syncData.state)
		{
		default:
			if (GetAlivePlayerCount() > 0 && syncData.activationProgress > 1.0)
			{
				syncData.state = RisingLiquidState.Erupting;
				syncData.stateStartTime = currentTime;
				syncData.stateStartLiquidProgressLinear = 0f;
				syncData.activationProgress = 1.0;
			}
			else
			{
				float num5 = Mathf.Clamp((float)(currentTime - prevTime), 0f, 0.1f);
				syncData.activationProgress = Mathf.MoveTowards((float)syncData.activationProgress, 0f, lavaActivationDrainRateVsPlayerCount.Evaluate(PlayerCount) * num5);
			}
			break;
		case RisingLiquidState.Erupting:
			if (currentTime > syncData.stateStartTime + (double)fullyDrainedWaitTime)
			{
				riseTime = riseTimeLookup[(int)nextRoundRiseSpeed];
				syncData.stateStartLiquidProgressLinear = 0f;
				syncData.state = RisingLiquidState.Rising;
				syncData.stateStartTime = currentTime;
			}
			break;
		case RisingLiquidState.Rising:
			if (GetAlivePlayerCount() <= 0)
			{
				UpdateWinner();
				syncData.stateStartLiquidProgressLinear = Mathf.Clamp01((float)((currentTime - syncData.stateStartTime) / (double)riseTime));
				syncData.state = RisingLiquidState.PreDrainDelay;
				syncData.stateStartTime = currentTime;
			}
			else if (currentTime > syncData.stateStartTime + (double)riseTime)
			{
				syncData.stateStartLiquidProgressLinear = 1f;
				syncData.state = RisingLiquidState.Full;
				syncData.stateStartTime = currentTime;
			}
			break;
		case RisingLiquidState.Full:
			if (GetAlivePlayerCount() <= 0 || currentTime > syncData.stateStartTime + (double)maxFullTime)
			{
				UpdateWinner();
				syncData.stateStartLiquidProgressLinear = 1f;
				syncData.state = RisingLiquidState.PreDrainDelay;
				syncData.stateStartTime = currentTime;
			}
			break;
		case RisingLiquidState.PreDrainDelay:
			if (currentTime > syncData.stateStartTime + (double)preDrainWaitTime)
			{
				syncData.state = RisingLiquidState.Draining;
				syncData.stateStartTime = currentTime;
				syncData.activationProgress = 0.0;
				for (int i = 0; i < rotatingRings.Length; i++)
				{
					float num2 = Mathf.Repeat(rotatingRings[i].resultingAngle, 360f);
					float num3 = UnityEngine.Random.Range(rotatingRingRandomAngleRange.x, rotatingRingRandomAngleRange.y);
					float num4 = ((UnityEngine.Random.Range(0f, 1f) > 0.5f) ? 1f : (-1f));
					rotatingRings[i].initialAngle = num2;
					rotatingRings[i].resultingAngle = num2 + num4 * num3;
				}
			}
			break;
		case RisingLiquidState.Draining:
		{
			double num = (1.0 - (double)syncData.stateStartLiquidProgressLinear) * (double)drainTime;
			if (currentTime + num > syncData.stateStartTime + (double)drainTime)
			{
				syncData.stateStartLiquidProgressLinear = 0f;
				syncData.state = RisingLiquidState.Drained;
				syncData.stateStartTime = currentTime;
				syncData.activationProgress = 0.0;
			}
			break;
		}
		}
		int GetAlivePlayerCount()
		{
			int num6 = 0;
			for (int j = 0; j < inGamePlayerCount; j++)
			{
				if (!inGamePlayerStates[j].touchedLiquid)
				{
					num6++;
				}
			}
			return num6;
		}
	}

	private void UpdateLocalState(double currentTime, SyncData syncData)
	{
		switch (syncData.state)
		{
		default:
			riseProgressLinear = 0f;
			riseProgress = 0f;
			if (!debugRandomizingRings)
			{
				ringRotationProgress = 1f;
			}
			break;
		case RisingLiquidState.Rising:
		{
			double num3 = (currentTime - syncData.stateStartTime) / (double)riseTime;
			riseProgressLinear = Mathf.Clamp01((float)num3);
			riseProgress = animationCurve.Evaluate(riseProgressLinear);
			ringRotationProgress = 1f;
			break;
		}
		case RisingLiquidState.Full:
			riseProgressLinear = 1f;
			riseProgress = 1f;
			ringRotationProgress = 1f;
			break;
		case RisingLiquidState.PreDrainDelay:
			riseProgressLinear = syncData.stateStartLiquidProgressLinear;
			riseProgress = animationCurve.Evaluate(riseProgressLinear);
			ringRotationProgress = 1f;
			break;
		case RisingLiquidState.Draining:
		{
			double num = (1.0 - (double)syncData.stateStartLiquidProgressLinear) * (double)drainTime;
			double num2 = (currentTime + num - syncData.stateStartTime) / (double)drainTime;
			riseProgressLinear = Mathf.Clamp01((float)(1.0 - num2));
			riseProgress = animationCurve.Evaluate(riseProgressLinear);
			ringRotationProgress = (float)(currentTime - syncData.stateStartTime) / (drainTime * syncData.stateStartLiquidProgressLinear);
			break;
		}
		}
	}

	private void UpdateLiquid(float fillProgress)
	{
		float num = Mathf.Lerp(minScale, maxScale, fillProgress);
		liquidMeshTransform.localScale = new Vector3(1f, 1f, num);
		bool active = reliableState.state == RisingLiquidState.Rising || reliableState.state == RisingLiquidState.Full || reliableState.state == RisingLiquidState.PreDrainDelay || reliableState.state == RisingLiquidState.Draining;
		liquidMeshTransform.gameObject.SetActive(active);
		if (entryWayLiquidMeshTransform != null)
		{
			float y = 0f;
			float z;
			float z2;
			if (num < entryLiquidScaleSyncOpeningBottom.y)
			{
				z = entryLiquidScaleSyncOpeningBottom.x;
				z2 = entryBridgeQuadMinMaxZHeight.x;
			}
			else if (num < entryLiquidScaleSyncOpeningTop.y)
			{
				float num2 = Mathf.InverseLerp(entryLiquidScaleSyncOpeningBottom.y, entryLiquidScaleSyncOpeningTop.y, num);
				z = Mathf.Lerp(entryLiquidScaleSyncOpeningBottom.x, entryLiquidScaleSyncOpeningTop.x, num2);
				z2 = Mathf.Lerp(entryBridgeQuadMinMaxZHeight.x, entryBridgeQuadMinMaxZHeight.y, num2);
				y = entryBridgeQuadMaxScaleY * Mathf.Sin(num2 * MathF.PI);
			}
			else
			{
				float t = Mathf.InverseLerp(entryLiquidScaleSyncOpeningTop.y, 0.6f * maxScale, num);
				z = Mathf.Lerp(entryLiquidScaleSyncOpeningTop.x, entryLiquidMaxScale, t);
				z2 = entryBridgeQuadMinMaxZHeight.y;
			}
			entryWayLiquidMeshTransform.localScale = new Vector3(entryWayLiquidMeshTransform.localScale.x, entryWayLiquidMeshTransform.localScale.y, z);
			entryWayBridgeQuadTransform.localScale = new Vector3(entryWayBridgeQuadTransform.localScale.x, y, entryWayBridgeQuadTransform.localScale.z);
			entryWayBridgeQuadTransform.localPosition = new Vector3(entryWayBridgeQuadTransform.localPosition.x, entryWayBridgeQuadTransform.localPosition.y, z2);
		}
	}

	private void UpdateRotatingRings(float rotationProgress)
	{
		for (int i = 0; i < rotatingRings.Length; i++)
		{
			float angle = Mathf.Lerp(rotatingRings[i].initialAngle, rotatingRings[i].resultingAngle, rotationProgress);
			rotatingRings[i].ringTransform.rotation = Quaternion.AngleAxis(angle, Vector3.up);
		}
	}

	private void UpdateDrainBlocker(double currentTime)
	{
		if (reliableState.state == RisingLiquidState.Draining)
		{
			float num = (float)(currentTime - reliableState.stateStartTime);
			float num2 = (1f - reliableState.stateStartLiquidProgressLinear) * drainTime;
			if (drainTime - (num + num2) < drainBlockerSlideTime)
			{
				drainBlocker.position = Vector3.MoveTowards(drainBlocker.position, drainBlockerClosedPosition.position, drainBlockerSlideSpeed * Time.deltaTime);
			}
			else
			{
				drainBlocker.position = Vector3.MoveTowards(drainBlocker.position, drainBlockerOpenPosition.position, drainBlockerSlideSpeed * Time.deltaTime);
			}
		}
		else
		{
			drainBlocker.position = drainBlockerClosedPosition.position;
		}
	}

	private void UpdateEffects()
	{
		switch (reliableState.state)
		{
		default:
			if (elements != null)
			{
				elements.sodaFizzParticles.gameObject.SetActive(value: false);
				elements.sodaEruptionParticles.gameObject.SetActive(value: false);
				fizzParticleEmission.rateOverTimeMultiplier = 0f;
			}
			hasPlayedEruptionEffects = false;
			hasPlayedDrainEffects = false;
			eruptionAudioSource.GTStop();
			drainAudioSource.GTStop();
			rotatingRingsAudioSource.GTStop();
			break;
		case RisingLiquidState.Drained:
			hasPlayedEruptionEffects = false;
			hasPlayedDrainEffects = false;
			eruptionAudioSource.GTStop();
			drainAudioSource.GTStop();
			rotatingRingsAudioSource.GTStop();
			if (elements != null)
			{
				elements.sodaEruptionParticles.gameObject.SetActive(value: false);
				elements.sodaFizzParticles.gameObject.SetActive(value: true);
				if (reliableState.activationProgress > 0.0010000000474974513)
				{
					fizzParticleEmission.rateOverTimeMultiplier = Mathf.Lerp(sodaFizzParticleEmissionMinMax.x, sodaFizzParticleEmissionMinMax.y, (float)reliableState.activationProgress);
				}
				else
				{
					fizzParticleEmission.rateOverTimeMultiplier = 0f;
				}
			}
			break;
		case RisingLiquidState.Erupting:
			if (!hasPlayedEruptionEffects)
			{
				eruptionAudioSource.loop = true;
				eruptionAudioSource.GTPlay();
				hasPlayedEruptionEffects = true;
				if (elements != null)
				{
					elements.sodaEruptionParticles.gameObject.SetActive(value: true);
					fizzParticleEmission.rateOverTimeMultiplier = sodaFizzParticleEmissionMinMax.y;
				}
			}
			break;
		case RisingLiquidState.Rising:
			if (elements != null)
			{
				fizzParticleEmission.rateOverTimeMultiplier = 0f;
			}
			break;
		case RisingLiquidState.Draining:
			hasPlayedEruptionEffects = false;
			eruptionAudioSource.GTStop();
			if (elements != null)
			{
				elements.sodaFizzParticles.gameObject.SetActive(value: false);
				elements.sodaEruptionParticles.gameObject.SetActive(value: false);
				fizzParticleEmission.rateOverTimeMultiplier = 0f;
			}
			if (!hasPlayedDrainEffects)
			{
				drainAudioSource.loop = true;
				drainAudioSource.GTPlay();
				rotatingRingsAudioSource.loop = true;
				rotatingRingsAudioSource.GTPlay();
				hasPlayedDrainEffects = true;
			}
			break;
		}
	}

	private void DisableObjectsInContactWithLava(float lavaScale)
	{
		if (elements == null)
		{
			return;
		}
		Plane plane = new Plane(liquidSurfacePlane.up, liquidSurfacePlane.position);
		if (reliableState.state == RisingLiquidState.Rising)
		{
			for (int i = 0; i < elements.disableByLiquidList.Count; i++)
			{
				if (!plane.GetSide(elements.disableByLiquidList[i].target.position + elements.disableByLiquidList[i].heightOffset * Vector3.up))
				{
					elements.disableByLiquidList[i].target.gameObject.SetActive(value: false);
				}
			}
		}
		else
		{
			if (reliableState.state != RisingLiquidState.Draining)
			{
				return;
			}
			for (int j = 0; j < elements.disableByLiquidList.Count; j++)
			{
				if (plane.GetSide(elements.disableByLiquidList[j].target.position + elements.disableByLiquidList[j].heightOffset * Vector3.up))
				{
					elements.disableByLiquidList[j].target.gameObject.SetActive(value: true);
				}
			}
		}
	}

	private void UpdateWinner()
	{
		float num = -1f;
		for (int i = 0; i < inGamePlayerCount; i++)
		{
			if (!inGamePlayerStates[i].touchedLiquid)
			{
				lastWinnerId = inGamePlayerStates[i].playerId;
				break;
			}
			if (inGamePlayerStates[i].touchedLiquidAtProgress > num)
			{
				num = inGamePlayerStates[i].touchedLiquidAtProgress;
				lastWinnerId = inGamePlayerStates[i].playerId;
			}
		}
		RefreshWinnerName();
	}

	private void RefreshWinnerName()
	{
		NetPlayer playerFromId = GetPlayerFromId(lastWinnerId);
		if (playerFromId != null)
		{
			lastWinnerName = playerFromId.NickName;
		}
		else
		{
			lastWinnerName = "None";
		}
	}

	private NetPlayer GetPlayerFromId(int id)
	{
		if (NetworkSystem.Instance.InRoom)
		{
			return NetworkSystem.Instance.GetPlayer(id);
		}
		if (id == NetworkSystem.Instance.LocalPlayer.ActorNumber)
		{
			return NetworkSystem.Instance.LocalPlayer;
		}
		return null;
	}

	private void UpdateRefreshWater()
	{
		if (refreshWaterVolume != null)
		{
			if (RefreshWaterAvailable && !refreshWaterVolume.gameObject.activeSelf)
			{
				refreshWaterVolume.gameObject.SetActive(value: true);
			}
			else if (!RefreshWaterAvailable && refreshWaterVolume.gameObject.activeSelf)
			{
				refreshWaterVolume.gameObject.SetActive(value: false);
			}
		}
	}

	private void ResetGame()
	{
		for (int i = 0; i < inGamePlayerCount; i++)
		{
			PlayerGameState playerGameState = inGamePlayerStates[i];
			playerGameState.touchedLiquid = false;
			playerGameState.touchedLiquidAtProgress = -1f;
			inGamePlayerStates[i] = playerGameState;
		}
	}

	public void RestartGame()
	{
		if (base.IsMine)
		{
			riseTime = riseTimeLookup[(int)nextRoundRiseSpeed];
			reliableState.state = RisingLiquidState.Erupting;
			reliableState.stateStartTime = (NetworkSystem.Instance.InRoom ? NetworkSystem.Instance.SimTime : ((double)Time.time));
			reliableState.stateStartLiquidProgressLinear = 0f;
			reliableState.activationProgress = 1.0;
			ResetGame();
		}
	}

	public void DebugErupt()
	{
		if (base.IsMine)
		{
			riseTime = riseTimeLookup[(int)nextRoundRiseSpeed];
			reliableState.state = RisingLiquidState.Erupting;
			reliableState.stateStartTime = (NetworkSystem.Instance.InRoom ? NetworkSystem.Instance.SimTime : ((double)Time.time));
			reliableState.stateStartLiquidProgressLinear = 0f;
			reliableState.activationProgress = 1.0;
		}
	}

	public void RandomizeRings()
	{
		for (int i = 0; i < rotatingRings.Length; i++)
		{
			float num = Mathf.Repeat(rotatingRings[i].resultingAngle, 360f);
			float num2 = UnityEngine.Random.Range(rotatingRingRandomAngleRange.x, rotatingRingRandomAngleRange.y);
			float num3 = ((UnityEngine.Random.Range(0f, 1f) > 0.5f) ? 1f : (-1f));
			rotatingRings[i].initialAngle = num;
			float num4 = num + num3 * num2;
			if (rotatingRingQuantizeAngles)
			{
				num4 = Mathf.Round(num4 / rotatingRingAngleSnapDegrees) * rotatingRingAngleSnapDegrees;
			}
			rotatingRings[i].resultingAngle = num4;
		}
		if (rotateRingsCoroutine != null)
		{
			StopCoroutine(rotateRingsCoroutine);
		}
		rotateRingsCoroutine = StartCoroutine(RotateRingsCoroutine());
	}

	private IEnumerator RotateRingsCoroutine()
	{
		if (debugRotateRingsTime > 0.01f)
		{
			float routineStartTime = Time.time;
			ringRotationProgress = 0f;
			debugRandomizingRings = true;
			while (ringRotationProgress < 1f)
			{
				ringRotationProgress = (Time.time - routineStartTime) / debugRotateRingsTime;
				yield return null;
			}
		}
		debugRandomizingRings = false;
		ringRotationProgress = 1f;
	}

	public bool GetMaterialIfPlayerInGame(int playerActorNumber, out int materialIndex)
	{
		for (int i = 0; i < inGamePlayerCount; i++)
		{
			if (inGamePlayerStates[i].playerId == playerActorNumber)
			{
				if (inGamePlayerStates[i].touchedLiquid)
				{
					materialIndex = 12;
					return true;
				}
				materialIndex = 0;
				return true;
			}
		}
		materialIndex = 0;
		return false;
	}

	private void OnPlayerTagged(NetPlayer taggedPlayer, NetPlayer taggingPlayer)
	{
		if (!base.IsMine)
		{
			return;
		}
		int num = -1;
		int num2 = -1;
		for (int i = 0; i < inGamePlayerCount; i++)
		{
			if (inGamePlayerStates[i].playerId == taggedPlayer.ActorNumber)
			{
				num = i;
			}
			else if (inGamePlayerStates[i].playerId == taggingPlayer.ActorNumber)
			{
				num2 = i;
			}
			if (num != -1 && num2 != -1)
			{
				break;
			}
		}
		if (num == -1 || num2 == -1)
		{
			return;
		}
		switch (tagBehavior)
		{
		case TagBehavior.Infect:
			if (inGamePlayerStates[num2].touchedLiquid && !inGamePlayerStates[num].touchedLiquid)
			{
				PlayerGameState playerGameState2 = inGamePlayerStates[num];
				playerGameState2.touchedLiquid = true;
				playerGameState2.touchedLiquidAtProgress = riseProgressLinear;
				inGamePlayerStates[num] = playerGameState2;
			}
			break;
		case TagBehavior.Revive:
			if (!inGamePlayerStates[num2].touchedLiquid && inGamePlayerStates[num].touchedLiquid)
			{
				PlayerGameState playerGameState = inGamePlayerStates[num];
				playerGameState.touchedLiquid = false;
				playerGameState.touchedLiquidAtProgress = -1f;
				inGamePlayerStates[num] = playerGameState;
			}
			break;
		case TagBehavior.None:
			break;
		}
	}

	private void OnColliderEnteredVolume(Collider collider)
	{
		VRRig component = collider.attachedRigidbody.gameObject.GetComponent<VRRig>();
		if (component != null && component.creator != null)
		{
			PlayerEnteredGameArea(component.creator.ActorNumber);
		}
	}

	private void OnColliderExitedVolume(Collider collider)
	{
		VRRig component = collider.attachedRigidbody.gameObject.GetComponent<VRRig>();
		if (component != null && component.creator != null)
		{
			PlayerExitedGameArea(component.creator.ActorNumber);
		}
	}

	private void OnColliderEnteredSoda(WaterVolume volume, Collider collider)
	{
		if (collider == GTPlayer.Instance.bodyCollider)
		{
			if (base.IsMine)
			{
				PlayerTouchedLava(NetworkSystem.Instance.LocalPlayer.ActorNumber);
			}
			else
			{
				base.GetView.RPC("PlayerTouchedLavaRPC", RpcTarget.MasterClient);
			}
		}
	}

	private void OnColliderExitedSoda(WaterVolume volume, Collider collider)
	{
	}

	private void OnColliderEnteredRefreshWater(WaterVolume volume, Collider collider)
	{
		if (collider == GTPlayer.Instance.bodyCollider)
		{
			if (base.IsMine)
			{
				PlayerTouchedRefreshWater(NetworkSystem.Instance.LocalPlayer.ActorNumber);
			}
			else
			{
				base.GetView.RPC("PlayerTouchedRefreshWaterRPC", RpcTarget.MasterClient);
			}
		}
	}

	private void OnColliderExitedRefreshWater(WaterVolume volume, Collider collider)
	{
	}

	private void OnProjectileEnteredSodaWater(SlingshotProjectile projectile, Collider collider)
	{
		if (projectile.gameObject.CompareTag(mentoProjectileTag))
		{
			AddLavaRock(projectile.projectileOwner.ActorNumber);
		}
	}

	private void AddLavaRock(int playerId)
	{
		if (!base.IsMine || reliableState.state != RisingLiquidState.Drained)
		{
			return;
		}
		bool flag = false;
		for (int i = 0; i < inGamePlayerCount; i++)
		{
			if (!inGamePlayerStates[i].touchedLiquid)
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			float num = lavaActivationRockProgressVsPlayerCount.Evaluate(PlayerCount);
			reliableState.activationProgress += num;
		}
	}

	public void OnWaterBalloonHitPlayer(NetPlayer hitPlayer)
	{
		bool flag = false;
		for (int i = 0; i < inGamePlayerCount; i++)
		{
			if (inGamePlayerStates[i].playerId == hitPlayer.ActorNumber)
			{
				flag = true;
			}
		}
		if (flag)
		{
			if (hitPlayer == NetworkSystem.Instance.LocalPlayer)
			{
				ValidateLocalPlayerWaterBalloonHit(hitPlayer.ActorNumber);
				return;
			}
			base.GetView.RPC("ValidateLocalPlayerWaterBalloonHitRPC", RpcTarget.Others, hitPlayer.ActorNumber);
		}
	}

	public override void WriteDataFusion()
	{
		ScienceManagerData data = new ScienceManagerData((int)reliableState.state, reliableState.stateStartTime, reliableState.stateStartLiquidProgressLinear, reliableState.activationProgress, (int)nextRoundRiseSpeed, riseTime, lastWinnerId, inGamePlayerCount, inGamePlayerStates, rotatingRings);
		Data = data;
	}

	public override void ReadDataFusion()
	{
		int num = lastWinnerId;
		_ = nextRoundRiseSpeed;
		reliableState.state = (RisingLiquidState)Data.reliableState;
		reliableState.stateStartTime = Data.stateStartTime;
		reliableState.stateStartLiquidProgressLinear = Data.stateStartLiquidProgressLinear.ClampSafe(0f, 1f);
		reliableState.activationProgress = Data.activationProgress.GetFinite();
		nextRoundRiseSpeed = (RiseSpeed)Data.nextRoundRiseSpeed;
		riseTime = Data.riseTime.GetFinite();
		lastWinnerId = Data.lastWinnerId;
		inGamePlayerCount = Mathf.Clamp(Data.inGamePlayerCount, 0, 10);
		for (int i = 0; i < 10; i++)
		{
			inGamePlayerStates[i].playerId = Data.playerIdArray[i];
			inGamePlayerStates[i].touchedLiquid = Data.touchedLiquidArray[i];
			inGamePlayerStates[i].touchedLiquidAtProgress = Data.touchedLiquidAtProgressArray[i].ClampSafe(0f, 1f);
		}
		for (int j = 0; j < rotatingRings.Length; j++)
		{
			rotatingRings[j].initialAngle = Data.initialAngleArray[j].GetFinite();
			rotatingRings[j].resultingAngle = Data.resultingAngleArray[j].GetFinite();
		}
		float num2 = riseProgress;
		UpdateLocalState(NetworkSystem.Instance.SimTime, reliableState);
		localLagRiseProgressOffset = num2 - riseProgress;
		if (num != lastWinnerId)
		{
			RefreshWinnerName();
		}
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		stream.SendNext((int)reliableState.state);
		stream.SendNext(reliableState.stateStartTime);
		stream.SendNext(reliableState.stateStartLiquidProgressLinear);
		stream.SendNext(reliableState.activationProgress);
		stream.SendNext((int)nextRoundRiseSpeed);
		stream.SendNext(riseTime);
		stream.SendNext(lastWinnerId);
		stream.SendNext(inGamePlayerCount);
		for (int i = 0; i < 10; i++)
		{
			stream.SendNext(inGamePlayerStates[i].playerId);
			stream.SendNext(inGamePlayerStates[i].touchedLiquid);
			stream.SendNext(inGamePlayerStates[i].touchedLiquidAtProgress);
		}
		for (int j = 0; j < rotatingRings.Length; j++)
		{
			stream.SendNext(rotatingRings[j].initialAngle);
			stream.SendNext(rotatingRings[j].resultingAngle);
		}
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		int num = lastWinnerId;
		_ = nextRoundRiseSpeed;
		reliableState.state = (RisingLiquidState)(int)stream.ReceiveNext();
		reliableState.stateStartTime = ((double)stream.ReceiveNext()).GetFinite();
		reliableState.stateStartLiquidProgressLinear = ((float)stream.ReceiveNext()).ClampSafe(0f, 1f);
		reliableState.activationProgress = ((double)stream.ReceiveNext()).GetFinite();
		nextRoundRiseSpeed = (RiseSpeed)(int)stream.ReceiveNext();
		riseTime = ((float)stream.ReceiveNext()).GetFinite();
		lastWinnerId = (int)stream.ReceiveNext();
		inGamePlayerCount = (int)stream.ReceiveNext();
		inGamePlayerCount = Mathf.Clamp(inGamePlayerCount, 0, 10);
		for (int i = 0; i < 10; i++)
		{
			inGamePlayerStates[i].playerId = (int)stream.ReceiveNext();
			inGamePlayerStates[i].touchedLiquid = (bool)stream.ReceiveNext();
			inGamePlayerStates[i].touchedLiquidAtProgress = ((float)stream.ReceiveNext()).ClampSafe(0f, 1f);
		}
		for (int j = 0; j < rotatingRings.Length; j++)
		{
			rotatingRings[j].initialAngle = ((float)stream.ReceiveNext()).GetFinite();
			rotatingRings[j].resultingAngle = ((float)stream.ReceiveNext()).GetFinite();
		}
		float num2 = riseProgress;
		UpdateLocalState(NetworkSystem.Instance.SimTime, reliableState);
		localLagRiseProgressOffset = num2 - riseProgress;
		if (num != lastWinnerId)
		{
			RefreshWinnerName();
		}
	}

	private void PlayerEnteredGameArea(int pId)
	{
		if (!base.IsMine)
		{
			return;
		}
		bool flag = false;
		for (int i = 0; i < inGamePlayerCount; i++)
		{
			if (inGamePlayerStates[i].playerId == pId)
			{
				flag = true;
				break;
			}
		}
		if (!flag && inGamePlayerCount < 10)
		{
			bool touchedLiquid = false;
			inGamePlayerStates[inGamePlayerCount] = new PlayerGameState
			{
				playerId = pId,
				touchedLiquid = touchedLiquid,
				touchedLiquidAtProgress = -1f
			};
			inGamePlayerCount++;
			if (optPlayersOutOfRoomGameMode)
			{
				GorillaGameModes.GameMode.OptOut(pId);
			}
		}
	}

	private void PlayerExitedGameArea(int playerId)
	{
		if (!base.IsMine)
		{
			return;
		}
		for (int i = 0; i < inGamePlayerCount; i++)
		{
			if (inGamePlayerStates[i].playerId == playerId)
			{
				inGamePlayerStates[i] = inGamePlayerStates[inGamePlayerCount - 1];
				inGamePlayerCount--;
				if (optPlayersOutOfRoomGameMode)
				{
					GorillaGameModes.GameMode.OptIn(playerId);
				}
				break;
			}
		}
	}

	[PunRPC]
	public void PlayerTouchedLavaRPC(PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "PlayerTouchedLavaRPC");
		PlayerTouchedLava(info.Sender.ActorNumber);
	}

	[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
	public unsafe void RPC_PlayerTouchedLava(RpcInfo info = default(RpcInfo))
	{
		if (((NetworkBehaviour)this).InvokeRpc)
		{
			((NetworkBehaviour)this).InvokeRpc = false;
		}
		else
		{
			NetworkBehaviourUtils.ThrowIfBehaviourNotInitialized(this);
			if (base.Runner.Stage == SimulationStages.Resimulate)
			{
				return;
			}
			int localAuthorityMask = base.Object.GetLocalAuthorityMask();
			if ((localAuthorityMask & 7) == 0)
			{
				NetworkBehaviourUtils.NotifyLocalSimulationNotAllowedToSendRpc("System.Void GorillaTag.ScienceExperimentManager::RPC_PlayerTouchedLava(Fusion.RpcInfo)", base.Object, 7);
				return;
			}
			if ((localAuthorityMask & 1) != 1)
			{
				int num = 8;
				if (!SimulationMessage.CanAllocateUserPayload(num))
				{
					NetworkBehaviourUtils.NotifyRpcPayloadSizeExceeded("System.Void GorillaTag.ScienceExperimentManager::RPC_PlayerTouchedLava(Fusion.RpcInfo)", num);
					return;
				}
				if (base.Runner.HasAnyActiveConnections())
				{
					SimulationMessage* ptr = SimulationMessage.Allocate(base.Runner.Simulation, num);
					byte* ptr2 = (byte*)ptr + 28;
					*(RpcHeader*)ptr2 = RpcHeader.Create(base.Object.Id, ((NetworkBehaviour)this).ObjectIndex, 1);
					int num2 = 8;
					ptr->Offset = num2 * 8;
					base.Runner.SendRpc(ptr);
				}
				if ((localAuthorityMask & 1) == 0)
				{
					return;
				}
			}
			info = RpcInfo.FromLocal(base.Runner, RpcChannel.Reliable, RpcHostMode.SourceIsServer);
		}
		PhotonMessageInfoWrapped infoWrapped = new PhotonMessageInfoWrapped(info);
		MonkeAgent.IncrementRPCCall(infoWrapped, "PlayerTouchedLavaRPC");
		PlayerTouchedLava(infoWrapped.Sender.ActorNumber);
	}

	private void PlayerTouchedLava(int playerId)
	{
		if (!base.IsMine)
		{
			return;
		}
		for (int i = 0; i < inGamePlayerCount; i++)
		{
			if (inGamePlayerStates[i].playerId == playerId)
			{
				PlayerGameState playerGameState = inGamePlayerStates[i];
				if (!playerGameState.touchedLiquid)
				{
					playerGameState.touchedLiquidAtProgress = riseProgressLinear;
				}
				playerGameState.touchedLiquid = true;
				inGamePlayerStates[i] = playerGameState;
				break;
			}
		}
	}

	[PunRPC]
	private void PlayerTouchedRefreshWaterRPC(PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "PlayerTouchedRefreshWaterRPC");
		PlayerTouchedRefreshWater(info.Sender.ActorNumber);
	}

	[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
	private unsafe void RPC_PlayerTouchedRefreshWater(RpcInfo info = default(RpcInfo))
	{
		if (((NetworkBehaviour)this).InvokeRpc)
		{
			((NetworkBehaviour)this).InvokeRpc = false;
		}
		else
		{
			NetworkBehaviourUtils.ThrowIfBehaviourNotInitialized(this);
			if (base.Runner.Stage == SimulationStages.Resimulate)
			{
				return;
			}
			int localAuthorityMask = base.Object.GetLocalAuthorityMask();
			if ((localAuthorityMask & 7) == 0)
			{
				NetworkBehaviourUtils.NotifyLocalSimulationNotAllowedToSendRpc("System.Void GorillaTag.ScienceExperimentManager::RPC_PlayerTouchedRefreshWater(Fusion.RpcInfo)", base.Object, 7);
				return;
			}
			if ((localAuthorityMask & 1) != 1)
			{
				int num = 8;
				if (!SimulationMessage.CanAllocateUserPayload(num))
				{
					NetworkBehaviourUtils.NotifyRpcPayloadSizeExceeded("System.Void GorillaTag.ScienceExperimentManager::RPC_PlayerTouchedRefreshWater(Fusion.RpcInfo)", num);
					return;
				}
				if (base.Runner.HasAnyActiveConnections())
				{
					SimulationMessage* ptr = SimulationMessage.Allocate(base.Runner.Simulation, num);
					byte* ptr2 = (byte*)ptr + 28;
					*(RpcHeader*)ptr2 = RpcHeader.Create(base.Object.Id, ((NetworkBehaviour)this).ObjectIndex, 2);
					int num2 = 8;
					ptr->Offset = num2 * 8;
					base.Runner.SendRpc(ptr);
				}
				if ((localAuthorityMask & 1) == 0)
				{
					return;
				}
			}
			info = RpcInfo.FromLocal(base.Runner, RpcChannel.Reliable, RpcHostMode.SourceIsServer);
		}
		PhotonMessageInfoWrapped infoWrapped = new PhotonMessageInfoWrapped(info);
		MonkeAgent.IncrementRPCCall(infoWrapped, "PlayerTouchedRefreshWaterRPC");
		PlayerTouchedRefreshWater(infoWrapped.Sender.ActorNumber);
	}

	private void PlayerTouchedRefreshWater(int playerId)
	{
		if (!base.IsMine || !RefreshWaterAvailable)
		{
			return;
		}
		for (int i = 0; i < inGamePlayerCount; i++)
		{
			if (inGamePlayerStates[i].playerId == playerId)
			{
				PlayerGameState playerGameState = inGamePlayerStates[i];
				playerGameState.touchedLiquid = false;
				playerGameState.touchedLiquidAtProgress = -1f;
				inGamePlayerStates[i] = playerGameState;
				break;
			}
		}
	}

	[PunRPC]
	private void ValidateLocalPlayerWaterBalloonHitRPC(int playerId, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "ValidateLocalPlayerWaterBalloonHitRPC");
		if (playerId == NetworkSystem.Instance.LocalPlayer.ActorNumber)
		{
			ValidateLocalPlayerWaterBalloonHit(playerId);
		}
	}

	[Rpc(InvokeLocal = false)]
	private unsafe void RPC_ValidateLocalPlayerWaterBalloonHit(int playerId, RpcInfo info = default(RpcInfo))
	{
		if (((NetworkBehaviour)this).InvokeRpc)
		{
			((NetworkBehaviour)this).InvokeRpc = false;
			MonkeAgent.IncrementRPCCall(new PhotonMessageInfoWrapped(info), "ValidateLocalPlayerWaterBalloonHitRPC");
			if (playerId == NetworkSystem.Instance.LocalPlayer.ActorNumber)
			{
				ValidateLocalPlayerWaterBalloonHit(playerId);
			}
			return;
		}
		NetworkBehaviourUtils.ThrowIfBehaviourNotInitialized(this);
		if (base.Runner.Stage == SimulationStages.Resimulate)
		{
			return;
		}
		int localAuthorityMask = base.Object.GetLocalAuthorityMask();
		if ((localAuthorityMask & 7) == 0)
		{
			NetworkBehaviourUtils.NotifyLocalSimulationNotAllowedToSendRpc("System.Void GorillaTag.ScienceExperimentManager::RPC_ValidateLocalPlayerWaterBalloonHit(System.Int32,Fusion.RpcInfo)", base.Object, 7);
			return;
		}
		int num = 8;
		num += 4;
		if (!SimulationMessage.CanAllocateUserPayload(num))
		{
			NetworkBehaviourUtils.NotifyRpcPayloadSizeExceeded("System.Void GorillaTag.ScienceExperimentManager::RPC_ValidateLocalPlayerWaterBalloonHit(System.Int32,Fusion.RpcInfo)", num);
		}
		else if (base.Runner.HasAnyActiveConnections())
		{
			SimulationMessage* ptr = SimulationMessage.Allocate(base.Runner.Simulation, num);
			byte* ptr2 = (byte*)ptr + 28;
			*(RpcHeader*)ptr2 = RpcHeader.Create(base.Object.Id, ((NetworkBehaviour)this).ObjectIndex, 3);
			int num2 = 8;
			*(int*)(ptr2 + num2) = playerId;
			num2 += 4;
			ptr->Offset = num2 * 8;
			base.Runner.SendRpc(ptr);
		}
	}

	private void ValidateLocalPlayerWaterBalloonHit(int playerId)
	{
		if (playerId == NetworkSystem.Instance.LocalPlayer.ActorNumber && !GTPlayer.Instance.InWater)
		{
			if (base.IsMine)
			{
				PlayerHitByWaterBalloon(NetworkSystem.Instance.LocalPlayer.ActorNumber);
				return;
			}
			base.GetView.RPC("PlayerHitByWaterBalloonRPC", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
		}
	}

	[PunRPC]
	private void PlayerHitByWaterBalloonRPC(int playerId, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "PlayerHitByWaterBalloonRPC");
		PlayerHitByWaterBalloon(playerId);
	}

	[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
	private unsafe void RPC_PlayerHitByWaterBalloon(int playerId, RpcInfo info = default(RpcInfo))
	{
		if (((NetworkBehaviour)this).InvokeRpc)
		{
			((NetworkBehaviour)this).InvokeRpc = false;
		}
		else
		{
			NetworkBehaviourUtils.ThrowIfBehaviourNotInitialized(this);
			if (base.Runner.Stage == SimulationStages.Resimulate)
			{
				return;
			}
			int localAuthorityMask = base.Object.GetLocalAuthorityMask();
			if ((localAuthorityMask & 7) == 0)
			{
				NetworkBehaviourUtils.NotifyLocalSimulationNotAllowedToSendRpc("System.Void GorillaTag.ScienceExperimentManager::RPC_PlayerHitByWaterBalloon(System.Int32,Fusion.RpcInfo)", base.Object, 7);
				return;
			}
			if ((localAuthorityMask & 1) != 1)
			{
				int num = 8;
				num += 4;
				if (!SimulationMessage.CanAllocateUserPayload(num))
				{
					NetworkBehaviourUtils.NotifyRpcPayloadSizeExceeded("System.Void GorillaTag.ScienceExperimentManager::RPC_PlayerHitByWaterBalloon(System.Int32,Fusion.RpcInfo)", num);
					return;
				}
				if (base.Runner.HasAnyActiveConnections())
				{
					SimulationMessage* ptr = SimulationMessage.Allocate(base.Runner.Simulation, num);
					byte* ptr2 = (byte*)ptr + 28;
					*(RpcHeader*)ptr2 = RpcHeader.Create(base.Object.Id, ((NetworkBehaviour)this).ObjectIndex, 4);
					int num2 = 8;
					*(int*)(ptr2 + num2) = playerId;
					num2 += 4;
					ptr->Offset = num2 * 8;
					base.Runner.SendRpc(ptr);
				}
				if ((localAuthorityMask & 1) == 0)
				{
					return;
				}
			}
			info = RpcInfo.FromLocal(base.Runner, RpcChannel.Reliable, RpcHostMode.SourceIsServer);
		}
		MonkeAgent.IncrementRPCCall(new PhotonMessageInfoWrapped(info), "PlayerHitByWaterBalloonRPC");
		PlayerHitByWaterBalloon(playerId);
	}

	private void PlayerHitByWaterBalloon(int playerId)
	{
		if (!base.IsMine)
		{
			return;
		}
		for (int i = 0; i < inGamePlayerCount; i++)
		{
			if (inGamePlayerStates[i].playerId == playerId)
			{
				PlayerGameState playerGameState = inGamePlayerStates[i];
				playerGameState.touchedLiquid = false;
				playerGameState.touchedLiquidAtProgress = -1f;
				inGamePlayerStates[i] = playerGameState;
				break;
			}
		}
	}

	public void OnPlayerLeftRoom(NetPlayer otherPlayer)
	{
		PlayerExitedGameArea(otherPlayer.ActorNumber);
	}

	public void OnLeftRoom()
	{
		inGamePlayerCount = 0;
		for (int i = 0; i < inGamePlayerCount; i++)
		{
			if (inGamePlayerStates[i].playerId == NetworkSystem.Instance.LocalPlayer.ActorNumber)
			{
				inGamePlayerStates[0] = inGamePlayerStates[i];
				inGamePlayerCount = 1;
				break;
			}
		}
	}

	protected override void OnOwnerSwitched(NetPlayer newOwningPlayer)
	{
		base.OnOwnerSwitched(newOwningPlayer);
		if (!NetworkSystem.Instance.IsMasterClient)
		{
			return;
		}
		for (int i = 0; i < inGamePlayerCount; i++)
		{
			if (!Utils.PlayerInRoom(inGamePlayerStates[i].playerId))
			{
				inGamePlayerStates[i] = inGamePlayerStates[inGamePlayerCount - 1];
				inGamePlayerCount--;
				i--;
			}
		}
	}

	[WeaverGenerated]
	public override void CopyBackingFieldsToState(bool P_0)
	{
		base.CopyBackingFieldsToState(P_0);
		Data = _Data;
	}

	[WeaverGenerated]
	public override void CopyStateToBackingFields()
	{
		base.CopyStateToBackingFields();
		_Data = Data;
	}

	[NetworkRpcWeavedInvoker(1, 7, 1)]
	[Preserve]
	[WeaverGenerated]
	protected unsafe static void RPC_PlayerTouchedLava_0040Invoker(NetworkBehaviour behaviour, SimulationMessage* message)
	{
		byte* ptr = (byte*)message + 28;
		int num = 8;
		RpcInfo info = RpcInfo.FromMessage(behaviour.Runner, message, RpcHostMode.SourceIsServer);
		behaviour.InvokeRpc = true;
		((ScienceExperimentManager)behaviour).RPC_PlayerTouchedLava(info);
	}

	[NetworkRpcWeavedInvoker(2, 7, 1)]
	[Preserve]
	[WeaverGenerated]
	protected unsafe static void RPC_PlayerTouchedRefreshWater_0040Invoker(NetworkBehaviour behaviour, SimulationMessage* message)
	{
		byte* ptr = (byte*)message + 28;
		int num = 8;
		RpcInfo info = RpcInfo.FromMessage(behaviour.Runner, message, RpcHostMode.SourceIsServer);
		behaviour.InvokeRpc = true;
		((ScienceExperimentManager)behaviour).RPC_PlayerTouchedRefreshWater(info);
	}

	[NetworkRpcWeavedInvoker(3, 7, 7)]
	[Preserve]
	[WeaverGenerated]
	protected unsafe static void RPC_ValidateLocalPlayerWaterBalloonHit_0040Invoker(NetworkBehaviour behaviour, SimulationMessage* message)
	{
		byte* ptr = (byte*)message + 28;
		int num = 8;
		int num2 = *(int*)(ptr + num);
		num += 4;
		int playerId = num2;
		RpcInfo info = RpcInfo.FromMessage(behaviour.Runner, message, RpcHostMode.SourceIsServer);
		behaviour.InvokeRpc = true;
		((ScienceExperimentManager)behaviour).RPC_ValidateLocalPlayerWaterBalloonHit(playerId, info);
	}

	[NetworkRpcWeavedInvoker(4, 7, 1)]
	[Preserve]
	[WeaverGenerated]
	protected unsafe static void RPC_PlayerHitByWaterBalloon_0040Invoker(NetworkBehaviour behaviour, SimulationMessage* message)
	{
		byte* ptr = (byte*)message + 28;
		int num = 8;
		int num2 = *(int*)(ptr + num);
		num += 4;
		int playerId = num2;
		RpcInfo info = RpcInfo.FromMessage(behaviour.Runner, message, RpcHostMode.SourceIsServer);
		behaviour.InvokeRpc = true;
		((ScienceExperimentManager)behaviour).RPC_PlayerHitByWaterBalloon(playerId, info);
	}
}

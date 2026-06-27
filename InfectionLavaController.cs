using System;
using System.Collections.Generic;
using GorillaGameModes;
using GorillaLocomotion;
using GorillaLocomotion.Swimming;
using GorillaTag;
using GorillaTag.Rendering;
using UnityEngine;

public class InfectionLavaController : MonoBehaviour, ITickSystemPost
{
	public enum RisingLavaState
	{
		Drained,
		Erupting,
		Rising,
		Full,
		Draining
	}

	private struct LavaSyncData
	{
		public RisingLavaState state;

		public double stateStartTime;

		public float activationProgress;
	}

	[OnEnterPlay_SetNew]
	private static readonly List<InfectionLavaController> activeControllers = new List<InfectionLavaController>();

	[SerializeField]
	private GTZone zone;

	[SerializeField]
	private float lavaMeshMinScale = 3.17f;

	[Tooltip("If you throw rocks into the volcano quickly enough, then it will raise to this height.")]
	[SerializeField]
	private float lavaMeshMaxScale = 8.941086f;

	[SerializeField]
	private float eruptTime = 3f;

	[SerializeField]
	private float riseTime = 10f;

	[SerializeField]
	private float fullTime = 240f;

	[SerializeField]
	private float drainTime = 10f;

	[Tooltip("Delay added when starting the eruption cycle so the sync event has time to reach other clients before visuals begin.")]
	[SerializeField]
	private float latencyBuffer = 0.5f;

	[SerializeField]
	private float lagResolutionLavaProgressPerSecond = 0.2f;

	[SerializeField]
	private AnimationCurve lavaProgressAnimationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[Header("Volcano Activation")]
	[SerializeField]
	[Range(0f, 1f)]
	private float activationVotePercentageDefaultQueue = 0.42f;

	[SerializeField]
	[Range(0f, 1f)]
	private float activationVotePercentageCompetitiveQueue = 0.6f;

	[SerializeField]
	private Gradient lavaActivationGradient;

	[SerializeField]
	private AnimationCurve lavaActivationRockProgressVsPlayerCount = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[SerializeField]
	private AnimationCurve lavaActivationDrainRateVsPlayerCount = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[SerializeField]
	private float lavaActivationVisualMovementProgressPerSecond = 1f;

	[SerializeField]
	private bool debugLavaActivationVotes;

	[Header("Scene References")]
	[SerializeField]
	private Transform lavaMeshTransform;

	[SerializeField]
	private Transform lavaSurfacePlaneTransform;

	[SerializeField]
	private WaterVolume lavaVolume;

	[SerializeField]
	private MeshRenderer lavaActivationRenderer;

	[SerializeField]
	private Transform lavaActivationStartPos;

	[SerializeField]
	private Transform lavaActivationEndPos;

	[SerializeField]
	private SlingshotProjectileHitNotifier lavaActivationProjectileHitNotifier;

	[SerializeField]
	private VolcanoEffects[] volcanoEffects;

	[SerializeField]
	private ZoneShaderSettings lavaZoneShaderSettings;

	[SerializeField]
	private ZoneShaderSettings baseZoneShaderSettings;

	[DebugReadout]
	private LavaSyncData reliableState;

	private readonly int[] lavaActivationVotePlayerIds = new int[20];

	private int lavaActivationVoteCount;

	private float localLagLavaProgressOffset;

	[DebugReadout]
	private float lavaProgressLinear;

	[DebugReadout]
	private float lavaProgressSmooth;

	private double lastTagSelfRPCTime;

	private const string lavaRockProjectileTag = "LavaRockProjectile";

	private double currentTime;

	private double prevTime;

	private float activationProgessSmooth;

	private float lavaScale;

	private MaterialPropertyBlock lavaActivationMPB;

	private double lastSyncSendTime;

	private const double syncInterval = 2.0;

	private bool localPlayerInZone;

	private static readonly int _shaderProp_GlobalMainWaterSurfacePlane = Shader.PropertyToID("_GlobalMainWaterSurfacePlane");

	private static readonly int _shaderProp_GlobalLavaResidueParams = Shader.PropertyToID("_GlobalLavaResidueParams");

	[Header("Lava Residue")]
	[SerializeField]
	[Range(0f, 1f)]
	private float residueIntensity = 0.85f;

	[Tooltip("How fast the residue plane trails behind the lava when draining (world units/sec).")]
	[SerializeField]
	private float residueDrainSpeed = 1.5f;

	[Tooltip("UV scale for the residue texture in world space.")]
	[SerializeField]
	private float residueUVScale = 0.25f;

	[SerializeField]
	private float residueOffset = 2f;

	private float residuePlaneY;

	public static IReadOnlyList<InfectionLavaController> ActiveControllers => activeControllers;

	public GTZone Zone => zone;

	private bool IsAuthority
	{
		get
		{
			if (!NetworkSystem.Instance.InRoom)
			{
				return true;
			}
			int zoneAuthorityActorNumber = GetZoneAuthorityActorNumber();
			if (zoneAuthorityActorNumber == int.MaxValue)
			{
				return RoomSystem.AmITheHost;
			}
			return zoneAuthorityActorNumber == NetworkSystem.Instance.LocalPlayer.ActorNumber;
		}
	}

	public bool LavaCurrentlyActivated => reliableState.state != RisingLavaState.Drained;

	public Plane LavaPlane => new Plane(lavaSurfacePlaneTransform.up, lavaSurfacePlaneTransform.position);

	public Vector3 SurfaceCenter => lavaSurfacePlaneTransform.position;

	private int PlayerCount
	{
		get
		{
			int result = 1;
			GorillaGameManager instance = GorillaGameManager.instance;
			if (instance != null && instance.currentNetPlayerArray != null)
			{
				result = instance.currentNetPlayerArray.Length;
			}
			return result;
		}
	}

	private bool InCompetitiveQueue
	{
		get
		{
			if (!NetworkSystem.Instance.InRoom)
			{
				return false;
			}
			return NetworkSystem.Instance.GameModeString.Contains("COMPETITIVE");
		}
	}

	bool ITickSystemPost.PostTickRunning { get; set; }

	public static InfectionLavaController GetControllerForZone(GTZone zone)
	{
		for (int i = 0; i < activeControllers.Count; i++)
		{
			if (activeControllers[i].zone == zone)
			{
				return activeControllers[i];
			}
		}
		return null;
	}

	private void Awake()
	{
		lavaActivationMPB = new MaterialPropertyBlock();
		RoomSystem.LeftRoomEvent += new Action(OnLeftRoom);
		RoomSystem.PlayerLeftEvent += new Action<NetPlayer>(OnPlayerLeftRoom);
		RoomSystem.PlayerJoinedEvent += new Action<NetPlayer>(OnPlayerJoinedRoom);
		RoomSystem.OnLavaSyncReceived = (Action<RoomSystem.LavaSyncEventData>)Delegate.Combine(RoomSystem.OnLavaSyncReceived, new Action<RoomSystem.LavaSyncEventData>(OnLavaSyncReceived));
	}

	protected void OnEnable()
	{
		activeControllers.Add(this);
		VerifyReferences();
		for (int i = 0; i < volcanoEffects.Length; i++)
		{
			if (volcanoEffects[i] != null)
			{
				volcanoEffects[i].PreloadAssets();
			}
		}
		if (lavaVolume != null)
		{
			lavaVolume.ColliderEnteredWater += OnColliderEnteredLava;
		}
		if (lavaActivationProjectileHitNotifier != null)
		{
			lavaActivationProjectileHitNotifier.OnProjectileHit += OnActivationLavaProjectileHit;
		}
		if (localPlayerInZone && lavaZoneShaderSettings != null && reliableState.state != RisingLavaState.Drained)
		{
			lavaZoneShaderSettings.BecomeActiveInstance();
		}
		TickSystem<object>.AddPostTickCallback(this);
	}

	protected void OnDisable()
	{
		activeControllers.Remove(this);
		TickSystem<object>.RemovePostTickCallback(this);
		if (lavaVolume != null)
		{
			lavaVolume.ColliderEnteredWater -= OnColliderEnteredLava;
		}
		if (lavaActivationProjectileHitNotifier != null)
		{
			lavaActivationProjectileHitNotifier.OnProjectileHit -= OnActivationLavaProjectileHit;
		}
		ResetLavaState();
	}

	private void VerifyReferences()
	{
		IfNullThenLogAndDisableSelf(lavaMeshTransform, "lavaMeshTransform");
		IfNullThenLogAndDisableSelf(lavaSurfacePlaneTransform, "lavaSurfacePlaneTransform");
		IfNullThenLogAndDisableSelf(lavaVolume, "lavaVolume");
		IfNullThenLogAndDisableSelf(lavaActivationRenderer, "lavaActivationRenderer");
		IfNullThenLogAndDisableSelf(lavaActivationStartPos, "lavaActivationStartPos");
		IfNullThenLogAndDisableSelf(lavaActivationEndPos, "lavaActivationEndPos");
		IfNullThenLogAndDisableSelf(lavaActivationProjectileHitNotifier, "lavaActivationProjectileHitNotifier");
		for (int i = 0; i < volcanoEffects.Length; i++)
		{
			IfNullThenLogAndDisableSelf(volcanoEffects[i], "volcanoEffects", i);
		}
	}

	private void IfNullThenLogAndDisableSelf(UnityEngine.Object obj, string fieldName, int index = -1)
	{
		if (!(obj != null))
		{
			fieldName = ((index != -1) ? $"{fieldName}[{index}]" : fieldName);
			Debug.LogError("InfectionLavaController: Disabling self because reference `" + fieldName + "` is null.", this);
			base.enabled = false;
		}
	}

	private void OnDestroy()
	{
		RoomSystem.LeftRoomEvent -= new Action(OnLeftRoom);
		RoomSystem.PlayerLeftEvent -= new Action<NetPlayer>(OnPlayerLeftRoom);
		RoomSystem.PlayerJoinedEvent -= new Action<NetPlayer>(OnPlayerJoinedRoom);
		RoomSystem.OnLavaSyncReceived = (Action<RoomSystem.LavaSyncEventData>)Delegate.Remove(RoomSystem.OnLavaSyncReceived, new Action<RoomSystem.LavaSyncEventData>(OnLavaSyncReceived));
	}

	private void ResetLavaState()
	{
		reliableState = default(LavaSyncData);
		lavaProgressLinear = 0f;
		lavaProgressSmooth = 0f;
		localLagLavaProgressOffset = 0f;
		activationProgessSmooth = 0f;
		currentTime = 0.0;
		prevTime = 0.0;
		lastSyncSendTime = 0.0;
		residuePlaneY = GetMinLavaY();
		Shader.SetGlobalVector(_shaderProp_GlobalLavaResidueParams, Vector4.zero);
		for (int i = 0; i < lavaActivationVotePlayerIds.Length; i++)
		{
			lavaActivationVotePlayerIds[i] = 0;
		}
		lavaActivationVoteCount = 0;
		for (int j = 0; j < volcanoEffects.Length; j++)
		{
			volcanoEffects[j]?.SetDrainedState();
		}
		UpdateLava(0f);
		ZoneShaderSettings.ActivateDefaultSettings();
		if (localPlayerInZone && baseZoneShaderSettings != null)
		{
			baseZoneShaderSettings.BecomeActiveInstance();
		}
	}

	void ITickSystemPost.PostTick()
	{
		prevTime = currentTime;
		currentTime = (NetworkSystem.Instance.InRoom ? NetworkSystem.Instance.SimTime : Time.timeAsDouble);
		bool flag = localPlayerInZone;
		localPlayerInZone = CheckLocalPlayerInZone();
		if (IsAuthority)
		{
			RisingLavaState state = reliableState.state;
			UpdateReliableState(currentTime, ref reliableState);
			bool num = reliableState.state != state;
			bool flag2 = reliableState.state != RisingLavaState.Drained && currentTime - lastSyncSendTime > 2.0;
			if (num || flag2)
			{
				SendSyncEvent();
			}
		}
		else
		{
			AdvanceLavaPhaseByTime(currentTime, ref reliableState);
			DrainActivationProgressLocally();
		}
		UpdateLocalState(currentTime, reliableState);
		if (localPlayerInZone && !flag)
		{
			if (reliableState.state != RisingLavaState.Drained)
			{
				if (lavaZoneShaderSettings != null)
				{
					lavaZoneShaderSettings.BecomeActiveInstance();
				}
			}
			else if (baseZoneShaderSettings != null)
			{
				baseZoneShaderSettings.BecomeActiveInstance();
			}
		}
		else if (!localPlayerInZone && flag)
		{
			ZoneShaderSettings.ActivateDefaultSettings();
			Shader.SetGlobalVector(_shaderProp_GlobalLavaResidueParams, Vector4.zero);
		}
		localLagLavaProgressOffset = Mathf.MoveTowards(localLagLavaProgressOffset, 0f, lagResolutionLavaProgressPerSecond * Time.deltaTime);
		UpdateLava(lavaProgressSmooth + localLagLavaProgressOffset);
		UpdateResidueState();
		UpdateVolcanoActivationLava(reliableState.activationProgress);
		CheckLocalPlayerAgainstLava(currentTime);
	}

	private void JumpToState(RisingLavaState state)
	{
		reliableState.state = state;
		switch (state)
		{
		case RisingLavaState.Draining:
		{
			for (int m = 0; m < volcanoEffects.Length; m++)
			{
				volcanoEffects[m]?.SetDrainingState();
			}
			if (localPlayerInZone && lavaZoneShaderSettings != null)
			{
				lavaZoneShaderSettings.BecomeActiveInstance();
			}
			break;
		}
		case RisingLavaState.Drained:
		{
			for (int k = 0; k < volcanoEffects.Length; k++)
			{
				volcanoEffects[k]?.SetDrainedState();
			}
			if (localPlayerInZone)
			{
				ZoneShaderSettings.ActivateDefaultSettings();
				if (baseZoneShaderSettings != null)
				{
					baseZoneShaderSettings.BecomeActiveInstance();
				}
			}
			break;
		}
		case RisingLavaState.Erupting:
		{
			for (int j = 0; j < volcanoEffects.Length; j++)
			{
				volcanoEffects[j]?.SetEruptingState();
			}
			if (localPlayerInZone && lavaZoneShaderSettings != null)
			{
				lavaZoneShaderSettings.BecomeActiveInstance();
			}
			break;
		}
		case RisingLavaState.Rising:
		{
			if (localPlayerInZone && lavaZoneShaderSettings != null)
			{
				lavaZoneShaderSettings.BecomeActiveInstance();
			}
			for (int l = 0; l < volcanoEffects.Length; l++)
			{
				volcanoEffects[l]?.SetRisingState();
			}
			break;
		}
		case RisingLavaState.Full:
		{
			if (localPlayerInZone && lavaZoneShaderSettings != null)
			{
				lavaZoneShaderSettings.BecomeActiveInstance();
			}
			for (int i = 0; i < volcanoEffects.Length; i++)
			{
				volcanoEffects[i]?.SetFullState();
			}
			break;
		}
		}
	}

	private void UpdateReliableState(double currentTime, ref LavaSyncData syncData)
	{
		if (syncData.stateStartTime - currentTime > (double)latencyBuffer + 1.0)
		{
			syncData.stateStartTime = currentTime;
		}
		switch (syncData.state)
		{
		case RisingLavaState.Erupting:
			if (currentTime > syncData.stateStartTime + (double)eruptTime)
			{
				syncData.stateStartTime += eruptTime;
				JumpToState(RisingLavaState.Rising);
			}
			return;
		case RisingLavaState.Rising:
			if (currentTime > syncData.stateStartTime + (double)riseTime)
			{
				syncData.stateStartTime += riseTime;
				JumpToState(RisingLavaState.Full);
			}
			return;
		case RisingLavaState.Full:
			if (currentTime > syncData.stateStartTime + (double)fullTime)
			{
				syncData.stateStartTime += fullTime;
				JumpToState(RisingLavaState.Draining);
			}
			return;
		case RisingLavaState.Draining:
		{
			float num = Mathf.Clamp((float)(currentTime - prevTime), 0f, 0.1f);
			syncData.activationProgress = Mathf.MoveTowards(syncData.activationProgress, 0f, lavaActivationDrainRateVsPlayerCount.Evaluate(PlayerCount) * num);
			if (currentTime > syncData.stateStartTime + (double)drainTime)
			{
				syncData.stateStartTime += drainTime;
				JumpToState(RisingLavaState.Drained);
			}
			return;
		}
		}
		if (syncData.activationProgress > 1f)
		{
			int playerCount = PlayerCount;
			float num2 = (InCompetitiveQueue ? activationVotePercentageCompetitiveQueue : activationVotePercentageDefaultQueue);
			int num3 = Mathf.RoundToInt((float)playerCount * num2);
			if (lavaActivationVoteCount >= num3)
			{
				for (int i = 0; i < lavaActivationVoteCount; i++)
				{
					lavaActivationVotePlayerIds[i] = 0;
				}
				lavaActivationVoteCount = 0;
				syncData.stateStartTime = currentTime + (double)latencyBuffer;
				syncData.activationProgress = 1f;
				JumpToState(RisingLavaState.Erupting);
			}
			return;
		}
		float num4 = Mathf.Clamp((float)(currentTime - prevTime), 0f, 0.1f);
		float activationProgress = syncData.activationProgress;
		syncData.activationProgress = Mathf.MoveTowards(syncData.activationProgress, 0f, lavaActivationDrainRateVsPlayerCount.Evaluate(PlayerCount) * num4);
		if (activationProgress > 0f && syncData.activationProgress <= float.Epsilon)
		{
			VolcanoEffects[] array = volcanoEffects;
			for (int j = 0; j < array.Length; j++)
			{
				array[j].OnVolcanoBellyEmpty();
			}
		}
	}

	private void AdvanceLavaPhaseByTime(double time, ref LavaSyncData syncData)
	{
		if (syncData.stateStartTime - time > (double)latencyBuffer + 1.0)
		{
			syncData.stateStartTime = time;
		}
		switch (syncData.state)
		{
		case RisingLavaState.Erupting:
			if (time > syncData.stateStartTime + (double)eruptTime)
			{
				syncData.stateStartTime += eruptTime;
				JumpToState(RisingLavaState.Rising);
			}
			break;
		case RisingLavaState.Rising:
			if (time > syncData.stateStartTime + (double)riseTime)
			{
				syncData.stateStartTime += riseTime;
				JumpToState(RisingLavaState.Full);
			}
			break;
		case RisingLavaState.Full:
			if (time > syncData.stateStartTime + (double)fullTime)
			{
				syncData.stateStartTime += fullTime;
				JumpToState(RisingLavaState.Draining);
			}
			break;
		case RisingLavaState.Draining:
			if (time > syncData.stateStartTime + (double)drainTime)
			{
				syncData.stateStartTime += drainTime;
				JumpToState(RisingLavaState.Drained);
			}
			break;
		}
	}

	private void DrainActivationProgressLocally()
	{
		if (reliableState.activationProgress <= 0f || (reliableState.state != RisingLavaState.Drained && reliableState.state != RisingLavaState.Draining))
		{
			return;
		}
		float num = Mathf.Clamp((float)(currentTime - prevTime), 0f, 0.1f);
		float activationProgress = reliableState.activationProgress;
		reliableState.activationProgress = Mathf.MoveTowards(reliableState.activationProgress, 0f, lavaActivationDrainRateVsPlayerCount.Evaluate(PlayerCount) * num);
		if (activationProgress > 0f && reliableState.activationProgress <= float.Epsilon)
		{
			for (int i = 0; i < volcanoEffects.Length; i++)
			{
				volcanoEffects[i]?.OnVolcanoBellyEmpty();
			}
		}
	}

	private void UpdateLocalState(double currentTime, LavaSyncData syncData)
	{
		VolcanoEffects[] array;
		switch (syncData.state)
		{
		case RisingLavaState.Erupting:
		{
			lavaProgressLinear = 0f;
			lavaProgressSmooth = 0f;
			float num4 = Mathf.Max(0f, (float)(currentTime - syncData.stateStartTime));
			float progress2 = Mathf.Clamp01(num4 / eruptTime);
			array = this.volcanoEffects;
			foreach (VolcanoEffects volcanoEffects3 in array)
			{
				if (volcanoEffects3 != null)
				{
					volcanoEffects3.UpdateEruptingState(num4, eruptTime - num4, progress2);
				}
			}
			return;
		}
		case RisingLavaState.Rising:
		{
			float num5 = Mathf.Max(0f, (float)(currentTime - syncData.stateStartTime));
			float value = num5 / riseTime;
			lavaProgressLinear = Mathf.Clamp01(value);
			lavaProgressSmooth = lavaProgressAnimationCurve.Evaluate(lavaProgressLinear);
			array = this.volcanoEffects;
			foreach (VolcanoEffects volcanoEffects4 in array)
			{
				if (volcanoEffects4 != null)
				{
					volcanoEffects4.UpdateRisingState(num5, riseTime - num5, lavaProgressLinear);
				}
			}
			return;
		}
		case RisingLavaState.Full:
		{
			lavaProgressLinear = 1f;
			lavaProgressSmooth = 1f;
			float num3 = Mathf.Max(0f, (float)(currentTime - syncData.stateStartTime));
			float progress = Mathf.Clamp01(num3 / fullTime);
			array = this.volcanoEffects;
			foreach (VolcanoEffects volcanoEffects2 in array)
			{
				if (volcanoEffects2 != null)
				{
					volcanoEffects2.UpdateFullState(num3, fullTime - num3, progress);
				}
			}
			return;
		}
		case RisingLavaState.Draining:
		{
			float num = Mathf.Max(0f, (float)(currentTime - syncData.stateStartTime));
			float num2 = Mathf.Clamp01(num / drainTime);
			lavaProgressLinear = 1f - num2;
			lavaProgressSmooth = lavaProgressAnimationCurve.Evaluate(lavaProgressLinear);
			array = this.volcanoEffects;
			foreach (VolcanoEffects volcanoEffects in array)
			{
				if (volcanoEffects != null)
				{
					volcanoEffects.UpdateDrainingState(num, riseTime - num, num2);
				}
			}
			return;
		}
		}
		lavaProgressLinear = 0f;
		lavaProgressSmooth = 0f;
		float time = Mathf.Max(0f, (float)(currentTime - syncData.stateStartTime));
		array = this.volcanoEffects;
		foreach (VolcanoEffects volcanoEffects5 in array)
		{
			if (volcanoEffects5 != null)
			{
				volcanoEffects5.UpdateDrainedState(time);
			}
		}
	}

	private void UpdateLava(float fillProgress)
	{
		lavaScale = Mathf.Lerp(lavaMeshMinScale, lavaMeshMaxScale, fillProgress);
		if (lavaMeshTransform != null)
		{
			lavaMeshTransform.localScale = new Vector3(lavaMeshTransform.localScale.x, lavaMeshTransform.localScale.y, lavaScale);
		}
	}

	private float GetMinLavaY()
	{
		if (lavaSurfacePlaneTransform == null || lavaMeshTransform == null)
		{
			return 0f;
		}
		float z = lavaMeshTransform.localScale.z;
		if (z < 0.001f)
		{
			return lavaSurfacePlaneTransform.position.y;
		}
		float y = lavaMeshTransform.position.y;
		float num = (lavaSurfacePlaneTransform.position.y - y) * (lavaMeshMinScale / z);
		return y + num;
	}

	private void UpdateResidueState()
	{
		float num = ((lavaSurfacePlaneTransform != null) ? lavaSurfacePlaneTransform.position.y : 0f);
		switch (reliableState.state)
		{
		case RisingLavaState.Erupting:
		case RisingLavaState.Rising:
		case RisingLavaState.Full:
			residuePlaneY = num;
			break;
		case RisingLavaState.Draining:
			residuePlaneY = Mathf.MoveTowards(residuePlaneY, num, residueDrainSpeed * Time.deltaTime);
			residuePlaneY = Mathf.Max(residuePlaneY, num);
			break;
		case RisingLavaState.Drained:
		{
			float minLavaY = GetMinLavaY();
			residuePlaneY = Mathf.MoveTowards(residuePlaneY, minLavaY, residueDrainSpeed * Time.deltaTime);
			break;
		}
		}
		if (localPlayerInZone)
		{
			float minLavaY2 = GetMinLavaY();
			float y = ((reliableState.state != RisingLavaState.Drained || residuePlaneY > minLavaY2 + 0.01f) ? residueIntensity : 0f);
			Shader.SetGlobalVector(_shaderProp_GlobalLavaResidueParams, new Vector4(residuePlaneY + residueOffset, y, residueUVScale, 0f));
		}
	}

	private void UpdateVolcanoActivationLava(float activationProgress)
	{
		if (!(lavaActivationRenderer == null))
		{
			activationProgessSmooth = Mathf.MoveTowards(activationProgessSmooth, activationProgress, lavaActivationVisualMovementProgressPerSecond * Time.deltaTime);
			lavaActivationMPB.SetColor(ShaderProps._BaseColor, lavaActivationGradient.Evaluate(activationProgessSmooth));
			lavaActivationRenderer.SetPropertyBlock(lavaActivationMPB);
			lavaActivationRenderer.transform.position = Vector3.Lerp(lavaActivationStartPos.position, lavaActivationEndPos.position, activationProgessSmooth);
		}
	}

	private void CheckLocalPlayerAgainstLava(double currentTime)
	{
		if (GTPlayer.Instance.InWater && GTPlayer.Instance.CurrentWaterVolume == lavaVolume)
		{
			LocalPlayerInLava(currentTime, enteredLavaThisFrame: false);
		}
	}

	private void OnColliderEnteredLava(WaterVolume volume, Collider collider)
	{
		if (collider == GTPlayer.Instance.bodyCollider)
		{
			LocalPlayerInLava(NetworkSystem.Instance.InRoom ? NetworkSystem.Instance.SimTime : Time.timeAsDouble, enteredLavaThisFrame: true);
		}
	}

	private void LocalPlayerInLava(double currentTime, bool enteredLavaThisFrame)
	{
		GorillaGameManager instance = GorillaGameManager.instance;
		if (instance != null && instance.CanAffectPlayer(NetworkSystem.Instance.LocalPlayer, enteredLavaThisFrame) && (currentTime - lastTagSelfRPCTime > 0.5 || enteredLavaThisFrame))
		{
			lastTagSelfRPCTime = currentTime;
			GameMode.ReportHit();
		}
	}

	public void OnActivationLavaProjectileHit(SlingshotProjectile projectile, Collision collision)
	{
		if (!projectile.gameObject.CompareTag("LavaRockProjectile") || reliableState.state != RisingLavaState.Drained)
		{
			return;
		}
		if (IsAuthority)
		{
			AddLavaRock(projectile.projectileOwner.ActorNumber);
			return;
		}
		reliableState.activationProgress += lavaActivationRockProgressVsPlayerCount.Evaluate(PlayerCount);
		for (int i = 0; i < volcanoEffects.Length; i++)
		{
			volcanoEffects[i].OnStoneAccepted(reliableState.activationProgress);
		}
	}

	private void AddLavaRock(int playerId)
	{
		float num = lavaActivationRockProgressVsPlayerCount.Evaluate(PlayerCount);
		reliableState.activationProgress += num;
		AddVoteForVolcanoActivation(playerId);
		for (int i = 0; i < volcanoEffects.Length; i++)
		{
			volcanoEffects[i].OnStoneAccepted(reliableState.activationProgress);
		}
		SendSyncEvent();
	}

	private void AddVoteForVolcanoActivation(int playerId)
	{
		if (!IsAuthority || lavaActivationVoteCount >= 20)
		{
			return;
		}
		bool flag = false;
		for (int i = 0; i < lavaActivationVoteCount; i++)
		{
			if (lavaActivationVotePlayerIds[i] == playerId)
			{
				flag = true;
			}
		}
		if (!flag)
		{
			lavaActivationVotePlayerIds[lavaActivationVoteCount] = playerId;
			lavaActivationVoteCount++;
		}
	}

	private void RemoveVoteForVolcanoActivation(int playerId)
	{
		if (!IsAuthority)
		{
			return;
		}
		for (int i = 0; i < lavaActivationVoteCount; i++)
		{
			if (lavaActivationVotePlayerIds[i] == playerId)
			{
				lavaActivationVotePlayerIds[i] = lavaActivationVotePlayerIds[lavaActivationVoteCount - 1];
				lavaActivationVoteCount--;
				break;
			}
		}
	}

	private void SendSyncEvent()
	{
		lastSyncSendTime = currentTime;
		if (NetworkSystem.Instance.InRoom)
		{
			RoomSystem.SendLavaSync((byte)zone, (byte)reliableState.state, reliableState.stateStartTime, reliableState.activationProgress, lavaActivationVoteCount, lavaActivationVotePlayerIds);
		}
	}

	private void SendSyncEventToPlayer(NetPlayer target)
	{
		if (NetworkSystem.Instance.InRoom)
		{
			RoomSystem.SendLavaSyncToPlayer((byte)zone, (byte)reliableState.state, reliableState.stateStartTime, reliableState.activationProgress, lavaActivationVoteCount, lavaActivationVotePlayerIds, target);
		}
	}

	private unsafe void OnLavaSyncReceived(RoomSystem.LavaSyncEventData data)
	{
		if (data.zone != (byte)zone || IsAuthority)
		{
			return;
		}
		int zoneAuthorityActorNumber = GetZoneAuthorityActorNumber();
		if (zoneAuthorityActorNumber == int.MaxValue || data.senderActorNumber == zoneAuthorityActorNumber)
		{
			RisingLavaState state = (RisingLavaState)data.state;
			float num = lavaProgressSmooth;
			reliableState.stateStartTime = data.stateStartTime;
			reliableState.activationProgress = data.activationProgress;
			lavaActivationVoteCount = data.voteCount;
			for (int i = 0; i < 20; i++)
			{
				lavaActivationVotePlayerIds[i] = data.votes[i];
			}
			if (state != reliableState.state)
			{
				JumpToState(state);
			}
			UpdateLocalState(NetworkSystem.Instance.InRoom ? NetworkSystem.Instance.SimTime : Time.timeAsDouble, reliableState);
			localLagLavaProgressOffset = num - lavaProgressSmooth;
		}
	}

	private void OnPlayerJoinedRoom(NetPlayer player)
	{
		if (IsAuthority)
		{
			SendSyncEventToPlayer(player);
		}
	}

	public void OnPlayerLeftRoom(NetPlayer otherNetPlayer)
	{
		RemoveVoteForVolcanoActivation(otherNetPlayer.ActorNumber);
		if (reliableState.state != RisingLavaState.Drained)
		{
			if (localPlayerInZone && lavaZoneShaderSettings != null)
			{
				lavaZoneShaderSettings.BecomeActiveInstance();
			}
			if (IsAuthority)
			{
				SendSyncEvent();
			}
		}
	}

	private void OnLeftRoom()
	{
		if (reliableState.state != RisingLavaState.Drained)
		{
			double num = currentTime - reliableState.stateStartTime;
			double timeAsDouble = Time.timeAsDouble;
			reliableState.stateStartTime = timeAsDouble - num;
			currentTime = timeAsDouble;
			prevTime = timeAsDouble;
			lastSyncSendTime = 0.0;
			for (int i = 0; i < lavaActivationVotePlayerIds.Length; i++)
			{
				lavaActivationVotePlayerIds[i] = 0;
			}
			lavaActivationVoteCount = 0;
		}
		else
		{
			ZoneShaderSettings.ActivateDefaultSettings();
			if (baseZoneShaderSettings != null)
			{
				baseZoneShaderSettings.BecomeActiveInstance();
			}
			ResetLavaState();
		}
	}

	private int CountRigsInZone()
	{
		int num = 0;
		IReadOnlyList<VRRig> activeRigs = VRRigCache.ActiveRigs;
		for (int i = 0; i < activeRigs.Count; i++)
		{
			if (activeRigs[i] != null && activeRigs[i].zoneEntity.currentZone == zone)
			{
				num++;
			}
		}
		return num;
	}

	private bool CheckLocalPlayerInZone()
	{
		IReadOnlyList<VRRig> activeRigs = VRRigCache.ActiveRigs;
		for (int i = 0; i < activeRigs.Count; i++)
		{
			if (activeRigs[i] != null && activeRigs[i].isLocal)
			{
				return activeRigs[i].zoneEntity.currentZone == zone;
			}
		}
		return false;
	}

	private int GetZoneAuthorityActorNumber()
	{
		int num = int.MaxValue;
		IReadOnlyList<VRRig> activeRigs = VRRigCache.ActiveRigs;
		for (int i = 0; i < activeRigs.Count; i++)
		{
			VRRig vRRig = activeRigs[i];
			if (vRRig == null || vRRig.zoneEntity.currentZone != zone)
			{
				continue;
			}
			int actorNumber;
			if (vRRig.isLocal)
			{
				actorNumber = NetworkSystem.Instance.LocalPlayer.ActorNumber;
			}
			else
			{
				NetPlayer creator = vRRig.Creator;
				if (creator == null)
				{
					continue;
				}
				actorNumber = creator.ActorNumber;
			}
			if (actorNumber < num)
			{
				num = actorNumber;
			}
		}
		return num;
	}
}

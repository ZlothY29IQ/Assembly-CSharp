using System;
using GorillaExtensions;
using GorillaGameModes;
using GorillaLocomotion;
using GorillaLocomotion.Swimming;
using GorillaTag.GuidedRefs;
using Photon.Pun;
using UnityEngine;

namespace GorillaTag;

public class InfectionLavaController : MonoBehaviour, IGorillaSerializeableScene, IGorillaSerializeable, ITickSystemPost, IGuidedRefReceiverMono, IGuidedRefMonoBehaviour, IGuidedRefObject
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

		public double activationProgress;
	}

	[OnEnterPlay_SetNull]
	private static InfectionLavaController instance;

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
	private GuidedRefReceiverFieldInfo lavaMeshTransform_gRef = new GuidedRefReceiverFieldInfo(useRecommendedDefaults: true);

	[SerializeField]
	private Transform lavaSurfacePlaneTransform;

	[SerializeField]
	private GuidedRefReceiverFieldInfo lavaSurfacePlaneTransform_gRef = new GuidedRefReceiverFieldInfo(useRecommendedDefaults: true);

	[SerializeField]
	private WaterVolume lavaVolume;

	[SerializeField]
	private GuidedRefReceiverFieldInfo lavaVolume_gRef = new GuidedRefReceiverFieldInfo(useRecommendedDefaults: true);

	[SerializeField]
	private MeshRenderer lavaActivationRenderer;

	[SerializeField]
	private GuidedRefReceiverFieldInfo lavaActivationRenderer_gRef = new GuidedRefReceiverFieldInfo(useRecommendedDefaults: true);

	[SerializeField]
	private Transform lavaActivationStartPos;

	[SerializeField]
	private GuidedRefReceiverFieldInfo lavaActivationStartPos_gRef = new GuidedRefReceiverFieldInfo(useRecommendedDefaults: true);

	[SerializeField]
	private Transform lavaActivationEndPos;

	[SerializeField]
	private GuidedRefReceiverFieldInfo lavaActivationEndPos_gRef = new GuidedRefReceiverFieldInfo(useRecommendedDefaults: true);

	[SerializeField]
	private SlingshotProjectileHitNotifier lavaActivationProjectileHitNotifier;

	[SerializeField]
	private GuidedRefReceiverFieldInfo lavaActivationProjectileHitNotifier_gRef = new GuidedRefReceiverFieldInfo(useRecommendedDefaults: true);

	[SerializeField]
	private VolcanoEffects[] volcanoEffects;

	[SerializeField]
	private GuidedRefReceiverArrayInfo volcanoEffects_gRefs = new GuidedRefReceiverArrayInfo(useRecommendedDefaults: true);

	[DebugReadout]
	private LavaSyncData reliableState;

	private int[] lavaActivationVotePlayerIds = new int[10];

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

	private GorillaSerializerScene networkObject;

	private bool guidedRefsFullyResolved;

	public static InfectionLavaController Instance => instance;

	public bool LavaCurrentlyActivated => reliableState.state != RisingLavaState.Drained;

	public Plane LavaPlane => new Plane(lavaSurfacePlaneTransform.up, lavaSurfacePlaneTransform.position);

	public Vector3 SurfaceCenter => lavaSurfacePlaneTransform.position;

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

	int IGuidedRefReceiverMono.GuidedRefsWaitingToResolveCount { get; set; }

	private void Awake()
	{
		if (instance.IsNotNull())
		{
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		instance = this;
		RoomSystem.LeftRoomEvent += new Action(OnLeftRoom);
		RoomSystem.PlayerLeftEvent += new Action<NetPlayer>(OnPlayerLeftRoom);
		((IGuidedRefObject)this).GuidedRefInitialize();
		if (lavaVolume != null)
		{
			lavaVolume.ColliderEnteredWater += OnColliderEnteredLava;
		}
		if (lavaActivationProjectileHitNotifier != null)
		{
			lavaActivationProjectileHitNotifier.OnProjectileHit += OnActivationLavaProjectileHit;
		}
	}

	protected void OnEnable()
	{
		if (guidedRefsFullyResolved)
		{
			VerifyReferences();
			TickSystem<object>.AddPostTickCallback(this);
		}
	}

	void IGorillaSerializeableScene.OnSceneLinking(GorillaSerializerScene netObj)
	{
		networkObject = netObj;
	}

	protected void OnDisable()
	{
		TickSystem<object>.RemovePostTickCallback(this);
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
		if (instance == this)
		{
			instance = null;
		}
		TickSystem<object>.RemovePostTickCallback(this);
		UpdateLava(0f);
		if (lavaVolume != null)
		{
			lavaVolume.ColliderEnteredWater -= OnColliderEnteredLava;
		}
		if (lavaActivationProjectileHitNotifier != null)
		{
			lavaActivationProjectileHitNotifier.OnProjectileHit -= OnActivationLavaProjectileHit;
		}
	}

	void ITickSystemPost.PostTick()
	{
		prevTime = currentTime;
		currentTime = (NetworkSystem.Instance.InRoom ? NetworkSystem.Instance.SimTime : Time.timeAsDouble);
		if (networkObject.HasAuthority)
		{
			UpdateReliableState(currentTime, ref reliableState);
		}
		UpdateLocalState(currentTime, reliableState);
		localLagLavaProgressOffset = Mathf.MoveTowards(localLagLavaProgressOffset, 0f, lagResolutionLavaProgressPerSecond * Time.deltaTime);
		UpdateLava(lavaProgressSmooth + localLagLavaProgressOffset);
		UpdateVolcanoActivationLava((float)reliableState.activationProgress);
		CheckLocalPlayerAgainstLava(currentTime);
	}

	private void JumpToState(RisingLavaState state)
	{
		reliableState.state = state;
		switch (state)
		{
		case RisingLavaState.Draining:
		{
			for (int j = 0; j < volcanoEffects.Length; j++)
			{
				volcanoEffects[j]?.SetDrainingState();
			}
			break;
		}
		case RisingLavaState.Drained:
		{
			for (int l = 0; l < volcanoEffects.Length; l++)
			{
				volcanoEffects[l]?.SetDrainedState();
			}
			break;
		}
		case RisingLavaState.Erupting:
		{
			for (int m = 0; m < volcanoEffects.Length; m++)
			{
				volcanoEffects[m]?.SetEruptingState();
			}
			break;
		}
		case RisingLavaState.Rising:
		{
			for (int k = 0; k < volcanoEffects.Length; k++)
			{
				volcanoEffects[k]?.SetRisingState();
			}
			break;
		}
		case RisingLavaState.Full:
		{
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
		if (currentTime < syncData.stateStartTime)
		{
			syncData.stateStartTime = currentTime;
		}
		switch (syncData.state)
		{
		case RisingLavaState.Erupting:
			if (currentTime > syncData.stateStartTime + (double)eruptTime)
			{
				syncData.state = RisingLavaState.Rising;
				syncData.stateStartTime = currentTime;
				for (int l = 0; l < volcanoEffects.Length; l++)
				{
					volcanoEffects[l]?.SetRisingState();
				}
			}
			return;
		case RisingLavaState.Rising:
			if (currentTime > syncData.stateStartTime + (double)riseTime)
			{
				syncData.state = RisingLavaState.Full;
				syncData.stateStartTime = currentTime;
				for (int j = 0; j < volcanoEffects.Length; j++)
				{
					volcanoEffects[j]?.SetFullState();
				}
			}
			return;
		case RisingLavaState.Full:
			if (currentTime > syncData.stateStartTime + (double)fullTime)
			{
				syncData.state = RisingLavaState.Draining;
				syncData.stateStartTime = currentTime;
				for (int k = 0; k < volcanoEffects.Length; k++)
				{
					volcanoEffects[k]?.SetDrainingState();
				}
			}
			return;
		case RisingLavaState.Draining:
			syncData.activationProgress = Mathf.MoveTowards((float)syncData.activationProgress, 0f, lavaActivationDrainRateVsPlayerCount.Evaluate(PlayerCount) * Time.deltaTime);
			if (currentTime > syncData.stateStartTime + (double)drainTime)
			{
				syncData.state = RisingLavaState.Drained;
				syncData.stateStartTime = currentTime;
				for (int i = 0; i < volcanoEffects.Length; i++)
				{
					volcanoEffects[i]?.SetDrainedState();
				}
			}
			return;
		}
		if (syncData.activationProgress > 1.0)
		{
			int playerCount = PlayerCount;
			float num = (InCompetitiveQueue ? activationVotePercentageCompetitiveQueue : activationVotePercentageDefaultQueue);
			int num2 = Mathf.RoundToInt((float)playerCount * num);
			if (lavaActivationVoteCount >= num2)
			{
				for (int m = 0; m < lavaActivationVoteCount; m++)
				{
					lavaActivationVotePlayerIds[m] = 0;
				}
				lavaActivationVoteCount = 0;
				syncData.state = RisingLavaState.Erupting;
				syncData.stateStartTime = currentTime;
				syncData.activationProgress = 1.0;
				for (int n = 0; n < volcanoEffects.Length; n++)
				{
					volcanoEffects[n]?.SetEruptingState();
				}
			}
			return;
		}
		float num3 = Mathf.Clamp((float)(currentTime - prevTime), 0f, 0.1f);
		double activationProgress = syncData.activationProgress;
		syncData.activationProgress = Mathf.MoveTowards((float)syncData.activationProgress, 0f, lavaActivationDrainRateVsPlayerCount.Evaluate(PlayerCount) * num3);
		if (activationProgress > 0.0 && syncData.activationProgress <= double.Epsilon)
		{
			VolcanoEffects[] array = volcanoEffects;
			for (int num4 = 0; num4 < array.Length; num4++)
			{
				array[num4].OnVolcanoBellyEmpty();
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
			float num4 = (float)(currentTime - syncData.stateStartTime);
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
			float value = (float)(currentTime - syncData.stateStartTime) / riseTime;
			lavaProgressLinear = Mathf.Clamp01(value);
			lavaProgressSmooth = lavaProgressAnimationCurve.Evaluate(lavaProgressLinear);
			float num5 = (float)(currentTime - syncData.stateStartTime);
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
			float num3 = (float)(currentTime - syncData.stateStartTime);
			float progress = Mathf.Clamp01(fullTime / num3);
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
			float num = (float)(currentTime - syncData.stateStartTime);
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
		float time = (float)(currentTime - syncData.stateStartTime);
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

	private void UpdateVolcanoActivationLava(float activationProgress)
	{
		activationProgessSmooth = Mathf.MoveTowards(activationProgessSmooth, activationProgress, lavaActivationVisualMovementProgressPerSecond * Time.deltaTime);
		lavaActivationRenderer.material.SetColor(ShaderProps._BaseColor, lavaActivationGradient.Evaluate(activationProgress));
		lavaActivationRenderer.transform.position = Vector3.Lerp(lavaActivationStartPos.position, lavaActivationEndPos.position, activationProgessSmooth);
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
		GorillaGameManager gorillaGameManager = GorillaGameManager.instance;
		if (gorillaGameManager != null && gorillaGameManager.CanAffectPlayer(NetworkSystem.Instance.LocalPlayer, enteredLavaThisFrame) && (currentTime - lastTagSelfRPCTime > 0.5 || enteredLavaThisFrame))
		{
			lastTagSelfRPCTime = currentTime;
			GameMode.ReportHit();
		}
	}

	public void OnActivationLavaProjectileHit(SlingshotProjectile projectile, Collision collision)
	{
		if (projectile.gameObject.CompareTag("LavaRockProjectile"))
		{
			AddLavaRock(projectile.projectileOwner.ActorNumber);
		}
	}

	private void AddLavaRock(int playerId)
	{
		if (networkObject.HasAuthority && reliableState.state == RisingLavaState.Drained)
		{
			float num = lavaActivationRockProgressVsPlayerCount.Evaluate(PlayerCount);
			reliableState.activationProgress += num;
			AddVoteForVolcanoActivation(playerId);
			VolcanoEffects[] array = volcanoEffects;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].OnStoneAccepted(reliableState.activationProgress);
			}
		}
	}

	private void AddVoteForVolcanoActivation(int playerId)
	{
		if (!networkObject.HasAuthority || lavaActivationVoteCount >= 10)
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
		if (!networkObject.HasAuthority)
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

	void IGorillaSerializeable.OnSerializeWrite(PhotonStream stream, PhotonMessageInfo info)
	{
		stream.SendNext((int)reliableState.state);
		stream.SendNext(reliableState.stateStartTime);
		stream.SendNext(reliableState.activationProgress);
		stream.SendNext(lavaActivationVoteCount);
		stream.SendNext(lavaActivationVotePlayerIds[0]);
		stream.SendNext(lavaActivationVotePlayerIds[1]);
		stream.SendNext(lavaActivationVotePlayerIds[2]);
		stream.SendNext(lavaActivationVotePlayerIds[3]);
		stream.SendNext(lavaActivationVotePlayerIds[4]);
		stream.SendNext(lavaActivationVotePlayerIds[5]);
		stream.SendNext(lavaActivationVotePlayerIds[6]);
		stream.SendNext(lavaActivationVotePlayerIds[7]);
		stream.SendNext(lavaActivationVotePlayerIds[8]);
		stream.SendNext(lavaActivationVotePlayerIds[9]);
	}

	void IGorillaSerializeable.OnSerializeRead(PhotonStream stream, PhotonMessageInfo info)
	{
		RisingLavaState risingLavaState = (RisingLavaState)(int)stream.ReceiveNext();
		reliableState.stateStartTime = ((double)stream.ReceiveNext()).GetFinite();
		reliableState.activationProgress = ((double)stream.ReceiveNext()).ClampSafe(0.0, 2.0);
		lavaActivationVoteCount = (int)stream.ReceiveNext();
		lavaActivationVotePlayerIds[0] = (int)stream.ReceiveNext();
		lavaActivationVotePlayerIds[1] = (int)stream.ReceiveNext();
		lavaActivationVotePlayerIds[2] = (int)stream.ReceiveNext();
		lavaActivationVotePlayerIds[3] = (int)stream.ReceiveNext();
		lavaActivationVotePlayerIds[4] = (int)stream.ReceiveNext();
		lavaActivationVotePlayerIds[5] = (int)stream.ReceiveNext();
		lavaActivationVotePlayerIds[6] = (int)stream.ReceiveNext();
		lavaActivationVotePlayerIds[7] = (int)stream.ReceiveNext();
		lavaActivationVotePlayerIds[8] = (int)stream.ReceiveNext();
		lavaActivationVotePlayerIds[9] = (int)stream.ReceiveNext();
		float num = lavaProgressSmooth;
		if (risingLavaState != reliableState.state)
		{
			JumpToState(risingLavaState);
		}
		UpdateLocalState((float)NetworkSystem.Instance.SimTime, reliableState);
		localLagLavaProgressOffset = num - lavaProgressSmooth;
	}

	public void OnPlayerLeftRoom(NetPlayer otherNetPlayer)
	{
		RemoveVoteForVolcanoActivation(otherNetPlayer.ActorNumber);
	}

	private void OnLeftRoom()
	{
		for (int i = 0; i < lavaActivationVotePlayerIds.Length; i++)
		{
			if (lavaActivationVotePlayerIds[i] != NetworkSystem.Instance.LocalPlayerID)
			{
				RemoveVoteForVolcanoActivation(lavaActivationVotePlayerIds[i]);
			}
		}
	}

	void IGorillaSerializeableScene.OnNetworkObjectDisable()
	{
	}

	void IGorillaSerializeableScene.OnNetworkObjectEnable()
	{
	}

	void IGuidedRefReceiverMono.OnAllGuidedRefsResolved()
	{
		guidedRefsFullyResolved = true;
		VerifyReferences();
		TickSystem<object>.AddPostTickCallback(this);
	}

	public void OnGuidedRefTargetDestroyed(int fieldId)
	{
		guidedRefsFullyResolved = false;
		TickSystem<object>.RemovePostTickCallback(this);
	}

	void IGuidedRefObject.GuidedRefInitialize()
	{
		GuidedRefHub.RegisterReceiverField(this, "lavaMeshTransform_gRef", ref lavaMeshTransform_gRef);
		GuidedRefHub.RegisterReceiverField(this, "lavaSurfacePlaneTransform_gRef", ref lavaSurfacePlaneTransform_gRef);
		GuidedRefHub.RegisterReceiverField(this, "lavaVolume_gRef", ref lavaVolume_gRef);
		GuidedRefHub.RegisterReceiverField(this, "lavaActivationRenderer_gRef", ref lavaActivationRenderer_gRef);
		GuidedRefHub.RegisterReceiverField(this, "lavaActivationStartPos_gRef", ref lavaActivationStartPos_gRef);
		GuidedRefHub.RegisterReceiverField(this, "lavaActivationEndPos_gRef", ref lavaActivationEndPos_gRef);
		GuidedRefHub.RegisterReceiverField(this, "lavaActivationProjectileHitNotifier_gRef", ref lavaActivationProjectileHitNotifier_gRef);
		GuidedRefHub.RegisterReceiverArray(this, "volcanoEffects_gRefs", ref volcanoEffects, ref volcanoEffects_gRefs);
		GuidedRefHub.ReceiverFullyRegistered(this);
	}

	bool IGuidedRefReceiverMono.GuidedRefTryResolveReference(GuidedRefTryResolveInfo target)
	{
		if (!GuidedRefHub.TryResolveField(this, ref lavaMeshTransform, lavaMeshTransform_gRef, target) && !GuidedRefHub.TryResolveField(this, ref lavaSurfacePlaneTransform, lavaSurfacePlaneTransform_gRef, target) && !GuidedRefHub.TryResolveField(this, ref lavaVolume, lavaVolume_gRef, target) && !GuidedRefHub.TryResolveField(this, ref lavaActivationRenderer, lavaActivationRenderer_gRef, target) && !GuidedRefHub.TryResolveField(this, ref lavaActivationStartPos, lavaActivationStartPos_gRef, target) && !GuidedRefHub.TryResolveField(this, ref lavaActivationEndPos, lavaActivationEndPos_gRef, target) && !GuidedRefHub.TryResolveField(this, ref lavaActivationProjectileHitNotifier, lavaActivationProjectileHitNotifier_gRef, target))
		{
			return GuidedRefHub.TryResolveArrayItem(this, volcanoEffects, volcanoEffects_gRefs, target);
		}
		return true;
	}

	Transform IGuidedRefMonoBehaviour.get_transform()
	{
		return base.transform;
	}

	int IGuidedRefObject.GetInstanceID()
	{
		return GetInstanceID();
	}
}

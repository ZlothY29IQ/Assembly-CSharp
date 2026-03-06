using System;
using System.Collections.Generic;
using GorillaExtensions;
using UnityEngine;

public class GrowingSnowballThrowable : SnowballThrowable
{
	[Serializable]
	public struct SizeParameters
	{
		public float snowballScale;

		public float impactEffectScale;

		public float impactSoundVolume;

		public float impactSoundPitch;

		public float throwSpeedMultiplier;

		public float gravityMultiplier;

		public SlingshotProjectile.AOEKnockbackConfig aoeKnockbackConfig;
	}

	private struct AOERangeDebugDraw
	{
		public float impactTime;

		public Vector3 position;

		public float innerRadius;

		public float outerRadius;
	}

	public Transform snowballModelParentTransform;

	public Transform snowballModelTransform;

	public Vector3 modelParentOffset = Vector3.zero;

	public Vector3 modelOffset = Vector3.zero;

	public float modelRadius = 0.055f;

	[Tooltip("Snowballs will combine into the larger snowball unless they are moving faster than this threshold.Then the faster moving snowball will go in to the more stationary hand")]
	public float combineBasedOnSpeedThreshold = 0.5f;

	public SoundBankPlayer sizeIncreaseSoundBankPlayer;

	public List<SizeParameters> snowballSizeLevels = new List<SizeParameters>();

	private int sizeLevel;

	private float maintainSizeLevelUntilLocalTime;

	private PhotonEvent changeSizeEvent;

	private PhotonEvent snowballThrowEvent;

	[HideInInspector]
	public static bool debugDrawAOERange = false;

	[HideInInspector]
	public static bool twoHandedSnowballGrowing = true;

	private Queue<AOERangeDebugDraw> aoeRangeDebugDrawQueue = new Queue<AOERangeDebugDraw>();

	private GrowingSnowballThrowable otherHandSnowball;

	private float debugDrawAOERangeTime = 1.5f;

	public int SizeLevel => sizeLevel;

	public int MaxSizeLevel => Mathf.Max(snowballSizeLevels.Count - 1, 0);

	public float CurrentSnowballRadius
	{
		get
		{
			if (snowballSizeLevels.Count > 0 && sizeLevel > -1 && sizeLevel < snowballSizeLevels.Count)
			{
				return snowballSizeLevels[sizeLevel].snowballScale * modelRadius * base.transform.lossyScale.x;
			}
			return modelRadius * base.transform.lossyScale.x;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		if (NetworkSystem.Instance != null)
		{
			NetworkSystem.Instance.OnMultiplayerStarted += new Action(StartedMultiplayerSession);
		}
		else
		{
			Debug.LogError("NetworkSystem.Instance was null in SnowballThrowable Awake");
		}
		VRRigCache.OnRigActivated += VRRigActivated;
		VRRigCache.OnRigDeactivated += VRRigDeactivated;
	}

	public override void OnEnable()
	{
		base.OnEnable();
		snowballModelParentTransform.localPosition = modelParentOffset;
		snowballModelTransform.localPosition = modelOffset;
		otherHandSnowball = (isLeftHanded ? (EquipmentInteractor.instance.rightHandHeldEquipment as GrowingSnowballThrowable) : (EquipmentInteractor.instance.leftHandHeldEquipment as GrowingSnowballThrowable));
		if (Time.time > maintainSizeLevelUntilLocalTime)
		{
			SetSizeLevelLocal(0);
		}
		CreatePhotonEventsIfNull();
	}

	protected override void OnDestroy()
	{
		DestroyPhotonEvents();
	}

	private void VRRigActivated(RigContainer rigContainer)
	{
		targetRig = GetComponentInParent<VRRig>(includeInactive: true);
		isOfflineRig = targetRig != null && targetRig.isOfflineVRRig;
		if (rigContainer.Rig == targetRig)
		{
			CreatePhotonEventsIfNull();
		}
	}

	private void VRRigDeactivated(RigContainer rigContainer)
	{
		if (rigContainer.Rig == targetRig)
		{
			DestroyPhotonEvents();
		}
	}

	private void StartedMultiplayerSession()
	{
		targetRig = GetComponentInParent<VRRig>(includeInactive: true);
		isOfflineRig = targetRig != null && targetRig.isOfflineVRRig;
		if (isOfflineRig)
		{
			DestroyPhotonEvents();
			CreatePhotonEventsIfNull();
		}
	}

	private void CreatePhotonEventsIfNull()
	{
		if (targetRig == null)
		{
			targetRig = GetComponentInParent<VRRig>(includeInactive: true);
			isOfflineRig = targetRig != null && targetRig.isOfflineVRRig;
		}
		if (!(targetRig == null) && !(targetRig.netView == null))
		{
			if (changeSizeEvent == null)
			{
				_ = "SnowballThrowable" + base.gameObject.name + (isLeftHanded ? "ChangeSizeEventLeft" : "ChangeSizeEventRight") + targetRig.netView.ViewID;
				int eventId = StaticHash.Compute("SnowballThrowable", base.gameObject.name, isLeftHanded ? "ChangeSizeEventLeft" : "ChangeSizeEventRight", targetRig.netView.ViewID.ToString());
				changeSizeEvent = new PhotonEvent(eventId);
				changeSizeEvent.reliable = true;
				changeSizeEvent += new Action<int, int, object[], PhotonMessageInfoWrapped>(ChangeSizeEventReceiver);
			}
			if (snowballThrowEvent == null)
			{
				_ = "SnowballThrowable" + base.gameObject.name + (isLeftHanded ? "SnowballThrowEventLeft" : "SnowballThrowEventRight") + targetRig.netView.ViewID;
				int eventId2 = StaticHash.Compute("SnowballThrowable", base.gameObject.name, isLeftHanded ? "SnowballThrowEventLeft" : "SnowballThrowEventRight", targetRig.netView.ViewID.ToString());
				snowballThrowEvent = new PhotonEvent(eventId2);
				snowballThrowEvent.reliable = true;
				snowballThrowEvent += new Action<int, int, object[], PhotonMessageInfoWrapped>(SnowballThrowEventReceiver);
			}
		}
	}

	private void DestroyPhotonEvents()
	{
		if (changeSizeEvent != null)
		{
			changeSizeEvent -= new Action<int, int, object[], PhotonMessageInfoWrapped>(ChangeSizeEventReceiver);
			changeSizeEvent.Dispose();
			changeSizeEvent = null;
		}
		if (snowballThrowEvent != null)
		{
			snowballThrowEvent -= new Action<int, int, object[], PhotonMessageInfoWrapped>(SnowballThrowEventReceiver);
			snowballThrowEvent.Dispose();
			snowballThrowEvent = null;
		}
	}

	public void IncreaseSize(int increase)
	{
		SetSizeLevelAuthority(sizeLevel + increase);
	}

	private void SetSizeLevelAuthority(int sizeLevel)
	{
		if (targetRig != null && targetRig.creator != null && targetRig.creator.IsLocal)
		{
			int validSizeLevel = GetValidSizeLevel(sizeLevel);
			if (validSizeLevel > this.sizeLevel)
			{
				sizeIncreaseSoundBankPlayer.Play();
			}
			SetSizeLevelLocal(validSizeLevel);
			changeSizeEvent?.RaiseOthers(validSizeLevel);
		}
	}

	private int GetValidSizeLevel(int inputSizeLevel)
	{
		int max = Mathf.Max(snowballSizeLevels.Count - 1, 0);
		return Mathf.Clamp(inputSizeLevel, 0, max);
	}

	private void SetSizeLevelLocal(int sizeLevel)
	{
		int validSizeLevel = GetValidSizeLevel(sizeLevel);
		if (validSizeLevel >= 0 && validSizeLevel != this.sizeLevel)
		{
			this.sizeLevel = validSizeLevel;
			snowballModelParentTransform.localScale = Vector3.one * snowballSizeLevels[this.sizeLevel].snowballScale;
		}
	}

	private void ChangeSizeEventReceiver(int sender, int receiver, object[] args, PhotonMessageInfoWrapped info)
	{
		if (sender != receiver || args == null || args.Length < 1)
		{
			return;
		}
		int num = ((targetRig != null && targetRig.gameObject.activeInHierarchy && targetRig.netView != null && targetRig.netView.Owner != null) ? targetRig.netView.Owner.ActorNumber : (-1));
		if (info.senderID == num)
		{
			MonkeAgent.IncrementRPCCall(info, "ChangeSizeEventReceiver");
			int num2 = (int)args[0];
			if (GetValidSizeLevel(num2) > sizeLevel && sizeIncreaseSoundBankPlayer.gameObject.activeInHierarchy)
			{
				sizeIncreaseSoundBankPlayer.Play();
			}
			SetSizeLevelLocal(num2);
			if (!base.gameObject.activeSelf)
			{
				maintainSizeLevelUntilLocalTime = Time.time + 0.1f;
			}
		}
	}

	private void SnowballThrowEventReceiver(int sender, int receiver, object[] args, PhotonMessageInfoWrapped info)
	{
		if (sender != receiver || args == null || args.Length < 3 || targetRig.IsNull() || !targetRig.gameObject.activeSelf)
		{
			return;
		}
		_ = targetRig.creator;
		if (info.senderID != targetRig.creator.ActorNumber)
		{
			return;
		}
		MonkeAgent.IncrementRPCCall(info, "SnowballThrowEventReceiver");
		if (FXSystem.CheckCallSpam(targetRig.fxSettings, 4, info.SentServerTime) && args[0] is Vector3 v && args[1] is Vector3 inVel && args[2] is int index)
		{
			Vector3 velocity = targetRig.ClampVelocityRelativeToPlayerSafe(inVel, 50f);
			float x = snowballModelTransform.lossyScale.x;
			if (v.IsValid(10000f) && targetRig.IsPositionInRange(v, 4f))
			{
				LaunchSnowballRemote(v, velocity, x, index, info);
			}
		}
	}

	protected override void LateUpdateLocal()
	{
		base.LateUpdateLocal();
		if (!twoHandedSnowballGrowing)
		{
			return;
		}
		if (otherHandSnowball != null && otherHandSnowball.isActiveAndEnabled)
		{
			IHoldableObject holdableObject = (isLeftHanded ? EquipmentInteractor.instance.rightHandHeldEquipment : EquipmentInteractor.instance.leftHandHeldEquipment);
			if (holdableObject != null && otherHandSnowball != (GrowingSnowballThrowable)holdableObject)
			{
				otherHandSnowball = null;
				return;
			}
			float num = otherHandSnowball.CurrentSnowballRadius + CurrentSnowballRadius;
			if (SizeLevel < MaxSizeLevel && otherHandSnowball.SizeLevel < otherHandSnowball.MaxSizeLevel && (otherHandSnowball.snowballModelTransform.position - snowballModelTransform.position).sqrMagnitude < num * num)
			{
				int num2 = SizeLevel - otherHandSnowball.SizeLevel;
				float magnitude = velocityEstimator.linearVelocity.magnitude;
				float magnitude2 = otherHandSnowball.velocityEstimator.linearVelocity.magnitude;
				bool flag = false;
				if ((!(Mathf.Abs(magnitude - magnitude2) > combineBasedOnSpeedThreshold) && num2 != 0) ? (num2 < 0) : (magnitude > magnitude2))
				{
					otherHandSnowball.IncreaseSize(sizeLevel + 1);
					GorillaTagger.Instance.StartVibration(!isLeftHanded, GorillaTagger.Instance.tapHapticStrength * 0.5f, GorillaTagger.Instance.tapHapticDuration * 0.5f);
					SetSnowballActiveLocal(enabled: false);
				}
				else
				{
					IncreaseSize(otherHandSnowball.SizeLevel + 1);
					GorillaTagger.Instance.StartVibration(isLeftHanded, GorillaTagger.Instance.tapHapticStrength * 0.5f, GorillaTagger.Instance.tapHapticDuration * 0.5f);
					otherHandSnowball.SetSnowballActiveLocal(enabled: false);
				}
			}
		}
		else
		{
			otherHandSnowball = null;
		}
	}

	protected override void OnSnowballRelease()
	{
		if (base.isActiveAndEnabled)
		{
			PerformSnowballThrowAuthority();
		}
	}

	protected override void PerformSnowballThrowAuthority()
	{
		if (targetRig != null && targetRig.creator != null && targetRig.creator.IsLocal)
		{
			Vector3 vector = Vector3.zero;
			Rigidbody component = GorillaTagger.Instance.GetComponent<Rigidbody>();
			if (component != null)
			{
				vector = component.linearVelocity;
			}
			Vector3 vector2 = velocityEstimator.linearVelocity - vector;
			float magnitude = vector2.magnitude;
			if (magnitude > 0.001f)
			{
				float num = Mathf.Clamp(magnitude * linSpeedMultiplier, 0f, maxLinSpeed);
				vector2 *= num / magnitude;
			}
			Vector3 vector3 = vector2 + vector;
			targetRig.GetThrowableProjectileColor(isLeftHanded);
			Transform obj = snowballModelTransform;
			Vector3 position = obj.position;
			float x = obj.lossyScale.x;
			SlingshotProjectile slingshotProjectile = LaunchSnowballLocal(position, vector3, x);
			SetSnowballActiveLocal(enabled: false);
			if (randModelIndex > -1 && randModelIndex < localModels.Count && localModels[randModelIndex].destroyAfterRelease)
			{
				slingshotProjectile.DestroyAfterRelease();
			}
			snowballThrowEvent?.RaiseOthers(position, vector3, slingshotProjectile.myProjectileCount);
		}
	}

	protected virtual SlingshotProjectile LaunchSnowballLocal(Vector3 location, Vector3 velocity, float scale)
	{
		return LaunchSnowballLocal(location, velocity, scale, false, Color.white);
	}

	protected override SlingshotProjectile LaunchSnowballLocal(Vector3 location, Vector3 velocity, float scale, bool randomizeColour, Color colour)
	{
		SlingshotProjectile slingshotProjectile = SpawnGrowingSnowball(ref velocity, scale);
		slingshotProjectile.Launch(projectileCount: ProjectileTracker.AddAndIncrementLocalProjectile(slingshotProjectile, velocity, location, scale), position: location, velocity: velocity, player: NetworkSystem.Instance.LocalPlayer, blueTeam: false, orangeTeam: false, scale: scale, shouldOverrideColor: randomizeColour, overrideColor: colour);
		if (string.IsNullOrEmpty(throwEventName))
		{
			PlayerGameEvents.LaunchedProjectile(projectilePrefab.name);
		}
		else
		{
			PlayerGameEvents.LaunchedProjectile(throwEventName);
		}
		slingshotProjectile.OnImpact += OnProjectileImpact;
		return slingshotProjectile;
	}

	protected virtual SlingshotProjectile LaunchSnowballRemote(Vector3 location, Vector3 velocity, float scale, int index, PhotonMessageInfoWrapped info)
	{
		return LaunchSnowballRemote(location, velocity, scale, index, randomizeColour: false, Color.white, info);
	}

	protected virtual SlingshotProjectile LaunchSnowballRemote(Vector3 location, Vector3 velocity, float scale, int index, bool randomizeColour, Color colour, PhotonMessageInfoWrapped info)
	{
		SlingshotProjectile slingshotProjectile = SpawnGrowingSnowball(ref velocity, scale);
		ProjectileTracker.AddRemotePlayerProjectile(info.Sender, slingshotProjectile, index, info.SentServerTime, velocity, location, scale);
		slingshotProjectile.Launch(location, velocity, info.Sender, blueTeam: false, orangeTeam: false, index, scale, randomizeColour, Color.white);
		if (string.IsNullOrEmpty(throwEventName))
		{
			PlayerGameEvents.LaunchedProjectile(projectilePrefab.name);
		}
		else
		{
			PlayerGameEvents.LaunchedProjectile(throwEventName);
		}
		slingshotProjectile.OnImpact += OnProjectileImpact;
		return slingshotProjectile;
	}

	private SlingshotProjectile SpawnGrowingSnowball(ref Vector3 velocity, float scale)
	{
		SlingshotProjectile component = ObjectPools.instance.Instantiate(randomModelSelection ? localModels[randModelIndex].projectilePrefab : projectilePrefab).GetComponent<SlingshotProjectile>();
		if (snowballSizeLevels.Count > 0 && sizeLevel >= 0 && sizeLevel < snowballSizeLevels.Count)
		{
			float num = scale / snowballSizeLevels[sizeLevel].snowballScale;
			SlingshotProjectile.AOEKnockbackConfig aoeKnockbackConfig = snowballSizeLevels[sizeLevel].aoeKnockbackConfig;
			aoeKnockbackConfig.aeoInnerRadius *= num;
			aoeKnockbackConfig.aeoOuterRadius *= num;
			aoeKnockbackConfig.knockbackVelocity *= num;
			aoeKnockbackConfig.impactVelocityThreshold *= num;
			velocity *= snowballSizeLevels[sizeLevel].throwSpeedMultiplier;
			component.gravityMultiplier = snowballSizeLevels[sizeLevel].gravityMultiplier;
			component.impactEffectScaleMultiplier = snowballSizeLevels[sizeLevel].impactEffectScale;
			component.aoeKnockbackConfig = aoeKnockbackConfig;
			component.impactSoundVolumeOverride = snowballSizeLevels[sizeLevel].impactSoundVolume;
			component.impactSoundPitchOverride = snowballSizeLevels[sizeLevel].impactSoundPitch;
		}
		return component;
	}

	public override void OnGrab(InteractionPoint pointGrabbed, GameObject grabbingHand)
	{
		if (targetRig != null && targetRig.creator != null && targetRig.creator.IsLocal && ((isLeftHanded && grabbingHand == EquipmentInteractor.instance.rightHand && EquipmentInteractor.instance.rightHandHeldEquipment == null) || (!isLeftHanded && grabbingHand == EquipmentInteractor.instance.leftHand && EquipmentInteractor.instance.leftHandHeldEquipment == null)) && (isLeftHanded ? SnowballMaker.rightHandInstance : SnowballMaker.leftHandInstance).TryCreateSnowball(matDataIndexes[0], out var result))
		{
			GrowingSnowballThrowable growingSnowballThrowable = result as GrowingSnowballThrowable;
			if (growingSnowballThrowable != null)
			{
				growingSnowballThrowable.IncreaseSize(sizeLevel);
				GorillaTagger.Instance.StartVibration(!isLeftHanded, GorillaTagger.Instance.tapHapticStrength * 0.5f, GorillaTagger.Instance.tapHapticDuration * 0.5f);
				SetSnowballActiveLocal(enabled: false);
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GorillaExtensions;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Serialization;

public class CrittersPawn : CrittersActor, IEyeScannable
{
	public enum CreatureState
	{
		Idle,
		Eating,
		AttractedTo,
		Running,
		Grabbed,
		Sleeping,
		SeekingFood,
		Captured,
		Stunned,
		WaitingToDespawn,
		Despawning,
		Spawning
	}

	internal struct CreatureUpdateData(CrittersPawn creature)
	{
		private double lastImpulseTime = creature.lastImpulseTime;

		private CreatureState state = creature.currentState;

		internal bool SameData(CrittersPawn creature)
		{
			if (lastImpulseTime == creature.lastImpulseTime)
			{
				return state == creature.currentState;
			}
			return false;
		}
	}

	[NonSerialized]
	public CritterConfiguration creatureConfiguration;

	public Collider bodyCollider;

	[NonSerialized]
	[HideInInspector]
	public float maxJumpVel;

	[NonSerialized]
	[HideInInspector]
	public float jumpCooldown;

	[NonSerialized]
	[HideInInspector]
	public float scaredJumpCooldown;

	[NonSerialized]
	[HideInInspector]
	public float jumpVariabilityTime;

	[NonSerialized]
	[HideInInspector]
	public float visionConeAngle;

	[NonSerialized]
	[HideInInspector]
	public float sensoryRange;

	[NonSerialized]
	[HideInInspector]
	public float maxHunger;

	[NonSerialized]
	[HideInInspector]
	public float hungryThreshold;

	[NonSerialized]
	[HideInInspector]
	public float satiatedThreshold;

	[NonSerialized]
	[HideInInspector]
	public float hungerLostPerSecond;

	[NonSerialized]
	[HideInInspector]
	public float hungerGainedPerSecond;

	[NonSerialized]
	[HideInInspector]
	public float maxFear;

	[NonSerialized]
	[HideInInspector]
	public float scaredThreshold;

	[NonSerialized]
	[HideInInspector]
	public float calmThreshold;

	[NonSerialized]
	[HideInInspector]
	public float fearLostPerSecond;

	[NonSerialized]
	public float maxAttraction;

	[NonSerialized]
	public float attractedThreshold;

	[NonSerialized]
	public float unattractedThreshold;

	[NonSerialized]
	public float attractionLostPerSecond;

	[NonSerialized]
	[HideInInspector]
	public float maxSleepiness;

	[NonSerialized]
	[HideInInspector]
	public float tiredThreshold;

	[NonSerialized]
	[HideInInspector]
	public float awakeThreshold;

	[NonSerialized]
	[HideInInspector]
	public float sleepinessGainedPerSecond;

	[NonSerialized]
	[HideInInspector]
	public float sleepinessLostPerSecond;

	[NonSerialized]
	[HideInInspector]
	public float maxStruggle;

	[NonSerialized]
	[HideInInspector]
	public float escapeThreshold;

	[NonSerialized]
	[HideInInspector]
	public float catchableThreshold;

	[NonSerialized]
	[HideInInspector]
	public float struggleGainedPerSecond;

	[NonSerialized]
	[HideInInspector]
	public float struggleLostPerSecond;

	public List<crittersAttractorStruct> attractedToList;

	public List<crittersAttractorStruct> afraidOfList;

	public Dictionary<CrittersActorType, float> afraidOfTypes;

	public Dictionary<CrittersActorType, float> attractedToTypes;

	private Rigidbody rB;

	[NonSerialized]
	public CreatureState currentState;

	[NonSerialized]
	public float currentHunger;

	[NonSerialized]
	public float currentFear;

	[NonSerialized]
	public float currentAttraction;

	[NonSerialized]
	public float currentSleepiness;

	[NonSerialized]
	public float currentStruggle;

	public double lifeTime = 10.0;

	public double lifeTimeStart;

	private CrittersFood eatingTarget;

	private CrittersActor fearTarget;

	private CrittersActor attractionTarget;

	private Vector3 lastSeenFearPosition;

	private Vector3 lastSeenAttractionPosition;

	private CrittersGrabber grabbedTarget;

	private CrittersCage cageTarget;

	private int actorIdTarget;

	[FormerlySerializedAs("eatingRadiusMax")]
	public float eatingRadiusMaxSquared;

	private bool withinEatingRadius;

	public Transform animTarget;

	public MeshRenderer myRenderer;

	public float autoSeeFoodDistance;

	public Dictionary<int, CrittersActor> soundsHeard;

	public float fudge = 1.1f;

	public float obstacleSeeDistance = 0.25f;

	private RaycastHit[] raycastHits;

	private bool canJump;

	private bool wasSomethingInTheWay;

	public Transform hat;

	private int LastTemplateIndex = -1;

	private int TemplateIndex = -1;

	private double _nextDespawnCheck;

	private double _nextStuckCheck;

	public float killHeight = -500f;

	private float remainingStunnedTime;

	private float remainingSlowedTime;

	private float slowSpeedMod = 1f;

	[Header("Visuals")]
	public CritterVisuals visuals;

	[HideInInspector]
	public Dictionary<CreatureState, GameObject> StartStateFX = new Dictionary<CreatureState, GameObject>();

	[HideInInspector]
	public Dictionary<CreatureState, GameObject> OngoingStateFX = new Dictionary<CreatureState, GameObject>();

	[NonSerialized]
	public GameObject OnReleasedFX;

	private GameObject currentOngoingStateFX;

	[HideInInspector]
	public Dictionary<CreatureState, CrittersAnim> stateAnim = new Dictionary<CreatureState, CrittersAnim>();

	private CrittersAnim currentAnim;

	private float currentAnimTime;

	public AudioClip grabbedHaptics;

	public float grabbedHapticsStrength;

	public AnimationCurve spawnInHeighMovement;

	public AnimationCurve despawnInHeighMovement;

	private Vector3 spawningStartingPosition;

	private double spawnStartTime;

	private double despawnStartTime;

	private float _spawnAnimationDuration;

	private float _despawnAnimationDuration;

	private double _spawnAnimTime;

	private double _despawnAnimTime;

	public MeshRenderer debugStateIndicator;

	public Color debugColorIdle;

	public Color debugColorSeekingFood;

	public Color debugColorEating;

	public Color debugColorScared;

	public Color debugColorSleeping;

	public Color debugColorCaught;

	public Color debugColorCaged;

	public Color debugColorStunned;

	public Color debugColorAttracted;

	[NonSerialized]
	public int regionId;

	private KeyValueStringPair[] eyeScanData = new KeyValueStringPair[6];

	int IEyeScannable.scannableId => base.gameObject.GetInstanceID();

	Vector3 IEyeScannable.Position => bodyCollider.bounds.center;

	Bounds IEyeScannable.Bounds => bodyCollider.bounds;

	IList<KeyValueStringPair> IEyeScannable.Entries => BuildEyeScannerData();

	public event Action OnDataChange;

	public override void Initialize()
	{
		base.Initialize();
		rB = GetComponentInChildren<Rigidbody>();
		soundsHeard = new Dictionary<int, CrittersActor>();
		base.transform.eulerAngles = new Vector3(0f, UnityEngine.Random.value * 360f, 0f);
		raycastHits = new RaycastHit[20];
		wasSomethingInTheWay = false;
		_spawnAnimationDuration = spawnInHeighMovement.keys.Last().time;
		_despawnAnimationDuration = despawnInHeighMovement.keys.Last().time;
	}

	private void InitializeTemplateValues()
	{
		sensoryRange *= sensoryRange;
		autoSeeFoodDistance *= autoSeeFoodDistance;
		currentSleepiness = UnityEngine.Random.value * tiredThreshold;
		currentHunger = UnityEngine.Random.value * hungryThreshold;
		currentFear = 0f;
		currentStruggle = 0f;
		currentAttraction = 0f;
	}

	public float JumpVelocityForDistanceAtAngle(float horizontalDistance, float angle)
	{
		return Mathf.Min(maxJumpVel, Mathf.Sqrt(horizontalDistance * Physics.gravity.magnitude / Mathf.Sin(2f * angle)));
	}

	public override void OnEnable()
	{
		base.OnEnable();
		CrittersManager.RegisterCritter(this);
		lifeTimeStart = (PhotonNetwork.InRoom ? PhotonNetwork.Time : ((double)Time.time));
		EyeScannerMono.Register(this);
	}

	public override void OnDisable()
	{
		base.OnDisable();
		CrittersManager.DeregisterCritter(this);
		if (currentOngoingStateFX.IsNotNull())
		{
			currentOngoingStateFX.SetActive(value: false);
			currentOngoingStateFX = null;
		}
		EyeScannerMono.Unregister(this);
	}

	private float GetAdditiveJumpDelay()
	{
		if (currentState == CreatureState.Running)
		{
			return 0f;
		}
		return Mathf.Max(0f, jumpCooldown * UnityEngine.Random.value * jumpVariabilityTime);
	}

	public void LocalJump(float maxVel, float jumpAngle)
	{
		maxVel *= slowSpeedMod;
		lastImpulsePosition = base.transform.position;
		lastImpulseVelocity = base.transform.forward * (Mathf.Sin(MathF.PI / 180f * jumpAngle) * maxVel) + Vector3.up * (Mathf.Cos(MathF.PI / 180f * jumpAngle) * maxVel);
		lastImpulseTime = (PhotonNetwork.InRoom ? PhotonNetwork.Time : ((double)Time.time));
		lastImpulseTime += GetAdditiveJumpDelay();
		lastImpulseQuaternion = base.transform.rotation;
		rB.linearVelocity = lastImpulseVelocity;
		rb.angularVelocity = Vector3.zero;
	}

	private bool CanSeeActor(Vector3 actorPosition)
	{
		Vector3 to = actorPosition - base.transform.position;
		if (!(to.sqrMagnitude < autoSeeFoodDistance))
		{
			if (to.sqrMagnitude < sensoryRange)
			{
				return Vector3.Angle(base.transform.forward, to) < visionConeAngle;
			}
			return false;
		}
		return true;
	}

	private bool IsGrabPossible(CrittersGrabber actor)
	{
		if (actor.grabbing)
		{
			return (base.transform.position - actor.grabPosition.position).magnitude < actor.grabDistance;
		}
		return false;
	}

	private bool WithinCaptureDistance(CrittersCage actor)
	{
		return (bodyCollider.bounds.center - actor.grabPosition.position).magnitude < actor.grabDistance;
	}

	public bool AwareOfActor(CrittersActor actor)
	{
		switch (actor.crittersActorType)
		{
		case CrittersActorType.Food:
			if (((CrittersFood)actor).currentFood > 0f)
			{
				return CanSeeActor(((CrittersFood)actor).food.transform.position);
			}
			return false;
		case CrittersActorType.LoudNoise:
			return (actor.transform.position - base.transform.position).sqrMagnitude < sensoryRange;
		case CrittersActorType.StickyGoo:
			return ((CrittersStickyGoo)actor).CanAffect(base.transform.position);
		case CrittersActorType.Grabber:
			return CanSeeActor(actor.transform.position);
		case CrittersActorType.Cage:
			return CanSeeActor(actor.transform.position);
		case CrittersActorType.Creature:
			return CanSeeActor(actor.transform.position);
		case CrittersActorType.BrightLight:
			return CanSeeActor(actor.transform.position);
		case CrittersActorType.FoodSpawner:
			return CanSeeActor(actor.transform.position);
		case CrittersActorType.StunBomb:
			return CanSeeActor(actor.transform.position);
		default:
			return false;
		}
	}

	public override bool ProcessLocal()
	{
		CreatureUpdateData creatureUpdateData = new CreatureUpdateData(this);
		bool flag = base.ProcessLocal();
		if (!isEnabled)
		{
			return flag;
		}
		wasSomethingInTheWay = false;
		UpdateMoodSourceData();
		StuckCheck();
		switch (currentState)
		{
		case CreatureState.Idle:
			IdleStateUpdate();
			DespawnCheck();
			break;
		case CreatureState.Eating:
			EatingStateUpdate();
			DespawnCheck();
			break;
		case CreatureState.Sleeping:
			SleepingStateUpdate();
			DespawnCheck();
			break;
		case CreatureState.AttractedTo:
			AttractedStateUpdate();
			DespawnCheck();
			break;
		case CreatureState.Running:
			RunningStateUpdate();
			DespawnCheck();
			break;
		case CreatureState.Grabbed:
			GrabbedStateUpdate();
			break;
		case CreatureState.SeekingFood:
			SeekingFoodStateUpdate();
			DespawnCheck();
			break;
		case CreatureState.Captured:
			CapturedStateUpdate();
			break;
		case CreatureState.Stunned:
			StunnedStateUpdate();
			break;
		case CreatureState.WaitingToDespawn:
			WaitingToDespawnStateUpdate();
			break;
		case CreatureState.Despawning:
			DespawningStateUpdate();
			break;
		case CreatureState.Spawning:
			SpawningStateUpdate();
			break;
		}
		UpdateStateAnim();
		updatedSinceLastFrame = flag || updatedSinceLastFrame || !creatureUpdateData.SameData(this);
		return updatedSinceLastFrame;
	}

	private void StuckCheck()
	{
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		if (!(_nextStuckCheck > (double)realtimeSinceStartup))
		{
			_nextStuckCheck = realtimeSinceStartup + 1f;
			if (!canJump && rb.IsSleeping())
			{
				canJump = true;
			}
			if (base.transform.position.y < killHeight)
			{
				SetState(CreatureState.Despawning);
			}
		}
	}

	private void DespawnCheck()
	{
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		if (!(_nextDespawnCheck > (double)realtimeSinceStartup))
		{
			_nextDespawnCheck = realtimeSinceStartup + 1f;
			bool flag = false;
			if ((!(lifeTime <= 0.0)) ? ((PhotonNetwork.InRoom ? PhotonNetwork.Time : ((double)Time.time)) - lifeTimeStart > lifeTime) : (creatureConfiguration != null && !creatureConfiguration.ShouldDespawn()))
			{
				SetState(CreatureState.WaitingToDespawn);
				spawningStartingPosition = base.gameObject.transform.position;
				despawnStartTime = (PhotonNetwork.InRoom ? PhotonNetwork.Time : ((double)Time.time));
			}
		}
	}

	public void SetTemplate(int templateIndex)
	{
		TemplateIndex = templateIndex;
		UpdateTemplate();
	}

	private void UpdateTemplate()
	{
		if (TemplateIndex != LastTemplateIndex)
		{
			creatureConfiguration = CrittersManager.instance.creatureIndex[TemplateIndex];
			if (creatureConfiguration != null)
			{
				creatureConfiguration.ApplyToCreature(this);
				InitializeAttractors();
			}
			LastTemplateIndex = TemplateIndex;
			InitializeTemplateValues();
		}
		if (this.OnDataChange != null)
		{
			this.OnDataChange();
		}
	}

	private void InitializeAttractors()
	{
		attractedToTypes = new Dictionary<CrittersActorType, float>();
		afraidOfTypes = new Dictionary<CrittersActorType, float>();
		if (attractedToList != null)
		{
			for (int i = 0; i < attractedToList.Count; i++)
			{
				attractedToTypes.Add(attractedToList[i].type, attractedToList[i].multiplier);
			}
		}
		if (afraidOfList != null)
		{
			for (int j = 0; j < afraidOfList.Count; j++)
			{
				afraidOfTypes.Add(afraidOfList[j].type, afraidOfList[j].multiplier);
			}
		}
	}

	public override void ProcessRemote()
	{
		UpdateTemplate();
		base.ProcessRemote();
		UpdateStateAnim();
	}

	public void SetState(CreatureState newState)
	{
		if (currentState == newState)
		{
			return;
		}
		if (currentState == CreatureState.Captured)
		{
			base.transform.localScale = Vector3.one;
		}
		ClearOngoingStateFX();
		currentState = newState;
		switch (newState)
		{
		case CreatureState.Spawning:
			if (CrittersManager.instance.LocalAuthority())
			{
				spawningStartingPosition = base.gameObject.transform.position;
				spawnStartTime = (PhotonNetwork.InRoom ? ((float)PhotonNetwork.Time) : Time.time);
			}
			break;
		case CreatureState.Despawning:
			if (CrittersManager.instance.LocalAuthority())
			{
				spawningStartingPosition = base.gameObject.transform.position;
				despawnStartTime = (PhotonNetwork.InRoom ? ((float)PhotonNetwork.Time) : Time.time);
			}
			break;
		}
		StartOngoingStateFX(newState);
		GameObject valueOrDefault = StartStateFX.GetValueOrDefault(currentState);
		if (valueOrDefault.IsNotNull())
		{
			GameObject pooled = CrittersPool.GetPooled(valueOrDefault);
			if (pooled != null)
			{
				pooled.transform.position = base.transform.position;
			}
		}
		currentAnimTime = 0f;
		if (stateAnim.TryGetValue(currentState, out var value))
		{
			currentAnim = value;
		}
		else
		{
			currentAnim = null;
			animTarget.localPosition = Vector3.zero;
			animTarget.localScale = Vector3.one;
		}
		if (this.OnDataChange != null)
		{
			this.OnDataChange();
		}
	}

	private void ClearOngoingStateFX()
	{
		if (currentOngoingStateFX.IsNotNull())
		{
			CrittersPool.Return(currentOngoingStateFX);
			currentOngoingStateFX = null;
		}
	}

	private void StartOngoingStateFX(CreatureState state)
	{
		GameObject valueOrDefault = OngoingStateFX.GetValueOrDefault(state);
		if (valueOrDefault.IsNotNull())
		{
			currentOngoingStateFX = CrittersPool.GetPooled(valueOrDefault);
			if (currentOngoingStateFX.IsNotNull())
			{
				currentOngoingStateFX.transform.SetParent(base.transform, worldPositionStays: false);
				currentOngoingStateFX.transform.localPosition = Vector3.zero;
			}
		}
	}

	[Conditional("UNITY_EDITOR")]
	public void UpdateStateColor()
	{
		switch (currentState)
		{
		case CreatureState.Idle:
			debugStateIndicator.material.color = debugColorIdle;
			break;
		case CreatureState.Eating:
			debugStateIndicator.material.color = debugColorEating;
			break;
		case CreatureState.Running:
			debugStateIndicator.material.color = debugColorScared;
			break;
		case CreatureState.Sleeping:
			debugStateIndicator.material.color = debugColorSleeping;
			break;
		case CreatureState.SeekingFood:
			debugStateIndicator.material.color = debugColorSeekingFood;
			break;
		case CreatureState.Grabbed:
			debugStateIndicator.material.color = debugColorCaught;
			break;
		case CreatureState.Captured:
			debugStateIndicator.material.color = debugColorCaged;
			break;
		case CreatureState.Stunned:
			debugStateIndicator.material.color = debugColorStunned;
			break;
		case CreatureState.AttractedTo:
			debugStateIndicator.material.color = debugColorAttracted;
			break;
		default:
			debugStateIndicator.material.color = new Color(1f, 0f, 1f);
			break;
		}
	}

	public void UpdateStateAnim()
	{
		if (currentAnim != null)
		{
			currentAnimTime += Time.deltaTime * currentAnim.playSpeed;
			currentAnimTime %= 1f;
			float num = currentAnim.squashAmount.Evaluate(currentAnimTime);
			float z = currentAnim.forwardOffset.Evaluate(currentAnimTime);
			float x = currentAnim.horizontalOffset.Evaluate(currentAnimTime);
			float y = currentAnim.verticalOffset.Evaluate(currentAnimTime);
			animTarget.localPosition = new Vector3(x, y, z);
			float num2 = 1f - num;
			num2 *= 0.5f;
			num2 += 1f;
			animTarget.localScale = new Vector3(num2, num, num2);
		}
	}

	public void IdleStateUpdate()
	{
		if (AboveFearThreshold())
		{
			SetState(CreatureState.Running);
		}
		else if (AboveAttractedThreshold() && (!AboveHungryThreshold() || !CrittersManager.AnyFoodNearby(this)))
		{
			SetState(CreatureState.AttractedTo);
		}
		else if (AboveHungryThreshold())
		{
			SetState(CreatureState.SeekingFood);
		}
		else if (AboveSleepyThreshold())
		{
			SetState(CreatureState.Sleeping);
		}
		else if (CanJump())
		{
			RandomJump();
		}
	}

	public void EatingStateUpdate()
	{
		if (AboveFearThreshold())
		{
			SetState(CreatureState.Running);
		}
		else if (BelowNotHungryThreshold())
		{
			SetState(CreatureState.Idle);
		}
		else if (!withinEatingRadius || eatingTarget.IsNull() || eatingTarget.currentFood <= 0f)
		{
			SetState(CreatureState.SeekingFood);
		}
	}

	public void SleepingStateUpdate()
	{
		if (AboveFearThreshold())
		{
			SetState(CreatureState.Running);
		}
		else if (BelowNotSleepyThreshold())
		{
			SetState(CreatureState.Idle);
		}
	}

	public void AttractedStateUpdate()
	{
		if (AboveFearThreshold())
		{
			SetState(CreatureState.Running);
		}
		else if (BelowUnAttractedThreshold())
		{
			SetState(CreatureState.Idle);
		}
		else
		{
			if (!CanJump())
			{
				return;
			}
			if (AboveHungryThreshold() && CrittersManager.AnyFoodNearby(this))
			{
				SetState(CreatureState.SeekingFood);
				return;
			}
			if (CrittersManager.instance.awareOfActors[this].Contains(attractionTarget))
			{
				lastSeenAttractionPosition = attractionTarget.transform.position;
			}
			JumpTowards(lastSeenAttractionPosition);
		}
	}

	public void RunningStateUpdate()
	{
		if (CanJump())
		{
			if (CrittersManager.instance.awareOfActors[this].Contains(fearTarget))
			{
				lastSeenFearPosition = fearTarget.transform.position;
			}
			JumpAwayFrom(lastSeenFearPosition);
		}
		if (BelowNotAfraidThreshold())
		{
			SetState(CreatureState.Idle);
		}
	}

	public void SeekingFoodStateUpdate()
	{
		if (AboveFearThreshold())
		{
			SetState(CreatureState.Running);
		}
		else
		{
			if (!CanJump())
			{
				return;
			}
			if (CrittersManager.CritterAwareOfAny(this))
			{
				eatingTarget = CrittersManager.ClosestFood(this);
				if (eatingTarget != null)
				{
					withinEatingRadius = (eatingTarget.food.transform.position - base.transform.position).sqrMagnitude < eatingRadiusMaxSquared;
					if (!withinEatingRadius)
					{
						JumpTowards(eatingTarget.food.transform.position);
						return;
					}
					base.transform.forward = (eatingTarget.food.transform.position - base.transform.position).X_Z().normalized;
					SetState(CreatureState.Eating);
					debugStateIndicator.material.color = debugColorEating;
				}
				else if (AboveAttractedThreshold())
				{
					SetState(CreatureState.AttractedTo);
				}
				else
				{
					RandomJump();
				}
			}
			else
			{
				RandomJump();
			}
		}
	}

	public void GrabbedStateUpdate()
	{
		if (currentState == CreatureState.Grabbed && grabbedTarget != null)
		{
			if (currentStruggle >= escapeThreshold || !grabbedTarget.grabbing)
			{
				Released(keepWorldPosition: true);
			}
		}
		else if (grabbedTarget == null)
		{
			Released(keepWorldPosition: true);
		}
	}

	protected override void HandleRemoteReleased()
	{
		base.HandleRemoteReleased();
		if (cageTarget.IsNotNull())
		{
			fearTarget = cageTarget;
			cageTarget.SetHasCritter(value: false);
			cageTarget = null;
		}
		if (grabbedTarget.IsNotNull())
		{
			fearTarget = grabbedTarget;
			grabbedTarget = null;
			if ((bool)OnReleasedFX)
			{
				CrittersPool.GetPooled(OnReleasedFX).transform.position = base.transform.position;
			}
		}
	}

	public override void Released(bool keepWorldPosition, Quaternion rotation = default(Quaternion), Vector3 position = default(Vector3), Vector3 impulse = default(Vector3), Vector3 impulseRotation = default(Vector3))
	{
		base.Released(keepWorldPosition, rotation, position, impulse, impulseRotation);
		if (currentState != CreatureState.Grabbed && currentState != CreatureState.Captured)
		{
			return;
		}
		if (grabbedTarget.IsNotNull() && grabbedTarget.grabbedActors.Contains(this))
		{
			grabbedTarget.grabbedActors.Remove(this);
		}
		if (currentState == CreatureState.Grabbed)
		{
			fearTarget = grabbedTarget;
			grabbedTarget = null;
			if ((bool)OnReleasedFX)
			{
				CrittersPool.GetPooled(OnReleasedFX).transform.position = base.transform.position;
			}
		}
		else if (currentState == CreatureState.Captured)
		{
			base.transform.localScale = Vector3.one;
			fearTarget = cageTarget;
			cageTarget.SetHasCritter(value: false);
			cageTarget = null;
		}
		if (struggleGainedPerSecond > 0f)
		{
			currentFear = maxFear;
			SetState(CreatureState.Running);
			lastSeenFearPosition = fearTarget.transform.position;
		}
		else
		{
			currentFear = 0f;
			SetState(CreatureState.Idle);
		}
	}

	public void CapturedStateUpdate()
	{
		if (cageTarget.IsNull())
		{
			cageTarget = (CrittersCage)CrittersManager.instance.actorById[actorIdTarget];
			cageTarget.SetHasCritter(value: false);
		}
		if (cageTarget.inReleasingPosition && cageTarget.heldByPlayer)
		{
			Released(keepWorldPosition: true);
		}
	}

	public void StunnedStateUpdate()
	{
		remainingStunnedTime = Mathf.Max(0f, remainingStunnedTime - Time.deltaTime);
		if (remainingStunnedTime <= 0f)
		{
			currentFear = maxFear;
			SetState(CreatureState.Running);
		}
	}

	public void WaitingToDespawnStateUpdate()
	{
		if (Mathf.FloorToInt(rb.linearVelocity.magnitude * 10f) == 0)
		{
			SetState(CreatureState.Despawning);
		}
	}

	public void DespawningStateUpdate()
	{
		_despawnAnimTime = (PhotonNetwork.InRoom ? PhotonNetwork.Time : ((double)Time.time)) - despawnStartTime;
		if (_despawnAnimTime >= (double)_despawnAnimationDuration)
		{
			base.gameObject.SetActive(value: false);
			TemplateIndex = -1;
		}
	}

	public void SpawningStateUpdate()
	{
		_spawnAnimTime = (PhotonNetwork.InRoom ? PhotonNetwork.Time : ((double)Time.time)) - spawnStartTime;
		MoveActor(spawningStartingPosition + new Vector3(0f, spawnInHeighMovement.Evaluate(Mathf.Clamp((float)_spawnAnimTime, 0f, _spawnAnimationDuration)), 0f), base.transform.rotation);
		if (_spawnAnimTime >= (double)_spawnAnimationDuration)
		{
			SetState(CreatureState.Idle);
		}
	}

	public void UpdateMoodSourceData()
	{
		UpdateHunger();
		UpdateFearAndAttraction();
		UpdateSleepiness();
		UpdateStruggle();
		UpdateSlowed();
		UpdateGrabbed();
		UpdateCaged();
	}

	public void UpdateHunger()
	{
		if (currentState == CreatureState.Eating && !eatingTarget.IsNull())
		{
			eatingTarget.Feed(hungerLostPerSecond * Time.deltaTime);
			currentHunger = Mathf.Max(0f, currentHunger - hungerLostPerSecond * Time.deltaTime);
		}
		else
		{
			currentHunger = Mathf.Min(maxHunger, currentHunger + hungerGainedPerSecond * Time.deltaTime);
		}
	}

	public void UpdateFearAndAttraction()
	{
		if (currentState == CreatureState.Spawning)
		{
			return;
		}
		currentFear = Mathf.Max(0f, currentFear - fearLostPerSecond * Time.deltaTime);
		currentAttraction = Mathf.Max(0f, currentAttraction - attractionLostPerSecond * Time.deltaTime);
		for (int i = 0; i < CrittersManager.instance.awareOfActors[this].Count; i++)
		{
			CrittersActor crittersActor = CrittersManager.instance.awareOfActors[this][i];
			float value2;
			if (afraidOfTypes != null && afraidOfTypes.TryGetValue(crittersActor.crittersActorType, out var value))
			{
				crittersActor.CalculateFear(this, value);
			}
			else if (attractedToTypes != null && attractedToTypes.TryGetValue(crittersActor.crittersActorType, out value2))
			{
				crittersActor.CalculateAttraction(this, value2);
			}
		}
	}

	public void IncreaseFear(float fearAmount, CrittersActor actor)
	{
		if (fearAmount > 0f)
		{
			currentFear += fearAmount;
			currentFear = Mathf.Min(maxFear, currentFear);
			fearTarget = actor;
			lastSeenFearPosition = fearTarget.transform.position;
		}
	}

	public void IncreaseAttraction(float attractionAmount, CrittersActor actor)
	{
		if (attractionAmount > 0f)
		{
			currentAttraction += attractionAmount;
			currentAttraction = Mathf.Min(maxAttraction, currentAttraction);
			attractionTarget = actor;
			lastSeenAttractionPosition = attractionTarget.transform.position;
		}
	}

	public void UpdateSleepiness()
	{
		if (currentState == CreatureState.Sleeping)
		{
			currentSleepiness = Mathf.Max(0f, currentSleepiness - Time.deltaTime * sleepinessLostPerSecond);
		}
		else
		{
			currentSleepiness = Mathf.Min(maxSleepiness, currentSleepiness + Time.deltaTime * sleepinessGainedPerSecond);
		}
	}

	public void UpdateStruggle()
	{
		if (currentState == CreatureState.Grabbed)
		{
			currentStruggle = Mathf.Clamp(currentStruggle + struggleGainedPerSecond * Time.deltaTime, 0f, maxStruggle);
		}
		else
		{
			currentStruggle = Mathf.Max(0f, currentStruggle - struggleLostPerSecond * Time.deltaTime);
		}
	}

	private void UpdateSlowed()
	{
		if (remainingSlowedTime > 0f)
		{
			remainingSlowedTime -= Time.deltaTime;
			if (remainingSlowedTime < 0f)
			{
				slowSpeedMod = 1f;
			}
		}
		else
		{
			if (currentState == CreatureState.Captured || currentState == CreatureState.Despawning || currentState == CreatureState.Grabbed || currentState == CreatureState.WaitingToDespawn || currentState == CreatureState.Spawning)
			{
				return;
			}
			for (int i = 0; i < CrittersManager.instance.awareOfActors[this].Count; i++)
			{
				CrittersActor crittersActor = CrittersManager.instance.awareOfActors[this][i];
				if (crittersActor.crittersActorType == CrittersActorType.StickyGoo)
				{
					CrittersStickyGoo crittersStickyGoo = crittersActor as CrittersStickyGoo;
					slowSpeedMod = crittersStickyGoo.slowModifier;
					remainingSlowedTime = crittersStickyGoo.slowDuration;
					crittersStickyGoo.EffectApplied(this);
				}
			}
		}
	}

	public void UpdateGrabbed()
	{
		if (currentState == CreatureState.Grabbed || currentState == CreatureState.Captured)
		{
			return;
		}
		for (int i = 0; i < CrittersManager.instance.awareOfActors[this].Count; i++)
		{
			CrittersActor crittersActor = CrittersManager.instance.awareOfActors[this][i];
			if (crittersActor.crittersActorType == CrittersActorType.Grabber && !crittersActor.isOnPlayer && IsGrabPossible((CrittersGrabber)crittersActor))
			{
				GrabbedBy(crittersActor, positionOverride: true);
			}
		}
	}

	public void UpdateCaged()
	{
		if (currentState == CreatureState.Captured)
		{
			return;
		}
		for (int i = 0; i < CrittersManager.instance.awareOfActors[this].Count; i++)
		{
			CrittersActor crittersActor = CrittersManager.instance.awareOfActors[this][i];
			CrittersCage crittersCage = crittersActor as CrittersCage;
			if (crittersActor.crittersActorType == CrittersActorType.Cage && crittersCage.IsNotNull() && crittersCage.CanCatch && WithinCaptureDistance(crittersCage))
			{
				GrabbedBy(crittersActor, positionOverride: true, crittersCage.cagePosition.localRotation, crittersCage.cagePosition.localPosition);
			}
		}
	}

	public void RandomJump()
	{
		for (int i = 0; i < 5; i++)
		{
			base.transform.eulerAngles = new Vector3(0f, 360f * UnityEngine.Random.value, 0f);
			if (!SomethingInTheWay())
			{
				break;
			}
		}
		LocalJump(maxJumpVel, 45f);
	}

	public void JumpTowards(Vector3 targetPos)
	{
		if (SomethingInTheWay((targetPos - base.transform.position).X_Z()))
		{
			RandomJump();
			return;
		}
		base.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(targetPos - base.transform.position, Vector3.up), Vector3.up);
		LocalJump(JumpVelocityForDistanceAtAngle(Vector3.ProjectOnPlane(targetPos - base.transform.position, Vector3.up).magnitude * fudge, 45f), 45f);
	}

	public void JumpAwayFrom(Vector3 targetPos)
	{
		Vector3 vector = (base.transform.position - targetPos).X_Z();
		if (vector == Vector3.zero)
		{
			vector = base.transform.forward;
		}
		Vector3 vector2 = Quaternion.Euler(0f, UnityEngine.Random.Range(-30, 30), 0f) * vector;
		if (SomethingInTheWay(vector2))
		{
			RandomJump();
			return;
		}
		base.transform.rotation = Quaternion.LookRotation(vector2, Vector3.up);
		LocalJump(maxJumpVel, 45f);
	}

	public bool SomethingInTheWay(Vector3 direction = default(Vector3))
	{
		if (direction == default(Vector3))
		{
			direction = base.transform.forward;
		}
		bool flag = Physics.RaycastNonAlloc(bodyCollider.bounds.center, direction, raycastHits, obstacleSeeDistance, CrittersManager.instance.movementLayers) > 0;
		wasSomethingInTheWay |= flag;
		return flag;
	}

	public override bool CanBeGrabbed(CrittersActor grabbedBy)
	{
		if (currentState == CreatureState.Captured)
		{
			return false;
		}
		return base.CanBeGrabbed(grabbedBy);
	}

	public override void GrabbedBy(CrittersActor grabbingActor, bool positionOverride = false, Quaternion localRotation = default(Quaternion), Vector3 localOffset = default(Vector3), bool disableGrabbing = false)
	{
		switch (grabbingActor.crittersActorType)
		{
		case CrittersActorType.Grabber:
			SetState(CreatureState.Grabbed);
			grabbedTarget = (CrittersGrabber)grabbingActor;
			actorIdTarget = grabbedTarget.actorId;
			base.GrabbedBy(grabbingActor, positionOverride, localRotation, localOffset, disableGrabbing);
			break;
		case CrittersActorType.Cage:
			SetState(CreatureState.Captured);
			cageTarget = (CrittersCage)grabbingActor;
			cageTarget.SetHasCritter(value: true);
			actorIdTarget = cageTarget.actorId;
			if (CrittersManager.instance.LocalAuthority())
			{
				base.transform.localScale = cageTarget.critterScale;
			}
			base.GrabbedBy(grabbingActor, positionOverride, localRotation, localOffset, disableGrabbing);
			break;
		}
	}

	protected override void RemoteGrabbedBy(CrittersActor grabbingActor)
	{
		base.RemoteGrabbedBy(grabbingActor);
		switch (grabbingActor.crittersActorType)
		{
		case CrittersActorType.Cage:
			cageTarget = (CrittersCage)grabbingActor;
			cageTarget.SetHasCritter(value: true);
			actorIdTarget = cageTarget.actorId;
			if (CrittersManager.instance.LocalAuthority())
			{
				base.transform.localScale = cageTarget.critterScale;
			}
			break;
		case CrittersActorType.Grabber:
			grabbedTarget = (CrittersGrabber)grabbingActor;
			actorIdTarget = grabbedTarget.actorId;
			break;
		}
	}

	public void Stunned(float duration)
	{
		if (currentState != CreatureState.Captured && currentState != CreatureState.Grabbed && currentState != CreatureState.Despawning && currentState != CreatureState.WaitingToDespawn)
		{
			remainingStunnedTime = duration;
			SetState(CreatureState.Stunned);
			updatedSinceLastFrame = true;
		}
	}

	public bool AboveFearThreshold()
	{
		return currentFear >= scaredThreshold;
	}

	public bool BelowNotAfraidThreshold()
	{
		return currentFear < calmThreshold;
	}

	public bool AboveAttractedThreshold()
	{
		return currentAttraction >= attractedThreshold;
	}

	public bool BelowUnAttractedThreshold()
	{
		return currentAttraction < unattractedThreshold;
	}

	public bool AboveHungryThreshold()
	{
		return currentHunger >= hungryThreshold;
	}

	public bool BelowNotHungryThreshold()
	{
		return currentHunger < satiatedThreshold;
	}

	public bool AboveSleepyThreshold()
	{
		return currentSleepiness >= tiredThreshold;
	}

	public bool BelowNotSleepyThreshold()
	{
		return currentSleepiness < awakeThreshold;
	}

	public bool CanJump()
	{
		if (!canJump)
		{
			return false;
		}
		float num = ((currentState != CreatureState.Running) ? jumpCooldown : scaredJumpCooldown);
		float num2 = (PhotonNetwork.InRoom ? ((float)PhotonNetwork.Time) : Time.time);
		if (lastImpulseTime > (double)(num2 + jumpCooldown + jumpVariabilityTime))
		{
			lastImpulseTime = num2 + GetAdditiveJumpDelay();
		}
		return (double)num2 > lastImpulseTime + (double)num;
	}

	public void OnCollisionEnter(Collision collision)
	{
		canJump = true;
	}

	public void OnCollisionExit(Collision collision)
	{
		canJump = false;
	}

	public void SetVelocity(Vector3 linearVelocity)
	{
		rb.linearVelocity = linearVelocity;
	}

	public override int AddActorDataToList(ref List<object> objList)
	{
		base.AddActorDataToList(ref objList);
		objList.Add(Mathf.FloorToInt(currentFear));
		objList.Add(Mathf.FloorToInt(currentHunger));
		objList.Add(Mathf.FloorToInt(currentSleepiness));
		objList.Add(Mathf.FloorToInt(currentStruggle));
		objList.Add(currentState);
		objList.Add(actorIdTarget);
		objList.Add(lifeTimeStart);
		objList.Add(TemplateIndex);
		objList.Add(Mathf.FloorToInt(remainingStunnedTime));
		objList.Add(spawnStartTime);
		objList.Add(despawnStartTime);
		objList.AddRange(visuals.Appearance.WriteToRPCData());
		return TotalActorDataLength();
	}

	public override int TotalActorDataLength()
	{
		return BaseActorDataLength() + 11 + CritterAppearance.DataLength();
	}

	public override int UpdateFromRPC(object[] data, int startingIndex)
	{
		startingIndex += base.UpdateFromRPC(data, startingIndex);
		if (!CrittersManager.ValidateDataType<int>(data[startingIndex], out var dataAsType))
		{
			return TotalActorDataLength();
		}
		if (!CrittersManager.ValidateDataType<int>(data[startingIndex + 1], out var dataAsType2))
		{
			return TotalActorDataLength();
		}
		if (!CrittersManager.ValidateDataType<int>(data[startingIndex + 2], out var dataAsType3))
		{
			return TotalActorDataLength();
		}
		if (!CrittersManager.ValidateDataType<int>(data[startingIndex + 3], out var dataAsType4))
		{
			return TotalActorDataLength();
		}
		if (!CrittersManager.ValidateDataType<int>(data[startingIndex + 4], out var dataAsType5))
		{
			return TotalActorDataLength();
		}
		if (!Enum.IsDefined(typeof(CreatureState), (CreatureState)dataAsType5))
		{
			return TotalActorDataLength();
		}
		if (!CrittersManager.ValidateDataType<int>(data[startingIndex + 5], out var dataAsType6))
		{
			return TotalActorDataLength();
		}
		if (!CrittersManager.ValidateDataType<double>(data[startingIndex + 6], out var dataAsType7))
		{
			return TotalActorDataLength();
		}
		if (!CrittersManager.ValidateDataType<int>(data[startingIndex + 7], out var dataAsType8))
		{
			return TotalActorDataLength();
		}
		if (!CrittersManager.ValidateDataType<int>(data[startingIndex + 8], out var dataAsType9))
		{
			return TotalActorDataLength();
		}
		if (!CrittersManager.ValidateDataType<double>(data[startingIndex + 9], out var dataAsType10))
		{
			return TotalActorDataLength();
		}
		if (!CrittersManager.ValidateDataType<double>(data[startingIndex + 10], out var dataAsType11))
		{
			return TotalActorDataLength();
		}
		currentFear = dataAsType;
		currentHunger = dataAsType2;
		currentSleepiness = dataAsType3;
		currentStruggle = dataAsType4;
		SetState((CreatureState)dataAsType5);
		actorIdTarget = dataAsType6;
		lifeTimeStart = dataAsType7.GetFinite();
		TemplateIndex = dataAsType8;
		remainingStunnedTime = dataAsType9;
		spawnStartTime = dataAsType10.GetFinite();
		despawnStartTime = dataAsType11.GetFinite();
		CrittersActor value = null;
		switch (currentState)
		{
		case CreatureState.Grabbed:
			if (CrittersManager.instance.actorById.TryGetValue(parentActorId, out value))
			{
				grabbedTarget = (CrittersGrabber)value;
			}
			cageTarget = null;
			break;
		case CreatureState.Captured:
			if (CrittersManager.instance.actorById.TryGetValue(parentActorId, out value))
			{
				cageTarget = (CrittersCage)value;
				if (cageTarget != null)
				{
					base.transform.localScale = cageTarget.critterScale;
				}
			}
			grabbedTarget = null;
			break;
		default:
			grabbedTarget = null;
			cageTarget = null;
			break;
		}
		UpdateTemplate();
		visuals.SetAppearance(CritterAppearance.ReadFromRPCData(data[(startingIndex + 11)..]));
		return TotalActorDataLength();
	}

	public override bool UpdateSpecificActor(PhotonStream stream)
	{
		if (!(base.UpdateSpecificActor(stream) & CrittersManager.ValidateDataType<int>(stream.ReceiveNext(), out var dataAsType) & CrittersManager.ValidateDataType<int>(stream.ReceiveNext(), out var dataAsType2) & CrittersManager.ValidateDataType<int>(stream.ReceiveNext(), out var dataAsType3) & CrittersManager.ValidateDataType<int>(stream.ReceiveNext(), out var dataAsType4) & CrittersManager.ValidateDataType<int>(stream.ReceiveNext(), out var dataAsType5) & Enum.IsDefined(typeof(CreatureState), (CreatureState)dataAsType5) & CrittersManager.ValidateDataType<int>(stream.ReceiveNext(), out var dataAsType6) & CrittersManager.ValidateDataType<double>(stream.ReceiveNext(), out var dataAsType7) & CrittersManager.ValidateDataType<int>(stream.ReceiveNext(), out var dataAsType8) & CrittersManager.ValidateDataType<int>(stream.ReceiveNext(), out var dataAsType9) & CrittersManager.ValidateDataType<double>(stream.ReceiveNext(), out var dataAsType10) & CrittersManager.ValidateDataType<double>(stream.ReceiveNext(), out var dataAsType11)))
		{
			return false;
		}
		currentFear = dataAsType;
		currentHunger = dataAsType2;
		currentSleepiness = dataAsType3;
		currentStruggle = dataAsType4;
		SetState((CreatureState)dataAsType5);
		actorIdTarget = dataAsType6;
		lifeTimeStart = dataAsType7;
		TemplateIndex = dataAsType8;
		remainingStunnedTime = dataAsType9;
		spawnStartTime = dataAsType10;
		despawnStartTime = dataAsType11;
		UpdateTemplate();
		CrittersActor value = null;
		switch (currentState)
		{
		case CreatureState.Grabbed:
			if (CrittersManager.instance.actorById.TryGetValue(parentActorId, out value))
			{
				grabbedTarget = (CrittersGrabber)value;
			}
			cageTarget = null;
			break;
		case CreatureState.Captured:
			if (CrittersManager.instance.actorById.TryGetValue(parentActorId, out value))
			{
				cageTarget = (CrittersCage)value;
				if (cageTarget != null)
				{
					base.transform.localScale = cageTarget.critterScale;
				}
			}
			grabbedTarget = null;
			break;
		default:
			grabbedTarget = null;
			cageTarget = null;
			break;
		}
		return true;
	}

	public override void SendDataByCrittersActorType(PhotonStream stream)
	{
		base.SendDataByCrittersActorType(stream);
		stream.SendNext(Mathf.FloorToInt(currentFear));
		stream.SendNext(Mathf.FloorToInt(currentHunger));
		stream.SendNext(Mathf.FloorToInt(currentSleepiness));
		stream.SendNext(Mathf.FloorToInt(currentStruggle));
		stream.SendNext(currentState);
		stream.SendNext(actorIdTarget);
		stream.SendNext(lifeTimeStart);
		stream.SendNext(TemplateIndex);
		stream.SendNext(Mathf.FloorToInt(remainingStunnedTime));
		stream.SendNext(spawnStartTime);
		stream.SendNext(despawnStartTime);
	}

	public void SetConfiguration(CritterConfiguration getRandomConfiguration)
	{
		throw new NotImplementedException();
	}

	public void SetSpawnData(object[] spawnData)
	{
		visuals.SetAppearance(CritterAppearance.ReadFromRPCData(spawnData));
	}

	private IList<KeyValueStringPair> BuildEyeScannerData()
	{
		eyeScanData[0] = new KeyValueStringPair("Name", creatureConfiguration.critterName);
		eyeScanData[1] = new KeyValueStringPair("Type", creatureConfiguration.animalType.ToString());
		eyeScanData[2] = new KeyValueStringPair("Temperament", creatureConfiguration.behaviour.temperament);
		eyeScanData[3] = new KeyValueStringPair("Habitat", creatureConfiguration.biome.GetHabitatDescription());
		eyeScanData[4] = new KeyValueStringPair("Size", visuals.Appearance.size.ToString("0.00"));
		eyeScanData[5] = new KeyValueStringPair("State", GetCurrentStateName());
		return eyeScanData;
	}

	private string GetCurrentStateName()
	{
		string text = currentState switch
		{
			CreatureState.Idle => "Adventuring", 
			CreatureState.Eating => "Eating", 
			CreatureState.AttractedTo => "Curious", 
			CreatureState.Running => "Scared", 
			CreatureState.Grabbed => (struggleGainedPerSecond > 0f) ? "Struggling" : "Happy", 
			CreatureState.Sleeping => "Sleeping", 
			CreatureState.SeekingFood => "Foraging", 
			CreatureState.Captured => "Captured", 
			CreatureState.Stunned => "Stunned", 
			_ => "Contemplating Life", 
		};
		if (slowSpeedMod < 1f)
		{
			text = "Slowed, " + text;
		}
		return text;
	}
}

using System.Collections.Generic;
using System.IO;
using CjLib;
using Photon.Pun;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class GREnemyRanged : MonoBehaviour, IGameEntityComponent, IGameEntitySerialize, IGameHittable, IGameAgentComponent, IGameProjectileLauncher, IGameEntityDebugComponent
{
	public enum Behavior
	{
		Idle,
		Patrol,
		Search,
		Stagger,
		Dying,
		SeekRangedAttackPosition,
		RangedAttack,
		RangedAttackCooldown,
		Flashed,
		Investigate,
		Jump,
		Count
	}

	public enum BodyState
	{
		Destroyed,
		Bones,
		Shell,
		Count
	}

	public GameEntity entity;

	public GameAgent agent;

	public GREnemy enemy;

	public GRArmorEnemy armor;

	public GameHittable hittable;

	public GRAttributes attributes;

	public Animation anim;

	public GRSenseNearby senseNearby;

	public GRSenseLineOfSight senseLineOfSight;

	public GRAbilityStagger abilityStagger;

	public GRAbilityDie abilityDie;

	public GRAbilityMoveToTarget abilityInvestigate;

	public GRAbilityPatrol abilityPatrol;

	public GRAbilityFlashed abilityFlashed;

	public GRAbilityKeepDistance abilityKeepDistance;

	public GRAbilityJump abilityJump;

	public List<Renderer> bones;

	public List<Renderer> always;

	public Transform coreMarker;

	public GRCollectible corePrefab;

	public Transform headTransform;

	public float sightDist;

	public float loseSightDist;

	public float sightFOV;

	public float sightLostFollowStopTime = 0.5f;

	public float searchTime = 5f;

	public float hearingRadius = 5f;

	public float turnSpeed = 540f;

	public Color chaseColor = Color.red;

	public AbilitySound attackAbilitySound;

	public AbilitySound chaseAbilitySound;

	public float rangedAttackDistMin = 6f;

	public float rangedAttackDistMax = 8f;

	public float rangedAttackChargeTime = 0.5f;

	public float rangedAttackRecoverTime = 2f;

	public float projectileSpeed = 5f;

	public float projectileHitRadius = 1f;

	public GameObject rangedProjectilePrefab;

	public Transform rangedProjectileFirePoint;

	[ReadOnly]
	[SerializeField]
	private GRPatrolPath patrolPath;

	public NavMeshAgent navAgent;

	public AudioSource audioSource;

	public AudioSource audioSecondarySource;

	public AudioClip damagedSound;

	public float damagedSoundVolume;

	public GameObject fxDamaged;

	public bool lastMoving;

	private Vector3? investigateLocation;

	public bool debugLog;

	public GameObject spitterHeadOnShoulders;

	public GameObject spitterHeadOnShouldersLight;

	public GameObject spitterHeadOnShouldersVFX;

	public GameObject spitterHeadInHand;

	public GameObject spitterHeadInHandLight;

	public GameObject spitterHeadInHandVFX;

	public double spitterLightTurnOffDelay = 0.75;

	private bool headLightReset;

	private double spitterLightTurnOffTime;

	[FormerlySerializedAs("headRemovalInterval")]
	public float headRemovalFrame = 7f / 30f;

	private double headRemovaltime;

	private bool headRemoved;

	private Transform target;

	[ReadOnly]
	public int hp;

	[ReadOnly]
	public Behavior currBehavior;

	[ReadOnly]
	public double behaviorEndTime;

	[ReadOnly]
	public BodyState currBodyState;

	[ReadOnly]
	public int nextPatrolNode;

	[ReadOnly]
	public NetPlayer targetPlayer;

	[ReadOnly]
	public Vector3 lastSeenTargetPosition;

	[ReadOnly]
	public double lastSeenTargetTime;

	[ReadOnly]
	public Vector3 searchPosition;

	[ReadOnly]
	public Vector3 rangedFiringPosition;

	[ReadOnly]
	public Vector3 rangedTargetPosition;

	[ReadOnly]
	private GRPlayer bestTargetPlayer;

	[ReadOnly]
	private NetPlayer bestTargetNetPlayer;

	private bool rangedAttackQueued;

	private double queuedFiringTime;

	private Vector3 queuedFiringPosition;

	private Vector3 queuedTargetPosition;

	private GameObject rangedProjectileInstance;

	private bool projectileHasImpacted;

	private double projectileImpactTime;

	private Rigidbody rigidBody;

	private List<Collider> colliders;

	private LayerMask visibilityLayerMask;

	private Color defaultColor;

	private float lastHitPlayerTime;

	private float minTimeBetweenHits = 0.5f;

	private static List<VRRig> tempRigs = new List<VRRig>(16);

	private bool IsMoving()
	{
		return navAgent.velocity.sqrMagnitude > 0f;
	}

	private void SoftResetThrowableHead()
	{
		headRemoved = false;
		spitterHeadOnShoulders.SetActive(value: true);
		spitterHeadOnShouldersVFX.SetActive(value: false);
		spitterHeadInHand.SetActive(value: false);
		spitterHeadInHandLight.SetActive(value: false);
		spitterHeadInHandVFX.SetActive(value: false);
		headLightReset = true;
		spitterLightTurnOffTime = Time.timeAsDouble + spitterLightTurnOffDelay;
	}

	private void ForceResetThrowableHead()
	{
		headRemoved = false;
		headLightReset = false;
		spitterHeadOnShoulders.SetActive(value: true);
		spitterHeadOnShouldersLight.SetActive(value: false);
		spitterHeadOnShouldersVFX.SetActive(value: false);
		spitterHeadInHand.SetActive(value: false);
		spitterHeadInHandLight.SetActive(value: false);
		spitterHeadInHandVFX.SetActive(value: false);
	}

	private void ForceHeadToDeadState()
	{
		headRemoved = false;
		headLightReset = false;
		spitterHeadOnShoulders.SetActive(value: true);
		spitterHeadOnShouldersLight.SetActive(value: false);
		spitterHeadOnShouldersVFX.SetActive(value: false);
		spitterHeadInHand.SetActive(value: false);
		spitterHeadInHandLight.SetActive(value: false);
		spitterHeadInHandVFX.SetActive(value: false);
	}

	private void EnableVFXForShoulderHead()
	{
		headLightReset = false;
		spitterHeadOnShoulders.SetActive(value: true);
		spitterHeadOnShouldersLight.SetActive(value: true);
		spitterHeadOnShouldersVFX.SetActive(value: true);
		spitterHeadInHand.SetActive(value: false);
		spitterHeadInHandLight.SetActive(value: false);
		spitterHeadInHandVFX.SetActive(value: false);
	}

	private void EnableVFXForHeadInHand()
	{
		headLightReset = false;
		spitterHeadOnShoulders.SetActive(value: false);
		spitterHeadOnShouldersLight.SetActive(value: false);
		spitterHeadOnShouldersVFX.SetActive(value: false);
		spitterHeadInHand.SetActive(value: true);
		spitterHeadInHandLight.SetActive(value: true);
		spitterHeadInHandVFX.SetActive(value: true);
	}

	private void DisableHeadInHand()
	{
		headLightReset = false;
		spitterHeadInHand.SetActive(value: false);
	}

	private void DisableHeadOnShoulderAndHeadInHand()
	{
		headLightReset = false;
		headRemoved = false;
		spitterHeadOnShoulders.SetActive(value: false);
		spitterHeadOnShouldersLight.SetActive(value: false);
		spitterHeadOnShouldersVFX.SetActive(value: false);
		spitterHeadInHand.SetActive(value: false);
		spitterHeadInHandLight.SetActive(value: false);
		spitterHeadInHandVFX.SetActive(value: false);
	}

	private void Awake()
	{
		rigidBody = GetComponent<Rigidbody>();
		colliders = new List<Collider>(4);
		GetComponentsInChildren(colliders);
		visibilityLayerMask = LayerMask.GetMask("Default");
		senseNearby.Setup(headTransform, entity);
		if (armor != null)
		{
			armor.SetHp(0);
		}
		navAgent.updateRotation = false;
		agent.onBodyStateChanged += OnNetworkBodyStateChange;
		agent.onBehaviorStateChanged += OnNetworkBehaviorStateChange;
	}

	public void OnEntityInit()
	{
		abilityStagger.Setup(agent, anim, audioSource, base.transform, headTransform, null);
		abilityInvestigate.Setup(agent, anim, audioSource, base.transform, headTransform, null);
		abilityPatrol.Setup(agent, anim, audioSource, base.transform, headTransform, null);
		abilityFlashed.Setup(agent, anim, audioSource, base.transform, headTransform, null);
		abilityKeepDistance.Setup(agent, anim, audioSource, base.transform, headTransform, null);
		abilityJump.Setup(agent, anim, audioSource, base.transform, headTransform, null);
		Setup(entity.createData);
		if ((bool)entity && (bool)entity.manager && (bool)entity.manager.ghostReactorManager && (bool)entity.manager.ghostReactorManager.reactor)
		{
			foreach (GRBonusEntry enemyGlobalBonuse in entity.manager.ghostReactorManager.reactor.GetCurrLevelGenConfig().enemyGlobalBonuses)
			{
				attributes.AddBonus(enemyGlobalBonuse);
			}
		}
		agent.navAgent.autoTraverseOffMeshLink = false;
		agent.onJumpRequested += OnAgentJumpRequested;
	}

	public void OnEntityDestroy()
	{
	}

	public void OnEntityStateChange(long prevState, long nextState)
	{
	}

	private void OnDestroy()
	{
		agent.onBodyStateChanged -= OnNetworkBodyStateChange;
		agent.onBehaviorStateChanged -= OnNetworkBehaviorStateChange;
		DestroyProjectile();
	}

	public void Setup(long entityCreateData)
	{
		SetPatrolPath(entityCreateData);
		if (abilityPatrol.HasValidPatrolPath())
		{
			SetBehavior(Behavior.Patrol, force: true);
		}
		else
		{
			SetBehavior(Behavior.Idle, force: true);
		}
		if (attributes.CalculateFinalValueForAttribute(GRAttributeType.ArmorMax) > 0)
		{
			SetBodyState(BodyState.Shell, force: true);
		}
		else
		{
			SetBodyState(BodyState.Bones, force: true);
		}
		abilityDie.Setup(agent, anim, audioSource, base.transform, headTransform, null);
	}

	private void OnAgentJumpRequested(Vector3 start, Vector3 end, float heightScale, float speedScale)
	{
		abilityJump.SetupJump(start, end, heightScale, speedScale);
		SetBehavior(Behavior.Jump);
	}

	public void OnNetworkBehaviorStateChange(byte newState)
	{
		if (newState >= 0 && newState < 11)
		{
			SetBehavior((Behavior)newState);
		}
	}

	public void OnNetworkBodyStateChange(byte newState)
	{
		if (newState >= 0 && newState < 3)
		{
			SetBodyState((BodyState)newState);
		}
	}

	public void SetPatrolPath(long entityCreateData)
	{
		abilityPatrol.SetPatrolPath(GhostReactorManager.Get(entity).reactor.GetPatrolPath(entityCreateData));
	}

	public void SetHP(int hp)
	{
		this.hp = hp;
	}

	public bool TrySetBehavior(Behavior newBehavior)
	{
		if (currBehavior == Behavior.Jump && newBehavior == Behavior.Stagger)
		{
			return false;
		}
		SetBehavior(newBehavior);
		return true;
	}

	public void SetBehavior(Behavior newBehavior, bool force = false)
	{
		if (currBehavior == newBehavior && !force)
		{
			return;
		}
		switch (currBehavior)
		{
		case Behavior.Dying:
			abilityDie.Stop();
			break;
		case Behavior.Stagger:
			abilityStagger.Stop();
			break;
		case Behavior.Flashed:
			abilityFlashed.Stop();
			break;
		case Behavior.SeekRangedAttackPosition:
			if (newBehavior != Behavior.RangedAttack)
			{
				SoftResetThrowableHead();
			}
			break;
		case Behavior.RangedAttack:
			if (newBehavior != Behavior.RangedAttackCooldown)
			{
				ForceResetThrowableHead();
			}
			break;
		case Behavior.RangedAttackCooldown:
			ForceResetThrowableHead();
			abilityKeepDistance.Stop();
			break;
		case Behavior.Investigate:
			abilityInvestigate.Stop();
			break;
		case Behavior.Patrol:
			abilityPatrol.Stop();
			break;
		case Behavior.Jump:
			abilityJump.Stop();
			break;
		}
		currBehavior = newBehavior;
		switch (currBehavior)
		{
		case Behavior.Dying:
			abilityDie.Start();
			if (entity.IsAuthority())
			{
				entity.manager.RequestCreateItem(corePrefab.gameObject.name.GetStaticHash(), coreMarker.position, coreMarker.rotation, 0L);
			}
			break;
		case Behavior.Stagger:
			abilityStagger.Start();
			break;
		case Behavior.Flashed:
			abilityFlashed.Start();
			break;
		case Behavior.Patrol:
			targetPlayer = null;
			abilityPatrol.Start();
			break;
		case Behavior.Search:
			targetPlayer = null;
			PlayAnim("GREnemyRangedWalk", 0.1f, 1f);
			navAgent.speed = attributes.CalculateFinalFloatValueForAttribute(GRAttributeType.PatrolSpeed);
			lastMoving = false;
			break;
		case Behavior.SeekRangedAttackPosition:
			PlayAnim("GREnemyRangedWalk", 0.1f, 1f);
			navAgent.speed = attributes.CalculateFinalFloatValueForAttribute(GRAttributeType.ChaseSpeed);
			EnableVFXForShoulderHead();
			chaseAbilitySound.Play(audioSecondarySource);
			break;
		case Behavior.RangedAttack:
			PlayAnim("GREnemyRangedAttack01", 0.1f, 1f);
			navAgent.speed = 0f;
			navAgent.velocity = Vector3.zero;
			headRemovaltime = PhotonNetwork.Time + (double)headRemovalFrame;
			attackAbilitySound.Play(audioSource);
			break;
		case Behavior.RangedAttackCooldown:
			lastMoving = true;
			abilityKeepDistance.SetTargetPlayer(targetPlayer);
			abilityKeepDistance.Start();
			break;
		case Behavior.Idle:
			targetPlayer = null;
			PlayAnim("GREnemyRangedIdleSearch", 0.1f, 1f);
			break;
		case Behavior.Investigate:
			abilityInvestigate.Start();
			break;
		case Behavior.Jump:
			abilityJump.Start();
			break;
		}
		RefreshBody();
		if (entity.IsAuthority())
		{
			agent.RequestBehaviorChange((byte)currBehavior);
		}
	}

	private void PlayAnim(string animName, float blendTime, float speed)
	{
		if (anim != null)
		{
			anim[animName].speed = speed;
			anim.CrossFade(animName, blendTime);
		}
	}

	public void SetBodyState(BodyState newBodyState, bool force = false)
	{
		if (currBodyState == newBodyState && !force)
		{
			return;
		}
		switch (currBodyState)
		{
		case BodyState.Destroyed:
		{
			ForceResetThrowableHead();
			for (int i = 0; i < colliders.Count; i++)
			{
				colliders[i].enabled = true;
			}
			break;
		}
		case BodyState.Bones:
			hp = attributes.CalculateFinalValueForAttribute(GRAttributeType.HPMax);
			break;
		case BodyState.Shell:
			hp = attributes.CalculateFinalValueForAttribute(GRAttributeType.ArmorMax);
			break;
		}
		currBodyState = newBodyState;
		switch (currBodyState)
		{
		case BodyState.Destroyed:
			DisableHeadOnShoulderAndHeadInHand();
			GhostReactorManager.Get(entity).ReportEnemyDeath();
			break;
		case BodyState.Bones:
			hp = attributes.CalculateFinalValueForAttribute(GRAttributeType.HPMax);
			break;
		case BodyState.Shell:
			hp = attributes.CalculateFinalValueForAttribute(GRAttributeType.ArmorMax);
			break;
		}
		RefreshBody();
		if (entity.IsAuthority())
		{
			agent.RequestStateChange((byte)newBodyState);
		}
	}

	private void RefreshBody()
	{
		switch (currBodyState)
		{
		case BodyState.Destroyed:
			armor.SetHp(0);
			GREnemy.HideRenderers(bones, hide: true);
			GREnemy.HideRenderers(always, hide: true);
			DisableHeadOnShoulderAndHeadInHand();
			break;
		case BodyState.Bones:
			armor.SetHp(0);
			GREnemy.HideRenderers(bones, hide: false);
			GREnemy.HideRenderers(always, hide: false);
			break;
		case BodyState.Shell:
			armor.SetHp(hp);
			GREnemy.HideRenderers(bones, hide: true);
			GREnemy.HideRenderers(always, hide: false);
			break;
		}
	}

	private void Update()
	{
		if (entity.IsAuthority())
		{
			OnUpdateAuthority(Time.deltaTime);
		}
		else
		{
			OnUpdateRemote(Time.deltaTime);
		}
		UpdateShared();
	}

	public void OnEntityThink(float dt)
	{
		if (entity.IsAuthority() && !GhostReactorManager.AggroDisabled)
		{
			switch (currBehavior)
			{
			case Behavior.RangedAttackCooldown:
				abilityKeepDistance.Think(dt);
				UpdateTarget();
				break;
			case Behavior.Idle:
			case Behavior.Patrol:
			case Behavior.Search:
			case Behavior.Investigate:
				UpdateTarget();
				break;
			}
		}
	}

	private void UpdateTarget()
	{
		bestTargetPlayer = null;
		bestTargetNetPlayer = null;
		tempRigs.Clear();
		tempRigs.Add(VRRig.LocalRig);
		VRRigCache.Instance.GetAllUsedRigs(tempRigs);
		senseNearby.UpdateNearby(tempRigs, senseLineOfSight);
		float outDistanceSq;
		VRRig vRRig = senseNearby.PickClosest(out outDistanceSq);
		if (vRRig != null)
		{
			GRPlayer component = vRRig.GetComponent<GRPlayer>();
			if ((object)component != null && component.State != GRPlayer.GRPlayerState.Ghost)
			{
				bestTargetPlayer = component;
				bestTargetNetPlayer = vRRig.OwningNetPlayer;
				lastSeenTargetTime = Time.timeAsDouble;
				lastSeenTargetPosition = vRRig.transform.position;
			}
		}
	}

	private void ChooseNewBehavior()
	{
		if (bestTargetPlayer != null && Time.timeAsDouble - lastSeenTargetTime < (double)sightLostFollowStopTime)
		{
			targetPlayer = bestTargetNetPlayer;
			lastSeenTargetTime = Time.timeAsDouble;
			investigateLocation = null;
			SetBehavior(Behavior.SeekRangedAttackPosition);
			return;
		}
		if (Time.timeAsDouble - lastSeenTargetTime < (double)searchTime)
		{
			SetBehavior(Behavior.Search);
			return;
		}
		investigateLocation = AbilityHelperFunctions.GetLocationToInvestigate(base.transform.position, hearingRadius, investigateLocation);
		if (investigateLocation.HasValue)
		{
			abilityInvestigate.SetTargetPos(investigateLocation.Value);
			SetBehavior(Behavior.Investigate);
		}
		else if (abilityPatrol.HasValidPatrolPath())
		{
			SetBehavior(Behavior.Patrol);
		}
		else
		{
			SetBehavior(Behavior.Idle);
		}
	}

	private void OnUpdateAuthority(float dt)
	{
		switch (currBehavior)
		{
		case Behavior.Idle:
			ChooseNewBehavior();
			break;
		case Behavior.Patrol:
			abilityPatrol.UpdateAuthority(dt);
			ChooseNewBehavior();
			break;
		case Behavior.Search:
			UpdateSearch();
			ChooseNewBehavior();
			break;
		case Behavior.Stagger:
			abilityStagger.UpdateAuthority(dt);
			if (abilityStagger.IsDone())
			{
				if (targetPlayer == null)
				{
					SetBehavior(Behavior.Search);
				}
				else
				{
					SetBehavior(Behavior.SeekRangedAttackPosition);
				}
			}
			break;
		case Behavior.Flashed:
			abilityFlashed.UpdateAuthority(dt);
			if (abilityFlashed.IsDone())
			{
				if (targetPlayer == null)
				{
					SetBehavior(Behavior.Search);
				}
				else
				{
					SetBehavior(Behavior.SeekRangedAttackPosition);
				}
			}
			break;
		case Behavior.Dying:
			abilityDie.UpdateAuthority(dt);
			break;
		case Behavior.SeekRangedAttackPosition:
		{
			if (targetPlayer == null)
			{
				break;
			}
			GRPlayer gRPlayer = GRPlayer.Get(targetPlayer.ActorNumber);
			if (!(gRPlayer != null) || gRPlayer.State != GRPlayer.GRPlayerState.Alive)
			{
				break;
			}
			Vector3 position = gRPlayer.transform.position;
			Vector3 position2 = base.transform.position;
			float magnitude = (position - position2).magnitude;
			if (magnitude > loseSightDist)
			{
				ChooseNewBehavior();
				break;
			}
			float num = Vector3.Distance(position, headTransform.position);
			bool flag = false;
			if (num < sightDist)
			{
				flag = Physics.RaycastNonAlloc(new Ray(headTransform.position, position - headTransform.position), GREnemyChaser.visibilityHits, num, visibilityLayerMask.value, QueryTriggerInteraction.Ignore) < 1;
			}
			if (flag)
			{
				lastSeenTargetPosition = position;
				lastSeenTargetTime = Time.timeAsDouble;
			}
			if (Time.timeAsDouble - lastSeenTargetTime < (double)sightLostFollowStopTime)
			{
				searchPosition = position;
				agent.RequestDestination(lastSeenTargetPosition);
				if (flag)
				{
					rangedTargetPosition = position;
					Vector3 vector = Vector3.up * 0.4f;
					rangedTargetPosition += vector;
					if (magnitude < rangedAttackDistMax)
					{
						behaviorEndTime = Time.timeAsDouble + (double)rangedAttackChargeTime;
						SetBehavior(Behavior.RangedAttack);
						GhostReactorManager.Get(entity).RequestFireProjectile(entity.id, rangedProjectileFirePoint.position, rangedTargetPosition, PhotonNetwork.Time + (double)rangedAttackChargeTime);
					}
				}
			}
			else
			{
				ChooseNewBehavior();
			}
			break;
		}
		case Behavior.RangedAttack:
			if (!(Time.timeAsDouble > behaviorEndTime))
			{
				break;
			}
			if (targetPlayer != null)
			{
				GRPlayer gRPlayer2 = GRPlayer.Get(targetPlayer.ActorNumber);
				if (gRPlayer2 != null && gRPlayer2.State == GRPlayer.GRPlayerState.Alive)
				{
					rangedTargetPosition = gRPlayer2.transform.position;
				}
			}
			SetBehavior(Behavior.RangedAttackCooldown);
			behaviorEndTime = Time.timeAsDouble + (double)rangedAttackRecoverTime;
			break;
		case Behavior.RangedAttackCooldown:
			if (Time.timeAsDouble > behaviorEndTime)
			{
				SetBehavior(Behavior.SeekRangedAttackPosition);
				behaviorEndTime = Time.timeAsDouble;
			}
			else
			{
				abilityKeepDistance.UpdateAuthority(dt);
			}
			break;
		case Behavior.Investigate:
			abilityInvestigate.UpdateAuthority(dt);
			if (GhostReactorManager.noiseDebugEnabled)
			{
				DebugUtil.DrawLine(base.transform.position, abilityInvestigate.GetTargetPos(), Color.green);
			}
			ChooseNewBehavior();
			break;
		case Behavior.Jump:
			abilityJump.UpdateAuthority(dt);
			if (abilityJump.IsDone())
			{
				ChooseNewBehavior();
			}
			break;
		}
		GameAgent.UpdateFacing(base.transform, navAgent, targetPlayer, turnSpeed);
	}

	private void OnUpdateRemote(float dt)
	{
		switch (currBehavior)
		{
		case Behavior.Stagger:
			abilityStagger.UpdateRemote(dt);
			break;
		case Behavior.Flashed:
			abilityFlashed.UpdateRemote(dt);
			break;
		case Behavior.Dying:
			abilityDie.UpdateRemote(dt);
			break;
		case Behavior.Patrol:
			abilityPatrol.UpdateRemote(dt);
			break;
		case Behavior.RangedAttackCooldown:
			abilityKeepDistance.UpdateRemote(dt);
			break;
		case Behavior.Investigate:
			abilityInvestigate.UpdateRemote(dt);
			if (GhostReactorManager.noiseDebugEnabled)
			{
				DebugUtil.DrawLine(base.transform.position, abilityInvestigate.GetTargetPos(), Color.green);
			}
			break;
		case Behavior.Jump:
			abilityJump.UpdateRemote(dt);
			break;
		case Behavior.Search:
		case Behavior.SeekRangedAttackPosition:
		case Behavior.RangedAttack:
			break;
		}
	}

	public void UpdateShared()
	{
		if (rangedAttackQueued)
		{
			if (!headRemoved && currBehavior == Behavior.RangedAttack && PhotonNetwork.Time >= headRemovaltime)
			{
				headRemoved = true;
				EnableVFXForHeadInHand();
			}
			if (PhotonNetwork.Time > queuedFiringTime)
			{
				rangedAttackQueued = false;
				FireRangedAttack(queuedFiringPosition, queuedTargetPosition);
			}
		}
		if (headLightReset && Time.timeAsDouble > spitterLightTurnOffTime)
		{
			spitterHeadOnShouldersLight.SetActive(value: false);
			headLightReset = false;
		}
	}

	private void UpdateSearch()
	{
		Vector3 vector = searchPosition - base.transform.position;
		if (new Vector3(vector.x, 0f, vector.z).sqrMagnitude < 0.15f)
		{
			Vector3 vector2 = lastSeenTargetPosition - searchPosition;
			vector2.y = 0f;
			searchPosition = lastSeenTargetPosition + vector2;
		}
		if (IsMoving())
		{
			if (!lastMoving)
			{
				PlayAnim("GREnemyRangedWalk", 0.1f, 1f);
				lastMoving = true;
			}
		}
		else if (lastMoving)
		{
			PlayAnim("GREnemyRangedWalk", 0.1f, 1f);
			lastMoving = false;
		}
		agent.RequestDestination(searchPosition);
		if (Time.timeAsDouble - lastSeenTargetTime > (double)searchTime)
		{
			ChooseNewBehavior();
		}
	}

	private void OnHitByClub(GRTool tool, GameHitData hit)
	{
		if (currBodyState == BodyState.Bones)
		{
			hp -= hit.hitAmount;
			audioSource.PlayOneShot(damagedSound, damagedSoundVolume);
			if (fxDamaged != null)
			{
				fxDamaged.SetActive(value: false);
				fxDamaged.SetActive(value: true);
			}
			if (hp <= 0)
			{
				abilityDie.SetInstigatingPlayerIndex(entity.GetLastHeldByPlayerForEntityID(hit.hitByEntityId));
				SetBodyState(BodyState.Destroyed);
				SetBehavior(Behavior.Dying);
				return;
			}
			lastSeenTargetPosition = tool.transform.position;
			lastSeenTargetTime = Time.timeAsDouble;
			Vector3 vector = lastSeenTargetPosition - base.transform.position;
			vector.y = 0f;
			searchPosition = lastSeenTargetPosition + vector.normalized * 1.5f;
			abilityStagger.SetStaggerVelocity(hit.hitImpulse);
			TrySetBehavior(Behavior.Stagger);
		}
		else if (currBodyState == BodyState.Shell && armor != null)
		{
			armor.PlayBlockFx(hit.hitEntityPosition);
		}
	}

	public void InstantDeath()
	{
		hp = 0;
		SetBodyState(BodyState.Destroyed);
		SetBehavior(Behavior.Dying);
	}

	private void OnHitByFlash(GRTool tool, GameHitData hit)
	{
		if (currBodyState == BodyState.Shell)
		{
			hp -= hit.hitAmount;
			if (armor != null)
			{
				armor.SetHp(hp);
			}
			if (hp <= 0)
			{
				if (armor != null)
				{
					armor.PlayDestroyFx(armor.transform.position);
				}
				SetBodyState(BodyState.Bones);
				if (tool.gameEntity.IsHeldByLocalPlayer())
				{
					PlayerGameEvents.MiscEvent("GRArmorBreak_" + base.name);
				}
				if (tool.HasUpgradeInstalled(GRToolProgressionManager.ToolParts.FlashDamage3))
				{
					armor.FragmentArmor();
				}
			}
			else if (tool != null)
			{
				if (armor != null)
				{
					armor.PlayHitFx(armor.transform.position);
				}
				lastSeenTargetPosition = tool.transform.position;
				lastSeenTargetTime = Time.timeAsDouble;
				Vector3 vector = lastSeenTargetPosition - base.transform.position;
				vector.y = 0f;
				searchPosition = lastSeenTargetPosition + vector.normalized * 1.5f;
				SetBehavior(Behavior.Search);
				RefreshBody();
			}
			else
			{
				if (armor != null)
				{
					armor.PlayHitFx(armor.transform.position);
				}
				RefreshBody();
			}
		}
		GRToolFlash component = tool.GetComponent<GRToolFlash>();
		if (component != null)
		{
			abilityFlashed.SetStunTime(component.stunDuration);
		}
		SetBehavior(Behavior.Flashed);
	}

	public void OnHitByShield(GRTool tool, GameHitData hit)
	{
		OnHitByClub(tool, hit);
	}

	public void OnGameEntitySerialize(BinaryWriter writer)
	{
		byte value = (byte)currBehavior;
		byte value2 = (byte)currBodyState;
		byte value3 = (byte)abilityPatrol.nextPatrolNode;
		int value4 = ((targetPlayer == null) ? (-1) : targetPlayer.ActorNumber);
		writer.Write(value);
		writer.Write(value2);
		writer.Write(hp);
		writer.Write(value3);
		writer.Write(value4);
	}

	public void OnGameEntityDeserialize(BinaryReader reader)
	{
		Behavior newBehavior = (Behavior)reader.ReadByte();
		BodyState newBodyState = (BodyState)reader.ReadByte();
		int hP = reader.ReadInt32();
		byte b = reader.ReadByte();
		int playerID = reader.ReadInt32();
		SetPatrolPath((int)entity.createData);
		abilityPatrol.SetNextPatrolNode(b);
		SetHP(hP);
		SetBehavior(newBehavior, force: true);
		SetBodyState(newBodyState, force: true);
		targetPlayer = NetworkSystem.Instance.GetPlayer(playerID);
	}

	public bool IsHitValid(GameHitData hit)
	{
		return true;
	}

	public void OnHit(GameHitData hit)
	{
		GameHitType hitTypeId = (GameHitType)hit.hitTypeId;
		GRTool gameComponent = entity.manager.GetGameComponent<GRTool>(hit.hitByEntityId);
		if (gameComponent != null)
		{
			switch (hitTypeId)
			{
			case GameHitType.Club:
				OnHitByClub(gameComponent, hit);
				break;
			case GameHitType.Flash:
				OnHitByFlash(gameComponent, hit);
				break;
			case GameHitType.Shield:
				OnHitByShield(gameComponent, hit);
				break;
			}
		}
	}

	public void RequestRangedAttack(Vector3 firingPosition, Vector3 targetPosition, double fireTime)
	{
		rangedAttackQueued = true;
		queuedFiringTime = fireTime;
		queuedFiringPosition = firingPosition;
		queuedTargetPosition = targetPosition;
	}

	private void DestroyProjectile()
	{
		if (entity.IsAuthority() && rangedProjectileInstance != null)
		{
			GameEntity component = rangedProjectileInstance.GetComponent<GameEntity>();
			if (component != null)
			{
				component.manager.RequestDestroyItem(component.id);
			}
		}
	}

	private void FireRangedAttack(Vector3 launchPosition, Vector3 targetPosition)
	{
		if (entity.IsAuthority())
		{
			DisableHeadInHand();
			DestroyProjectile();
			if (CalculateLaunchDirection(launchPosition, targetPosition, projectileSpeed, out var direction))
			{
				entity.manager.RequestCreateItem(rangedProjectilePrefab.name.GetStaticHash(), launchPosition, Quaternion.LookRotation(direction, Vector3.up), entity.GetNetId());
			}
		}
	}

	public static bool CalculateLaunchDirection(Vector3 startPos, Vector3 targetPos, float speed, out Vector3 direction)
	{
		direction = Vector3.zero;
		Vector3 vector = targetPos - startPos;
		Vector3 vector2 = new Vector3(vector.x, 0f, vector.z);
		float magnitude = vector2.magnitude;
		Vector3 normalized = vector2.normalized;
		float y = vector.y;
		float num = 9.8f;
		float num2 = speed * speed;
		float num3 = num2 * num2 - num * (num * magnitude * magnitude + 2f * y * num2);
		if (num3 < 0f)
		{
			return false;
		}
		float num4 = Mathf.Sqrt(num3);
		float num5 = (num2 + num4) / (num * magnitude);
		float num6 = (num2 - num4) / (num * magnitude);
		float num7 = num2 / (num5 * num5 + 1f);
		float num8 = num2 / (num6 * num6 + 1f);
		float num9 = (false ? Mathf.Min(num7, num8) : Mathf.Max(num7, num8));
		float num10 = ((0 == 0) ? ((num7 > num8) ? Mathf.Sign(num5) : Mathf.Sign(num6)) : ((num7 < num8) ? Mathf.Sign(num5) : Mathf.Sign(num6)));
		float num11 = Mathf.Sqrt(num9);
		float num12 = Mathf.Sqrt(Mathf.Abs(num2 - num9));
		direction = (normalized * num11 + new Vector3(0f, num12 * num10, 0f)).normalized;
		return true;
	}

	public void OnProjectileInit(GRRangedEnemyProjectile projectile)
	{
		rangedProjectileInstance = projectile.gameObject;
	}

	public void OnProjectileHit(GRRangedEnemyProjectile projectile, Collision collision)
	{
	}

	public void GetDebugTextLines(out List<string> strings)
	{
		strings = new List<string>();
		strings.Add($"State: <color=\"yellow\">{currBehavior.ToString()}<color=\"white\"> HP: <color=\"yellow\">{hp}<color=\"white\">");
		strings.Add($"speed: <color=\"yellow\">{navAgent.speed}<color=\"white\"> patrol node:<color=\"yellow\">{abilityPatrol.nextPatrolNode}/{((abilityPatrol.GetPatrolPath() != null) ? abilityPatrol.GetPatrolPath().patrolNodes.Count : 0)}<color=\"white\">");
		if (targetPlayer != null)
		{
			GRPlayer gRPlayer = GRPlayer.Get(targetPlayer.ActorNumber);
			if (gRPlayer != null)
			{
				float magnitude = (gRPlayer.transform.position - base.transform.position).magnitude;
				strings.Add($"TargetDis: <color=\"yellow\">{magnitude}<color=\"white\"> ");
			}
		}
	}
}

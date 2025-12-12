using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.AI;

public class GREnemyBossMoon : MonoBehaviour, IGameEntityComponent, IGameEntitySerialize, IGameHittable, IGameAgentComponent, IGameEntityDebugComponent
{
	[Serializable]
	public class Eye
	{
	}

	public enum Behavior
	{
		Idle,
		Patrol,
		Stagger,
		Dying,
		Chase,
		Search,
		AttackTentacle00,
		AttackTentacle01,
		AttackTentacle02,
		AttackTentacle03,
		AttackTentacle04,
		Attack,
		AttackDisco,
		AttackSlamdown,
		Flashed,
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

	[SerializeField]
	private GRAttributes attributes;

	public GRSenseNearby senseNearby;

	public GRSenseLineOfSight senseLineOfSight;

	public Animation anim;

	public GRAbilityAttackSimple abilityAttackTentacle00;

	public GRAbilityAttackSimple abilityAttackTentacle01;

	public GRAbilityAttackSimple abilityAttackTentacle02;

	public GRAbilityAttackSimple abilityAttackTentacle03;

	public GRAbilityAttackSimple abilityAttackTentacle04;

	public GRAbilityAttackSimple abilityAttackTongue01;

	private GRAbilityBase[] abilities;

	private GRAbilityBase currAbility;

	public List<Eye> eyes;

	public GRAbilityAgent abilityAgent;

	public GRAbilityIdle abilityIdle;

	public GRAbilityChase abilityChase;

	public GRAbilityIdle abilitySearch;

	public GRAbilityAttackLaser abilityAttackLaser;

	public GRAbilityAttackSimpleWander abilityAttackDiscoWander;

	public GRAbilityAttackSimple abilityAttackSlamdown;

	public bool allowStagger;

	public GRAbilityStagger abilityStagger;

	public GRAbilityDie abilityDie;

	public GRAbilityPatrol abilityPatrol;

	public GRAbilityFlashed abilityFlashed;

	public List<Renderer> bones;

	public List<Renderer> always;

	public Transform coreMarker;

	public GRCollectible corePrefab;

	public Transform headTransform;

	public float turnSpeed = 540f;

	public SoundBankPlayer chaseSoundBank;

	public float attackRange = 1.5f;

	[ReadOnly]
	[SerializeField]
	private GRPatrolPath patrolPath;

	public NavMeshAgent navAgent;

	public AudioSource audioSource;

	public AudioClip damagedSound;

	public float damagedSoundVolume;

	public List<AudioClip> damagedSounds;

	private int damagedSoundIndex;

	public GameObject fxDamaged;

	private float lastStaggerTime;

	public float staggerImmuneTime = 10f;

	private Transform target;

	[ReadOnly]
	public int hp;

	[ReadOnly]
	public Behavior currBehavior;

	[ReadOnly]
	public BodyState currBodyState;

	[ReadOnly]
	public NetPlayer targetPlayer;

	[ReadOnly]
	public Vector3 lastSeenTargetPosition;

	[ReadOnly]
	public double lastSeenTargetTime;

	[ReadOnly]
	public Vector3 searchPosition;

	private double lastJumpEndtime;

	public bool canChaseJump = true;

	public float chaseJumpDistance = 5f;

	public float chaseJumpMinInterval = 1f;

	public float minChaseJumpDistance = 2f;

	private Rigidbody rigidBody;

	private List<Collider> colliders;

	private float lastHitPlayerTime;

	private float minTimeBetweenHits = 0.5f;

	public float hearingRadius = 5f;

	private static List<VRRig> tempRigs = new List<VRRig>(16);

	private Coroutine tryHitPlayerCoroutine;

	private void Awake()
	{
		rigidBody = GetComponent<Rigidbody>();
		colliders = new List<Collider>(4);
		GetComponentsInChildren(colliders);
		if (armor != null)
		{
			armor.SetHp(0);
		}
		if (navAgent != null)
		{
			navAgent.updateRotation = false;
		}
		agent.onBodyStateChanged += OnNetworkBodyStateChange;
		agent.onBehaviorStateChanged += OnNetworkBehaviorStateChange;
		abilities = new GRAbilityBase[15];
	}

	public void OnEntityInit()
	{
		currAbility = null;
		SetupAbility(Behavior.Idle, abilityIdle, agent, anim, audioSource, null, null, null);
		SetupAbility(Behavior.Chase, abilityChase, agent, anim, audioSource, base.transform, headTransform, senseLineOfSight);
		SetupAbility(Behavior.Search, abilitySearch, agent, anim, audioSource, null, null, null);
		SetupAbility(Behavior.AttackTentacle00, abilityAttackTentacle00, agent, anim, audioSource, base.transform, headTransform, null);
		SetupAbility(Behavior.AttackTentacle01, abilityAttackTentacle01, agent, anim, audioSource, base.transform, headTransform, null);
		SetupAbility(Behavior.AttackTentacle02, abilityAttackTentacle02, agent, anim, audioSource, base.transform, headTransform, null);
		SetupAbility(Behavior.AttackTentacle03, abilityAttackTentacle03, agent, anim, audioSource, base.transform, headTransform, null);
		SetupAbility(Behavior.AttackTentacle04, abilityAttackTentacle04, agent, anim, audioSource, base.transform, headTransform, null);
		SetupAbility(Behavior.Attack, abilityAttackLaser, agent, anim, audioSource, base.transform, headTransform, null);
		SetupAbility(Behavior.AttackDisco, abilityAttackDiscoWander, agent, anim, audioSource, base.transform, headTransform, null);
		SetupAbility(Behavior.AttackSlamdown, abilityAttackSlamdown, agent, anim, audioSource, base.transform, headTransform, null);
		SetupAbility(Behavior.Patrol, abilityPatrol, agent, anim, audioSource, base.transform, null, null);
		SetupAbility(Behavior.Stagger, abilityStagger, agent, anim, audioSource, base.transform, null, null);
		SetupAbility(Behavior.Dying, abilityDie, agent, anim, audioSource, base.transform, null, null);
		SetupAbility(Behavior.Flashed, abilityFlashed, agent, anim, audioSource, base.transform, null, null);
		senseNearby.Setup(headTransform);
		Setup(entity.createData);
		if ((bool)entity && (bool)entity.manager && (bool)entity.manager.ghostReactorManager && (bool)entity.manager.ghostReactorManager.reactor)
		{
			foreach (GRBonusEntry enemyGlobalBonuse in entity.manager.ghostReactorManager.reactor.GetCurrLevelGenConfig().enemyGlobalBonuses)
			{
				attributes.AddBonus(enemyGlobalBonuse);
			}
		}
		if (agent.navAgent != null)
		{
			agent.navAgent.autoTraverseOffMeshLink = false;
		}
		SetBehavior(Behavior.Idle, force: true);
	}

	private void SetupAbility(Behavior behavior, GRAbilityBase ability, GameAgent agent, Animation anim, AudioSource audioSource, Transform root, Transform head, GRSenseLineOfSight lineOfSight)
	{
		abilities[(int)behavior] = ability;
		ability.Setup(agent, anim, audioSource, root, head, lineOfSight);
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
	}

	public void OnNetworkBehaviorStateChange(byte newState)
	{
		if (newState >= 0 && newState < 15)
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
		GRPatrolPath gRPatrolPath = GhostReactorManager.Get(entity).reactor.GetPatrolPath(entityCreateData);
		abilityPatrol.SetPatrolPath(gRPatrolPath);
	}

	public void SetHP(int hp)
	{
		this.hp = hp;
	}

	public bool TrySetBehavior(Behavior newBehavior)
	{
		if (newBehavior == Behavior.Stagger)
		{
			return false;
		}
		if (newBehavior == Behavior.Stagger && Time.time < lastStaggerTime + staggerImmuneTime)
		{
			return false;
		}
		SetBehavior(newBehavior);
		return true;
	}

	public void SetBehavior(Behavior newBehavior, bool force = false)
	{
		if (newBehavior < Behavior.Idle || (int)newBehavior >= abilities.Length)
		{
			Debug.LogErrorFormat("New Behavior Index is invalid {0} {1} {2}", (int)newBehavior, newBehavior, base.gameObject.name);
			return;
		}
		GRAbilityBase gRAbilityBase = abilities[(int)newBehavior];
		if (currBehavior != newBehavior || force)
		{
			if (currAbility != null)
			{
				currAbility.Stop();
			}
			currBehavior = newBehavior;
			currAbility = gRAbilityBase;
			if (currAbility != null)
			{
				currAbility.Start();
			}
			switch (currBehavior)
			{
			case Behavior.Stagger:
				lastStaggerTime = Time.time;
				break;
			case Behavior.Chase:
				abilityChase.SetTargetPlayer(agent.targetPlayer);
				break;
			case Behavior.Attack:
				abilityAttackLaser.SetTargetPlayer(agent.targetPlayer);
				break;
			}
			RefreshBody();
			if (entity.IsAuthority())
			{
				agent.RequestBehaviorChange((byte)currBehavior);
			}
		}
	}

	private int CalcMaxHP()
	{
		float difficultyScalingForCurrentFloor = entity.manager.ghostReactorManager.reactor.difficultyScalingForCurrentFloor;
		return (int)((float)attributes.CalculateFinalValueForAttribute(GRAttributeType.HPMax) * difficultyScalingForCurrentFloor);
	}

	public void SetBodyState(BodyState newBodyState, bool force = false)
	{
		if (currBodyState != newBodyState || force)
		{
			switch (currBodyState)
			{
			case BodyState.Bones:
				hp = CalcMaxHP();
				break;
			case BodyState.Shell:
				hp = attributes.CalculateFinalValueForAttribute(GRAttributeType.ArmorMax);
				break;
			}
			currBodyState = newBodyState;
			switch (currBodyState)
			{
			case BodyState.Destroyed:
				GhostReactorManager.Get(entity).ReportEnemyDeath();
				break;
			case BodyState.Bones:
				hp = CalcMaxHP();
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
	}

	private void RefreshBody()
	{
		switch (currBodyState)
		{
		case BodyState.Destroyed:
			armor.SetHp(0);
			GREnemy.HideRenderers(bones, hide: false);
			GREnemy.HideRenderers(always, hide: false);
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
		OnUpdate(Time.deltaTime);
	}

	public void OnEntityThink(float dt)
	{
		if (!entity.IsAuthority())
		{
			return;
		}
		tempRigs.Clear();
		tempRigs.Add(VRRig.LocalRig);
		VRRigCache.Instance.GetAllUsedRigs(tempRigs);
		senseNearby.UpdateNearby(tempRigs, senseLineOfSight);
		float outDistanceSq;
		VRRig vRRig = senseNearby.PickClosest(out outDistanceSq);
		agent.RequestTarget((vRRig == null) ? null : vRRig.OwningNetPlayer);
		if (currAbility != null)
		{
			currAbility.Think(dt);
		}
		switch (currBehavior)
		{
		case Behavior.Idle:
		case Behavior.Patrol:
		case Behavior.Search:
			ChooseNewBehavior();
			break;
		case Behavior.Chase:
			if (agent.targetPlayer != null)
			{
				abilityChase.SetTargetPlayer(agent.targetPlayer);
			}
			abilityChase.Think(dt);
			ChooseNewBehavior();
			break;
		case Behavior.Stagger:
		case Behavior.Dying:
			break;
		}
	}

	private bool TryChooseAttackBehavior(float toPlayerDistSq)
	{
		bool flag = senseNearby.IsAnyoneNearby(abilityAttackTentacle00.GetRange());
		if (flag && abilityAttackTentacle00.IsCoolDownOver())
		{
			SetBehavior(Behavior.AttackTentacle00);
			return true;
		}
		if (flag && abilityAttackTentacle01.IsCoolDownOver())
		{
			SetBehavior(Behavior.AttackTentacle01);
			return true;
		}
		if (flag && abilityAttackTentacle02.IsCoolDownOver())
		{
			SetBehavior(Behavior.AttackTentacle02);
			return true;
		}
		if (flag && abilityAttackTentacle03.IsCoolDownOver())
		{
			SetBehavior(Behavior.AttackTentacle03);
			return true;
		}
		if (flag && abilityAttackTentacle04.IsCoolDownOver())
		{
			SetBehavior(Behavior.AttackTentacle04);
			return true;
		}
		return false;
	}

	private void ChooseNewBehavior()
	{
		if (!GhostReactorManager.AggroDisabled && senseNearby.IsAnyoneNearby())
		{
			if (agent.targetPlayer != null)
			{
				float magnitude = (GRPlayer.Get(agent.targetPlayer).transform.position - base.transform.position).magnitude;
				if (TryChooseAttackBehavior(magnitude * magnitude))
				{
					return;
				}
			}
			if (!abilityAttackLaser.IsCoolDownOver())
			{
				TrySetBehavior(Behavior.Idle);
			}
			else
			{
				TrySetBehavior(Behavior.Chase);
			}
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

	public void OnUpdate(float dt)
	{
		if (entity.IsAuthority())
		{
			OnUpdateAuthority(dt);
		}
		else
		{
			OnUpdateRemote(dt);
		}
	}

	public void OnUpdateAuthority(float dt)
	{
		if (currAbility != null)
		{
			currAbility.UpdateAuthority(dt);
			if (currAbility.IsDone() && currAbility.IsDone())
			{
				ChooseNewBehavior();
			}
		}
		if (currBehavior == Behavior.Chase && !abilityChase.IsDone())
		{
			GRPlayer gRPlayer = GRPlayer.Get(agent.targetPlayer);
			if (gRPlayer != null)
			{
				float sqrMagnitude = (gRPlayer.transform.position - base.transform.position).sqrMagnitude;
				TryChooseAttackBehavior(sqrMagnitude);
			}
		}
	}

	public void OnUpdateRemote(float dt)
	{
		if (currAbility != null)
		{
			currAbility.UpdateRemote(dt);
		}
	}

	public void OnHitByClub(GRTool tool, GameHitData hit)
	{
		if (currBodyState == BodyState.Bones)
		{
			hp -= hit.hitAmount;
			if (damagedSounds.Count > 0)
			{
				damagedSoundIndex = AbilityHelperFunctions.RandomRangeUnique(0, damagedSounds.Count, damagedSoundIndex);
				audioSource.PlayOneShot(damagedSounds[damagedSoundIndex], damagedSoundVolume);
			}
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
			if (allowStagger)
			{
				abilityStagger.SetStaggerVelocity(hit.hitImpulse);
				TrySetBehavior(Behavior.Stagger);
			}
		}
		else if (currBodyState == BodyState.Shell && armor != null)
		{
			armor.PlayBlockFx(hit.hitEntityPosition);
		}
	}

	public void OnHitByFlash(GRTool grTool, GameHitData hit)
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
				if (grTool.gameEntity.IsHeldByLocalPlayer())
				{
					PlayerGameEvents.MiscEvent("GRArmorBreak_" + base.name);
				}
				if (grTool.HasUpgradeInstalled(GRToolProgressionManager.ToolParts.FlashDamage3))
				{
					armor.FragmentArmor();
				}
			}
			else if (grTool != null)
			{
				if (armor != null)
				{
					armor.PlayHitFx(armor.transform.position);
				}
				lastSeenTargetPosition = grTool.transform.position;
				lastSeenTargetTime = Time.timeAsDouble;
				Vector3 vector = lastSeenTargetPosition - base.transform.position;
				vector.y = 0f;
				searchPosition = lastSeenTargetPosition + vector.normalized * 1.5f;
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
		GRToolFlash component = grTool.GetComponent<GRToolFlash>();
		if (component != null)
		{
			abilityFlashed.SetStunTime(component.stunDuration);
		}
		TrySetBehavior(Behavior.Flashed);
	}

	public void OnHitByShield(GRTool tool, GameHitData hit)
	{
		OnHitByClub(tool, hit);
	}

	private void OnTriggerEnter(Collider collider)
	{
		if (currBodyState == BodyState.Destroyed || (currBehavior != Behavior.Attack && currBehavior != Behavior.AttackDisco && currBehavior != Behavior.AttackSlamdown))
		{
			return;
		}
		GRShieldCollider component = collider.GetComponent<GRShieldCollider>();
		if (component != null)
		{
			GameHittable component2 = GetComponent<GameHittable>();
			component.BlockHittable(headTransform.position, base.transform.forward, component2);
			return;
		}
		Rigidbody attachedRigidbody = collider.attachedRigidbody;
		if (!(attachedRigidbody != null))
		{
			return;
		}
		GRPlayer component3 = attachedRigidbody.GetComponent<GRPlayer>();
		if (component3 != null && component3.gamePlayer.IsLocal() && Time.time > lastHitPlayerTime + minTimeBetweenHits)
		{
			if (tryHitPlayerCoroutine != null)
			{
				StopCoroutine(tryHitPlayerCoroutine);
			}
			tryHitPlayerCoroutine = StartCoroutine(TryHitPlayer(component3));
		}
		GRBreakable component4 = attachedRigidbody.GetComponent<GRBreakable>();
		GameHittable component5 = attachedRigidbody.GetComponent<GameHittable>();
		if (component4 != null && component5 != null)
		{
			GameHitData gameHitData = default(GameHitData);
			gameHitData.hitTypeId = 0;
			gameHitData.hitEntityId = component5.gameEntity.id;
			gameHitData.hitByEntityId = entity.id;
			gameHitData.hitEntityPosition = component4.transform.position;
			gameHitData.hitImpulse = Vector3.zero;
			gameHitData.hitPosition = component4.transform.position;
			GameHitData hitData = gameHitData;
			component5.RequestHit(hitData);
		}
	}

	private IEnumerator TryHitPlayer(GRPlayer player)
	{
		yield return new WaitForUpdate();
		if ((currBehavior == Behavior.Attack || currBehavior == Behavior.AttackDisco || currBehavior == Behavior.AttackSlamdown) && player != null && player.gamePlayer.IsLocal() && Time.time > lastHitPlayerTime + minTimeBetweenHits)
		{
			lastHitPlayerTime = Time.time;
			Vector3 vector = player.transform.position - base.transform.position;
			vector.y = 0f;
			vector = vector.normalized * 6f;
			GhostReactorManager.Get(entity).RequestEnemyHitPlayer(GhostReactor.EnemyType.Chaser, entity.id, player, base.transform.position, vector);
		}
	}

	public void GetDebugTextLines(out List<string> strings)
	{
		strings = new List<string>();
		strings.Add($"State: <color=\"yellow\">{currBehavior.ToString()}<color=\"white\"> HP: <color=\"yellow\">{hp}<color=\"white\">");
		float num = ((navAgent == null) ? 0f : navAgent.speed);
		strings.Add($"speed: <color=\"yellow\">{num}<color=\"white\"> patrol node:<color=\"yellow\">{abilityPatrol.nextPatrolNode}/{((abilityPatrol.GetPatrolPath() != null) ? abilityPatrol.GetPatrolPath().patrolNodes.Count : 0)}<color=\"white\">");
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
		byte nextPatrolNode = reader.ReadByte();
		int playerID = reader.ReadInt32();
		SetPatrolPath(entity.createData);
		abilityPatrol.SetNextPatrolNode(nextPatrolNode);
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
}

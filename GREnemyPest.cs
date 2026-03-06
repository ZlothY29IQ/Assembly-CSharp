using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.AI;

public class GREnemyPest : MonoBehaviour, IGameEntityComponent, IGameEntitySerialize, IGameHittable, IGameAgentComponent, IGameEntityDebugComponent, ITickSystemTick
{
	public enum Behavior
	{
		Idle,
		Wander,
		Chase,
		Attack,
		Stagger,
		Grabbed,
		Thrown,
		Destroyed,
		Investigate,
		Jump,
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

	public GRAttributes attributes;

	public Animation anim;

	public GRSenseNearby senseNearby;

	public GRSenseLineOfSight senseLineOfSight;

	public GRAbilityIdle abilityIdle;

	public GRAbilityChase abilityChase;

	public GRAbilityWander abilityWander;

	public GRAbilityAttackJump abilityAttack;

	public GRAbilityStagger abilityStagger;

	public GRAbilityStagger abilityFlashed;

	public GRAbilityDie abilityDie;

	public GRAbilityGrabbed abilityGrabbed;

	public GRAbilityThrown abilityThrown;

	public AbilitySound spawnSound;

	public GRAbilityMoveToTarget abilityInvestigate;

	public GRAbilityJump abilityJump;

	public List<GameObject> bonesStateVisibleObjects;

	public List<GameObject> alwaysVisibleObjects;

	public Transform coreMarker;

	public GRCollectible corePrefab;

	public Transform headTransform;

	public float attackRange = 2f;

	public List<VRRig> rigsNearby;

	public NavMeshAgent navAgent;

	public AudioSource audioSource;

	public float hearingRadius = 5f;

	private Vector3? investigateLocation;

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
	public Vector3 searchPosition;

	[ReadOnly]
	public double behaviorStartTime;

	private Rigidbody rigidBody;

	private List<Collider> colliders;

	private float lastHitPlayerTime;

	private float minTimeBetweenHits = 0.5f;

	private static List<VRRig> tempRigs = new List<VRRig>(16);

	private Coroutine tryHitPlayerCoroutine;

	public bool TickRunning { get; set; }

	private void Awake()
	{
		rigidBody = GetComponent<Rigidbody>();
		colliders = new List<Collider>(4);
		GetComponentsInChildren(colliders);
		if (armor != null)
		{
			armor.SetHp(0);
		}
		navAgent.updateRotation = false;
		behaviorStartTime = -1.0;
		agent.onBehaviorStateChanged += OnNetworkBehaviorStateChange;
		senseNearby.Setup(headTransform, entity);
		GameEntity gameEntity = entity;
		gameEntity.OnGrabbed = (Action)Delegate.Combine(gameEntity.OnGrabbed, new Action(OnGrabbed));
		GameEntity gameEntity2 = entity;
		gameEntity2.OnReleased = (Action)Delegate.Combine(gameEntity2.OnReleased, new Action(OnReleased));
		Invoke("PlaySpawnAudio", 0.1f);
	}

	private void OnEnable()
	{
		TickSystem<object>.AddTickCallback(this);
	}

	private void OnDisable()
	{
		TickSystem<object>.RemoveTickCallback(this);
	}

	private void PlaySpawnAudio()
	{
		spawnSound.Play(null);
	}

	public void OnEntityInit()
	{
		abilityIdle.Setup(agent, anim, audioSource, base.transform, headTransform, senseLineOfSight);
		abilityChase.Setup(agent, anim, audioSource, base.transform, headTransform, senseLineOfSight);
		abilityAttack.Setup(agent, anim, audioSource, base.transform, headTransform, senseLineOfSight);
		abilityWander.Setup(agent, anim, audioSource, base.transform, headTransform, senseLineOfSight);
		abilityDie.Setup(agent, anim, audioSource, base.transform, headTransform, senseLineOfSight);
		abilityGrabbed.Setup(agent, anim, audioSource, base.transform, headTransform, senseLineOfSight);
		abilityThrown.Setup(agent, anim, audioSource, base.transform, headTransform, senseLineOfSight);
		abilityStagger.Setup(agent, anim, audioSource, base.transform, headTransform, senseLineOfSight);
		abilityFlashed.Setup(agent, anim, audioSource, base.transform, headTransform, senseLineOfSight);
		abilityInvestigate.Setup(agent, anim, audioSource, base.transform, headTransform, senseLineOfSight);
		abilityJump.Setup(agent, anim, audioSource, base.transform, headTransform, senseLineOfSight);
		SetBehavior(Behavior.Wander);
		if ((bool)entity && (bool)entity.manager && (bool)entity.manager.ghostReactorManager && (bool)entity.manager.ghostReactorManager.reactor)
		{
			foreach (GRBonusEntry enemyGlobalBonuse in entity.manager.ghostReactorManager.reactor.GetCurrLevelGenConfig().enemyGlobalBonuses)
			{
				attributes.AddBonus(enemyGlobalBonuse);
			}
		}
		navAgent.speed = attributes.CalculateFinalFloatValueForAttribute(GRAttributeType.PatrolSpeed);
		SetHP(attributes.CalculateFinalValueForAttribute(GRAttributeType.HPMax));
		agent.navAgent.autoTraverseOffMeshLink = false;
		agent.onJumpRequested += OnAgentJumpRequested;
		if (attributes.CalculateFinalValueForAttribute(GRAttributeType.ArmorMax) > 0)
		{
			SetBodyState(BodyState.Shell, force: true);
		}
		else
		{
			SetBodyState(BodyState.Bones, force: true);
		}
	}

	public void OnEntityDestroy()
	{
	}

	public void OnEntityStateChange(long prevState, long nextState)
	{
	}

	private void OnDestroy()
	{
		agent.onBehaviorStateChanged -= OnNetworkBehaviorStateChange;
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
		if (currBehavior != newBehavior || force)
		{
			switch (currBehavior)
			{
			case Behavior.Chase:
				abilityChase.Stop();
				break;
			case Behavior.Stagger:
				abilityStagger.Stop();
				break;
			case Behavior.Wander:
				abilityWander.Stop();
				break;
			case Behavior.Idle:
				abilityIdle.Stop();
				break;
			case Behavior.Attack:
				abilityAttack.Stop();
				break;
			case Behavior.Destroyed:
				abilityDie.Stop();
				break;
			case Behavior.Grabbed:
				abilityGrabbed.Stop();
				break;
			case Behavior.Thrown:
				abilityThrown.Stop();
				break;
			case Behavior.Investigate:
				abilityInvestigate.Stop();
				break;
			case Behavior.Jump:
				abilityJump.Stop();
				break;
			case Behavior.Flashed:
				abilityFlashed.Stop();
				break;
			}
			currBehavior = newBehavior;
			behaviorStartTime = Time.timeAsDouble;
			switch (currBehavior)
			{
			case Behavior.Wander:
				abilityWander.Start();
				break;
			case Behavior.Chase:
				abilityChase.Start();
				abilityChase.SetTargetPlayer(agent.targetPlayer);
				break;
			case Behavior.Attack:
				abilityAttack.Start();
				abilityAttack.SetTargetPlayer(agent.targetPlayer);
				break;
			case Behavior.Idle:
				abilityIdle.Start();
				break;
			case Behavior.Stagger:
				abilityStagger.Start();
				break;
			case Behavior.Destroyed:
				abilityDie.Start();
				break;
			case Behavior.Grabbed:
				abilityGrabbed.Start();
				break;
			case Behavior.Thrown:
				abilityThrown.Start();
				break;
			case Behavior.Investigate:
				abilityInvestigate.Start();
				break;
			case Behavior.Jump:
				abilityJump.Start();
				break;
			case Behavior.Flashed:
				abilityFlashed.Start();
				break;
			}
			if (entity.IsAuthority())
			{
				agent.RequestBehaviorChange((byte)currBehavior);
			}
		}
	}

	private void OnGrabbed()
	{
		if (currBehavior != Behavior.Destroyed)
		{
			SetBehavior(Behavior.Grabbed);
		}
	}

	private void OnReleased()
	{
		if (currBehavior != Behavior.Destroyed)
		{
			SetBehavior(Behavior.Thrown);
		}
	}

	public void Tick()
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
		switch (currBehavior)
		{
		case Behavior.Idle:
			ChooseNewBehavior();
			break;
		case Behavior.Wander:
			abilityWander.Think(dt);
			ChooseNewBehavior();
			break;
		case Behavior.Chase:
			if (agent.targetPlayer != null)
			{
				abilityChase.SetTargetPlayer(agent.targetPlayer);
			}
			abilityChase.Think(dt);
			break;
		case Behavior.Investigate:
			abilityInvestigate.Think(dt);
			ChooseNewBehavior();
			break;
		}
	}

	private void ChooseNewBehavior()
	{
		if (!GhostReactorManager.AggroDisabled && senseNearby.IsAnyoneNearby())
		{
			investigateLocation = null;
			SetBehavior(Behavior.Chase);
			return;
		}
		investigateLocation = AbilityHelperFunctions.GetLocationToInvestigate(base.transform.position, hearingRadius, investigateLocation);
		if (investigateLocation.HasValue)
		{
			abilityInvestigate.SetTargetPos(investigateLocation.Value);
			SetBehavior(Behavior.Investigate);
		}
		else
		{
			SetBehavior(Behavior.Wander);
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
		switch (currBehavior)
		{
		case Behavior.Idle:
			abilityIdle.UpdateAuthority(dt);
			break;
		case Behavior.Chase:
		{
			abilityChase.UpdateAuthority(dt);
			if (abilityChase.IsDone())
			{
				SetBehavior(Behavior.Wander);
				break;
			}
			GRPlayer gRPlayer = GRPlayer.Get(agent.targetPlayer);
			if (gRPlayer != null)
			{
				float num = attackRange * attackRange;
				if ((gRPlayer.transform.position - base.transform.position).sqrMagnitude < num)
				{
					SetBehavior(Behavior.Attack);
				}
			}
			break;
		}
		case Behavior.Attack:
			abilityAttack.UpdateAuthority(dt);
			if (abilityAttack.IsDone())
			{
				SetBehavior(Behavior.Chase);
			}
			break;
		case Behavior.Stagger:
			abilityStagger.UpdateAuthority(dt);
			if (abilityStagger.IsDone())
			{
				SetBehavior(Behavior.Wander);
			}
			break;
		case Behavior.Destroyed:
			abilityDie.UpdateAuthority(dt);
			break;
		case Behavior.Wander:
			abilityWander.UpdateAuthority(dt);
			break;
		case Behavior.Thrown:
			if (abilityThrown.IsDone())
			{
				SetBehavior(Behavior.Wander);
			}
			break;
		case Behavior.Investigate:
			abilityInvestigate.UpdateAuthority(dt);
			break;
		case Behavior.Jump:
			abilityJump.UpdateAuthority(dt);
			if (abilityJump.IsDone())
			{
				ChooseNewBehavior();
			}
			break;
		case Behavior.Flashed:
			abilityFlashed.UpdateAuthority(dt);
			if (abilityFlashed.IsDone())
			{
				ChooseNewBehavior();
			}
			break;
		case Behavior.Grabbed:
			break;
		}
	}

	public void OnUpdateRemote(float dt)
	{
		switch (currBehavior)
		{
		case Behavior.Attack:
			abilityAttack.UpdateRemote(dt);
			break;
		case Behavior.Chase:
			abilityChase.UpdateRemote(dt);
			break;
		case Behavior.Stagger:
			abilityStagger.UpdateRemote(dt);
			break;
		case Behavior.Destroyed:
			abilityDie.UpdateRemote(dt);
			break;
		case Behavior.Wander:
			abilityWander.UpdateRemote(dt);
			break;
		case Behavior.Investigate:
			abilityInvestigate.UpdateRemote(dt);
			break;
		case Behavior.Jump:
			abilityJump.UpdateRemote(dt);
			break;
		case Behavior.Flashed:
			abilityFlashed.UpdateRemote(dt);
			break;
		case Behavior.Grabbed:
		case Behavior.Thrown:
			break;
		}
	}

	public void OnGameEntitySerialize(BinaryWriter writer)
	{
		byte value = (byte)currBehavior;
		byte value2 = (byte)currBodyState;
		writer.Write(value);
		writer.Write(hp);
		writer.Write(value2);
	}

	public void OnGameEntityDeserialize(BinaryReader reader)
	{
		Behavior newBehavior = (Behavior)reader.ReadByte();
		int hP = reader.ReadInt32();
		BodyState newBodyState = (BodyState)reader.ReadByte();
		SetHP(hP);
		SetBehavior(newBehavior, force: true);
		SetBodyState(newBodyState, force: true);
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
				OnHitByClub(hit);
				break;
			case GameHitType.Flash:
				OnHitByFlash(gameComponent, hit);
				break;
			case GameHitType.Shield:
				OnHitByShield(hit);
				break;
			}
		}
	}

	private void OnHitByClub(GameHitData hit)
	{
		if (currBodyState == BodyState.Bones)
		{
			if (currBehavior != Behavior.Destroyed)
			{
				hp -= hit.hitAmount;
				if (hp <= 0)
				{
					abilityDie.SetInstigatingPlayerIndex(entity.GetLastHeldByPlayerForEntityID(hit.hitByEntityId));
					SetBehavior(Behavior.Destroyed);
				}
				else
				{
					abilityStagger.SetStaggerVelocity(hit.hitImpulse);
					TrySetBehavior(Behavior.Stagger);
				}
			}
		}
		else if (currBodyState == BodyState.Shell && armor != null)
		{
			armor.PlayBlockFx(hit.hitEntityPosition);
		}
	}

	public void InstantDeath()
	{
		hp = 0;
		SetBehavior(Behavior.Destroyed);
	}

	private void OnHitByFlash(GRTool tool, GameHitData hit)
	{
		abilityFlashed.SetStaggerVelocity(hit.hitImpulse);
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
		TrySetBehavior(Behavior.Flashed);
	}

	private void OnHitByShield(GameHitData hit)
	{
		OnHitByClub(hit);
	}

	private void OnTriggerEnter(Collider collider)
	{
		if (currBehavior != Behavior.Attack)
		{
			return;
		}
		GRShieldCollider component = collider.GetComponent<GRShieldCollider>();
		if (component != null)
		{
			Vector3 enemyAttackDirection = abilityAttack.targetPos - abilityAttack.initialPos;
			GameHittable component2 = GetComponent<GameHittable>();
			component.BlockHittable(base.transform.position, enemyAttackDirection, component2);
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
			GameHitData hitData = new GameHitData
			{
				hitTypeId = 0,
				hitEntityId = component5.gameEntity.id,
				hitByEntityId = entity.id,
				hitEntityPosition = component4.transform.position,
				hitImpulse = Vector3.zero,
				hitPosition = component4.transform.position,
				hittablePoint = component5.FindHittablePoint(collider)
			};
			component5.RequestHit(hitData);
		}
	}

	private IEnumerator TryHitPlayer(GRPlayer player)
	{
		yield return new WaitForUpdate();
		if (currBehavior == Behavior.Attack && player != null && player.gamePlayer.IsLocal() && Time.time > lastHitPlayerTime + minTimeBetweenHits)
		{
			lastHitPlayerTime = Time.time;
			GhostReactorManager.Get(entity).RequestEnemyHitPlayer(GhostReactor.EnemyType.Chaser, entity.id, player, base.transform.position);
		}
	}

	private void RefreshBody()
	{
		switch (currBodyState)
		{
		case BodyState.Destroyed:
			armor.SetHp(0);
			break;
		case BodyState.Bones:
			armor.SetHp(0);
			GREnemy.HideObjects(bonesStateVisibleObjects, hide: false);
			GREnemy.HideObjects(alwaysVisibleObjects, hide: false);
			break;
		case BodyState.Shell:
			armor.SetHp(hp);
			GREnemy.HideObjects(bonesStateVisibleObjects, hide: true);
			GREnemy.HideObjects(alwaysVisibleObjects, hide: false);
			break;
		}
	}

	public void SetBodyState(BodyState newBodyState, bool force = false)
	{
		if (currBodyState != newBodyState || force)
		{
			switch (currBodyState)
			{
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
	}

	public void GetDebugTextLines(out List<string> strings)
	{
		strings = new List<string>();
		strings.Add($"State: <color=\"yellow\">{currBehavior.ToString()}<color=\"white\"> HP: <color=\"yellow\">{hp}<color=\"white\">");
		float magnitude = (GRSenseNearby.GetRigTestLocation(VRRig.LocalRig) - base.transform.position).magnitude;
		bool flag = GRSenseLineOfSight.HasGeoLineOfSight(headTransform.position, GRSenseNearby.GetRigTestLocation(VRRig.LocalRig), senseLineOfSight.sightDist, senseLineOfSight.visibilityMask);
		strings.Add($"player rig dis: {magnitude} has los: {flag}");
	}
}

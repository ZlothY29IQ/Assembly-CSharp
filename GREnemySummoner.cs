using System.Collections.Generic;
using System.IO;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.AI;

public class GREnemySummoner : MonoBehaviour, IGameEntityComponent, IGameEntitySerialize, IGameHittable, IGameEntityDebugComponent, IGameAgentComponent, IGRSummoningEntity
{
	public enum Behavior
	{
		Idle,
		Wander,
		Stagger,
		Destroyed,
		Summon,
		KeepDistance,
		MoveToTarget,
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

	private GameEntity entity;

	private GameAgent agent;

	private GREnemy enemy;

	public GRArmorEnemy armor;

	public GRAttributes attributes;

	public Animation anim;

	public GRSenseNearby senseNearby;

	public GRSenseLineOfSight senseLineOfSight;

	public GRAbilityIdle abilityIdle;

	public GRAbilityWander abilityWander;

	public GRAbilityAttackJump abilityAttack;

	public GRAbilityStagger abilityStagger;

	public GRAbilityDie abilityDie;

	public GRAbilitySummon abilitySummon;

	public GRAbilityKeepDistance abilityKeepDistance;

	public GRAbilityMoveToTarget abilityMoveToTarget;

	public GRAbilityMoveToTarget abilityInvestigate;

	public GRAbilityJump abilityJump;

	public GRAbilityStagger abilityFlashed;

	public AbilitySound soundWander;

	public AbilitySound soundAttack;

	public GameLight summonLight;

	public List<Renderer> bones;

	public List<Renderer> always;

	public List<GameObject> bonesStateVisibleObjects;

	public List<GameObject> alwaysVisibleObjects;

	public Transform coreMarker;

	public GRCollectible corePrefab;

	public Transform headTransform;

	public float attackRange = 2f;

	public List<VRRig> rigsNearby;

	public NavMeshAgent navAgent;

	public AudioSource audioSource;

	public float idleDuration = 2f;

	public float keepDistanceThreshold = 3f;

	public float tooFarDistanceThreshold = 5f;

	public double lastSummonTime;

	public float minSummonInterval = 4f;

	public int maxSimultaneousSummonedEntities = 3;

	public float hearingRadius = 7f;

	[ReadOnly]
	public int hp;

	[ReadOnly]
	public Behavior currBehavior;

	[ReadOnly]
	public double behaviorEndTime;

	[ReadOnly]
	public BodyState currBodyState;

	[ReadOnly]
	public Vector3 searchPosition;

	[ReadOnly]
	public double behaviorStartTime;

	private Rigidbody rigidBody;

	private List<Collider> colliders;

	private List<int> trackedEntities;

	private Vector3? investigateLocation;

	private float lastUpdateTime;

	private static List<VRRig> tempRigs = new List<VRRig>(16);

	private void Awake()
	{
		rigidBody = GetComponent<Rigidbody>();
		colliders = new List<Collider>(4);
		trackedEntities = new List<int>();
		GetComponentsInChildren(colliders);
		agent = GetComponent<GameAgent>();
		entity = GetComponent<GameEntity>();
		enemy = GetComponent<GREnemy>();
		if (armor != null)
		{
			armor.SetHp(0);
		}
		navAgent.updateRotation = false;
		behaviorStartTime = -1.0;
		agent.onBehaviorStateChanged += OnNetworkBehaviorStateChange;
		senseNearby.Setup(headTransform, entity);
	}

	public void OnEntityInit()
	{
		abilityIdle.Setup(agent, anim, audioSource, null, null, null);
		abilityWander.Setup(agent, anim, audioSource, base.transform, null, null);
		abilityDie.Setup(agent, anim, audioSource, base.transform, null, null);
		abilitySummon.Setup(agent, anim, audioSource, base.transform, null, null);
		abilityKeepDistance.Setup(agent, anim, audioSource, base.transform, null, null);
		abilityMoveToTarget.Setup(agent, anim, audioSource, base.transform, null, null);
		abilityStagger.Setup(agent, anim, audioSource, base.transform, null, null);
		abilityInvestigate.Setup(agent, anim, audioSource, base.transform, null, null);
		abilityJump.Setup(agent, anim, audioSource, base.transform, null, null);
		abilityFlashed.Setup(agent, anim, audioSource, base.transform, null, null);
		SetBehavior(Behavior.Idle, force: true);
		if ((bool)entity && (bool)entity.manager && (bool)entity.manager.ghostReactorManager && (bool)entity.manager.ghostReactorManager.reactor)
		{
			foreach (GRBonusEntry enemyGlobalBonuse in entity.manager.ghostReactorManager.reactor.GetCurrLevelGenConfig().enemyGlobalBonuses)
			{
				attributes.AddBonus(enemyGlobalBonuse);
			}
		}
		SetHP(attributes.CalculateFinalValueForAttribute(GRAttributeType.HPMax));
		navAgent.speed = attributes.CalculateFinalValueForAttribute(GRAttributeType.PatrolSpeed);
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
		if (newState >= 0 && newState < 10)
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
		if (currBehavior == newBehavior && !force)
		{
			return;
		}
		switch (currBehavior)
		{
		case Behavior.Stagger:
			abilityStagger.Stop();
			break;
		case Behavior.Wander:
			abilityWander.Stop();
			break;
		case Behavior.Idle:
			abilityIdle.Stop();
			break;
		case Behavior.Destroyed:
			abilityDie.Stop();
			break;
		case Behavior.Summon:
			abilitySummon.Stop();
			if (summonLight != null)
			{
				summonLight.gameObject.SetActive(value: false);
			}
			break;
		case Behavior.KeepDistance:
			abilityKeepDistance.Stop();
			break;
		case Behavior.MoveToTarget:
			abilityMoveToTarget.Stop();
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
			soundWander.Play(audioSource);
			break;
		case Behavior.Idle:
			abilityIdle.Start();
			break;
		case Behavior.Stagger:
			abilityStagger.Start();
			break;
		case Behavior.Destroyed:
			if (entity.IsAuthority())
			{
				entity.manager.RequestCreateItem(corePrefab.gameObject.name.GetStaticHash(), coreMarker.position, coreMarker.rotation, 0L);
			}
			abilityDie.Start();
			break;
		case Behavior.Summon:
			if (summonLight != null)
			{
				summonLight.gameObject.SetActive(value: true);
			}
			lastSummonTime = Time.timeAsDouble;
			abilitySummon.SetLookAtTarget(GetPlayerTransform(agent.targetPlayer));
			abilitySummon.Start();
			break;
		case Behavior.KeepDistance:
			abilityKeepDistance.SetTargetPlayer(agent.targetPlayer);
			abilityKeepDistance.Start();
			break;
		case Behavior.MoveToTarget:
			abilityMoveToTarget.SetTarget(GetPlayerTransform(agent.targetPlayer));
			abilityMoveToTarget.Start();
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
		lastUpdateTime = Time.time;
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
			abilityIdle.Think(dt);
			ChooseNewBehavior();
			break;
		case Behavior.Wander:
			abilityWander.Think(dt);
			ChooseNewBehavior();
			break;
		case Behavior.Investigate:
			abilityInvestigate.Think(dt);
			ChooseNewBehavior();
			break;
		case Behavior.Summon:
			abilitySummon.Think(dt);
			if (abilitySummon.IsDone())
			{
				ChooseNewBehavior();
			}
			break;
		case Behavior.KeepDistance:
			abilityKeepDistance.Think(dt);
			ChooseNewBehavior();
			break;
		case Behavior.MoveToTarget:
			abilityMoveToTarget.Think(dt);
			ChooseNewBehavior();
			break;
		case Behavior.Stagger:
		case Behavior.Destroyed:
			break;
		}
	}

	public bool CanSummon()
	{
		if (GhostReactorManager.AggroDisabled)
		{
			return false;
		}
		if (currBehavior == Behavior.Summon && abilitySummon.IsDone())
		{
			return false;
		}
		if (Time.timeAsDouble - lastSummonTime < (double)minSummonInterval)
		{
			return false;
		}
		return trackedEntities.Count < maxSimultaneousSummonedEntities;
	}

	public Transform GetPlayerTransform(NetPlayer targetPlayer)
	{
		if (targetPlayer != null)
		{
			GRPlayer gRPlayer = GRPlayer.Get(targetPlayer.ActorNumber);
			if (gRPlayer != null && gRPlayer.State == GRPlayer.GRPlayerState.Alive)
			{
				return gRPlayer.transform;
			}
		}
		return null;
	}

	private void ChooseNewBehavior()
	{
		float outDistanceSq = 0f;
		VRRig vRRig = senseNearby.PickClosest(out outDistanceSq);
		if (!GhostReactorManager.AggroDisabled && vRRig != null)
		{
			investigateLocation = null;
			float num = ((currBehavior == Behavior.KeepDistance) ? (keepDistanceThreshold + 1f) : keepDistanceThreshold);
			if (outDistanceSq < num * num)
			{
				SetBehavior(Behavior.KeepDistance);
				return;
			}
			if (CanSummon())
			{
				SetBehavior(Behavior.Summon);
				return;
			}
			float num2 = tooFarDistanceThreshold * tooFarDistanceThreshold;
			if (outDistanceSq > num2)
			{
				SetBehavior(Behavior.MoveToTarget);
			}
			else
			{
				SetBehavior(Behavior.Idle);
			}
			return;
		}
		investigateLocation = AbilityHelperFunctions.GetLocationToInvestigate(base.transform.position, hearingRadius, investigateLocation);
		if (investigateLocation.HasValue)
		{
			abilityInvestigate.SetTargetPos(investigateLocation.Value);
			SetBehavior(Behavior.Investigate);
			return;
		}
		double num3 = Time.timeAsDouble - abilityIdle.startTime;
		if (currBehavior == Behavior.Idle && num3 < (double)idleDuration)
		{
			SetBehavior(Behavior.Idle);
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
		case Behavior.Summon:
			abilitySummon.UpdateAuthority(dt);
			break;
		case Behavior.KeepDistance:
			abilityKeepDistance.UpdateAuthority(dt);
			break;
		case Behavior.MoveToTarget:
			abilityMoveToTarget.UpdateAuthority(dt);
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
		}
	}

	public void OnUpdateRemote(float dt)
	{
		switch (currBehavior)
		{
		case Behavior.Stagger:
			abilityStagger.UpdateRemote(dt);
			break;
		case Behavior.Destroyed:
			abilityDie.UpdateRemote(dt);
			break;
		case Behavior.Wander:
			abilityWander.UpdateRemote(dt);
			break;
		case Behavior.Summon:
			abilitySummon.UpdateRemote(dt);
			break;
		case Behavior.KeepDistance:
			abilityKeepDistance.UpdateRemote(dt);
			break;
		case Behavior.MoveToTarget:
			abilityMoveToTarget.UpdateRemote(dt);
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

	private void OnHitByClub(GRTool tool, GameHitData hit)
	{
		if (currBehavior == Behavior.Destroyed)
		{
			return;
		}
		if (currBodyState == BodyState.Bones)
		{
			hp -= hit.hitAmount;
			if (hp <= 0)
			{
				abilityDie.SetInstigatingPlayerIndex(entity.GetLastHeldByPlayerForEntityID(hit.hitByEntityId));
				abilityDie.SetStaggerVelocity(hit.hitImpulse);
				SetBehavior(Behavior.Destroyed);
			}
			else
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

	public void OnHitByShield(GRTool tool, GameHitData hit)
	{
		OnHitByClub(tool, hit);
	}

	private void OnTriggerEnter(Collider collider)
	{
		Rigidbody attachedRigidbody = collider.attachedRigidbody;
		if (attachedRigidbody != null)
		{
			GRPlayer component = attachedRigidbody.GetComponent<GRPlayer>();
			if (component != null && component.gamePlayer.IsLocal())
			{
				GhostReactorManager.Get(entity).RequestEnemyHitPlayer(GhostReactor.EnemyType.Phantom, entity.id, component, base.transform.position);
			}
			GRBreakable component2 = attachedRigidbody.GetComponent<GRBreakable>();
			GameHittable component3 = attachedRigidbody.GetComponent<GameHittable>();
			if (component2 != null && component3 != null)
			{
				GameHitData hitData = new GameHitData
				{
					hitTypeId = 0,
					hitEntityId = component3.gameEntity.id,
					hitByEntityId = entity.id,
					hitEntityPosition = component2.transform.position,
					hitImpulse = Vector3.zero,
					hitPosition = component2.transform.position,
					hittablePoint = component3.FindHittablePoint(collider)
				};
				component3.RequestHit(hitData);
			}
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
			GREnemy.HideRenderers(bones, hide: false);
			GREnemy.HideRenderers(always, hide: false);
			GREnemy.HideObjects(bonesStateVisibleObjects, hide: false);
			GREnemy.HideObjects(alwaysVisibleObjects, hide: false);
			break;
		case BodyState.Shell:
			armor.SetHp(hp);
			GREnemy.HideRenderers(bones, hide: true);
			GREnemy.HideRenderers(always, hide: false);
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
		strings.Add($"Nearby rigs: <color=\"yellow\">{senseNearby.rigsNearby.Count}<color=\"white\">");
		strings.Add($"Spawned entities: <color=\"yellow\">{trackedEntities.Count}<color=\"white\">");
	}

	public void AddTrackedEntity(GameEntity entityToTrack)
	{
		int netId = entityToTrack.GetNetId();
		trackedEntities.AddIfNew(netId);
	}

	public void RemoveTrackedEntity(GameEntity entityToRemove)
	{
		int netId = entityToRemove.GetNetId();
		if (trackedEntities.Contains(netId))
		{
			trackedEntities.Remove(netId);
		}
	}

	public void OnSummonedEntityInit(GameEntity entity)
	{
		AddTrackedEntity(entity);
	}

	public void OnSummonedEntityDestroy(GameEntity entity)
	{
		RemoveTrackedEntity(entity);
	}
}

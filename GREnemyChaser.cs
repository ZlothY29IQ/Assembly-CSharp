using System.Collections;
using System.Collections.Generic;
using System.IO;
using CjLib;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.AI;

public class GREnemyChaser : MonoBehaviour, IGameEntityComponent, IGameEntitySerialize, IGameHittable, IGameAgentComponent, IGameEntityDebugComponent
{
	public enum Behavior
	{
		Idle,
		Patrol,
		Wander,
		Stagger,
		Dying,
		Chase,
		Search,
		Attack,
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

	[SerializeField]
	private GRAttributes attributes;

	public GRSenseNearby senseNearby;

	public GRSenseLineOfSight senseLineOfSight;

	public Animation anim;

	public GRAbilityIdle abilityIdle;

	public GRAbilityChase abilityChase;

	public GRAbilityIdle abilitySearch;

	public GRAbilityAttackSwipe abilityAttackSwipe;

	public GRAbilityStagger abilityStagger;

	public GRAbilityDie abilityDie;

	public GRAbilityMoveToTarget abilityInvestigate;

	public GRAbilityPatrol abilityPatrol;

	public GRAbilityWander abilityWander;

	public GRAbilityFlashed abilityFlashed;

	public GRAbilityJump abilityJump;

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

	private Vector3? investigateLocation;

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

	public bool canChaseJump = true;

	public float chaseJumpDistance = 5f;

	public float chaseJumpMinInterval = 1f;

	public float minChaseJumpDistance = 2f;

	public static RaycastHit[] visibilityHits = new RaycastHit[16];

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
		navAgent.updateRotation = false;
		agent.onBodyStateChanged += OnNetworkBodyStateChange;
		agent.onBehaviorStateChanged += OnNetworkBehaviorStateChange;
	}

	public void OnEntityInit()
	{
		abilityIdle.Setup(agent, anim, audioSource, null, null, null);
		abilityChase.Setup(agent, anim, audioSource, base.transform, headTransform, senseLineOfSight);
		abilitySearch.Setup(agent, anim, audioSource, null, null, null);
		abilityAttackSwipe.Setup(agent, anim, audioSource, base.transform, null, null);
		abilityInvestigate.Setup(agent, anim, audioSource, base.transform, null, null);
		abilityPatrol.Setup(agent, anim, audioSource, base.transform, null, null);
		abilityWander.Setup(agent, anim, audioSource, base.transform, null, null);
		abilityStagger.Setup(agent, anim, audioSource, base.transform, null, null);
		abilityDie.Setup(agent, anim, audioSource, base.transform, null, null);
		abilityFlashed.Setup(agent, anim, audioSource, base.transform, null, null);
		abilityJump.Setup(agent, anim, audioSource, base.transform, null, null);
		senseNearby.Setup(headTransform, entity);
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
	}

	private void Setup(long entityCreateData)
	{
		SetPatrolPath(entityCreateData);
		if (abilityPatrol.HasValidPatrolPath())
		{
			SetBehavior(Behavior.Patrol, force: true);
		}
		else
		{
			SetBehavior(Behavior.Wander, force: true);
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

	private void OnAgentJumpRequested(Vector3 start, Vector3 end, float heightScale, float speedScale)
	{
		abilityJump.SetupJump(start, end, heightScale, speedScale);
		SetBehavior(Behavior.Jump);
	}

	private void OnNetworkBehaviorStateChange(byte newState)
	{
		if (newState >= 0 && newState < 11)
		{
			SetBehavior((Behavior)newState);
		}
	}

	private void OnNetworkBodyStateChange(byte newState)
	{
		if (newState >= 0 && newState < 3)
		{
			SetBodyState((BodyState)newState);
		}
	}

	private void SetPatrolPath(long entityCreateData)
	{
		GRPatrolPath gRPatrolPath = GhostReactorManager.Get(entity).reactor.GetPatrolPath(entityCreateData);
		abilityPatrol.SetPatrolPath(gRPatrolPath);
	}

	private void SetNextPatrolNode(int nextPatrolNode)
	{
		abilityPatrol.SetNextPatrolNode(nextPatrolNode);
	}

	public void SetHP(int hp)
	{
		this.hp = hp;
	}

	private bool TrySetBehavior(Behavior newBehavior)
	{
		if (currBehavior == Behavior.Jump && newBehavior == Behavior.Stagger)
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

	private void SetBehavior(Behavior newBehavior, bool force = false)
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
		case Behavior.Search:
			abilitySearch.Stop();
			break;
		case Behavior.Idle:
			abilityIdle.Stop();
			break;
		case Behavior.Chase:
			abilityChase.Stop();
			break;
		case Behavior.Attack:
			abilityAttackSwipe.Stop();
			break;
		case Behavior.Investigate:
			abilityInvestigate.Stop();
			break;
		case Behavior.Patrol:
			abilityPatrol.Stop();
			break;
		case Behavior.Wander:
			abilityWander.Stop();
			break;
		case Behavior.Jump:
			abilityJump.Stop();
			break;
		}
		currBehavior = newBehavior;
		switch (currBehavior)
		{
		case Behavior.Dying:
			if (entity.IsAuthority())
			{
				entity.manager.RequestCreateItem(corePrefab.gameObject.name.GetStaticHash(), coreMarker.position, coreMarker.rotation, 0L);
			}
			abilityDie.Start();
			break;
		case Behavior.Stagger:
			abilityStagger.Start();
			lastStaggerTime = Time.time;
			break;
		case Behavior.Flashed:
			abilityFlashed.Start();
			break;
		case Behavior.Patrol:
			abilityPatrol.Start();
			break;
		case Behavior.Wander:
			abilityWander.Start();
			break;
		case Behavior.Search:
			abilitySearch.Start();
			break;
		case Behavior.Chase:
			abilityChase.Start();
			investigateLocation = null;
			abilityChase.SetTargetPlayer(agent.targetPlayer);
			break;
		case Behavior.Attack:
			abilityAttackSwipe.Start();
			investigateLocation = null;
			abilityAttackSwipe.SetTargetPlayer(agent.targetPlayer);
			break;
		case Behavior.Idle:
			abilitySearch.Start();
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

	private void SetBodyState(BodyState newBodyState, bool force = false)
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
		switch (currBehavior)
		{
		case Behavior.Idle:
		case Behavior.Patrol:
		case Behavior.Investigate:
			ChooseNewBehavior();
			break;
		case Behavior.Search:
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
			ChooseNewBehavior();
			break;
		case Behavior.Stagger:
		case Behavior.Dying:
		case Behavior.Attack:
		case Behavior.Flashed:
			break;
		}
	}

	private void ChooseNewBehavior()
	{
		if (!GhostReactorManager.AggroDisabled && senseNearby.IsAnyoneNearby())
		{
			if (agent.targetPlayer != null)
			{
				Vector3 position = GRPlayer.Get(agent.targetPlayer).transform.position;
				Vector3 vector = position - base.transform.position;
				float magnitude = vector.magnitude;
				if (magnitude < attackRange)
				{
					SetBehavior(Behavior.Attack);
				}
				else if (canChaseJump && abilityJump.IsCoolDownOver(chaseJumpMinInterval) && magnitude > attackRange + minChaseJumpDistance && GRSenseLineOfSight.HasNavmeshLineOfSight(base.transform.position, position, 10f))
				{
					Vector3 vector2 = vector / magnitude;
					float num = Mathf.Clamp(chaseJumpDistance, minChaseJumpDistance, magnitude - attackRange * 0.5f);
					if (NavMesh.SamplePosition(base.transform.position + vector2 * num, out var hit, 0.5f, AbilityHelperFunctions.GetNavMeshWalkableArea()))
					{
						agent.GetGameAgentManager().RequestJump(agent, base.transform.position, hit.position, 0.25f, 1.5f);
						return;
					}
				}
			}
			TrySetBehavior(Behavior.Chase);
		}
		else
		{
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
				SetBehavior(Behavior.Wander);
			}
		}
	}

	private void OnUpdate(float dt)
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

	private void OnUpdateAuthority(float dt)
	{
		switch (currBehavior)
		{
		case Behavior.Idle:
			abilityIdle.UpdateAuthority(dt);
			break;
		case Behavior.Patrol:
			abilityPatrol.UpdateAuthority(dt);
			break;
		case Behavior.Wander:
			abilityWander.UpdateAuthority(dt);
			break;
		case Behavior.Search:
			abilitySearch.UpdateAuthority(dt);
			if (abilitySearch.IsDone())
			{
				ChooseNewBehavior();
			}
			break;
		case Behavior.Stagger:
			abilityStagger.UpdateAuthority(dt);
			if (abilityStagger.IsDone())
			{
				if (agent.targetPlayer == null)
				{
					SetBehavior(Behavior.Search);
				}
				else
				{
					SetBehavior(Behavior.Chase);
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
					SetBehavior(Behavior.Chase);
				}
			}
			break;
		case Behavior.Chase:
		{
			abilityChase.UpdateAuthority(dt);
			if (abilityChase.IsDone())
			{
				SetBehavior(Behavior.Search);
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
			abilityAttackSwipe.UpdateAuthority(dt);
			if (abilityAttackSwipe.IsDone())
			{
				SetBehavior(Behavior.Chase);
			}
			break;
		case Behavior.Dying:
			abilityDie.UpdateAuthority(dt);
			break;
		case Behavior.Investigate:
			abilityInvestigate.UpdateAuthority(dt);
			if (abilityInvestigate.IsDone())
			{
				investigateLocation = null;
			}
			if (GhostReactorManager.noiseDebugEnabled)
			{
				DebugUtil.DrawLine(base.transform.position, abilityInvestigate.GetTargetPos(), Color.green);
			}
			break;
		case Behavior.Jump:
			abilityJump.UpdateAuthority(dt);
			if (abilityJump.IsDone())
			{
				ChooseNewBehavior();
			}
			break;
		}
	}

	private void OnUpdateRemote(float dt)
	{
		switch (currBehavior)
		{
		case Behavior.Chase:
			abilityChase.UpdateRemote(dt);
			break;
		case Behavior.Stagger:
			abilityStagger.UpdateRemote(dt);
			break;
		case Behavior.Patrol:
			abilityPatrol.UpdateRemote(dt);
			break;
		case Behavior.Wander:
			abilityWander.UpdateRemote(dt);
			break;
		case Behavior.Attack:
			abilityAttackSwipe.UpdateRemote(dt);
			break;
		case Behavior.Search:
			abilitySearch.UpdateRemote(dt);
			break;
		case Behavior.Idle:
			abilityIdle.UpdateRemote(dt);
			break;
		case Behavior.Dying:
			abilityDie.UpdateRemote(dt);
			break;
		case Behavior.Investigate:
			abilityInvestigate.UpdateRemote(dt);
			break;
		case Behavior.Flashed:
			abilityFlashed.UpdateRemote(dt);
			break;
		case Behavior.Jump:
			abilityJump.UpdateRemote(dt);
			break;
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
		Debug.Log($"Chaser On Hit By Shield dmg:{hit.hitAmount} impulse:{hit.hitImpulse} size:{hit.hitImpulse.magnitude}");
		OnHitByClub(tool, hit);
	}

	private void OnTriggerEnter(Collider collider)
	{
		if (currBodyState == BodyState.Destroyed || currBehavior != Behavior.Attack)
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

	public void GetDebugTextLines(out List<string> strings)
	{
		strings = new List<string>();
		strings.Add($"State: <color=\"yellow\">{currBehavior.ToString()}<color=\"white\"> HP: <color=\"yellow\">{hp}<color=\"white\">");
		strings.Add($"speed: <color=\"yellow\">{navAgent.speed}<color=\"white\"> patrol node:<color=\"yellow\">{abilityPatrol.nextPatrolNode}/{((abilityPatrol.GetPatrolPath() != null) ? abilityPatrol.GetPatrolPath().patrolNodes.Count : 0)}<color=\"white\">");
		strings.Add($"Dest: <color=\"yellow\">{agent.navAgent.destination}<color=\"white\"> Pos: <color=\"yellow\">{agent.transform.position}<color=\"white\">");
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
		SetNextPatrolNode(nextPatrolNode);
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

using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.AI;

public class GREnemyBossMoonEye : MonoBehaviour, IGameEntityComponent, IGameEntitySerialize, IGameHittable, IGameAgentComponent, IGameEntityDebugComponent
{
	public enum Behavior
	{
		Idle,
		AttackLaser,
		Closed,
		GravityStart,
		GravityEnd,
		GravityIdle,
		Dying,
		None,
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

	private GRAbilityBase[] abilities;

	private GRAbilityBase currAbility;

	public GRAbilityAgent abilityAgent;

	public GRAbilityIdle abilityIdle;

	public GRAbilityIdle abilityClosed;

	public GRAbilityAttackLaser abilityAttackLaser;

	public GRAbilityDie abilityDie;

	public GRAbilityIdle abilityGravityStart;

	public GRAbilityIdle abilityGravityEnd;

	public GRAbilityIdle abilityGravityIdle;

	public Transform headTransform;

	public NavMeshAgent navAgent;

	public AudioSource audioSource;

	public float counterAttackWindow = 3f;

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

	public bool allowLaserAttack;

	public bool canChaseJump = true;

	public float chaseJumpDistance = 5f;

	public float chaseJumpMinInterval = 1f;

	public float minChaseJumpDistance = 2f;

	private double lastHitTime;

	private List<Collider> colliders;

	private float lastHitPlayerTime;

	private float minTimeBetweenHits = 0.5f;

	public float hearingRadius = 5f;

	public int maxSimultaneousSummonedEntities = 6;

	private static List<VRRig> tempRigs = new List<VRRig>(16);

	private static List<Behavior> tempPotentialAttacks = new List<Behavior>(16);

	private Coroutine tryHitPlayerCoroutine;

	private void Awake()
	{
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
		agent.onBehaviorStateChanged += OnNetworkBehaviorStateChange;
		abilities = new GRAbilityBase[8];
	}

	public void OnEntityInit()
	{
		currBehavior = Behavior.None;
		currAbility = null;
		SetupAbility(Behavior.Idle, abilityIdle, agent, anim, audioSource, null, null, null);
		SetupAbility(Behavior.AttackLaser, abilityAttackLaser, agent, anim, audioSource, base.transform, headTransform, null);
		SetupAbility(Behavior.Closed, abilityClosed, agent, anim, audioSource, null, null, null);
		SetupAbility(Behavior.GravityStart, abilityGravityStart, agent, anim, audioSource, null, null, null);
		SetupAbility(Behavior.GravityEnd, abilityGravityEnd, agent, anim, audioSource, null, null, null);
		SetupAbility(Behavior.GravityIdle, abilityGravityIdle, agent, anim, audioSource, null, null, null);
		SetupAbility(Behavior.Dying, abilityDie, agent, anim, audioSource, base.transform, null, null);
		senseNearby.Setup(headTransform, entity);
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
		int num = CalcMaxHP();
		if (enemy != null)
		{
			enemy.SetMaxHP(num);
		}
		SetHP(num);
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
		agent.onBehaviorStateChanged -= OnNetworkBehaviorStateChange;
	}

	public void Setup(long entityCreateData)
	{
		SetBehavior(Behavior.Idle, force: true);
	}

	public void OnNetworkBehaviorStateChange(byte newState)
	{
		if (newState >= 0 && newState < 8)
		{
			SetBehavior((Behavior)newState);
		}
	}

	public void ResetEye()
	{
		if (entity.IsAuthority())
		{
			SetBehavior(Behavior.Idle);
		}
	}

	public void SetHP(int hp)
	{
		this.hp = hp;
		if (enemy != null)
		{
			enemy.SetHP(hp);
		}
	}

	public bool TrySetBehavior(Behavior newBehavior)
	{
		SetBehavior(newBehavior);
		return true;
	}

	private void SetBehavior(Behavior newBehavior, bool force = false)
	{
		if (abilities == null)
		{
			Debug.LogError("Abilities have not been initialized", this);
			return;
		}
		if (newBehavior < Behavior.Idle || (int)newBehavior >= abilities.Length)
		{
			Debug.LogErrorFormat("New Behavior Index is invalid {0} {1} {2}", (int)newBehavior, newBehavior, base.gameObject.name);
			return;
		}
		GRAbilityBase gRAbilityBase = abilities[(int)newBehavior];
		if (currBehavior != newBehavior || force)
		{
			Debug.LogFormat("Boss Eye SetBehavior {0} -> {1}", currBehavior, newBehavior);
			if (currAbility != null)
			{
				currAbility.Stop();
			}
			if (currBehavior == Behavior.Closed)
			{
				SetHP(CalcMaxHP());
			}
			currBehavior = newBehavior;
			currAbility = gRAbilityBase;
			if (currAbility != null)
			{
				currAbility.Start();
			}
			if (currBehavior == Behavior.AttackLaser)
			{
				abilityAttackLaser.SetTargetPlayer(agent.targetPlayer);
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

	private void RefreshBody()
	{
	}

	private void Update()
	{
		OnUpdate(Time.deltaTime);
	}

	public void OnEntityThink(float dt)
	{
		if (entity.IsAuthority())
		{
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
			if (currBehavior == Behavior.Idle)
			{
				ChooseNewBehavior();
			}
		}
	}

	private bool TryChooseAttackBehavior()
	{
		if (Time.timeAsDouble > lastHitTime + (double)counterAttackWindow)
		{
			return false;
		}
		if (currBehavior == Behavior.Closed)
		{
			return false;
		}
		tempPotentialAttacks.Clear();
		if (allowLaserAttack)
		{
			tempPotentialAttacks.Add(Behavior.AttackLaser);
		}
		for (int num = tempPotentialAttacks.Count - 1; num >= 0; num--)
		{
			GRAbilityBase gRAbilityBase = abilities[(int)tempPotentialAttacks[num]];
			if (gRAbilityBase == null || !senseNearby.IsAnyoneNearby(gRAbilityBase.GetRange()) || !gRAbilityBase.IsCoolDownOver())
			{
				tempPotentialAttacks.RemoveAt(num);
			}
		}
		if (tempPotentialAttacks.Count <= 0)
		{
			return false;
		}
		int index = Random.Range(0, tempPotentialAttacks.Count);
		SetBehavior(tempPotentialAttacks[index]);
		return true;
	}

	private void ChooseNewBehavior()
	{
		if (GhostReactorManager.AggroDisabled || !TryChooseAttackBehavior())
		{
			TrySetBehavior(Behavior.Idle);
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
		if (currAbility != null)
		{
			currAbility.UpdateAuthority(dt);
			if (currAbility.IsDone())
			{
				SetBehavior(Behavior.None);
				ChooseNewBehavior();
			}
		}
	}

	private void OnUpdateRemote(float dt)
	{
		if (currAbility != null)
		{
			currAbility.UpdateRemote(dt);
		}
	}

	public void InstantKill()
	{
		if (hp > 0)
		{
			SetHP(0);
			lastHitTime = Time.timeAsDouble;
			if (entity.IsAuthority())
			{
				SetBehavior(Behavior.Closed);
			}
		}
	}

	public void OnHitByClub(GRTool tool, GameHitData hit)
	{
		if (currBehavior == Behavior.Dying)
		{
			return;
		}
		SetHP(hp - hit.hitAmount);
		lastHitTime = Time.timeAsDouble;
		if (hp <= 0)
		{
			hp = 0;
			if (entity.IsAuthority())
			{
				SetBehavior(Behavior.Closed);
			}
		}
		else
		{
			lastSeenTargetPosition = tool.transform.position;
			lastSeenTargetTime = Time.timeAsDouble;
		}
	}

	public void OnHitByShield(GRTool tool, GameHitData hit)
	{
		OnHitByClub(tool, hit);
	}

	private void OnTriggerEnter(Collider collider)
	{
		if (currBehavior != Behavior.AttackLaser)
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
		if (player != null && player.gamePlayer.IsLocal() && Time.time > lastHitPlayerTime + minTimeBetweenHits)
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
	}

	public void OnGameEntitySerialize(BinaryWriter writer)
	{
		byte value = (byte)currBehavior;
		int value2 = ((targetPlayer == null) ? (-1) : targetPlayer.ActorNumber);
		writer.Write(value);
		writer.Write(hp);
		writer.Write(value2);
	}

	public void OnGameEntityDeserialize(BinaryReader reader)
	{
		Behavior newBehavior = (Behavior)reader.ReadByte();
		int hP = reader.ReadInt32();
		int playerID = reader.ReadInt32();
		SetHP(hP);
		SetBehavior(newBehavior, force: true);
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
			case GameHitType.Shield:
				OnHitByShield(gameComponent, hit);
				break;
			}
		}
	}
}

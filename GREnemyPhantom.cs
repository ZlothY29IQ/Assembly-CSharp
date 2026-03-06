using System.Collections.Generic;
using System.IO;
using Photon.Pun;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.AI;

public class GREnemyPhantom : MonoBehaviour, IGameEntityComponent, IGameEntitySerialize, IGameAgentComponent, IGameEntityDebugComponent
{
	public enum Behavior
	{
		Mine,
		Idle,
		Alert,
		Return,
		Rage,
		Chase,
		Attack,
		Investigate,
		Jump,
		Count
	}

	public enum BodyState
	{
		Destroyed,
		Bones,
		Count
	}

	public GameEntity entity;

	public GameAgent agent;

	public GRArmorEnemy armor;

	public GRAttributes attributes;

	public Animation anim;

	public GRSenseNearby senseNearby;

	public GRSenseLineOfSight senseLineOfSight;

	public GRAbilityIdle abilityMine;

	public AbilitySound soundMine;

	public GRAbilityIdle abilityIdle;

	public GRAbilityWatch abilityRage;

	public AbilitySound soundRage;

	public GRAbilityWatch abilityAlert;

	public AbilitySound soundAlert;

	public GRAbilityChase abilityChase;

	public AbilitySound soundChase;

	public GRAbilityMoveToTarget abilityReturn;

	public AbilitySound soundReturn;

	public GRAbilityAttackLatchOn abilityAttack;

	public AbilitySound soundAttack;

	public GRAbilityMoveToTarget abilityInvestigate;

	public GRAbilityJump abilityJump;

	public List<Renderer> bones;

	public List<Renderer> always;

	public Transform coreMarker;

	public GRCollectible corePrefab;

	public Transform headTransform;

	public float attackRange = 2f;

	public float hearingRadius = 7f;

	public List<VRRig> rigsNearby;

	public GameLight attackLight;

	public GameLight negativeLight;

	[ReadOnly]
	[SerializeField]
	private GRPatrolPath patrolPath;

	private Transform idleLocation;

	public NavMeshAgent navAgent;

	public AudioSource audioSource;

	public double lastStateChange;

	private Vector3? investigateLocation;

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
	public Vector3 searchPosition;

	[ReadOnly]
	public double behaviorStartTime;

	private Rigidbody rigidBody;

	private List<Collider> colliders;

	private static List<VRRig> tempRigs = new List<VRRig>(16);

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
		agent.onBodyStateChanged += OnNetworkBodyStateChange;
		agent.onBehaviorStateChanged += OnNetworkBehaviorStateChange;
		senseNearby.Setup(headTransform, entity);
	}

	public void OnEntityInit()
	{
		abilityMine.Setup(agent, anim, audioSource, base.transform, headTransform, senseLineOfSight);
		abilityIdle.Setup(agent, anim, audioSource, base.transform, headTransform, senseLineOfSight);
		abilityRage.Setup(agent, anim, audioSource, base.transform, headTransform, senseLineOfSight);
		abilityAlert.Setup(agent, anim, audioSource, base.transform, headTransform, senseLineOfSight);
		abilityChase.Setup(agent, anim, audioSource, base.transform, headTransform, senseLineOfSight);
		abilityReturn.Setup(agent, anim, audioSource, base.transform, headTransform, senseLineOfSight);
		abilityAttack.Setup(agent, anim, audioSource, base.transform, headTransform, senseLineOfSight);
		abilityInvestigate.Setup(agent, anim, audioSource, base.transform, headTransform, senseLineOfSight);
		abilityJump.Setup(agent, anim, audioSource, base.transform, headTransform, senseLineOfSight);
		int num = (int)entity.createData;
		Setup(num);
		if ((bool)entity && (bool)entity.manager && (bool)entity.manager.ghostReactorManager && (bool)entity.manager.ghostReactorManager.reactor)
		{
			foreach (GRBonusEntry enemyGlobalBonuse in entity.manager.ghostReactorManager.reactor.GetCurrLevelGenConfig().enemyGlobalBonuses)
			{
				attributes.AddBonus(enemyGlobalBonuse);
			}
		}
		navAgent.speed = attributes.CalculateFinalFloatValueForAttribute(GRAttributeType.PatrolSpeed);
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

	private void Setup(long createData)
	{
		SetPatrolPath(createData);
		if (patrolPath != null && patrolPath.patrolNodes.Count > 0)
		{
			nextPatrolNode = 0;
			target = patrolPath.patrolNodes[0];
			idleLocation = target;
			SetBehavior(Behavior.Return, force: true);
		}
		else
		{
			SetBehavior(Behavior.Mine, force: true);
		}
		SetBodyState(BodyState.Bones, force: true);
		if (attackLight != null)
		{
			attackLight.gameObject.SetActive(value: false);
		}
		if (negativeLight != null)
		{
			negativeLight.gameObject.SetActive(value: false);
		}
		GREnemy.HideRenderers(bones, hide: false);
		GREnemy.HideRenderers(always, hide: false);
	}

	private void OnAgentJumpRequested(Vector3 start, Vector3 end, float heightScale, float speedScale)
	{
		abilityJump.SetupJump(start, end, heightScale, speedScale);
		SetBehavior(Behavior.Jump);
	}

	public void OnNetworkBehaviorStateChange(byte newState)
	{
		if (newState >= 0 && newState < 9)
		{
			SetBehavior((Behavior)newState);
		}
	}

	public void OnNetworkBodyStateChange(byte newState)
	{
		if (newState >= 0 && newState < 2)
		{
			SetBodyState((BodyState)newState);
		}
	}

	public void SetPatrolPath(long createData)
	{
		GRPatrolPath gRPatrolPath = GhostReactorManager.Get(entity).reactor.GetPatrolPath(createData);
		patrolPath = gRPatrolPath;
	}

	public void SetNextPatrolNode(int nextPatrolNode)
	{
		this.nextPatrolNode = nextPatrolNode;
	}

	public void SetHP(int hp)
	{
		this.hp = hp;
	}

	public void SetBehavior(Behavior newBehavior, bool force = false)
	{
		if (currBehavior == newBehavior && !force)
		{
			return;
		}
		lastStateChange = PhotonNetwork.Time;
		switch (currBehavior)
		{
		case Behavior.Chase:
			abilityChase.Stop();
			if (negativeLight != null)
			{
				negativeLight.gameObject.SetActive(value: false);
			}
			break;
		case Behavior.Return:
			abilityReturn.Stop();
			break;
		case Behavior.Mine:
			abilityMine.Stop();
			break;
		case Behavior.Idle:
			abilityIdle.Stop();
			break;
		case Behavior.Rage:
			abilityRage.Stop();
			break;
		case Behavior.Alert:
			abilityAlert.Stop();
			break;
		case Behavior.Attack:
			abilityAttack.Stop();
			if (attackLight != null)
			{
				attackLight.gameObject.SetActive(value: false);
			}
			break;
		case Behavior.Investigate:
			abilityInvestigate.Stop();
			break;
		case Behavior.Jump:
			abilityJump.Stop();
			break;
		}
		currBehavior = newBehavior;
		behaviorStartTime = Time.timeAsDouble;
		switch (currBehavior)
		{
		case Behavior.Return:
			abilityReturn.Start();
			soundReturn.Play(audioSource);
			abilityReturn.SetTarget(idleLocation);
			break;
		case Behavior.Chase:
			abilityChase.Start();
			soundChase.Play(audioSource);
			abilityChase.SetTargetPlayer(agent.targetPlayer);
			investigateLocation = null;
			if (negativeLight != null)
			{
				negativeLight.gameObject.SetActive(value: true);
			}
			break;
		case Behavior.Attack:
			abilityAttack.Start();
			abilityAttack.SetTargetPlayer(agent.targetPlayer);
			investigateLocation = null;
			soundAttack.Play(audioSource);
			if (attackLight != null)
			{
				attackLight.gameObject.SetActive(value: true);
			}
			break;
		case Behavior.Idle:
			abilityIdle.Start();
			break;
		case Behavior.Mine:
			abilityMine.Start();
			break;
		case Behavior.Rage:
			abilityRage.Start();
			soundRage.Play(audioSource);
			break;
		case Behavior.Alert:
			abilityAlert.Start();
			soundAlert.Play(audioSource);
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

	public void SetBodyState(BodyState newBodyState, bool force = false)
	{
		if (currBodyState != newBodyState || force)
		{
			if (currBodyState == BodyState.Bones)
			{
				hp = attributes.CalculateFinalValueForAttribute(GRAttributeType.HPMax);
			}
			currBodyState = newBodyState;
			if (currBodyState == BodyState.Bones)
			{
				hp = attributes.CalculateFinalValueForAttribute(GRAttributeType.HPMax);
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
			break;
		case BodyState.Bones:
			armor.SetHp(0);
			break;
		}
	}

	private void Update()
	{
		OnUpdate(Time.deltaTime);
	}

	private void ChooseNewBehavior()
	{
		if (!GhostReactorManager.AggroDisabled && senseNearby.IsAnyoneNearby())
		{
			investigateLocation = null;
			SetBehavior(Behavior.Alert);
			return;
		}
		investigateLocation = AbilityHelperFunctions.GetLocationToInvestigate(base.transform.position, hearingRadius, investigateLocation);
		if (investigateLocation.HasValue)
		{
			abilityInvestigate.SetTargetPos(investigateLocation.Value);
			SetBehavior(Behavior.Investigate);
		}
		else if (currBehavior == Behavior.Investigate)
		{
			if (idleLocation != null)
			{
				SetBehavior(Behavior.Return);
			}
			else
			{
				SetBehavior(Behavior.Idle);
			}
		}
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
		case Behavior.Mine:
			ChooseNewBehavior();
			break;
		case Behavior.Return:
			abilityReturn.SetTarget(idleLocation);
			abilityReturn.Think(dt);
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
		case Behavior.Alert:
		case Behavior.Rage:
		case Behavior.Attack:
			break;
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
		case Behavior.Mine:
			abilityMine.UpdateAuthority(dt);
			if (idleLocation != null)
			{
				GameAgent.UpdateFacingDir(base.transform, agent.navAgent, idleLocation.forward, 180f);
			}
			break;
		case Behavior.Rage:
			abilityRage.UpdateAuthority(dt);
			if (abilityRage.IsDone())
			{
				SetBehavior(Behavior.Chase);
			}
			break;
		case Behavior.Alert:
			UpdateAlert(dt);
			break;
		case Behavior.Return:
			abilityReturn.UpdateAuthority(dt);
			if (abilityReturn.IsDone())
			{
				SetBehavior(Behavior.Mine);
			}
			break;
		case Behavior.Chase:
		{
			abilityChase.UpdateAuthority(dt);
			if (abilityChase.IsDone())
			{
				SetBehavior(Behavior.Return);
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
		}
	}

	public void OnUpdateRemote(float dt)
	{
		switch (currBehavior)
		{
		case Behavior.Attack:
			abilityAttack.UpdateRemote(dt);
			break;
		case Behavior.Investigate:
			abilityInvestigate.UpdateRemote(dt);
			break;
		case Behavior.Chase:
			abilityChase.UpdateRemote(dt);
			break;
		case Behavior.Return:
			abilityReturn.UpdateRemote(dt);
			break;
		case Behavior.Jump:
			abilityJump.UpdateRemote(dt);
			break;
		case Behavior.Rage:
			break;
		}
	}

	public void UpdateAlert(float dt)
	{
		abilityAlert.SetTargetPlayer(agent.targetPlayer);
		abilityAlert.UpdateAuthority(dt);
		_ = Time.timeAsDouble;
		float outDistanceSq;
		if (!senseNearby.IsAnyoneNearby())
		{
			SetBehavior(Behavior.Return);
		}
		else if (abilityAlert.IsDone() && senseNearby.PickClosest(out outDistanceSq) != null)
		{
			SetBehavior(Behavior.Rage);
		}
	}

	private void OnTriggerEnter(Collider collider)
	{
		if (currBodyState == BodyState.Destroyed || currBehavior != Behavior.Attack)
		{
			return;
		}
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

	public void GetDebugTextLines(out List<string> strings)
	{
		strings = new List<string>();
		strings.Add($"State: <color=\"yellow\">{currBehavior.ToString()}<color=\"white\"> HP: <color=\"yellow\">{hp}<color=\"white\">");
	}

	public void OnGameEntitySerialize(BinaryWriter writer)
	{
		byte value = (byte)currBehavior;
		byte value2 = (byte)currBodyState;
		byte value3 = (byte)nextPatrolNode;
		writer.Write(value);
		writer.Write(value2);
		writer.Write(hp);
		writer.Write(value3);
	}

	public void OnGameEntityDeserialize(BinaryReader reader)
	{
		Behavior newBehavior = (Behavior)reader.ReadByte();
		BodyState newBodyState = (BodyState)reader.ReadByte();
		int hP = reader.ReadInt32();
		byte b = reader.ReadByte();
		SetPatrolPath(entity.createData);
		SetNextPatrolNode(b);
		SetHP(hP);
		SetBehavior(newBehavior, force: true);
		SetBodyState(newBodyState, force: true);
	}
}

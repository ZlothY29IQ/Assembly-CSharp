using System.Collections.Generic;
using GorillaExtensions;
using GorillaGameModes;
using GorillaTagScripts.VirtualStumpCustomMaps;
using GT_CustomMapSupportRuntime;
using UnityEngine;
using UnityEngine.AI;

public class CustomMapsAIBehaviourController : MonoBehaviour, IGameEntityComponent
{
	public enum CustomMapsAIBehaviour
	{
		Search,
		Chase,
		Attack
	}

	private static readonly int movementSpeedParamIndex = Animator.StringToHash("MovementSpeed");

	public GameEntity entity;

	public GameAgent agent;

	public GRAttributes attributes;

	private Animator[] animators;

	public short luaAgentID;

	private List<VRRig> tempRigs = new List<VRRig>(20);

	private static RaycastHit[] visibilityHits = new RaycastHit[10];

	private LayerMask visibilityLayerMask;

	private bool allowTargetingTaggedPlayers;

	private Dictionary<AgentBehaviours, CustomMapsBehaviourBase> behaviourDict = new Dictionary<AgentBehaviours, CustomMapsBehaviourBase>(8);

	private List<AgentBehaviours> usedBehaviours = new List<AgentBehaviours>(8);

	private AgentBehaviours currentBehaviour;

	private int currentBehaviourIndex;

	private const int BEHAVIOUR_COUNT = 3;

	public GRPlayer TargetPlayer { get; private set; }

	private void Awake()
	{
		TargetPlayer = null;
		visibilityLayerMask = LayerMask.GetMask("Default", "Gorilla Object");
		agent.onBehaviorStateChanged += OnNetworkBehaviourStateChanged;
	}

	private void OnDestroy()
	{
		agent.onBehaviorStateChanged -= OnNetworkBehaviourStateChanged;
	}

	public void SetTarget(GRPlayer newTarget)
	{
		if (newTarget.IsNull())
		{
			ClearTarget();
		}
		else
		{
			TargetPlayer = newTarget;
		}
	}

	public void ClearTarget()
	{
		TargetPlayer = null;
	}

	private void Update()
	{
		OnThink();
		UpdateAnimators();
	}

	private void OnTriggerEnter(Collider collider)
	{
		behaviourDict[currentBehaviour]?.OnTriggerEnter(collider);
	}

	private void InitAnimators()
	{
		animators = base.gameObject.GetComponentsInChildren<Animator>();
	}

	private void UpdateAnimators()
	{
		if (!animators.IsNullOrEmpty())
		{
			float magnitude = agent.navAgent.velocity.magnitude;
			for (int i = 0; i < animators.Length; i++)
			{
				animators[i].SetFloat(movementSpeedParamIndex, magnitude);
			}
		}
	}

	public void PlayAnimation(string stateName, float blendTime = 0f)
	{
		for (int i = 0; i < animators.Length; i++)
		{
			animators[i].CrossFadeInFixedTime(stateName, blendTime);
		}
	}

	public bool IsAnimationPlaying(string stateName)
	{
		int num = 0;
		if (num < animators.Length)
		{
			Animator animator = animators[num];
			AnimatorStateInfo currentAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
			if (currentAnimatorStateInfo.IsName(stateName) && currentAnimatorStateInfo.normalizedTime < 1f)
			{
				return true;
			}
			if (animator.GetNextAnimatorStateInfo(0).IsName(stateName))
			{
				return true;
			}
			return false;
		}
		return false;
	}

	public void SetupBehaviours(AIAgent aiAgent)
	{
		allowTargetingTaggedPlayers = aiAgent.allowTargetingTaggedPlayers;
		for (int i = 0; i < aiAgent.agentBehaviours.Count; i++)
		{
			if (!usedBehaviours.Contains(aiAgent.agentBehaviours[i]))
			{
				switch (aiAgent.agentBehaviours[i])
				{
				case AgentBehaviours.Chase:
					behaviourDict[AgentBehaviours.Chase] = new CustomMapsChaseBehaviour(this, aiAgent);
					break;
				case AgentBehaviours.Search:
					behaviourDict[AgentBehaviours.Search] = new CustomMapsSearchBehaviour(this, aiAgent);
					break;
				case AgentBehaviours.Attack:
					behaviourDict[AgentBehaviours.Attack] = new CustomMapsAttackBehaviour(this, aiAgent);
					break;
				default:
					continue;
				}
				usedBehaviours.Add(aiAgent.agentBehaviours[i]);
			}
		}
	}

	public void StopMoving()
	{
		RequestDestination(base.transform.position);
	}

	public void RequestDestination(Vector3 destination)
	{
		if (entity.IsAuthority())
		{
			agent.RequestDestination(destination);
		}
	}

	private void OnThink()
	{
		if (!entity.IsAuthority() || behaviourDict == null || behaviourDict.Count == 0)
		{
			return;
		}
		int num = -1;
		if (currentBehaviourIndex != -1 && behaviourDict[usedBehaviours[currentBehaviourIndex]].CanContinueExecuting())
		{
			num = currentBehaviourIndex;
		}
		else
		{
			for (int i = 0; i < usedBehaviours.Count; i++)
			{
				if (i != currentBehaviourIndex && behaviourDict[usedBehaviours[i]].CanExecute())
				{
					num = i;
					break;
				}
			}
		}
		if (num != -1)
		{
			if (currentBehaviourIndex != num)
			{
				currentBehaviourIndex = num;
				currentBehaviour = usedBehaviours[num];
				agent.RequestBehaviorChange((byte)currentBehaviour);
			}
			behaviourDict[currentBehaviour].Execute();
		}
	}

	private void OnNetworkBehaviourStateChanged(byte newstate)
	{
		if (newstate < 0 || newstate >= 3)
		{
			return;
		}
		if (behaviourDict.ContainsKey((AgentBehaviours)newstate))
		{
			if (currentBehaviour != (AgentBehaviours)newstate && behaviourDict.ContainsKey(currentBehaviour))
			{
				behaviourDict[currentBehaviour].ResetBehavior();
			}
			currentBehaviour = (AgentBehaviours)newstate;
			behaviourDict[currentBehaviour].NetExecute();
		}
	}

	public void OnEntityInit()
	{
		bool flag = AISpawnManager.HasInstance && AISpawnManager.instance != null;
		if (flag || !(MapSpawnManager.instance == null))
		{
			entity.transform.parent = (flag ? AISpawnManager.instance.transform : MapSpawnManager.instance.transform);
			AIAgent.UnpackCreateData(entity.createData, out var entityTypeID, out luaAgentID);
			if (flag && AISpawnManager.instance.SpawnEnemy(entityTypeID, out AIAgent newEnemy))
			{
				SetupNewEnemy(newEnemy);
				return;
			}
			if (!flag && MapSpawnManager.instance.SpawnEntity(entityTypeID, out MapEntity newEnemy2))
			{
				SetupNewEnemy((AIAgent)newEnemy2);
				return;
			}
			GTDev.LogError("CustomMapsAIBehaviourController::OnEntityInit could not spawn enemy");
			Object.Destroy(base.gameObject);
		}
	}

	private void SetupNewEnemy(AIAgent newEnemy)
	{
		newEnemy.gameObject.SetActive(value: true);
		newEnemy.transform.parent = entity.transform;
		newEnemy.transform.localPosition = Vector3.zero;
		newEnemy.transform.localRotation = Quaternion.identity;
		InitAnimators();
		NavMeshAgent component = entity.gameObject.GetComponent<NavMeshAgent>();
		if (component.IsNull())
		{
			GTDev.LogError("nav mesh agent is null");
			Object.Destroy(base.gameObject);
			return;
		}
		component.agentTypeID = GetNavAgentType(newEnemy.navAgentType);
		component.speed = newEnemy.movementSpeed;
		component.angularSpeed = newEnemy.turnSpeed;
		component.acceleration = newEnemy.acceleration;
		SetupBehaviours(newEnemy);
	}

	private int GetNavAgentType(NavAgentType navType)
	{
		int settingsCount = NavMesh.GetSettingsCount();
		int agentTypeID = NavMesh.GetSettingsByIndex(0).agentTypeID;
		for (int i = 0; i < settingsCount; i++)
		{
			NavMeshBuildSettings settingsByIndex = NavMesh.GetSettingsByIndex(i);
			if (NavMesh.GetSettingsNameFromID(settingsByIndex.agentTypeID) == navType.ToString())
			{
				agentTypeID = settingsByIndex.agentTypeID;
				break;
			}
		}
		return agentTypeID;
	}

	public void OnEntityDestroy()
	{
	}

	public void OnEntityStateChange(long prevState, long newState)
	{
	}

	public GRPlayer FindBestTarget(Vector3 sourcePos, float maxRange, float maxRangeSq, float minDotVal)
	{
		float num = 0f;
		GRPlayer result = null;
		tempRigs.Clear();
		tempRigs.Add(VRRig.LocalRig);
		VRRigCache.Instance.GetAllUsedRigs(tempRigs);
		Vector3 rhs = base.transform.rotation * Vector3.forward;
		for (int i = 0; i < tempRigs.Count; i++)
		{
			GRPlayer component = tempRigs[i].GetComponent<GRPlayer>();
			if (!IsTargetInRange(sourcePos, component, maxRangeSq, out var toTarget))
			{
				continue;
			}
			float num2 = 0f;
			if (toTarget.sqrMagnitude > 0f)
			{
				num2 = Mathf.Sqrt(toTarget.magnitude);
			}
			float num3 = Vector3.Dot(toTarget.normalized, rhs);
			if (!(num3 < minDotVal))
			{
				float num4 = Mathf.Lerp(0f, 0.5f, 1f - num2 / maxRange);
				float num5 = Mathf.Lerp(0f, 0.5f, (1f - minDotVal - (1f - num3)) / (1f - minDotVal));
				if (num4 + num5 > num && IsTargetVisible(sourcePos, component, maxRange))
				{
					num = num4 + num5;
					result = component;
				}
			}
		}
		return result;
	}

	public bool IsTargetVisible(Vector3 startPos, GRPlayer target, float maxDist)
	{
		if (!IsTargetable(target))
		{
			return false;
		}
		int num = Physics.RaycastNonAlloc(new Ray(startPos, target.transform.position - startPos), visibilityHits, Mathf.Min(Vector3.Distance(target.transform.position, startPos), maxDist), visibilityLayerMask.value, QueryTriggerInteraction.Ignore);
		for (int i = 0; i < num; i++)
		{
			if (visibilityHits[i].transform != base.transform && !visibilityHits[i].transform.IsChildOf(base.transform))
			{
				return false;
			}
		}
		return true;
	}

	public bool IsTargetInRange(Vector3 startPos, GRPlayer target, float maxRangeSq, out Vector3 toTarget)
	{
		toTarget = Vector3.zero;
		if (!IsTargetable(target))
		{
			return false;
		}
		Vector3 position = target.transform.position;
		toTarget = position - startPos;
		return toTarget.sqrMagnitude <= maxRangeSq;
	}

	public bool IsTargetable(GRPlayer potentialTarget)
	{
		if (potentialTarget.IsNull())
		{
			return false;
		}
		if (potentialTarget.State == GRPlayer.GRPlayerState.Ghost)
		{
			return false;
		}
		if (potentialTarget.MyRig.isLocal)
		{
			if (CustomMapManager.IsLocalPlayerInVirtualStump())
			{
				return false;
			}
		}
		else if (CustomMapManager.IsRemotePlayerInVirtualStump(potentialTarget.MyRig.OwningNetPlayer.UserId))
		{
			return false;
		}
		if (!allowTargetingTaggedPlayers && GameMode.ActiveGameMode.GameType() != GameModeType.Custom && GameMode.LocalIsTagged(potentialTarget.MyRig.OwningNetPlayer))
		{
			return false;
		}
		return true;
	}
}

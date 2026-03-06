using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using GorillaLocomotion;
using GorillaTagScripts.GhostReactor;
using JetBrains.Annotations;
using Unity.XR.CoreUtils;
using UnityEngine;

public class GREnemyBossMoon : MonoBehaviour, IGameEntityComponent, IGameEntitySerialize, IGameHittable, IGameAgentComponent, IGameEntityDebugComponent, IGRSummoningEntity
{
	[Serializable]
	public class PhaseDef
	{
		public int minHP;

		public List<Behavior> attacks;

		public List<Behavior> comboAttacks;

		public bool restAfterAttack = true;

		public float comboAttackChance = 0.25f;

		public bool allowConsecutiveCombos;

		public List<Behavior> summons;

		public int maxSimultaneousEnemies = 6;

		public int maxEnemiesForReveal = 4;

		public int attacksBetweenSummons = 4;

		public bool retreatAfterSummon = true;

		public float randomSummonChance = 0.1f;

		public bool runawayAfterPhase;
	}

	[Serializable]
	public class LootPhase
	{
		public GREnemyType enemyType;

		public GRBreakableItemSpawnConfig lootTable;
	}

	public enum Behavior
	{
		HiddenIdle,
		Idle,
		Reveal,
		Exposed,
		ExposedIdle,
		Stagger,
		Dying,
		AttackTentacle00,
		AttackTentacle01,
		AttackTentacle02,
		AttackTentacle03,
		AttackTentacle04,
		AttackTentacle05,
		AttackQuickTentacle00,
		AttackQuickTentacle01,
		AttackQuickTentacle02,
		AttackQuickTentacle03,
		AttackTongue,
		SummonStart,
		SummonEnd,
		Summon01,
		Summon02,
		Summon03,
		Summon04,
		RetreatStart,
		RetreatEnd,
		RetreatIdle,
		DyingIdle,
		Runaway,
		AttackTongueSwipe,
		NextPhase,
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

	public GameHittable hittable;

	[SerializeField]
	private GRAttributes attributes;

	public List<PhaseDef> phases;

	private int internalPhaseIndex = -1;

	public List<LootPhase> lootPhases;

	public GRSenseNearby senseNearby;

	public GRSenseLineOfSight senseLineOfSight;

	public List<GREnemyBossMoonEye> eyes;

	public GRSpherePushVolume eyesPushVolume;

	public Animation anim;

	public GRAbilityIdle abilityReveal;

	private bool firstTimeReveal = true;

	public GRAbilityIdle abilityIdle;

	public GRAbilityIdle abilityHiddenIdle;

	public GRBossMoonTentacleAttack abilityAttackTentacle00;

	public GRBossMoonTentacleAttack abilityAttackTentacle01;

	public GRBossMoonTentacleAttack abilityAttackTentacle02;

	public GRBossMoonTentacleAttack abilityAttackTentacle03;

	public GRBossMoonTentacleAttack abilityAttackTentacle04;

	public GRBossMoonTentacleAttack abilityAttackTentacle05;

	public GRBossMoonTentacleAttack abilityAttackQuickTentacle00;

	public GRBossMoonTentacleAttack abilityAttackQuickTentacle01;

	public GRBossMoonTentacleAttack abilityAttackQuickTentacle02;

	public GRBossMoonTentacleAttack abilityAttackQuickTentacle03;

	public GRBossMoonTentacleAttack abilityAttackTongue01;

	public GRBossMoonTentacleAttack abilityAttackTongueSwipe01;

	public GRAbilityIdle abilitySummonStart;

	public GRAbilityIdle abilitySummonEnd;

	public GRAbilitySummon abilitySummon01;

	public GRAbilitySummon abilitySummon02;

	public GRAbilitySummon abilitySummon03;

	public GRAbilitySummon abilitySummon04;

	public GRAbilityIdle abilityRetreatStart;

	public GRAbilityIdle abilityRetreatEnd;

	public GRAbilityIdle abilityRetreatIdle;

	public GRAbilityIdle abilityExposed;

	public GRAbilityIdle abilityExposedIdle;

	public GRAbilityDie abilityDie;

	public GRAbilityDie abilityDieIdle;

	public GRAbilityDie abilityRunaway;

	public GRAbilityIdle abilityNextPhase;

	private GRAbilityBase[] abilities;

	private GRAbilityBase currAbility;

	private GRAbilitySummon currSummon;

	public GRAbilityAgent abilityAgent;

	public List<Renderer> bones;

	public List<Renderer> always;

	public Transform headTransform;

	public AudioSource audioSource;

	public AudioClip damagedSound;

	public float damagedSoundVolume;

	public List<AudioClip> damagedSounds;

	private int damagedSoundIndex;

	public GameObject fxDamaged;

	public GameObject[] gravActivators;

	private GameObject currentGravActivator;

	public Renderer bodyRenderer;

	public Material[] defaultBodyMaterials;

	public Material[] shockedBodyMaterials;

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

	private Behavior lastBehavior;

	private bool restAfterAttack;

	private int consecutiveCombos;

	private int attacksAfterSummon = 3;

	private float waitInRetreat;

	private double lastJumpEndtime;

	public bool canChaseJump = true;

	public float chaseJumpDistance = 5f;

	public float chaseJumpMinInterval = 1f;

	public float minChaseJumpDistance = 2f;

	public float knockbackImpulse = 11f;

	public Transform knockbackTransform;

	private Rigidbody rigidBody;

	private List<Collider> colliders;

	private float lastHitPlayerTime;

	private float minTimeBetweenHits = 2f;

	public float hearingRadius = 5f;

	public List<GREnemyBossMoonColliderHelper> shockColliders;

	public List<GRSquishVolume> squishVolumes;

	public CameraShakeDispatcher cameraShaker;

	private List<int> trackedEntities;

	private List<GameEntity> trackedGameEntities;

	private GRAdaptiveMusicController adaptiveMusicController;

	private bool triggerNextMusicTransition;

	private static List<VRRig> tempRigs = new List<VRRig>(16);

	private static List<Behavior> tempPotentialAttacks = new List<Behavior>(16);

	private Coroutine tryHitPlayerCoroutine;

	private Coroutine tryShockPlayerCoroutine;

	public bool BossHasRevealed { get; private set; }

	public GRAbilityBase CurrAbility => currAbility;

	private void Awake()
	{
		trackedEntities = new List<int>(16);
		trackedGameEntities = new List<GameEntity>(16);
		rigidBody = GetComponent<Rigidbody>();
		colliders = new List<Collider>(4);
		GetComponentsInChildren(colliders);
		agent.onBodyStateChanged += OnNetworkBodyStateChange;
		agent.onBehaviorStateChanged += OnNetworkBehaviorStateChange;
		abilities = new GRAbilityBase[32];
		adaptiveMusicController = UnityEngine.Object.FindObjectOfType<GRAdaptiveMusicController>();
	}

	public void OnEntityInit()
	{
		currBehavior = Behavior.None;
		currAbility = null;
		SetupAbility(Behavior.HiddenIdle, abilityHiddenIdle, agent, anim, audioSource, null, null, null);
		SetupAbility(Behavior.Reveal, abilityReveal, agent, anim, audioSource, null, null, null);
		SetupAbility(Behavior.Idle, abilityIdle, agent, anim, audioSource, null, null, null);
		SetupAbility(Behavior.Exposed, abilityExposed, agent, anim, audioSource, null, null, null);
		SetupAbility(Behavior.ExposedIdle, abilityExposedIdle, agent, anim, audioSource, null, null, null);
		SetupAbility(Behavior.AttackTongue, abilityAttackTongue01, agent, anim, audioSource, base.transform, headTransform, null);
		SetupAbility(Behavior.AttackTongueSwipe, abilityAttackTongueSwipe01, agent, anim, audioSource, base.transform, headTransform, null);
		SetupAbility(Behavior.AttackTentacle00, abilityAttackTentacle00, agent, anim, audioSource, base.transform, headTransform, null);
		SetupAbility(Behavior.AttackTentacle01, abilityAttackTentacle01, agent, anim, audioSource, base.transform, headTransform, null);
		SetupAbility(Behavior.AttackTentacle02, abilityAttackTentacle02, agent, anim, audioSource, base.transform, headTransform, null);
		SetupAbility(Behavior.AttackTentacle03, abilityAttackTentacle03, agent, anim, audioSource, base.transform, headTransform, null);
		SetupAbility(Behavior.AttackTentacle04, abilityAttackTentacle04, agent, anim, audioSource, base.transform, headTransform, null);
		SetupAbility(Behavior.AttackTentacle05, abilityAttackTentacle05, agent, anim, audioSource, base.transform, headTransform, null);
		SetupAbility(Behavior.AttackQuickTentacle00, abilityAttackQuickTentacle00, agent, anim, audioSource, base.transform, headTransform, null);
		SetupAbility(Behavior.AttackQuickTentacle01, abilityAttackQuickTentacle01, agent, anim, audioSource, base.transform, headTransform, null);
		SetupAbility(Behavior.AttackQuickTentacle02, abilityAttackQuickTentacle02, agent, anim, audioSource, base.transform, headTransform, null);
		SetupAbility(Behavior.AttackQuickTentacle03, abilityAttackQuickTentacle03, agent, anim, audioSource, base.transform, headTransform, null);
		SetupAbility(Behavior.SummonStart, abilitySummonStart, agent, anim, audioSource, base.transform, headTransform, null);
		SetupAbility(Behavior.SummonEnd, abilitySummonEnd, agent, anim, audioSource, base.transform, headTransform, null);
		SetupAbility(Behavior.Summon01, abilitySummon01, agent, anim, audioSource, base.transform, headTransform, null);
		SetupAbility(Behavior.Summon02, abilitySummon02, agent, anim, audioSource, base.transform, headTransform, null);
		SetupAbility(Behavior.Summon03, abilitySummon03, agent, anim, audioSource, base.transform, headTransform, null);
		SetupAbility(Behavior.Summon04, abilitySummon04, agent, anim, audioSource, base.transform, headTransform, null);
		SetupAbility(Behavior.RetreatStart, abilityRetreatStart, agent, anim, audioSource, base.transform, headTransform, null);
		SetupAbility(Behavior.RetreatEnd, abilityRetreatEnd, agent, anim, audioSource, base.transform, headTransform, null);
		SetupAbility(Behavior.RetreatIdle, abilityRetreatIdle, agent, anim, audioSource, base.transform, headTransform, null);
		SetupAbility(Behavior.Dying, abilityDie, agent, anim, audioSource, base.transform, null, null);
		SetupAbility(Behavior.DyingIdle, abilityDieIdle, agent, anim, audioSource, base.transform, null, null);
		SetupAbility(Behavior.Runaway, abilityRunaway, agent, anim, audioSource, base.transform, null, null);
		SetupAbility(Behavior.NextPhase, abilityIdle, agent, anim, audioSource, null, null, null);
		senseNearby.Setup(headTransform, entity);
		Setup(entity.createData);
		if ((bool)entity && (bool)entity.manager && (bool)entity.manager.ghostReactorManager && (bool)entity.manager.ghostReactorManager.reactor)
		{
			GhostReactorLevelGenConfig currLevelGenConfig = entity.manager.ghostReactorManager.reactor.GetCurrLevelGenConfig();
			foreach (GRBonusEntry enemyGlobalBonuse in currLevelGenConfig.enemyGlobalBonuses)
			{
				attributes.AddBonus(enemyGlobalBonuse);
			}
			if (currLevelGenConfig.minEnemyKills.Count > 0)
			{
				GREnemyCount gREnemyCount = currLevelGenConfig.minEnemyKills[0];
				switch (gREnemyCount.EnemyType)
				{
				case GREnemyType.MoonBoss_Phase1:
					phases[0].runawayAfterPhase = true;
					break;
				case GREnemyType.MoonBoss_Phase2:
					phases[1].runawayAfterPhase = true;
					break;
				}
				GRBreakableItemSpawnConfig lootTableForType = GetLootTableForType(gREnemyCount.EnemyType);
				abilityDie.lootTable = lootTableForType;
				abilityRunaway.lootTable = lootTableForType;
			}
		}
		if (agent.navAgent != null)
		{
			agent.navAgent.autoTraverseOffMeshLink = false;
		}
		SetBehavior(Behavior.HiddenIdle, force: true);
		int num = CalcMaxHP();
		if (enemy != null)
		{
			enemy.SetMaxHP(num);
		}
		SetHP(num);
	}

	private GRBreakableItemSpawnConfig GetLootTableForType(GREnemyType enemyType)
	{
		for (int i = 0; i < lootPhases.Count; i++)
		{
			if (lootPhases[i].enemyType == enemyType)
			{
				return lootPhases[i].lootTable;
			}
		}
		return null;
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
		SetBehavior(Behavior.HiddenIdle, force: true);
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
		if (newState >= 0 && newState < 32)
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
		if (newBehavior == Behavior.Stagger)
		{
			return false;
		}
		SetBehavior(newBehavior);
		return true;
	}

	public void SetBehavior(Behavior newBehavior, bool force = false)
	{
		if (newBehavior < Behavior.HiddenIdle || (int)newBehavior >= abilities.Length)
		{
			Debug.LogErrorFormat("New Behavior Index is invalid {0} {1} {2}", (int)newBehavior, newBehavior, base.gameObject.name);
			return;
		}
		GRAbilityBase gRAbilityBase = abilities[(int)newBehavior];
		if (currBehavior == newBehavior && !force)
		{
			return;
		}
		switch (currBehavior)
		{
		case Behavior.AttackTongue:
		{
			for (int i = 0; i < eyes.Count; i++)
			{
				eyes[i].ResetEye();
			}
			consecutiveCombos = 0;
			attacksAfterSummon = 0;
			currSummon = null;
			KillAllSummoned(ignoreMonkeye: true);
			if (triggerNextMusicTransition)
			{
				triggerNextMusicTransition = false;
				if (adaptiveMusicController != null)
				{
					adaptiveMusicController.TransitionToNextTrack();
				}
			}
			break;
		}
		case Behavior.NextPhase:
			IncrementBossPhase();
			break;
		}
		Debug.LogFormat("Boss SetBehavior {0} -> {1}", currBehavior, newBehavior);
		if (currAbility != null)
		{
			currAbility.Stop();
		}
		lastBehavior = currBehavior;
		currBehavior = newBehavior;
		currAbility = gRAbilityBase;
		if (currAbility != null)
		{
			currAbility.Start();
		}
		switch (currBehavior)
		{
		case Behavior.Reveal:
			if (firstTimeReveal)
			{
				if (adaptiveMusicController != null)
				{
					adaptiveMusicController.Restart();
				}
				internalPhaseIndex = 0;
			}
			firstTimeReveal = false;
			BossHasRevealed = true;
			break;
		case Behavior.Exposed:
			ToggleShockColliders(toggle: false);
			break;
		case Behavior.Stagger:
			lastStaggerTime = Time.time;
			break;
		case Behavior.AttackTongue:
			ToggleShockColliders(toggle: true);
			break;
		case Behavior.Summon01:
		case Behavior.Summon02:
		case Behavior.Summon03:
		case Behavior.Summon04:
			currSummon = (GRAbilitySummon)currAbility;
			break;
		case Behavior.Dying:
		{
			KillAllSummoned();
			TurnOffGrav();
			for (int j = 0; j < eyes.Count; j++)
			{
				eyes[j].TrySetBehavior(GREnemyBossMoonEye.Behavior.Dying);
			}
			if (adaptiveMusicController != null)
			{
				adaptiveMusicController.TransitionToLastTrack();
			}
			ToggleShockColliders(toggle: false);
			break;
		}
		case Behavior.RetreatStart:
			TurnOnGrav();
			break;
		case Behavior.RetreatEnd:
			TurnOffGrav();
			break;
		case Behavior.Runaway:
			if (entity.manager.ghostReactorManager != null)
			{
				entity.manager.ghostReactorManager.InstantDeathForCurrentEnemies();
			}
			if (adaptiveMusicController != null)
			{
				adaptiveMusicController.TransitionToLastTrack();
			}
			break;
		}
		RefreshBody();
		if (entity.IsAuthority())
		{
			agent.RequestBehaviorChange((byte)currBehavior);
		}
	}

	public void SetSquishVolumeState(bool squishEnabled)
	{
		for (int i = 0; i < squishVolumes.Count; i++)
		{
			squishVolumes[i].overrideDisabled = !squishEnabled;
			squishVolumes[i].SliceUpdate();
		}
	}

	private int CalcMaxHP()
	{
		float difficultyScalingForCurrentFloor = entity.manager.ghostReactorManager.reactor.difficultyScalingForCurrentFloor;
		int result = (int)((float)attributes.CalculateFinalValueForAttribute(GRAttributeType.HPMax) * difficultyScalingForCurrentFloor);
		for (int i = 0; i < phases.Count; i++)
		{
			phases[i].minHP = Mathf.RoundToInt((float)phases[i].minHP * difficultyScalingForCurrentFloor);
		}
		return result;
	}

	public int GetCurrPhaseIndex()
	{
		if (phases == null)
		{
			return -1;
		}
		for (int i = 0; i < phases.Count; i++)
		{
			if (hp > phases[i].minHP)
			{
				return i;
			}
		}
		return phases.Count - 1;
	}

	public PhaseDef GetCurrPhase()
	{
		int currPhaseIndex = GetCurrPhaseIndex();
		if (currPhaseIndex < 0 || currPhaseIndex >= phases.Count)
		{
			return null;
		}
		return phases[currPhaseIndex];
	}

	public void RestoreFullHealth()
	{
		SetHP(CalcMaxHP());
	}

	public void HurtBossHP()
	{
		HurtBoss(100, entity.id, Vector3.zero);
	}

	public void KillAllEyes()
	{
		for (int i = 0; i < eyes.Count; i++)
		{
			eyes[i].InstantKill();
		}
	}

	public void KillAllSummoned()
	{
		KillAllSummoned(ignoreMonkeye: true);
	}

	public void KillAllSummoned(bool ignoreMonkeye = false, bool killAllEnemies = true)
	{
		int num = 0;
		for (int i = 0; i < trackedGameEntities.Count; i++)
		{
			if (trackedGameEntities[i] == null)
			{
				continue;
			}
			GREnemyChaser component = trackedGameEntities[i].GetComponent<GREnemyChaser>();
			if (component != null)
			{
				component.InstantDeath();
				num++;
				continue;
			}
			GREnemyRanged component2 = trackedGameEntities[i].GetComponent<GREnemyRanged>();
			if (component2 != null)
			{
				component2.InstantDeath();
				num++;
				continue;
			}
			GREnemyPest component3 = trackedGameEntities[i].GetComponent<GREnemyPest>();
			if (component3 != null)
			{
				component3.InstantDeath();
				num++;
				continue;
			}
			GREnemySummoner component4 = trackedGameEntities[i].GetComponent<GREnemySummoner>();
			if (component4 != null)
			{
				component4.InstantDeath();
				num++;
			}
			else if (!ignoreMonkeye)
			{
				GREnemyMonkeye component5 = trackedGameEntities[i].GetComponent<GREnemyMonkeye>();
				if (component5 != null)
				{
					component5.InstantDeath();
					num++;
				}
			}
		}
		if (killAllEnemies && entity.manager.ghostReactorManager != null)
		{
			entity.manager.ghostReactorManager.InstantDeathForCurrentEnemies();
		}
		Debug.Log($"Report killed all summon {num}");
	}

	public void GoBackPhase()
	{
		int currPhaseIndex = GetCurrPhaseIndex();
		if (currPhaseIndex <= 0)
		{
			Debug.LogWarning("GREnemyBossMoon - GoBackPhase - At first phase");
		}
		else
		{
			SetHP(phases[currPhaseIndex - 1].minHP);
		}
	}

	public void GoToNextPhase()
	{
		int currPhaseIndex = GetCurrPhaseIndex();
		if (currPhaseIndex >= 0 && currPhaseIndex < phases.Count)
		{
			SetHP(phases[currPhaseIndex].minHP);
		}
	}

	private bool IsSummon(Behavior behavior)
	{
		for (int i = 0; i < phases.Count; i++)
		{
			if (phases[i] != null && phases[i].summons != null && phases[i].summons.Contains(behavior))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsAnySummonBehavior(Behavior behavior)
	{
		if (currBehavior != Behavior.SummonStart && currBehavior != Behavior.SummonEnd && currBehavior != Behavior.Summon01 && currBehavior != Behavior.Summon02 && currBehavior != Behavior.Summon03)
		{
			return currBehavior == Behavior.Summon04;
		}
		return true;
	}

	public Behavior ChooseSummonForPhase()
	{
		PhaseDef currPhase = GetCurrPhase();
		if (currPhase == null)
		{
			return Behavior.None;
		}
		return ChooseRandomBehavior(currPhase.summons);
	}

	public Behavior ChooseAttackForPhase()
	{
		PhaseDef currPhase = GetCurrPhase();
		if (currPhase == null)
		{
			return Behavior.None;
		}
		return ChooseRandomBehavior(currPhase.attacks);
	}

	public Behavior ChooseRandomBehavior(List<Behavior> behaviors)
	{
		if (behaviors == null || behaviors.Count <= 0)
		{
			return Behavior.None;
		}
		int index = UnityEngine.Random.Range(0, behaviors.Count);
		return behaviors[index];
	}

	public void SetBodyState(BodyState newBodyState, bool force = false)
	{
		if (currBodyState != newBodyState || force)
		{
			currBodyState = newBodyState;
			if (currBodyState == BodyState.Destroyed)
			{
				GhostReactorManager.Get(entity).ReportEnemyDeath();
			}
			Debug.LogFormat("State Change {0} {1}", entity.id.index, currBodyState);
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
			GREnemy.HideRenderers(bones, hide: false);
			GREnemy.HideRenderers(always, hide: false);
			break;
		case BodyState.Bones:
			GREnemy.HideRenderers(bones, hide: false);
			GREnemy.HideRenderers(always, hide: false);
			break;
		case BodyState.Shell:
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
			if (currAbility.IsDone())
			{
				ChooseNewBehavior();
			}
			break;
		case Behavior.HiddenIdle:
			ChooseNewBehavior(forceAttack: true);
			break;
		case Behavior.RetreatIdle:
			waitInRetreat += dt * 12f;
			if (trackedEntities.Count <= 0 || waitInRetreat > 20f)
			{
				TrySetBehavior(Behavior.RetreatEnd);
			}
			break;
		}
	}

	private Behavior TryChooseAttackBehavior()
	{
		PhaseDef currPhase = GetCurrPhase();
		if (currBehavior == Behavior.HiddenIdle)
		{
			if (currPhase != null && trackedEntities.Count <= currPhase.maxEnemiesForReveal && senseNearby.IsAnyoneNearby(abilityReveal.GetRange(), firstTimeReveal))
			{
				return Behavior.Reveal;
			}
			return Behavior.None;
		}
		if (GhostReactorManager.AggroDisabled)
		{
			return Behavior.None;
		}
		if (currPhase == null)
		{
			return Behavior.None;
		}
		if (currPhase.summons != null && currPhase.summons.Count > 0 && attacksAfterSummon <= 0 && trackedEntities.Count < currPhase.maxSimultaneousEnemies)
		{
			attacksAfterSummon = currPhase.attacksBetweenSummons;
			if (currPhase.summons.Count > 0)
			{
				currSummon = (GRAbilitySummon)abilities[(int)currPhase.summons[0]];
				if (currSummon != null)
				{
					for (int i = trackedEntities.Count; i < currPhase.maxSimultaneousEnemies; i++)
					{
						currSummon.ForceSpawn();
					}
				}
			}
		}
		List<Behavior> list = currPhase.attacks;
		if (currPhase.comboAttacks != null && currPhase.comboAttacks.Count > 0 && ((currPhase.allowConsecutiveCombos && consecutiveCombos < 3) || consecutiveCombos <= 0) && UnityEngine.Random.value < currPhase.comboAttackChance)
		{
			list = currPhase.comboAttacks;
			consecutiveCombos++;
		}
		else
		{
			consecutiveCombos = 0;
		}
		if (list != null && list.Count > 0)
		{
			tempPotentialAttacks.Clear();
			for (int j = 0; j < list.Count; j++)
			{
				tempPotentialAttacks.Add(list[j]);
			}
			for (int num = tempPotentialAttacks.Count - 1; num >= 0; num--)
			{
				GRAbilityBase gRAbilityBase = abilities[(int)tempPotentialAttacks[num]];
				if (gRAbilityBase == null || !senseNearby.IsAnyoneNearby(gRAbilityBase.GetRange()) || !gRAbilityBase.IsCoolDownOver())
				{
					tempPotentialAttacks.RemoveAt(num);
				}
			}
			if (tempPotentialAttacks.Count > 0)
			{
				attacksAfterSummon--;
				int index = UnityEngine.Random.Range(0, tempPotentialAttacks.Count);
				return tempPotentialAttacks[index];
			}
		}
		return Behavior.None;
	}

	private bool AreAllEyesClosed()
	{
		for (int i = 0; i < eyes.Count; i++)
		{
			if (eyes[i].hp > 0)
			{
				return false;
			}
		}
		return true;
	}

	public void GotoDyingIdle()
	{
		SetBehavior(Behavior.DyingIdle, force: true);
	}

	private void ChooseNewBehavior(bool forceAttack = false)
	{
		if (hp <= 0)
		{
			TrySetBehavior(Behavior.Dying);
			return;
		}
		if (AreAllEyesClosed())
		{
			if (eyesPushVolume != null)
			{
				eyesPushVolume.Trigger();
			}
			TrySetBehavior(Behavior.Exposed);
			return;
		}
		if (forceAttack || !restAfterAttack)
		{
			restAfterAttack = false;
			Behavior behavior = TryChooseAttackBehavior();
			if (behavior != Behavior.None)
			{
				if (TrySetBehavior(behavior) && currBehavior != Behavior.AttackTongue)
				{
					PhaseDef currPhase = GetCurrPhase();
					restAfterAttack = currPhase.restAfterAttack;
				}
				if (currSummon != null)
				{
					PhaseDef currPhase2 = GetCurrPhase();
					if (trackedEntities.Count < currPhase2.maxSimultaneousEnemies && UnityEngine.Random.value < currPhase2.randomSummonChance)
					{
						currSummon.ForceSpawn();
					}
				}
				return;
			}
		}
		if (currBehavior == Behavior.None)
		{
			restAfterAttack = false;
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
		if (currBehavior == Behavior.Runaway)
		{
			currAbility.UpdateAuthority(dt);
			return;
		}
		if (currBehavior == Behavior.ExposedIdle)
		{
			PhaseDef currPhase = GetCurrPhase();
			if (hp <= 0)
			{
				SetBehavior(Behavior.Dying);
			}
			else if (hp <= currPhase.minHP)
			{
				SetBehavior(Behavior.AttackTongue);
			}
		}
		if (currAbility == null)
		{
			return;
		}
		currAbility.UpdateAuthority(dt);
		PhaseDef currPhase2 = GetCurrPhase();
		if (currAbility.IsDone())
		{
			if (currBehavior == Behavior.NextPhase)
			{
				SetBehavior(Behavior.AttackTongue);
			}
			else if (currBehavior == Behavior.Exposed)
			{
				SetBehavior(Behavior.ExposedIdle);
			}
			else if (currBehavior == Behavior.SummonStart)
			{
				Behavior newBehavior = ChooseSummonForPhase();
				SetBehavior(newBehavior);
			}
			else if (currBehavior == Behavior.SummonEnd && currPhase2.retreatAfterSummon)
			{
				SetBehavior(Behavior.RetreatStart);
			}
			else if (currBehavior == Behavior.RetreatStart)
			{
				waitInRetreat = 0f;
				SetBehavior(Behavior.RetreatIdle);
			}
			else if (currBehavior == Behavior.RetreatIdle)
			{
				SetBehavior(Behavior.RetreatEnd);
			}
			else if (currBehavior == Behavior.ExposedIdle)
			{
				SetBehavior(Behavior.AttackTongue);
			}
			else if (currBehavior == Behavior.AttackTongue)
			{
				SetBehavior(Behavior.HiddenIdle);
			}
			else if (IsSummon(currBehavior))
			{
				if (currPhase2 == null || trackedEntities.Count >= currPhase2.maxSimultaneousEnemies)
				{
					SetBehavior(Behavior.SummonEnd);
					return;
				}
				SetBehavior(Behavior.None);
				Behavior newBehavior2 = ChooseSummonForPhase();
				SetBehavior(newBehavior2);
			}
			else
			{
				SetBehavior(Behavior.None);
				ChooseNewBehavior();
			}
		}
		else if (AreAllEyesClosed() && currBehavior != Behavior.Exposed && currBehavior != Behavior.ExposedIdle && lastBehavior != Behavior.Exposed && lastBehavior != Behavior.ExposedIdle)
		{
			TrySetBehavior(Behavior.Exposed);
		}
	}

	private void OnUpdateRemote(float dt)
	{
		if (currAbility != null)
		{
			currAbility.UpdateRemote(dt);
		}
	}

	private void CatchUpPhase(int phase)
	{
		BossHasRevealed = true;
		internalPhaseIndex = phase;
		AdjustByPhaseIndex(phase);
		if (adaptiveMusicController != null)
		{
			adaptiveMusicController.RestartAt(phase);
		}
	}

	private void IncrementBossPhase()
	{
		internalPhaseIndex++;
		triggerNextMusicTransition = true;
		AdjustByPhaseIndex(internalPhaseIndex);
		Debug.Log($"Incrementing phase to phase {internalPhaseIndex}!");
	}

	private void SyncPhase(int phase)
	{
		internalPhaseIndex = phase;
		if (adaptiveMusicController != null)
		{
			adaptiveMusicController.GoToTrack(internalPhaseIndex);
		}
		AdjustByPhaseIndex(internalPhaseIndex);
		Debug.Log($"Syncing phase to phase {internalPhaseIndex}!");
	}

	private void AdjustByPhaseIndex(int phase)
	{
		switch (internalPhaseIndex)
		{
		case 1:
			abilityIdle.SpeedUp(3f);
			AdjustAttackAnimSpeed(1.2f);
			break;
		case 2:
			abilityIdle.SpeedUp(4f);
			AdjustAttackAnimSpeed(1.4f);
			break;
		case 3:
			abilityIdle.SpeedUp(4f);
			AdjustAttackAnimSpeed(1.6f);
			break;
		}
	}

	private void AdjustAttackAnimSpeed(float speed)
	{
		abilityAttackTentacle00.attackAnimData.speed = speed;
		abilityAttackTentacle01.attackAnimData.speed = speed;
		abilityAttackTentacle02.attackAnimData.speed = speed;
		abilityAttackTentacle03.attackAnimData.speed = speed;
		abilityAttackTentacle04.attackAnimData.speed = speed;
		abilityAttackTentacle05.attackAnimData.speed = speed;
	}

	public void OnHitByClub(GRTool tool, GameHitData hit)
	{
		HurtBoss(hit.hitAmount, hit.hitEntityId, tool.transform.position);
	}

	private void HurtBoss(int hitAmount, GameEntityId hitByEntityId, Vector3 toolPosition)
	{
		if (currBehavior == Behavior.Dying || currBehavior == Behavior.DyingIdle || currBehavior == Behavior.Runaway || IsAnySummonBehavior(currBehavior) || currBodyState != BodyState.Bones)
		{
			return;
		}
		int num = hp;
		PhaseDef currPhase = GetCurrPhase();
		SetHP(hp - hitAmount);
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
			if (hitByEntityId != GameEntityId.Invalid)
			{
				abilityDie.SetInstigatingPlayerIndex(entity.GetLastHeldByPlayerForEntityID(hitByEntityId));
			}
			SetBodyState(BodyState.Destroyed);
			SetBehavior(Behavior.Dying);
			return;
		}
		if (num > currPhase.minHP && hp <= currPhase.minHP)
		{
			if (currPhase.runawayAfterPhase)
			{
				Debug.Log("Force runaway!");
				if (hitByEntityId != GameEntityId.Invalid)
				{
					abilityRunaway.SetInstigatingPlayerIndex(entity.GetLastHeldByPlayerForEntityID(hitByEntityId));
				}
				SetBehavior(Behavior.Runaway);
			}
			else
			{
				Debug.Log("Force next phase transition!");
				SetBehavior(Behavior.NextPhase);
			}
		}
		lastSeenTargetPosition = toolPosition;
		lastSeenTargetTime = Time.timeAsDouble;
		Vector3 vector = lastSeenTargetPosition - base.transform.position;
		vector.y = 0f;
		searchPosition = lastSeenTargetPosition + vector.normalized * 1.5f;
	}

	public void OnHitByFlash(GRTool grTool, GameHitData hit)
	{
	}

	public void OnHitByShield(GRTool tool, GameHitData hit)
	{
		OnHitByClub(tool, hit);
	}

	public void ReportDeathStat()
	{
		if (currAbility != null && currAbility is GRAbilityDie gRAbilityDie)
		{
			gRAbilityDie.ReportDeathStat();
		}
	}

	private bool IsAttackBehavior(Behavior behavior)
	{
		if (behavior != Behavior.AttackTentacle00 && behavior != Behavior.AttackTentacle01 && behavior != Behavior.AttackTentacle02 && behavior != Behavior.AttackTentacle03 && behavior != Behavior.AttackTentacle04 && behavior != Behavior.AttackTentacle05 && behavior != Behavior.AttackQuickTentacle00 && behavior != Behavior.AttackQuickTentacle01 && behavior != Behavior.AttackQuickTentacle02 && behavior != Behavior.AttackQuickTentacle03 && behavior != Behavior.AttackTongue)
		{
			return behavior == Behavior.AttackTongueSwipe;
		}
		return true;
	}

	[CanBeNull]
	private GRAbilityBase GetAssociatedAbilityForBehavior(Behavior behavior)
	{
		return behavior switch
		{
			Behavior.AttackTentacle00 => abilityAttackTentacle00, 
			Behavior.AttackTentacle01 => abilityAttackTentacle01, 
			Behavior.AttackTentacle02 => abilityAttackTentacle02, 
			Behavior.AttackTentacle03 => abilityAttackTentacle03, 
			Behavior.AttackTentacle04 => abilityAttackTentacle04, 
			Behavior.AttackTentacle05 => abilityAttackTentacle05, 
			Behavior.AttackQuickTentacle00 => abilityAttackQuickTentacle00, 
			Behavior.AttackQuickTentacle01 => abilityAttackQuickTentacle01, 
			Behavior.AttackQuickTentacle02 => abilityAttackQuickTentacle02, 
			Behavior.AttackQuickTentacle03 => abilityAttackQuickTentacle03, 
			Behavior.AttackTongue => abilityAttackTongue01, 
			Behavior.AttackTongueSwipe => abilityAttackTongueSwipe01, 
			_ => null, 
		};
	}

	private void OnTriggerEnter(Collider collider)
	{
		if (currBodyState == BodyState.Destroyed || !IsAttackBehavior(currBehavior) || collider.isTrigger)
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
		if (component3 == null)
		{
			GorillaTagger component4 = attachedRigidbody.GetComponent<GorillaTagger>();
			if (component4 != null && component4.offlineVRRig != null)
			{
				component3 = component4.offlineVRRig.GetComponent<GRPlayer>();
			}
		}
		if (component3 != null && component3.gamePlayer.IsLocal() && Time.time > lastHitPlayerTime + minTimeBetweenHits)
		{
			HitPlayer(component3);
		}
		GRBreakable component5 = attachedRigidbody.GetComponent<GRBreakable>();
		GameHittable component6 = attachedRigidbody.GetComponent<GameHittable>();
		if (component5 != null && component6 != null)
		{
			GameHitData hitData = new GameHitData
			{
				hitTypeId = 0,
				hitEntityId = component6.gameEntity.id,
				hitByEntityId = entity.id,
				hitEntityPosition = component5.transform.position,
				hitImpulse = Vector3.zero,
				hitPosition = component5.transform.position,
				hittablePoint = component6.FindHittablePoint(collider)
			};
			component6.RequestHit(hitData);
		}
	}

	private void TurnOnGrav()
	{
		if (!(currentGravActivator != null))
		{
			currentGravActivator = gravActivators[UnityEngine.Random.Range(0, gravActivators.Length)];
			currentGravActivator.SetActive(value: true);
		}
	}

	private void TurnOffGrav()
	{
		if (!(currentGravActivator == null))
		{
			currentGravActivator.SetActive(value: false);
			currentGravActivator = null;
		}
	}

	[ContextMenu("Debug Hit Player")]
	private void DebugHitPlayer()
	{
		HitPlayer(VRRig.LocalRig.GetComponent<GRPlayer>(), useImpulse: true);
	}

	public void HitPlayer(GRPlayer player, bool useImpulse = false)
	{
		if (currBodyState == BodyState.Destroyed || tryHitPlayerCoroutine != null)
		{
			StopCoroutine(tryHitPlayerCoroutine);
		}
		tryHitPlayerCoroutine = StartCoroutine(TryHitPlayer(player, useImpulse));
	}

	private IEnumerator TryHitPlayer(GRPlayer player, bool useImpulse = false)
	{
		yield return new WaitForUpdate();
		if (!(player != null) || !player.gamePlayer.IsLocal() || !(Time.time > lastHitPlayerTime + minTimeBetweenHits))
		{
			yield break;
		}
		lastHitPlayerTime = Time.time;
		Vector3 vector2;
		if (GetAssociatedAbilityForBehavior(currBehavior) is ICustomKnockbackAbility customKnockbackAbility)
		{
			Vector3? vector = customKnockbackAbility.CalculateImpulse(player.transform);
			if (vector.HasValue)
			{
				Vector3 valueOrDefault = vector.GetValueOrDefault();
				vector2 = valueOrDefault;
				goto IL_00f4;
			}
		}
		vector2 = (player.transform.position - knockbackTransform.position).normalized * knockbackImpulse;
		goto IL_00f4;
		IL_00f4:
		GhostReactorManager.Get(entity).RequestEnemyHitPlayer(GhostReactor.EnemyType.Chaser, entity.id, player, base.transform.position, vector2);
		cameraShaker.Shake();
		float magnitude = vector2.magnitude;
		GorillaTagger.Instance.StartVibration(forLeftController: true, magnitude, 0.333f);
		GorillaTagger.Instance.StartVibration(forLeftController: false, magnitude, 0.333f);
		if (useImpulse)
		{
			GTPlayer.Instance.ApplyKnockback(vector2 / magnitude, magnitude, forceOffTheGround: true);
		}
	}

	public void ShockPlayer()
	{
		if (currBodyState != BodyState.Destroyed && tryShockPlayerCoroutine == null)
		{
			tryShockPlayerCoroutine = StartCoroutine(TryShockPlayer());
		}
	}

	private IEnumerator TryShockPlayer()
	{
		bodyRenderer.sharedMaterials = shockedBodyMaterials;
		yield return new WaitForSecondsRealtime(1f);
		bodyRenderer.sharedMaterials = defaultBodyMaterials;
		tryShockPlayerCoroutine = null;
	}

	private void ToggleShockColliders(bool toggle)
	{
		for (int i = 0; i < shockColliders.Count; i++)
		{
			shockColliders[i].enabled = toggle;
		}
	}

	public void GroundSlamWeak(Transform slamCenter)
	{
		_GroundSlam(slamCenter, 0.1f, 6f, 5f);
	}

	public void GroundSlam(Transform slamCenter)
	{
		_GroundSlam(slamCenter, 1f, 11f, 8f);
	}

	public async void _GroundSlam(Transform slamCenter, float duration, float distance, float hitVelocity)
	{
		Vector3 slamPosition = slamCenter.position;
		float timeHit = Time.time;
		bool playerHit = false;
		GTPlayer player = GTPlayer.Instance;
		float upwardsAngleBoost = 55f;
		if ((player.HeadCenterPosition - slamCenter.position).magnitude < distance * 1.25f)
		{
			cameraShaker.Shake();
			GorillaTagger.Instance.StartVibration(forLeftController: true, GorillaTagger.Instance.tapHapticStrength * 3f, 0.5f);
			GorillaTagger.Instance.StartVibration(forLeftController: false, GorillaTagger.Instance.tapHapticStrength * 3f, 0.5f);
		}
		while (!playerHit && Time.time < timeHit + duration)
		{
			if ((!player.IsGroundedHand && !player.IsGroundedButt) || !((player.HeadCenterPosition - slamPosition).magnitude < distance))
			{
				await Awaitable.WaitForSecondsAsync(0.1f);
			}
			else
			{
				playerHit = true;
			}
		}
		if (playerHit)
		{
			Vector3 vector = player.HeadCenterPosition - slamPosition;
			float num = Vector3.Angle(base.transform.forward, Vector3.up);
			vector = Vector3.RotateTowards(vector.normalized, Vector3.up, Mathf.Clamp(num - upwardsAngleBoost, 0f, upwardsAngleBoost) * (MathF.PI / 180f), 0f);
			GorillaTagger.Instance.StartVibration(forLeftController: true, GorillaTagger.Instance.tapHapticStrength * 5f, 0.75f);
			GorillaTagger.Instance.StartVibration(forLeftController: false, GorillaTagger.Instance.tapHapticStrength * 5f, 0.75f);
			player.ApplyKnockback(vector, hitVelocity, forceOffTheGround: true);
			GhostReactorManager.Get(entity).RequestEnemyHitPlayer(GhostReactor.EnemyType.Chaser, entity.id, GRPlayer.GetLocal(), base.transform.position, vector.normalized * hitVelocity);
		}
	}

	public void GetDebugTextLines(out List<string> strings)
	{
		strings = new List<string>();
		strings.Add("<color=\"white\">State:</color> <color=\"yellow\">" + currBehavior.ToString() + "</color>\n" + $"<color=\"white\">Phase:</color> <color=\"yellow\">{GetCurrPhaseIndex()}</color>\n" + $"<color=\"white\">HP:</color> <color=\"yellow\">{hp}</color>");
	}

	public void OnGameEntitySerialize(BinaryWriter writer)
	{
		byte value = (byte)currBehavior;
		byte value2 = (byte)currBodyState;
		int value3 = ((targetPlayer == null) ? (-1) : targetPlayer.ActorNumber);
		writer.Write(value);
		writer.Write(value2);
		writer.Write(hp);
		writer.Write(value3);
		writer.Write(internalPhaseIndex);
	}

	public void OnGameEntityDeserialize(BinaryReader reader)
	{
		Behavior newBehavior = (Behavior)reader.ReadByte();
		BodyState newBodyState = (BodyState)reader.ReadByte();
		int hP = reader.ReadInt32();
		int playerID = reader.ReadInt32();
		int num = reader.ReadInt32();
		SetHP(hP);
		SetBehavior(newBehavior, force: true);
		SetBodyState(newBodyState, force: true);
		targetPlayer = NetworkSystem.Instance.GetPlayer(playerID);
		if (num != -1)
		{
			if (internalPhaseIndex == -1)
			{
				Debug.Log($"Catching up to boss phase {num}.");
				CatchUpPhase(num);
			}
			else if (num != internalPhaseIndex)
			{
				Debug.Log($"Syncing up to boss phase {internalPhaseIndex}.");
				SyncPhase(num);
			}
		}
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

	private void AddTrackedEntity(GameEntity entityToTrack)
	{
		int netId = entityToTrack.GetNetId();
		trackedEntities.AddIfNew(netId);
		if (!trackedGameEntities.Contains(entityToTrack))
		{
			trackedGameEntities.Add(entityToTrack);
		}
	}

	private void RemoveTrackedEntity(GameEntity entityToRemove)
	{
		int netId = entityToRemove.GetNetId();
		if (trackedEntities.Contains(netId))
		{
			trackedEntities.Remove(netId);
		}
		if (trackedGameEntities.Contains(entityToRemove))
		{
			trackedGameEntities.Remove(entityToRemove);
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

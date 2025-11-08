using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR;

public class GRToolCollector : MonoBehaviour, IGameEntityDebugComponent, IGameEntityComponent
{
	private enum State
	{
		Idle,
		Vacuuming,
		Collect,
		Cooldown
	}

	public GameEntity gameEntity;

	public GRTool tool;

	public GRAttributes attributes;

	public int energyDepositPerUse = 100;

	public Transform shootFrom;

	public LayerMask collectibleLayerMask;

	public ParticleSystem vacuumParticleEffect;

	public ParticleSystem upgrade1VacuumParticleEffect;

	public ParticleSystem upgrade2VacuumParticleEffect;

	public ParticleSystem upgrade3VacuumParticleEffect;

	public ParticleSystem passiveChargeParticleEffect;

	public AudioSource vacuumAudioSource;

	public AudioClip vacuumSound;

	public AudioClip upgrade1vacuumSound;

	public AudioClip upgrade2vacuumSound;

	public AudioClip upgrade3vacuumSound;

	public float vacuumSoundVolume = 0.2f;

	public AudioSource collectAudioSource;

	[FormerlySerializedAs("flashSound")]
	public AudioClip collectSound;

	[FormerlySerializedAs("flashSoundVolume")]
	public float collectSoundVolume = 1f;

	public AudioClip chargeBeamSound;

	public float chargeBeamVolume = 0.2f;

	public LightningDispatcher lightningDispatcher;

	public float chargeDuration = 0.75f;

	[FormerlySerializedAs("flashDuration")]
	public float collectDuration = 0.1f;

	public float cooldownDuration;

	public AbilityHaptic collectHaptic;

	[NonSerialized]
	public GhostReactorManager grManager;

	private float rechargeRate;

	public float rechargeInterval = 1f;

	private double lastRechargeTime;

	public float level3ChargeRadius = 4f;

	private State state;

	private float stateTimeRemaining;

	private bool activatedLocally;

	private bool waitingForButtonRelease;

	private RaycastHit[] tempHitResults = new RaycastHit[128];

	private void Awake()
	{
		state = State.Idle;
		stateTimeRemaining = -1f;
	}

	private void OnEnable()
	{
		SetState(State.Idle);
	}

	public void OnEntityInit()
	{
		if (tool != null)
		{
			tool.onToolUpgraded += OnToolUpgraded;
			OnToolUpgraded(tool);
		}
		lastRechargeTime = Time.time;
	}

	public void OnEntityDestroy()
	{
	}

	public void OnEntityStateChange(long prevState, long nextState)
	{
	}

	private void OnToolUpgraded(GRTool tool)
	{
		rechargeRate = attributes.CalculateFinalFloatValueForAttribute(GRAttributeType.RechargeRate);
		if (tool.HasUpgradeInstalled(GRToolProgressionManager.ToolParts.CollectorBonus1))
		{
			vacuumSound = upgrade1vacuumSound;
			vacuumParticleEffect = upgrade1VacuumParticleEffect;
		}
		else if (tool.HasUpgradeInstalled(GRToolProgressionManager.ToolParts.CollectorBonus2))
		{
			vacuumSound = upgrade2vacuumSound;
			vacuumParticleEffect = upgrade2VacuumParticleEffect;
		}
		else if (tool.HasUpgradeInstalled(GRToolProgressionManager.ToolParts.CollectorBonus3))
		{
			vacuumSound = upgrade3vacuumSound;
			vacuumParticleEffect = upgrade3VacuumParticleEffect;
		}
	}

	private bool IsHeldLocal()
	{
		return gameEntity.heldByActorNumber == PhotonNetwork.LocalPlayer.ActorNumber;
	}

	public void OnUpdate(float dt)
	{
		if (IsHeldLocal() || activatedLocally)
		{
			OnUpdateAuthority(dt);
		}
		else
		{
			OnUpdateRemote(dt);
		}
	}

	public void Update()
	{
		float deltaTime = Time.deltaTime;
		if (IsHeldLocal() || activatedLocally)
		{
			OnUpdateAuthority(deltaTime);
		}
		else
		{
			OnUpdateRemote(deltaTime);
		}
	}

	private void OnUpdateAuthority(float dt)
	{
		switch (state)
		{
		case State.Idle:
		{
			bool flag2 = IsButtonHeld();
			waitingForButtonRelease &= flag2;
			if (flag2 && !waitingForButtonRelease)
			{
				SetStateAuthority(State.Vacuuming);
				activatedLocally = true;
			}
			if (rechargeRate > 0f && Time.timeAsDouble > lastRechargeTime + (double)rechargeInterval)
			{
				gameEntity.manager.ghostReactorManager.RequestChargeTool(gameEntity.id, gameEntity.id, (int)(rechargeRate * rechargeInterval), useCollectorEnergy: false);
				lastRechargeTime = Time.timeAsDouble;
				if (passiveChargeParticleEffect != null)
				{
					passiveChargeParticleEffect.Play();
				}
			}
			break;
		}
		case State.Vacuuming:
		{
			bool flag = IsButtonHeld();
			stateTimeRemaining -= dt;
			if (stateTimeRemaining <= 0f)
			{
				SetStateAuthority(State.Collect);
			}
			else if (!flag)
			{
				SetStateAuthority(State.Idle);
				activatedLocally = false;
			}
			break;
		}
		case State.Collect:
			stateTimeRemaining -= dt;
			if (stateTimeRemaining <= 0f)
			{
				SetStateAuthority(State.Cooldown);
			}
			break;
		case State.Cooldown:
			stateTimeRemaining -= dt;
			if (stateTimeRemaining <= 0f)
			{
				activatedLocally = false;
				waitingForButtonRelease = true;
				SetStateAuthority(State.Idle);
			}
			break;
		}
	}

	private void OnUpdateRemote(float dt)
	{
		State state = (State)gameEntity.GetState();
		if (state != this.state)
		{
			SetState(state);
		}
	}

	private void SetStateAuthority(State newState)
	{
		SetState(newState);
		gameEntity.RequestState(gameEntity.id, (long)newState);
	}

	private void SetState(State newState)
	{
		state = newState;
		switch (state)
		{
		case State.Vacuuming:
			StartVacuum();
			stateTimeRemaining = chargeDuration;
			break;
		case State.Collect:
			TryCollect();
			stateTimeRemaining = collectDuration;
			break;
		case State.Cooldown:
			stateTimeRemaining = cooldownDuration;
			break;
		case State.Idle:
			StopVacuum();
			stateTimeRemaining = -1f;
			lastRechargeTime = Time.time;
			break;
		}
	}

	private void StartVacuum()
	{
		vacuumAudioSource.clip = vacuumSound;
		vacuumAudioSource.volume = vacuumSoundVolume;
		vacuumAudioSource.loop = true;
		vacuumAudioSource.Play();
		vacuumParticleEffect.Play();
		if (IsHeldLocal())
		{
			PlayVibration(GorillaTagger.Instance.tapHapticStrength, chargeDuration);
		}
	}

	private void StopVacuum()
	{
		vacuumAudioSource.loop = false;
		vacuumAudioSource.Stop();
		vacuumParticleEffect.Stop();
	}

	private void TryCollect()
	{
		if (!IsHeldLocal())
		{
			return;
		}
		int num = Physics.SphereCastNonAlloc(shootFrom.position, 0.2f, shootFrom.rotation * Vector3.forward, tempHitResults, 1f, collectibleLayerMask);
		for (int i = 0; i < num; i++)
		{
			RaycastHit raycastHit = tempHitResults[i];
			GameObject gameObject = null;
			Rigidbody attachedRigidbody = raycastHit.collider.attachedRigidbody;
			if (attachedRigidbody != null)
			{
				gameObject = attachedRigidbody.gameObject;
			}
			else
			{
				GameEntity gameEntity = GameEntity.Get(raycastHit.collider);
				if (gameEntity != null)
				{
					gameObject = gameEntity.gameObject;
				}
			}
			if (gameObject != null)
			{
				GRCollectible component = gameObject.GetComponent<GRCollectible>();
				if (component != null && component.type != ProgressionManager.CoreType.ChaosSeed && tool.energy < tool.GetEnergyMax())
				{
					GhostReactorManager.Get(this.gameEntity).RequestCollectItem(component.entity.id, this.gameEntity.id);
					return;
				}
			}
		}
		for (int j = 0; j < num; j++)
		{
			RaycastHit raycastHit2 = tempHitResults[j];
			GameObject gameObject2 = null;
			Rigidbody attachedRigidbody2 = raycastHit2.collider.attachedRigidbody;
			if (attachedRigidbody2 != null)
			{
				gameObject2 = attachedRigidbody2.gameObject;
			}
			else
			{
				GameEntity gameEntity2 = GameEntity.Get(raycastHit2.collider);
				if (gameEntity2 != null)
				{
					gameObject2 = gameEntity2.gameObject;
				}
			}
			if (!(gameObject2 != null))
			{
				continue;
			}
			if (gameObject2.GetComponent<GRCurrencyDepositor>() != null)
			{
				if (tool.energy > 0)
				{
					GhostReactorManager.Get(this.gameEntity).RequestDepositCurrency(this.gameEntity.id);
				}
				break;
			}
			GRTool component2 = gameObject2.GetComponent<GRTool>();
			if (component2 == null || component2 == tool)
			{
				continue;
			}
			GameEntity component3 = gameObject2.GetComponent<GameEntity>();
			if (!(component2 != null) || !(component3 != null))
			{
				continue;
			}
			GhostReactorManager.Get(this.gameEntity).RequestChargeTool(this.gameEntity.id, component3.id);
			if (!tool.HasUpgradeInstalled(GRToolProgressionManager.ToolParts.CollectorBonus3) || tool.energy <= 50)
			{
				break;
			}
			List<GRTool> list = new List<GRTool>();
			this.gameEntity.manager.GetEntitiesWithComponentInRadius(base.transform.position, level3ChargeRadius, checkRootOnly: true, list);
			for (int k = 0; k < list.Count; k++)
			{
				GRTool gRTool = list[k];
				if (!(gRTool.GetComponent<GRToolCollector>() != null) && !(gRTool.gameEntity == this.gameEntity) && !(gRTool.gameEntity == component3))
				{
					GhostReactorManager.Get(this.gameEntity).RequestChargeTool(this.gameEntity.id, gRTool.gameEntity.id, 0, useCollectorEnergy: false);
				}
			}
			break;
		}
	}

	public void PerformCollection(GRCollectible collectible)
	{
		tool.RefillEnergy(collectible.energyValue + attributes.CalculateFinalValueForAttribute(GRAttributeType.HarvestGain), collectible.entity.id);
		collectAudioSource.volume = collectSoundVolume;
		collectAudioSource.PlayOneShot(collectSound);
	}

	public void PlayChargeEffect(GRTool targetTool)
	{
		if (targetTool == null || targetTool == tool)
		{
			return;
		}
		collectAudioSource.volume = chargeBeamVolume;
		collectAudioSource.PlayOneShot(chargeBeamSound);
		for (int i = 0; i < targetTool.energyMeters.Count; i++)
		{
			if (targetTool.energyMeters[i].chargePoint != null)
			{
				lightningDispatcher.DispatchLightning(lightningDispatcher.transform.position, targetTool.energyMeters[i].chargePoint.position);
			}
			else
			{
				lightningDispatcher.DispatchLightning(lightningDispatcher.transform.position, targetTool.energyMeters[i].transform.position);
			}
		}
	}

	public void PlayChargeEffect(GRCurrencyDepositor targetDepositor)
	{
		if (!(targetDepositor == null))
		{
			collectAudioSource.volume = chargeBeamVolume;
			collectAudioSource.PlayOneShot(chargeBeamSound);
			lightningDispatcher.DispatchLightning(lightningDispatcher.transform.position, targetDepositor.depositingChargePoint.position);
		}
	}

	private bool IsButtonHeld()
	{
		if (!IsHeldLocal())
		{
			return false;
		}
		if (!GamePlayer.TryGetGamePlayer(gameEntity.heldByActorNumber, out var out_gamePlayer))
		{
			return false;
		}
		int num = out_gamePlayer.FindHandIndex(gameEntity.id);
		if (num == -1)
		{
			return false;
		}
		return ControllerInputPoller.TriggerFloat(GamePlayer.IsLeftHand(num) ? XRNode.LeftHand : XRNode.RightHand) > 0.25f;
	}

	private void PlayVibration(float strength, float duration)
	{
		if (IsHeldLocal() && GamePlayer.TryGetGamePlayer(gameEntity.heldByActorNumber, out var out_gamePlayer))
		{
			int num = out_gamePlayer.FindHandIndex(gameEntity.id);
			if (num != -1)
			{
				GorillaTagger.Instance.StartVibration(GamePlayer.IsLeftHand(num), strength, duration);
			}
		}
	}

	public void GetDebugTextLines(out List<string> strings)
	{
		strings = new List<string>();
		strings.Add($"Recharge Rate: <color=\"yellow\">{rechargeRate}<color=\"white\">");
	}
}

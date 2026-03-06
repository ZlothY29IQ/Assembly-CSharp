using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.XR;

public class GRToolFlash : MonoBehaviour, IGameEntityDebugComponent, IGameEntityComponent
{
	[Flags]
	public enum UpgradeTypes
	{
		None = 1,
		UpagredA = 2,
		UpagredB = 4,
		UpagredC = 8
	}

	private enum State
	{
		Idle,
		Charging,
		Flash,
		Cooldown,
		Count
	}

	public GameEntity gameEntity;

	public GRTool tool;

	public GRAttributes attributes;

	public GameObject flash;

	public Transform shootFrom;

	public LayerMask enemyLayerMask;

	public AudioSource audioSource;

	public AudioClip chargeSound;

	public float chargeSoundVolume = 0.2f;

	public AudioClip flashSound;

	public AudioClip upgrade1FlashSound;

	public AudioClip upgrade2FlashSound;

	public AudioClip upgrade3FlashSound;

	public GameObject upgrade1FlashCone;

	public GameObject upgrade2FlashCone;

	public GameObject upgrade3FlashCone;

	public float flashSoundVolume = 1f;

	public float stunDuration;

	public UpgradeTypes upgradesApplied;

	public float chargeDuration = 0.75f;

	public float flashDuration = 0.1f;

	public float cooldownDuration;

	private float timeLastFlashed;

	private float cooldownMinimum = 0.35f;

	private bool activatedLocally;

	public GameEntity item;

	private GameHitter gameHitter;

	private State state;

	private float stateTimeRemaining;

	private RaycastHit[] tempHitResults = new RaycastHit[128];

	private void Awake()
	{
		state = State.Idle;
		stateTimeRemaining = -1f;
		gameHitter = GetComponent<GameHitter>();
	}

	private void OnEnable()
	{
		StopFlash();
		SetState(State.Idle);
	}

	public void OnEntityInit()
	{
		if (tool != null)
		{
			tool.onToolUpgraded += OnToolUpgraded;
			OnToolUpgraded(tool);
		}
	}

	public void OnEntityDestroy()
	{
	}

	public void OnEntityStateChange(long prevState, long nextState)
	{
	}

	private void OnToolUpgraded(GRTool tool)
	{
		stunDuration = attributes.CalculateFinalFloatValueForAttribute(GRAttributeType.FlashStunDuration);
		if (tool.HasUpgradeInstalled(GRToolProgressionManager.ToolParts.FlashDamage1))
		{
			flashSound = upgrade1FlashSound;
			flash = upgrade1FlashCone;
		}
		else if (tool.HasUpgradeInstalled(GRToolProgressionManager.ToolParts.FlashDamage2))
		{
			flashSound = upgrade2FlashSound;
			flash = upgrade2FlashCone;
		}
		else if (tool.HasUpgradeInstalled(GRToolProgressionManager.ToolParts.FlashDamage3))
		{
			flashSound = upgrade3FlashSound;
			flash = upgrade3FlashCone;
		}
	}

	private bool IsHeldLocal()
	{
		return item.heldByActorNumber == PhotonNetwork.LocalPlayer.ActorNumber;
	}

	public void OnUpdate(float dt)
	{
		if (IsHeldLocal())
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
			if (tool.HasEnoughEnergy() && IsButtonHeld())
			{
				SetStateAuthority(State.Charging);
				activatedLocally = true;
			}
			break;
		case State.Charging:
		{
			bool flag = IsButtonHeld();
			stateTimeRemaining -= dt;
			if (stateTimeRemaining <= 0f)
			{
				SetStateAuthority(State.Flash);
			}
			else if (!flag)
			{
				SetStateAuthority(State.Idle);
				activatedLocally = false;
			}
			break;
		}
		case State.Flash:
			stateTimeRemaining -= dt;
			if (stateTimeRemaining <= 0f)
			{
				SetStateAuthority(State.Cooldown);
			}
			break;
		case State.Cooldown:
			stateTimeRemaining -= dt;
			if (stateTimeRemaining <= 0f && !IsButtonHeld())
			{
				SetStateAuthority(State.Idle);
				activatedLocally = false;
			}
			break;
		}
	}

	private void OnUpdateRemote(float dt)
	{
		State state = (State)gameEntity.GetState();
		if (state == this.state)
		{
			return;
		}
		if (this.state == State.Charging && state == State.Cooldown)
		{
			SetState(State.Flash);
		}
		else if (this.state == State.Flash && state == State.Cooldown)
		{
			if (Time.time > timeLastFlashed + flashDuration)
			{
				SetState(State.Cooldown);
			}
		}
		else
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
		if (CanChangeState((long)newState))
		{
			state = newState;
			switch (state)
			{
			case State.Charging:
				StartCharge();
				stateTimeRemaining = chargeDuration;
				break;
			case State.Flash:
				StartFlash();
				stateTimeRemaining = flashDuration;
				break;
			case State.Cooldown:
				StopFlash();
				stateTimeRemaining = cooldownDuration;
				break;
			case State.Idle:
				stateTimeRemaining = -1f;
				break;
			}
		}
	}

	private void StartCharge()
	{
		audioSource.volume = chargeSoundVolume;
		audioSource.clip = chargeSound;
		audioSource.Play();
		if (IsHeldLocal())
		{
			PlayVibration(GorillaTagger.Instance.tapHapticStrength, chargeDuration);
		}
	}

	private void StartFlash()
	{
		flash.SetActive(value: true);
		audioSource.volume = flashSoundVolume;
		audioSource.clip = flashSound;
		audioSource.Play();
		tool.UseEnergy();
		timeLastFlashed = Time.time;
		if (!IsHeldLocal())
		{
			return;
		}
		int num = Physics.SphereCastNonAlloc(shootFrom.position, 1f, shootFrom.rotation * Vector3.forward, tempHitResults, 5f, enemyLayerMask);
		for (int i = 0; i < num; i++)
		{
			RaycastHit raycastHit = tempHitResults[i];
			Rigidbody attachedRigidbody = raycastHit.collider.attachedRigidbody;
			if (attachedRigidbody != null)
			{
				GameHittable component = attachedRigidbody.GetComponent<GameHittable>();
				if (component != null && gameHitter != null)
				{
					GameHitData hitData = new GameHitData
					{
						hitTypeId = 1,
						hitEntityId = component.gameEntity.id,
						hitByEntityId = gameEntity.id,
						hitEntityPosition = component.gameEntity.transform.position,
						hitPosition = ((raycastHit.distance == 0f) ? shootFrom.position : raycastHit.point),
						hitImpulse = Vector3.zero,
						hitAmount = gameHitter.CalcHitAmount(GameHitType.Flash, component, gameEntity),
						hittablePoint = component.FindHittablePoint(raycastHit.collider)
					};
					component.RequestHit(hitData);
				}
			}
		}
	}

	private void StopFlash()
	{
		flash.SetActive(value: false);
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
		int num = out_gamePlayer.FindHandIndex(item.id);
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
			int num = out_gamePlayer.FindHandIndex(item.id);
			if (num != -1)
			{
				GorillaTagger.Instance.StartVibration(GamePlayer.IsLeftHand(num), strength, duration);
			}
		}
	}

	public bool CanChangeState(long newStateIndex)
	{
		if (newStateIndex < 0 || newStateIndex >= 4)
		{
			return false;
		}
		if ((int)newStateIndex == 2)
		{
			return Time.time > timeLastFlashed + cooldownMinimum;
		}
		return true;
	}

	public void GetDebugTextLines(out List<string> strings)
	{
		strings = new List<string>();
		strings.Add($"Stun Duration: <color=\"yellow\">{stunDuration}<color=\"white\">");
	}
}

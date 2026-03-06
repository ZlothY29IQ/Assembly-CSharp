using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.XR;

public class GRToolDirectionalShield : MonoBehaviour, IGameHitter
{
	private enum State
	{
		Closed,
		Open
	}

	[Header("References")]
	public GameEntity gameEntity;

	public GRTool tool;

	public Rigidbody rigidBody;

	public AudioSource audioSource;

	public List<Animator> shieldAnimators;

	public Transform openCollidersParent;

	private GameHitter hitter;

	private GRAttributes attributes;

	[Header("Audio")]
	public AudioClip openAudio;

	public float openVolume = 0.5f;

	public AudioClip closeAudio;

	public float closeVolume = 0.5f;

	public AudioClip deflectAudio;

	public AudioClip upgrade1DeflectAudio;

	public AudioClip upgrade2DeflectAudio;

	public AudioClip upgrade3DeflectAudio;

	public float deflectVolume = 0.5f;

	[Header("VFX")]
	public ParticleSystem shieldDeflectVFX;

	public ParticleSystem upgrade1ShieldDeflectVFX;

	public ParticleSystem upgrade2ShieldDeflectVFX;

	public ParticleSystem upgrade3ShieldDeflectVFX;

	public ParticleSystem shieldDeflectImpactPointVFX;

	public Transform shieldArcCenterReferencePoint;

	public float shieldArcCenterRadius = 1f;

	[Header("Haptic")]
	public AbilityHaptic openHaptic;

	public AbilityHaptic closeHaptic;

	public bool reflectsProjectiles;

	private State state;

	private void Awake()
	{
		hitter = GetComponent<GameHitter>();
		attributes = GetComponent<GRAttributes>();
		if (tool != null)
		{
			tool.onToolUpgraded += OnToolUpgraded;
			OnToolUpgraded(tool);
		}
	}

	private void OnToolUpgraded(GRTool tool)
	{
		if (tool.HasUpgradeInstalled(GRToolProgressionManager.ToolParts.DirectionalShieldSize1))
		{
			deflectAudio = upgrade1DeflectAudio;
			shieldDeflectVFX = upgrade1ShieldDeflectVFX;
			reflectsProjectiles = true;
		}
		else if (tool.HasUpgradeInstalled(GRToolProgressionManager.ToolParts.DirectionalShieldSize2))
		{
			deflectAudio = upgrade2DeflectAudio;
			shieldDeflectVFX = upgrade2ShieldDeflectVFX;
			reflectsProjectiles = false;
		}
		else if (tool.HasUpgradeInstalled(GRToolProgressionManager.ToolParts.DirectionalShieldSize3))
		{
			deflectAudio = upgrade3DeflectAudio;
			shieldDeflectVFX = upgrade3ShieldDeflectVFX;
			reflectsProjectiles = true;
		}
		else
		{
			reflectsProjectiles = false;
		}
	}

	public void OnEnable()
	{
		SetState(State.Closed);
	}

	private bool IsHeldLocal()
	{
		return gameEntity.heldByActorNumber == PhotonNetwork.LocalPlayer.ActorNumber;
	}

	private bool IsHeld()
	{
		return gameEntity.heldByActorNumber != -1;
	}

	public void BlockHittable(Vector3 enemyPosition, Vector3 enemyAttackDirection, GameHittable hittable, GRShieldCollider shieldCollider)
	{
		if (!IsHeldLocal())
		{
			return;
		}
		float num = 1f;
		if (attributes != null && attributes.HasValueForAttribute(GRAttributeType.KnockbackMultiplier))
		{
			num = attributes.CalculateFinalFloatValueForAttribute(GRAttributeType.KnockbackMultiplier);
		}
		Vector3 hitImpulse = -enemyAttackDirection * shieldCollider.KnockbackVelocity * num;
		if (reflectsProjectiles)
		{
			GRRangedEnemyProjectile component = hittable.GetComponent<GRRangedEnemyProjectile>();
			if (component != null && component.owningEntity != null && GREnemyRanged.CalculateLaunchDirection(enemyPosition, component.owningEntity.transform.position + new Vector3(0f, 0.5f, 0f), component.projectileSpeed, out var direction))
			{
				hitImpulse = direction * component.projectileSpeed;
			}
		}
		GameHitData hitData = new GameHitData
		{
			hitTypeId = 2,
			hitEntityId = hittable.gameEntity.id,
			hitByEntityId = gameEntity.id,
			hitEntityPosition = enemyPosition,
			hitImpulse = hitImpulse,
			hitPosition = enemyPosition,
			hitAmount = hitter.CalcHitAmount(GameHitType.Shield, hittable, gameEntity)
		};
		if (hittable.IsHitValid(hitData))
		{
			hittable.RequestHit(hitData);
		}
	}

	public void OnEnemyBlocked(Vector3 enemyPosition)
	{
		tool.UseEnergy();
		PlayBlockEffects(enemyPosition);
	}

	private void PlayBlockEffects(Vector3 enemyPosition)
	{
		audioSource.PlayOneShot(deflectAudio, deflectVolume);
		shieldDeflectVFX.Play();
		Vector3 vector = Vector3.ClampMagnitude(enemyPosition - shieldArcCenterReferencePoint.position, shieldArcCenterRadius);
		Vector3 position = shieldArcCenterReferencePoint.position + vector;
		shieldDeflectImpactPointVFX.transform.position = position;
		shieldDeflectImpactPointVFX.Play();
	}

	public void OnSuccessfulHit(GameHitData hitData)
	{
		tool.UseEnergy();
		PlayBlockEffects(hitData.hitEntityPosition);
	}

	public void Update()
	{
		float deltaTime = Time.deltaTime;
		if (IsHeld())
		{
			if (IsHeldLocal())
			{
				OnUpdateAuthority(deltaTime);
			}
			else
			{
				OnUpdateRemote(deltaTime);
			}
		}
		else
		{
			SetState(State.Closed);
		}
	}

	private void OnUpdateAuthority(float dt)
	{
		switch (state)
		{
		case State.Closed:
			if (IsButtonHeld() && tool.HasEnoughEnergy())
			{
				SetStateAuthority(State.Open);
			}
			break;
		case State.Open:
			if (!IsButtonHeld() || !tool.HasEnoughEnergy())
			{
				SetStateAuthority(State.Closed);
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
		if (state == newState)
		{
			return;
		}
		if (state != State.Closed)
		{
			_ = 1;
		}
		state = newState;
		switch (state)
		{
		case State.Closed:
		{
			openCollidersParent.gameObject.SetActive(value: false);
			for (int j = 0; j < shieldAnimators.Count; j++)
			{
				shieldAnimators[j].SetBool("Activated", value: false);
			}
			audioSource.PlayOneShot(closeAudio, closeVolume);
			closeHaptic.PlayIfHeldLocal(gameEntity);
			_ = hitter != null;
			break;
		}
		case State.Open:
		{
			openCollidersParent.gameObject.SetActive(value: true);
			for (int i = 0; i < shieldAnimators.Count; i++)
			{
				shieldAnimators[i].SetBool("Activated", value: true);
			}
			audioSource.PlayOneShot(openAudio, openVolume);
			openHaptic.PlayIfHeldLocal(gameEntity);
			_ = hitter != null;
			break;
		}
		}
	}

	private bool IsButtonHeld()
	{
		if (!IsHeldLocal())
		{
			return false;
		}
		GamePlayer gamePlayer = GamePlayer.GetGamePlayer(gameEntity.heldByActorNumber);
		if (gamePlayer == null)
		{
			return false;
		}
		int num = gamePlayer.FindHandIndex(gameEntity.id);
		if (num == -1)
		{
			return false;
		}
		return ControllerInputPoller.TriggerFloat(GamePlayer.IsLeftHand(num) ? XRNode.LeftHand : XRNode.RightHand) > 0.25f;
	}
}

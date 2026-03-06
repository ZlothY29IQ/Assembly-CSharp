using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class GRToolClub : MonoBehaviourTick, IGameHitter, IGameEntityDebugComponent, IGameEntityComponent
{
	private enum State
	{
		Idle,
		Extended
	}

	public GameEntity gameEntity;

	public GameHitter gameHitter;

	public GRTool tool;

	public Rigidbody rigidBody;

	public AudioSource audioSource;

	public AudioSource humAudioSource;

	public List<ParticleSystem> humParticleEffects = new List<ParticleSystem>();

	public GRAttributes attributes;

	public AudioClip extendAudio;

	public float extendVolume = 0.5f;

	public AudioClip retractAudio;

	public float retractVolume = 0.5f;

	public GameHitFx noPowerFx;

	public GameHitFx poweredImpactFx;

	public GameHitFx upgrade1ImpactVFX;

	public GameHitFx upgrade2ImpactVFX;

	public GameHitFx upgrade3ImpactVFX;

	public GRAttributeType noPowerAttribute;

	public GRAttributeType poweredAttribute;

	public float minHitSpeed = 2.25f;

	public GameObject dullLight;

	public List<MeshAndMaterials> meshAndMaterials;

	public Transform retractableSection;

	public Collider idleCollider;

	public Collider extendedCollider;

	public float retractableSectionMin = -0.31f;

	public float retractableSectionMax;

	public float extensionTime = 0.15f;

	[Header("Haptic")]
	public AbilityHaptic openHaptic;

	public AbilityHaptic closeHaptic;

	private float extendedAmount;

	private State state;

	private void Awake()
	{
		retractableSection.localPosition = new Vector3(0f, 0f, 0f);
	}

	public new void OnEnable()
	{
		base.OnEnable();
		SetExtendedAmount(0f);
		gameHitter.hitFx = noPowerFx;
		gameHitter.damageAttribute = noPowerAttribute;
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
	}

	private void EnableImpactVFXForCurrentUpgradeLevel()
	{
		if (tool.HasUpgradeInstalled(GRToolProgressionManager.ToolParts.BatonDamage1))
		{
			gameHitter.hitFx = upgrade1ImpactVFX;
		}
		else if (tool.HasUpgradeInstalled(GRToolProgressionManager.ToolParts.BatonDamage2))
		{
			gameHitter.hitFx = upgrade2ImpactVFX;
		}
		else if (tool.HasUpgradeInstalled(GRToolProgressionManager.ToolParts.BatonDamage3))
		{
			gameHitter.hitFx = upgrade3ImpactVFX;
		}
		else
		{
			gameHitter.hitFx = poweredImpactFx;
		}
	}

	public override void Tick()
	{
		float deltaTime = Time.deltaTime;
		if (gameEntity.IsHeld())
		{
			if (gameEntity.IsHeldByLocalPlayer())
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
			SetState(State.Idle);
		}
		OnUpdateShared(deltaTime);
	}

	private void OnUpdateAuthority(float dt)
	{
		switch (state)
		{
		case State.Idle:
			if (IsButtonHeld() && tool.HasEnoughEnergy())
			{
				SetState(State.Extended);
			}
			break;
		case State.Extended:
			if (!IsButtonHeld() || !tool.HasEnoughEnergy())
			{
				SetState(State.Idle);
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

	private void OnUpdateShared(float dt)
	{
		switch (state)
		{
		case State.Idle:
			if (extendedAmount > 0f)
			{
				float num2 = Mathf.MoveTowards(extendedAmount, 0f, 1f / extensionTime * Time.deltaTime);
				SetExtendedAmount(num2);
			}
			break;
		case State.Extended:
			if (extendedAmount < 1f)
			{
				float num = Mathf.MoveTowards(extendedAmount, 1f, 1f / extensionTime * Time.deltaTime);
				SetExtendedAmount(num);
			}
			break;
		}
	}

	private void SetExtendedAmount(float newExtendedAmount)
	{
		extendedAmount = newExtendedAmount;
		float y = Mathf.Lerp(retractableSectionMin, retractableSectionMax, extendedAmount);
		retractableSection.localPosition = new Vector3(0f, y, 0f);
	}

	private void SetState(State newState)
	{
		if (state == newState)
		{
			return;
		}
		if (state != State.Idle)
		{
			_ = 1;
		}
		state = newState;
		switch (state)
		{
		case State.Idle:
		{
			extendedCollider.enabled = false;
			idleCollider.enabled = true;
			for (int k = 0; k < meshAndMaterials.Count; k++)
			{
				MaterialUtils.SwapMaterial(meshAndMaterials[k], isOnToOff: true);
			}
			humAudioSource.Stop();
			dullLight.SetActive(value: false);
			audioSource.PlayOneShot(retractAudio, retractVolume);
			for (int l = 0; l < humParticleEffects.Count; l++)
			{
				humParticleEffects[l].gameObject.SetActive(value: false);
			}
			gameHitter.hitFx = noPowerFx;
			gameHitter.damageAttribute = noPowerAttribute;
			closeHaptic.PlayIfHeldLocal(gameEntity);
			break;
		}
		case State.Extended:
		{
			idleCollider.enabled = false;
			extendedCollider.enabled = true;
			for (int i = 0; i < meshAndMaterials.Count; i++)
			{
				MaterialUtils.SwapMaterial(meshAndMaterials[i], isOnToOff: false);
			}
			humAudioSource.Play();
			dullLight.SetActive(value: true);
			audioSource.PlayOneShot(extendAudio, extendVolume);
			for (int j = 0; j < humParticleEffects.Count; j++)
			{
				humParticleEffects[j].gameObject.SetActive(value: true);
			}
			EnableImpactVFXForCurrentUpgradeLevel();
			gameHitter.damageAttribute = poweredAttribute;
			openHaptic.PlayIfHeldLocal(gameEntity);
			break;
		}
		}
		if (gameEntity.IsHeldByLocalPlayer())
		{
			gameEntity.RequestState(gameEntity.id, (long)newState);
		}
	}

	private bool IsButtonHeld()
	{
		if (!gameEntity.IsHeldByLocalPlayer())
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

	public void OnSuccessfulHit(GameHitData hitData)
	{
		if (state == State.Extended)
		{
			tool.UseEnergy();
		}
	}

	public void GetDebugTextLines(out List<string> strings)
	{
		strings = new List<string>();
		strings.Add($"Knockback: <color=\"yellow\">x{gameHitter.knockbackMultiplier}<color=\"white\">");
	}
}

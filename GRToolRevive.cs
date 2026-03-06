using UnityEngine;
using UnityEngine.XR;

[RequireComponent(typeof(GameEntity))]
public class GRToolRevive : MonoBehaviour
{
	private enum State
	{
		Idle,
		Reviving,
		Cooldown
	}

	public GameEntity gameEntity;

	public GRTool tool;

	[SerializeField]
	private Transform shootFrom;

	[SerializeField]
	private LayerMask playerLayerMask;

	[SerializeField]
	private float reviveDistance = 1.5f;

	[SerializeField]
	private GameObject reviveFx;

	[SerializeField]
	private float reviveSoundVolume;

	[SerializeField]
	private AudioClip reviveSound;

	[SerializeField]
	private float reviveDuration = 0.75f;

	[SerializeField]
	private AudioSource audioSource;

	[Header("Haptic")]
	public AbilityHaptic onHaptic;

	private State state;

	private float stateTimeRemaining;

	private RaycastHit[] tempHitResults = new RaycastHit[128];

	private void Awake()
	{
		state = State.Idle;
	}

	private void OnEnable()
	{
		StopRevive();
		state = State.Idle;
	}

	private void OnDestroy()
	{
	}

	public void Update()
	{
		float deltaTime = Time.deltaTime;
		if (gameEntity.IsHeldByLocalPlayer())
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
				SetStateAuthority(State.Reviving);
			}
			break;
		case State.Reviving:
			stateTimeRemaining -= dt;
			if (stateTimeRemaining <= 0f)
			{
				SetStateAuthority(State.Cooldown);
			}
			break;
		case State.Cooldown:
			if (!IsButtonHeld())
			{
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
		if (state != newState)
		{
			if (state == State.Reviving)
			{
				StopRevive();
			}
			state = newState;
			switch (state)
			{
			case State.Reviving:
				StartRevive();
				stateTimeRemaining = reviveDuration;
				break;
			case State.Idle:
				stateTimeRemaining = -1f;
				break;
			}
		}
	}

	private void StartRevive()
	{
		reviveFx.SetActive(value: true);
		audioSource.volume = reviveSoundVolume;
		audioSource.clip = reviveSound;
		audioSource.Play();
		tool.UseEnergy();
		onHaptic.PlayIfHeldLocal(gameEntity);
		if (!gameEntity.IsAuthority())
		{
			return;
		}
		int num = Physics.SphereCastNonAlloc(shootFrom.position, 0.5f, shootFrom.rotation * Vector3.forward, tempHitResults, reviveDistance, playerLayerMask);
		for (int i = 0; i < num; i++)
		{
			RaycastHit raycastHit = tempHitResults[i];
			Rigidbody attachedRigidbody = raycastHit.collider.attachedRigidbody;
			if (!(attachedRigidbody == null))
			{
				GRPlayer component = attachedRigidbody.GetComponent<GRPlayer>();
				if (component != null && component.State != GRPlayer.GRPlayerState.Alive)
				{
					GhostReactorManager.Get(gameEntity).RequestPlayerStateChange(component, GRPlayer.GRPlayerState.Alive);
					break;
				}
			}
		}
	}

	private void StopRevive()
	{
		reviveFx.SetActive(value: false);
		audioSource.Stop();
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
}

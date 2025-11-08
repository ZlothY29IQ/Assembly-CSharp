using GorillaLocomotion;
using UnityEngine;

public class SIGadgetGrenadeGravity : SIGadgetGrenade
{
	private enum State
	{
		Idle,
		Activated,
		Triggered,
		Count
	}

	[Header("Activation")]
	[SerializeField]
	private float counterDuration = 1f;

	[Header("Gravity Effect")]
	[SerializeField]
	private GameObject gravityField;

	[SerializeField]
	private bool freezePositionOnTrigger;

	[SerializeField]
	private float triggerDuration = 5f;

	[SerializeField]
	private float standardGravityMultiplier = 1f;

	[SerializeField]
	private float attractorStrength;

	[Header("FX")]
	[SerializeField]
	private MeshRenderer mesh;

	[SerializeField]
	private Material idleMat;

	[SerializeField]
	private Material activatedMat;

	[SerializeField]
	private Material triggeredMat;

	private State state;

	private float stateRemainingDuration;

	private bool isLocalPlayerInEffect;

	protected override void OnEnable()
	{
		base.OnEnable();
		gravityField.SetActive(value: false);
		state = State.Idle;
		stateRemainingDuration = -1f;
		isLocalPlayerInEffect = false;
	}

	protected override void HandleActivated()
	{
		if (state == State.Idle)
		{
			activatedLocally = true;
			SetStateAuthority(State.Activated);
		}
		else
		{
			SetStateAuthority(State.Idle);
		}
	}

	protected override void HandleThrown()
	{
	}

	protected override void HandleHitSurface()
	{
	}

	protected override void OnUpdateAuthority(float dt)
	{
		switch (state)
		{
		case State.Activated:
			stateRemainingDuration -= dt;
			if (stateRemainingDuration <= 0f)
			{
				SetStateAuthority(State.Triggered);
			}
			break;
		case State.Triggered:
			stateRemainingDuration -= dt;
			if (stateRemainingDuration <= 0f)
			{
				SetStateAuthority(State.Idle);
			}
			else if (freezePositionOnTrigger)
			{
				CheckReenabledFreezePosition();
			}
			break;
		case State.Idle:
			break;
		}
	}

	protected override void OnUpdateRemote(float dt)
	{
		State state = (State)gameEntity.GetState();
		if (state != this.state)
		{
			SetState(state);
		}
		if (freezePositionOnTrigger)
		{
			CheckReenabledFreezePosition();
		}
	}

	private void SetStateAuthority(State newState)
	{
		SetState(newState);
		gameEntity.RequestState(gameEntity.id, (long)newState);
	}

	private void SetState(State newState)
	{
		if (newState != state && CanChangeState((long)newState))
		{
			state = newState;
			switch (state)
			{
			case State.Idle:
				activatedLocally = false;
				stateRemainingDuration = -1f;
				mesh.material = idleMat;
				DeactivateGravityEffect();
				break;
			case State.Activated:
				stateRemainingDuration = counterDuration;
				mesh.material = activatedMat;
				DeactivateGravityEffect();
				break;
			case State.Triggered:
				stateRemainingDuration = triggerDuration;
				mesh.material = triggeredMat;
				ActivateGravityEffect();
				break;
			}
		}
	}

	public bool CanChangeState(long newStateIndex)
	{
		if (newStateIndex < 0 || newStateIndex >= 3)
		{
			return false;
		}
		return true;
	}

	private void ActivateGravityEffect()
	{
		gravityField.SetActive(value: true);
		if (freezePositionOnTrigger)
		{
			rb.isKinematic = true;
			rb.linearVelocity = Vector3.zero;
		}
	}

	private void DeactivateGravityEffect()
	{
		gravityField.SetActive(value: false);
		if (isLocalPlayerInEffect)
		{
			isLocalPlayerInEffect = false;
			GTPlayer instance = GTPlayer.Instance;
			if (instance != null)
			{
				instance.UnsetGravityOverride(this);
			}
		}
		if (freezePositionOnTrigger && !thrownGadget.IsHeld())
		{
			rb.isKinematic = false;
		}
	}

	private void CheckReenabledFreezePosition()
	{
		if (state == State.Triggered && !thrownGadget.IsHeld() && !rb.isKinematic)
		{
			rb.isKinematic = true;
			rb.linearVelocity = Vector3.zero;
		}
	}

	private void OnTriggerEnter(Collider collider)
	{
		GTPlayer instance = GTPlayer.Instance;
		if (instance != null && collider == instance.headCollider)
		{
			isLocalPlayerInEffect = true;
			instance.SetGravityOverride(this, GravityOverrideFunction);
		}
	}

	private void OnTriggerExit(Collider collider)
	{
		GTPlayer instance = GTPlayer.Instance;
		if (instance != null && collider == instance.headCollider)
		{
			isLocalPlayerInEffect = false;
			instance.UnsetGravityOverride(this);
		}
	}

	public void GravityOverrideFunction(GTPlayer player)
	{
		Vector3 vector = Physics.gravity * standardGravityMultiplier;
		Vector3 vector2 = Vector3.zero;
		if (!thrownGadget.IsHeldLocal())
		{
			vector2 = (base.transform.position - player.headCollider.transform.position).normalized * attractorStrength;
		}
		player.AddForce((vector + vector2) * player.scale, ForceMode.Acceleration);
	}
}

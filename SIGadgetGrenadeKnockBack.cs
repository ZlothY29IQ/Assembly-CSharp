using GorillaLocomotion;
using Photon.Pun;
using UnityEngine;

public class SIGadgetGrenadeKnockBack : SIGadgetGrenade
{
	private enum State
	{
		Idle,
		Thrown,
		Triggered
	}

	[SerializeField]
	private float knockbackStrength;

	[SerializeField]
	private float explosionRadius;

	private State state;

	protected override void OnEnable()
	{
		base.OnEnable();
		state = State.Idle;
	}

	protected override void HandleActivated()
	{
	}

	protected override void HandleHitSurface()
	{
		if (state == State.Thrown)
		{
			SetStateAuthority(State.Triggered);
		}
	}

	protected override void HandleThrown()
	{
		if (state == State.Idle)
		{
			SetStateAuthority(State.Thrown);
		}
	}

	protected override void OnUpdateAuthority(float dt)
	{
	}

	protected override void OnUpdateRemote(float dt)
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
		if (newState != state)
		{
			state = newState;
			switch (state)
			{
			case State.Triggered:
				TriggerExplosion();
				break;
			case State.Idle:
			case State.Thrown:
				break;
			}
		}
	}

	private void TriggerExplosion()
	{
		Vector3 vector = GTPlayer.Instance.transform.position - base.transform.position;
		float sqrMagnitude = vector.sqrMagnitude;
		if (explosionRadius * explosionRadius > sqrMagnitude)
		{
			float num = Mathf.Sqrt(sqrMagnitude);
			float num2 = 1f - num / explosionRadius;
			float speed = knockbackStrength * num2;
			GTPlayer.Instance.ApplyKnockback(vector.normalized, speed);
		}
		if (gameEntity.lastHeldByActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
		{
			SetStateAuthority(State.Idle);
		}
		GrenadeFinished?.Invoke();
	}
}

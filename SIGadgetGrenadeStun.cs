using Photon.Pun;
using UnityEngine;

public class SIGadgetGrenadeStun : SIGadgetGrenade
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
		Collider[] array = Physics.OverlapSphere(base.transform.position, explosionRadius, UnityLayer.GorillaTagCollider.ToLayerMask());
		for (int i = 0; i < array.Length; i++)
		{
			VRRig componentInParent = array[i].GetComponentInParent<VRRig>();
			if (componentInParent != null)
			{
				Vector3 vector = componentInParent.transform.position - base.transform.position;
				float magnitude = vector.magnitude;
				float num = 1f - magnitude / explosionRadius;
				float num2 = knockbackStrength * num;
				RoomSystem.LaunchPlayer(componentInParent.OwningNetPlayer, num2 * vector / magnitude);
				RoomSystem.SendStatusEffectToPlayer(RoomSystem.StatusEffects.TaggedTime, componentInParent.OwningNetPlayer);
			}
		}
		if (gameEntity.lastHeldByActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
		{
			SetStateAuthority(State.Idle);
		}
	}
}

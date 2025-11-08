using UnityEngine;

public class GRBarrierOverloadable : MonoBehaviour
{
	public enum State
	{
		Active,
		Destroyed
	}

	public GRTool tool;

	public GameEntity gameEntity;

	public AudioSource audioSource;

	public MeshRenderer meshRenderer;

	public Collider collider;

	private State state;

	private void OnEnable()
	{
		tool.OnEnergyChange += OnEnergyChange;
		gameEntity.OnStateChanged += OnEntityStateChanged;
	}

	private void OnEnergyChange(GRTool tool, int energyChange, GameEntityId chargingEntityId)
	{
		if (state == State.Active && tool.energy >= tool.GetEnergyMax())
		{
			SetState(State.Destroyed);
			if (gameEntity.IsAuthority())
			{
				gameEntity.RequestState(gameEntity.id, 1L);
			}
		}
	}

	private void OnEntityStateChanged(long prevState, long nextState)
	{
		if (!gameEntity.IsAuthority())
		{
			SetState((State)nextState);
		}
	}

	public void SetState(State newState)
	{
		if (state != newState)
		{
			state = newState;
			switch (state)
			{
			case State.Active:
				meshRenderer.enabled = true;
				collider.enabled = true;
				break;
			case State.Destroyed:
				audioSource.Play();
				meshRenderer.enabled = false;
				collider.enabled = false;
				break;
			}
		}
	}
}

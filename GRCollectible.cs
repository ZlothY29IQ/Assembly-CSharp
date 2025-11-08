using System;
using UnityEngine;

public class GRCollectible : MonoBehaviour, IGameEntityComponent
{
	public GameEntity entity;

	public int energyValue = 100;

	public ProgressionManager.CoreType type;

	public Action OnCollected;

	private void Awake()
	{
	}

	public void OnEntityInit()
	{
		GameEntityManager manager = entity.manager;
		GameEntity gameEntity = manager.GetGameEntity(manager.GetEntityIdFromNetId((int)entity.createData));
		if (gameEntity != null)
		{
			GRCollectibleDispenser component = gameEntity.GetComponent<GRCollectibleDispenser>();
			if (component != null)
			{
				component.GetSpawnedCollectible(this);
			}
		}
	}

	public void OnEntityDestroy()
	{
	}

	public void OnEntityStateChange(long prevState, long nextState)
	{
	}

	public void InvokeOnCollected()
	{
		OnCollected?.Invoke();
	}
}

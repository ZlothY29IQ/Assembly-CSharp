using System.Collections.Generic;
using UnityEngine;

public class GameHittable : MonoBehaviour
{
	public GameEntity gameEntity;

	private List<IGameHittable> components;

	private void Awake()
	{
		components = new List<IGameHittable>(1);
		GetComponentsInChildren(components);
	}

	public void RequestHit(GameHitData hitData)
	{
		hitData.hitEntityId = gameEntity.id;
		gameEntity.manager.RequestHit(hitData);
	}

	public void ApplyHit(GameHitData hitData)
	{
		for (int i = 0; i < components.Count; i++)
		{
			components[i].OnHit(hitData);
		}
		GameHitter component = gameEntity.manager.GetGameEntity(hitData.hitByEntityId).GetComponent<GameHitter>();
		if (component != null)
		{
			component.ApplyHit(hitData);
		}
	}

	public bool IsHitValid(GameHitData hitData)
	{
		for (int i = 0; i < components.Count; i++)
		{
			if (!components[i].IsHitValid(hitData))
			{
				return false;
			}
		}
		return true;
	}
}

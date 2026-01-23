using System;
using System.Collections.Generic;
using UnityEngine;

public class GameHittable : MonoBehaviour
{
	[Serializable]
	public class HittablePoint
	{
		public List<Collider> colliders;

		public GRDamageFlash damageFlash;
	}

	public GameEntity gameEntity;

	public List<HittablePoint> hittablePoints;

	private List<IGameHittable> components;

	private void Awake()
	{
		components = new List<IGameHittable>(1);
		GetComponentsInChildren(components);
		for (int i = 0; i < hittablePoints.Count; i++)
		{
			hittablePoints[i].damageFlash.Setup();
		}
	}

	private void OnEnable()
	{
		if (gameEntity != null)
		{
			GameEntity obj = gameEntity;
			obj.OnTick = (Action)Delegate.Combine(obj.OnTick, new Action(OnUpdate));
		}
	}

	private void OnDisable()
	{
		if (gameEntity != null)
		{
			GameEntity obj = gameEntity;
			obj.OnTick = (Action)Delegate.Remove(obj.OnTick, new Action(OnUpdate));
		}
	}

	public void OnUpdate()
	{
		for (int i = 0; i < hittablePoints.Count; i++)
		{
			hittablePoints[i].damageFlash.Update();
		}
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
		GetHittablePoint(hitData.hittablePoint)?.damageFlash.Play();
	}

	private HittablePoint GetHittablePoint(int hittablePoint)
	{
		if (hittablePoint < 0 || hittablePoint >= hittablePoints.Count)
		{
			return null;
		}
		return hittablePoints[hittablePoint];
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
		if (hittablePoints.Count > 0 && (hitData.hittablePoint < 0 || hitData.hittablePoint >= hittablePoints.Count))
		{
			return false;
		}
		return true;
	}

	public int FindHittablePoint(Collider collider)
	{
		if (hittablePoints == null || hittablePoints.Count == 0)
		{
			return 0;
		}
		for (int i = 0; i < hittablePoints.Count; i++)
		{
			if (hittablePoints[i].colliders.Contains(collider))
			{
				return i;
			}
		}
		return 0;
	}

	public bool IsColliderValid(Collider collider)
	{
		if (hittablePoints == null || hittablePoints.Count == 0)
		{
			return true;
		}
		for (int i = 0; i < hittablePoints.Count; i++)
		{
			if (hittablePoints[i].colliders.Contains(collider))
			{
				return true;
			}
		}
		return false;
	}
}

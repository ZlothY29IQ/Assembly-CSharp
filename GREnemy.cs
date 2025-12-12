using System;
using System.Collections.Generic;
using GorillaTagScripts.GhostReactor;
using UnityEngine;

public class GREnemy : MonoBehaviour, IGameEntityComponent, IGameHittable
{
	public GRHealthMeter healthMeter;

	public GREnemyType enemyType;

	public GameEntity gameEntity;

	public GRDamageFlash damageFlash;

	private void Awake()
	{
		damageFlash.Setup();
	}

	public void OnEntityInit()
	{
		if (gameEntity != null)
		{
			GameEntity obj = gameEntity;
			obj.OnTick = (Action)Delegate.Combine(obj.OnTick, new Action(OnUpdate));
		}
	}

	public void OnEntityDestroy()
	{
		if (gameEntity != null)
		{
			GameEntity obj = gameEntity;
			obj.OnTick = (Action)Delegate.Combine(obj.OnTick, new Action(OnUpdate));
		}
	}

	public void OnEntityStateChange(long prevState, long nextState)
	{
	}

	public static void HideRenderers(List<Renderer> renderers, bool hide)
	{
		if (renderers == null)
		{
			return;
		}
		for (int i = 0; i < renderers.Count; i++)
		{
			if (renderers[i] != null)
			{
				renderers[i].enabled = !hide;
			}
		}
	}

	public static void HideObjects(List<GameObject> objects, bool hide)
	{
		if (objects == null)
		{
			return;
		}
		for (int i = 0; i < objects.Count; i++)
		{
			if (objects[i] != null)
			{
				objects[i].SetActive(!hide);
			}
		}
	}

	public void OnUpdate()
	{
		damageFlash.Update();
	}

	public void SetMaxHP(int maxHp)
	{
		if (healthMeter != null)
		{
			healthMeter.Setup(maxHp);
		}
	}

	public void SetHP(int newHp)
	{
		if (healthMeter != null)
		{
			healthMeter.SetHP(newHp);
		}
	}

	public bool IsHitValid(GameHitData hit)
	{
		return true;
	}

	public void OnHit(GameHitData hit)
	{
		if (hit.hitAmount > 0)
		{
			damageFlash.Play();
		}
	}
}

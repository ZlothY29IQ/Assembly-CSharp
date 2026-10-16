using System;
using UnityEngine;

[RequireComponent(typeof(GameEntity))]
public class SpawnableEntity : MonoBehaviour, IGorillaSliceableSimple
{
	public GameEntity entity;

	[Tooltip("Lifespan in seconds.  If zero, object will not expire.")]
	public float lifespan = 300f;

	public string type;

	private float _expireTime;

	private bool _held;

	private EntitySpawner _spawner;

	public void SetSpawner(EntitySpawner spawner)
	{
		_spawner = spawner;
	}

	private void Reset()
	{
		entity = GetComponent<GameEntity>();
	}

	private void Start()
	{
		if (!entity)
		{
			entity = GetComponent<GameEntity>();
		}
		if (lifespan > 0f)
		{
			GameEntity gameEntity = entity;
			gameEntity.OnGrabbed = (Action)Delegate.Combine(gameEntity.OnGrabbed, new Action(OnGrabbed));
			GameEntity gameEntity2 = entity;
			gameEntity2.OnReleased = (Action)Delegate.Combine(gameEntity2.OnReleased, new Action(OnReleased));
			StartCountdown();
		}
	}

	private void OnDestroy()
	{
		if (!ApplicationQuittingState.IsQuitting)
		{
			if (lifespan > 0f)
			{
				GameEntity gameEntity = entity;
				gameEntity.OnGrabbed = (Action)Delegate.Remove(gameEntity.OnGrabbed, new Action(OnGrabbed));
				GameEntity gameEntity2 = entity;
				gameEntity2.OnReleased = (Action)Delegate.Remove(gameEntity2.OnReleased, new Action(OnReleased));
				StopCountdown();
			}
			_spawner.Unregister(this);
		}
	}

	private void OnGrabbed()
	{
		_held = true;
	}

	private void OnReleased()
	{
		_expireTime = Time.time + lifespan;
		_held = false;
	}

	private void StartCountdown()
	{
		_expireTime = Time.time + lifespan;
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
	}

	private void StopCountdown()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
	}

	public void SliceUpdate()
	{
		if (!_held && entity.IsAuthority() && Time.time >= _expireTime)
		{
			entity.manager.RequestDestroyItem(entity.id);
		}
	}
}

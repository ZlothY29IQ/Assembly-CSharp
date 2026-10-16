using System.Collections.Generic;
using UnityEngine;

public class EntitySpawner : MonoBehaviour, IGorillaSliceableSimple
{
	[SerializeField]
	private SpawnableEntity[] spawnables;

	[SerializeField]
	private Vector3 spawnArea;

	[SerializeField]
	private bool randomizeRotation;

	[Tooltip("Number of entities spawned per second while spawning")]
	[SerializeField]
	private float spawnRate = 1f;

	[Tooltip("Maximum number of live spawns. 0 = no limit")]
	[SerializeField]
	private int maxSpawns = 5;

	[Tooltip("If true, spawning stops once maxSpawns is reached. If false, spawning pauses until there is room again")]
	[SerializeField]
	private bool stopWhenFull = true;

	[Tooltip("If true, will start spawning immediately")]
	[SerializeField]
	private bool startSpawningOnEnable;

	[Tooltip("If true, will destroy all spawned objects on disable")]
	[SerializeField]
	private bool destroySpawnsOnDisable;

	private EntitySpawnManager _manager;

	private List<SpawnableEntity> _spawns = new List<SpawnableEntity>();

	private bool _isSpawning;

	private float _lastSliceTime;

	private float _spawnAccumulator;

	public SpawnableEntity[] Spawnables => spawnables;

	public bool IsSpawning => _isSpawning;

	public bool IsFull
	{
		get
		{
			if (maxSpawns > 0)
			{
				return _spawns.Count >= maxSpawns;
			}
			return false;
		}
	}

	private bool MissingManager()
	{
		return !GetComponentInParent<EntitySpawnManager>(includeInactive: true);
	}

	private void Awake()
	{
		_manager = GetComponentInParent<EntitySpawnManager>(includeInactive: true);
		if (!_manager)
		{
			Debug.LogError(base.name + ": EntitySpawner requires an EntitySpawnManager on a parent object", this);
		}
	}

	private void OnEnable()
	{
		if (startSpawningOnEnable)
		{
			StartSpawning();
		}
	}

	private void OnDisable()
	{
		if (!ApplicationQuittingState.IsQuitting)
		{
			StopSpawning();
			if (destroySpawnsOnDisable)
			{
				DestroyAllSpawns();
			}
		}
	}

	public void SpawnEntity()
	{
		if ((bool)_manager && _manager.CanSpawn && spawnables.Length != 0)
		{
			SpawnableEntity prefab = spawnables[Random.Range(0, spawnables.Length)];
			Vector3 position = base.transform.TransformPoint(new Vector3(Random.Range((0f - spawnArea.x) / 2f, spawnArea.x / 2f), Random.Range((0f - spawnArea.y) / 2f, spawnArea.y / 2f), Random.Range((0f - spawnArea.z) / 2f, spawnArea.z / 2f)));
			Quaternion rotation = (randomizeRotation ? Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)) : base.transform.rotation);
			SpawnableEntity spawnableEntity = _manager.SpawnEntity(prefab, position, rotation);
			if ((bool)spawnableEntity)
			{
				_spawns.Add(spawnableEntity);
				spawnableEntity.SetSpawner(this);
			}
		}
	}

	public void StartSpawning()
	{
		if (!_isSpawning)
		{
			_isSpawning = true;
			_lastSliceTime = Time.time;
			_spawnAccumulator = 0f;
			GorillaSlicerSimpleManager.RegisterSliceable(this);
		}
	}

	public void StopSpawning()
	{
		if (_isSpawning)
		{
			_isSpawning = false;
			GorillaSlicerSimpleManager.UnregisterSliceable(this);
		}
	}

	public void DestroyAllSpawns()
	{
		foreach (SpawnableEntity spawn in _spawns)
		{
			if ((bool)spawn)
			{
				spawn.entity.manager.RequestDestroyItem(spawn.entity.id);
			}
		}
	}

	public void SliceUpdate()
	{
		float time = Time.time;
		_spawnAccumulator += (time - _lastSliceTime) * spawnRate;
		_lastSliceTime = time;
		while (_spawnAccumulator >= 1f)
		{
			_spawns.RemoveAll((SpawnableEntity s) => !s);
			if (IsFull)
			{
				_spawnAccumulator = 0f;
				if (stopWhenFull)
				{
					StopSpawning();
				}
				break;
			}
			_spawnAccumulator -= 1f;
			SpawnEntity();
		}
	}

	public void Unregister(SpawnableEntity spawn)
	{
		if ((bool)spawn)
		{
			_spawns.Remove(spawn);
		}
	}

	private void OnDrawGizmos()
	{
		Gizmos.matrix = base.transform.localToWorldMatrix;
		Gizmos.DrawWireCube(Vector3.zero, spawnArea);
		Gizmos.matrix = Matrix4x4.identity;
	}
}

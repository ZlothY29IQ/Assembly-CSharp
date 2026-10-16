using UnityEngine;

[RequireComponent(typeof(GameEntity))]
[RequireComponent(typeof(XSceneRefTarget))]
public class EntitySpawnManager : MonoBehaviour, IGameEntityComponent
{
	[SerializeField]
	private GameEntity entity;

	private EntitySpawner[] _spawners;

	private bool _managerIsAuthority;

	private bool _zoneIsActive;

	public bool CanSpawn
	{
		get
		{
			if (_managerIsAuthority)
			{
				return _zoneIsActive;
			}
			return false;
		}
	}

	private void Reset()
	{
		entity = GetComponent<GameEntity>();
	}

	private void Awake()
	{
		_spawners = GetComponentsInChildren<EntitySpawner>(includeInactive: true);
		RegisterSpawnables();
	}

	private void RegisterSpawnables()
	{
		EntitySpawner[] spawners = _spawners;
		foreach (EntitySpawner entitySpawner in spawners)
		{
			if ((bool)entitySpawner && entitySpawner.Spawnables != null)
			{
				GameEntityManager.AddToFactory(entitySpawner.Spawnables);
			}
		}
	}

	public SpawnableEntity SpawnEntity(SpawnableEntity[] spawns, Vector3 position, Quaternion rotation)
	{
		if (!CanSpawn)
		{
			return null;
		}
		if (spawns == null || spawns.Length == 0)
		{
			return null;
		}
		SpawnableEntity spawnableEntity = spawns[Random.Range(0, spawns.Length)];
		if (!spawnableEntity)
		{
			return null;
		}
		GameEntityId id = entity.manager.RequestCreateItem(spawnableEntity.name.GetStaticHash(), position, rotation, 0L);
		return entity.manager.GetGameEntity(id)?.GetComponent<SpawnableEntity>();
	}

	public SpawnableEntity SpawnEntity(SpawnableEntity prefab, Vector3 position, Quaternion rotation)
	{
		if (!CanSpawn)
		{
			return null;
		}
		if (!prefab)
		{
			return null;
		}
		GameEntityId id = entity.manager.RequestCreateItem(prefab.name.GetStaticHash(), position, rotation, 0L);
		return entity.manager.GetGameEntity(id)?.GetComponent<SpawnableEntity>();
	}

	public void OnEntityInit()
	{
		RegisterSpawnables();
		entity.manager.OnAuthorityChanged += OnAuthorityChanged;
		entity.manager.OnZoneActiveChanged += OnZoneActiveChanged;
		SetIsAuthority(entity.manager.IsAuthority());
		SetZoneActive(entity.manager.IsZoneActive());
	}

	public void OnEntityDestroy()
	{
		entity.manager.OnAuthorityChanged -= OnAuthorityChanged;
		entity.manager.OnZoneActiveChanged -= OnZoneActiveChanged;
	}

	public void OnEntityStateChange(long prevState, long newState)
	{
	}

	private void OnAuthorityChanged(NetPlayer fromPlayer, NetPlayer toPlayer)
	{
		SetIsAuthority(toPlayer?.IsLocal ?? false);
	}

	private void OnZoneActiveChanged(bool zoneActive)
	{
		SetZoneActive(zoneActive);
	}

	private void SetIsAuthority(bool newAuthority)
	{
		_managerIsAuthority = newAuthority;
	}

	private void SetZoneActive(bool newActive)
	{
		_zoneIsActive = newActive;
	}
}

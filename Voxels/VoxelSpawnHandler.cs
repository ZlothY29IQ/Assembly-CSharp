using System;
using UnityEngine;

namespace Voxels;

[RequireComponent(typeof(GameEntity))]
public class VoxelSpawnHandler : MonoBehaviour, IGameEntityComponent
{
	[Serializable]
	private class SpawnableSet
	{
		public int interval = 100;

		public float chance = 0.1f;

		public GameEntity[] prefabs;
	}

	[SerializeField]
	private GameEntity entity;

	[SerializeField]
	private VoxelMaterialSet materialSet;

	[SerializeField]
	private SpawnableSet[] spawnables;

	private bool _managerIsAuthority;

	private bool _zoneIsActive;

	private bool _isListening;

	private int[] _counts;

	private bool _spawnablesRegistered;

	private void Reset()
	{
		entity = GetComponent<GameEntity>();
	}

	private void Awake()
	{
		RegisterSpawnables();
	}

	private void OnDisable()
	{
		UpdateListeningState();
	}

	private void RegisterSpawnables()
	{
		if (_spawnablesRegistered)
		{
			return;
		}
		GameEntityManager gameEntityManager = entity.manager;
		if (!gameEntityManager)
		{
			foreach (GameEntityManager value in GameEntityManager.managersByZone.Values)
			{
				if (value.GetZoneSceneName() == base.gameObject.scene.name)
				{
					gameEntityManager = value;
					break;
				}
			}
		}
		SpawnableSet[] array = spawnables;
		foreach (SpawnableSet spawnableSet in array)
		{
			gameEntityManager.AddToFactory(spawnableSet.prefabs);
		}
		_spawnablesRegistered = true;
	}

	public void OnEntityInit()
	{
		_counts = new int[materialSet.Materials.Length];
		RegisterSpawnables();
		entity.manager.OnAuthorityChanged += OnAuthorityChanged;
		entity.manager.OnZoneActiveChanged += OnZoneActiveChanged;
		SetIsAuthority(entity.manager.IsAuthority());
		SetZoneActive(entity.manager.IsZoneActive());
	}

	public void OnEntityDestroy()
	{
		entity.manager.OnAuthorityChanged -= OnAuthorityChanged;
		entity.manager.OnZoneActiveChanged += OnZoneActiveChanged;
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
		if (newAuthority != _managerIsAuthority)
		{
			_managerIsAuthority = newAuthority;
			UpdateListeningState();
		}
	}

	private void SetZoneActive(bool newActive)
	{
		if (newActive != _zoneIsActive)
		{
			_zoneIsActive = newActive;
			UpdateListeningState();
		}
	}

	private void UpdateListeningState()
	{
		bool flag = _managerIsAuthority && _zoneIsActive;
		if (flag != _isListening)
		{
			if (flag)
			{
				VoxelEvents.OnResourcesMined += OnResourcesMined;
			}
			else
			{
				VoxelEvents.OnResourcesMined -= OnResourcesMined;
			}
			_isListening = flag;
		}
	}

	private void OnResourcesMined(VoxelWorld world, Vector3 hitPoint, Vector3 hitNormal, int[] amounts)
	{
		if (world.MaterialSet != materialSet || (ZoneGraphBSP.Instance.FindZoneAtPoint(hitPoint)?.zoneId ?? GTZone.none) != entity.manager.zone)
		{
			return;
		}
		for (int i = 0; i < amounts.Length; i++)
		{
			_counts[i] += amounts[i];
			while (_counts[i] >= spawnables[i].interval)
			{
				_counts[i] -= spawnables[i].interval;
				if (UnityEngine.Random.value < spawnables[i].chance)
				{
					SpawnItem(spawnables[i], hitPoint, hitNormal);
				}
			}
		}
	}

	private void SpawnItem(SpawnableSet spawns, Vector3 hitPoint, Vector3 hitNormal)
	{
		GameEntity gameEntity = spawns.prefabs[UnityEngine.Random.Range(0, spawns.prefabs.Length)];
		Quaternion rotation = Quaternion.LookRotation(hitNormal) * Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);
		GameEntityId id = entity.manager.RequestCreateItem(gameEntity.name.GetStaticHash(), hitPoint - hitNormal * 0.1f, rotation, 0L);
		GameEntity gameEntity2 = entity.manager.GetGameEntity(id);
		if ((bool)gameEntity2)
		{
			Rigidbody component = gameEntity2.GetComponent<Rigidbody>();
			component.linearVelocity = hitNormal * 3f;
			component.angularVelocity = UnityEngine.Random.insideUnitSphere * 32f;
			gameEntity2.PlayThrowFx();
		}
	}
}

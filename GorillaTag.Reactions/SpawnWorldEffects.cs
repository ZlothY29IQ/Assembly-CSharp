using GorillaExtensions;
using GorillaNetworking;
using UnityEngine;

namespace GorillaTag.Reactions;

public class SpawnWorldEffects : MonoBehaviour
{
	[Tooltip("The defaults are numbers for the flamethrower hair dryer.")]
	private readonly float _maxParticleHitReactionRate = 2f;

	[Tooltip("Must be in the global object pool and have a tag.")]
	[SerializeField]
	private GameObject _prefabToSpawn;

	private bool _hasPrefabToSpawn;

	private bool _isPrefabInPool;

	private double _lastCollisionTime;

	private SinglePool _pool;

	protected void OnEnable()
	{
		if (GorillaComputer.instance == null)
		{
			Debug.LogError("SpawnWorldEffects: Disabling because GorillaComputer not found! Hierarchy path: " + base.transform.GetPath(), this);
			base.enabled = false;
			return;
		}
		if (_prefabToSpawn != null && !_isPrefabInPool)
		{
			if (_prefabToSpawn.CompareTag("Untagged"))
			{
				Debug.LogError("SpawnWorldEffects: Disabling because Spawn Prefab has no tag! Hierarchy path: " + base.transform.GetPath(), this);
				base.enabled = false;
				return;
			}
			_isPrefabInPool = ObjectPools.instance.DoesPoolExist(_prefabToSpawn);
			if (!_isPrefabInPool)
			{
				Debug.LogError("SpawnWorldEffects: Disabling because Spawn Prefab not in pool! Hierarchy path: " + base.transform.GetPath(), this);
				base.enabled = false;
				return;
			}
			_pool = ObjectPools.instance.GetPoolByObjectType(_prefabToSpawn);
		}
		_hasPrefabToSpawn = _prefabToSpawn != null && _isPrefabInPool;
	}

	public void RequestSpawn(Vector3 worldPosition)
	{
		RequestSpawn(worldPosition, Vector3.up);
	}

	public void RequestSpawn(Vector3 worldPosition, Vector3 normal)
	{
		if (_maxParticleHitReactionRate < 1E-05f || !FireManager.hasInstance)
		{
			return;
		}
		double num = GTTime.TimeAsDouble();
		if (!((float)(num - _lastCollisionTime) < 1f / _maxParticleHitReactionRate))
		{
			if (_hasPrefabToSpawn && _isPrefabInPool && _pool.GetInactiveCount() > 0)
			{
				FireManager.SpawnFire(_pool, worldPosition, normal, base.transform.lossyScale.x);
			}
			_lastCollisionTime = num;
		}
	}
}

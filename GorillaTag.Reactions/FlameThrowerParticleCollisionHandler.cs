using System.Collections.Generic;
using GorillaExtensions;
using GorillaNetworking;
using UnityEngine;

namespace GorillaTag.Reactions;

public class FlameThrowerParticleCollisionHandler : MonoBehaviour
{
	[Tooltip("The defaults are numbers for the flamethrower hair dryer.")]
	private readonly float _maxParticleHitReactionRate = 2f;

	[Tooltip("Must be in the global object pool and have a tag.")]
	[SerializeField]
	private GameObject _prefabToSpawn;

	[Tooltip("How much to extinguish any hit fire by.")]
	[SerializeField]
	private float _extinguishAmount;

	private ParticleSystem _particleSystem;

	private List<ParticleCollisionEvent> _collisionEvents;

	private bool _hasPrefabToSpawn;

	private bool _isPrefabInPool;

	private double _lastCollisionTime;

	private SinglePool _pool;

	protected void OnEnable()
	{
		if (GorillaComputer.instance == null)
		{
			Debug.LogError("FlameThrowerParticleCollisionHandler: Disabling because GorillaComputer not found! Hierarchy path: " + base.transform.GetPath(), this);
			base.enabled = false;
			return;
		}
		if (_prefabToSpawn != null && !_isPrefabInPool)
		{
			if (_prefabToSpawn.CompareTag("Untagged"))
			{
				Debug.LogError("FlameThrowerParticleCollisionHandler: Disabling because Spawn Prefab has no tag! Hierarchy path: " + base.transform.GetPath(), this);
				base.enabled = false;
				return;
			}
			_isPrefabInPool = ObjectPools.instance.DoesPoolExist(_prefabToSpawn);
			if (!_isPrefabInPool)
			{
				Debug.LogError("FlameThrowerParticleCollisionHandler: Disabling because Spawn Prefab not in pool! Hierarchy path: " + base.transform.GetPath(), this);
				base.enabled = false;
				return;
			}
			_pool = ObjectPools.instance.GetPoolByObjectType(_prefabToSpawn);
		}
		_hasPrefabToSpawn = _prefabToSpawn != null && _isPrefabInPool;
		if (_particleSystem == null)
		{
			_particleSystem = GetComponent<ParticleSystem>();
		}
		if (_particleSystem == null)
		{
			Debug.LogError("FlameThrowerParticleCollisionHandler: Disabling because could not find ParticleSystem! Hierarchy path: " + base.transform.GetPath(), this);
			base.enabled = false;
		}
		else if (_collisionEvents == null)
		{
			_collisionEvents = new List<ParticleCollisionEvent>(_particleSystem.main.maxParticles);
		}
	}

	protected void OnParticleCollision(GameObject other)
	{
		if (_maxParticleHitReactionRate < 1E-05f || !FireManager.hasInstance)
		{
			return;
		}
		double num = GTTime.TimeAsDouble();
		if (!((float)(num - _lastCollisionTime) < 1f / _maxParticleHitReactionRate) && _particleSystem.GetCollisionEvents(other, _collisionEvents) > 0)
		{
			if (_hasPrefabToSpawn && _isPrefabInPool && _pool.GetInactiveCount() > 0)
			{
				ParticleCollisionEvent particleCollisionEvent = _collisionEvents[0];
				FireManager.SpawnFire(_pool, particleCollisionEvent.intersection, particleCollisionEvent.normal, base.transform.lossyScale.x);
			}
			if (_extinguishAmount > 0f)
			{
				FireManager.Extinguish(other, _extinguishAmount);
			}
			_lastCollisionTime = num;
		}
	}
}

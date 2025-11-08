using GorillaExtensions;
using UnityEngine;

namespace Critters.Scripts;

public class CrittersActorSpawner : MonoBehaviour
{
	public CrittersActorSpawnerPoint spawnPoint;

	public CrittersActor currentSpawnedObject;

	public CrittersActor.CrittersActorType actorType;

	public int subActorIndex = -1;

	public Collider insideSpawnerCheck;

	public int spawnDelay = 5;

	public bool applyImpulseOnSpawn = true;

	public bool attachSpawnedObjectToSpawnLocation;

	private double nextSpawnTime;

	private void Awake()
	{
		spawnPoint.OnSpawnChanged += HandleSpawnedActor;
	}

	private void OnEnable()
	{
		if (!CrittersManager.instance.actorSpawners.Contains(this))
		{
			CrittersManager.instance.actorSpawners.Add(this);
		}
	}

	private void OnDisable()
	{
		if (CrittersManager.instance.actorSpawners.Contains(this))
		{
			CrittersManager.instance.actorSpawners.Remove(this);
		}
	}

	public void ProcessLocal()
	{
		if (!CrittersManager.instance.LocalAuthority())
		{
			return;
		}
		if (nextSpawnTime <= (double)Time.time)
		{
			nextSpawnTime = Time.time + (float)spawnDelay;
			if (currentSpawnedObject == null || !currentSpawnedObject.isEnabled)
			{
				SpawnActor();
			}
		}
		if (currentSpawnedObject.IsNotNull())
		{
			if (!currentSpawnedObject.isEnabled)
			{
				currentSpawnedObject = null;
				spawnPoint.SetSpawnedActor(null);
			}
			else if (!insideSpawnerCheck.bounds.Contains(currentSpawnedObject.transform.position))
			{
				currentSpawnedObject.RemoveDespawnBlock();
				currentSpawnedObject = null;
				spawnPoint.SetSpawnedActor(null);
			}
			else if (!VerifySpawnAttached())
			{
				currentSpawnedObject.RemoveDespawnBlock();
				currentSpawnedObject = null;
				spawnPoint.SetSpawnedActor(null);
			}
		}
	}

	public void DoReset()
	{
		currentSpawnedObject = null;
	}

	private void HandleSpawnedActor(CrittersActor spawnedActor)
	{
		currentSpawnedObject = spawnedActor;
	}

	private void SpawnActor()
	{
		CrittersActor crittersActor = CrittersManager.instance.SpawnActor(actorType, subActorIndex);
		spawnPoint.SetSpawnedActor(crittersActor);
		if (crittersActor.IsNull())
		{
			return;
		}
		if (attachSpawnedObjectToSpawnLocation)
		{
			crittersActor.GrabbedBy(spawnPoint, positionOverride: true);
			return;
		}
		crittersActor.MoveActor(spawnPoint.transform.position, spawnPoint.transform.rotation);
		crittersActor.rb.linearVelocity = Vector3.zero;
		if (applyImpulseOnSpawn)
		{
			crittersActor.SetImpulse();
		}
	}

	private bool VerifySpawnAttached()
	{
		if (attachSpawnedObjectToSpawnLocation)
		{
			CrittersManager.instance.actorById.TryGetValue(currentSpawnedObject.parentActorId, out var value);
			if (value.IsNull() || value != spawnPoint)
			{
				return false;
			}
		}
		return true;
	}
}

using Critters.Scripts;
using UnityEngine;

public class CrittersActorSpawnerShim : MonoBehaviour
{
	public Transform spawnerPointTransform;

	public CrittersActor.CrittersActorType actorType;

	public int subActorIndex;

	public BoxCollider insideSpawnerBounds;

	public int spawnDelay;

	public bool applyImpulseOnSpawn;

	public bool attachSpawnedObjectToSpawnLocation;

	public BoxCollider colliderTrigger;

	[ContextMenu("Copy Spawner Data To Shim")]
	private CrittersActorSpawner CopySpawnerDataInPrefab()
	{
		CrittersActorSpawner component = base.gameObject.GetComponent<CrittersActorSpawner>();
		spawnerPointTransform = component.spawnPoint.transform;
		actorType = component.actorType;
		subActorIndex = component.subActorIndex;
		insideSpawnerBounds = (BoxCollider)component.insideSpawnerCheck;
		spawnDelay = component.spawnDelay;
		applyImpulseOnSpawn = component.applyImpulseOnSpawn;
		attachSpawnedObjectToSpawnLocation = component.attachSpawnedObjectToSpawnLocation;
		colliderTrigger = base.gameObject.GetComponent<BoxCollider>();
		return component;
	}

	[ContextMenu("Replace Spawner With Shim")]
	private void ReplaceSpawnerWithShim()
	{
		CrittersActorSpawner crittersActorSpawner = CopySpawnerDataInPrefab();
		if (crittersActorSpawner.spawnPoint.GetComponent<Rigidbody>() != null)
		{
			Object.DestroyImmediate(crittersActorSpawner.spawnPoint.GetComponent<Rigidbody>());
		}
		Object.DestroyImmediate(crittersActorSpawner.spawnPoint);
		Object.DestroyImmediate(crittersActorSpawner);
	}
}

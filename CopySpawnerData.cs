using System.Collections.Generic;
using System.Linq;
using Critters.Scripts;
using UnityEngine;

public class CopySpawnerData : MonoBehaviour
{
	public Transform spawnerDataParent;

	[ContextMenu("Copy Spawner Data")]
	private void CopySpawnerDataInPrefab()
	{
		if (spawnerDataParent != null)
		{
			Object.DestroyImmediate(spawnerDataParent.gameObject);
		}
		spawnerDataParent = new GameObject().transform;
		spawnerDataParent.name = "Spawner Data Parent";
		spawnerDataParent.parent = base.transform;
		spawnerDataParent.localPosition = Vector3.zero;
		spawnerDataParent.localRotation = Quaternion.identity;
		CopyEquipmentSpawner();
		CopyCageDeposits();
	}

	private void CopyCageDeposits()
	{
		List<CrittersCageDepositShim> list = Object.FindObjectsByType<CrittersCageDepositShim>(FindObjectsSortMode.None).ToList();
		for (int i = 0; i < list.Count; i++)
		{
			CrittersCageDepositShim crittersCageDepositShim = list[i];
			GameObject obj = new GameObject();
			obj.transform.position = crittersCageDepositShim.transform.position;
			obj.transform.rotation = crittersCageDepositShim.transform.rotation;
			obj.layer = crittersCageDepositShim.gameObject.layer;
			obj.transform.parent = spawnerDataParent;
			obj.name = "Cage Deposit";
			CrittersCageDeposit crittersCageDeposit = obj.AddComponent<CrittersCageDeposit>();
			crittersCageDeposit.disableGrabOnAttach = crittersCageDepositShim.disableGrabOnAttach;
			crittersCageDeposit.allowMultiAttach = crittersCageDepositShim.allowMultiAttach;
			crittersCageDeposit.snapOnAttach = crittersCageDepositShim.snapOnAttach;
			crittersCageDeposit.depositStartLocation = crittersCageDepositShim.startLocation;
			crittersCageDeposit.depositEndLocation = crittersCageDepositShim.endLocation;
			crittersCageDeposit.submitDuration = crittersCageDepositShim.submitDuration;
			crittersCageDeposit.returnDuration = crittersCageDepositShim.returnDuration;
			crittersCageDeposit.depositStartSound = crittersCageDepositShim.depositStartSound;
			crittersCageDeposit.depositEmptySound = crittersCageDepositShim.depositEmptySound;
			crittersCageDeposit.depositCritterSound = crittersCageDepositShim.depositCritterSound;
			crittersCageDeposit.actorType = crittersCageDepositShim.type;
			BoxCollider boxCollider = obj.AddComponent<BoxCollider>();
			boxCollider.center = crittersCageDepositShim.cageBoxCollider.center;
			boxCollider.size = crittersCageDepositShim.cageBoxCollider.size;
			boxCollider.isTrigger = true;
			boxCollider.includeLayers = crittersCageDepositShim.cageBoxCollider.includeLayers;
			boxCollider.excludeLayers = crittersCageDepositShim.cageBoxCollider.excludeLayers;
			GameObject gameObject = new GameObject();
			gameObject.name = "Attach Point";
			gameObject.transform.parent = crittersCageDeposit.transform;
			CrittersActor crittersActor = (crittersCageDeposit.attachPoint = gameObject.AddComponent<CrittersActor>());
			crittersActor.isSceneActor = true;
			crittersActor.crittersActorType = crittersCageDepositShim.type;
			gameObject.AddComponent<Rigidbody>().isKinematic = true;
			gameObject.transform.position = crittersCageDepositShim.attachPointTransform.position;
			gameObject.transform.rotation = crittersCageDepositShim.attachPointTransform.rotation;
			AudioSource audioSource = gameObject.AddComponent<AudioSource>();
			audioSource.rolloffMode = AudioRolloffMode.Linear;
			audioSource.maxDistance = 15f;
			audioSource.spatialBlend = 1f;
			crittersCageDeposit.depositAudio = audioSource;
			GameObject obj2 = new GameObject();
			Transform child = crittersCageDepositShim.attachPointTransform.GetChild(0);
			obj2.transform.parent = gameObject.transform;
			obj2.transform.position = child.position;
			obj2.transform.rotation = child.rotation;
			MeshFilter meshFilter = obj2.AddComponent<MeshFilter>();
			meshFilter.sharedMesh = child.GetComponent<MeshFilter>().sharedMesh;
			obj2.AddComponent<MeshRenderer>().sharedMaterial = child.GetComponent<MeshRenderer>().sharedMaterial;
			obj2.layer = child.gameObject.layer;
			obj2.AddComponent<MeshCollider>().sharedMesh = meshFilter.sharedMesh;
			ZoneBasedObject zoneBasedObject = obj2.AddComponent<ZoneBasedObject>();
			zoneBasedObject.zones = new GTZone[1];
			zoneBasedObject.zones[0] = GTZone.critters;
		}
	}

	private void CopyEquipmentSpawner()
	{
		List<CrittersActorSpawnerShim> list = Object.FindObjectsByType<CrittersActorSpawnerShim>(FindObjectsSortMode.None).ToList();
		for (int i = 0; i < list.Count; i++)
		{
			CrittersActorSpawnerShim crittersActorSpawnerShim = list[i];
			GameObject gameObject = new GameObject();
			gameObject.transform.position = crittersActorSpawnerShim.transform.position;
			gameObject.transform.rotation = crittersActorSpawnerShim.transform.rotation;
			gameObject.layer = crittersActorSpawnerShim.gameObject.layer;
			gameObject.transform.parent = spawnerDataParent;
			gameObject.name = "Spawner " + crittersActorSpawnerShim.actorType;
			CrittersActorSpawner crittersActorSpawner = gameObject.AddComponent<CrittersActorSpawner>();
			crittersActorSpawner.actorType = crittersActorSpawnerShim.actorType;
			crittersActorSpawner.subActorIndex = crittersActorSpawnerShim.subActorIndex;
			crittersActorSpawner.spawnDelay = crittersActorSpawnerShim.spawnDelay;
			crittersActorSpawner.applyImpulseOnSpawn = crittersActorSpawnerShim.applyImpulseOnSpawn;
			crittersActorSpawner.attachSpawnedObjectToSpawnLocation = crittersActorSpawnerShim.attachSpawnedObjectToSpawnLocation;
			BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
			boxCollider.center = crittersActorSpawnerShim.colliderTrigger.center;
			boxCollider.size = crittersActorSpawnerShim.colliderTrigger.size;
			boxCollider.isTrigger = true;
			GameObject obj = new GameObject();
			obj.name = "Inside Spawner Bounds";
			obj.transform.parent = gameObject.transform;
			obj.transform.position = crittersActorSpawnerShim.insideSpawnerBounds.transform.position;
			obj.transform.rotation = crittersActorSpawnerShim.insideSpawnerBounds.transform.rotation;
			obj.transform.localScale = crittersActorSpawnerShim.insideSpawnerBounds.transform.localScale;
			BoxCollider boxCollider2 = obj.AddComponent<BoxCollider>();
			boxCollider2.size = crittersActorSpawnerShim.insideSpawnerBounds.size;
			boxCollider2.center = crittersActorSpawnerShim.insideSpawnerBounds.center;
			boxCollider2.isTrigger = true;
			obj.layer = crittersActorSpawnerShim.insideSpawnerBounds.gameObject.layer;
			crittersActorSpawner.insideSpawnerCheck = boxCollider2;
			GameObject gameObject2 = new GameObject();
			gameObject2.name = "Spawner Point";
			gameObject2.transform.parent = crittersActorSpawner.transform;
			gameObject2.AddComponent<CrittersActorSpawnerPoint>().isSceneActor = true;
			crittersActorSpawner.spawnPoint = gameObject2.GetComponent<CrittersActorSpawnerPoint>();
			crittersActorSpawner.spawnPoint.crittersActorType = CrittersActor.CrittersActorType.AttachPoint;
			gameObject2.AddComponent<Rigidbody>().isKinematic = true;
			gameObject2.transform.position = crittersActorSpawnerShim.spawnerPointTransform.position;
			gameObject2.transform.rotation = crittersActorSpawnerShim.spawnerPointTransform.rotation;
		}
	}
}

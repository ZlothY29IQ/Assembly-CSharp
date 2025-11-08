using System.Collections.Generic;
using GorillaExtensions;
using Photon.Pun;
using UnityEngine;

public class TransferableObjectSpawner : MonoBehaviour
{
	private enum SpawnMode
	{
		OnGround,
		AtCurrentTransform
	}

	private enum SpawnTrigger
	{
		Timer
	}

	private Vector3 spawnPosition = Vector3.zero;

	private Quaternion spawnRotation = Quaternion.identity;

	[SerializeField]
	private GameObject[] TransferrableObjectsToSpawn;

	private List<TransferrableObject> objectsToSpawn = new List<TransferrableObject>();

	[SerializeField]
	private SpawnMode spawnMode;

	[SerializeField]
	private SpawnTrigger spawnTrigger;

	[SerializeField]
	private double SpawnDelay = 5.0;

	private double lastSpawnTime;

	[SerializeField]
	private LayerMask groundRaycastMask = LayerMask.NameToLayer("Gorilla Object");

	[SerializeField]
	private float spawnRadius = 0.5f;

	public void Awake()
	{
		GameObject[] transferrableObjectsToSpawn = TransferrableObjectsToSpawn;
		for (int i = 0; i < transferrableObjectsToSpawn.Length; i++)
		{
			TransferrableObject componentInChildren = transferrableObjectsToSpawn[i].GetComponentInChildren<TransferrableObject>();
			if (componentInChildren.IsNotNull())
			{
				objectsToSpawn.Add(componentInChildren);
			}
			else
			{
				Debug.LogError("Failed to add object " + componentInChildren.gameObject.name + " - missing a Transferrable object");
			}
		}
	}

	private void OnValidate()
	{
		if (Application.isPlaying)
		{
			return;
		}
		GameObject[] transferrableObjectsToSpawn = TransferrableObjectsToSpawn;
		foreach (GameObject gameObject in transferrableObjectsToSpawn)
		{
			TransferrableObject componentInChildren = gameObject.GetComponentInChildren<TransferrableObject>();
			if (componentInChildren.IsNull())
			{
				Debug.LogError(base.name + " at path " + this.GetComponentPath() + " has " + gameObject.name + " assigned to TransferrableObjectsToSpawn collection, but it does not have a TransferrableObject component.  It will not spawn.");
			}
			else if (componentInChildren.worldShareableInstance == null)
			{
				Debug.LogError(base.name + " at path " + this.GetComponentPath() + " has " + gameObject.name + " assigned to TransferrableObjectsToSpawn collection, but it's worldShareableInstance is null.");
			}
		}
	}

	public void Update()
	{
		if (spawnTrigger == SpawnTrigger.Timer && PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient && PhotonNetwork.Time > lastSpawnTime + SpawnDelay)
		{
			SpawnTransferrableObject();
			lastSpawnTime = PhotonNetwork.Time;
		}
	}

	private bool SpawnOnGround()
	{
		if (Physics.Raycast(new Ray(base.transform.position + Random.insideUnitCircle.x0y() * spawnRadius, Vector3.down), out var hitInfo, 3f, groundRaycastMask))
		{
			spawnPosition = hitInfo.point;
			spawnRotation = Quaternion.FromToRotation(Vector3.up, hitInfo.normal);
			return true;
		}
		return false;
	}

	private void SpawnAtCurrentLocation()
	{
		spawnPosition = base.transform.position;
		spawnRotation = base.transform.rotation;
	}

	public void SpawnTransferrableObject()
	{
		if (!NetworkSystem.Instance.IsMasterClient)
		{
			return;
		}
		switch (spawnMode)
		{
		case SpawnMode.AtCurrentTransform:
			SpawnAtCurrentLocation();
			break;
		case SpawnMode.OnGround:
			if (!SpawnOnGround())
			{
				return;
			}
			break;
		default:
			return;
		}
		TransferrableObject transferrableObject = null;
		int num = 0;
		foreach (TransferrableObject item in objectsToSpawn)
		{
			if (!item.InHand())
			{
				num++;
				if (Random.Range(0, num) == 0)
				{
					transferrableObject = item;
				}
			}
		}
		if (transferrableObject != null)
		{
			if (!transferrableObject.IsLocalOwnedWorldShareable)
			{
				transferrableObject.WorldShareableRequestOwnership();
			}
			if (transferrableObject.worldShareableInstance != null)
			{
				transferrableObject.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
				transferrableObject.worldShareableInstance.SetWillTeleport();
			}
			else
			{
				Debug.LogError("WorldShareableInstance for " + transferrableObject.name + " is null");
			}
		}
	}
}

using UnityEngine;

public class GROneTimeEntitySpawner : MonoBehaviour
{
	public GhostReactor reactor;

	public GameEntity EntityPrefab;

	private bool bHasSpawned;

	private float SpawnDelay = 3f;

	private void Start()
	{
		if (EntityPrefab == null)
		{
			Debug.Log("Can't  spawn null entity", this);
		}
		Invoke("TrySpawn", SpawnDelay);
	}

	private void Update()
	{
	}

	private void TrySpawn()
	{
		if (bHasSpawned || !(EntityPrefab != null))
		{
			return;
		}
		Debug.Log("trying to spawn entity" + EntityPrefab.name, this);
		GameEntityManager gameEntityManager = reactor.grManager.gameEntityManager;
		if (gameEntityManager.IsAuthority())
		{
			if (!gameEntityManager.IsZoneActive())
			{
				Debug.Log("delaying spawn attempt because zone not active", this);
				Invoke("TrySpawn", 0.2f);
			}
			else
			{
				Debug.Log("trying to spawn entity", this);
				gameEntityManager.RequestCreateItem(EntityPrefab.name.GetStaticHash(), base.transform.position + new Vector3(0f, 0f, 0f), base.transform.rotation, 0L);
				bHasSpawned = true;
			}
		}
	}
}

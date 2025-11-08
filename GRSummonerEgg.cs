using UnityEngine;

public class GRSummonerEgg : MonoBehaviour
{
	public GameEntity entity;

	public AudioSource hatchAudio;

	public AbilitySound hatchSound;

	public GameEntity entityPrefabToSpawn;

	public Vector3 spawnOffset = new Vector3(0f, 0f, 0.3f);

	public float minHatchTime = 3f;

	public float maxHatchTime = 6f;

	private float hatchTime = 2f;

	private GRSummonedEntity summonedEntity;

	private void Awake()
	{
		summonedEntity = GetComponent<GRSummonedEntity>();
	}

	private void Start()
	{
		hatchTime = Random.Range(minHatchTime, maxHatchTime);
		Rigidbody component = GetComponent<Rigidbody>();
		if (component != null)
		{
			component.isKinematic = false;
			component.position = base.transform.position;
			component.rotation = base.transform.rotation;
			component.linearVelocity = Vector3.up * 2f;
			component.angularVelocity = Vector3.zero;
		}
		Invoke("HatchEgg", hatchTime);
	}

	public void HatchEgg()
	{
		GRBreakable component = GetComponent<GRBreakable>();
		if ((bool)component)
		{
			component.BreakLocal();
		}
		if (entity.IsAuthority())
		{
			Vector3 position = entity.transform.position + spawnOffset;
			Quaternion identity = Quaternion.identity;
			GameEntityManager gameEntityManager = GhostReactorManager.Get(entity).gameEntityManager;
			Debug.Log($"Attempting to spawn {entityPrefabToSpawn.name} from egg at {position.ToString()}", this);
			gameEntityManager.RequestCreateItem(entityPrefabToSpawn.name.GetStaticHash(), position, identity, (summonedEntity != null) ? summonedEntity.GetSummonerNetID() : 0);
		}
		Invoke("DestroySelf", 2f);
		hatchSound.Play(hatchAudio);
	}

	private void Update()
	{
	}

	public void DestroySelf()
	{
		if (entity.IsAuthority())
		{
			entity.manager.RequestDestroyItem(entity.id);
		}
	}
}

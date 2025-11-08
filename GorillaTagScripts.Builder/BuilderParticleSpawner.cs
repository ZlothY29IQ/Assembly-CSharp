using UnityEngine;

namespace GorillaTagScripts.Builder;

public class BuilderParticleSpawner : MonoBehaviour
{
	[SerializeField]
	private BuilderPiece myPiece;

	public GameObject prefab;

	public float cooldown = 0.1f;

	private float lastSpawnTime;

	[SerializeField]
	private BuilderSmallMonkeTrigger spawnTrigger;

	[SerializeField]
	private bool spawnOnEnter = true;

	[SerializeField]
	private bool spawnOnExit;

	[SerializeField]
	private Transform spawnLocation;

	private void Start()
	{
		spawnTrigger.onTriggerFirstEntered += OnEnter;
		spawnTrigger.onTriggerLastExited += OnExit;
	}

	private void OnDestroy()
	{
		if (spawnTrigger != null)
		{
			spawnTrigger.onTriggerFirstEntered -= OnEnter;
			spawnTrigger.onTriggerLastExited -= OnExit;
		}
	}

	public void TrySpawning()
	{
		if (Time.time > lastSpawnTime + cooldown)
		{
			lastSpawnTime = Time.time;
			ObjectPools.instance.Instantiate(prefab, spawnLocation.position, spawnLocation.rotation, myPiece.GetScale());
		}
	}

	private void OnEnter()
	{
		if (spawnOnEnter)
		{
			TrySpawning();
		}
	}

	private void OnExit()
	{
		if (spawnOnExit)
		{
			TrySpawning();
		}
	}
}

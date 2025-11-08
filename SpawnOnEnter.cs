using UnityEngine;

public class SpawnOnEnter : MonoBehaviour
{
	public GameObject prefab;

	public float cooldown = 0.1f;

	private float lastSpawnTime;

	public void OnTriggerEnter(Collider other)
	{
		if (Time.time > lastSpawnTime + cooldown)
		{
			lastSpawnTime = Time.time;
			ObjectPools.instance.Instantiate(prefab, other.transform.position);
		}
	}
}

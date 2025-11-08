using GorillaTag.Reactions;
using UnityEngine;

[RequireComponent(typeof(SpawnWorldEffects))]
public class SpawnWorldEffectsTrigger : MonoBehaviour
{
	private SpawnWorldEffects swe;

	private float spawnTime;

	[SerializeField]
	private float spawnCooldown = 1f;

	private void OnEnable()
	{
		if (swe == null)
		{
			swe = GetComponent<SpawnWorldEffects>();
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		spawnTime = Time.time;
		swe.RequestSpawn(base.transform.position);
	}

	private void OnTriggerStay(Collider other)
	{
		if (!(Time.time - spawnTime < spawnCooldown))
		{
			swe.RequestSpawn(base.transform.position);
			spawnTime = Time.time;
		}
	}
}

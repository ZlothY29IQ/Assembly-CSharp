using UnityEngine;

public class CosmeticCritterSpawnerShadeFleeing : CosmeticCritterSpawner
{
	private Vector3 spawnPosition;

	public void SetSpawnPosition(Vector3 pos)
	{
		spawnPosition = pos;
	}

	public override void OnSpawn(CosmeticCritter critter)
	{
		base.OnSpawn(critter);
		(critter as CosmeticCritterShadeFleeing).SetFleePosition(spawnPosition, base.transform.position);
	}
}

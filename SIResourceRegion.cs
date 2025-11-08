public class SIResourceRegion : SpawnRegion<GameEntity, SIResourceRegion>
{
	public SIResource resourcePrefab;

	public float LastSpawnTime { get; set; }
}

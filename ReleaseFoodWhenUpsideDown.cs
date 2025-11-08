using Critters.Scripts;
using GorillaExtensions;
using Photon.Pun;
using UnityEngine;

public class ReleaseFoodWhenUpsideDown : MonoBehaviour
{
	public CrittersFoodDispenser dispenser;

	public float angle = 30f;

	private bool latch;

	public Transform spawnPoint;

	public float maxFood;

	public float startingFood;

	public float startingSize;

	public int foodSubIndex;

	public float spawnDelay = 0.6f;

	private double nextSpawnTime;

	private void Awake()
	{
		latch = false;
	}

	private void Update()
	{
		if (!CrittersManager.instance.LocalAuthority() || !dispenser.heldByPlayer)
		{
			return;
		}
		if (Vector3.Angle(base.transform.up, Vector3.down) < angle)
		{
			if (latch)
			{
				return;
			}
			latch = true;
			if (!(nextSpawnTime > (PhotonNetwork.InRoom ? PhotonNetwork.Time : ((double)Time.time))))
			{
				nextSpawnTime = (PhotonNetwork.InRoom ? PhotonNetwork.Time : ((double)Time.time)) + (double)spawnDelay;
				CrittersActor crittersActor = CrittersManager.instance.SpawnActor(CrittersActor.CrittersActorType.Food, foodSubIndex);
				if (!crittersActor.IsNull())
				{
					CrittersFood obj = (CrittersFood)crittersActor;
					obj.MoveActor(spawnPoint.position, spawnPoint.rotation);
					obj.SetImpulseVelocity(Vector3.zero, Vector3.zero);
					obj.SpawnData(maxFood, startingFood, startingSize);
				}
			}
		}
		else
		{
			latch = false;
		}
	}
}

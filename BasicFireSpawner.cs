using GorillaTag;
using GorillaTag.Reactions;
using UnityEngine;

public class BasicFireSpawner : MonoBehaviour
{
	[SerializeField]
	private HashWrapper firePrefab;

	[SerializeField]
	private Vector2 fireScaleMinMax = Vector2.one;

	private SinglePool firePool;

	private float scale;

	private void Awake()
	{
		scale = fireScaleMinMax.y;
	}

	public void InterpolateScale(float f)
	{
		scale = Mathf.Lerp(fireScaleMinMax.x, fireScaleMinMax.y, f);
	}

	public void Spawn()
	{
		if (firePool == null)
		{
			firePool = ObjectPools.instance.GetPoolByHash(firePrefab);
		}
		FireManager.SpawnFire(firePool, base.transform.position, Vector3.up, scale);
	}
}

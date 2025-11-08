using UnityEngine;

public class DelayedDestroyCrittersPooledObject : MonoBehaviour
{
	public float destroyDelay = 1f;

	private float timeToDie = -1f;

	protected void OnEnable()
	{
		if (!(ObjectPools.instance == null) && ObjectPools.instance.initialized)
		{
			timeToDie = Time.time + destroyDelay;
		}
	}

	protected void LateUpdate()
	{
		if (Time.time >= timeToDie)
		{
			CrittersPool.Return(base.gameObject);
		}
	}
}

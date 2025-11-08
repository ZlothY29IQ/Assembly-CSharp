using UnityEngine;

public class DelayedDestroyPooledObj : MonoBehaviour
{
	[Tooltip("Return to the object pool after this many seconds.")]
	public float destroyDelay;

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
		if (Time.time > timeToDie)
		{
			ObjectPools.instance.Destroy(base.gameObject);
		}
	}
}

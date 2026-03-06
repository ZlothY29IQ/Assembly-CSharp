using UnityEngine;

namespace GorillaTag;

[DefaultExecutionOrder(2000)]
public class StaticLodGroup : MonoBehaviour, IGorillaSimpleBackgroundWorker
{
	public const int k_monoDefaultExecutionOrder = 2000;

	private int index;

	public float collisionEnableDistance = 3f;

	public float uiFadeDistanceMax = 10f;

	private bool initialized;

	protected void OnEnable()
	{
		if (initialized)
		{
			StaticLodManager.SetEnabled(index, enable: true);
		}
		else
		{
			GorillaSimpleBackgroundWorkerManager.WorkerSignup(this);
		}
	}

	protected void OnDisable()
	{
		if (initialized)
		{
			StaticLodManager.SetEnabled(index, enable: false);
		}
	}

	private void OnDestroy()
	{
		if (initialized)
		{
			StaticLodManager.Unregister(index);
		}
	}

	public void SimpleWork()
	{
		if (!initialized)
		{
			index = StaticLodManager.Register(this);
			StaticLodManager.SetEnabled(index, enable: true);
			initialized = true;
		}
	}
}

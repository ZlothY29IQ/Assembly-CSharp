using UnityEngine;

namespace GorillaTag;

[DefaultExecutionOrder(2000)]
public class StaticLodGroup : MonoBehaviour
{
	public const int k_monoDefaultExecutionOrder = 2000;

	private int index;

	public float collisionEnableDistance = 3f;

	public float uiFadeDistanceMax = 10f;

	protected void Awake()
	{
		index = StaticLodManager.Register(this);
	}

	protected void OnEnable()
	{
		StaticLodManager.SetEnabled(index, enable: true);
	}

	protected void OnDisable()
	{
		StaticLodManager.SetEnabled(index, enable: false);
	}

	private void OnDestroy()
	{
		StaticLodManager.Unregister(index);
	}
}

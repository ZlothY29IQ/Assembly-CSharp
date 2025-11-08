using GorillaExtensions;
using UnityEngine;
using UnityEngine.Events;

public class TriggerOnSpeed : MonoBehaviour, ITickSystemTick
{
	[SerializeField]
	private float speedThreshold;

	[SerializeField]
	private UnityEvent onFaster;

	[SerializeField]
	private UnityEvent onSlower;

	[SerializeField]
	private GorillaVelocityEstimator velocityEstimator;

	private bool wasFaster;

	public bool TickRunning { get; set; }

	private void OnEnable()
	{
		TickSystem<object>.AddCallbackTarget(this);
	}

	private void OnDisable()
	{
		TickSystem<object>.RemoveCallbackTarget(this);
	}

	public void Tick()
	{
		bool flag = velocityEstimator.linearVelocity.IsLongerThan(speedThreshold);
		if (flag != wasFaster)
		{
			if (flag)
			{
				onFaster.Invoke();
			}
			else
			{
				onSlower.Invoke();
			}
			wasFaster = flag;
		}
	}
}

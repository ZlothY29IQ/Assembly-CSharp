using UnityEngine;
using UnityEngine.Events;

public class TaggedColliderTrigger : MonoBehaviour
{
	public new UnityTag tag;

	public UnityEvent<Collider> onEnter = new UnityEvent<Collider>();

	public UnityEvent<Collider> onExit = new UnityEvent<Collider>();

	public float enterHysteresis = 0.125f;

	public float exitHysteresis = 0.125f;

	private TimeSince _sinceLastEnter;

	private TimeSince _sinceLastExit;

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag(tag) && _sinceLastEnter.HasElapsed(enterHysteresis, resetOnElapsed: true))
		{
			onEnter?.Invoke(other);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.CompareTag(tag) && _sinceLastExit.HasElapsed(exitHysteresis, resetOnElapsed: true))
		{
			onExit?.Invoke(other);
		}
	}
}

using UnityEngine;

public class TriggerEventNotifier : MonoBehaviour
{
	public delegate void TriggerEvent(TriggerEventNotifier notifier, Collider collider);

	[HideInInspector]
	public int maskIndex;

	public event TriggerEvent TriggerEnterEvent;

	public event TriggerEvent TriggerExitEvent;

	private void OnTriggerEnter(Collider other)
	{
		this.TriggerEnterEvent?.Invoke(this, other);
	}

	private void OnTriggerExit(Collider other)
	{
		this.TriggerExitEvent?.Invoke(this, other);
	}
}

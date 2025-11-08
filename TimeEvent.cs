using UnityEngine;
using UnityEngine.Events;

public class TimeEvent : MonoBehaviour
{
	public UnityEvent onEventStart;

	public UnityEvent onEventStop;

	[SerializeField]
	protected bool _ongoing;

	protected void StartEvent()
	{
		_ongoing = true;
		onEventStart?.Invoke();
	}

	protected void StopEvent()
	{
		_ongoing = false;
		onEventStop?.Invoke();
	}
}

using UnityEngine;
using UnityEngine.Events;

public class DevWatchButton : MonoBehaviour
{
	public UnityEvent SearchEvent = new UnityEvent();

	public void OnTriggerEnter(Collider other)
	{
		SearchEvent.Invoke();
	}
}

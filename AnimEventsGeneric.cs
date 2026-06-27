using UnityEngine;
using UnityEngine.Events;

public class AnimEventsGeneric : MonoBehaviour
{
	[SerializeField]
	private UnityEvent event1;

	[SerializeField]
	private UnityEvent event2;

	[SerializeField]
	private UnityEvent event3;

	[SerializeField]
	private UnityEvent event4;

	[SerializeField]
	private UnityEvent event5;

	[SerializeField]
	private UnityEvent event6;

	[SerializeField]
	private UnityEvent event7;

	[SerializeField]
	private UnityEvent event8;

	[SerializeField]
	private UnityEvent event9;

	[SerializeField]
	private UnityEvent event10;

	public void Event1()
	{
		event1.Invoke();
	}

	public void Event2()
	{
		event2.Invoke();
	}

	public void Event3()
	{
		event3.Invoke();
	}

	public void Event4()
	{
		event4.Invoke();
	}

	public void Event5()
	{
		event5.Invoke();
	}

	public void Event6()
	{
		event6.Invoke();
	}

	public void Event7()
	{
		event7.Invoke();
	}

	public void Event8()
	{
		event8.Invoke();
	}

	public void Event9()
	{
		event9.Invoke();
	}

	public void Event10()
	{
		event10.Invoke();
	}
}

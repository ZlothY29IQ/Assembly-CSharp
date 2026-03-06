using System.Collections.Generic;
using UnityEngine.Events;

namespace GorillaExtensions;

public static class UnityEventExtensions
{
	public static void InvokeAll(this IEnumerable<UnityEvent> events)
	{
		foreach (UnityEvent @event in events)
		{
			@event.Invoke();
		}
	}

	public static void InvokeAll<TArg>(this IEnumerable<UnityEvent<TArg>> events, TArg arg)
	{
		foreach (UnityEvent<TArg> @event in events)
		{
			@event.Invoke(arg);
		}
	}
}

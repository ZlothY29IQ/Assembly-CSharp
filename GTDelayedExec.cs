using System;
using UnityEngine;

public class GTDelayedExec : ITickSystemTick
{
	private struct Listener(IDelayedExecListener listener, int contextId)
	{
		public readonly IDelayedExecListener listener = listener;

		public readonly int contextId = contextId;
	}

	public const int k_defaultMaxListenersCount = 1024;

	public static int maxListenersCount = 1024;

	private static float[] _listenerDelays = new float[1024];

	private static Listener[] _listeners = new Listener[1024];

	public static GTDelayedExec instance { get; private set; }

	public static int listenerCount { get; private set; }

	bool ITickSystemTick.TickRunning { get; set; }

	[OnEnterPlay_Run]
	private static void EdReInit()
	{
		_listenerDelays = new float[1024];
		_listeners = new Listener[1024];
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
	private static void InitializeAfterAssemblies()
	{
		listenerCount = 0;
		instance = new GTDelayedExec();
		TickSystem<object>.AddTickCallback(instance);
	}

	internal static void Add(IDelayedExecListener listener, float delay, int contextId)
	{
		if (listenerCount >= maxListenersCount)
		{
			Debug.LogError("ERROR!!!  GTDelayedExec: Recovering from default maximum number of delayed listeners " + 1024 + " reached. Please set the k_defaultMaxListenersCount value to " + maxListenersCount * 2 + ".");
			maxListenersCount *= 2;
			Array.Resize(ref _listenerDelays, maxListenersCount);
			Array.Resize(ref _listeners, maxListenersCount);
		}
		_listenerDelays[listenerCount] = Time.unscaledTime + delay;
		_listeners[listenerCount] = new Listener(listener, contextId);
		listenerCount++;
	}

	void ITickSystemTick.Tick()
	{
		for (int i = 0; i < listenerCount; i++)
		{
			if (Time.unscaledTime >= _listenerDelays[i])
			{
				try
				{
					_listeners[i].listener.OnDelayedAction(_listeners[i].contextId);
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}
				listenerCount--;
				_listenerDelays[i] = _listenerDelays[listenerCount];
				_listeners[i] = _listeners[listenerCount];
				i--;
			}
		}
	}
}

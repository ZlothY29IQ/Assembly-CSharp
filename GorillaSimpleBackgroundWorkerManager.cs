using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class GorillaSimpleBackgroundWorkerManager : MonoBehaviour
{
	private static GorillaSimpleBackgroundWorkerManager _instance;

	private static bool hasInstance = false;

	private static long MINIMUM_TICKS_OF_WORK = 10000L;

	public Queue<IGorillaSimpleBackgroundWorker> workerSignups = new Queue<IGorillaSimpleBackgroundWorker>();

	private Stopwatch stopwatch = new Stopwatch();

	protected void Awake()
	{
		if (hasInstance && _instance != this)
		{
			Object.Destroy(this);
		}
		else
		{
			SetInstance(this);
		}
	}

	private static void SetInstance(GorillaSimpleBackgroundWorkerManager manager)
	{
		_instance = manager;
		hasInstance = true;
		if (Application.isPlaying)
		{
			Object.DontDestroyOnLoad(manager);
		}
	}

	public static void CreateManager()
	{
		GameObject obj = new GameObject("GorillaSimpleBackgroundWorkerManager");
		GorillaSimpleBackgroundWorkerManager instance = obj.AddComponent<GorillaSimpleBackgroundWorkerManager>();
		Object.DontDestroyOnLoad(obj);
		SetInstance(instance);
	}

	public static long DoWork(long ticksOfWork)
	{
		if (!hasInstance)
		{
			CreateManager();
		}
		return _instance._DoWork(ticksOfWork);
	}

	public long _DoWork(long ticksOfWork)
	{
		stopwatch.Restart();
		if (ticksOfWork < MINIMUM_TICKS_OF_WORK)
		{
			ticksOfWork = MINIMUM_TICKS_OF_WORK;
		}
		while (stopwatch.ElapsedTicks < ticksOfWork && workerSignups.Count > 0)
		{
			workerSignups.Dequeue()?.SimpleWork();
		}
		return stopwatch.ElapsedTicks;
	}

	public static void WorkerSignup(IGorillaSimpleBackgroundWorker worker)
	{
		if (!hasInstance)
		{
			CreateManager();
		}
		_instance.workerSignups.Enqueue(worker);
	}
}

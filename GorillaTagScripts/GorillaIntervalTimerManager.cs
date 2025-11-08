using System.Collections.Generic;
using UnityEngine;

namespace GorillaTagScripts;

public class GorillaIntervalTimerManager : MonoBehaviour
{
	private static GorillaIntervalTimerManager instance;

	private static bool hasInstance = false;

	private static List<GorillaIntervalTimer> allTimers = new List<GorillaIntervalTimer>();

	protected void Awake()
	{
		if (hasInstance && instance != null && instance != this)
		{
			Object.Destroy(this);
		}
		else
		{
			SetInstance(this);
		}
	}

	private static void CreateManager()
	{
		SetInstance(new GameObject("GorillaIntervalTimerManager").AddComponent<GorillaIntervalTimerManager>());
	}

	private static void SetInstance(GorillaIntervalTimerManager manager)
	{
		instance = manager;
		hasInstance = true;
		if (Application.isPlaying)
		{
			Object.DontDestroyOnLoad(manager);
		}
	}

	public static void RegisterGorillaTimer(GorillaIntervalTimer gTimer)
	{
		if (!hasInstance)
		{
			CreateManager();
		}
		if (!allTimers.Contains(gTimer))
		{
			allTimers.Add(gTimer);
		}
	}

	public static void UnregisterGorillaTimer(GorillaIntervalTimer gTimer)
	{
		if (!hasInstance)
		{
			CreateManager();
		}
		if (allTimers.Contains(gTimer))
		{
			allTimers.Remove(gTimer);
		}
	}

	private void Update()
	{
		for (int i = 0; i < allTimers.Count; i++)
		{
			allTimers[i].InvokeUpdate();
		}
	}
}

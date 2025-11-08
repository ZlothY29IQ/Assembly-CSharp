using System.Collections.Generic;
using UnityEngine;

namespace GorillaTagScripts;

public class GorillaTimerManager : MonoBehaviour
{
	public static GorillaTimerManager instance;

	public static bool hasInstance = false;

	public static List<GorillaTimer> allTimers = new List<GorillaTimer>();

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

	public static void CreateManager()
	{
		SetInstance(new GameObject("GorillaTimerManager").AddComponent<GorillaTimerManager>());
	}

	private static void SetInstance(GorillaTimerManager manager)
	{
		instance = manager;
		hasInstance = true;
		if (Application.isPlaying)
		{
			Object.DontDestroyOnLoad(manager);
		}
	}

	public static void RegisterGorillaTimer(GorillaTimer gTimer)
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

	public static void UnregisterGorillaTimer(GorillaTimer gTimer)
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

	public void Update()
	{
		for (int i = 0; i < allTimers.Count; i++)
		{
			allTimers[i].InvokeUpdate();
		}
	}
}

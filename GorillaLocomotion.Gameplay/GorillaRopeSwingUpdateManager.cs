using System.Collections.Generic;
using UnityEngine;

namespace GorillaLocomotion.Gameplay;

public class GorillaRopeSwingUpdateManager : MonoBehaviour
{
	public static GorillaRopeSwingUpdateManager instance;

	public static bool hasInstance = false;

	public static List<GorillaRopeSwing> allGorillaRopeSwings = new List<GorillaRopeSwing>();

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
		SetInstance(new GameObject("GorillaRopeSwingUpdateManager").AddComponent<GorillaRopeSwingUpdateManager>());
	}

	private static void SetInstance(GorillaRopeSwingUpdateManager manager)
	{
		instance = manager;
		hasInstance = true;
		if (Application.isPlaying)
		{
			Object.DontDestroyOnLoad(manager);
		}
	}

	public static void RegisterRopeSwing(GorillaRopeSwing ropeSwing)
	{
		if (!hasInstance)
		{
			CreateManager();
		}
		if (!allGorillaRopeSwings.Contains(ropeSwing))
		{
			allGorillaRopeSwings.Add(ropeSwing);
		}
	}

	public static void UnregisterRopeSwing(GorillaRopeSwing ropeSwing)
	{
		if (!hasInstance)
		{
			CreateManager();
		}
		if (allGorillaRopeSwings.Contains(ropeSwing))
		{
			allGorillaRopeSwings.Remove(ropeSwing);
		}
	}

	public void Update()
	{
		for (int i = 0; i < allGorillaRopeSwings.Count; i++)
		{
			allGorillaRopeSwings[i].InvokeUpdate();
		}
	}
}

using System.Collections.Generic;
using UnityEngine;

public class FlockingUpdateManager : MonoBehaviour
{
	public static FlockingUpdateManager instance;

	public static bool hasInstance = false;

	public static List<Flocking> allFlockings = new List<Flocking>();

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
		SetInstance(new GameObject("FlockingUpdateManager").AddComponent<FlockingUpdateManager>());
	}

	private static void SetInstance(FlockingUpdateManager manager)
	{
		instance = manager;
		hasInstance = true;
		if (Application.isPlaying)
		{
			Object.DontDestroyOnLoad(manager);
		}
	}

	public static void RegisterFlocking(Flocking flocking)
	{
		if (!hasInstance)
		{
			CreateManager();
		}
		if (!allFlockings.Contains(flocking))
		{
			allFlockings.Add(flocking);
		}
	}

	public static void UnregisterFlocking(Flocking flocking)
	{
		if (!hasInstance)
		{
			CreateManager();
		}
		if (allFlockings.Contains(flocking))
		{
			allFlockings.Remove(flocking);
		}
	}

	public void Update()
	{
		for (int i = 0; i < allFlockings.Count; i++)
		{
			allFlockings[i].InvokeUpdate();
		}
	}
}

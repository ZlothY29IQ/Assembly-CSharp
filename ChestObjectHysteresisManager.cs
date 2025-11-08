using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(2000)]
public class ChestObjectHysteresisManager : MonoBehaviourTick
{
	public static ChestObjectHysteresisManager instance;

	public static bool hasInstance = false;

	public static List<ChestObjectHysteresis> allChests = new List<ChestObjectHysteresis>();

	protected void Awake()
	{
		if (hasInstance && instance != this)
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
		SetInstance(new GameObject("ChestObjectHysteresisManager").AddComponent<ChestObjectHysteresisManager>());
	}

	private static void SetInstance(ChestObjectHysteresisManager manager)
	{
		instance = manager;
		hasInstance = true;
		if (Application.isPlaying)
		{
			Object.DontDestroyOnLoad(manager);
		}
	}

	public static void RegisterCH(ChestObjectHysteresis cOH)
	{
		if (!hasInstance)
		{
			CreateManager();
		}
		if (!allChests.Contains(cOH))
		{
			allChests.Add(cOH);
		}
	}

	public static void UnregisterCH(ChestObjectHysteresis cOH)
	{
		if (!hasInstance)
		{
			CreateManager();
		}
		if (allChests.Contains(cOH))
		{
			allChests.Remove(cOH);
		}
	}

	public override void Tick()
	{
		for (int i = 0; i < allChests.Count; i++)
		{
			allChests[i].InvokeUpdate();
		}
	}
}

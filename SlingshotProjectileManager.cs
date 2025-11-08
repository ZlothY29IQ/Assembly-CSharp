using System.Collections.Generic;
using UnityEngine;

public class SlingshotProjectileManager : MonoBehaviourTick
{
	public static SlingshotProjectileManager instance;

	public static bool hasInstance = false;

	public static List<SlingshotProjectile> allsP = new List<SlingshotProjectile>();

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
		SetInstance(new GameObject("SlingshotProjectileManager").AddComponent<SlingshotProjectileManager>());
	}

	private static void SetInstance(SlingshotProjectileManager manager)
	{
		instance = manager;
		hasInstance = true;
		if (Application.isPlaying)
		{
			Object.DontDestroyOnLoad(manager);
		}
	}

	public static void RegisterSP(SlingshotProjectile sP)
	{
		if (!hasInstance)
		{
			CreateManager();
		}
		if (!allsP.Contains(sP))
		{
			allsP.Add(sP);
		}
	}

	public static void UnregisterSP(SlingshotProjectile sP)
	{
		if (!hasInstance)
		{
			CreateManager();
		}
		if (allsP.Contains(sP))
		{
			allsP.Remove(sP);
		}
	}

	public override void Tick()
	{
		for (int i = 0; i < allsP.Count; i++)
		{
			allsP[i].InvokeUpdate();
		}
	}
}

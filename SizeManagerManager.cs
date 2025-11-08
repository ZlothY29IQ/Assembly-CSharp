using System.Collections.Generic;
using UnityEngine;

public class SizeManagerManager : MonoBehaviour
{
	[OnEnterPlay_SetNull]
	public static SizeManagerManager instance;

	[OnEnterPlay_Set(false)]
	public static bool hasInstance = false;

	[OnEnterPlay_Clear]
	public static List<SizeManager> allSM = new List<SizeManager>();

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
		SetInstance(new GameObject("SizeManagerManager").AddComponent<SizeManagerManager>());
	}

	private static void SetInstance(SizeManagerManager manager)
	{
		instance = manager;
		hasInstance = true;
		if (Application.isPlaying)
		{
			Object.DontDestroyOnLoad(manager);
		}
	}

	public static void RegisterSM(SizeManager sM)
	{
		if (!hasInstance)
		{
			CreateManager();
		}
		if (!allSM.Contains(sM))
		{
			allSM.Add(sM);
		}
	}

	public static void UnregisterSM(SizeManager sM)
	{
		if (!hasInstance)
		{
			CreateManager();
		}
		if (allSM.Contains(sM))
		{
			allSM.Remove(sM);
		}
	}

	public void FixedUpdate()
	{
		for (int i = 0; i < allSM.Count; i++)
		{
			allSM[i].InvokeFixedUpdate();
		}
	}
}

using System.Collections.Generic;
using UnityEngine;

public class ObjectHierarchyFlattenerManager : MonoBehaviourPostTick
{
	public static ObjectHierarchyFlattenerManager instance;

	[OnEnterPlay_Set(false)]
	public static bool hasInstance = false;

	public static List<ObjectHierarchyFlattener> alloHF = new List<ObjectHierarchyFlattener>();

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
		SetInstance(new GameObject("ObjectHierarchyFlattenerManager").AddComponent<ObjectHierarchyFlattenerManager>());
	}

	private static void SetInstance(ObjectHierarchyFlattenerManager manager)
	{
		instance = manager;
		hasInstance = true;
		if (Application.isPlaying)
		{
			Object.DontDestroyOnLoad(manager);
		}
	}

	public static void RegisterOHF(ObjectHierarchyFlattener rbWI)
	{
		if (!hasInstance)
		{
			CreateManager();
		}
		if (!alloHF.Contains(rbWI))
		{
			alloHF.Add(rbWI);
		}
	}

	public static void UnregisterOHF(ObjectHierarchyFlattener rbWI)
	{
		if (!hasInstance)
		{
			CreateManager();
		}
		if (alloHF.Contains(rbWI))
		{
			alloHF.Remove(rbWI);
		}
	}

	public override void PostTick()
	{
		for (int i = 0; i < alloHF.Count; i++)
		{
			alloHF[i].InvokeLateUpdate();
		}
	}
}

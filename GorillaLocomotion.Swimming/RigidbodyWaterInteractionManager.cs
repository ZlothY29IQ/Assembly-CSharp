using System.Collections.Generic;
using UnityEngine;

namespace GorillaLocomotion.Swimming;

public class RigidbodyWaterInteractionManager : MonoBehaviour
{
	public static RigidbodyWaterInteractionManager instance;

	[OnEnterPlay_Set(false)]
	public static bool hasInstance = false;

	public static List<RigidbodyWaterInteraction> allrBWI = new List<RigidbodyWaterInteraction>();

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
		SetInstance(new GameObject("RigidbodyWaterInteractionManager").AddComponent<RigidbodyWaterInteractionManager>());
	}

	private static void SetInstance(RigidbodyWaterInteractionManager manager)
	{
		instance = manager;
		hasInstance = true;
		if (Application.isPlaying)
		{
			Object.DontDestroyOnLoad(manager);
		}
	}

	public static void RegisterRBWI(RigidbodyWaterInteraction rbWI)
	{
		if (!hasInstance)
		{
			CreateManager();
		}
		if (!allrBWI.Contains(rbWI))
		{
			allrBWI.Add(rbWI);
		}
	}

	public static void UnregisterRBWI(RigidbodyWaterInteraction rbWI)
	{
		if (!hasInstance)
		{
			CreateManager();
		}
		if (allrBWI.Contains(rbWI))
		{
			allrBWI.Remove(rbWI);
		}
	}

	public void FixedUpdate()
	{
		for (int i = 0; i < allrBWI.Count; i++)
		{
			allrBWI[i].InvokeFixedUpdate();
		}
	}
}

using System.Collections.Generic;
using UnityEngine;

public class CosmeticAnchorManager : MonoBehaviour, IGorillaSliceableSimple
{
	public static CosmeticAnchorManager instance;

	public static bool hasInstance = false;

	public static List<CosmeticAnchors> allAnchors = new List<CosmeticAnchors>();

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
		SetInstance(new GameObject("CosmeticAnchorManager").AddComponent<CosmeticAnchorManager>());
	}

	private static void SetInstance(CosmeticAnchorManager manager)
	{
		instance = manager;
		hasInstance = true;
		if (Application.isPlaying)
		{
			Object.DontDestroyOnLoad(manager);
		}
	}

	public static void RegisterCosmeticAnchor(CosmeticAnchors cA)
	{
		if (!hasInstance)
		{
			CreateManager();
		}
		if ((cA.AffectedByHunt() || cA.AffectedByBuilder()) && !allAnchors.Contains(cA))
		{
			allAnchors.Add(cA);
		}
	}

	public static void UnregisterCosmeticAnchor(CosmeticAnchors cA)
	{
		if (!hasInstance)
		{
			CreateManager();
		}
		if ((cA.AffectedByHunt() || cA.AffectedByBuilder()) && allAnchors.Contains(cA))
		{
			allAnchors.Remove(cA);
		}
	}

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void SliceUpdate()
	{
		for (int i = 0; i < allAnchors.Count; i++)
		{
			allAnchors[i].TryUpdate();
		}
	}
}

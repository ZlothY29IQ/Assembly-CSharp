using System.Collections.Generic;
using GorillaTag;
using UnityEngine;

public class WorldShareableItemManager : MonoBehaviour
{
	public static WorldShareableItemManager instance;

	private static bool hasInstance = false;

	public static readonly List<WorldShareableItem> worldShareableItems = new List<WorldShareableItem>(1024);

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

	protected void OnDestroy()
	{
		if (instance == this)
		{
			hasInstance = false;
			instance = null;
		}
	}

	protected void Update()
	{
		if (GTAppState.isQuitting)
		{
			return;
		}
		for (int i = 0; i < worldShareableItems.Count; i++)
		{
			if (worldShareableItems[i] != null)
			{
				worldShareableItems[i].TriggeredUpdate();
			}
		}
	}

	public static void CreateManager()
	{
		if (!GTAppState.isQuitting)
		{
			SetInstance(new GameObject("WorldShareableItemManager").AddComponent<WorldShareableItemManager>());
		}
	}

	private static void SetInstance(WorldShareableItemManager manager)
	{
		if (!GTAppState.isQuitting)
		{
			instance = manager;
			hasInstance = true;
			if (Application.isPlaying)
			{
				Object.DontDestroyOnLoad(manager);
			}
		}
	}

	public static void Register(WorldShareableItem worldShareableItem)
	{
		if (!GTAppState.isQuitting)
		{
			if (!hasInstance)
			{
				CreateManager();
			}
			if (!worldShareableItems.Contains(worldShareableItem))
			{
				worldShareableItems.Add(worldShareableItem);
			}
		}
	}

	public static void Unregister(WorldShareableItem worldShareableItem)
	{
		if (!GTAppState.isQuitting)
		{
			if (!hasInstance)
			{
				CreateManager();
			}
			if (worldShareableItems.Contains(worldShareableItem))
			{
				worldShareableItems.Remove(worldShareableItem);
			}
		}
	}
}

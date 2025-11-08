using System;
using UnityEngine;

public class XSceneRefTarget : MonoBehaviour
{
	public int UniqueID;

	[NonSerialized]
	private int lastRegisteredID = -1;

	private static DateTime epoch = new DateTime(2024, 1, 1);

	private static int lastAssignedID;

	private void Awake()
	{
		Register();
	}

	private void Reset()
	{
		UniqueID = CreateNewID();
	}

	private void OnValidate()
	{
		if (!Application.isPlaying)
		{
			Register();
		}
	}

	public void Register(bool force = false)
	{
		if (UniqueID != lastRegisteredID || force)
		{
			if (lastRegisteredID != -1)
			{
				XSceneRefGlobalHub.Unregister(lastRegisteredID, this);
			}
			XSceneRefGlobalHub.Register(UniqueID, this);
			lastRegisteredID = UniqueID;
		}
	}

	private void OnDestroy()
	{
		XSceneRefGlobalHub.Unregister(UniqueID, this);
	}

	private void AssignNewID()
	{
		UniqueID = CreateNewID();
		Register();
	}

	public static int CreateNewID()
	{
		int num = (int)((DateTime.Now - epoch).TotalSeconds * 8.0 % 2147483646.0) + 1;
		if (num <= lastAssignedID)
		{
			lastAssignedID++;
			return lastAssignedID;
		}
		lastAssignedID = num;
		return num;
	}
}

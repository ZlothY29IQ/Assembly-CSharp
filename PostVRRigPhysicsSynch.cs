using System.Collections.Generic;
using UnityEngine;

public class PostVRRigPhysicsSynch : MonoBehaviour
{
	private static readonly List<AutoSyncTransforms> k_syncList = new List<AutoSyncTransforms>(5);

	private void LateUpdate()
	{
		Physics.SyncTransforms();
	}

	public static void AddSyncTarget(AutoSyncTransforms body)
	{
		k_syncList.Add(body);
	}

	public static void RemoveSyncTarget(AutoSyncTransforms body)
	{
		k_syncList.Remove(body);
	}
}

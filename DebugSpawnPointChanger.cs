using System;
using System.Collections.Generic;
using System.Linq;
using GorillaLocomotion;
using UnityEngine;

public class DebugSpawnPointChanger : MonoBehaviour
{
	[Serializable]
	private struct GeoTriggersGroup
	{
		public string levelName;

		public GorillaGeoHideShowTrigger enterTrigger;

		public GorillaGeoHideShowTrigger[] leaveTrigger;

		public int[] canJumpToIndex;
	}

	[SerializeField]
	private GeoTriggersGroup[] levelTriggers;

	private int lastLocationIndex;

	private void AttachSpawnPoint(VRRig rig, Transform[] spawnPts, int locationIndex)
	{
		if (spawnPts == null)
		{
			return;
		}
		GTPlayer gTPlayer = null;
		gTPlayer = UnityEngine.Object.FindAnyObjectByType<GTPlayer>();
		if (gTPlayer == null)
		{
			return;
		}
		lastLocationIndex = locationIndex;
		foreach (Transform transform in spawnPts)
		{
			if (transform.name == levelTriggers[locationIndex].levelName)
			{
				rig.transform.position = transform.position;
				rig.transform.rotation = transform.rotation;
				gTPlayer.transform.position = transform.position;
				gTPlayer.transform.rotation = transform.rotation;
				gTPlayer.InitializeValues();
				SpawnPoint component = transform.GetComponent<SpawnPoint>();
				if (component != null)
				{
					gTPlayer.SetScaleMultiplier(component.startSize);
					ZoneManagement.SetActiveZone(component.startZone);
				}
				else
				{
					Debug.LogWarning("Attempt to spawn at transform that does not have SpawnPoint component will be ignored: " + transform.name);
				}
				break;
			}
		}
	}

	private void ChangePoint(int index)
	{
		SpawnManager spawnManager = UnityEngine.Object.FindAnyObjectByType<SpawnManager>();
		if (spawnManager != null)
		{
			Transform[] spawnPts = spawnManager.ChildrenXfs();
			VRRig[] array = UnityEngine.Object.FindObjectsByType<VRRig>(FindObjectsSortMode.None);
			foreach (VRRig rig in array)
			{
				AttachSpawnPoint(rig, spawnPts, index);
			}
		}
	}

	public List<string> GetPlausibleJumpLocation()
	{
		return levelTriggers[lastLocationIndex].canJumpToIndex.Select((int index) => levelTriggers[index].levelName).ToList();
	}

	public void JumpTo(int canJumpIndex)
	{
		GeoTriggersGroup geoTriggersGroup = levelTriggers[lastLocationIndex];
		ChangePoint(geoTriggersGroup.canJumpToIndex[canJumpIndex]);
	}

	public void SetLastLocation(string levelName)
	{
		for (int i = 0; i < levelTriggers.Length; i++)
		{
			if (!(levelTriggers[i].levelName != levelName))
			{
				lastLocationIndex = i;
				break;
			}
		}
	}
}

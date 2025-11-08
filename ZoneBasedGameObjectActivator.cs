using System.Collections.Generic;
using UnityEngine;

public class ZoneBasedGameObjectActivator : MonoBehaviour
{
	[SerializeField]
	private GTZone[] zones;

	[SerializeField]
	private GameObject[] gameObjects;

	private void OnEnable()
	{
		ZoneManagement.OnZoneChange += ZoneManagement_OnZoneChange;
	}

	private void OnDisable()
	{
		ZoneManagement.OnZoneChange -= ZoneManagement_OnZoneChange;
	}

	private void ZoneManagement_OnZoneChange(ZoneData[] zoneData)
	{
		HashSet<GTZone> hashSet = new HashSet<GTZone>(zones);
		bool flag = false;
		for (int i = 0; i < zoneData.Length; i++)
		{
			flag |= zoneData[i].active && hashSet.Contains(zoneData[i].zone);
		}
		for (int j = 0; j < gameObjects.Length; j++)
		{
			gameObjects[j].SetActive(flag);
		}
	}
}

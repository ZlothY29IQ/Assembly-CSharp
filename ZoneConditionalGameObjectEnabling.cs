using System;
using UnityEngine;

public class ZoneConditionalGameObjectEnabling : MonoBehaviour
{
	[SerializeField]
	private GTZone zone;

	[SerializeField]
	private bool invisibleWhileLoaded;

	[SerializeField]
	private GameObject[] gameObjects;

	private void Start()
	{
		OnZoneChanged();
		ZoneManagement instance = ZoneManagement.instance;
		instance.onZoneChanged = (Action)Delegate.Combine(instance.onZoneChanged, new Action(OnZoneChanged));
	}

	private void OnDestroy()
	{
		ZoneManagement instance = ZoneManagement.instance;
		instance.onZoneChanged = (Action)Delegate.Remove(instance.onZoneChanged, new Action(OnZoneChanged));
	}

	private void OnZoneChanged()
	{
		if (invisibleWhileLoaded)
		{
			if (gameObjects != null)
			{
				for (int i = 0; i < gameObjects.Length; i++)
				{
					gameObjects[i].SetActive(!ZoneManagement.IsInZone(zone));
				}
			}
		}
		else if (gameObjects != null)
		{
			for (int j = 0; j < gameObjects.Length; j++)
			{
				gameObjects[j].SetActive(ZoneManagement.IsInZone(zone));
			}
		}
	}
}

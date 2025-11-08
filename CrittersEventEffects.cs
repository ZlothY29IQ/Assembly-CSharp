using System;
using System.Collections.Generic;
using GorillaExtensions;
using UnityEngine;

public class CrittersEventEffects : MonoBehaviour
{
	[Serializable]
	public class CrittersEventResponse
	{
		public CrittersManager.CritterEvent eventType;

		public GameObject effect;
	}

	public CrittersManager manager;

	public CrittersEventResponse[] eventEffects;

	private Dictionary<CrittersManager.CritterEvent, GameObject> effectResponse;

	private void Awake()
	{
		if (manager == null)
		{
			GTDev.LogError("CrittersEventEffects missing reference to CrittersManager");
			return;
		}
		effectResponse = new Dictionary<CrittersManager.CritterEvent, GameObject>();
		for (int i = 0; i < eventEffects.Length; i++)
		{
			if (eventEffects[i].effect != null)
			{
				effectResponse.Add(eventEffects[i].eventType, eventEffects[i].effect);
			}
		}
		manager.OnCritterEventReceived += HandleReceivedEvent;
	}

	private void HandleReceivedEvent(CrittersManager.CritterEvent eventType, int sourceActor, Vector3 position, Quaternion rotation)
	{
		if (effectResponse.TryGetValue(eventType, out var value))
		{
			GameObject pooled = CrittersPool.GetPooled(value);
			if (pooled.IsNotNull())
			{
				pooled.transform.position = position;
				pooled.transform.rotation = rotation;
			}
		}
	}
}

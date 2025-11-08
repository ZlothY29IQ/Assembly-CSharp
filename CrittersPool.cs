using System;
using System.Collections.Generic;
using UnityEngine;

public class CrittersPool : MonoBehaviour
{
	[Serializable]
	public class CrittersPoolSettings
	{
		public GameObject poolObject;

		public int poolSize = 20;
	}

	private static CrittersPool instance;

	public CrittersPoolSettings[] eventEffects;

	private Dictionary<GameObject, List<GameObject>> pools;

	public Transform poolParent;

	public static GameObject GetPooled(GameObject prefab)
	{
		return instance?.GetInstance(prefab);
	}

	public static void Return(GameObject pooledGO)
	{
		instance?.ReturnInstance(pooledGO);
	}

	private void Awake()
	{
		if (instance != null)
		{
			UnityEngine.Object.Destroy(this);
			return;
		}
		instance = this;
		SetupPools();
	}

	private void SetupPools()
	{
		pools = new Dictionary<GameObject, List<GameObject>>();
		GameObject gameObject = new GameObject("CrittersPool");
		gameObject.transform.parent = base.transform;
		poolParent = gameObject.transform;
		for (int i = 0; i < eventEffects.Length; i++)
		{
			CrittersPoolSettings crittersPoolSettings = eventEffects[i];
			if (crittersPoolSettings.poolObject == null || crittersPoolSettings.poolSize <= 0)
			{
				GTDev.Log("CrittersPool.SetupPools Failed. Pool has no poolObject or has size 0.");
				continue;
			}
			List<GameObject> list = new List<GameObject>();
			for (int j = 0; j < crittersPoolSettings.poolSize; j++)
			{
				GameObject gameObject2 = UnityEngine.Object.Instantiate(crittersPoolSettings.poolObject);
				gameObject2.transform.SetParent(poolParent);
				gameObject2.name += j;
				gameObject2.SetActive(value: false);
				list.Add(gameObject2);
			}
			pools.Add(crittersPoolSettings.poolObject, list);
		}
	}

	private GameObject GetInstance(GameObject prefab)
	{
		if (pools.TryGetValue(prefab, out var value))
		{
			for (int i = 0; i < value.Count; i++)
			{
				if (value[i] != null && !value[i].activeSelf)
				{
					value[i].SetActive(value: true);
					return value[i];
				}
			}
			GTDev.Log("CrittersPool.GetInstance Failed. No available instance.");
			return null;
		}
		GTDev.LogError("CrittersPool.GetInstance Failed. Prefab doesn't have a valid pool setup.");
		return null;
	}

	private void ReturnInstance(GameObject instance)
	{
		instance.transform.SetParent(poolParent);
		instance.SetActive(value: false);
	}
}

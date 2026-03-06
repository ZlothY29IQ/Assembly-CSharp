using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SinglePool : IGorillaSimpleBackgroundWorker
{
	public GameObject objectToPool;

	public int initAmountToPool = 8;

	private HashSet<int> pooledObjects;

	private Stack<GameObject> inactivePool;

	private Dictionary<int, GameObject> activePool;

	private GameObject gameObject;

	private int amountAllocatedToPool;

	public void SimpleWork()
	{
		int count = inactivePool.Count;
		if (count < initAmountToPool)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(objectToPool, this.gameObject.transform, worldPositionStays: true);
			gameObject.name = objectToPool.name + "(PoolIndex=" + count + ")";
			gameObject.SetActive(value: false);
			inactivePool.Push(gameObject);
			amountAllocatedToPool++;
			int instanceID = gameObject.GetInstanceID();
			pooledObjects.Add(instanceID);
			GorillaSimpleBackgroundWorkerManager.WorkerSignup(this);
		}
	}

	private void PrivAllocPooledObjects()
	{
		if (inactivePool.Count == 0)
		{
			SimpleWork();
		}
		else
		{
			GorillaSimpleBackgroundWorkerManager.WorkerSignup(this);
		}
	}

	public void Initialize(GameObject gameObject_)
	{
		gameObject = gameObject_;
		activePool = new Dictionary<int, GameObject>(initAmountToPool);
		inactivePool = new Stack<GameObject>(initAmountToPool);
		pooledObjects = new HashSet<int>();
		GorillaSimpleBackgroundWorkerManager.WorkerSignup(this);
	}

	public GameObject Instantiate(bool setActive = true)
	{
		if (inactivePool.Count == 0)
		{
			Debug.LogWarning("Pool '" + objectToPool.name + "'is expanding consider changing initial pool size");
			PrivAllocPooledObjects();
		}
		GameObject gameObject = inactivePool.Pop();
		int instanceID = gameObject.GetInstanceID();
		gameObject.SetActive(setActive);
		activePool.Add(instanceID, gameObject);
		return gameObject;
	}

	public void Destroy(GameObject obj)
	{
		int instanceID = obj.GetInstanceID();
		if (activePool.ContainsKey(instanceID) && pooledObjects.Contains(instanceID))
		{
			obj.SetActive(value: false);
			inactivePool.Push(obj);
			activePool.Remove(instanceID);
		}
	}

	public int PoolGUID()
	{
		return PoolUtils.GameObjHashCode(objectToPool);
	}

	public int GetTotalCount()
	{
		return pooledObjects.Count;
	}

	public int GetActiveCount()
	{
		return activePool.Count;
	}

	public int GetInactiveCount()
	{
		return inactivePool.Count;
	}
}

using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPools : MonoBehaviour, IBuildValidation
{
	private struct DelayedSpawnData
	{
		public int prefabHash;

		public Transform xform;

		public Vector3 pos;
	}

	private class DelayedSpawnListener : IDelayedExecListener
	{
		public void OnDelayedAction(int contextId)
		{
			if ((uint)contextId < (uint)_delayedHighWater)
			{
				ref DelayedSpawnData reference = ref _delayedData[contextId];
				if (reference.prefabHash != 0 && instance != null)
				{
					Vector3 position = ((reference.xform != null) ? reference.xform.TransformPoint(reference.pos) : reference.pos);
					instance.Instantiate(reference.prefabHash, position);
				}
				reference = default(DelayedSpawnData);
				_delayedFreeNext[contextId] = _delayedFreeHead;
				_delayedFreeHead = contextId;
			}
		}
	}

	public static ObjectPools instance = null;

	[SerializeField]
	private List<SinglePool> pools;

	private Dictionary<int, SinglePool> lookUp;

	private const int k_delayedInitialCount = 16;

	[OnEnterPlay_Set(0)]
	private static int _delayedHighWater;

	[OnEnterPlay_Set(-1)]
	private static int _delayedFreeHead = -1;

	[OnEnterPlay_SetNew]
	private static DelayedSpawnData[] _delayedData = new DelayedSpawnData[16];

	[OnEnterPlay_SetNew]
	private static int[] _delayedFreeNext = new int[16];

	[OnEnterPlay_SetNew]
	private static readonly DelayedSpawnListener _delayedListener = new DelayedSpawnListener();

	public bool initialized { get; private set; }

	protected void Awake()
	{
		instance = this;
	}

	protected void Start()
	{
		InitializePools();
	}

	public void InitializePools()
	{
		if (initialized)
		{
			return;
		}
		lookUp = new Dictionary<int, SinglePool>();
		foreach (SinglePool pool in pools)
		{
			pool.Initialize(base.gameObject);
			int num = pool.PoolGUID();
			if (lookUp.ContainsKey(num))
			{
				foreach (SinglePool pool2 in pools)
				{
					if (pool2.PoolGUID() == num)
					{
						Debug.LogError("Pools contain more then one instance of the same object\n" + $"First object in question is {pool2.objectToPool} tag: {pool2.objectToPool.tag}\n" + $"Second object is {pool.objectToPool} tag: {pool.objectToPool.tag}");
						break;
					}
				}
			}
			else
			{
				lookUp.Add(pool.PoolGUID(), pool);
			}
		}
		initialized = true;
	}

	public bool DoesPoolExist(GameObject obj)
	{
		return DoesPoolExist(PoolUtils.GameObjHashCode(obj));
	}

	public bool DoesPoolExist(int hash)
	{
		return lookUp.ContainsKey(hash);
	}

	public SinglePool GetPoolByHash(int hash)
	{
		return lookUp[hash];
	}

	public SinglePool GetPoolByObjectType(GameObject obj)
	{
		int hash = PoolUtils.GameObjHashCode(obj);
		return GetPoolByHash(hash);
	}

	public GameObject Instantiate(GameObject obj, bool setActive = true)
	{
		return GetPoolByObjectType(obj).Instantiate(setActive);
	}

	public GameObject Instantiate(int hash, bool setActive = true)
	{
		return GetPoolByHash(hash).Instantiate(setActive);
	}

	public GameObject Instantiate(int hash, Vector3 position, bool setActive = true)
	{
		GameObject obj = Instantiate(hash, setActive);
		obj.transform.position = position;
		return obj;
	}

	public GameObject Instantiate(int hash, Vector3 position, Quaternion rotation, bool setActive = true)
	{
		GameObject obj = Instantiate(hash, setActive);
		obj.transform.SetPositionAndRotation(position, rotation);
		return obj;
	}

	public GameObject Instantiate(GameObject obj, Vector3 position, bool setActive = true)
	{
		GameObject obj2 = Instantiate(obj, setActive);
		obj2.transform.position = position;
		return obj2;
	}

	public GameObject Instantiate(GameObject obj, Vector3 position, Quaternion rotation, bool setActive = true)
	{
		GameObject obj2 = Instantiate(obj, setActive);
		obj2.transform.SetPositionAndRotation(position, rotation);
		return obj2;
	}

	public GameObject Instantiate(GameObject obj, Vector3 position, Quaternion rotation, float scale, bool setActive = true)
	{
		GameObject obj2 = Instantiate(obj, setActive);
		obj2.transform.SetPositionAndRotation(position, rotation);
		obj2.transform.localScale = Vector3.one * scale;
		return obj2;
	}

	public void Destroy(GameObject obj)
	{
		GetPoolByObjectType(obj).Destroy(obj);
	}

	public bool BuildValidationCheck()
	{
		bool result = true;
		foreach (SinglePool pool in pools)
		{
			if (pool.objectToPool == null)
			{
				Debug.Log("GlobalObjectPools contains a nullref. Failing build validation.");
				result = false;
				continue;
			}
			DelayedDestroyPooledObj[] componentsInChildren = pool.objectToPool.GetComponentsInChildren<DelayedDestroyPooledObj>(includeInactive: true);
			if (componentsInChildren.Length > 1)
			{
				Debug.LogError(string.Concat($"Pooled prefab '{pool.objectToPool.name}' has {componentsInChildren.Length} ", "DelayedDestroyPooledObj components in its hierarchy. Only the root should have one. Children with their own will try to pool-destroy themselves and spam 'not contained in the activePool' errors. Extra components on:", string.Concat(Array.ConvertAll(componentsInChildren, (DelayedDestroyPooledObj c) => (!(c.gameObject == pool.objectToPool)) ? ("\n  - " + c.gameObject.name) : ""))), pool.objectToPool);
				result = false;
			}
		}
		return result;
	}

	public static int InstantiateDelayed(GameObject prefab, Vector3 pos, float delay)
	{
		return InstantiateDelayed(prefab, null, pos, delay);
	}

	public static int InstantiateDelayed(GameObject prefab, Transform xform, Vector3 localPos, float delay)
	{
		if (ApplicationQuittingState.IsQuitting)
		{
			return -1;
		}
		int num;
		if (_delayedFreeHead >= 0)
		{
			num = _delayedFreeHead;
			_delayedFreeHead = _delayedFreeNext[num];
		}
		else
		{
			if (_delayedHighWater >= _delayedData.Length)
			{
				int newSize = _delayedData.Length * 2;
				Array.Resize(ref _delayedData, newSize);
				Array.Resize(ref _delayedFreeNext, newSize);
			}
			num = _delayedHighWater++;
		}
		_delayedData[num] = new DelayedSpawnData
		{
			prefabHash = PoolUtils.GameObjHashCode(prefab),
			xform = xform,
			pos = localPos
		};
		GTDelayedExec.Add(_delayedListener, delay, num);
		return num;
	}

	public static void UpdateDelayedInstantiate(int idx, Transform xform)
	{
		if ((uint)idx < (uint)_delayedHighWater)
		{
			_delayedData[idx].xform = xform;
		}
	}

	public static void UpdateDelayedInstantiate(int idx, Vector3 localPos)
	{
		if ((uint)idx < (uint)_delayedHighWater)
		{
			_delayedData[idx].pos = localPos;
		}
	}

	public static void CancelDelayedInstantiate(int idx)
	{
		if ((uint)idx < (uint)_delayedHighWater)
		{
			_delayedData[idx].prefabHash = 0;
		}
	}

	public static void UpdateDelayedInstantiate(int idx, Transform xform, Vector3 localPos)
	{
		if ((uint)idx < (uint)_delayedHighWater)
		{
			ref DelayedSpawnData reference = ref _delayedData[idx];
			reference.xform = xform;
			reference.pos = localPos;
		}
	}
}

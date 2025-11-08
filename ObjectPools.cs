using System.Collections.Generic;
using UnityEngine;

public class ObjectPools : MonoBehaviour, IBuildValidation
{
	public static ObjectPools instance;

	[SerializeField]
	private List<SinglePool> pools;

	private Dictionary<int, SinglePool> lookUp;

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
		foreach (SinglePool pool in pools)
		{
			if (pool.objectToPool == null)
			{
				Debug.Log("GlobalObjectPools contains a nullref. Failing build validation.");
				return false;
			}
		}
		return true;
	}
}

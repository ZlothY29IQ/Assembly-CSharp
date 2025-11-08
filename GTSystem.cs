using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;

[DisallowMultipleComponent]
public abstract class GTSystem<T> : MonoBehaviour, IReadOnlyList<T>, IEnumerable<T>, IEnumerable, IReadOnlyCollection<T> where T : MonoBehaviour
{
	[SerializeField]
	protected List<T> _instances = new List<T>();

	[SerializeField]
	private bool _networked;

	[SerializeField]
	private PhotonView _photonView;

	private static GTSystem<T> gSingleton;

	private static bool gInitializing = false;

	private static bool gAppQuitting = false;

	private static HashSet<T> gQueueRegister = new HashSet<T>();

	public PhotonView photonView => _photonView;

	int IReadOnlyCollection<T>.Count => _instances.Count;

	T IReadOnlyList<T>.this[int index] => _instances[index];

	public static PhotonView PhotonView => gSingleton._photonView;

	protected virtual void Awake()
	{
		SetSingleton(this);
	}

	protected virtual void Tick()
	{
		float deltaTime = Time.deltaTime;
		for (int i = 0; i < _instances.Count; i++)
		{
			T val = _instances[i];
			if ((bool)val)
			{
				OnTick(deltaTime, val);
			}
		}
	}

	protected virtual void OnApplicationQuit()
	{
		gAppQuitting = true;
	}

	protected virtual void OnTick(float dt, T instance)
	{
	}

	private bool RegisterInstance(T instance)
	{
		if (instance == null)
		{
			GTDev.LogError("[" + GetType().Name + "::Register] Instance is null.");
			return false;
		}
		if (_instances.Contains(instance))
		{
			return false;
		}
		_instances.Add(instance);
		OnRegister(instance);
		return true;
	}

	protected virtual void OnRegister(T instance)
	{
	}

	private bool UnregisterInstance(T instance)
	{
		if (instance == null)
		{
			GTDev.LogError("[" + GetType().Name + "::Unregister] Instance is null.");
			return false;
		}
		if (!_instances.Contains(instance))
		{
			return false;
		}
		_instances.Remove(instance);
		OnUnregister(instance);
		return true;
	}

	protected virtual void OnUnregister(T instance)
	{
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return ((IEnumerable<T>)_instances).GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable<T>)_instances).GetEnumerator();
	}

	protected static void SetSingleton(GTSystem<T> system)
	{
		if (gAppQuitting)
		{
			return;
		}
		if (gSingleton != null && gSingleton != system)
		{
			Object.Destroy(system);
			GTDev.LogWarning("Singleton of type " + gSingleton.GetType().Name + " already exists.");
			return;
		}
		gSingleton = system;
		if (gInitializing)
		{
			gSingleton._instances.Clear();
			T[] collection = gQueueRegister.Where((T x) => x != null).ToArray();
			gSingleton._instances.AddRange(collection);
			gQueueRegister.Clear();
			PhotonView component = gSingleton.GetComponent<PhotonView>();
			if (component != null)
			{
				gSingleton._photonView = component;
				gSingleton._networked = true;
			}
			gInitializing = false;
		}
	}

	public static void Register(T instance)
	{
		if (!gAppQuitting && !(instance == null))
		{
			if (gInitializing)
			{
				gQueueRegister.Add(instance);
			}
			else if (gSingleton == null && !gInitializing)
			{
				gInitializing = true;
				gQueueRegister.Add(instance);
			}
			else
			{
				gSingleton.RegisterInstance(instance);
			}
		}
	}

	public static void Unregister(T instance)
	{
		if (!gAppQuitting && !(instance == null))
		{
			if (gInitializing)
			{
				gQueueRegister.Remove(instance);
			}
			else if (gSingleton == null && !gInitializing)
			{
				gInitializing = true;
				gQueueRegister.Remove(instance);
			}
			else
			{
				gSingleton.UnregisterInstance(instance);
			}
		}
	}
}

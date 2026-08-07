using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;

namespace Pooling;

public class Poolable : MonoBehaviour, IPoolable<Poolable>
{
	public UnityEvent onCreate;

	public UnityEvent onPreGet;

	public UnityEvent onPostGet;

	public UnityEvent onRelease;

	public IObjectPool<Poolable> Pool { get; set; }

	public void OnCreate()
	{
		onCreate?.Invoke();
	}

	public void OnPreGet()
	{
		onPreGet?.Invoke();
	}

	public void OnPostGet()
	{
		onPostGet?.Invoke();
	}

	public void OnRelease()
	{
		onRelease?.Invoke();
	}
}

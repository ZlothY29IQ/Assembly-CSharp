using UnityEngine;
using UnityEngine.Pool;

namespace Pooling;

public interface IPoolable<T> where T : Component, IPoolable<T>
{
	IObjectPool<T> Pool { get; set; }

	void OnCreate();

	void OnPreGet();

	void OnPostGet();

	void OnRelease();
}

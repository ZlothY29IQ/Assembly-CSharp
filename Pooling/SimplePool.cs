using UnityEngine.Pool;

namespace Pooling;

public static class SimplePool<T> where T : class, new()
{
	private static readonly ObjectPool<T> s_Pool = new ObjectPool<T>(() => new T());

	public static T Get()
	{
		return s_Pool.Get();
	}

	public static PooledObject<T> Get(out T value)
	{
		return s_Pool.Get(out value);
	}

	public static void Release(T toRelease)
	{
		s_Pool.Release(toRelease);
	}
}

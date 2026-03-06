using System;

[Serializable]
public struct GTSerializableKeyValue<T1, T2>(T1 k, T2 v)
{
	public T1 k = k;

	public T2 v = v;
}

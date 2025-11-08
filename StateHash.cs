using System;

[Serializable]
public struct StateHash
{
	public int last;

	public int next;

	public override int GetHashCode()
	{
		return HashCode.Combine(last, next);
	}

	public override string ToString()
	{
		return GetHashCode().ToString();
	}

	public bool Changed()
	{
		if (last == next)
		{
			return false;
		}
		last = next;
		return true;
	}

	public void Poll<T0>(T0 v0)
	{
		last = next;
		next = HashCode.Combine(v0);
	}

	public void Poll<T1, T2>(T1 v1, T2 v2)
	{
		last = next;
		next = HashCode.Combine(v1, v2);
	}

	public void Poll<T1, T2, T3>(T1 v1, T2 v2, T3 v3)
	{
		last = next;
		next = HashCode.Combine(v1, v2, v3);
	}

	public void Poll<T1, T2, T3, T4>(T1 v1, T2 v2, T3 v3, T4 v4)
	{
		last = next;
		next = HashCode.Combine(v1, v2, v3, v4);
	}

	public void Poll<T1, T2, T3, T4, T5>(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5)
	{
		last = next;
		next = HashCode.Combine(v1, v2, v3, v4, v5);
	}

	public void Poll<T1, T2, T3, T4, T5, T6>(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6)
	{
		last = next;
		next = HashCode.Combine(v1, v2, v3, v4, v5, v6);
	}

	public void Poll<T1, T2, T3, T4, T5, T6, T7>(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7)
	{
		last = next;
		next = HashCode.Combine(v1, v2, v3, v4, v5, v6, v7);
	}

	public void Poll<T1, T2, T3, T4, T5, T6, T7, T8>(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7, T8 v8)
	{
		last = next;
		next = HashCode.Combine(v1, v2, v3, v4, v5, v6, v7, v8);
	}

	public void Poll<T1, T2, T3, T4, T5, T6, T7, T8, T9>(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7, T8 v8, T9 v9)
	{
		last = next;
		int value = HashCode.Combine(v1, v2, v3, v4, v5, v6, v7, v8);
		next = HashCode.Combine(value, v9);
	}

	public void Poll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7, T8 v8, T9 v9, T10 v10)
	{
		last = next;
		int value = HashCode.Combine(v1, v2, v3, v4, v5, v6, v7, v8);
		next = HashCode.Combine(value, v9, v10);
	}

	public void Poll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7, T8 v8, T9 v9, T10 v10, T11 v11)
	{
		last = next;
		int value = HashCode.Combine(v1, v2, v3, v4, v5, v6, v7, v8);
		next = HashCode.Combine(value, v9, v10, v11);
	}

	public void Poll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7, T8 v8, T9 v9, T10 v10, T11 v11, T12 v12)
	{
		last = next;
		int value = HashCode.Combine(v1, v2, v3, v4, v5, v6, v7, v8);
		next = HashCode.Combine(value, v9, v10, v11, v12);
	}

	public void Poll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7, T8 v8, T9 v9, T10 v10, T11 v11, T12 v12, T13 v13)
	{
		last = next;
		int value = HashCode.Combine(v1, v2, v3, v4, v5, v6, v7, v8);
		next = HashCode.Combine(value, v9, v10, v11, v12, v13);
	}

	public void Poll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7, T8 v8, T9 v9, T10 v10, T11 v11, T12 v12, T13 v13, T14 v14)
	{
		last = next;
		int value = HashCode.Combine(v1, v2, v3, v4, v5, v6, v7, v8);
		next = HashCode.Combine(value, v9, v10, v11, v12, v13, v14);
	}

	public void Poll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7, T8 v8, T9 v9, T10 v10, T11 v11, T12 v12, T13 v13, T14 v14, T15 v15)
	{
		last = next;
		int value = HashCode.Combine(v1, v2, v3, v4, v5, v6, v7, v8);
		next = HashCode.Combine(value, v9, v10, v11, v12, v13, v14, v15);
	}

	public void Poll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7, T8 v8, T9 v9, T10 v10, T11 v11, T12 v12, T13 v13, T14 v14, T15 v15, T16 v16)
	{
		last = next;
		int value = HashCode.Combine(v1, v2, v3, v4, v5, v6, v7, v8);
		int value2 = HashCode.Combine(value, v9, v10, v11, v12, v13, v14, v15);
		next = HashCode.Combine(value, value2, v16);
	}
}

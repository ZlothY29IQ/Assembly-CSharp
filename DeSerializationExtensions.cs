public static class DeSerializationExtensions
{
	public static bool TryDeserializeTo<T1>(this object[] eventData, out T1 v1)
	{
		v1 = default(T1);
		if (eventData == null || eventData.Length != 1)
		{
			return false;
		}
		if (!(eventData[0] is T1 val))
		{
			return false;
		}
		v1 = val;
		return true;
	}

	public static bool TryDeserializeTo<T1, T2>(this object[] eventData, out T1 v1, out T2 v2)
	{
		v1 = default(T1);
		v2 = default(T2);
		if (eventData == null || eventData.Length != 2)
		{
			return false;
		}
		if (!(eventData[0] is T1 val) || !(eventData[1] is T2 val2))
		{
			return false;
		}
		v1 = val;
		v2 = val2;
		return true;
	}

	public static bool TryDeserializeTo<T1, T2, T3>(this object[] eventData, out T1 v1, out T2 v2, out T3 v3)
	{
		v1 = default(T1);
		v2 = default(T2);
		v3 = default(T3);
		if (eventData == null || eventData.Length != 3)
		{
			return false;
		}
		if (!(eventData[0] is T1 val) || !(eventData[1] is T2 val2) || !(eventData[2] is T3 val3))
		{
			return false;
		}
		v1 = val;
		v2 = val2;
		v3 = val3;
		return true;
	}

	public static bool TryDeserializeTo<T1, T2, T3, T4>(this object[] eventData, out T1 v1, out T2 v2, out T3 v3, out T4 v4)
	{
		v1 = default(T1);
		v2 = default(T2);
		v3 = default(T3);
		v4 = default(T4);
		if (eventData == null || eventData.Length != 4)
		{
			return false;
		}
		if (!(eventData[0] is T1 val) || !(eventData[1] is T2 val2) || !(eventData[2] is T3 val3) || !(eventData[3] is T4 val4))
		{
			return false;
		}
		v1 = val;
		v2 = val2;
		v3 = val3;
		v4 = val4;
		return true;
	}

	public static bool TryDeserializeTo<T1, T2, T3, T4, T5>(this object[] eventData, out T1 v1, out T2 v2, out T3 v3, out T4 v4, out T5 v5)
	{
		v1 = default(T1);
		v2 = default(T2);
		v3 = default(T3);
		v4 = default(T4);
		v5 = default(T5);
		if (eventData == null || eventData.Length != 5)
		{
			return false;
		}
		if (!(eventData[0] is T1 val) || !(eventData[1] is T2 val2) || !(eventData[2] is T3 val3) || !(eventData[3] is T4 val4) || !(eventData[4] is T5 val5))
		{
			return false;
		}
		v1 = val;
		v2 = val2;
		v3 = val3;
		v4 = val4;
		v5 = val5;
		return true;
	}

	public static bool TryDeserializeTo<T1, T2, T3, T4, T5, T6>(this object[] eventData, out T1 v1, out T2 v2, out T3 v3, out T4 v4, out T5 v5, out T6 v6)
	{
		v1 = default(T1);
		v2 = default(T2);
		v3 = default(T3);
		v4 = default(T4);
		v5 = default(T5);
		v6 = default(T6);
		if (eventData == null || eventData.Length != 6)
		{
			return false;
		}
		if (!(eventData[0] is T1 val) || !(eventData[1] is T2 val2) || !(eventData[2] is T3 val3) || !(eventData[3] is T4 val4) || !(eventData[4] is T5 val5) || !(eventData[5] is T6 val6))
		{
			return false;
		}
		v1 = val;
		v2 = val2;
		v3 = val3;
		v4 = val4;
		v5 = val5;
		v6 = val6;
		return true;
	}

	public static bool TryDeserializeToRef<T1>(this object[] eventData, ref T1 v1)
	{
		if (eventData == null || eventData.Length != 1)
		{
			return false;
		}
		if (!(eventData[0] is T1 val))
		{
			return false;
		}
		v1 = val;
		return true;
	}

	public static bool TryDeserializeToRef<T1, T2>(this object[] eventData, ref T1 v1, ref T2 v2)
	{
		if (eventData == null || eventData.Length != 2)
		{
			return false;
		}
		if (!(eventData[0] is T1 val) || !(eventData[1] is T2 val2))
		{
			return false;
		}
		v1 = val;
		v2 = val2;
		return true;
	}

	public static bool TryDeserializeToRef<T1, T2, T3>(this object[] eventData, ref T1 v1, ref T2 v2, ref T3 v3)
	{
		if (eventData == null || eventData.Length != 3)
		{
			return false;
		}
		if (!(eventData[0] is T1 val) || !(eventData[1] is T2 val2) || !(eventData[2] is T3 val3))
		{
			return false;
		}
		v1 = val;
		v2 = val2;
		v3 = val3;
		return true;
	}

	public static bool TryDeserializeToRef<T1, T2, T3, T4>(this object[] eventData, ref T1 v1, ref T2 v2, ref T3 v3, ref T4 v4)
	{
		if (eventData == null || eventData.Length != 4)
		{
			return false;
		}
		if (!(eventData[0] is T1 val) || !(eventData[1] is T2 val2) || !(eventData[2] is T3 val3) || !(eventData[3] is T4 val4))
		{
			return false;
		}
		v1 = val;
		v2 = val2;
		v3 = val3;
		v4 = val4;
		return true;
	}

	public static bool TryDeserializeToRef<T1, T2, T3, T4, T5>(this object[] eventData, ref T1 v1, ref T2 v2, ref T3 v3, ref T4 v4, ref T5 v5)
	{
		if (eventData == null || eventData.Length != 5)
		{
			return false;
		}
		if (!(eventData[0] is T1 val) || !(eventData[1] is T2 val2) || !(eventData[2] is T3 val3) || !(eventData[3] is T4 val4) || !(eventData[4] is T5 val5))
		{
			return false;
		}
		v1 = val;
		v2 = val2;
		v3 = val3;
		v4 = val4;
		v5 = val5;
		return true;
	}

	public static bool TryDeserializeToRef<T1, T2, T3, T4, T5, T6>(this object[] eventData, ref T1 v1, ref T2 v2, ref T3 v3, ref T4 v4, ref T5 v5, ref T6 v6)
	{
		if (eventData == null || eventData.Length != 6)
		{
			return false;
		}
		if (!(eventData[0] is T1 val) || !(eventData[1] is T2 val2) || !(eventData[2] is T3 val3) || !(eventData[3] is T4 val4) || !(eventData[4] is T5 val5) || !(eventData[5] is T6 val6))
		{
			return false;
		}
		v1 = val;
		v2 = val2;
		v3 = val3;
		v4 = val4;
		v5 = val5;
		v6 = val6;
		return true;
	}
}

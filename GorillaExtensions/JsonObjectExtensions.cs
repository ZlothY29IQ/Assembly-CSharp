using PlayFab.Json;

namespace GorillaExtensions;

public static class JsonObjectExtensions
{
	public static T? GetValue<T>(this JsonObject obj, string key)
	{
		if (!obj.TryGetValue(key, out var value))
		{
			return default(T);
		}
		if (value is T)
		{
			return (T)value;
		}
		return default(T);
	}

	public static bool TryGetValue<T>(this JsonObject obj, string key, out T? t)
	{
		t = obj.GetValue<T>(key);
		return t != null;
	}
}

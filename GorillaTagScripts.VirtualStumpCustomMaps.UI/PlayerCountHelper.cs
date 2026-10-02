using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GorillaExtensions;
using GorillaNetworking;
using Modio.Mods;
using PlayFab;
using PlayFab.CloudScriptModels;
using PlayFab.Json;
using UnityEngine;

namespace GorillaTagScripts.VirtualStumpCustomMaps.UI;

public static class PlayerCountHelper
{
	private const string MapsJsonKey = "Maps";

	private const string PlayerCountJsonKey = "PlayerCount";

	public static void GetPlayerCount(Mod mod, Action<string> successCallback, Action<PlayFabError>? errorCallback = null)
	{
		GetPlayerCountInternal(mod.Id.ToString(), delegate(ulong count)
		{
			successCallback(FormatPlayerCount(count));
		}, errorCallback);
	}

	public static void GetPlayerCountBatched(IDictionary<Mod, Action<string>> modsAndCallbacks, Action<PlayFabError>? errorCallback = null)
	{
		GorillaServer instance = GorillaServer.Instance;
		if ((object)instance != null)
		{
			ReturnVstumpMapStatsRequest request = new ReturnVstumpMapStatsRequest
			{
				mapIds = modsAndCallbacks.Keys.Select((Mod mod) => mod.Id.ToString()).ToList()
			};
			instance.ReturnVstumpMapStats(request, delegate(ExecuteFunctionResult executeFunctionResult)
			{
				UnpackSuccessBatched(executeFunctionResult, modsAndCallbacks);
			}, errorCallback ?? new Action<PlayFabError>(DefaultErrorCallback));
		}
	}

	public static Task<(bool success, Dictionary<string, ulong> counts)> GetPlayerCountsAsync(IReadOnlyCollection<string> modIds)
	{
		TaskCompletionSource<(bool, Dictionary<string, ulong>)> completion = new TaskCompletionSource<(bool, Dictionary<string, ulong>)>();
		GorillaServer instance = GorillaServer.Instance;
		if ((object)instance == null)
		{
			completion.SetResult((false, new Dictionary<string, ulong>()));
			return completion.Task;
		}
		ReturnVstumpMapStatsRequest request = new ReturnVstumpMapStatsRequest
		{
			mapIds = modIds.ToList()
		};
		try
		{
			instance.ReturnVstumpMapStats(request, delegate(ExecuteFunctionResult executeFunctionResult)
			{
				completion.TrySetResult((true, UnpackCounts(executeFunctionResult)));
			}, delegate(PlayFabError error)
			{
				DefaultErrorCallback(error);
				completion.TrySetResult((false, new Dictionary<string, ulong>()));
			});
		}
		catch (Exception ex)
		{
			Debug.Log("Error fetching player counts: " + ex.Message);
			completion.TrySetResult((false, new Dictionary<string, ulong>()));
		}
		return completion.Task;
	}

	private static void GetPlayerCountInternal(string modId, Action<ulong> successCallback, Action<PlayFabError>? errorCallback = null)
	{
		GorillaServer instance = GorillaServer.Instance;
		if ((object)instance != null)
		{
			ReturnVstumpMapStatsRequest request = new ReturnVstumpMapStatsRequest
			{
				mapIds = new List<string> { modId }
			};
			instance.ReturnVstumpMapStats(request, delegate(ExecuteFunctionResult executeFunctionResult)
			{
				UnpackSuccess(executeFunctionResult, modId, successCallback);
			}, errorCallback ?? new Action<PlayFabError>(DefaultErrorCallback));
		}
	}

	private static void UnpackSuccess(ExecuteFunctionResult result, string modId, Action<ulong> callback)
	{
		if (result.FunctionResult is JsonObject obj && obj.TryGetValue<JsonObject>("Maps", out JsonObject t) && t != null && t.TryGetValue<JsonObject>(modId, out JsonObject t2) && t2 != null && t2.TryGetValue<ulong>("PlayerCount", out var t3))
		{
			callback(t3);
		}
	}

	private static void UnpackSuccessBatched(ExecuteFunctionResult result, IDictionary<Mod, Action<string>> modsAndCallbacks)
	{
		Dictionary<string, ulong> dictionary = UnpackCounts(result);
		foreach (KeyValuePair<Mod, Action<string>> modsAndCallback in modsAndCallbacks)
		{
			if (dictionary.TryGetValue(modsAndCallback.Key.Id.ToString(), out var value))
			{
				string obj = FormatPlayerCount(value);
				modsAndCallback.Value(obj);
			}
		}
	}

	private static Dictionary<string, ulong> UnpackCounts(ExecuteFunctionResult result)
	{
		Dictionary<string, ulong> dictionary = new Dictionary<string, ulong>();
		if (!(result.FunctionResult is JsonObject obj))
		{
			return dictionary;
		}
		if (!obj.TryGetValue<JsonObject>("Maps", out JsonObject t))
		{
			return dictionary;
		}
		if (t == null)
		{
			return dictionary;
		}
		foreach (string key in t.Keys)
		{
			if (t.TryGetValue<JsonObject>(key, out JsonObject t2) && t2 != null && t2.TryGetValue<ulong>("PlayerCount", out var t3))
			{
				dictionary[key] = t3;
			}
		}
		return dictionary;
	}

	private static void DefaultErrorCallback(PlayFabError error)
	{
		Debug.Log("Error fetching player count: " + error.ErrorMessage);
	}

	public static string FormatPlayerCount(ulong count)
	{
		if (count < 1000)
		{
			return count.ToString();
		}
		float num = count;
		char[] array = new char[3] { 'K', 'M', 'B' };
		foreach (char c in array)
		{
			num /= 1000f;
			if (!(num >= 1000f))
			{
				return num.ToString("###.###") + c;
			}
		}
		throw new Exception("Tried to format too-large player count.");
	}
}

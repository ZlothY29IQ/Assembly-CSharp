using System.Threading.Tasks;
using GorillaNetworking;
using Liv.Lck;
using PlayFab;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
public class LckCosmeticsFeatureFlagManagerPlayFab : ILckCosmeticsFeatureFlagManager
{
	private const string TitleDataKey = "EnableLckCosmetics";

	private const int MaxRetries = 2;

	private const int RetryDelayMilliseconds = 5000;

	private Task<bool> _initializationTask;

	private readonly object _lock = new object();

	[Preserve]
	public LckCosmeticsFeatureFlagManagerPlayFab()
	{
	}

	public Task<bool> IsEnabledAsync()
	{
		if (_initializationTask != null)
		{
			return _initializationTask;
		}
		lock (_lock)
		{
			return _initializationTask ?? (_initializationTask = GetEnabledStateWithRetryAsync());
		}
	}

	private async Task<bool> GetEnabledStateWithRetryAsync()
	{
		for (int i = 0; i < 2; i++)
		{
			if (PlayFabTitleDataCache.Instance == null)
			{
				Debug.LogWarning("LCK: PlayFabTitleDataCache instance is not available. " + $"Retrying feature flag check in {5} seconds... (Attempt {i + 1}/{2})");
				await Task.Delay(5000);
				continue;
			}
			TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
			PlayFabTitleDataCache.Instance.GetTitleData("EnableLckCosmetics", delegate(string data)
			{
				if (bool.TryParse(data, out var result))
				{
					Debug.Log(string.Format("LCK: Feature flag '{0}' is set to '{1}'.", "EnableLckCosmetics", result));
					tcs.TrySetResult(result);
				}
				else
				{
					Debug.LogError("LCK: Failed to parse feature flag 'EnableLckCosmetics' from value '" + data + "'. Defaulting to 'true'.");
					tcs.TrySetResult(result: true);
				}
			}, delegate(PlayFabError error)
			{
				Debug.LogError("LCK: Error fetching feature flag 'EnableLckCosmetics': " + error.ErrorMessage + ". Defaulting to 'true'.");
				tcs.TrySetResult(result: true);
			});
			return await tcs.Task;
		}
		Debug.LogError(string.Format("LCK: {0} instance was not available after {1} attempts. ", "PlayFabTitleDataCache", 2) + "Cosmetics feature will be enabled by default as a fallback measure.");
		return true;
	}
}

using System;
using System.Collections.Generic;
using System.Text;
using PlayFab;
using UnityEngine;

namespace GorillaNetworking;

public class TitleDataFeatureFlags
{
	public string TitleDataKey = "DeployFeatureFlags";

	public Dictionary<string, bool> defaults = new Dictionary<string, bool>
	{
		{ "2026-04-VStumpGrabbablesFix", true },
		{ "2026-04-SuppressZonesInVStump", true }
	};

	private Dictionary<string, int> flagValueByName = new Dictionary<string, int>();

	private Dictionary<string, List<string>> flagValueByUser = new Dictionary<string, List<string>>();

	private readonly HashSet<(string flagName, string playFabId)> logSent = new HashSet<(string, string)>();

	public bool ready { get; private set; }

	public void FetchFeatureFlags()
	{
		PlayFabTitleDataCache.Instance.GetTitleData(TitleDataKey, delegate(string json)
		{
			try
			{
				FeatureFlagData[] flags = JsonUtility.FromJson<FeatureFlagListData>(json).flags;
				foreach (FeatureFlagData featureFlagData in flags)
				{
					if (featureFlagData.valueType == "percent")
					{
						flagValueByName.AddOrUpdate(featureFlagData.name, featureFlagData.value);
					}
					List<string> alwaysOnForUsers = featureFlagData.alwaysOnForUsers;
					if (alwaysOnForUsers != null && alwaysOnForUsers.Count > 0)
					{
						flagValueByUser.AddOrUpdate(featureFlagData.name, featureFlagData.alwaysOnForUsers);
					}
				}
			}
			catch (Exception arg)
			{
				Debug.LogError($"Error parsing rollout feature flags: {arg}");
			}
			finally
			{
				ready = true;
			}
		}, delegate(PlayFabError e)
		{
			Debug.LogError("Error fetching rollout feature flags: " + e.ErrorMessage);
			ready = true;
		});
	}

	public bool IsEnabled(string flagName)
	{
		return IsEnabledForUser(flagName, PlayFabAuthenticator.instance.GetPlayFabPlayerId());
	}

	public bool IsEnabledForUser(string flagName, string playFabId)
	{
		bool flag = !logSent.Add((flagName, playFabId));
		if (flagValueByUser.TryGetValue(flagName, out var value) && value.Contains(playFabId))
		{
			return true;
		}
		bool value3;
		if (!flagValueByName.TryGetValue(flagName, out var value2))
		{
			return defaults.TryGetValue(flagName, out value3) && value3;
		}
		if (value2 <= 0)
		{
			return false;
		}
		if (value2 >= 100)
		{
			return true;
		}
		return XXHash32.Compute(Encoding.UTF8.GetBytes(playFabId)) % 100 < value2;
	}

	public bool IsEnabledForAnyone(string flagName)
	{
		if (flagValueByUser.TryGetValue(flagName, out var value) && value.Count > 0)
		{
			return true;
		}
		bool value3;
		if (!flagValueByName.TryGetValue(flagName, out var value2))
		{
			return defaults.TryGetValue(flagName, out value3) && value3;
		}
		return value2 > 0;
	}
}

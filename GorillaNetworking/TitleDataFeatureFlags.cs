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
		{ "2024-06-CosmeticsAuthenticationV2", true },
		{ "2025-04-CosmeticsAuthenticationV2-SetData", false },
		{ "2025-04-CosmeticsAuthenticationV2-ReadData", false },
		{ "2025-04-CosmeticsAuthenticationV2-Compat", true }
	};

	private Dictionary<string, int> flagValueByName = new Dictionary<string, int>();

	private Dictionary<string, List<string>> flagValueByUser = new Dictionary<string, List<string>>();

	private Dictionary<string, bool> logSent = new Dictionary<string, bool>();

	public bool ready { get; private set; }

	public void FetchFeatureFlags()
	{
		PlayFabTitleDataCache.Instance.GetTitleData(TitleDataKey, delegate(string json)
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
			ready = true;
		}, delegate(PlayFabError e)
		{
			Debug.LogError("Error fetching rollout feature flags: " + e.ErrorMessage);
			ready = true;
		});
	}

	public bool IsEnabledForUser(string flagName)
	{
		logSent.TryGetValue(flagName, out var _);
		logSent[flagName] = true;
		string playFabPlayerId = PlayFabAuthenticator.instance.GetPlayFabPlayerId();
		if (flagValueByUser.TryGetValue(flagName, out var value2) && value2 != null && value2.Contains(playFabPlayerId))
		{
			return true;
		}
		bool value4;
		if (!flagValueByName.TryGetValue(flagName, out var value3))
		{
			return defaults.TryGetValue(flagName, out value4) && value4;
		}
		if (value3 <= 0)
		{
			return false;
		}
		if (value3 >= 100)
		{
			return true;
		}
		return XXHash32.Compute(Encoding.UTF8.GetBytes(playFabPlayerId)) % 100 < value3;
	}
}

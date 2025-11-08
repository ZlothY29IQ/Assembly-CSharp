using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using PlayFab;
using UnityEngine;

namespace GorillaNetworking;

public class TitleDataFeatureFlags
{
	public string TitleDataKey = "DeployFeatureFlags";

	public Dictionary<string, bool> defaults = new Dictionary<string, bool>
	{
		{ "2024-05-ReturnCurrentVersionV2", true },
		{ "2024-05-ReturnMyOculusHashV2", true },
		{ "2024-05-TryDistributeCurrencyV2", true },
		{ "2024-05-AddOrRemoveDLCOwnershipV2", true },
		{ "2024-05-BroadcastMyRoomV2", true },
		{ "2024-06-CosmeticsAuthenticationV2", true },
		{ "2024-08-KIDIntegrationV1", true },
		{ "2025-04-CosmeticsAuthenticationV2-SetData", false },
		{ "2025-04-CosmeticsAuthenticationV2-ReadData", false },
		{ "2025-04-CosmeticsAuthenticationV2-Compat", true }
	};

	private Dictionary<string, int> flagValueByName = new Dictionary<string, int>();

	private Dictionary<string, List<string>> flagValueByUser = new Dictionary<string, List<string>>();

	public bool ready { get; private set; }

	public void FetchFeatureFlags()
	{
		PlayFabTitleDataCache.Instance.GetTitleData(TitleDataKey, delegate(string json)
		{
			FeatureFlagListData featureFlagListData = JsonUtility.FromJson<FeatureFlagListData>(json);
			FeatureFlagData[] flags = featureFlagListData.flags;
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
			Debug.Log($"GorillaServer: Fetched flags ({featureFlagListData})");
			ready = true;
		}, delegate(PlayFabError e)
		{
			Debug.LogError("Error fetching rollout feature flags: " + e.ErrorMessage);
			ready = true;
		});
	}

	public bool IsEnabledForUser(string flagName)
	{
		string playFabPlayerId = PlayFabAuthenticator.instance.GetPlayFabPlayerId();
		Debug.Log("GorillaServer: Checking flag " + flagName + " for " + playFabPlayerId + "\nFlag values:\n" + JsonConvert.SerializeObject(flagValueByName) + "\n\nDefaults:\n" + JsonConvert.SerializeObject(defaults));
		if (flagValueByUser.TryGetValue(flagName, out var value) && value != null && value.Contains(playFabPlayerId))
		{
			return true;
		}
		if (!flagValueByName.TryGetValue(flagName, out var value2))
		{
			Debug.Log("GorillaServer: Returning default");
			bool value3;
			return defaults.TryGetValue(flagName, out value3) && value3;
		}
		Debug.Log($"GorillaServer: Rollout % is {value2}");
		if (value2 <= 0)
		{
			Debug.Log("GorillaServer: " + flagName + " is off (<=0%).");
			return false;
		}
		if (value2 >= 100)
		{
			Debug.Log("GorillaServer: " + flagName + " is on (>=100%).");
			return true;
		}
		uint num = XXHash32.Compute(Encoding.UTF8.GetBytes(playFabPlayerId)) % 100;
		Debug.Log($"GorillaServer: Partial rollout, seed = {num} flag value = {num < value2}");
		return num < value2;
	}
}

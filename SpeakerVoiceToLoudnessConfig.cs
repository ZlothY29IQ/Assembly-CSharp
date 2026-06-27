using System;
using GorillaNetworking;
using PlayFab;
using UnityEngine;

internal static class SpeakerVoiceToLoudnessConfig
{
	[Serializable]
	private struct SerializedConfig
	{
		public bool EnableLoudnessLimit;

		public float LoudnessLimitThreshold;
	}

	private static SerializedConfig k_config = new SerializedConfig
	{
		EnableLoudnessLimit = true,
		LoudnessLimitThreshold = 0.5f
	};

	public static StaticArrayBag<float> StaticArrays = new StaticArrayBag<float>();

	private const string k_titleDataKey = "SpeakerVoiceToLoudnessConfig";

	public static bool EnableLoudnessLimit => k_config.EnableLoudnessLimit;

	public static float LoudnessLimitThreshold => k_config.LoudnessLimitThreshold;

	[RuntimeInitializeOnLoadMethod]
	private static void StaticLoad()
	{
		PlayFabTitleDataCache.RegisterOnLoad(OnTitleDataCacheReady);
	}

	private static void OnTitleDataCacheReady(PlayFabTitleDataCache titleDataCache)
	{
		titleDataCache.GetTitleData("SpeakerVoiceToLoudnessConfig", OnTitleDataCacheResponse, OnTitleDataCacheError);
	}

	private static void OnTitleDataCacheResponse(string json)
	{
		SerializedConfig serializedConfig = default(SerializedConfig);
		try
		{
			serializedConfig = JsonUtility.FromJson<SerializedConfig>(json);
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
			serializedConfig = k_config;
		}
		finally
		{
			k_config = serializedConfig;
		}
	}

	private static void OnTitleDataCacheError(PlayFabError errorMsg)
	{
	}
}

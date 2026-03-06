using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.CloudScriptModels;
using UnityEngine;

namespace GorillaNetworking;

public class GorillaServer : MonoBehaviour, ISerializationCallbackReceiver
{
	public static volatile GorillaServer Instance;

	public string FeatureFlagsTitleDataKey = "DeployFeatureFlags";

	public List<string> DefaultDeployFeatureFlagsEnabled = new List<string>();

	private TitleDataFeatureFlags featureFlags = new TitleDataFeatureFlags();

	private bool debug;

	private JsonSerializerSettings serializationSettings = new JsonSerializerSettings
	{
		NullValueHandling = NullValueHandling.Ignore,
		DefaultValueHandling = DefaultValueHandling.Ignore,
		MissingMemberHandling = MissingMemberHandling.Ignore,
		ObjectCreationHandling = ObjectCreationHandling.Replace,
		ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
		TypeNameHandling = TypeNameHandling.Auto
	};

	public bool FeatureFlagsReady => featureFlags.ready;

	private PlayFab.CloudScriptModels.EntityKey playerEntity => new PlayFab.CloudScriptModels.EntityKey
	{
		Id = PlayFabSettings.staticPlayer.EntityId,
		Type = PlayFabSettings.staticPlayer.EntityType
	};

	public void Start()
	{
		featureFlags.FetchFeatureFlags();
	}

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			UnityEngine.Object.Destroy(this);
		}
	}

	public void ReturnCurrentVersion(ReturnCurrentVersionRequest request, Action<ExecuteFunctionResult> successCallback, Action<PlayFabError> errorCallback)
	{
		successCallback = DebugWrapCb(successCallback, "ReturnCurrentVersion result");
		errorCallback = DebugWrapCb(errorCallback, "ReturnCurrentVersion error");
		PlayFabCloudScriptAPI.ExecuteFunction(new ExecuteFunctionRequest
		{
			Entity = playerEntity,
			FunctionName = "ReturnCurrentVersionV2",
			FunctionParameter = request
		}, successCallback, errorCallback);
	}

	public void TryDistributeCurrency(Action<ExecuteFunctionResult> successCallback, Action<PlayFabError> errorCallback)
	{
		successCallback = DebugWrapCb(successCallback, "TryDistributeCurrency result");
		errorCallback = DebugWrapCb(errorCallback, "TryDistributeCurrency error");
		PlayFabCloudScriptAPI.ExecuteFunction(new ExecuteFunctionRequest
		{
			Entity = playerEntity,
			FunctionName = "TryDistributeCurrencyV2",
			FunctionParameter = new { }
		}, successCallback, errorCallback);
	}

	public void AddOrRemoveDLCOwnership(Action<ExecuteFunctionResult> successCallback, Action<PlayFabError> errorCallback)
	{
		successCallback = DebugWrapCb(successCallback, "AddOrRemoveDLCOwnership result");
		errorCallback = DebugWrapCb(errorCallback, "AddOrRemoveDLCOwnership error");
		PlayFabCloudScriptAPI.ExecuteFunction(new ExecuteFunctionRequest
		{
			Entity = playerEntity,
			FunctionName = "AddOrRemoveDLCOwnershipV2",
			FunctionParameter = new { }
		}, successCallback, errorCallback);
	}

	public void BroadcastMyRoom(BroadcastMyRoomRequest request, Action<ExecuteFunctionResult> successCallback, Action<PlayFabError> errorCallback)
	{
		successCallback = DebugWrapCb(successCallback, "BroadcastMyRoom result");
		errorCallback = DebugWrapCb(errorCallback, "BroadcastMyRoom error");
		PlayFabCloudScriptAPI.ExecuteFunction(new ExecuteFunctionRequest
		{
			Entity = playerEntity,
			FunctionName = "BroadcastMyRoomV2",
			FunctionParameter = request
		}, successCallback, errorCallback);
	}

	public void UpdateUserCosmetics()
	{
		PlayFabCloudScriptAPI.ExecuteFunction(new ExecuteFunctionRequest
		{
			Entity = playerEntity,
			FunctionName = "UpdatePersonalCosmeticsList",
			FunctionParameter = new { },
			GeneratePlayStreamEvent = false
		}, delegate
		{
			if (CosmeticsController.instance != null)
			{
				CosmeticsController.instance.CheckCosmeticsSharedGroup();
			}
		}, delegate
		{
		});
	}

	public void GetAcceptedAgreements(GetAcceptedAgreementsRequest request, Action<Dictionary<string, string>> successCallback, Action<PlayFabError> errorCallback)
	{
		successCallback = DebugWrapCb(successCallback, "GetAcceptedAgreements result");
		errorCallback = DebugWrapCb(errorCallback, "GetAcceptedAgreements json error");
		PlayFabCloudScriptAPI.ExecuteFunction(new ExecuteFunctionRequest
		{
			Entity = playerEntity,
			FunctionName = "GetAcceptedAgreements",
			FunctionParameter = string.Join(",", request.AgreementKeys),
			GeneratePlayStreamEvent = false
		}, delegate(ExecuteFunctionResult result)
		{
			try
			{
				string value = Convert.ToString(result.FunctionResult);
				successCallback(JsonConvert.DeserializeObject<Dictionary<string, string>>(value));
			}
			catch (Exception arg)
			{
				errorCallback(new PlayFabError
				{
					ErrorMessage = $"Invalid format for GetAcceptedAgreements ({arg})",
					Error = PlayFabErrorCode.JsonParseError
				});
			}
		}, errorCallback);
	}

	public void SubmitAcceptedAgreements(SubmitAcceptedAgreementsRequest request, Action<ExecuteFunctionResult> successCallback, Action<PlayFabError> errorCallback)
	{
		successCallback = DebugWrapCb(successCallback, "SubmitAcceptedAgreements result");
		errorCallback = DebugWrapCb(errorCallback, "SubmitAcceptedAgreements error");
		PlayFabCloudScriptAPI.ExecuteFunction(new ExecuteFunctionRequest
		{
			Entity = playerEntity,
			FunctionName = "SubmitAcceptedAgreements",
			FunctionParameter = request.Agreements,
			GeneratePlayStreamEvent = false
		}, successCallback, errorCallback);
	}

	public void UploadGorillanalytics(object uploadData)
	{
		PlayFabCloudScriptAPI.ExecuteFunction(new ExecuteFunctionRequest
		{
			Entity = playerEntity,
			FunctionName = "Gorillanalytics",
			FunctionParameter = uploadData,
			GeneratePlayStreamEvent = false
		}, delegate
		{
		}, delegate
		{
		});
	}

	public void CheckForBadName(CheckForBadNameRequest request, Action<ExecuteFunctionResult> successCallback, Action<PlayFabError> errorCallback)
	{
		successCallback = DebugWrapCb(successCallback, "CheckForBadName result");
		errorCallback = DebugWrapCb(errorCallback, "CheckForBadName error");
		PlayFabCloudScriptAPI.ExecuteFunction(new ExecuteFunctionRequest
		{
			Entity = playerEntity,
			FunctionName = "CheckForBadName",
			FunctionParameter = new
			{
				name = request.name,
				forRoom = request.forRoom.ToString(),
				forTroop = request.forTroop.ToString()
			},
			GeneratePlayStreamEvent = false
		}, successCallback, errorCallback);
	}

	public void GetRandomName(Action<ExecuteFunctionResult> successCallback, Action<PlayFabError> errorCallback)
	{
		successCallback = DebugWrapCb(successCallback, "GetRandomName result");
		errorCallback = DebugWrapCb(errorCallback, "GetRandomName error");
		PlayFabCloudScriptAPI.ExecuteFunction(new ExecuteFunctionRequest
		{
			Entity = playerEntity,
			FunctionName = "GetRandomName",
			GeneratePlayStreamEvent = false
		}, successCallback, errorCallback);
	}

	public void ReturnQueueStats(ReturnQueueStatsRequest request, Action<ExecuteFunctionResult> successCallback, Action<PlayFabError> errorCallback)
	{
		successCallback = DebugWrapCb(successCallback, "ReturnQueueStats result");
		errorCallback = DebugWrapCb(errorCallback, "ReturnQueueStats error");
		PlayFabCloudScriptAPI.ExecuteFunction(new ExecuteFunctionRequest
		{
			Entity = playerEntity,
			FunctionName = "ReturnQueueStats",
			FunctionParameter = new
			{
				QueueName = request.queueName
			},
			GeneratePlayStreamEvent = false
		}, successCallback, errorCallback);
	}

	private Action<T> DebugWrapCb<T>(Action<T> cb, string label)
	{
		return delegate(T arg)
		{
			_ = debug;
			cb(arg);
		};
	}

	private ExecuteFunctionResult toFunctionResult(PlayFab.ClientModels.ExecuteCloudScriptResult csResult)
	{
		FunctionExecutionError error = null;
		if (csResult.Error != null)
		{
			error = new FunctionExecutionError
			{
				Error = csResult.Error.Error,
				Message = csResult.Error.Message,
				StackTrace = csResult.Error.StackTrace
			};
		}
		return new ExecuteFunctionResult
		{
			CustomData = csResult.CustomData,
			Error = error,
			ExecutionTimeMilliseconds = Convert.ToInt32(Math.Round(csResult.ExecutionTimeSeconds * 1000.0)),
			FunctionName = csResult.FunctionName,
			FunctionResult = csResult.FunctionResult,
			FunctionResultTooLarge = csResult.FunctionResultTooLarge
		};
	}

	public void OnBeforeSerialize()
	{
		FeatureFlagsTitleDataKey = featureFlags.TitleDataKey;
		DefaultDeployFeatureFlagsEnabled.Clear();
		foreach (KeyValuePair<string, bool> @default in featureFlags.defaults)
		{
			if (@default.Value)
			{
				DefaultDeployFeatureFlagsEnabled.Add(@default.Key);
			}
		}
	}

	public void OnAfterDeserialize()
	{
		featureFlags.TitleDataKey = FeatureFlagsTitleDataKey;
		foreach (string item in DefaultDeployFeatureFlagsEnabled)
		{
			featureFlags.defaults.AddOrUpdate(item, value: true);
		}
	}

	public bool CheckIsInKIDOptInCohort()
	{
		return featureFlags.IsEnabledForUser("2025-04-KIDOptIn");
	}

	public bool CheckIsInKIDRequiredCohort()
	{
		return featureFlags.IsEnabledForUser("2025-04-KIDRequired");
	}

	public bool CheckOptedInKID()
	{
		return KIDManager.HasOptedInToKID;
	}

	public bool CheckIsTZE_Enabled()
	{
		return featureFlags.IsEnabledForUser("2025-10-TelemetryZoneEventSampling");
	}

	public bool CheckIsMothershipTelemetryEnabled()
	{
		return featureFlags.IsEnabledForUser("2025-09-MothershipAnalyticsSampleRate");
	}

	public bool CheckIsPlayFabTelemetryEnabled()
	{
		return featureFlags.IsEnabledForUser("2025-09-PlayFabAnalyticsSampleRate");
	}
}

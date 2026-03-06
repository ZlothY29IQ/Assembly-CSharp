using System;
using System.Collections;
using GorillaNetworking;
using Oculus.Platform;
using Oculus.Platform.Models;
using UnityEngine;
using UnityEngine.Networking;

public class DeepLinkHandler : MonoBehaviour
{
	[Serializable]
	private class CollabRequest
	{
		public string itemGUID;

		public string launchSource;

		public string oculusUserID;

		public string playFabID;

		public string playFabSessionTicket;

		public string mothershipId;

		public string mothershipToken;

		public string mothershipEnvId;
	}

	public static volatile DeepLinkHandler instance;

	private LaunchDetails cachedLaunchDetails;

	private const string WitchbloodAppID = "7221491444554579";

	private readonly string[] WitchbloodCollabCosmeticID = new string[1] { "LMAKT." };

	private const string RaccoonLagoonAppID = "1903584373052985";

	private readonly string[] RaccoonLagoonCosmeticIDs = new string[2] { "LMALI.", "LHAGS." };

	private const string HiddenPathCollabEndpoint = "/api/ConsumeItem";

	public void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
		else if (instance != this)
		{
			UnityEngine.Object.Destroy(this);
		}
	}

	public static void Initialize(GameObject parent)
	{
		if (instance == null && parent != null)
		{
			parent.AddComponent<DeepLinkHandler>();
		}
		if (!(instance == null))
		{
			instance.RefreshLaunchDetails();
			if (instance.cachedLaunchDetails != null && instance.cachedLaunchDetails.LaunchType == LaunchType.Deeplink)
			{
				instance.HandleDeepLink();
			}
			else
			{
				UnityEngine.Object.Destroy(instance);
			}
		}
	}

	private void RefreshLaunchDetails()
	{
		if (UnityEngine.Application.platform != RuntimePlatform.Android)
		{
			GTDev.Log("[DeepLinkHandler::RefreshLaunchDetails] Not on Android Platform!");
			return;
		}
		cachedLaunchDetails = ApplicationLifecycle.GetLaunchDetails();
		GTDev.Log("[DeepLinkHandler::RefreshLaunchDetails] LaunchType: " + cachedLaunchDetails.LaunchType.ToString() + "\n[DeepLinkHandler::RefreshLaunchDetails] LaunchSource: " + cachedLaunchDetails.LaunchSource + "\n[DeepLinkHandler::RefreshLaunchDetails] DeepLinkMessage: " + cachedLaunchDetails.DeeplinkMessage);
	}

	private static IEnumerator ProcessWebRequest(string url, string data, string contentType, Action<UnityWebRequest> callback)
	{
		UnityWebRequest request = UnityWebRequest.Post(url, data, contentType);
		yield return request.SendWebRequest();
		callback(request);
	}

	private void HandleDeepLink()
	{
		GTDev.Log("[DeepLinkHandler::HandleDeepLink] Handling deep link...");
		if (cachedLaunchDetails.LaunchSource.Contains("7221491444554579"))
		{
			GTDev.Log("[DeepLinkHandler::HandleDeepLink] DeepLink received from Witchblood, processing...");
			string text = JsonUtility.ToJson(new CollabRequest
			{
				itemGUID = cachedLaunchDetails.DeeplinkMessage,
				launchSource = cachedLaunchDetails.LaunchSource,
				oculusUserID = PlayFabAuthenticator.instance.userID,
				playFabID = PlayFabAuthenticator.instance.GetPlayFabPlayerId(),
				playFabSessionTicket = PlayFabAuthenticator.instance.GetPlayFabSessionTicket(),
				mothershipId = MothershipClientContext.MothershipId,
				mothershipToken = MothershipClientContext.Token,
				mothershipEnvId = MothershipClientApiUnity.EnvironmentId
			});
			GTDev.Log("[DeepLinkHandler::HandleDeepLink] Web Request body: \n" + text);
			StartCoroutine(ProcessWebRequest(PlayFabAuthenticatorSettings.HpPromoApiBaseUrl + "/api/ConsumeItem", text, "application/json", OnWitchbloodCollabResponse));
		}
		else if (cachedLaunchDetails.LaunchSource.Contains("1903584373052985"))
		{
			GTDev.Log("[DeepLinkHandler::HandleDeepLink] DeepLink received from Racoon Lagoon, processing...");
			string text2 = JsonUtility.ToJson(new CollabRequest
			{
				itemGUID = cachedLaunchDetails.DeeplinkMessage,
				launchSource = cachedLaunchDetails.LaunchSource,
				oculusUserID = PlayFabAuthenticator.instance.userID,
				playFabID = PlayFabAuthenticator.instance.GetPlayFabPlayerId(),
				playFabSessionTicket = PlayFabAuthenticator.instance.GetPlayFabSessionTicket(),
				mothershipId = MothershipClientContext.MothershipId,
				mothershipToken = MothershipClientContext.Token,
				mothershipEnvId = MothershipClientApiUnity.EnvironmentId
			});
			GTDev.Log("[DeepLinkHandler::HandleDeepLink] Web Request body: \n" + text2);
			StartCoroutine(ProcessWebRequest(PlayFabAuthenticatorSettings.HpPromoApiBaseUrl + "/api/ConsumeItem", text2, "application/json", OnRaccoonLagoonCollabResponse));
		}
		else
		{
			GTDev.LogError("[DeepLinkHandler::HandleDeepLink] App launched via DeepLink, but from an unknown app. App ID: " + cachedLaunchDetails.LaunchSource);
			UnityEngine.Object.Destroy(this);
		}
	}

	private void OnWitchbloodCollabResponse(UnityWebRequest completedRequest)
	{
		if (completedRequest.result != UnityWebRequest.Result.Success)
		{
			GTDev.LogError("[DeepLinkHandler::OnWitchbloodCollabResponse] Web Request failed: " + completedRequest.error + "\nDetails: " + completedRequest.downloadHandler.text);
			UnityEngine.Object.Destroy(this);
		}
		else if (completedRequest.downloadHandler.text.Contains("AlreadyRedeemed", StringComparison.OrdinalIgnoreCase))
		{
			GTDev.Log("[DeepLinkHandler::OnWitchbloodCollabResponse] Item has already been redeemed!");
			UnityEngine.Object.Destroy(this);
		}
		else
		{
			GTDev.Log("[DeepLinkHandler::OnWitchbloodCollabResponse] Item successfully granted, processing external unlock...");
			StartCoroutine(CheckProcessExternalUnlock(WitchbloodCollabCosmeticID, autoEquip: true, isLeftHand: true, destroyOnFinish: true));
		}
	}

	private void OnRaccoonLagoonCollabResponse(UnityWebRequest completedRequest)
	{
		if (completedRequest.result != UnityWebRequest.Result.Success)
		{
			GTDev.LogError("[DeepLinkHandler::OnRaccoonLagoonCollabResponse] Web Request failed: " + completedRequest.error + "\nDetails: " + completedRequest.downloadHandler.text);
			UnityEngine.Object.Destroy(this);
		}
		else if (completedRequest.downloadHandler.text.Contains("AlreadyRedeemed", StringComparison.OrdinalIgnoreCase))
		{
			GTDev.Log("[DeepLinkHandler::OnRaccoonLagoonCollabResponse] Item has already been redeemed!");
			UnityEngine.Object.Destroy(this);
		}
		else
		{
			GTDev.Log("[DeepLinkHandler::OnRaccoonLagoonCollabResponse] Item successfully granted, processing external unlock...");
			StartCoroutine(CheckProcessExternalUnlock(RaccoonLagoonCosmeticIDs, autoEquip: true, isLeftHand: true, destroyOnFinish: true));
		}
	}

	private IEnumerator CheckProcessExternalUnlock(string[] itemIDs, bool autoEquip, bool isLeftHand, bool destroyOnFinish)
	{
		GTDev.Log("[DeepLinkHandler::CheckProcessExternalUnlock] Cosmetics initialized, proceeding to process external unlock...");
		foreach (string itemID in itemIDs)
		{
			CosmeticsController.instance.ProcessExternalUnlock(itemID, autoEquip, isLeftHand);
		}
		if (destroyOnFinish)
		{
			UnityEngine.Object.Destroy(this);
		}
		yield return null;
	}
}

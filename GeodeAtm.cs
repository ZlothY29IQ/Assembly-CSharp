using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Oculus.Platform;
using Oculus.Platform.Models;
using Steamworks;
using TMPro;
using UnityEngine;

public class GeodeAtm : MonoBehaviour
{
	private enum GeodePurchaseSize
	{
		SMALL,
		MEDIUM,
		LARGE
	}

	private const string GEODE_ATM_PREFIX = "GEODE_ATM_";

	private const string BALANCE_KEY = "GEODE_ATM_BALANCE";

	private const string BALANCE_LOADING_KEY = "GEODE_ATM_BALANCE_LOADING";

	private const string BALANCE_ERROR_KEY = "GEODE_ATM_BALANCE_ERROR";

	private const string PURCHASE_IN_PROGRESS_KEY = "GEODE_ATM_PURCHASE_IN_PROGRESS";

	private const string PURCHASE_CANCELLED_KEY = "GEODE_ATM_PURCHASE_CANCELLED";

	private const string PURCHASE_ERROR_KEY = "GEODE_ATM_PURCHASE_ERROR";

	private const string PURCHASE_FINALIZE_ERROR_KEY = "GEODE_ATM_PURCHASE_FINALIZE_ERROR";

	private const string PURCHASE_META_ERROR_KEY = "GEODE_ATM_PURCHASE_META_ERROR";

	public static bool ProcessingGeodePurchase;

	[SerializeField]
	private List<TextMeshPro> StatusTexts;

	[Header("Global Storefront Info")]
	[SerializeField]
	private string MothershipGeodeOfferDisplayIdLive;

	[SerializeField]
	private string MothershipGeodeOfferDisplayIdDev;

	[Header("Small Geode Offer")]
	[SerializeField]
	private string QuestSmallSku;

	[SerializeField]
	private string RiftSmallSku;

	[SerializeField]
	private string MothershipSteamSmallGeodeOfferIdLive;

	[SerializeField]
	private string MothershipSteamSmallGeodeOfferIdDev;

	[SerializeField]
	private int MothershipSmallSteamGeodeOfferDisplayIndexLive;

	[SerializeField]
	private int MothershipSmallSteamGeodeOfferDisplayIndexDev;

	[Header("Medium Geode Offer")]
	[SerializeField]
	private string QuestMediumSku;

	[SerializeField]
	private string RiftMediumSku;

	[SerializeField]
	private string MothershipSteamMediumGeodeOfferIdLive;

	[SerializeField]
	private string MothershipSteamMediumGeodeOfferIdDev;

	[SerializeField]
	private int MothershipMediumSteamGeodeOfferDisplayIndexLive;

	[SerializeField]
	private int MothershipMediumSteamGeodeOfferDisplayIndexDev;

	[Header("Large Geode Offer")]
	[SerializeField]
	private string QuestLargeSku;

	[SerializeField]
	private string RiftLargeSku;

	[SerializeField]
	private string MothershipSteamLargeGeodeOfferIdLive;

	[SerializeField]
	private string MothershipSteamLargeGeodeOfferIdDev;

	[SerializeField]
	private int MothershipLargeSteamGeodeOfferDisplayIndexLive;

	[SerializeField]
	private int MothershipLargeSteamGeodeOfferDisplayIndexDev;

	private static bool fetchedGeodes;

	private static int geodes;

	private string steamOrderId = "";

	private Callback<MicroTxnAuthorizationResponse_t> _steamMicroTransactionAuthorizationResponse;

	private bool purchaseInFlight;

	private string GetMetaSku(GeodePurchaseSize size)
	{
		string text = "";
		return size switch
		{
			_ => text, 
		};
	}

	private static string GetLocalizedText(string key, string fallback)
	{
		if (!LocalisationManager.TryGetKeyForCurrentLocale(key, out var result, fallback))
		{
			Debug.LogError("[LOCALIZATION::GEODE_ATM] Failed to get key for Geode ATM localization [" + key + "]");
		}
		return result;
	}

	private void UpdateStatusText(string text)
	{
		string text2 = "";
		text2 = ((!fetchedGeodes) ? GetLocalizedText("GEODE_ATM_BALANCE_LOADING", "Loading Geodes Balance...") : GetLocalizedText("GEODE_ATM_BALANCE", "Current Geodes Balance: {balance}").Replace("{balance}", geodes.ToString()));
		text2 = text2 + "\n" + text;
		foreach (TextMeshPro statusText in StatusTexts)
		{
			statusText.text = text2;
		}
	}

	private void ProcessSteamCallback(MicroTxnAuthorizationResponse_t callBackResponse)
	{
		if (callBackResponse.m_bAuthorized == 0)
		{
			Debug.Log("The user did not authorize the steam geodes purchase.");
			UpdateStatusText(GetLocalizedText("GEODE_ATM_PURCHASE_CANCELLED", "The purchase could not continue because it was cancelled. Please try again."));
			purchaseInFlight = false;
			fetchedGeodes = false;
			return;
		}
		if (steamOrderId.IsNullOrEmpty())
		{
			steamOrderId = callBackResponse.m_ulOrderID.ToString();
		}
		MothershipClientApiUnity.FinalizeSteamPurchase(callBackResponse.m_ulOrderID.ToString(), delegate
		{
			ProcessingGeodePurchase = false;
			fetchedGeodes = false;
			purchaseInFlight = false;
			RefreshGeodeBalance();
		}, delegate(MothershipError Error, int Status)
		{
			ProcessingGeodePurchase = false;
			fetchedGeodes = false;
			purchaseInFlight = false;
			Debug.LogError("Geodes ATM could not finalzie STEAM iap. Trace ID: " + Error.TraceId + ", Error Code: " + Error.MothershipErrorCode);
			UpdateStatusText(GetLocalizedText("GEODE_ATM_PURCHASE_FINALIZE_ERROR", "An unexpected error occurred while finalizing this purchase. Trace ID: {traceId}, Error Code: {errorCode}, Session ID: {sessionId} Refreshing current Geodes Balance...").Replace("{traceId}", Error.TraceId ?? "").Replace("{errorCode}", Error.MothershipErrorCode ?? "").Replace("{sessionId}", MothershipClientApiUnity.SessionId ?? ""));
			RefreshGeodeBalance();
		});
	}

	private void OnMetaPurchaseComplete(Message<Purchase> msg)
	{
		purchaseInFlight = false;
		if (msg.IsError)
		{
			Error error = msg.GetError();
			Debug.Log($"Meta Geodes Failure: Error Code: {error?.Code}, HTTP Code: {error?.HttpCode}, Message: {error?.Message}");
			fetchedGeodes = false;
			UpdateStatusText(GetLocalizedText("GEODE_ATM_PURCHASE_META_ERROR", "This purchase could not continue because something went wrong processing the transaction with Meta. Was the transaction cancelled? Please try again."));
			RefreshGeodeBalance();
			return;
		}
		MothershipClientApiUnity.RefreshMetaIAP(delegate
		{
			fetchedGeodes = false;
			RefreshGeodeBalance();
		}, delegate(MothershipError Error, int StatusCode)
		{
			fetchedGeodes = false;
			RefreshGeodeBalance();
			Debug.Log("Something went wrong refreshing meta iap with Mothership " + Error.Message + " " + Error.MothershipErrorCode + " Trace ID: " + Error.TraceId + " Session ID: " + MothershipClientApiUnity.SessionId);
		});
	}

	private async Task StartPurchase(GeodePurchaseSize size)
	{
		if (SteamManager.Initialized && _steamMicroTransactionAuthorizationResponse == null)
		{
			_steamMicroTransactionAuthorizationResponse = Callback<MicroTxnAuthorizationResponse_t>.Create(ProcessSteamCallback);
		}
		Debug.Log("Starting Steam Geodes Purchase");
		UpdateStatusText(GetLocalizedText("GEODE_ATM_PURCHASE_IN_PROGRESS", "Geodes Purchase in Progress"));
		ProcessingGeodePurchase = true;
		string offerId = "";
		int displayIndex = -1;
		string mothershipGeodeOfferDisplayIdLive = MothershipGeodeOfferDisplayIdLive;
		switch (size)
		{
		case GeodePurchaseSize.SMALL:
			offerId = MothershipSteamSmallGeodeOfferIdLive;
			displayIndex = MothershipSmallSteamGeodeOfferDisplayIndexLive;
			break;
		case GeodePurchaseSize.MEDIUM:
			offerId = MothershipSteamMediumGeodeOfferIdLive;
			displayIndex = MothershipMediumSteamGeodeOfferDisplayIndexLive;
			break;
		case GeodePurchaseSize.LARGE:
			offerId = MothershipSteamLargeGeodeOfferIdDev;
			displayIndex = MothershipLargeSteamGeodeOfferDisplayIndexLive;
			break;
		}
		MothershipClientApiUnity.InitSteamPurchase(mothershipGeodeOfferDisplayIdLive, offerId, displayIndex, delegate(InitSteamPurchaseResponse Response)
		{
			steamOrderId = Response.SteamOrderId;
		}, delegate(MothershipError Error, int StatusCode)
		{
			UpdateStatusText(GetLocalizedText("GEODE_ATM_PURCHASE_ERROR", "Something went wrong trying to purchase Geodes: {errorCode} Trace ID: {traceId} Session ID: {sessionId}").Replace("{errorCode}", Error.MothershipErrorCode ?? "").Replace("{traceId}", Error.TraceId ?? "").Replace("{sessionId}", MothershipClientApiUnity.SessionId ?? ""));
			Debug.Log("Something went wrong trying to purchase Geodes on Stea, " + Error.Message + " " + Error.MothershipErrorCode + " Trace ID: " + Error.TraceId + " Session ID: " + MothershipClientApiUnity.SessionId);
		});
	}

	public void SmallPurchase()
	{
		if (!purchaseInFlight)
		{
			purchaseInFlight = true;
			StartPurchase(GeodePurchaseSize.SMALL);
		}
	}

	public void MediumPurchase()
	{
		if (!purchaseInFlight)
		{
			purchaseInFlight = true;
			StartPurchase(GeodePurchaseSize.MEDIUM);
		}
	}

	public void LargePurchase()
	{
		if (!purchaseInFlight)
		{
			purchaseInFlight = true;
			StartPurchase(GeodePurchaseSize.LARGE);
		}
	}

	public void RefreshGeodeBalance()
	{
		if (fetchedGeodes)
		{
			UpdateStatusText("");
			return;
		}
		MothershipClientApiUnity.GetUserInventory(delegate(MothershipGetInventoryResponse Result)
		{
			geodes = 0;
			fetchedGeodes = true;
			foreach (KeyValuePair<string, MothershipPlayerInventorySummary> result in Result.Results)
			{
				foreach (MothershipInventoryItemSummary entitlement in result.Value.entitlements)
				{
					if (entitlement.in_game_id.Equals("geodes", StringComparison.OrdinalIgnoreCase))
					{
						geodes += entitlement.quantity;
					}
				}
			}
			UpdateStatusText("");
		}, delegate(MothershipError Error, int StatusCode)
		{
			fetchedGeodes = false;
			UpdateStatusText(GetLocalizedText("GEODE_ATM_BALANCE_ERROR", "Something went wrong getting current Geodes balance: {errorCode} Trace ID: {traceId} Session ID: {sessionId}").Replace("{errorCode}", Error.MothershipErrorCode ?? "").Replace("{traceId}", Error.TraceId ?? "").Replace("{sessionId}", MothershipClientApiUnity.SessionId ?? ""));
			Debug.Log("Something went wrong refreshing inventory for geodes " + Error.Message + " " + Error.MothershipErrorCode + " Trace ID: " + Error.TraceId + " Session ID: " + MothershipClientApiUnity.SessionId);
		});
	}
}

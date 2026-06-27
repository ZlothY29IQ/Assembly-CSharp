using System;
using System.Collections.Generic;
using GorillaNetworking;
using Oculus.Platform;
using Oculus.Platform.Models;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.Video;

namespace GorillaTagScripts.Subscription;

public class SubscriptionKiosk : MonoBehaviour, ITouchScreenStation, IGorillaSliceableSimple
{
	private enum ScreenState
	{
		SafeAccount,
		WaitingForScan,
		Scanning,
		SubscriptionStatusUnknown,
		MainMenuSubscribed,
		MainMenuUnsubscribed,
		SubscriptionData,
		PurchaseSubscription,
		SubscriptionPurchaseInProgress,
		SubscriptionPurchaseResult,
		FeatureToggles,
		SubscriptionSteamWarning,
		None
	}

	private enum PurchaseResult
	{
		Success,
		Failure,
		Cancel
	}

	private const string SUBSCRIPTION_KIOSK_PREFIX = "SUBKIOSK";

	private const string PURCHASE_SUCCESS_KEY = "SUBKIOSKPURCHASE_SUCCESS";

	private const string PURCHASE_CANCEL_KEY = "SUBKIOSKPURCHASE_CANCEL";

	private const string PURCHASE_FAIL_KEY = "SUBKIOSKPURCHASE_FAIL";

	private const string subSKU = "fan_club";

	[SerializeField]
	private VideoPlayer subsVideoPlayer;

	[SerializeField]
	private ObservableBehavior subsVideoObservable;

	[SerializeField]
	private VideoClip defaultVideoClip;

	[SerializeField]
	private VideoClip steamSubsVideoClip;

	[SerializeField]
	private ObservableBehaviorRule defaultObservableRule;

	[SerializeField]
	private ObservableBehaviorRule steamObservableRule;

	[Space]
	[SerializeField]
	private GameObject steamComingSoon;

	[SerializeField]
	private GameObject safeAccountScreen;

	[SerializeField]
	private GameObject waitingForScanScreen;

	[SerializeField]
	private GameObject scanningScreen;

	[SerializeField]
	private GameObject subStatusUnknownScreen;

	[SerializeField]
	private GameObject mainMenuSubscribedScreen;

	[Space]
	[SerializeField]
	private GameObject mainMenuUnsubscribedScreen;

	[SerializeField]
	private GameObject mainMenuUnsubscribedQuestText;

	[SerializeField]
	private GameObject mainMenuUnsubscribedSteamText;

	[Space]
	[SerializeField]
	private GameObject subDataScreen;

	[SerializeField]
	private GameObject purchaseSubScreen;

	[SerializeField]
	private GameObject purchaseProgressScreen;

	[SerializeField]
	private GameObject purchaseResultScreen;

	[SerializeField]
	private GameObject featureTogglesScreen;

	private List<SITouchscreenButtonContainer> toggleButtonContainers;

	private Dictionary<ScreenState, GameObject> screensByState;

	private string steamOrderId = "";

	[SerializeField]
	private TextMeshPro subMenuPlayerName;

	[SerializeField]
	private TextMeshPro subMenuDaysAccrued;

	[SerializeField]
	private TextMeshPro unsubscribedMenuPlayerName;

	[SerializeField]
	private TextMeshPro subDataPlayerName;

	[SerializeField]
	private TextMeshPro subDataDaysAccrued;

	[SerializeField]
	private TextMeshPro subDataDaysRemaining;

	[SerializeField]
	private TextMeshPro subDataAutoRenew;

	[SerializeField]
	private TextMeshPro subDataRenewDate;

	[SerializeField]
	private TextMeshPro subDataSubscriptionTerm;

	[SerializeField]
	private GameObject subDataSubscribeButton;

	[SerializeField]
	private TextMeshPro purchaseResultText;

	private ScreenState currentState = ScreenState.WaitingForScan;

	private ScreenState lastState;

	private PurchaseResult lastPurchase;

	private Callback<MicroTxnAuthorizationResponse_t> _steamMicroTransactionAuthorizationResponse;

	public static bool ProcessingSubscriptionPurchase { get; set; }

	public SIScreenRegion ScreenRegion { get; }

	private void Awake()
	{
		toggleButtonContainers = new List<SITouchscreenButtonContainer>(GetComponentsInChildren<SITouchscreenButtonContainer>(includeInactive: true));
		for (int num = toggleButtonContainers.Count - 1; num >= 0; num--)
		{
			if (toggleButtonContainers[num].button.buttonMode != SITouchscreenButton.ButtonMode.Toggle)
			{
				toggleButtonContainers.RemoveAt(num);
			}
		}
		screensByState = new Dictionary<ScreenState, GameObject>();
		screensByState.Add(ScreenState.SafeAccount, safeAccountScreen);
		screensByState.Add(ScreenState.WaitingForScan, waitingForScanScreen);
		screensByState.Add(ScreenState.Scanning, scanningScreen);
		screensByState.Add(ScreenState.SubscriptionStatusUnknown, subStatusUnknownScreen);
		screensByState.Add(ScreenState.MainMenuSubscribed, mainMenuSubscribedScreen);
		screensByState.Add(ScreenState.MainMenuUnsubscribed, mainMenuUnsubscribedScreen);
		screensByState.Add(ScreenState.SubscriptionData, subDataScreen);
		screensByState.Add(ScreenState.PurchaseSubscription, purchaseSubScreen);
		screensByState.Add(ScreenState.SubscriptionPurchaseInProgress, purchaseProgressScreen);
		screensByState.Add(ScreenState.SubscriptionPurchaseResult, purchaseResultScreen);
		screensByState.Add(ScreenState.FeatureToggles, featureTogglesScreen);
		screensByState.Add(ScreenState.SubscriptionSteamWarning, steamComingSoon);
	}

	private void OnEnable()
	{
		if (PlayFabAuthenticator.instance.GetSafety())
		{
			UpdateState(ScreenState.SafeAccount);
			UnityEngine.Object.Destroy(this);
			return;
		}
		UpdateState(ScreenState.WaitingForScan);
		subsVideoPlayer.clip = defaultVideoClip;
		GorillaSlicerSimpleManager.RegisterSliceable(this);
		SubscriptionManager.OnLocalSubscriptionData = (Action)Delegate.Combine(SubscriptionManager.OnLocalSubscriptionData, new Action(LocalSubscriptionDataUpdated));
	}

	private void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this);
		SubscriptionManager.OnLocalSubscriptionData = (Action)Delegate.Remove(SubscriptionManager.OnLocalSubscriptionData, new Action(LocalSubscriptionDataUpdated));
		_steamMicroTransactionAuthorizationResponse?.Unregister();
	}

	public void HandScanAborted()
	{
		if (currentState == ScreenState.Scanning)
		{
			UpdateState(ScreenState.WaitingForScan);
		}
	}

	public void KioskAbandoned()
	{
		UpdateState(ScreenState.WaitingForScan);
	}

	public void HandScanStarted()
	{
		if (currentState == ScreenState.WaitingForScan)
		{
			UpdateState(ScreenState.Scanning);
		}
	}

	public void HandScanned()
	{
		if (!PlayFabAuthenticator.instance.GetSafety())
		{
			switch (SubscriptionManager.LocalSubscriptionStatus())
			{
			case SubscriptionManager.SubscriptionStatus.Active:
				UpdateState(ScreenState.MainMenuSubscribed);
				break;
			case SubscriptionManager.SubscriptionStatus.Inactive:
				UpdateState(ScreenState.MainMenuUnsubscribed);
				break;
			default:
				UpdateState(ScreenState.SubscriptionStatusUnknown);
				break;
			}
		}
	}

	private void UpdateState(ScreenState newState)
	{
		lastState = currentState;
		currentState = newState;
		if (lastState == currentState)
		{
			return;
		}
		ActivateScreen(currentState);
		switch (currentState)
		{
		case ScreenState.MainMenuSubscribed:
			UpdateSubscribedMenu();
			break;
		case ScreenState.MainMenuUnsubscribed:
			UpdateUnsubscribedMenu();
			break;
		case ScreenState.SubscriptionData:
			UpdateSubscriptionData();
			break;
		case ScreenState.FeatureToggles:
		{
			FeatureTogglesScreen component = screensByState[ScreenState.FeatureToggles].GetComponent<FeatureTogglesScreen>();
			if ((object)component != null)
			{
				component.enabled = true;
				component.MarkDirty();
			}
			break;
		}
		case ScreenState.WaitingForScan:
		case ScreenState.Scanning:
		case ScreenState.SubscriptionStatusUnknown:
		case ScreenState.PurchaseSubscription:
		case ScreenState.SubscriptionPurchaseInProgress:
		case ScreenState.SubscriptionPurchaseResult:
			break;
		}
	}

	private void ActivateScreen(ScreenState activeScreen)
	{
		foreach (KeyValuePair<ScreenState, GameObject> item in screensByState)
		{
			item.Value.SetActive(item.Key == activeScreen);
		}
	}

	public void AddButton(SITouchscreenButton button, bool isPopupButton = false)
	{
	}

	public void TouchscreenButtonPressed(SITouchscreenButton.SITouchscreenButtonType buttonType, int data, int actorNr)
	{
		if (actorNr != NetworkSystem.Instance.LocalPlayer.ActorNumber)
		{
			return;
		}
		switch (currentState)
		{
		case ScreenState.MainMenuSubscribed:
			switch (buttonType)
			{
			case SITouchscreenButton.SITouchscreenButtonType.Help:
				UpdateState(ScreenState.SubscriptionData);
				break;
			case SITouchscreenButton.SITouchscreenButtonType.PageSelect:
				UpdateState(ScreenState.FeatureToggles);
				break;
			}
			break;
		case ScreenState.MainMenuUnsubscribed:
			if (buttonType == SITouchscreenButton.SITouchscreenButtonType.Subscribe)
			{
				UpdateState(ScreenState.SubscriptionSteamWarning);
				subsVideoPlayer.clip = steamSubsVideoClip;
				subsVideoObservable.ObservableBehaviorRule = steamObservableRule;
			}
			break;
		case ScreenState.SubscriptionSteamWarning:
			if (buttonType == SITouchscreenButton.SITouchscreenButtonType.Subscribe)
			{
				UpdateState(ScreenState.PurchaseSubscription);
			}
			break;
		case ScreenState.SubscriptionData:
			switch (buttonType)
			{
			case SITouchscreenButton.SITouchscreenButtonType.Back:
				HandScanned();
				break;
			case SITouchscreenButton.SITouchscreenButtonType.Subscribe:
				UpdateState(ScreenState.PurchaseSubscription);
				break;
			}
			break;
		case ScreenState.PurchaseSubscription:
			switch (buttonType)
			{
			case SITouchscreenButton.SITouchscreenButtonType.Subscribe:
				PurchaseSubscription((SubscriptionManager.SubscriptionTerm)data);
				break;
			case SITouchscreenButton.SITouchscreenButtonType.Back:
				HandScanned();
				break;
			}
			subsVideoPlayer.clip = defaultVideoClip;
			subsVideoObservable.ObservableBehaviorRule = defaultObservableRule;
			break;
		case ScreenState.SubscriptionPurchaseResult:
			if (buttonType == SITouchscreenButton.SITouchscreenButtonType.Confirm)
			{
				HandScanned();
			}
			break;
		case ScreenState.SubscriptionPurchaseInProgress:
		case ScreenState.FeatureToggles:
			break;
		}
	}

	public void TouchscreenToggleButtonPressed(SITouchscreenButton.SITouchscreenButtonType buttonType, int data, int actorNr, bool isToggledOn)
	{
		_ = NetworkSystem.Instance.LocalPlayer.ActorNumber;
	}

	public void OnToggleFeaturesExitButtonPressed(SITouchscreenButton.SITouchscreenButtonType buttonType, int data, int actorNr)
	{
		if (actorNr == NetworkSystem.Instance.LocalPlayer.ActorNumber)
		{
			UpdateState(ScreenState.MainMenuSubscribed);
		}
	}

	private void UpdateToggleButtonState(int buttonData, bool state)
	{
		foreach (SITouchscreenButtonContainer toggleButtonContainer in toggleButtonContainers)
		{
			if (toggleButtonContainer.button.data == buttonData)
			{
				toggleButtonContainer.button.SetToggleState(state, invokeEvent: true);
				break;
			}
		}
	}

	private bool GetSubscriptionFeatureState(int buttonData)
	{
		switch (buttonData)
		{
		case 0:
			return SubscriptionManager.GetSubscriptionSettingBool(SubscriptionManager.SubscriptionFeatures.GoldenName);
		case 1:
			return SubscriptionManager.GetSubscriptionSettingBool(SubscriptionManager.SubscriptionFeatures.IOBT);
		case 2:
			return SubscriptionManager.GetSubscriptionSettingBool(SubscriptionManager.SubscriptionFeatures.HandTracking);
		default:
			Debug.Log($"Getting current state for subscription kiosk {buttonData}");
			return false;
		}
	}

	public void UpdateGoldNameTag(bool state)
	{
		ToggleSubscriptionSettingValue(SubscriptionManager.SubscriptionFeatures.GoldenName, state);
		VRRig.LocalRig.OnSubscriptionData();
		if (GorillaScoreboardTotalUpdater.instance != null)
		{
			GorillaScoreboardTotalUpdater.instance.UpdateActiveScoreboards();
		}
	}

	public void UpdateIOTBExperimentalFeature(bool state)
	{
		ToggleSubscriptionSettingValue(SubscriptionManager.SubscriptionFeatures.IOBT, state);
		if (GorillaIK.playerIK != null)
		{
			GorillaIK.playerIK.ResetIKData();
			GorillaIK.playerIK.usingUpdatedIK = state;
		}
	}

	public void UpdateHandTrackingExperimentalFeature(bool state)
	{
		ToggleSubscriptionSettingValue(SubscriptionManager.SubscriptionFeatures.HandTracking, state);
	}

	private void ToggleSubscriptionSettingValue(SubscriptionManager.SubscriptionFeatures feature, bool state)
	{
		SubscriptionManager.SetSubscriptionSettingValue(feature, state ? 1 : 0);
	}

	private void UpdateSubscribedMenu()
	{
		subMenuPlayerName.text = NetworkSystem.Instance.LocalPlayer.SanitizedNickName;
		subMenuDaysAccrued.text = SubscriptionManager.GetSubscriptionDetails().daysAccrued.ToString();
		foreach (SITouchscreenButtonContainer toggleButtonContainer in toggleButtonContainers)
		{
			UpdateToggleButtonState(toggleButtonContainer.data, GetSubscriptionFeatureState(toggleButtonContainer.data));
		}
	}

	private void UpdateUnsubscribedMenu()
	{
		unsubscribedMenuPlayerName.text = NetworkSystem.Instance.LocalPlayer.SanitizedNickName;
		mainMenuUnsubscribedQuestText.SetActive(value: false);
		mainMenuUnsubscribedSteamText.SetActive(value: true);
		subsVideoPlayer.clip = defaultVideoClip;
		subsVideoObservable.ObservableBehaviorRule = defaultObservableRule;
	}

	private void UpdateSubscriptionData()
	{
		SubscriptionManager.SubscriptionDetails subscriptionDetails = SubscriptionManager.GetSubscriptionDetails();
		subDataPlayerName.text = NetworkSystem.Instance.LocalPlayer.SanitizedNickName;
		subDataDaysAccrued.text = subscriptionDetails.daysAccrued.ToString();
		subDataDaysRemaining.text = Mathf.RoundToInt((float)(subscriptionDetails.subscriptionActiveUntilDate - DateTime.UtcNow).TotalDays).ToString();
		subDataAutoRenew.text = (subscriptionDetails.autoRenew ? "ENABLED" : "DISABLED");
		subDataRenewDate.text = subscriptionDetails.subscriptionActiveUntilDate.ToString("MMM d, yyyy").ToUpper();
		subDataSubscriptionTerm.text = subscriptionDetails.autoRenewMonths + " MONTH" + ((subscriptionDetails.autoRenewMonths > 1) ? "S" : "");
		if (subDataSubscribeButton.activeSelf == subscriptionDetails.autoRenew)
		{
			subDataSubscribeButton.SetActive(!subscriptionDetails.autoRenew);
		}
	}

	private void UpdatePurchaseResultScreen(PurchaseResult result)
	{
		lastPurchase = result;
		string result2 = "";
		switch (result)
		{
		case PurchaseResult.Success:
			result2 = "SUBSCRIPTION SUCCESSFUL! WELCOME TO THE FAN CLUB, YOU ARE NOW A VERY IMPORTANT MONKE (V.I.M.)!";
			LocalisationManager.TryGetKeyForCurrentLocale("SUBKIOSKPURCHASE_SUCCESS", out result2, result2);
			break;
		case PurchaseResult.Failure:
			result2 = "PURCHASE FAILED! WE'RE NOT SURE WHAT HAPPENED, BUT PLEASE CHECK YOUR INFORMATION, OR TRY AGAIN LATER. IF IT LOOKED LIKE THE PURCHASE SHOULD HAVE SUCCEEDED, TRY RESTARTING THE GAME.";
			LocalisationManager.TryGetKeyForCurrentLocale("SUBKIOSKPURCHASE_FAIL", out result2, result2);
			break;
		case PurchaseResult.Cancel:
			result2 = "PURCHASE CANCELED! WE'LL BE HERE IF YOU CHANGE YOUR MIND!";
			LocalisationManager.TryGetKeyForCurrentLocale("SUBKIOSKPURCHASE_CANCEL", out result2, result2);
			break;
		}
		purchaseResultText.text = result2;
	}

	private void ProcessSteamCallback(MicroTxnAuthorizationResponse_t callBackResponse)
	{
		if (callBackResponse.m_bAuthorized == 0)
		{
			Debug.Log("The user did not authorize the steam subscription purchase");
			UpdatePurchaseResultScreen(PurchaseResult.Cancel);
			UpdateState(ScreenState.SubscriptionPurchaseResult);
		}
		MothershipClientApiUnity.FinalizeSteamSubscriptionTransaction(steamOrderId, delegate
		{
			ProcessingSubscriptionPurchase = false;
			UpdatePurchaseResultScreen(PurchaseResult.Success);
			UpdateState(ScreenState.SubscriptionPurchaseResult);
			SubscriptionManager.InitializePersonalSubscriptionData();
		}, delegate(MothershipError Error, int Status)
		{
			ProcessingSubscriptionPurchase = false;
			UpdatePurchaseResultScreen(PurchaseResult.Failure);
			UpdateState(ScreenState.SubscriptionPurchaseResult);
			Debug.LogError("SubscriptionKiosk could not finalzie STEAM iap. Trace ID " + Error.TraceId + ", Error Code: " + Error.MothershipErrorCode);
		});
	}

	private void PurchaseSubscription(SubscriptionManager.SubscriptionTerm subTerm)
	{
		if (SteamManager.Initialized && _steamMicroTransactionAuthorizationResponse == null)
		{
			_steamMicroTransactionAuthorizationResponse = Callback<MicroTxnAuthorizationResponse_t>.Create(ProcessSteamCallback);
		}
		Debug.Log("Starting Steam Subscription Purchase");
		int frequency = 1;
		int priceInUSDCents = 999;
		string frequencyUnit = "Month";
		switch (subTerm)
		{
		case SubscriptionManager.SubscriptionTerm.MONTHLY:
			frequency = 1;
			priceInUSDCents = 999;
			frequencyUnit = "Month";
			break;
		case SubscriptionManager.SubscriptionTerm.QUARTERLY:
			frequency = 3;
			priceInUSDCents = 2699;
			frequencyUnit = "Month";
			break;
		case SubscriptionManager.SubscriptionTerm.SEMIANNUAL:
			frequency = 6;
			priceInUSDCents = 4999;
			frequencyUnit = "Month";
			break;
		case SubscriptionManager.SubscriptionTerm.ANNUAL:
			frequency = 1;
			priceInUSDCents = 9499;
			frequencyUnit = "Year";
			break;
		}
		ProcessingSubscriptionPurchase = true;
		MothershipClientApiUnity.InitSteamSubscriptionTransaction("40494", frequencyUnit, frequency, priceInUSDCents, delegate(InitSteamSubscriptionPurchaseResponse Response)
		{
			steamOrderId = Response.SteamOrderId;
		}, delegate(MothershipError Error, int Status)
		{
			UpdatePurchaseResultScreen(PurchaseResult.Failure);
			UpdateState(ScreenState.SubscriptionPurchaseResult);
			Debug.LogError("SubscriptionKiosk could not start STEAM iap. Trace ID " + Error.TraceId + ", Error Code: " + Error.MothershipErrorCode);
			ProcessingSubscriptionPurchase = false;
		});
		UpdateState(ScreenState.SubscriptionPurchaseInProgress);
	}

	public void LaunchCheckoutFlowCallback(Message<Purchase> msg)
	{
		Debug.Log($"SubscriptionKiosk Purchase result: {msg.Type}   isError: {msg.IsError}   Data: {msg.Data.ToString()}");
		if (msg.IsError)
		{
			Error error = msg.GetError();
			if (error != null && error.Message != null && error.Message.Contains("cancel"))
			{
				UpdatePurchaseResultScreen(PurchaseResult.Cancel);
				return;
			}
			UpdatePurchaseResultScreen(PurchaseResult.Failure);
		}
		else
		{
			Purchase purchase = msg.GetPurchase();
			if (purchase != null && !string.IsNullOrEmpty(purchase.Sku))
			{
				UpdatePurchaseResultScreen(PurchaseResult.Success);
			}
			else
			{
				UpdatePurchaseResultScreen(PurchaseResult.Failure);
			}
		}
		SubscriptionManager.InitializePersonalSubscriptionData();
		UpdateState(ScreenState.SubscriptionPurchaseResult);
	}

	public void LocalSubscriptionDataUpdated()
	{
		SubscriptionManager.SubscriptionDetails subscriptionDetails = SubscriptionManager.LocalSubscriptionDetails();
		if (subscriptionDetails.active)
		{
			if (lastPurchase == PurchaseResult.Failure)
			{
				UpdatePurchaseResultScreen(PurchaseResult.Success);
			}
			if (currentState == ScreenState.MainMenuUnsubscribed)
			{
				UpdateState(ScreenState.MainMenuSubscribed);
			}
			if (currentState == ScreenState.PurchaseSubscription && subscriptionDetails.autoRenew)
			{
				UpdateState(ScreenState.SubscriptionData);
			}
		}
		subsVideoPlayer.clip = defaultVideoClip;
	}

	private void UpdateSubsVideo()
	{
		if (SubscriptionManager.IsLocalSubscribed())
		{
			subsVideoObservable.ObservableBehaviorRule = defaultObservableRule;
			subsVideoPlayer.clip = defaultVideoClip;
		}
		else
		{
			subsVideoPlayer.clip = steamSubsVideoClip;
			subsVideoObservable.ObservableBehaviorRule = steamObservableRule;
		}
	}

	public void SliceUpdate()
	{
		if (currentState == ScreenState.SubscriptionStatusUnknown)
		{
			HandScanned();
		}
	}
}

using System;
using GorillaNetworking;
using TMPro;
using UnityEngine;

[DefaultExecutionOrder(500)]
public class SIPurchaseTerminal : MonoBehaviour, ITouchScreenStation
{
	public enum PurchaseTerminalState
	{
		PurchaseAmountSelection,
		ConfirmPurchasePopup,
		PendingPurchasePopup,
		PurchaseCompletePopup,
		InsufficientFundsPopup,
		UnableToCompletePurchasePopup
	}

	private PurchaseTerminalState currentState;

	[SerializeField]
	private SIScreenRegion screenRegion;

	[SerializeField]
	private GameObject PopupBackgroundScreen;

	[SerializeField]
	private GameObject ConfirmPurchasePopupScreen;

	[SerializeField]
	private GameObject PurchaseCompletePopupScreen;

	[SerializeField]
	private GameObject PendingPurchasePopupScreen;

	[SerializeField]
	private GameObject InsufficientFundsPopupScreen;

	[SerializeField]
	private GameObject UnableToCompletePurchasePopupScreen;

	[SerializeField]
	private TextMeshProUGUI PurchaseAmountShinyRockCount;

	[SerializeField]
	private TextMeshProUGUI PurchaseAmountTechPointCount;

	[SerializeField]
	private TextMeshProUGUI PurchaseAmountCurrentShinyRockCount;

	[SerializeField]
	private TextMeshProUGUI PurchaseAmountCurrentTechPointsCount;

	[SerializeField]
	private TextMeshProUGUI ConfirmPurchaseShinyRockCount;

	[SerializeField]
	private TextMeshProUGUI ConfirmPurchaseTechPointCount;

	[SerializeField]
	private TextMeshProUGUI PurchasedTechPointCount;

	[SerializeField]
	private int maxPurchaseSize = 10;

	[SerializeField]
	private int minPurchaseSize = 1;

	[SerializeField]
	private int costPerTechPoint = 100;

	private int purchaseSize = 1;

	public SIScreenRegion ScreenRegion => screenRegion;

	private void OnEnable()
	{
		if (CosmeticsController.hasInstance)
		{
			DelayedOnEnable();
		}
		else
		{
			CosmeticsV2Spawner_Dirty.OnPostInstantiateAllPrefabs = (Action)Delegate.Combine(CosmeticsV2Spawner_Dirty.OnPostInstantiateAllPrefabs, new Action(DelayedOnEnable));
		}
	}

	private void DelayedOnEnable()
	{
		CosmeticsV2Spawner_Dirty.OnPostInstantiateAllPrefabs = (Action)Delegate.Remove(CosmeticsV2Spawner_Dirty.OnPostInstantiateAllPrefabs, new Action(DelayedOnEnable));
		CosmeticsController instance = CosmeticsController.instance;
		instance.OnGetCurrency = (Action)Delegate.Combine(instance.OnGetCurrency, new Action(OnUpdateCurrencyBalance));
		OnUpdateCurrencyBalance();
		PopupBackgroundScreen.SetActive(value: false);
		ConfirmPurchasePopupScreen.SetActive(value: false);
		PendingPurchasePopupScreen.SetActive(value: false);
		PurchaseCompletePopupScreen.SetActive(value: false);
		InsufficientFundsPopupScreen.SetActive(value: false);
		UnableToCompletePurchasePopupScreen.SetActive(value: false);
		UpdateState(PurchaseTerminalState.PurchaseAmountSelection, forceUpdate: true);
		purchaseSize = 1;
		UpdatePurchaseAmount();
	}

	private void OnDisable()
	{
		CosmeticsController instance = CosmeticsController.instance;
		instance.OnGetCurrency = (Action)Delegate.Remove(instance.OnGetCurrency, new Action(OnUpdateCurrencyBalance));
	}

	public void UpdateCurrentTechPoints()
	{
		PurchaseAmountCurrentTechPointsCount.text = SIPlayer.LocalPlayer.CurrentProgression.resourceArray[0].ToString();
	}

	private void OnUpdateCurrencyBalance()
	{
		PurchaseAmountCurrentShinyRockCount.text = CosmeticsController.instance.currencyBalance.ToString().ToUpperInvariant();
	}

	public void AddButton(SITouchscreenButton button, bool isPopupButton = false)
	{
	}

	public void TouchscreenButtonPressed(SITouchscreenButton.SITouchscreenButtonType buttonType, int data, int actorNr)
	{
		switch (currentState)
		{
		case PurchaseTerminalState.PurchaseAmountSelection:
			switch (buttonType)
			{
			case SITouchscreenButton.SITouchscreenButtonType.Purchase:
				SelectPurchase();
				break;
			case SITouchscreenButton.SITouchscreenButtonType.Next:
				IncreasePurchase();
				break;
			case SITouchscreenButton.SITouchscreenButtonType.Back:
				DecreasePurcahse();
				break;
			}
			break;
		case PurchaseTerminalState.ConfirmPurchasePopup:
			switch (buttonType)
			{
			case SITouchscreenButton.SITouchscreenButtonType.Confirm:
				ConfirmPurchase();
				break;
			case SITouchscreenButton.SITouchscreenButtonType.Cancel:
				ReturnToBaseScreen();
				break;
			}
			break;
		case PurchaseTerminalState.PurchaseCompletePopup:
			if (buttonType == SITouchscreenButton.SITouchscreenButtonType.Confirm)
			{
				ReturnToBaseScreen();
			}
			break;
		case PurchaseTerminalState.InsufficientFundsPopup:
			if (buttonType == SITouchscreenButton.SITouchscreenButtonType.Back)
			{
				ReturnToBaseScreen();
			}
			break;
		case PurchaseTerminalState.UnableToCompletePurchasePopup:
			if (buttonType == SITouchscreenButton.SITouchscreenButtonType.Back)
			{
				ReturnToBaseScreen();
			}
			break;
		case PurchaseTerminalState.PendingPurchasePopup:
			break;
		}
	}

	public void TouchscreenToggleButtonPressed(SITouchscreenButton.SITouchscreenButtonType buttonType, int data, int actorNr, bool isToggledOn)
	{
	}

	private void IncreasePurchase()
	{
		purchaseSize = Math.Min(purchaseSize + 1, maxPurchaseSize);
		UpdatePurchaseAmount();
	}

	private void DecreasePurcahse()
	{
		purchaseSize = Math.Max(purchaseSize - 1, minPurchaseSize);
		UpdatePurchaseAmount();
	}

	private void UpdatePurchaseAmount()
	{
		PurchaseAmountShinyRockCount.text = (purchaseSize * costPerTechPoint).ToString().ToUpperInvariant();
		PurchaseAmountTechPointCount.text = purchaseSize.ToString().ToUpperInvariant();
		ConfirmPurchaseShinyRockCount.text = (purchaseSize * costPerTechPoint).ToString().ToUpperInvariant();
		ConfirmPurchaseTechPointCount.text = purchaseSize.ToString().ToUpperInvariant();
		PurchasedTechPointCount.text = purchaseSize.ToString().ToUpperInvariant();
	}

	private void SelectPurchase()
	{
		UpdateState(PurchaseTerminalState.ConfirmPurchasePopup);
	}

	private void ConfirmPurchase()
	{
		int num = purchaseSize * costPerTechPoint;
		if (CosmeticsController.instance.currencyBalance < num)
		{
			UpdateState(PurchaseTerminalState.InsufficientFundsPopup);
			return;
		}
		UpdateState(PurchaseTerminalState.PendingPurchasePopup);
		ProgressionManager.Instance.PurchaseTechPoints(purchaseSize, delegate
		{
			SIProgression.Instance.SendPurchaseTechPointsData(purchaseSize);
			UpdateState(PurchaseTerminalState.PurchaseCompletePopup);
			ProgressionManager.Instance.RefreshUserInventory();
		}, delegate(string error)
		{
			Debug.LogError("[SIPurchaseTerminal] PurchaseTechPoints failed: " + error);
			UpdateState(PurchaseTerminalState.UnableToCompletePurchasePopup);
		});
	}

	private void ReturnToBaseScreen()
	{
		UpdateState(PurchaseTerminalState.PurchaseAmountSelection);
	}

	private void UpdateState(PurchaseTerminalState newState, bool forceUpdate = false)
	{
		if (forceUpdate || currentState != newState)
		{
			SetScreenVisibility(currentState, isEnabled: false);
			currentState = newState;
			SetScreenVisibility(currentState, isEnabled: true);
		}
	}

	private void SetScreenVisibility(PurchaseTerminalState screenState, bool isEnabled)
	{
		switch (screenState)
		{
		case PurchaseTerminalState.ConfirmPurchasePopup:
			PopupBackgroundScreen.SetActive(isEnabled);
			ConfirmPurchasePopupScreen.SetActive(isEnabled);
			break;
		case PurchaseTerminalState.PendingPurchasePopup:
			PopupBackgroundScreen.SetActive(isEnabled);
			PendingPurchasePopupScreen.SetActive(isEnabled);
			break;
		case PurchaseTerminalState.PurchaseCompletePopup:
			PopupBackgroundScreen.SetActive(isEnabled);
			PurchaseCompletePopupScreen.SetActive(isEnabled);
			break;
		case PurchaseTerminalState.InsufficientFundsPopup:
			PopupBackgroundScreen.SetActive(isEnabled);
			InsufficientFundsPopupScreen.SetActive(isEnabled);
			break;
		case PurchaseTerminalState.UnableToCompletePurchasePopup:
			PopupBackgroundScreen.SetActive(isEnabled);
			UnableToCompletePurchasePopupScreen.SetActive(isEnabled);
			break;
		}
	}
}

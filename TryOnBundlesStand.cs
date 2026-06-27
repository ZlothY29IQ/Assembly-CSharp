using System.Collections.Generic;
using System.Linq;
using Cosmetics;
using GorillaNetworking;
using GorillaNetworking.Store;
using PlayFab;
using UnityEngine;
using UnityEngine.UI;

public class TryOnBundlesStand : MonoBehaviour, IBuildValidation
{
	[SerializeField]
	private TryOnBundleButton[] TryOnBundleButtons;

	[SerializeField]
	private Image[] BundleIcons;

	[SerializeField]
	private GameObject creatorCodeProvider;

	[Header("The Index of the Selected Bundle from CosmeticsBundle Array in CosmeticsController")]
	private int SelectedButtonIndex = -1;

	public TryOnPurchaseButton purchaseButton;

	public Image selectedBundleImage;

	public Text computerScreenText;

	public string ComputerDefaultTextTitleDataKey;

	[SerializeField]
	private string ComputerDefaultTextTitleDataValue = "";

	public string ComputerAlreadyOwnTextTitleDataKey;

	[SerializeField]
	private string ComputerAlreadyOwnTextTitleDataValue = "";

	public string PurchaseButtonDefaultTextTitleDataKey;

	[SerializeField]
	private string PurchaseButtonDefaultTextTitleDataValue = "";

	public string PurchaseButtonAlreadyOwnTextTitleDataKey;

	[SerializeField]
	private string PurchaseButtonAlreadyOwnTextTitleDataValue = "";

	private bool bError;

	[Header("Error Text for Computer Screen")]
	public string computerScreeErrorText = "ERROR COMPLETING PURCHASE! PLEASE RESTART THE GAME, AND MAKE SURE YOU HAVE A STABLE INTERNET CONNECTION. ";

	private List<StoreBundle> storeBundles = new List<StoreBundle>();

	private string SelectedBundlePlayFabID => TryOnBundleButtons[SelectedButtonIndex].playfabBundleID;

	public static string CleanUpTitleDataValues(string titleDataResult)
	{
		string text = titleDataResult.Replace("\\r", "\r").Replace("\\n", "\n");
		if (text[0] == '"' && text[text.Length - 1] == '"')
		{
			text = text.Substring(1, text.Length - 2);
		}
		return text;
	}

	private void InitalizeButtons()
	{
		GetTryOnButtons();
		for (int i = 0; i < TryOnBundleButtons.Length; i++)
		{
			if (!CosmeticsController.instance.GetItemFromDict(TryOnBundleButtons[i].playfabBundleID).isNullItem)
			{
				TryOnBundleButtons[i].UpdateColor();
			}
		}
	}

	private void OnEnable()
	{
		BundleManager.instance._tryOnBundlesStand = this;
	}

	private void Start()
	{
		PlayFabTitleDataCache.Instance.GetTitleData(ComputerDefaultTextTitleDataKey, OnComputerDefaultTextTitleDataSuccess, OnComputerDefaultTextTitleDataFailure);
		PlayFabTitleDataCache.Instance.GetTitleData(ComputerAlreadyOwnTextTitleDataKey, OnComputerAlreadyOwnTextTitleDataSuccess, OnComputerAlreadyOwnTextTitleDataFailure);
		PlayFabTitleDataCache.Instance.GetTitleData(PurchaseButtonDefaultTextTitleDataKey, OnPurchaseButtonDefaultTextTitleDataSuccess, OnPurchaseButtonDefaultTextTitleDataFailure);
		PlayFabTitleDataCache.Instance.GetTitleData(PurchaseButtonAlreadyOwnTextTitleDataKey, OnPurchaseButtonAlreadyOwnTextTitleDataSuccess, OnPurchaseButtonAlreadyOwnTextTitleDataFailure);
		InitalizeButtons();
	}

	private void OnComputerDefaultTextTitleDataSuccess(string data)
	{
		ComputerDefaultTextTitleDataValue = CleanUpTitleDataValues(data);
		computerScreenText.text = ComputerDefaultTextTitleDataValue;
	}

	private void OnComputerDefaultTextTitleDataFailure(PlayFabError error)
	{
		ComputerDefaultTextTitleDataValue = "Failed to get TD Key : " + ComputerDefaultTextTitleDataKey;
		computerScreenText.text = ComputerDefaultTextTitleDataValue;
		Debug.LogError($"Error getting Computer Screen Title Data: {error}");
	}

	private void OnComputerAlreadyOwnTextTitleDataSuccess(string data)
	{
		ComputerAlreadyOwnTextTitleDataValue = CleanUpTitleDataValues(data);
	}

	private void OnComputerAlreadyOwnTextTitleDataFailure(PlayFabError error)
	{
		ComputerAlreadyOwnTextTitleDataValue = "Failed to get TD Key : " + ComputerAlreadyOwnTextTitleDataKey;
		Debug.LogError($"Error getting Computer Already Screen Title Data: {error}");
	}

	private void OnPurchaseButtonDefaultTextTitleDataSuccess(string data)
	{
		PurchaseButtonDefaultTextTitleDataValue = CleanUpTitleDataValues(data);
		purchaseButton.offText = PurchaseButtonDefaultTextTitleDataValue;
		purchaseButton.UpdateColor();
	}

	private void OnPurchaseButtonDefaultTextTitleDataFailure(PlayFabError error)
	{
		PurchaseButtonDefaultTextTitleDataValue = "Failed to get TD Key : " + PurchaseButtonDefaultTextTitleDataKey;
		purchaseButton.offText = PurchaseButtonDefaultTextTitleDataValue;
		purchaseButton.UpdateColor();
		Debug.LogError($"Error getting Tryon Purchase Button Default Text Title Data: {error}");
	}

	private void OnPurchaseButtonAlreadyOwnTextTitleDataSuccess(string data)
	{
		PurchaseButtonAlreadyOwnTextTitleDataValue = CleanUpTitleDataValues(data);
		purchaseButton.AlreadyOwnText = PurchaseButtonAlreadyOwnTextTitleDataValue;
	}

	private void OnPurchaseButtonAlreadyOwnTextTitleDataFailure(PlayFabError error)
	{
		PurchaseButtonAlreadyOwnTextTitleDataValue = "Failed to get TD Key : " + PurchaseButtonAlreadyOwnTextTitleDataKey;
		purchaseButton.AlreadyOwnText = PurchaseButtonAlreadyOwnTextTitleDataValue;
		Debug.LogError($"Error getting Tryon Purchase Button Already Own Text Title Data: {error}");
	}

	public void ClearSelectedBundle()
	{
		if (SelectedButtonIndex != -1)
		{
			TryOnBundleButtons[SelectedButtonIndex].isOn = false;
			if (TryOnBundleButtons[SelectedButtonIndex].playfabBundleID != "NULL" || TryOnBundleButtons[SelectedButtonIndex].playfabBundleID != "")
			{
				RemoveBundle(SelectedBundlePlayFabID);
				purchaseButton.offText = PurchaseButtonDefaultTextTitleDataValue;
				purchaseButton.ResetButton();
				selectedBundleImage.sprite = null;
				TryOnBundleButtons[SelectedButtonIndex].UpdateColor();
				SelectedButtonIndex = -1;
			}
		}
		computerScreenText.text = (bError ? computerScreeErrorText : ComputerDefaultTextTitleDataValue);
	}

	private void RemoveBundle(string BundleID)
	{
		CosmeticsController.CosmeticItem itemFromDict = CosmeticsController.instance.GetItemFromDict(BundleID);
		if (!itemFromDict.isNullItem)
		{
			string[] bundledItems = itemFromDict.bundledItems;
			foreach (string itemName in bundledItems)
			{
				CosmeticsController.instance.RemoveCosmeticItemFromSet(CosmeticsController.instance.tryOnSet, itemName, applyToPlayerPrefs: false);
			}
		}
	}

	private void TryOnBundle(string BundleID)
	{
		CosmeticsController.CosmeticItem itemFromDict = CosmeticsController.instance.GetItemFromDict(BundleID);
		if (itemFromDict.isNullItem)
		{
			return;
		}
		CosmeticsController.CosmeticItem[] items = CosmeticsController.instance.tryOnSet.items;
		for (int i = 0; i < items.Length; i++)
		{
			CosmeticsController.CosmeticItem cosmeticItem = items[i];
			if (!itemFromDict.bundledItems.Contains(cosmeticItem.itemName))
			{
				CosmeticsController.instance.RemoveCosmeticItemFromSet(CosmeticsController.instance.tryOnSet, cosmeticItem.itemName, applyToPlayerPrefs: false);
			}
		}
		string[] bundledItems = itemFromDict.bundledItems;
		foreach (string itemID in bundledItems)
		{
			if (!CosmeticsController.instance.tryOnSet.HasItem(itemID))
			{
				CosmeticsController.instance.ApplyCosmeticItemToSet(CosmeticsController.instance.tryOnSet, CosmeticsController.instance.GetItemFromDict(itemID), isLeftHand: false, applyToPlayerPrefs: false);
			}
		}
	}

	private async void LoadBundle(TryOnBundleButton pressedTryOnBundleButton, bool isLeftHand)
	{
		CosmeticsController.CosmeticItem BundleToTry = CosmeticsController.instance.GetItemFromDict(pressedTryOnBundleButton.playfabBundleID);
		float timeEntered = Time.time;
		float maxTime = 1f;
		bool flag = true;
		while (flag)
		{
			if (Time.time > timeEntered + maxTime)
			{
				return;
			}
			await Awaitable.EndOfFrameAsync();
			flag = false;
			for (int i = 0; i < BundleToTry.bundledItems.Length; i++)
			{
				if (VRRig.LocalRig.cosmeticsObjectRegistry.Cosmetic(BundleToTry.bundledItems[i]) == null)
				{
					flag = true;
				}
			}
		}
		PressTryOnBundleButton(pressedTryOnBundleButton, isLeftHand);
	}

	public void PressTryOnBundleButton(TryOnBundleButton pressedTryOnBundleButton, bool isLeftHand)
	{
		if (pressedTryOnBundleButton.playfabBundleID == "NULL")
		{
			Debug.LogError("TryOnBundlesStand - PressTryOnBundleButton - Invalid bundle ID");
			return;
		}
		CosmeticsController.CosmeticItem itemFromDict = CosmeticsController.instance.GetItemFromDict(pressedTryOnBundleButton.playfabBundleID);
		if (itemFromDict.isNullItem)
		{
			Debug.LogError("TryOnBundlesStand - PressTryOnBundleButton - Bundle is Null + " + pressedTryOnBundleButton.playfabBundleID);
			return;
		}
		bool flag = false;
		for (int i = 0; i < itemFromDict.bundledItems.Length; i++)
		{
			if (VRRig.LocalRig.cosmeticsObjectRegistry.Cosmetic(itemFromDict.bundledItems[i]) == null)
			{
				flag = true;
			}
		}
		if (flag)
		{
			LoadBundle(pressedTryOnBundleButton, isLeftHand);
			return;
		}
		if (SelectedButtonIndex != pressedTryOnBundleButton.buttonIndex)
		{
			ClearSelectedBundle();
		}
		switch (CosmeticsController.instance.CheckIfCosmeticSetMatchesItemSet(CosmeticsController.instance.tryOnSet, pressedTryOnBundleButton.playfabBundleID))
		{
		case CosmeticsController.EWearingCosmeticSet.Complete:
			ClearSelectedBundle();
			break;
		case CosmeticsController.EWearingCosmeticSet.NotWearing:
			TryOnBundle(pressedTryOnBundleButton.playfabBundleID);
			SelectedButtonIndex = pressedTryOnBundleButton.buttonIndex;
			break;
		case CosmeticsController.EWearingCosmeticSet.Partial:
			if (pressedTryOnBundleButton.isOn)
			{
				ClearSelectedBundle();
				break;
			}
			TryOnBundle(pressedTryOnBundleButton.playfabBundleID);
			SelectedButtonIndex = pressedTryOnBundleButton.buttonIndex;
			break;
		case CosmeticsController.EWearingCosmeticSet.NotASet:
			Debug.LogError("TryOnBundlesStand - PressTryOnBundleButton - Item is Not A Set");
			break;
		}
		if (SelectedButtonIndex != -1)
		{
			if (!bError)
			{
				selectedBundleImage.sprite = BundleManager.instance.storeBundlesById[pressedTryOnBundleButton.playfabBundleID].bundleImage;
				pressedTryOnBundleButton.isOn = true;
				purchaseButton.offText = GetPurchaseButtonText(pressedTryOnBundleButton.playfabBundleID);
				computerScreenText.text = GetComputerScreenText(pressedTryOnBundleButton.playfabBundleID);
				AlreadyOwnCheck();
			}
			pressedTryOnBundleButton.UpdateColor();
		}
		else
		{
			if (!bError)
			{
				computerScreenText.text = ComputerDefaultTextTitleDataValue;
				purchaseButton.offText = PurchaseButtonDefaultTextTitleDataValue;
			}
			pressedTryOnBundleButton.isOn = false;
			selectedBundleImage.sprite = null;
			purchaseButton.offText = PurchaseButtonDefaultTextTitleDataValue;
			purchaseButton.ResetButton();
			purchaseButton.UpdateColor();
		}
		CosmeticsController.instance.UpdateShoppingCart();
		CosmeticsController.instance.UpdateWornCosmetics(sync: true);
		pressedTryOnBundleButton.UpdateColor();
	}

	private string GetComputerScreenText(string playfabBundleID)
	{
		return BundleManager.instance.storeBundlesById[playfabBundleID].bundleDescriptionText;
	}

	private string GetPurchaseButtonText(string playfabBundleID)
	{
		return BundleManager.instance.storeBundlesById[playfabBundleID].purchaseButtonText;
	}

	public void PurchaseButtonPressed()
	{
		if (SelectedButtonIndex != -1)
		{
			CosmeticsController.instance.PurchaseBundle(BundleManager.instance.storeBundlesById[SelectedBundlePlayFabID], creatorCodeProvider.GetComponent<ICreatorCodeProvider>());
		}
	}

	public void AlreadyOwnCheck()
	{
		if (SelectedButtonIndex == -1)
		{
			return;
		}
		if (BundleManager.instance.storeBundlesById[SelectedBundlePlayFabID].isOwned)
		{
			purchaseButton.AlreadyOwn();
			if (!bError)
			{
				computerScreenText.text = ComputerAlreadyOwnTextTitleDataValue;
			}
		}
		else
		{
			if (!bError)
			{
				computerScreenText.text = GetBundleComputerText(SelectedBundlePlayFabID);
			}
			purchaseButton.UpdateColor();
		}
	}

	public void GetTryOnButtons()
	{
		StoreBundleData[] tryOnButtons = BundleManager.instance.GetTryOnButtons();
		for (int i = 0; i < TryOnBundleButtons.Length; i++)
		{
			if (i < tryOnButtons.Length)
			{
				if (tryOnButtons[i] != null && tryOnButtons[i].playfabBundleID != "NULL" && tryOnButtons[i].bundleImage != null)
				{
					TryOnBundleButtons[i].playfabBundleID = tryOnButtons[i].playfabBundleID;
					BundleIcons[i].sprite = tryOnButtons[i].bundleImage;
				}
				else
				{
					TryOnBundleButtons[i].playfabBundleID = "NULL";
					BundleIcons[i].sprite = null;
				}
			}
			else
			{
				TryOnBundleButtons[i].playfabBundleID = "NULL";
				BundleIcons[i].sprite = null;
			}
			TryOnBundleButtons[i].UpdateColor();
		}
	}

	public void UpdateBundles(StoreBundleData[] Bundles)
	{
		Debug.LogWarning("TryOnBundlesStand - UpdateBundles is an editor only function!");
	}

	private string GetBundleComputerText(string PlayFabID)
	{
		if (BundleManager.instance.storeBundlesById.TryGetValue(PlayFabID, out var value))
		{
			return value.bundleDescriptionText;
		}
		return "ERROR THIS DOES NOT EXIST YET";
	}

	public void ErrorCompleting()
	{
		bError = true;
		purchaseButton.ErrorHappened();
		computerScreenText.text = computerScreeErrorText;
	}

	bool IBuildValidation.BuildValidationCheck()
	{
		if (creatorCodeProvider == null || !creatorCodeProvider.TryGetComponent<ICreatorCodeProvider>(out var _))
		{
			Debug.LogError(base.name + " has no Creator Code Provider. This will break bundle purchasing.");
			return false;
		}
		return true;
	}
}

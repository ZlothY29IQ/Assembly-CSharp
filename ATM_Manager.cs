using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using GorillaNetworking;
using GorillaNetworking.Store;
using TMPro;
using UnityEngine;

public class ATM_Manager : MonoBehaviour
{
	public enum CreatorCodeStatus
	{
		Empty,
		Unchecked,
		Validating,
		Valid
	}

	public enum ATMStages
	{
		Unavailable,
		Begin,
		Menu,
		Balance,
		Choose,
		Confirm,
		Purchasing,
		Success,
		Failure,
		SafeAccount
	}

	private const string ATM_STARTUP_KEY = "ATM_STARTUP";

	private const string ATM_SCREEN_KEY = "ATM_SCREEN";

	private const string ATM_NOT_AVAILABLE_KEY = "ATM_NOT_AVAILABLE";

	private const string ATM_BEGIN_KEY = "ATM_BEGIN";

	private const string ATM_MAIN_SCREEN_KEY = "ATM_MAIN_SCREEN";

	private const string ATM_CHECK_YOUR_BALANCE_KEY = "ATM_CHECK_YOUR_BALANCE";

	private const string ATM_PURCHASING_DISABLED_OUT_OF_ORDER_KEY = "ATM_PURCHASING_DISABLED_OUT_OF_ORDER";

	private const string ATM_CURRENT_BALANCE_KEY = "ATM_CURRENT_BALANCE";

	private const string ATM_MODDED_CLIENT_KEY = "ATM_MODDED_CLIENT";

	private const string ATM_CHOOSE_PURCHASE_KEY = "ATM_CHOOSE_PURCHASE";

	private const string ATM_PURCHASE_CONFIRMATION_KEY = "ATM_PURCHASE_CONFIRMATION";

	private const string ATM_PURCHASE_CONFIRMATION_STEAM_KEY = "ATM_PURCHASE_CONFIRMATION_STEAM";

	private const string ATM_PURCHASING_KEY = "ATM_PURCHASING";

	private const string ATM_SUCCESS_NEW_BALANCE_KEY = "ATM_SUCCESS_NEW_BALANCE";

	private const string ATM_PURCHASE_CANCELLED_KEY = "ATM_PURCHASE_CANCELLED";

	private const string ATM_LOCKED_KEY = "ATM_LOCKED";

	private const string ATM_RETURN_KEY = "ATM_RETURN";

	private const string ATM_BACK_KEY = "ATM_BACK";

	private const string ATM_CONFIRM_KEY = "ATM_CONFIRM";

	private const string ATM_IAP_NOT_AVAILABLE_KEY = "ATM_IAP_NOT_AVAILABLE";

	private const string ATM_BALANCE_KEY = "ATM_BALANCE";

	private const string ATM_PURCHASE_KEY = "ATM_PURCHASE";

	private const string ATM_CREATOR_CODE_KEY = "ATM_CREATOR_CODE";

	private const string ATM_CREATOR_CODE_VALIDATING_KEY = "ATM_CREATOR_CODE_VALIDATING";

	private const string ATM_CREATOR_CODE_VALID_KEY = "ATM_CREATOR_CODE_VALID";

	private const string ATM_CREATOR_CODE_INVALID_KEY = "ATM_CREATOR_CODE_INVALID";

	private const string ATM_PURCHASE_OPTION_FIRST_KEY = "ATM_PURCHASE_OPTION_FIRST";

	private const string ATM_PURCHASE_OPTION_SECOND_KEY = "ATM_PURCHASE_OPTION_SECOND";

	private const string ATM_PURCHASE_OPTION_THIRD_KEY = "ATM_PURCHASE_OPTION_THIRD";

	private const string ATM_PURCHASE_OPTION_FOURTH_KEY = "ATM_PURCHASE_OPTION_FOURTH";

	[OnEnterPlay_SetNull]
	public static volatile ATM_Manager instance;

	private const int MAX_CODE_LENGTH = 10;

	public List<ATM_UI> atmUIs = new List<ATM_UI>();

	[HideInInspector]
	public List<CreatorCodeSmallDisplay> smallDisplays;

	private string currentCreatorCode;

	private string codeFirstUsedTime;

	private string initialCode;

	private string temporaryOverrideCode;

	private CreatorCodeStatus creatorCodeStatus;

	private ATMStages currentATMStage;

	public int numShinyRocksToBuy;

	public float shinyRocksCost;

	private Member supportedMember;

	public bool alreadyBegan;

	public string ValidatedCreatorCode { get; set; }

	public ATMStages CurrentATMStage => currentATMStage;

	public void Awake()
	{
		if ((bool)instance)
		{
			UnityEngine.Object.Destroy(this);
		}
		else
		{
			instance = this;
		}
		string defaultResult = "CREATOR CODE: ";
		if (!LocalisationManager.TryGetKeyForCurrentLocale("ATM_CREATOR_CODE", out var result, defaultResult))
		{
			Debug.LogError("[LOCALIZATION::ATM_MANAGER] Failed to get key for [ATM_CREATOR_CODE]");
		}
		foreach (ATM_UI atmUI in atmUIs)
		{
			atmUI.creatorCodeTitle.text = result;
		}
		SwitchToStage(ATMStages.Unavailable);
		smallDisplays = new List<CreatorCodeSmallDisplay>();
	}

	public void Start()
	{
		Debug.Log("ATM COUNT: " + atmUIs.Count);
		Debug.Log("SMALL DISPLAY COUNT: " + smallDisplays.Count);
		GameEvents.OnGorrillaATMKeyButtonPressedEvent.AddListener(PressButton);
		currentCreatorCode = "";
		if (PlayerPrefs.HasKey("CodeUsedTime"))
		{
			codeFirstUsedTime = PlayerPrefs.GetString("CodeUsedTime");
			DateTime dateTime = DateTime.Parse(codeFirstUsedTime);
			if ((DateTime.Now - dateTime).TotalDays > 14.0)
			{
				PlayerPrefs.SetString("CreatorCode", "");
			}
			else
			{
				currentCreatorCode = PlayerPrefs.GetString("CreatorCode", "");
				initialCode = currentCreatorCode;
				Debug.Log("Initial code: " + initialCode);
				if (string.IsNullOrEmpty(currentCreatorCode))
				{
					creatorCodeStatus = CreatorCodeStatus.Empty;
				}
				else
				{
					creatorCodeStatus = CreatorCodeStatus.Unchecked;
				}
				foreach (CreatorCodeSmallDisplay smallDisplay in smallDisplays)
				{
					smallDisplay.SetCode(currentCreatorCode);
				}
			}
		}
		foreach (ATM_UI atmUI in atmUIs)
		{
			atmUI.creatorCodeField.text = currentCreatorCode;
		}
	}

	private void OnEnable()
	{
		LocalisationManager.RegisterOnLanguageChanged(OnLanguageChanged);
		SwitchToStage(currentATMStage);
	}

	private void OnDisable()
	{
		LocalisationManager.UnregisterOnLanguageChanged(OnLanguageChanged);
	}

	private void OnLanguageChanged()
	{
		SwitchToStage(currentATMStage);
	}

	public void PressButton(GorillaATMKeyBindings buttonPressed)
	{
		if (currentATMStage != ATMStages.Confirm || creatorCodeStatus == CreatorCodeStatus.Validating)
		{
			return;
		}
		string defaultResult = "CREATOR CODE: ";
		LocalisationManager.TryGetKeyForCurrentLocale("ATM_CREATOR_CODE", out var result, defaultResult);
		foreach (ATM_UI atmUI in atmUIs)
		{
			atmUI.creatorCodeTitle.text = result;
		}
		if (buttonPressed == GorillaATMKeyBindings.delete)
		{
			if (currentCreatorCode.Length > 0)
			{
				currentCreatorCode = currentCreatorCode.Substring(0, currentCreatorCode.Length - 1);
				if (currentCreatorCode.Length == 0)
				{
					creatorCodeStatus = CreatorCodeStatus.Empty;
					ValidatedCreatorCode = "";
					foreach (CreatorCodeSmallDisplay smallDisplay in smallDisplays)
					{
						smallDisplay.SetCode("");
					}
					PlayerPrefs.SetString("CreatorCode", "");
					PlayerPrefs.Save();
				}
				else
				{
					creatorCodeStatus = CreatorCodeStatus.Unchecked;
				}
			}
		}
		else if (currentCreatorCode.Length < 10)
		{
			string text = currentCreatorCode;
			string text2;
			if (buttonPressed >= GorillaATMKeyBindings.delete)
			{
				text2 = buttonPressed.ToString();
			}
			else
			{
				int num = (int)buttonPressed;
				text2 = num.ToString();
			}
			currentCreatorCode = text + text2;
			creatorCodeStatus = CreatorCodeStatus.Unchecked;
		}
		foreach (ATM_UI atmUI2 in atmUIs)
		{
			atmUI2.creatorCodeField.text = currentCreatorCode;
		}
	}

	public void ProcessATMState(string currencyButton)
	{
		switch (currentATMStage)
		{
		case ATMStages.Begin:
			SwitchToStage(ATMStages.Menu);
			break;
		case ATMStages.Menu:
			if (PlayFabAuthenticator.instance.GetSafety())
			{
				if (!(currencyButton == "one"))
				{
					if (currencyButton == "four")
					{
						SwitchToStage(ATMStages.Begin);
					}
				}
				else
				{
					SwitchToStage(ATMStages.Balance);
				}
				break;
			}
			switch (currencyButton)
			{
			case "one":
				SwitchToStage(ATMStages.Balance);
				break;
			case "two":
				SwitchToStage(ATMStages.Choose);
				break;
			case "back":
				SwitchToStage(ATMStages.Begin);
				break;
			}
			break;
		case ATMStages.Balance:
			if (currencyButton == "back")
			{
				SwitchToStage(ATMStages.Menu);
			}
			break;
		case ATMStages.Choose:
			switch (currencyButton)
			{
			case "one":
				numShinyRocksToBuy = 1000;
				shinyRocksCost = 4.99f;
				CosmeticsController.instance.itemToPurchase = "1000SHINYROCKS";
				CosmeticsController.instance.buyingBundle = false;
				SwitchToStage(ATMStages.Confirm);
				break;
			case "two":
				numShinyRocksToBuy = 2200;
				shinyRocksCost = 9.99f;
				CosmeticsController.instance.itemToPurchase = "2200SHINYROCKS";
				CosmeticsController.instance.buyingBundle = false;
				SwitchToStage(ATMStages.Confirm);
				break;
			case "three":
				numShinyRocksToBuy = 5000;
				shinyRocksCost = 19.99f;
				CosmeticsController.instance.itemToPurchase = "5000SHINYROCKS";
				CosmeticsController.instance.buyingBundle = false;
				SwitchToStage(ATMStages.Confirm);
				break;
			case "four":
				numShinyRocksToBuy = 11000;
				shinyRocksCost = 39.99f;
				CosmeticsController.instance.itemToPurchase = "11000SHINYROCKS";
				CosmeticsController.instance.buyingBundle = false;
				SwitchToStage(ATMStages.Confirm);
				break;
			case "back":
				SwitchToStage(ATMStages.Menu);
				break;
			}
			break;
		case ATMStages.Confirm:
			if (!(currencyButton == "one"))
			{
				if (currencyButton == "back")
				{
					SwitchToStage(ATMStages.Choose);
				}
			}
			else if (creatorCodeStatus == CreatorCodeStatus.Empty)
			{
				CosmeticsController.instance.SteamPurchase();
				SwitchToStage(ATMStages.Purchasing);
			}
			else
			{
				StartCoroutine(CheckValidationCoroutine());
			}
			break;
		default:
			SwitchToStage(ATMStages.Menu);
			break;
		case ATMStages.Unavailable:
		case ATMStages.Purchasing:
			break;
		}
	}

	public void AddATM(ATM_UI newATM)
	{
		atmUIs.Add(newATM);
		newATM.creatorCodeField.text = currentCreatorCode;
		SwitchToStage(currentATMStage);
	}

	public void RemoveATM(ATM_UI atmToRemove)
	{
		atmUIs.Remove(atmToRemove);
	}

	public void SetTemporaryCreatorCode(string creatorCode, bool onlyIfEmpty = true, Action<bool> OnComplete = null)
	{
		if (onlyIfEmpty && (creatorCodeStatus != 0 || !currentCreatorCode.IsNullOrEmpty()))
		{
			OnComplete?.Invoke(obj: false);
			return;
		}
		string pattern = "^[a-zA-Z0-9]+$";
		if (creatorCode.Length > 10 || !Regex.IsMatch(creatorCode, pattern))
		{
			OnComplete?.Invoke(obj: false);
			return;
		}
		NexusManager.instance.VerifyCreatorCode(creatorCode, delegate
		{
			if (currentATMStage > ATMStages.Confirm)
			{
				OnComplete?.Invoke(obj: false);
			}
			else if (onlyIfEmpty && (creatorCodeStatus != 0 || !currentCreatorCode.IsNullOrEmpty()))
			{
				OnComplete?.Invoke(obj: false);
			}
			else
			{
				temporaryOverrideCode = creatorCode;
				currentCreatorCode = creatorCode;
				creatorCodeStatus = CreatorCodeStatus.Unchecked;
				foreach (CreatorCodeSmallDisplay smallDisplay in smallDisplays)
				{
					smallDisplay.SetCode(currentCreatorCode);
				}
				foreach (ATM_UI atmUI in atmUIs)
				{
					atmUI.creatorCodeField.text = currentCreatorCode;
				}
				OnComplete?.Invoke(obj: true);
			}
		}, delegate
		{
			OnComplete?.Invoke(obj: false);
		});
	}

	public void ResetTemporaryCreatorCode()
	{
		if (creatorCodeStatus == CreatorCodeStatus.Unchecked && currentCreatorCode.Equals(temporaryOverrideCode))
		{
			currentCreatorCode = "";
			creatorCodeStatus = CreatorCodeStatus.Empty;
			foreach (CreatorCodeSmallDisplay smallDisplay in smallDisplays)
			{
				smallDisplay.SetCode("");
			}
			foreach (ATM_UI atmUI in atmUIs)
			{
				atmUI.creatorCodeField.text = currentCreatorCode;
			}
		}
		temporaryOverrideCode = "";
	}

	private void ResetCreatorCode()
	{
		Debug.Log("Resetting creator code");
		string defaultResult = "CREATOR CODE: ";
		LocalisationManager.TryGetKeyForCurrentLocale("ATM_CREATOR_CODE", out var result, defaultResult);
		foreach (ATM_UI atmUI in atmUIs)
		{
			atmUI.creatorCodeTitle.text = result;
		}
		foreach (CreatorCodeSmallDisplay smallDisplay in smallDisplays)
		{
			smallDisplay.SetCode("");
		}
		currentCreatorCode = "";
		creatorCodeStatus = CreatorCodeStatus.Empty;
		supportedMember = default(Member);
		ValidatedCreatorCode = "";
		PlayerPrefs.SetString("CreatorCode", "");
		PlayerPrefs.Save();
		foreach (ATM_UI atmUI2 in atmUIs)
		{
			atmUI2.creatorCodeField.text = currentCreatorCode;
		}
	}

	private IEnumerator CheckValidationCoroutine()
	{
		foreach (ATM_UI atmUI in atmUIs)
		{
			atmUI.creatorCodeTitle.text = "CREATOR CODE: VALIDATING";
			LocalisationManager.TryGetKeyForCurrentLocale("ATM_CREATOR_CODE_VALIDATING", out var result, atmUI.atmText.text);
			atmUI.creatorCodeTitle.text = result;
		}
		VerifyCreatorCode();
		while (creatorCodeStatus == CreatorCodeStatus.Validating)
		{
			yield return new WaitForSeconds(0.5f);
		}
		if (creatorCodeStatus != CreatorCodeStatus.Valid)
		{
			yield break;
		}
		foreach (ATM_UI atmUI2 in atmUIs)
		{
			atmUI2.creatorCodeTitle.text = "CREATOR CODE: VALID";
			LocalisationManager.TryGetKeyForCurrentLocale("ATM_CREATOR_CODE_VALID", out var result2, atmUI2.atmText.text);
			atmUI2.creatorCodeTitle.text = result2;
		}
		SwitchToStage(ATMStages.Purchasing);
		CosmeticsController.instance.SteamPurchase();
	}

	public void SwitchToStage(ATMStages newStage)
	{
		currentATMStage = newStage;
		foreach (ATM_UI atmUI in atmUIs)
		{
			if (!atmUI.atmText)
			{
				continue;
			}
			string result = "";
			string result2 = "";
			string result3 = "";
			string result4 = "";
			string result5 = "";
			switch (newStage)
			{
			case ATMStages.Unavailable:
				atmUI.atmText.text = "ATM NOT AVAILABLE! PLEASE TRY AGAIN LATER!";
				LocalisationManager.TryGetKeyForCurrentLocale("ATM_NOT_AVAILABLE", out result, atmUI.atmText.text);
				atmUI.atmText.text = result;
				atmUI.ATM_RightColumnButtonText[0].text = "";
				atmUI.ATM_RightColumnArrowText[0].enabled = false;
				atmUI.ATM_RightColumnButtonText[1].text = "";
				atmUI.ATM_RightColumnArrowText[1].enabled = false;
				atmUI.ATM_RightColumnButtonText[2].text = "";
				atmUI.ATM_RightColumnArrowText[2].enabled = false;
				atmUI.ATM_RightColumnButtonText[3].text = "";
				atmUI.ATM_RightColumnArrowText[3].enabled = false;
				atmUI.creatorCodeObject.SetActive(value: false);
				break;
			case ATMStages.Begin:
				atmUI.atmText.text = "WELCOME! PRESS ANY BUTTON TO BEGIN.";
				LocalisationManager.TryGetKeyForCurrentLocale("ATM_STARTUP", out result, atmUI.atmText.text);
				LocalisationManager.TryGetKeyForCurrentLocale("ATM_BEGIN", out result5, "BEGIN");
				atmUI.atmText.text = result;
				atmUI.ATM_RightColumnButtonText[0].text = "";
				atmUI.ATM_RightColumnArrowText[0].enabled = false;
				atmUI.ATM_RightColumnButtonText[1].text = "";
				atmUI.ATM_RightColumnArrowText[1].enabled = false;
				atmUI.ATM_RightColumnButtonText[2].text = "";
				atmUI.ATM_RightColumnArrowText[2].enabled = false;
				atmUI.ATM_RightColumnButtonText[3].text = result5;
				atmUI.ATM_RightColumnArrowText[3].enabled = true;
				atmUI.creatorCodeObject.SetActive(value: false);
				break;
			case ATMStages.Menu:
				if (PlayFabAuthenticator.instance.GetSafety())
				{
					atmUI.atmText.text = "CHECK YOUR BALANCE.";
					LocalisationManager.TryGetKeyForCurrentLocale("ATM_CHECK_YOUR_BALANCE", out result, atmUI.atmText.text);
					LocalisationManager.TryGetKeyForCurrentLocale("ATM_BALANCE", out result2, atmUI.atmText.text);
					atmUI.atmText.text = result;
					atmUI.ATM_RightColumnButtonText[0].text = result2;
					atmUI.ATM_RightColumnArrowText[0].enabled = true;
					atmUI.ATM_RightColumnButtonText[1].text = "";
					atmUI.ATM_RightColumnArrowText[1].enabled = false;
					atmUI.ATM_RightColumnButtonText[2].text = "";
					atmUI.ATM_RightColumnArrowText[2].enabled = false;
					atmUI.ATM_RightColumnButtonText[3].text = "";
					atmUI.ATM_RightColumnArrowText[3].enabled = false;
					atmUI.creatorCodeObject.SetActive(value: false);
				}
				else
				{
					atmUI.atmText.text = "CHECK YOUR BALANCE OR PURCHASE MORE SHINY ROCKS.";
					LocalisationManager.TryGetKeyForCurrentLocale("ATM_MAIN_SCREEN", out result, atmUI.atmText.text);
					LocalisationManager.TryGetKeyForCurrentLocale("ATM_BALANCE", out result2, atmUI.atmText.text);
					LocalisationManager.TryGetKeyForCurrentLocale("ATM_PURCHASE", out result3, atmUI.atmText.text);
					atmUI.atmText.text = result;
					atmUI.ATM_RightColumnButtonText[0].text = result2;
					atmUI.ATM_RightColumnArrowText[0].enabled = true;
					atmUI.ATM_RightColumnButtonText[1].text = result3;
					atmUI.ATM_RightColumnArrowText[1].enabled = true;
					atmUI.ATM_RightColumnButtonText[2].text = "";
					atmUI.ATM_RightColumnArrowText[2].enabled = false;
					atmUI.ATM_RightColumnButtonText[3].text = "";
					atmUI.ATM_RightColumnArrowText[3].enabled = false;
					atmUI.creatorCodeObject.SetActive(value: false);
				}
				break;
			case ATMStages.Balance:
				atmUI.atmText.text = "CURRENT BALANCE:\n\n" + CosmeticsController.instance.CurrencyBalance;
				LocalisationManager.TryGetKeyForCurrentLocale("ATM_CURRENT_BALANCE", out result, atmUI.atmText.text);
				atmUI.atmText.text = result + "\n\n" + CosmeticsController.instance.CurrencyBalance;
				atmUI.ATM_RightColumnButtonText[0].text = "";
				atmUI.ATM_RightColumnArrowText[0].enabled = false;
				atmUI.ATM_RightColumnButtonText[1].text = "";
				atmUI.ATM_RightColumnArrowText[1].enabled = false;
				atmUI.ATM_RightColumnButtonText[2].text = "";
				atmUI.ATM_RightColumnArrowText[2].enabled = false;
				atmUI.ATM_RightColumnButtonText[3].text = "";
				atmUI.ATM_RightColumnArrowText[3].enabled = false;
				atmUI.creatorCodeObject.SetActive(value: false);
				break;
			case ATMStages.Choose:
			{
				string defaultResult = "{numShinyRocksToBuy} - {currencySymbol}{shinyRocksCost}";
				string defaultResult2 = "{numShinyRocksToBuy} - {currencySymbol}{shinyRocksCost}\r\n({discount}% BONUS!";
				LocalisationManager.TryGetKeyForCurrentLocale("ATM_PURCHASE_OPTION_FIRST", out result2, defaultResult);
				LocalisationManager.TryGetKeyForCurrentLocale("ATM_PURCHASE_OPTION_SECOND", out result3, defaultResult2);
				LocalisationManager.TryGetKeyForCurrentLocale("ATM_PURCHASE_OPTION_SECOND", out result4, defaultResult2);
				LocalisationManager.TryGetKeyForCurrentLocale("ATM_PURCHASE_OPTION_SECOND", out result5, defaultResult2);
				result2 = result2.Replace("{numShinyRocksToBuy}", "1000").Replace("{currencySymbol}", "$").Replace("{shinyRocksCost}", "4.99");
				result3 = result3.Replace("{numShinyRocksToBuy}", "2200").Replace("{currencySymbol}", "$").Replace("{shinyRocksCost}", "9.99")
					.Replace("{discount}", "10");
				result4 = result4.Replace("{numShinyRocksToBuy}", "5000").Replace("{currencySymbol}", "$").Replace("{shinyRocksCost}", "19.99")
					.Replace("{discount}", "25");
				result5 = result5.Replace("{numShinyRocksToBuy}", "11000").Replace("{currencySymbol}", "$").Replace("{shinyRocksCost}", "39.99")
					.Replace("{discount}", "37");
				atmUI.atmText.text = "CHOOSE AN AMOUNT OF SHINY ROCKS TO PURCHASE.";
				LocalisationManager.TryGetKeyForCurrentLocale("ATM_CHOOSE_PURCHASE", out result, atmUI.atmText.text);
				atmUI.atmText.text = result;
				atmUI.ATM_RightColumnButtonText[0].text = result2;
				atmUI.ATM_RightColumnArrowText[0].enabled = true;
				atmUI.ATM_RightColumnButtonText[1].text = result3;
				atmUI.ATM_RightColumnArrowText[1].enabled = true;
				atmUI.ATM_RightColumnButtonText[2].text = result4;
				atmUI.ATM_RightColumnArrowText[2].enabled = true;
				atmUI.ATM_RightColumnButtonText[3].text = result5;
				atmUI.ATM_RightColumnArrowText[3].enabled = true;
				atmUI.creatorCodeObject.SetActive(value: false);
				break;
			}
			case ATMStages.Confirm:
				atmUI.atmText.text = "YOU HAVE CHOSEN TO PURCHASE " + numShinyRocksToBuy + " SHINY ROCKS FOR $" + shinyRocksCost + ". CONFIRM TO LAUNCH A STEAM WINDOW TO COMPLETE YOUR PURCHASE.";
				LocalisationManager.TryGetKeyForCurrentLocale("ATM_PURCHASE_CONFIRMATION_STEAM", out result, atmUI.atmText.text);
				LocalisationManager.TryGetKeyForCurrentLocale("ATM_CONFIRM", out result2, "CONFIRM");
				result = result.Replace("{numShinyRocksToBuy}", numShinyRocksToBuy.ToString());
				result = result.Replace("{currencySymbol}", "$");
				result = result.Replace("{shinyRocksCost}", shinyRocksCost.ToString());
				atmUI.atmText.text = result;
				atmUI.ATM_RightColumnButtonText[0].text = result2;
				atmUI.ATM_RightColumnArrowText[0].enabled = true;
				atmUI.ATM_RightColumnButtonText[1].text = "";
				atmUI.ATM_RightColumnArrowText[1].enabled = false;
				atmUI.ATM_RightColumnButtonText[2].text = "";
				atmUI.ATM_RightColumnArrowText[2].enabled = false;
				atmUI.ATM_RightColumnButtonText[3].text = "";
				atmUI.ATM_RightColumnArrowText[3].enabled = false;
				atmUI.creatorCodeObject.SetActive(value: true);
				break;
			case ATMStages.Purchasing:
				atmUI.atmText.text = "PURCHASING IN STEAM...";
				LocalisationManager.TryGetKeyForCurrentLocale("ATM_PURCHASING", out result, atmUI.atmText.text);
				atmUI.atmText.text = result;
				atmUI.creatorCodeObject.SetActive(value: false);
				break;
			case ATMStages.Success:
				atmUI.atmText.text = "SUCCESS! NEW SHINY ROCKS BALANCE: " + (CosmeticsController.instance.CurrencyBalance + numShinyRocksToBuy);
				LocalisationManager.TryGetKeyForCurrentLocale("ATM_SUCCESS_NEW_BALANCE", out result, atmUI.atmText.text);
				atmUI.atmText.text = result + (CosmeticsController.instance.CurrencyBalance + numShinyRocksToBuy);
				if (creatorCodeStatus == CreatorCodeStatus.Valid)
				{
					string text = supportedMember.name;
					if (!string.IsNullOrEmpty(text))
					{
						TMP_Text atmText = atmUI.atmText;
						atmText.text = atmText.text + "\n\nTHIS PURCHASE SUPPORTED\n" + text + "!";
						foreach (CreatorCodeSmallDisplay smallDisplay in smallDisplays)
						{
							smallDisplay.SuccessfulPurchase(text);
						}
					}
				}
				atmUI.ATM_RightColumnButtonText[0].text = "";
				atmUI.ATM_RightColumnArrowText[0].enabled = false;
				atmUI.ATM_RightColumnButtonText[1].text = "";
				atmUI.ATM_RightColumnArrowText[1].enabled = false;
				atmUI.ATM_RightColumnButtonText[2].text = "";
				atmUI.ATM_RightColumnArrowText[2].enabled = false;
				atmUI.ATM_RightColumnButtonText[3].text = "";
				atmUI.ATM_RightColumnArrowText[3].enabled = false;
				atmUI.creatorCodeObject.SetActive(value: false);
				break;
			case ATMStages.Failure:
				atmUI.atmText.text = "PURCHASE CANCELLED. NO FUNDS WERE SPENT.";
				LocalisationManager.TryGetKeyForCurrentLocale("ATM_PURCHASE_CANCELLED", out result, atmUI.atmText.text);
				atmUI.atmText.text = result;
				atmUI.ATM_RightColumnButtonText[0].text = "";
				atmUI.ATM_RightColumnArrowText[0].enabled = false;
				atmUI.ATM_RightColumnButtonText[1].text = "";
				atmUI.ATM_RightColumnArrowText[1].enabled = false;
				atmUI.ATM_RightColumnButtonText[2].text = "";
				atmUI.ATM_RightColumnArrowText[2].enabled = false;
				atmUI.ATM_RightColumnButtonText[3].text = "";
				atmUI.ATM_RightColumnArrowText[3].enabled = false;
				atmUI.creatorCodeObject.SetActive(value: false);
				break;
			case ATMStages.SafeAccount:
				atmUI.atmText.text = "Out Of Order.";
				LocalisationManager.TryGetKeyForCurrentLocale("ATM_PURCHASING_DISABLED_OUT_OF_ORDER", out result, atmUI.atmText.text);
				atmUI.atmText.text = result;
				atmUI.ATM_RightColumnButtonText[0].text = "";
				atmUI.ATM_RightColumnArrowText[0].enabled = false;
				atmUI.ATM_RightColumnButtonText[1].text = "";
				atmUI.ATM_RightColumnArrowText[1].enabled = false;
				atmUI.ATM_RightColumnButtonText[2].text = "";
				atmUI.ATM_RightColumnArrowText[2].enabled = false;
				atmUI.ATM_RightColumnButtonText[3].text = "";
				atmUI.ATM_RightColumnArrowText[3].enabled = false;
				atmUI.creatorCodeObject.SetActive(value: false);
				break;
			}
		}
	}

	public void SetATMText(string newText)
	{
		foreach (ATM_UI atmUI in atmUIs)
		{
			atmUI.atmText.text = newText;
		}
	}

	public void PressCurrencyPurchaseButton(string currencyPurchaseSize)
	{
		ProcessATMState(currencyPurchaseSize);
	}

	public void VerifyCreatorCode()
	{
		creatorCodeStatus = CreatorCodeStatus.Validating;
		NexusManager.instance.VerifyCreatorCode(currentCreatorCode, OnCreatorCodeSucess, OnCreatorCodeFailure);
	}

	private void OnCreatorCodeSucess(Member member)
	{
		creatorCodeStatus = CreatorCodeStatus.Valid;
		supportedMember = member;
		ValidatedCreatorCode = currentCreatorCode;
		foreach (CreatorCodeSmallDisplay smallDisplay in smallDisplays)
		{
			smallDisplay.SetCode(ValidatedCreatorCode);
		}
		PlayerPrefs.SetString("CreatorCode", ValidatedCreatorCode);
		if (initialCode != ValidatedCreatorCode)
		{
			PlayerPrefs.SetString("CodeUsedTime", DateTime.Now.ToString());
		}
		PlayerPrefs.Save();
		Debug.Log("ATM CODE SUCCESS: " + supportedMember.name);
	}

	private void OnCreatorCodeFailure()
	{
		supportedMember = default(Member);
		ResetCreatorCode();
		foreach (ATM_UI atmUI in atmUIs)
		{
			atmUI.creatorCodeTitle.text = "CREATOR CODE: INVALID";
			LocalisationManager.TryGetKeyForCurrentLocale("ATM_CREATOR_CODE_INVALID", out var result, atmUI.atmText.text);
			atmUI.creatorCodeTitle.text = result;
		}
		Debug.Log("ATM CODE FAILURE");
	}

	public void LeaveSystemMenu()
	{
	}
}

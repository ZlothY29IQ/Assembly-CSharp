using System;
using System.Collections.Generic;
using GorillaNetworking;
using GorillaNetworking.Store;
using TMPro;
using UnityEngine;

public class ATM_Manager : MonoBehaviour, IBuildValidation
{
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

	public Dictionary<ATM_UI, Tuple<string, string>> atmUIToMemberCode = new Dictionary<ATM_UI, Tuple<string, string>>();

	[HideInInspector]
	public List<CreatorCodeSmallDisplay> smallDisplays;

	private ATMStages currentATMStage;

	public int numShinyRocksToBuy;

	public float shinyRocksCost;

	public bool alreadyBegan;

	[SerializeField]
	private NexusGroupId[] nexusGroups;

	private string _tempCreatorCodeOveride;

	private string ATM_TERMINAL_ID = "atm_terminal_id";

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
			atmUI.SetCreatorCodeTitle(result);
		}
		SwitchToStage(ATMStages.Unavailable);
		smallDisplays = new List<CreatorCodeSmallDisplay>();
		ATM_TERMINAL_ID = string.Empty;
		for (int i = 0; i < nexusGroups.Length; i++)
		{
			ATM_TERMINAL_ID += nexusGroups[i];
		}
		HookupToCreatorCodes();
	}

	public void Start()
	{
		Debug.Log("ATM COUNT: " + atmUIs.Count);
		Debug.Log("SMALL DISPLAY COUNT: " + smallDisplays.Count);
		GameEvents.OnGorrillaATMKeyButtonPressedEvent.AddListener(PressButton);
	}

	public void HookupToCreatorCodes()
	{
		CreatorCodes.InitializedEvent += CreatorCodesInitialized;
		CreatorCodes.OnCreatorCodeChangedEvent += OnCreatorCodeChanged;
		CreatorCodes.OnCreatorCodeFailureEvent += OnOnCreatorCodeFailureEvent;
		if (CreatorCodes.Intialized)
		{
			CreatorCodesInitialized();
		}
	}

	public void CreatorCodesInitialized()
	{
		foreach (CreatorCodeSmallDisplay smallDisplay in smallDisplays)
		{
			smallDisplay.SetCode(CreatorCodes.getCurrentCreatorCode(ATM_TERMINAL_ID));
		}
		foreach (ATM_UI atmUI in atmUIs)
		{
			atmUI.SetCreatorCodeField(CreatorCodes.getCurrentCreatorCode(ATM_TERMINAL_ID));
		}
	}

	public void OnCreatorCodeChanged(string id)
	{
		if (id != ATM_TERMINAL_ID)
		{
			return;
		}
		foreach (CreatorCodeSmallDisplay smallDisplay in smallDisplays)
		{
			smallDisplay.SetCode(CreatorCodes.getCurrentCreatorCode(ATM_TERMINAL_ID));
		}
		foreach (ATM_UI atmUI in atmUIs)
		{
			atmUI.SetCreatorCodeField(CreatorCodes.getCurrentCreatorCode(ATM_TERMINAL_ID));
		}
		string text = "CREATOR CODE:";
		switch (CreatorCodes.getCurrentCreatorCodeStatus(ATM_TERMINAL_ID))
		{
		case CreatorCodes.CreatorCodeStatus.Valid:
			text += " VALID";
			break;
		case CreatorCodes.CreatorCodeStatus.Validating:
			text += " VALIDATING";
			break;
		}
		foreach (ATM_UI atmUI2 in atmUIs)
		{
			atmUI2.SetCreatorCodeTitle(text);
		}
	}

	private void OnOnCreatorCodeFailureEvent(string id)
	{
		if (id != ATM_TERMINAL_ID)
		{
			return;
		}
		foreach (ATM_UI atmUI in atmUIs)
		{
			atmUI.SetCreatorCodeTitle("CREATOR CODE: INVALID");
			LocalisationManager.TryGetKeyForCurrentLocale("ATM_CREATOR_CODE_INVALID", out var result, atmUI.atmText.text);
			atmUI.SetCreatorCodeTitle(result);
		}
		Debug.Log("ATM CODE FAILURE");
	}

	public void OnCreatorCodeInvalid(string id)
	{
		if (id != ATM_TERMINAL_ID)
		{
			return;
		}
		foreach (ATM_UI atmUI in atmUIs)
		{
			atmUI.SetCreatorCodeTitle("CREATOR CODE: INVALID");
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
		if (currentATMStage != ATMStages.Confirm || CreatorCodes.getCurrentCreatorCodeStatus(ATM_TERMINAL_ID) == CreatorCodes.CreatorCodeStatus.Validating)
		{
			return;
		}
		string defaultResult = "CREATOR CODE: ";
		LocalisationManager.TryGetKeyForCurrentLocale("ATM_CREATOR_CODE", out var result, defaultResult);
		foreach (ATM_UI atmUI in atmUIs)
		{
			atmUI.SetCreatorCodeTitle(result);
		}
		if (buttonPressed == GorillaATMKeyBindings.delete)
		{
			CreatorCodes.DeleteCharacter(ATM_TERMINAL_ID);
			return;
		}
		string aTM_TERMINAL_ID = ATM_TERMINAL_ID;
		string input;
		if (buttonPressed >= GorillaATMKeyBindings.delete)
		{
			input = buttonPressed.ToString();
		}
		else
		{
			int num = (int)buttonPressed;
			input = num.ToString();
		}
		CreatorCodes.AppendKey(aTM_TERMINAL_ID, input);
	}

	public async void ProcessATMState(ATM_UI atm_ui, string currencyButton)
	{
		switch (currentATMStage)
		{
		case ATMStages.Begin:
			SwitchToStage(ATMStages.Menu);
			break;
		case ATMStages.Menu:
			if (PlayFabAuthenticator.instance.GetSafety())
			{
				string text = currencyButton;
				if (!(text == "one"))
				{
					if (text == "four")
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
		{
			string text = currencyButton;
			if (!(text == "one"))
			{
				if (text == "back")
				{
					SwitchToStage(ATMStages.Choose);
				}
				break;
			}
			if (atm_ui != null)
			{
				CosmeticsController.instance.PurchaseLocation = atm_ui.PurchaseLocation;
			}
			if (atm_ui != null && atmUIToMemberCode.ContainsKey(atm_ui))
			{
				SwitchToStage(ATMStages.Purchasing);
				CosmeticsController.instance.SetValidatedCreatorCode(atmUIToMemberCode[atm_ui].Item1, atmUIToMemberCode[atm_ui].Item2, string.Empty);
				CosmeticsController.instance.SteamPurchase();
				break;
			}
			if (CreatorCodes.getCurrentCreatorCodeStatus(ATM_TERMINAL_ID) == CreatorCodes.CreatorCodeStatus.Empty)
			{
				CosmeticsController.instance.SteamPurchase();
				SwitchToStage(ATMStages.Purchasing);
				break;
			}
			CreatorCodeValidating();
			NexusManager.MemberCode memberCode = await CreatorCodes.CheckValidationCoroutineJIT(ATM_TERMINAL_ID, CreatorCodes.getCurrentCreatorCode(ATM_TERMINAL_ID), nexusGroups);
			if (memberCode != null)
			{
				SwitchToStage(ATMStages.Purchasing);
				CosmeticsController.instance.SetValidatedCreatorCode(memberCode.memberCode, memberCode.groupId.Code, ATM_TERMINAL_ID);
				CosmeticsController.instance.SteamPurchase();
			}
			else
			{
				OnCreatorCodeInvalid(ATM_TERMINAL_ID);
			}
			break;
		}
		default:
			SwitchToStage(ATMStages.Menu);
			break;
		case ATMStages.Unavailable:
		case ATMStages.Purchasing:
			break;
		}
	}

	public void AddATM(ATM_UI newATM, Tuple<string, string> creatorCode)
	{
		atmUIs.Add(newATM);
		if (creatorCode != null)
		{
			atmUIToMemberCode.Add(newATM, creatorCode);
		}
		else
		{
			newATM.SetCreatorCodeField(CreatorCodes.getCurrentCreatorCode(ATM_TERMINAL_ID));
		}
		SwitchToStage(currentATMStage);
	}

	public void RemoveATM(ATM_UI atmToRemove)
	{
		atmUIs.Remove(atmToRemove);
	}

	public void CreatorCodeValidating()
	{
		foreach (ATM_UI atmUI in atmUIs)
		{
			atmUI.SetCreatorCodeTitle("CREATOR CODE: VALIDATING");
		}
	}

	public void CreatorCodeValid()
	{
		foreach (ATM_UI atmUI in atmUIs)
		{
			atmUI.SetCreatorCodeTitle("CREATOR CODE: VALIDATING");
		}
		if (currentATMStage == ATMStages.Confirm)
		{
			SwitchToStage(ATMStages.Purchasing);
		}
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
				atmUI.HideCreatorCode();
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
				atmUI.HideCreatorCode();
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
					atmUI.HideCreatorCode();
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
					atmUI.HideCreatorCode();
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
				atmUI.HideCreatorCode();
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
				atmUI.HideCreatorCode();
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
				atmUI.ShowCreatorCode();
				break;
			case ATMStages.Purchasing:
				atmUI.atmText.text = "PURCHASING IN STEAM...";
				LocalisationManager.TryGetKeyForCurrentLocale("ATM_PURCHASING", out result, atmUI.atmText.text);
				atmUI.atmText.text = result;
				atmUI.HideCreatorCode();
				break;
			case ATMStages.Success:
				atmUI.atmText.text = "SUCCESS! NEW SHINY ROCKS BALANCE: " + (CosmeticsController.instance.CurrencyBalance + numShinyRocksToBuy);
				LocalisationManager.TryGetKeyForCurrentLocale("ATM_SUCCESS_NEW_BALANCE", out result, atmUI.atmText.text);
				atmUI.atmText.text = result + (CosmeticsController.instance.CurrencyBalance + numShinyRocksToBuy);
				if (CreatorCodes.getCurrentCreatorCodeStatus(ATM_TERMINAL_ID) == CreatorCodes.CreatorCodeStatus.Valid)
				{
					string text = CreatorCodes.supportedMember.name;
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
				atmUI.HideCreatorCode();
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
				atmUI.HideCreatorCode();
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
				atmUI.HideCreatorCode();
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

	public void PressCurrencyPurchaseButton(ATM_UI atm_ui, string currencyPurchaseSize)
	{
		ProcessATMState(atm_ui, currencyPurchaseSize);
	}

	public void LeaveSystemMenu()
	{
	}

	bool IBuildValidation.BuildValidationCheck()
	{
		if (nexusGroups.Length == 0)
		{
			Debug.LogError("You have to set at least one nexusGroup in " + base.name + " or things will not work!");
			return false;
		}
		return true;
	}

	internal void SetTemporaryCreatorCode(string code)
	{
		if (code == null)
		{
			CreatorCodes.ResetCreatorCode(ATM_TERMINAL_ID);
			CreatorCodes.AppendKey(ATM_TERMINAL_ID, _tempCreatorCodeOveride);
			_tempCreatorCodeOveride = null;
			return;
		}
		if (_tempCreatorCodeOveride == null)
		{
			_tempCreatorCodeOveride = CreatorCodes.getCurrentCreatorCode(ATM_TERMINAL_ID);
		}
		CreatorCodes.ResetCreatorCode(ATM_TERMINAL_ID);
		CreatorCodes.AppendKey(ATM_TERMINAL_ID, code);
	}
}

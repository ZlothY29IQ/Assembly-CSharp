using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GorillaNetworking;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

namespace GorillaTagScripts.GhostReactor;

public class GRKiosk : MonoBehaviour
{
	private enum PurchaseState
	{
		Initialize,
		AlreadyOwned,
		AvailableForPurchase,
		CheckoutPressed,
		CheckoutConfirmation
	}

	private enum ButtonSide
	{
		Left,
		Right
	}

	[SerializeField]
	public string CosmeticNameForPurchase;

	[SerializeField]
	public GorillaPressableButton LeftPurchaseButton;

	[SerializeField]
	public GorillaPressableButton RightPurchaseButton;

	[SerializeField]
	public TMP_Text PurchaseText;

	private CosmeticsController.CosmeticItem _cosmeticForPurchase;

	[SerializeField]
	private AudioSource _audioSource;

	[SerializeField]
	private AudioClip _purchaseAudioClip;

	[SerializeField]
	private ParticleSystem _purchaseParticles;

	[SerializeField]
	private LocalizedText _purchaseTextLoc;

	private LocalizedString _purchaseTextLocStr;

	private StringVariable _itemNameVar;

	private IntVariable _itemCostVar;

	private IntVariable _currencyBalanceVar;

	private PurchaseState _purchaseState;

	private async void Start()
	{
		if (string.IsNullOrEmpty(CosmeticNameForPurchase))
		{
			Debug.LogError("No cosmetic set for GRKiosk.");
			UnityEngine.Object.Destroy(this);
			return;
		}
		while (!Application.isPlaying || CosmeticsController.instance == null || CosmeticsController.instance.allCosmetics == null)
		{
			await Task.Yield();
		}
		if (_purchaseTextLoc != null)
		{
			_purchaseTextLocStr = _purchaseTextLoc.StringReference;
			_itemNameVar = _purchaseTextLocStr["item-name"] as StringVariable;
			_itemCostVar = _purchaseTextLocStr["item-cost"] as IntVariable;
			_currencyBalanceVar = _purchaseTextLocStr["currency-balance"] as IntVariable;
		}
		else
		{
			_purchaseTextLocStr = new LocalizedString();
			_itemNameVar = new StringVariable();
			_itemCostVar = new IntVariable();
			_currencyBalanceVar = new IntVariable();
		}
		if (_purchaseParticles != null)
		{
			_purchaseParticles.Stop();
		}
		CosmeticsController instance = CosmeticsController.instance;
		instance.OnGetCurrency = (Action)Delegate.Combine(instance.OnGetCurrency, new Action(OnGetCurrency));
		LeftPurchaseButton.onPressed += OnLeftPurchaseButtonPressed;
		RightPurchaseButton.onPressed += OnRightPurchaseButtonPressed;
		_cosmeticForPurchase = CosmeticsController.instance.allCosmetics.Find(MatchesCosmeticForPurchase);
		_purchaseState = (PlayerOwnsItem() ? PurchaseState.AlreadyOwned : PurchaseState.AvailableForPurchase);
		ProcessPurchaseItemState(null);
	}

	private void ProcessPurchaseItemState(ButtonSide? button, HashSet<PurchaseState> recentStates = null)
	{
		if (recentStates == null)
		{
			recentStates = new HashSet<PurchaseState>();
		}
		recentStates.Add(_purchaseState);
		switch (_purchaseState)
		{
		case PurchaseState.Initialize:
			throw new Exception("ProcessPurchaseItemState called in non-initialized GRKiosk!");
		case PurchaseState.AlreadyOwned:
			ResetButtons();
			break;
		case PurchaseState.AvailableForPurchase:
			SetAvailableForPurchaseDisplays(button);
			break;
		case PurchaseState.CheckoutPressed:
			SetCheckoutConfirmationDisplays(button);
			break;
		case PurchaseState.CheckoutConfirmation:
			ConfirmCheckout(button);
			break;
		}
		if (!recentStates.Contains(_purchaseState))
		{
			ProcessPurchaseItemState(null, recentStates);
		}
		FormattedPurchaseText();
	}

	private bool PlayerOwnsItem()
	{
		return CosmeticsController.instance.unlockedCosmetics.Any(MatchesCosmeticForPurchase);
	}

	private void OnGetCurrency()
	{
		ProcessPurchaseItemState(null);
	}

	private void ResetButtons()
	{
		LeftPurchaseButton.myTmpText.text = "-";
		RightPurchaseButton.myTmpText.text = "-";
		LeftPurchaseButton.buttonRenderer.material = LeftPurchaseButton.pressedMaterial;
		RightPurchaseButton.buttonRenderer.material = RightPurchaseButton.pressedMaterial;
	}

	private void SetAvailableForPurchaseDisplays(ButtonSide? button)
	{
		if (_cosmeticForPurchase.cost <= CosmeticsController.instance.currencyBalance)
		{
			LocalisationManager.TryGetKeyForCurrentLocale("MONKE_BLOCKS_PURCHASE_BUTTON_WANT_TO_BUY_CANCEL", out var result, "NO!");
			LocalisationManager.TryGetKeyForCurrentLocale("MONKE_BLOCKS_PURCHASE_BUTTON_WANT_TO_BUY_CONFIRM", out var result2, "YES!");
			LeftPurchaseButton.myTmpText.text = result;
			RightPurchaseButton.myTmpText.text = result2;
			LeftPurchaseButton.buttonRenderer.material = LeftPurchaseButton.unpressedMaterial;
			RightPurchaseButton.buttonRenderer.material = RightPurchaseButton.unpressedMaterial;
			if (button == ButtonSide.Right)
			{
				_purchaseState = PurchaseState.CheckoutPressed;
			}
		}
		else
		{
			LeftPurchaseButton.myTmpText.text = "-";
			RightPurchaseButton.myTmpText.text = "-";
			LeftPurchaseButton.buttonRenderer.material = LeftPurchaseButton.pressedMaterial;
			RightPurchaseButton.buttonRenderer.material = RightPurchaseButton.pressedMaterial;
			_purchaseState = PurchaseState.AvailableForPurchase;
		}
	}

	private void SetCheckoutConfirmationDisplays(ButtonSide? button)
	{
		LocalisationManager.TryGetKeyForCurrentLocale("MONKE_BLOCKS_PURCHASE_BUTTON_CONFIRMATION_CANCEL", out var result, "LET ME THINK ABOUT IT");
		LocalisationManager.TryGetKeyForCurrentLocale("MONKE_BLOCKS_PURCHASE_BUTTON_CONFIRMATION_CONFIRM", out var result2, "YES! I NEED IT!");
		LeftPurchaseButton.myTmpText.text = result2;
		RightPurchaseButton.myTmpText.text = result;
		LeftPurchaseButton.buttonRenderer.material = LeftPurchaseButton.unpressedMaterial;
		RightPurchaseButton.buttonRenderer.material = RightPurchaseButton.unpressedMaterial;
		_purchaseState = PurchaseState.CheckoutConfirmation;
	}

	private void ConfirmCheckout(ButtonSide? button)
	{
		if (button == ButtonSide.Left)
		{
			PurchaseItem();
		}
		else if (button == ButtonSide.Right)
		{
			_purchaseState = PurchaseState.AvailableForPurchase;
		}
	}

	private void PurchaseItem()
	{
		PlayFabClientAPI.PurchaseItem(new PurchaseItemRequest
		{
			ItemId = _cosmeticForPurchase.itemName,
			Price = _cosmeticForPurchase.cost,
			VirtualCurrency = "SR"
		}, delegate(PurchaseItemResult result)
		{
			_purchaseState = ((result.Items.Count > 0) ? PurchaseState.AlreadyOwned : PurchaseState.AvailableForPurchase);
			if (_purchaseParticles != null)
			{
				_purchaseParticles.Play();
			}
			GorillaTagger.Instance.offlineVRRig.AddCosmetic(_cosmeticForPurchase.itemName);
			ProcessPurchaseItemState(null);
		}, delegate(PlayFabError error)
		{
			Debug.LogError(error.ToString());
		});
	}

	private bool MatchesCosmeticForPurchase(CosmeticsController.CosmeticItem item)
	{
		if (!(CosmeticNameForPurchase == item.displayName) && !(CosmeticNameForPurchase == item.overrideDisplayName))
		{
			return CosmeticNameForPurchase == item.itemName;
		}
		return true;
	}

	private void OnLeftPurchaseButtonPressed(GorillaPressableButton button, bool isLeftHand)
	{
		ProcessPurchaseItemState(ButtonSide.Left);
	}

	private void OnRightPurchaseButtonPressed(GorillaPressableButton button, bool isLeftHand)
	{
		ProcessPurchaseItemState(ButtonSide.Right);
	}

	private void FormattedPurchaseText()
	{
		if (_itemNameVar == null || _itemCostVar == null || _currencyBalanceVar == null)
		{
			Debug.LogError("[LOCALIZATION::GRKIOSK] One of the dynamic variables is NULL and cannot update the [PurchaseText] screen");
			return;
		}
		_itemNameVar.Value = _cosmeticForPurchase.displayName.ToUpper();
		_itemCostVar.Value = _cosmeticForPurchase.cost;
		_currencyBalanceVar.Value = CosmeticsController.instance.currencyBalance;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("ITEM: ").Append(_cosmeticForPurchase.overrideDisplayName.ToUpper());
		stringBuilder.Append("\nITEM COST: ").Append(_cosmeticForPurchase.cost);
		stringBuilder.Append("\nYOU HAVE: ").Append(CosmeticsController.instance.currencyBalance);
		StringBuilder stringBuilder2 = stringBuilder.Append("\n");
		stringBuilder2.Append(_purchaseState switch
		{
			PurchaseState.AlreadyOwned => "YOU ALREADY OWN THIS!", 
			PurchaseState.AvailableForPurchase => "PURCHASE?", 
			PurchaseState.CheckoutPressed => "CONFIRM PURCHASE?", 
			PurchaseState.CheckoutConfirmation => "CONFIRMING PURCHASE...", 
			_ => "ERROR", 
		});
		PurchaseText.text = stringBuilder.ToString();
	}
}

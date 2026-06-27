using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameObjectScheduling;
using GorillaNetworking;
using GorillaTagScripts;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

public class BuilderKiosk : MonoBehaviour
{
	public const string MONKE_BLOCKS_PURCHASE_BUTTON_WANT_TO_BUY_CONFIRM_KEY = "MONKE_BLOCKS_PURCHASE_BUTTON_WANT_TO_BUY_CONFIRM";

	public const string MONKE_BLOCKS_PURCHASE_BUTTON_WANT_TO_BUY_CANCEL_KEY = "MONKE_BLOCKS_PURCHASE_BUTTON_WANT_TO_BUY_CANCEL";

	public const string MONKE_BLOCKS_PURCHASE_BUTTON_CONFIRMATION_CONFIRM_KEY = "MONKE_BLOCKS_PURCHASE_BUTTON_CONFIRMATION_CONFIRM";

	public const string MONKE_BLOCKS_PURCHASE_BUTTON_CONFIRMATION_CANCEL_KEY = "MONKE_BLOCKS_PURCHASE_BUTTON_CONFIRMATION_CANCEL";

	public BuilderPieceSet pieceSetForSale;

	public GorillaPressableButton leftPurchaseButton;

	public GorillaPressableButton rightPurchaseButton;

	public TMP_Text purchaseText;

	[SerializeField]
	private bool isMiniKiosk;

	[SerializeField]
	private bool useTitleCountDown = true;

	[Header("Buttons")]
	[SerializeField]
	private GorillaPressableButton[] setButtons;

	[SerializeField]
	private GorillaPressableButton previousPageButton;

	[SerializeField]
	private GorillaPressableButton nextPageButton;

	private BuilderPieceSet currentSet;

	private int pageIndex;

	private int setsPerPage = 3;

	private int totalPages = 1;

	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private AudioClip purchaseSetAudioClip;

	[SerializeField]
	private ParticleSystem purchaseParticles;

	[SerializeField]
	private GameObject emptyDisplay;

	private List<BuilderPieceSet> availableItems = new List<BuilderPieceSet>(10);

	internal CosmeticsController.PurchaseItemStages currentPurchaseItemStage;

	private bool hasInitFromPlayfab;

	internal BuilderSetManager.BuilderSetStoreItem itemToBuy;

	public static BuilderSetManager.BuilderSetStoreItem nullItem;

	private GameObject currentDiorama;

	private GameObject nextDiorama;

	private bool animating;

	[SerializeField]
	private Transform itemDisplayPos;

	[SerializeField]
	private Transform nextItemDisplayPos;

	[SerializeField]
	private Animation itemDisplayAnimation;

	[SerializeField]
	private CountdownText countdownText;

	private string countdownOverride = string.Empty;

	private bool isLastHandTouchedLeft;

	private string finalLine;

	[Header("Localization")]
	[SerializeField]
	private LocalizedText _puchaseTextLoc;

	private LocalizedString _puchaseTextLocStr;

	private StringVariable _itemNameVar;

	private IntVariable _finalLineVar;

	private IntVariable _itemCostVar;

	private IntVariable _currencyBalanceVar;

	private void Awake()
	{
		nullItem = new BuilderSetManager.BuilderSetStoreItem
		{
			displayName = "NOTHING",
			playfabID = "NULL",
			isNullItem = true
		};
	}

	private async void Start()
	{
		_puchaseTextLocStr = _puchaseTextLoc.StringReference;
		_itemNameVar = _puchaseTextLocStr["item-name"] as StringVariable;
		_itemCostVar = _puchaseTextLocStr["item-cost"] as IntVariable;
		_currencyBalanceVar = _puchaseTextLocStr["currency-balance"] as IntVariable;
		_finalLineVar = _puchaseTextLocStr["final-line-index"] as IntVariable;
		purchaseParticles.Stop();
		BuilderSetManager.instance.OnOwnedSetsUpdated.AddListener(OnOwnedSetsUpdated);
		CosmeticsController instance = CosmeticsController.instance;
		instance.OnGetCurrency = (Action)Delegate.Combine(instance.OnGetCurrency, new Action(OnUpdateCurrencyBalance));
		leftPurchaseButton.onPressed += PressLeftPurchaseItemButton;
		rightPurchaseButton.onPressed += PressRightPurchaseItemButton;
		if (BuilderTable.TryGetBuilderTableForZone(GTZone.monkeBlocks, out var table))
		{
			table.OnTableConfigurationUpdated.AddListener(UpdateCountdown);
		}
		UpdateCountdown();
		availableItems.Clear();
		if (isMiniKiosk)
		{
			availableItems.Add(pieceSetForSale);
		}
		else
		{
			availableItems.AddRange(BuilderSetManager.instance.GetPermanentSetsForSale());
			availableItems.AddRange(BuilderSetManager.instance.GetSeasonalSetsForSale());
		}
		if (!isMiniKiosk)
		{
			SetupSetButtons();
		}
		await Task.Delay(TimeSpan.FromSeconds(5.0));
		Debug.Log("Task await complete.");
		if (availableItems.Count > 0 && BuilderSetManager.instance.pulledStoreItems)
		{
			hasInitFromPlayfab = true;
			if (pieceSetForSale != null)
			{
				itemToBuy = BuilderSetManager.instance.GetStoreItemFromSetID(pieceSetForSale.GetIntIdentifier());
				UpdateLabels();
				UpdateDiorama();
				currentPurchaseItemStage = CosmeticsController.PurchaseItemStages.CheckoutButtonPressed;
				ProcessPurchaseItemState(null, isLeftHand: false);
			}
			else
			{
				itemToBuy = nullItem;
				UpdateLabels();
				UpdateDiorama();
				currentPurchaseItemStage = CosmeticsController.PurchaseItemStages.Start;
				ProcessPurchaseItemState(null, isLeftHand: false);
			}
		}
		else
		{
			itemToBuy = nullItem;
		}
	}

	private void UpdateCountdown()
	{
		if (useTitleCountDown && !string.IsNullOrEmpty(BuilderTable.nextUpdateOverride) && !BuilderTable.nextUpdateOverride.Equals(countdownOverride))
		{
			countdownOverride = BuilderTable.nextUpdateOverride;
			CountdownTextDate countdown = countdownText.Countdown;
			countdown.CountdownTo = countdownOverride;
			countdownText.Countdown = countdown;
		}
	}

	private void SetupSetButtons()
	{
		setsPerPage = setButtons.Length;
		totalPages = availableItems.Count / setsPerPage;
		if (availableItems.Count % setsPerPage > 0)
		{
			totalPages++;
		}
		previousPageButton.gameObject.SetActive(totalPages > 1);
		nextPageButton.gameObject.SetActive(totalPages > 1);
		previousPageButton.myTmpText.enabled = totalPages > 1;
		nextPageButton.myTmpText.enabled = totalPages > 1;
		previousPageButton.onPressButton.AddListener(OnPreviousPageClicked);
		nextPageButton.onPressButton.AddListener(OnNextPageClicked);
		GorillaPressableButton[] array = setButtons;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].onPressed += OnSetButtonPressed;
		}
		UpdateLabels();
	}

	private void OnDestroy()
	{
		if (leftPurchaseButton != null)
		{
			leftPurchaseButton.onPressed -= PressLeftPurchaseItemButton;
		}
		if (rightPurchaseButton != null)
		{
			rightPurchaseButton.onPressed -= PressRightPurchaseItemButton;
		}
		if (BuilderSetManager.instance != null)
		{
			BuilderSetManager.instance.OnOwnedSetsUpdated.RemoveListener(OnOwnedSetsUpdated);
		}
		if (CosmeticsController.instance != null)
		{
			CosmeticsController instance = CosmeticsController.instance;
			instance.OnGetCurrency = (Action)Delegate.Remove(instance.OnGetCurrency, new Action(OnUpdateCurrencyBalance));
		}
		if (!isMiniKiosk)
		{
			GorillaPressableButton[] array = setButtons;
			foreach (GorillaPressableButton gorillaPressableButton in array)
			{
				if (gorillaPressableButton != null)
				{
					gorillaPressableButton.onPressed -= OnSetButtonPressed;
				}
			}
			if (previousPageButton != null)
			{
				previousPageButton.onPressButton.RemoveListener(OnPreviousPageClicked);
			}
			if (nextPageButton != null)
			{
				nextPageButton.onPressButton.RemoveListener(OnNextPageClicked);
			}
		}
		if (currentDiorama != null)
		{
			UnityEngine.Object.Destroy(currentDiorama);
			currentDiorama = null;
		}
		if (nextDiorama != null)
		{
			UnityEngine.Object.Destroy(nextDiorama);
			nextDiorama = null;
		}
		if (BuilderTable.TryGetBuilderTableForZone(GTZone.monkeBlocks, out var table))
		{
			table.OnTableConfigurationUpdated.RemoveListener(UpdateCountdown);
		}
	}

	private void OnOwnedSetsUpdated()
	{
		if (!hasInitFromPlayfab && BuilderSetManager.instance.pulledStoreItems)
		{
			hasInitFromPlayfab = true;
			availableItems.Clear();
			if (isMiniKiosk)
			{
				availableItems.Add(pieceSetForSale);
			}
			else
			{
				availableItems.AddRange(BuilderSetManager.instance.GetPermanentSetsForSale());
				availableItems.AddRange(BuilderSetManager.instance.GetSeasonalSetsForSale());
			}
			if (pieceSetForSale != null)
			{
				itemToBuy = BuilderSetManager.instance.GetStoreItemFromSetID(pieceSetForSale.GetIntIdentifier());
				UpdateLabels();
				UpdateDiorama();
				currentPurchaseItemStage = CosmeticsController.PurchaseItemStages.CheckoutButtonPressed;
				ProcessPurchaseItemState(null, isLeftHand: false);
			}
			else
			{
				currentPurchaseItemStage = CosmeticsController.PurchaseItemStages.Start;
				ProcessPurchaseItemState(null, isLeftHand: false);
			}
		}
		else if (currentPurchaseItemStage == CosmeticsController.PurchaseItemStages.Start || currentPurchaseItemStage == CosmeticsController.PurchaseItemStages.CheckoutButtonPressed)
		{
			ProcessPurchaseItemState(null, isLeftHand: false);
		}
	}

	private void OnSetButtonPressed(GorillaPressableButton button, bool isLeft)
	{
		if (currentPurchaseItemStage == CosmeticsController.PurchaseItemStages.Buying || animating)
		{
			return;
		}
		currentPurchaseItemStage = CosmeticsController.PurchaseItemStages.CheckoutButtonPressed;
		int num = 0;
		for (int i = 0; i < setButtons.Length; i++)
		{
			if (button.Equals(setButtons[i]))
			{
				num = i;
				break;
			}
		}
		int num2 = pageIndex * setsPerPage + num;
		if (num2 < availableItems.Count)
		{
			BuilderPieceSet builderPieceSet = availableItems[num2];
			if (builderPieceSet.SetName.Equals(itemToBuy.displayName))
			{
				itemToBuy = nullItem;
				currentPurchaseItemStage = CosmeticsController.PurchaseItemStages.Start;
			}
			else
			{
				itemToBuy = BuilderSetManager.instance.GetStoreItemFromSetID(builderPieceSet.GetIntIdentifier());
				UpdateLabels();
				UpdateDiorama();
			}
			ProcessPurchaseItemState(null, isLeft);
		}
	}

	private void OnPreviousPageClicked()
	{
		int num = Mathf.Clamp(pageIndex - 1, 0, totalPages - 1);
		if (num != pageIndex)
		{
			pageIndex = num;
			UpdateLabels();
		}
	}

	private void OnNextPageClicked()
	{
		int num = Mathf.Clamp(pageIndex + 1, 0, totalPages - 1);
		if (num != pageIndex)
		{
			pageIndex = num;
			UpdateLabels();
		}
	}

	private void UpdateLabels()
	{
		if (isMiniKiosk)
		{
			return;
		}
		for (int i = 0; i < setButtons.Length; i++)
		{
			int num = pageIndex * setsPerPage + i;
			if (num < availableItems.Count && availableItems[num] != null)
			{
				if (!setButtons[i].gameObject.activeSelf)
				{
					setButtons[i].gameObject.SetActive(value: true);
					setButtons[i].myTmpText.gameObject.SetActive(value: true);
				}
				if (setButtons[i].myTmpText.text != availableItems[num].SetName.ToUpper())
				{
					setButtons[i].myTmpText.text = availableItems[num].SetName.ToUpper();
				}
				bool flag = !itemToBuy.isNullItem && availableItems[num].playfabID == itemToBuy.playfabID;
				if (flag != setButtons[i].isOn || !setButtons[i].enabled)
				{
					setButtons[i].isOn = flag;
					setButtons[i].buttonRenderer.material = (flag ? setButtons[i].pressedMaterial : setButtons[i].unpressedMaterial);
				}
				setButtons[i].enabled = true;
			}
			else
			{
				if (setButtons[i].gameObject.activeSelf)
				{
					setButtons[i].gameObject.SetActive(value: false);
					setButtons[i].myTmpText.gameObject.SetActive(value: false);
				}
				if (setButtons[i].isOn || setButtons[i].enabled)
				{
					setButtons[i].isOn = false;
					setButtons[i].enabled = false;
				}
			}
		}
		bool flag2 = pageIndex > 0 && totalPages > 1;
		bool flag3 = pageIndex < totalPages - 1 && totalPages > 1;
		if (previousPageButton.myTmpText.enabled != flag2)
		{
			previousPageButton.myTmpText.enabled = flag2;
		}
		if (nextPageButton.myTmpText.enabled != flag3)
		{
			nextPageButton.myTmpText.enabled = flag3;
		}
	}

	public void UpdateDiorama()
	{
		if (isMiniKiosk || !base.gameObject.activeInHierarchy)
		{
			return;
		}
		if (itemToBuy.isNullItem)
		{
			countdownText.gameObject.SetActive(value: false);
		}
		else
		{
			countdownText.gameObject.SetActive(BuilderSetManager.instance.IsSetSeasonal(itemToBuy.playfabID));
		}
		if (animating)
		{
			StopCoroutine(PlaySwapAnimation());
			if (currentDiorama != null)
			{
				UnityEngine.Object.Destroy(currentDiorama);
				currentDiorama = null;
			}
			currentDiorama = nextDiorama;
			nextDiorama = null;
		}
		animating = true;
		if (nextDiorama != null)
		{
			UnityEngine.Object.Destroy(nextDiorama);
			nextDiorama = null;
		}
		if (!itemToBuy.isNullItem && itemToBuy.displayModel != null)
		{
			nextDiorama = UnityEngine.Object.Instantiate(itemToBuy.displayModel, nextItemDisplayPos);
		}
		else
		{
			nextDiorama = UnityEngine.Object.Instantiate(emptyDisplay, nextItemDisplayPos);
		}
		itemDisplayAnimation.Rewind();
		if (currentDiorama != null)
		{
			currentDiorama.transform.SetParent(itemDisplayPos, worldPositionStays: false);
		}
		StartCoroutine(PlaySwapAnimation());
	}

	private IEnumerator PlaySwapAnimation()
	{
		itemDisplayAnimation.Play();
		yield return new WaitForSeconds(itemDisplayAnimation.clip.length);
		if (currentDiorama != null)
		{
			UnityEngine.Object.Destroy(currentDiorama);
			currentDiorama = null;
		}
		currentDiorama = nextDiorama;
		nextDiorama = null;
		animating = false;
	}

	public void PressLeftPurchaseItemButton(GorillaPressableButton pressedPurchaseItemButton, bool isLeftHand)
	{
		if (currentPurchaseItemStage != CosmeticsController.PurchaseItemStages.Start && !animating)
		{
			ProcessPurchaseItemState("left", isLeftHand);
		}
	}

	public void PressRightPurchaseItemButton(GorillaPressableButton pressedPurchaseItemButton, bool isLeftHand)
	{
		if (currentPurchaseItemStage != CosmeticsController.PurchaseItemStages.Start && !animating)
		{
			ProcessPurchaseItemState("right", isLeftHand);
		}
	}

	public void OnUpdateCurrencyBalance()
	{
		if (currentPurchaseItemStage == CosmeticsController.PurchaseItemStages.Start || currentPurchaseItemStage == CosmeticsController.PurchaseItemStages.CheckoutButtonPressed || currentPurchaseItemStage == CosmeticsController.PurchaseItemStages.ItemOwned)
		{
			ProcessPurchaseItemState(null, isLeftHand: false);
		}
	}

	public void ClearCheckout()
	{
		GorillaTelemetry.PostBuilderKioskEvent(GorillaTagger.Instance.offlineVRRig, GTShopEventType.checkout_cancel, itemToBuy);
		itemToBuy = nullItem;
		currentPurchaseItemStage = CosmeticsController.PurchaseItemStages.Start;
	}

	public void ProcessPurchaseItemState(string buttonSide, bool isLeftHand)
	{
		switch (currentPurchaseItemStage)
		{
		case CosmeticsController.PurchaseItemStages.Start:
			itemToBuy = nullItem;
			FormattedPurchaseText(0);
			leftPurchaseButton.myTmpText.text = "-";
			rightPurchaseButton.myTmpText.text = "-";
			UpdateLabels();
			UpdateDiorama();
			break;
		case CosmeticsController.PurchaseItemStages.CheckoutButtonPressed:
			if (availableItems.Count > 1)
			{
				GorillaTelemetry.PostBuilderKioskEvent(GorillaTagger.Instance.offlineVRRig, GTShopEventType.checkout_start, itemToBuy);
			}
			if (BuilderSetManager.instance.IsPieceSetOwnedLocally(itemToBuy.setID))
			{
				FormattedPurchaseText(1);
				leftPurchaseButton.myTmpText.text = "-";
				rightPurchaseButton.myTmpText.text = "-";
				leftPurchaseButton.buttonRenderer.material = leftPurchaseButton.pressedMaterial;
				rightPurchaseButton.buttonRenderer.material = rightPurchaseButton.pressedMaterial;
				currentPurchaseItemStage = CosmeticsController.PurchaseItemStages.ItemOwned;
			}
			else if (itemToBuy.cost <= CosmeticsController.instance.currencyBalance)
			{
				FormattedPurchaseText(2);
				LocalisationManager.TryGetKeyForCurrentLocale("MONKE_BLOCKS_PURCHASE_BUTTON_WANT_TO_BUY_CANCEL", out var result, "NO!");
				LocalisationManager.TryGetKeyForCurrentLocale("MONKE_BLOCKS_PURCHASE_BUTTON_WANT_TO_BUY_CONFIRM", out var result2, "YES!");
				leftPurchaseButton.myTmpText.text = result;
				rightPurchaseButton.myTmpText.text = result2;
				leftPurchaseButton.buttonRenderer.material = leftPurchaseButton.unpressedMaterial;
				rightPurchaseButton.buttonRenderer.material = rightPurchaseButton.unpressedMaterial;
				currentPurchaseItemStage = CosmeticsController.PurchaseItemStages.ItemSelected;
			}
			else
			{
				FormattedPurchaseText(3);
				leftPurchaseButton.myTmpText.text = "-";
				rightPurchaseButton.myTmpText.text = "-";
				leftPurchaseButton.buttonRenderer.material = leftPurchaseButton.pressedMaterial;
				rightPurchaseButton.buttonRenderer.material = rightPurchaseButton.pressedMaterial;
				if (!isMiniKiosk)
				{
					currentPurchaseItemStage = CosmeticsController.PurchaseItemStages.Start;
				}
			}
			break;
		case CosmeticsController.PurchaseItemStages.ItemSelected:
			if (buttonSide == "right")
			{
				GorillaTelemetry.PostBuilderKioskEvent(GorillaTagger.Instance.offlineVRRig, GTShopEventType.item_select, itemToBuy);
				FormattedPurchaseText(4);
				LocalisationManager.TryGetKeyForCurrentLocale("MONKE_BLOCKS_PURCHASE_BUTTON_CONFIRMATION_CANCEL", out var result3, "LET ME THINK ABOUT IT");
				LocalisationManager.TryGetKeyForCurrentLocale("MONKE_BLOCKS_PURCHASE_BUTTON_CONFIRMATION_CONFIRM", out var result4, "YES! I NEED IT!");
				leftPurchaseButton.myTmpText.text = result4;
				rightPurchaseButton.myTmpText.text = result3;
				leftPurchaseButton.buttonRenderer.material = leftPurchaseButton.unpressedMaterial;
				rightPurchaseButton.buttonRenderer.material = rightPurchaseButton.unpressedMaterial;
				currentPurchaseItemStage = CosmeticsController.PurchaseItemStages.FinalPurchaseAcknowledgement;
			}
			else
			{
				currentPurchaseItemStage = CosmeticsController.PurchaseItemStages.CheckoutButtonPressed;
				ProcessPurchaseItemState(null, isLeftHand);
			}
			break;
		case CosmeticsController.PurchaseItemStages.FinalPurchaseAcknowledgement:
			if (buttonSide == "left")
			{
				FormattedPurchaseText(5);
				leftPurchaseButton.myTmpText.text = "-";
				rightPurchaseButton.myTmpText.text = "-";
				leftPurchaseButton.buttonRenderer.material = leftPurchaseButton.pressedMaterial;
				rightPurchaseButton.buttonRenderer.material = rightPurchaseButton.pressedMaterial;
				currentPurchaseItemStage = CosmeticsController.PurchaseItemStages.Buying;
				isLastHandTouchedLeft = isLeftHand;
				PurchaseItem();
			}
			else
			{
				currentPurchaseItemStage = CosmeticsController.PurchaseItemStages.CheckoutButtonPressed;
				ProcessPurchaseItemState(null, isLeftHand);
			}
			break;
		case CosmeticsController.PurchaseItemStages.Failure:
			FormattedPurchaseText(6);
			leftPurchaseButton.myTmpText.text = "-";
			rightPurchaseButton.myTmpText.text = "-";
			leftPurchaseButton.buttonRenderer.material = leftPurchaseButton.pressedMaterial;
			rightPurchaseButton.buttonRenderer.material = rightPurchaseButton.pressedMaterial;
			break;
		case CosmeticsController.PurchaseItemStages.Success:
			FormattedPurchaseText(7);
			audioSource.GTPlayOneShot(purchaseSetAudioClip);
			purchaseParticles.Play();
			GorillaTagger.Instance.offlineVRRig.AddCosmetic(itemToBuy.playfabID);
			leftPurchaseButton.myTmpText.text = "-";
			rightPurchaseButton.myTmpText.text = "-";
			leftPurchaseButton.buttonRenderer.material = leftPurchaseButton.pressedMaterial;
			rightPurchaseButton.buttonRenderer.material = rightPurchaseButton.pressedMaterial;
			break;
		case CosmeticsController.PurchaseItemStages.ItemOwned:
		case CosmeticsController.PurchaseItemStages.Buying:
			break;
		}
	}

	public void FormattedPurchaseText(int finalLineVar)
	{
		if (_itemNameVar == null || _itemCostVar == null || _currencyBalanceVar == null || _finalLineVar == null)
		{
			Debug.LogError("[LOCALIZATION::BUILDER_KIOSK] One of the dynamic variables is NULL and cannot update the [purchaseText] screen");
			return;
		}
		_itemNameVar.Value = itemToBuy.displayName.ToUpper();
		_itemCostVar.Value = (int)itemToBuy.cost;
		_currencyBalanceVar.Value = CosmeticsController.instance.currencyBalance;
		_finalLineVar.Value = finalLineVar;
	}

	public void PurchaseItem()
	{
		BuilderSetManager.instance.TryPurchaseItem(itemToBuy.setID, delegate(bool result)
		{
			if (result)
			{
				currentPurchaseItemStage = CosmeticsController.PurchaseItemStages.Success;
				CosmeticsController.instance.currencyBalance -= (int)itemToBuy.cost;
				ProcessPurchaseItemState(null, isLastHandTouchedLeft);
			}
			else
			{
				currentPurchaseItemStage = CosmeticsController.PurchaseItemStages.Failure;
				ProcessPurchaseItemState(null, isLeftHand: false);
			}
		});
	}
}

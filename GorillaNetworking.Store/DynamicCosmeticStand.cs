using System;
using System.Collections;
using GT_CustomMapSupportRuntime;
using GorillaExtensions;
using GorillaTag.CosmeticSystem;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GorillaNetworking.Store;

public class DynamicCosmeticStand : MonoBehaviour, iFlagForBaking
{
	public HeadModel_CosmeticStand DisplayHeadModel;

	public GorillaPressableButton AddToCartButton;

	[HideInInspector]
	public Text slotPriceText;

	[HideInInspector]
	public Text addToCartText;

	public TMP_Text slotPriceTextTMP;

	public TMP_Text addToCartTextTMP;

	private CosmeticsController.CosmeticItem thisCosmeticItem;

	[FormerlySerializedAs("StandID")]
	public string StandName;

	public string _thisCosmeticName = "";

	public GameObject GorillaHeadModel;

	public GameObject GorillaTorsoModel;

	public GameObject GorillaTorsoPostModel;

	public GameObject GorillaMannequinModel;

	public GameObject GuitarStandModel;

	public GameObject GuitarStandMount;

	public GameObject JeweleryBoxModel;

	public GameObject JeweleryBoxMount;

	public GameObject TableMount;

	[FormerlySerializedAs("PinDisplayMounnt")]
	[FormerlySerializedAs("PinDisplayMountn")]
	public GameObject PinDisplayMount;

	public GameObject root;

	public GameObject TagEffectDisplayMount;

	public GameObject TageEffectDisplayModel;

	private Scene customMapScene;

	[HideInInspector]
	public StoreDisplay parentDisplay;

	[HideInInspector]
	public StoreDepartment parentDepartment;

	private int searchIndex;

	public string thisCosmeticName
	{
		get
		{
			return _thisCosmeticName;
		}
		set
		{
			_thisCosmeticName = value;
		}
	}

	public virtual void SetForBaking()
	{
		GorillaHeadModel.SetActive(value: true);
		GorillaTorsoModel.SetActive(value: true);
		GorillaTorsoPostModel.SetActive(value: true);
		GorillaMannequinModel.SetActive(value: true);
		JeweleryBoxModel.SetActive(value: true);
		root.SetActive(value: true);
		DisplayHeadModel.gameObject.SetActive(value: false);
	}

	public void OnEnable()
	{
		addToCartTextTMP?.gameObject.SetActive(value: true);
		slotPriceTextTMP?.gameObject.SetActive(value: true);
		AddStandToStoreController();
		if (CosmeticsController.hasInstance)
		{
			CosmeticsController instance = CosmeticsController.instance;
			instance.OnCosmeticsUpdated = (Action)Delegate.Combine(instance.OnCosmeticsUpdated, new Action(RefreshPurchaseGate));
		}
	}

	public void OnDisable()
	{
		addToCartTextTMP?.gameObject.SetActive(value: false);
		slotPriceTextTMP?.gameObject.SetActive(value: false);
		RemoveStandFromStoreController();
		if (CosmeticsController.hasInstance)
		{
			CosmeticsController instance = CosmeticsController.instance;
			instance.OnCosmeticsUpdated = (Action)Delegate.Remove(instance.OnCosmeticsUpdated, new Action(RefreshPurchaseGate));
		}
	}

	public void AddStandToStoreController()
	{
		if (StoreController.instance != null && StoreController.instance.cosmeticsInitialized)
		{
			_AddStandToStoreController();
		}
		else
		{
			StartCoroutine(ConnectToStoreController());
		}
	}

	private IEnumerator ConnectToStoreController()
	{
		int i = 0;
		while (i < 30 && !(StoreController.instance != null))
		{
			if (i == 29)
			{
				UnityEngine.Object.Destroy(this);
				throw new Exception("Could not connect to store controller.");
			}
			yield return null;
			int num = i + 1;
			i = num;
		}
		if (!StoreController.instance.cosmeticsInitialized)
		{
			AsyncAddStandToStoreController();
			yield break;
		}
		while (Application.isPlaying && (!CosmeticsController.hasInstance || !CosmeticsController.instance.v2_allCosmeticsInfoAssetRef_isLoaded))
		{
			yield return null;
		}
		_AddStandToStoreController();
	}

	public async void AsyncAddStandToStoreController()
	{
		while (!StoreController.instance.cosmeticsInitialized)
		{
			await Awaitable.NextFrameAsync();
		}
		_AddStandToStoreController();
	}

	public void _AddStandToStoreController()
	{
		StoreController.instance.AddStandToCosmeticStandsDictionary(this);
		StoreController.instance.AddStandToPlayfabIDDictionary(this);
		if (StoreController.instance.LoadFromTitleData)
		{
			StoreController.instance.InitializeStandFromTitleData(this);
		}
		else
		{
			InitializeCosmetic();
		}
	}

	public void RemoveStandFromStoreController()
	{
		if (!(StoreController.instance == null) && StoreController.instance.cosmeticsInitialized)
		{
			StoreController.instance.RemoveStandFromDynamicCosmeticStandsDictionary(this);
			StoreController.instance.RemoveStandFromPlayFabIDDictionary(this);
		}
	}

	public virtual void SetForGame()
	{
		DisplayHeadModel.gameObject.SetActive(value: true);
		SetStandType(DisplayHeadModel.bustType);
		parentDisplay = GetComponentInParent<StoreDisplay>();
		parentDepartment = GetComponentInParent<StoreDepartment>();
	}

	public void InitializeCosmetic()
	{
		thisCosmeticItem = CosmeticsController.instance.allCosmetics.Find((CosmeticsController.CosmeticItem x) => thisCosmeticName == x.displayName || thisCosmeticName == x.overrideDisplayName || thisCosmeticName == x.itemName);
		if (slotPriceText != null)
		{
			slotPriceText.text = thisCosmeticItem.itemCategory.ToString().ToUpper() + " " + thisCosmeticItem.cost;
		}
		if (slotPriceTextTMP != null)
		{
			slotPriceTextTMP.text = thisCosmeticItem.itemCategory.ToString().ToUpper() + " " + thisCosmeticItem.cost;
		}
		RefreshPurchaseGate();
	}

	public void RefreshPurchaseGate()
	{
		if (thisCosmeticItem.itemCategory != CosmeticsController.CosmeticCategory.Collectable)
		{
			return;
		}
		CosmeticsController instance = CosmeticsController.instance;
		CosmeticCollectionParentLink[] collectionParentLinks = thisCosmeticItem.collectionParentLinks;
		string text = null;
		string text2 = null;
		if (collectionParentLinks != null)
		{
			for (int i = 0; i < collectionParentLinks.Length; i++)
			{
				string parentPlayFabID = collectionParentLinks[i].parentPlayFabID;
				if (!string.IsNullOrEmpty(parentPlayFabID))
				{
					if (text2 == null)
					{
						text2 = parentPlayFabID;
					}
					if (instance.IsOwnedByPlayFabID(parentPlayFabID))
					{
						text = parentPlayFabID;
						break;
					}
				}
			}
		}
		if (text == null)
		{
			AddToCartButton.gameObject.SetActive(value: false);
			CosmeticsController.CosmeticItem value;
			string text3 = ((text2 != null && instance.allCosmeticsDict.TryGetValue(text2, out value)) ? value.overrideDisplayName : text2);
			if (slotPriceTextTMP != null)
			{
				slotPriceTextTMP.text = "REQUIRES\n" + text3;
			}
		}
		else if (!instance.CanPurchaseCollectable(thisCosmeticItem.itemName))
		{
			AddToCartButton.gameObject.SetActive(value: false);
			if (slotPriceTextTMP != null)
			{
				slotPriceTextTMP.text = "SLOTS FULL";
			}
		}
		else
		{
			AddToCartButton.gameObject.SetActive(value: true);
			if (slotPriceTextTMP != null)
			{
				slotPriceTextTMP.text = "ADD-ON   " + thisCosmeticItem.cost;
			}
		}
	}

	public void SpawnItemOntoStand(string PlayFabID)
	{
		ClearCosmetics();
		if (PlayFabID.IsNullOrEmpty())
		{
			GTDev.LogWarning("ManuallyInitialize: PlayFabID is null or empty for " + StandName);
			return;
		}
		if (StoreController.instance.IsNotNull() && Application.isPlaying)
		{
			StoreController.instance.RemoveStandFromPlayFabIDDictionary(this);
		}
		thisCosmeticName = PlayFabID;
		if (thisCosmeticName.Length == 5)
		{
			thisCosmeticName += ".";
		}
		if (Application.isPlaying)
		{
			DisplayHeadModel.LoadCosmeticPartsV2(thisCosmeticName);
		}
		else
		{
			DisplayHeadModel.LoadCosmeticParts(StoreController.FindCosmeticInAllCosmeticsArraySO(thisCosmeticName));
		}
		if (StoreController.instance.IsNotNull() && Application.isPlaying)
		{
			StoreController.instance.AddStandToPlayfabIDDictionary(this);
		}
	}

	public void ClearCosmetics()
	{
		thisCosmeticName = "";
		DisplayHeadModel.ClearManuallySpawnedCosmeticParts();
		DisplayHeadModel.ClearCosmetics();
	}

	public void SetStandType(HeadModel_CosmeticStand.BustType newBustType)
	{
		DisplayHeadModel.SetStandType(newBustType);
		GorillaHeadModel.SetActive(value: false);
		GorillaTorsoModel.SetActive(value: false);
		GorillaTorsoPostModel.SetActive(value: false);
		GorillaMannequinModel.SetActive(value: false);
		GuitarStandModel.SetActive(value: false);
		JeweleryBoxModel.SetActive(value: false);
		AddToCartButton.gameObject.SetActive(value: true);
		slotPriceText?.gameObject.SetActive(value: true);
		slotPriceTextTMP?.gameObject.SetActive(value: true);
		addToCartText?.gameObject.SetActive(value: true);
		addToCartTextTMP?.gameObject.SetActive(value: true);
		switch (newBustType)
		{
		case HeadModel_CosmeticStand.BustType.Disabled:
			ClearCosmetics();
			thisCosmeticName = "";
			AddToCartButton.gameObject.SetActive(value: false);
			slotPriceText?.gameObject.SetActive(value: false);
			slotPriceTextTMP?.gameObject.SetActive(value: false);
			addToCartText?.gameObject.SetActive(value: false);
			addToCartTextTMP?.gameObject.SetActive(value: false);
			DisplayHeadModel.transform.localPosition = Vector3.zero;
			DisplayHeadModel.transform.localRotation = Quaternion.identity;
			root.SetActive(value: false);
			break;
		case HeadModel_CosmeticStand.BustType.GorillaHead:
			root.SetActive(value: true);
			GorillaHeadModel.SetActive(value: true);
			DisplayHeadModel.transform.localPosition = GorillaHeadModel.transform.localPosition;
			DisplayHeadModel.transform.localRotation = GorillaHeadModel.transform.localRotation;
			break;
		case HeadModel_CosmeticStand.BustType.GorillaTorso:
			root.SetActive(value: true);
			GorillaTorsoModel.SetActive(value: true);
			DisplayHeadModel.transform.localPosition = GorillaTorsoModel.transform.localPosition;
			DisplayHeadModel.transform.localRotation = GorillaTorsoModel.transform.localRotation;
			break;
		case HeadModel_CosmeticStand.BustType.GorillaTorsoPost:
			root.SetActive(value: true);
			GorillaTorsoPostModel.SetActive(value: true);
			DisplayHeadModel.transform.localPosition = GorillaTorsoPostModel.transform.localPosition;
			DisplayHeadModel.transform.localRotation = GorillaTorsoPostModel.transform.localRotation;
			break;
		case HeadModel_CosmeticStand.BustType.GorillaMannequin:
			root.SetActive(value: true);
			GorillaMannequinModel.SetActive(value: true);
			DisplayHeadModel.transform.localPosition = GorillaMannequinModel.transform.localPosition;
			DisplayHeadModel.transform.localRotation = GorillaMannequinModel.transform.localRotation;
			break;
		case HeadModel_CosmeticStand.BustType.GuitarStand:
			root.SetActive(value: true);
			GuitarStandModel.SetActive(value: true);
			DisplayHeadModel.transform.localPosition = GuitarStandMount.transform.localPosition;
			DisplayHeadModel.transform.localRotation = GuitarStandMount.transform.localRotation;
			break;
		case HeadModel_CosmeticStand.BustType.JewelryBox:
			root.SetActive(value: true);
			JeweleryBoxModel.SetActive(value: true);
			DisplayHeadModel.transform.localPosition = JeweleryBoxMount.transform.localPosition;
			DisplayHeadModel.transform.localRotation = JeweleryBoxMount.transform.localRotation;
			break;
		case HeadModel_CosmeticStand.BustType.Table:
			root.SetActive(value: true);
			DisplayHeadModel.transform.localPosition = TableMount.transform.localPosition;
			DisplayHeadModel.transform.localRotation = TableMount.transform.localRotation;
			break;
		case HeadModel_CosmeticStand.BustType.TagEffectDisplay:
			root.SetActive(value: true);
			break;
		case HeadModel_CosmeticStand.BustType.PinDisplay:
			root.SetActive(value: true);
			DisplayHeadModel.transform.localPosition = PinDisplayMount.transform.localPosition;
			DisplayHeadModel.transform.localRotation = PinDisplayMount.transform.localRotation;
			break;
		default:
			root.SetActive(value: true);
			DisplayHeadModel.transform.localPosition = Vector3.zero;
			DisplayHeadModel.transform.localRotation = Quaternion.identity;
			break;
		}
		SpawnItemOntoStand(thisCosmeticName);
	}

	public void CopyChildsName()
	{
		DynamicCosmeticStand[] componentsInChildren = base.gameObject.GetComponentsInChildren<DynamicCosmeticStand>(includeInactive: true);
		foreach (DynamicCosmeticStand dynamicCosmeticStand in componentsInChildren)
		{
			if (dynamicCosmeticStand != this)
			{
				StandName = dynamicCosmeticStand.StandName;
			}
		}
	}

	public void PressCosmeticStandButton()
	{
		if (!StoreController.instance.StandsByPlayfabID.ContainsKey(thisCosmeticName) || CosmeticsController.instance.GetCosmeticSOFromDisplayName(thisCosmeticName) == null)
		{
			return;
		}
		searchIndex = CosmeticsController.instance.currentCart.IndexOf(thisCosmeticItem);
		if (searchIndex != -1)
		{
			GorillaTelemetry.PostShopEvent(GorillaTagger.Instance.offlineVRRig, GTShopEventType.cart_item_remove, thisCosmeticItem);
			CosmeticsController.instance.currentCart.RemoveAt(searchIndex);
			foreach (DynamicCosmeticStand item in StoreController.instance.StandsByPlayfabID[thisCosmeticItem.itemName])
			{
				item.AddToCartButton.isOn = false;
				item.AddToCartButton.UpdateColor();
			}
			for (int i = 0; i < 16; i++)
			{
				if (thisCosmeticItem.itemName == CosmeticsController.instance.tryOnSet.items[i].itemName)
				{
					CosmeticsController.instance.tryOnSet.items[i] = CosmeticsController.instance.nullItem;
				}
			}
		}
		else
		{
			GorillaTelemetry.PostShopEvent(GorillaTagger.Instance.offlineVRRig, GTShopEventType.cart_item_add, thisCosmeticItem);
			CosmeticsController.instance.currentCart.Insert(0, thisCosmeticItem);
			foreach (DynamicCosmeticStand item2 in StoreController.instance.StandsByPlayfabID[thisCosmeticName])
			{
				item2.AddToCartButton.isOn = true;
				item2.AddToCartButton.UpdateColor();
			}
			if (CosmeticsController.instance.currentCart.Count > CosmeticsController.instance.numFittingRoomButtons)
			{
				foreach (DynamicCosmeticStand item3 in StoreController.instance.StandsByPlayfabID[CosmeticsController.instance.currentCart[CosmeticsController.instance.numFittingRoomButtons].itemName])
				{
					item3.AddToCartButton.isOn = false;
					item3.AddToCartButton.UpdateColor();
				}
				CosmeticsController.instance.currentCart.RemoveAt(CosmeticsController.instance.numFittingRoomButtons);
			}
		}
		CosmeticsController.instance.UpdateShoppingCart();
	}

	public void SetStandTypeString(string bustTypeString)
	{
		switch (bustTypeString)
		{
		case "Disabled":
			SetStandType(HeadModel_CosmeticStand.BustType.Disabled);
			break;
		case "GorillaHead":
			SetStandType(HeadModel_CosmeticStand.BustType.GorillaHead);
			break;
		case "GorillaTorso":
			SetStandType(HeadModel_CosmeticStand.BustType.GorillaTorso);
			break;
		case "GorillaTorsoPost":
			SetStandType(HeadModel_CosmeticStand.BustType.GorillaTorsoPost);
			break;
		case "GorillaMannequin":
			SetStandType(HeadModel_CosmeticStand.BustType.GorillaMannequin);
			break;
		case "GuitarStand":
			SetStandType(HeadModel_CosmeticStand.BustType.GuitarStand);
			break;
		case "JewelryBox":
			SetStandType(HeadModel_CosmeticStand.BustType.JewelryBox);
			break;
		case "Table":
			SetStandType(HeadModel_CosmeticStand.BustType.Table);
			break;
		case "PinDisplay":
			SetStandType(HeadModel_CosmeticStand.BustType.PinDisplay);
			break;
		case "TagEffectDisplay":
			SetStandType(HeadModel_CosmeticStand.BustType.TagEffectDisplay);
			break;
		default:
			SetStandType(HeadModel_CosmeticStand.BustType.Table);
			break;
		}
	}

	public void UpdateCosmeticsMountPositions()
	{
		DisplayHeadModel.UpdateCosmeticsMountPositions(StoreController.FindCosmeticInAllCosmeticsArraySO(thisCosmeticName));
	}

	public void InitializeForCustomMapCosmeticItem(GTObjectPlaceholder.ECustomMapCosmeticItem cosmeticItemSlot, Scene scene)
	{
		StandName = "CustomMapCosmeticItemStand-" + cosmeticItemSlot;
		customMapScene = scene;
		ClearCosmetics();
		if (CosmeticsController.instance.customMapCosmeticsData.TryGetItem(cosmeticItemSlot, out var foundItem))
		{
			thisCosmeticName = foundItem.playFabID;
			SetStandType(foundItem.bustType);
			InitializeCosmetic();
		}
	}

	public bool IsFromCustomMapScene(Scene scene)
	{
		return customMapScene == scene;
	}
}

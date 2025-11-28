using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace GorillaNetworking.Store;

[Serializable]
public class StoreBundle
{
	private static readonly string defaultPrice = "$--.--";

	private static readonly string defaultCurrencySymbol = "$";

	[NonSerialized]
	public string purchaseButtonStringFormat = "THE {0}\n{1}";

	[SerializeField]
	public List<BundleStand> bundleStands;

	public bool isOwned;

	private string _price = defaultPrice;

	private string _bundleName = "";

	public string purchaseButtonText = "";

	[FormerlySerializedAs("storeBundleDataReference")]
	[SerializeField]
	[ReadOnly]
	private StoreBundleData _storeBundleDataReference;

	public string playfabBundleID => _storeBundleDataReference.playfabBundleID;

	public string bundleSKU => _storeBundleDataReference.bundleSKU;

	public Sprite bundleImage => _storeBundleDataReference.bundleImage;

	public NexusCreatorCode nexusCreatorCode => _storeBundleDataReference.creatorCode;

	public string price => _price;

	public string bundleName
	{
		get
		{
			if (_bundleName.IsNullOrEmpty())
			{
				int num = CosmeticsController.instance.allCosmetics.FindIndex((CosmeticsController.CosmeticItem x) => playfabBundleID == x.itemName);
				if (num > -1)
				{
					if (!CosmeticsController.instance.allCosmetics[num].overrideDisplayName.IsNullOrEmpty())
					{
						_bundleName = CosmeticsController.instance.allCosmetics[num].overrideDisplayName;
					}
					else
					{
						_bundleName = CosmeticsController.instance.allCosmetics[num].displayName;
					}
				}
				else
				{
					_bundleName = "NULL_BUNDLE_NAME";
				}
			}
			return _bundleName;
		}
	}

	public bool HasPrice
	{
		get
		{
			if (!string.IsNullOrEmpty(price))
			{
				return price != defaultPrice;
			}
			return false;
		}
	}

	public string bundleDescriptionText => _storeBundleDataReference.bundleDescriptionText;

	public StoreBundle()
	{
		isOwned = false;
		bundleStands = new List<BundleStand>();
	}

	public StoreBundle(StoreBundleData data)
	{
		isOwned = false;
		bundleStands = new List<BundleStand>();
		_storeBundleDataReference = data;
	}

	public void InitializebundleStands()
	{
		foreach (BundleStand bundleStand in bundleStands)
		{
			bundleStand.UpdateDescriptionText(bundleDescriptionText);
			bundleStand.InitializeEventListeners();
		}
	}

	public void TryUpdatePrice(uint bundlePrice)
	{
		TryUpdatePrice(((decimal)bundlePrice / 100m).ToString());
	}

	public void TryUpdatePrice(string bundlePrice = null)
	{
		if (!string.IsNullOrEmpty(bundlePrice))
		{
			_price = (decimal.TryParse(bundlePrice, out var _) ? (defaultCurrencySymbol + bundlePrice) : bundlePrice);
		}
		UpdatePurchaseButtonText();
	}

	public void UpdatePurchaseButtonText()
	{
		purchaseButtonText = string.Format(purchaseButtonStringFormat, bundleName, price);
		foreach (BundleStand bundleStand in bundleStands)
		{
			bundleStand.UpdatePurchaseButtonText(purchaseButtonText);
		}
	}

	public void ValidateBundleData()
	{
		if (_storeBundleDataReference == null)
		{
			Debug.LogError("StoreBundleData is null");
			foreach (BundleStand bundleStand in bundleStands)
			{
				if (bundleStand == null)
				{
					Debug.LogError("BundleStand is null");
				}
				else if (bundleStand._bundleDataReference != null)
				{
					_storeBundleDataReference = bundleStand._bundleDataReference;
					Debug.LogError("BundleStand StoreBundleData is not equal to StoreBundle StoreBundleData");
				}
			}
		}
		if (_storeBundleDataReference == null)
		{
			Debug.LogError("StoreBundleData is null");
			return;
		}
		if (_storeBundleDataReference.playfabBundleID.IsNullOrEmpty())
		{
			Debug.LogError("playfabBundleID is null");
		}
		if (_storeBundleDataReference.bundleSKU.IsNullOrEmpty())
		{
			Debug.LogError("bundleSKU is null");
		}
		if (_storeBundleDataReference.bundleImage == null)
		{
			Debug.LogError("bundleImage is null");
		}
		if (_storeBundleDataReference.bundleDescriptionText.IsNullOrEmpty())
		{
			Debug.LogError("bundleDescriptionText is null");
		}
	}
}

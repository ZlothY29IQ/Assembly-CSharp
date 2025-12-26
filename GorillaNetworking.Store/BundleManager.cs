using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cosmetics;
using GorillaExtensions;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace GorillaNetworking.Store;

public class BundleManager : MonoBehaviour
{
	[Serializable]
	public class BundleStandSpawn
	{
		public EndCapSpawnPoint spawnLocation;

		public BundleStand bundleStand;

		private static IEnumerable GetEndCapSpawnPoints()
		{
			return from x in UnityEngine.Object.FindObjectsByType<EndCapSpawnPoint>(FindObjectsSortMode.None)
				select new ValueDropdownItem(x.transform.parent.parent.name + "/" + x.transform.parent.name + "/" + x.name, x);
		}
	}

	public static volatile BundleManager instance;

	[FormerlySerializedAs("_TryOnBundlesStand")]
	public TryOnBundlesStand _tryOnBundlesStand;

	[SerializeField]
	private StoreBundleData nullBundleData;

	private List<StoreBundleData> _bundleScriptableObjects = new List<StoreBundleData>();

	[SerializeField]
	private List<StoreBundle> _storeBundles = new List<StoreBundle>();

	[FormerlySerializedAs("_SpawnedBundleStands")]
	[SerializeField]
	private List<SpawnedBundle> _spawnedBundleStands = new List<SpawnedBundle>();

	public Dictionary<string, StoreBundle> storeBundlesById = new Dictionary<string, StoreBundle>();

	public Dictionary<string, StoreBundle> storeBundlesBySKU = new Dictionary<string, StoreBundle>();

	[Header("Enable Advanced Search window in your settings to easily see all bundle prefabs")]
	[SerializeField]
	private List<BundleStandSpawn> BundleStands = new List<BundleStandSpawn>();

	[SerializeField]
	private StoreBundleData tryOnBundleButton1;

	[SerializeField]
	private StoreBundleData tryOnBundleButton2;

	[SerializeField]
	private StoreBundleData tryOnBundleButton3;

	[SerializeField]
	private StoreBundleData tryOnBundleButton4;

	[SerializeField]
	private StoreBundleData tryOnBundleButton5;

	private IEnumerable GetStoreBundles()
	{
		List<StoreBundleData> list = new List<StoreBundleData>();
		list.Add(nullBundleData);
		list.AddRange(_bundleScriptableObjects);
		return list;
	}

	public void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
		else if (instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private void Start()
	{
		GenerateBundleDictionaries();
		Initialize();
	}

	private void Initialize()
	{
		foreach (StoreBundle storeBundle in _storeBundles)
		{
			storeBundle.InitializebundleStands();
		}
	}

	private void ValidateBundleData()
	{
		foreach (StoreBundle storeBundle in _storeBundles)
		{
			storeBundle.ValidateBundleData();
		}
	}

	private void SpawnBundleStands()
	{
		foreach (StoreBundle storeBundle in _storeBundles)
		{
			foreach (BundleStand bundleStand in storeBundle.bundleStands)
			{
				if (bundleStand != null)
				{
					UnityEngine.Object.DestroyImmediate(bundleStand.gameObject);
				}
			}
		}
		_spawnedBundleStands.Clear();
		storeBundlesById.Clear();
		storeBundlesBySKU.Clear();
		_storeBundles.Clear();
		_bundleScriptableObjects.Clear();
		BundleStand[] array = UnityEngine.Object.FindObjectsByType<BundleStand>(FindObjectsSortMode.None);
		for (int i = 0; i < array.Length; i++)
		{
			UnityEngine.Object.DestroyImmediate(array[i].gameObject);
		}
		for (int j = 0; j < BundleStands.Count; j++)
		{
			if (BundleStands[j].spawnLocation == null)
			{
				Debug.LogError("No spawn location set for Bundle Stand " + j);
			}
			else if (BundleStands[j].bundleStand == null)
			{
				Debug.LogError("No Bundle Stand set for Bundle Stand " + j);
			}
		}
		GenerateAllStoreBundleReferences();
		if (!_bundleScriptableObjects.Contains(tryOnBundleButton1))
		{
			tryOnBundleButton1 = nullBundleData;
		}
		if (!_bundleScriptableObjects.Contains(tryOnBundleButton2))
		{
			tryOnBundleButton2 = nullBundleData;
		}
		if (!_bundleScriptableObjects.Contains(tryOnBundleButton3))
		{
			tryOnBundleButton3 = nullBundleData;
		}
		if (!_bundleScriptableObjects.Contains(tryOnBundleButton4))
		{
			tryOnBundleButton4 = nullBundleData;
		}
		if (!_bundleScriptableObjects.Contains(tryOnBundleButton5))
		{
			tryOnBundleButton4 = nullBundleData;
		}
	}

	public void ClearEverything()
	{
		foreach (StoreBundle storeBundle in _storeBundles)
		{
			foreach (BundleStand bundleStand in storeBundle.bundleStands)
			{
				if (bundleStand != null)
				{
					UnityEngine.Object.DestroyImmediate(bundleStand.gameObject);
				}
			}
		}
		_spawnedBundleStands.Clear();
		storeBundlesById.Clear();
		storeBundlesBySKU.Clear();
		_storeBundles.Clear();
		_bundleScriptableObjects.Clear();
		tryOnBundleButton1 = nullBundleData;
		tryOnBundleButton2 = nullBundleData;
		tryOnBundleButton3 = nullBundleData;
		tryOnBundleButton4 = nullBundleData;
		tryOnBundleButton5 = nullBundleData;
		BundleStand[] array = UnityEngine.Object.FindObjectsByType<BundleStand>(FindObjectsSortMode.None);
		for (int i = 0; i < array.Length; i++)
		{
			UnityEngine.Object.DestroyImmediate(array[i].gameObject);
		}
	}

	public void GenerateAllStoreBundleReferences()
	{
	}

	private void AddNewBundleStand(BundleStand bundleStand)
	{
		foreach (StoreBundle storeBundle2 in _storeBundles)
		{
			if (storeBundle2.playfabBundleID == bundleStand._bundleDataReference.playfabBundleID)
			{
				storeBundle2.bundleStands.Add(bundleStand);
				return;
			}
		}
		StoreBundle storeBundle = new StoreBundle(bundleStand._bundleDataReference);
		storeBundle.bundleStands.Add(bundleStand);
		_storeBundles.Add(storeBundle);
	}

	public void GenerateBundleDictionaries()
	{
		storeBundlesById.Clear();
		storeBundlesBySKU.Clear();
		foreach (StoreBundle storeBundle in _storeBundles)
		{
			storeBundlesById.Add(storeBundle.playfabBundleID, storeBundle);
			storeBundlesBySKU.Add(storeBundle.bundleSKU, storeBundle);
		}
	}

	public void BundlePurchaseButtonPressed(string playFabItemName, ICreatorCodeProvider ccp)
	{
		CosmeticsController.instance.PurchaseBundle(storeBundlesById[playFabItemName], ccp);
	}

	public void FixBundles()
	{
		_storeBundles.Clear();
		for (int num = _spawnedBundleStands.Count - 1; num >= 0; num--)
		{
			if (_spawnedBundleStands[num].bundleStand == null)
			{
				_spawnedBundleStands.RemoveAt(num);
			}
		}
		BundleStand[] array = UnityEngine.Object.FindObjectsByType<BundleStand>(FindObjectsSortMode.None);
		foreach (BundleStand bundle in array)
		{
			if (_spawnedBundleStands.Any((SpawnedBundle x) => x.spawnLocationPath == bundle.transform.parent.gameObject.GetPath(3)))
			{
				SpawnedBundle spawnedBundle = _spawnedBundleStands.First((SpawnedBundle x) => x.spawnLocationPath == bundle.transform.parent.gameObject.GetPath(3));
				if (spawnedBundle != null && spawnedBundle.bundleStand != bundle)
				{
					UnityEngine.Object.DestroyImmediate(spawnedBundle.bundleStand.gameObject);
					spawnedBundle.bundleStand = bundle;
				}
			}
			else
			{
				_spawnedBundleStands.Add(new SpawnedBundle
				{
					spawnLocationPath = bundle.transform.parent.gameObject.GetPath(3),
					bundleStand = bundle
				});
			}
		}
		GenerateAllStoreBundleReferences();
	}

	public StoreBundleData[] GetTryOnButtons()
	{
		return new StoreBundleData[5] { tryOnBundleButton1, tryOnBundleButton2, tryOnBundleButton3, tryOnBundleButton4, tryOnBundleButton5 };
	}

	public void NotifyBundleOfErrorByPlayFabID(string ItemId)
	{
		if (!storeBundlesById.TryGetValue(ItemId, out var value))
		{
			return;
		}
		foreach (BundleStand bundleStand in value.bundleStands)
		{
			bundleStand.ErrorHappened();
		}
	}

	public void NotifyBundleOfErrorBySKU(string ItemSKU)
	{
		if (!storeBundlesBySKU.TryGetValue(ItemSKU, out var value))
		{
			return;
		}
		foreach (BundleStand bundleStand in value.bundleStands)
		{
			bundleStand.ErrorHappened();
		}
	}

	public void MarkBundleOwnedByPlayFabID(string ItemId)
	{
		if (!storeBundlesById.ContainsKey(ItemId))
		{
			return;
		}
		storeBundlesById[ItemId].isOwned = true;
		foreach (BundleStand bundleStand in storeBundlesById[ItemId].bundleStands)
		{
			bundleStand.NotifyAlreadyOwn();
		}
	}

	public void MarkBundleOwnedBySKU(string SKU)
	{
		if (!storeBundlesBySKU.ContainsKey(SKU))
		{
			return;
		}
		storeBundlesBySKU[SKU].isOwned = true;
		foreach (BundleStand bundleStand in storeBundlesBySKU[SKU].bundleStands)
		{
			bundleStand.NotifyAlreadyOwn();
		}
	}

	public void CheckIfBundlesOwned()
	{
		foreach (StoreBundle value in storeBundlesById.Values)
		{
			if (!value.isOwned)
			{
				continue;
			}
			foreach (BundleStand bundleStand in value.bundleStands)
			{
				bundleStand.NotifyAlreadyOwn();
			}
		}
	}

	public void PressTryOnBundleButton(TryOnBundleButton pressedTryOnBundleButton, bool isLeftHand)
	{
		if (_tryOnBundlesStand.IsNotNull())
		{
			_tryOnBundlesStand?.PressTryOnBundleButton(pressedTryOnBundleButton, isLeftHand);
		}
	}

	public void PressPurchaseTryOnBundleButton()
	{
		_tryOnBundlesStand?.PurchaseButtonPressed();
	}

	public void UpdateBundlePrice(string productSku, string productFormattedPrice)
	{
		if (storeBundlesBySKU.ContainsKey(productSku))
		{
			storeBundlesBySKU[productSku].TryUpdatePrice(productFormattedPrice);
		}
	}

	public void CheckForNoPriceBundlesAndDefaultPrice()
	{
		foreach (var (_, storeBundle2) in storeBundlesBySKU)
		{
			if (!storeBundle2.HasPrice)
			{
				storeBundle2.TryUpdatePrice();
			}
		}
	}
}

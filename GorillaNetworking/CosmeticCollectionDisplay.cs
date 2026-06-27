using System.Collections.Generic;
using GorillaTag;
using GorillaTag.CosmeticSystem;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GorillaNetworking;

public class CosmeticCollectionDisplay : MonoBehaviour
{
	private static readonly Dictionary<(VRRig, string), CosmeticCollectionDisplay> Registered = new Dictionary<(VRRig, string), CosmeticCollectionDisplay>();

	private static readonly List<CosmeticCollectionDisplay> AllDisplays = new List<CosmeticCollectionDisplay>();

	private bool isCycling;

	private bool isVisible = true;

	private bool isLocal;

	private int activeIndex;

	private int visibleMask;

	private VRRig registeredRig;

	private string registeredParentID;

	private readonly List<GameObject> spawnedAnchors = new List<GameObject>();

	private readonly List<AsyncOperationHandle<GameObject>> loadOps = new List<AsyncOperationHandle<GameObject>>();

	private readonly List<CosmeticsController.CosmeticItem> placedCollectables = new List<CosmeticsController.CosmeticItem>();

	private readonly List<int> canonicalIndices = new List<int>();

	public string ParentPlayFabID { get; private set; }

	public int ActiveIndex => activeIndex;

	public int Count => spawnedAnchors.Count;

	public int VisibleMask => visibleMask;

	public bool IsLocal => isLocal;

	public CosmeticsController.CosmeticItem? ActiveCollectable
	{
		get
		{
			if (placedCollectables.Count <= 0)
			{
				return null;
			}
			return placedCollectables[activeIndex];
		}
	}

	public int ActiveCanonicalIndex
	{
		get
		{
			if (placedCollectables.Count == 0 || activeIndex < 0 || activeIndex >= canonicalIndices.Count)
			{
				return -1;
			}
			return canonicalIndices[activeIndex];
		}
	}

	public static void Register(VRRig rig, string parentID, CosmeticCollectionDisplay display, bool isLocal)
	{
		display.registeredRig = rig;
		display.registeredParentID = parentID;
		display.ParentPlayFabID = parentID;
		display.isLocal = isLocal;
		if (Registered.TryGetValue((rig, parentID), out var value) && value != null && value != display)
		{
			Object.Destroy(value);
		}
		Registered[(rig, parentID)] = display;
		if (!AllDisplays.Contains(display))
		{
			AllDisplays.Add(display);
		}
	}

	public static CosmeticCollectionDisplay FindForRig(VRRig rig, string parentID)
	{
		Registered.TryGetValue((rig, parentID), out var value);
		return value;
	}

	public static void GetAllForParent(VRRig rig, string parentID, List<CosmeticCollectionDisplay> result)
	{
		result.Clear();
		for (int i = 0; i < AllDisplays.Count; i++)
		{
			CosmeticCollectionDisplay cosmeticCollectionDisplay = AllDisplays[i];
			if (!(cosmeticCollectionDisplay == null) && cosmeticCollectionDisplay.registeredRig == rig && cosmeticCollectionDisplay.registeredParentID == parentID)
			{
				result.Add(cosmeticCollectionDisplay);
			}
		}
	}

	public static void DestroyAllForParentExcept(VRRig rig, string parentID, GameObject host)
	{
		for (int num = AllDisplays.Count - 1; num >= 0; num--)
		{
			CosmeticCollectionDisplay cosmeticCollectionDisplay = AllDisplays[num];
			if (cosmeticCollectionDisplay == null)
			{
				AllDisplays.RemoveAt(num);
			}
			else if (!(cosmeticCollectionDisplay.registeredRig != rig) && !(cosmeticCollectionDisplay.registeredParentID != parentID) && !(cosmeticCollectionDisplay.gameObject == host))
			{
				Object.Destroy(cosmeticCollectionDisplay);
			}
		}
	}

	public static void GetDisplaysForRig(VRRig rig, List<CosmeticCollectionDisplay> result)
	{
		result.Clear();
		foreach (KeyValuePair<(VRRig, string), CosmeticCollectionDisplay> item in Registered)
		{
			if (item.Key.Item1 == rig)
			{
				result.Add(item.Value);
			}
		}
	}

	public CosmeticsController.CosmeticItem? GetCollectableAt(int index)
	{
		if (index < 0 || index >= placedCollectables.Count)
		{
			return null;
		}
		return placedCollectables[index];
	}

	public bool ContentMatches(IReadOnlyList<CosmeticsController.CosmeticItem> items)
	{
		if (placedCollectables.Count != items.Count)
		{
			return false;
		}
		for (int i = 0; i < placedCollectables.Count; i++)
		{
			if (placedCollectables[i].itemName != items[i].itemName)
			{
				return false;
			}
		}
		return true;
	}

	public void Populate(IReadOnlyList<CosmeticsController.CosmeticItem> ownedCollectables, CosmeticInfoV2 parentInfo, Transform rootXform)
	{
		ClearSpawnedAnchors();
		placedCollectables.Clear();
		canonicalIndices.Clear();
		isCycling = parentInfo.collectionIsCycling;
		bool collectionUsesIndexTargeting = parentInfo.collectionUsesIndexTargeting;
		if (isCycling)
		{
			CosmeticCollectionSlotDefinition cosmeticCollectionSlotDefinition = parentInfo.collectionSlots[0];
			Vector3 localScale = cosmeticCollectionSlotDefinition.offset.scale;
			if (Mathf.Abs(localScale.x) < 0.001f || Mathf.Abs(localScale.y) < 0.001f || Mathf.Abs(localScale.z) < 0.001f)
			{
				localScale = Vector3.one;
			}
			for (int i = 0; i < ownedCollectables.Count; i++)
			{
				GameObject gameObject = new GameObject($"CollectionSlot_{i}");
				gameObject.transform.SetParent(rootXform, worldPositionStays: false);
				gameObject.transform.localPosition = cosmeticCollectionSlotDefinition.offset.pos;
				gameObject.transform.localRotation = cosmeticCollectionSlotDefinition.offset.rot;
				gameObject.transform.localScale = localScale;
				spawnedAnchors.Add(gameObject);
				placedCollectables.Add(ownedCollectables[i]);
				canonicalIndices.Add(ResolveCanonicalIndex(parentInfo.playFabID, ownedCollectables[i].itemName));
				InstantiateIntoAnchor(ownedCollectables[i], gameObject.transform);
			}
		}
		else
		{
			int num = 0;
			for (int j = 0; j < parentInfo.collectionSlots.Length; j++)
			{
				CosmeticCollectionSlotDefinition cosmeticCollectionSlotDefinition2 = parentInfo.collectionSlots[j];
				CosmeticsController.CosmeticItem? cosmeticItem = null;
				if (collectionUsesIndexTargeting)
				{
					for (int k = 0; k < ownedCollectables.Count; k++)
					{
						if (ownedCollectables[k].GetTargetSlotIndexForParent(parentInfo.playFabID) == j)
						{
							cosmeticItem = ownedCollectables[k];
							break;
						}
					}
				}
				else if (num < ownedCollectables.Count)
				{
					cosmeticItem = ownedCollectables[num++];
				}
				if (cosmeticItem.HasValue)
				{
					Vector3 localScale2 = cosmeticCollectionSlotDefinition2.offset.scale;
					if (Mathf.Abs(localScale2.x) < 0.001f || Mathf.Abs(localScale2.y) < 0.001f || Mathf.Abs(localScale2.z) < 0.001f)
					{
						localScale2 = Vector3.one;
					}
					GameObject gameObject2 = new GameObject($"CollectionSlot_{j}");
					gameObject2.transform.SetParent(rootXform, worldPositionStays: false);
					gameObject2.transform.localPosition = cosmeticCollectionSlotDefinition2.offset.pos;
					gameObject2.transform.localRotation = cosmeticCollectionSlotDefinition2.offset.rot;
					gameObject2.transform.localScale = localScale2;
					spawnedAnchors.Add(gameObject2);
					placedCollectables.Add(cosmeticItem.Value);
					canonicalIndices.Add(ResolveCanonicalIndex(parentInfo.playFabID, cosmeticItem.Value.itemName));
					InstantiateIntoAnchor(cosmeticItem.Value, gameObject2.transform);
				}
			}
		}
		activeIndex = 0;
		visibleMask = 0;
		for (int l = 0; l < canonicalIndices.Count; l++)
		{
			int num2 = canonicalIndices[l];
			if (num2 >= 0 && num2 < 32)
			{
				visibleMask |= 1 << num2;
			}
		}
		ApplyCyclingVisibility();
	}

	private static int ResolveCanonicalIndex(string parentPlayFabID, string itemName)
	{
		if (!CosmeticsController.hasInstance)
		{
			return -1;
		}
		return CosmeticsController.instance.GetCanonicalCollectableIndex(parentPlayFabID, itemName);
	}

	public void SetActiveIndex(int index)
	{
		if (spawnedAnchors.Count != 0)
		{
			activeIndex = Mathf.Clamp(index, 0, spawnedAnchors.Count - 1);
			RefreshAnchorVisibility();
			PersistLocalState();
		}
	}

	public void SetVisibleMask(int mask)
	{
		visibleMask = mask;
		RefreshAnchorVisibility();
		PersistLocalState();
	}

	public bool SetEquippedAtCanonical(int canonicalIndex, bool equipped)
	{
		if (canonicalIndex < 0 || canonicalIndex >= 32)
		{
			return false;
		}
		int num = 1 << canonicalIndex;
		int num2 = (equipped ? (visibleMask | num) : (visibleMask & ~num));
		if (num2 == visibleMask)
		{
			return false;
		}
		visibleMask = num2;
		RefreshAnchorVisibility();
		PersistLocalState();
		return true;
	}

	public bool IsEquippedAtCanonical(int canonicalIndex)
	{
		if (canonicalIndex < 0 || canonicalIndex >= 32)
		{
			return true;
		}
		return (visibleMask & (1 << canonicalIndex)) != 0;
	}

	public void PersistLocalState()
	{
		if (isLocal && !string.IsNullOrEmpty(registeredParentID) && CosmeticsController.hasInstance)
		{
			CosmeticsController.instance.localCycleStates[(registeredRig, registeredParentID)] = new CosmeticsController.CollectionState
			{
				activeIndex = activeIndex,
				visibleMask = visibleMask
			};
		}
	}

	public void CycleActive(int direction)
	{
		if (isCycling && spawnedAnchors.Count != 0)
		{
			activeIndex = (activeIndex + direction + spawnedAnchors.Count) % spawnedAnchors.Count;
			RefreshAnchorVisibility();
		}
	}

	public void SetVisible(bool visible)
	{
		isVisible = visible;
		RefreshAnchorVisibility();
	}

	private void InstantiateIntoAnchor(CosmeticsController.CosmeticItem collectable, Transform anchor)
	{
		if (!CosmeticsController.instance.TryGetCosmeticInfoV2(collectable.itemName, out var cosmeticInfo))
		{
			return;
		}
		CosmeticPart[] array = (cosmeticInfo.hasStoreParts ? cosmeticInfo.storeParts : cosmeticInfo.functionalParts);
		if (array == null || array.Length == 0)
		{
			return;
		}
		GTAssetRef<GameObject> prefabAssetRef = array[0].prefabAssetRef;
		if (prefabAssetRef == null || !prefabAssetRef.RuntimeKeyIsValid())
		{
			return;
		}
		Vector3 attachScale = Vector3.one;
		CosmeticPart[] functionalParts = cosmeticInfo.functionalParts;
		if (functionalParts != null && functionalParts.Length != 0)
		{
			CosmeticAttachInfo[] attachAnchors = functionalParts[0].attachAnchors;
			if (attachAnchors != null && attachAnchors.Length != 0)
			{
				Vector3 scale = attachAnchors[0].offset.scale;
				if (Mathf.Abs(scale.x) >= 0.001f && Mathf.Abs(scale.y) >= 0.001f && Mathf.Abs(scale.z) >= 0.001f)
				{
					attachScale = scale;
				}
			}
		}
		AsyncOperationHandle<GameObject> item = prefabAssetRef.InstantiateAsync(anchor);
		loadOps.Add(item);
		item.Completed += delegate(AsyncOperationHandle<GameObject> handle)
		{
			if (handle.Status == AsyncOperationStatus.Succeeded)
			{
				if (anchor == null || handle.Result == null)
				{
					Addressables.ReleaseInstance(handle);
				}
				else
				{
					handle.Result.transform.localPosition = Vector3.zero;
					handle.Result.transform.localRotation = Quaternion.identity;
					handle.Result.transform.localScale = attachScale;
				}
			}
		};
	}

	private void ApplyCyclingVisibility()
	{
		RefreshAnchorVisibility();
	}

	private void RefreshAnchorVisibility()
	{
		for (int i = 0; i < spawnedAnchors.Count; i++)
		{
			if (!(spawnedAnchors[i] == null))
			{
				int num = ((i < canonicalIndices.Count) ? canonicalIndices[i] : i);
				bool flag = num < 0 || num >= 32 || (visibleMask & (1 << num)) != 0;
				bool active = isVisible && flag && (!isCycling || i == activeIndex);
				spawnedAnchors[i].SetActive(active);
			}
		}
	}

	private void ClearSpawnedAnchors()
	{
		for (int i = 0; i < loadOps.Count; i++)
		{
			if (loadOps[i].IsValid())
			{
				Addressables.ReleaseInstance(loadOps[i]);
			}
		}
		loadOps.Clear();
		for (int j = 0; j < spawnedAnchors.Count; j++)
		{
			if (spawnedAnchors[j] != null)
			{
				Object.Destroy(spawnedAnchors[j]);
			}
		}
		spawnedAnchors.Clear();
		placedCollectables.Clear();
		canonicalIndices.Clear();
	}

	private void UnregisterIfOwner()
	{
		(VRRig, string) key = (registeredRig, registeredParentID);
		if (Registered.TryGetValue(key, out var value) && value == this)
		{
			Registered.Remove(key);
		}
	}

	private void OnDisable()
	{
		UnregisterIfOwner();
	}

	private void OnEnable()
	{
		if (!string.IsNullOrEmpty(registeredParentID))
		{
			(VRRig, string) key = (registeredRig, registeredParentID);
			if (!Registered.TryGetValue(key, out var value) || value == null || value == this)
			{
				Registered[key] = this;
			}
			if (isLocal && CosmeticsController.hasInstance && CosmeticsController.instance.localCycleStates.TryGetValue((registeredRig, registeredParentID), out var value2))
			{
				visibleMask = value2.visibleMask;
				SetActiveIndex(value2.activeIndex);
			}
		}
	}

	private void OnDestroy()
	{
		UnregisterIfOwner();
		AllDisplays.Remove(this);
		ClearSpawnedAnchors();
	}
}

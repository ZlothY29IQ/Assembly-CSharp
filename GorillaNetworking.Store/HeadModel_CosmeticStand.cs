using System.Collections.Generic;
using GorillaTag;
using GorillaTag.CosmeticSystem;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GorillaNetworking.Store;

public class HeadModel_CosmeticStand : HeadModel
{
	public enum BustType
	{
		Disabled,
		GorillaHead,
		GorillaTorso,
		GorillaTorsoPost,
		GorillaMannequin,
		GuitarStand,
		JewelryBox,
		Table,
		PinDisplay,
		TagEffectDisplay
	}

	[ReadOnly]
	public BustType bustType = BustType.JewelryBox;

	[SerializeField]
	[ReadOnly]
	private List<GameObject> _manuallySpawnedCosmeticParts = new List<GameObject>();

	public GameObject mannequin;

	public Material defaultMannequinFace;

	public Material defaultMannequinChest;

	public Material defaultMannequinBody;

	[DebugReadout]
	private readonly Dictionary<AsyncOperationHandle, int> _loadOp_to_partInfoIndex = new Dictionary<AsyncOperationHandle, int>(1);

	private string mountID => "Mount_" + bustType;

	public void LoadCosmeticParts(CosmeticSO cosmeticInfo, bool forRightSide = false)
	{
		ClearManuallySpawnedCosmeticParts();
		ClearCosmetics();
		if (cosmeticInfo == null)
		{
			Debug.LogWarning("Dynamic Cosmetics - LoadWardRobeParts -  No Cosmetic Info");
			return;
		}
		Debug.Log("Dynamic Cosmetics - Loading Wardrobe Parts for " + cosmeticInfo.info.playFabID);
		HandleLoadCosmeticParts(cosmeticInfo, forRightSide);
	}

	private void ResetMannequinSkin()
	{
		List<Material> value;
		if (mannequin.TryGetComponent<SkinnedMeshRenderer>(out var component))
		{
			using (ListPool<Material>.Get(out value))
			{
				value.Clear();
				value.EnsureCapacity(3);
				value.Add(defaultMannequinBody);
				value.Add(defaultMannequinChest);
				value.Add(defaultMannequinFace);
				component.SetSharedMaterials(value);
				return;
			}
		}
		List<Material> value2;
		if (mannequin.TryGetComponent<MeshRenderer>(out var component2))
		{
			using (ListPool<Material>.Get(out value2))
			{
				value2.Clear();
				value2.EnsureCapacity(3);
				value2.Add(defaultMannequinBody);
				value2.Add(defaultMannequinChest);
				value2.Add(defaultMannequinFace);
				component2.SetSharedMaterials(value2);
			}
		}
	}

	private void HandleLoadCosmeticParts(CosmeticSO cosmeticInfo, bool forRightSide)
	{
		if (cosmeticInfo.info.category == CosmeticsController.CosmeticCategory.Set && !cosmeticInfo.info.hasStoreParts)
		{
			CosmeticSO[] setCosmetics = cosmeticInfo.info.setCosmetics;
			foreach (CosmeticSO cosmeticInfo2 in setCosmetics)
			{
				HandleLoadCosmeticParts(cosmeticInfo2, forRightSide);
			}
			return;
		}
		CosmeticPart[] array;
		CosmeticPart[] functionalParts;
		if (cosmeticInfo.info.storeParts.Length != 0)
		{
			array = cosmeticInfo.info.storeParts;
		}
		else
		{
			if (cosmeticInfo.info.category == CosmeticsController.CosmeticCategory.Fur)
			{
				functionalParts = cosmeticInfo.info.functionalParts;
				int i = 0;
				if (i < functionalParts.Length)
				{
					CosmeticPart cosmeticPart = functionalParts[i];
					GameObject obj = LoadAndInstantiatePrefab(cosmeticPart.prefabAssetRef, base.transform);
					obj.GetComponent<GorillaSkinToggle>().ApplyToMannequin(mannequin);
					Object.DestroyImmediate(obj);
					return;
				}
			}
			array = cosmeticInfo.info.wardrobeParts;
		}
		functionalParts = array;
		for (int i = 0; i < functionalParts.Length; i++)
		{
			CosmeticPart cosmeticPart2 = functionalParts[i];
			CosmeticAttachInfo[] attachAnchors = cosmeticPart2.attachAnchors;
			for (int j = 0; j < attachAnchors.Length; j++)
			{
				CosmeticAttachInfo attachInfo = attachAnchors[j];
				if ((!forRightSide || !(attachInfo.selectSide == ECosmeticSelectSide.Left)) && (forRightSide || !(attachInfo.selectSide == ECosmeticSelectSide.Right)))
				{
					_CosmeticPartLoadInfo partLoadInfo = new _CosmeticPartLoadInfo
					{
						playFabId = cosmeticInfo.info.playFabID,
						prefabAssetRef = cosmeticPart2.prefabAssetRef,
						attachInfo = attachInfo,
						xform = null
					};
					GameObject gameObject = LoadAndInstantiatePrefab(cosmeticPart2.prefabAssetRef, base.transform);
					partLoadInfo.xform = gameObject.transform;
					_manuallySpawnedCosmeticParts.Add(gameObject);
					gameObject.SetActive(value: true);
					switch (bustType)
					{
					case BustType.GorillaHead:
					case BustType.GorillaTorso:
					case BustType.GorillaTorsoPost:
					case BustType.GuitarStand:
					case BustType.JewelryBox:
					case BustType.Table:
					case BustType.PinDisplay:
					case BustType.TagEffectDisplay:
						PositionWardRobeItems(gameObject, partLoadInfo);
						break;
					case BustType.GorillaMannequin:
						_manuallySpawnedCosmeticParts.Remove(gameObject);
						Object.DestroyImmediate(gameObject);
						break;
					case BustType.Disabled:
						PositionWithWardRobeOffsets(partLoadInfo);
						break;
					default:
						PositionWithWardRobeOffsets(partLoadInfo);
						break;
					}
				}
			}
		}
	}

	public void LoadCosmeticPartsV2(string playFabId, bool forRightSide = false)
	{
		ClearManuallySpawnedCosmeticParts();
		ClearCosmetics();
		if (!CosmeticsController.instance.TryGetCosmeticInfoV2(playFabId, out var cosmeticInfo))
		{
			switch (playFabId)
			{
			case "NOTHING":
				return;
			case "Slingshot":
				return;
			}
			Debug.LogError("HeadModel.playFabId: Cosmetic id \"" + playFabId + "\" not found in `CosmeticsController`.", this);
		}
		else
		{
			HandleLoadingAllPieces(playFabId, forRightSide, cosmeticInfo);
		}
	}

	private void HandleLoadingAllPieces(string playFabId, bool forRightSide, CosmeticInfoV2 cosmeticInfo)
	{
		CosmeticPart[] array;
		if (cosmeticInfo.storeParts.Length != 0)
		{
			array = cosmeticInfo.storeParts;
		}
		else
		{
			if (cosmeticInfo.category == CosmeticsController.CosmeticCategory.Fur)
			{
				HandleLoadingFur(playFabId, forRightSide, cosmeticInfo);
				return;
			}
			if (cosmeticInfo.category == CosmeticsController.CosmeticCategory.Set)
			{
				CosmeticSO[] setCosmetics = cosmeticInfo.setCosmetics;
				foreach (CosmeticSO cosmeticSO in setCosmetics)
				{
					HandleLoadingAllPieces(playFabId, forRightSide, cosmeticSO.info);
				}
				return;
			}
			array = cosmeticInfo.wardrobeParts;
		}
		CosmeticPart[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			CosmeticPart cosmeticPart = array2[i];
			CosmeticAttachInfo[] attachAnchors = cosmeticPart.attachAnchors;
			for (int j = 0; j < attachAnchors.Length; j++)
			{
				CosmeticAttachInfo attachInfo = attachAnchors[j];
				if ((!forRightSide || !(attachInfo.selectSide == ECosmeticSelectSide.Left)) && (forRightSide || !(attachInfo.selectSide == ECosmeticSelectSide.Right)))
				{
					_CosmeticPartLoadInfo item = new _CosmeticPartLoadInfo
					{
						playFabId = playFabId,
						prefabAssetRef = cosmeticPart.prefabAssetRef,
						attachInfo = attachInfo,
						loadOp = cosmeticPart.prefabAssetRef.InstantiateAsync(base.transform),
						xform = null
					};
					item.loadOp.Completed += _HandleLoadCosmeticPartsV2;
					_loadOp_to_partInfoIndex[item.loadOp] = _currentPartLoadInfos.Count;
					_currentPartLoadInfos.Add(item);
				}
			}
		}
	}

	private void _HandleLoadCosmeticPartsV2(AsyncOperationHandle<GameObject> loadOp)
	{
		if (!_loadOp_to_partInfoIndex.TryGetValue(loadOp, out var value))
		{
			if (loadOp.Status == AsyncOperationStatus.Succeeded && (bool)loadOp.Result)
			{
				Object.Destroy(loadOp.Result);
			}
			return;
		}
		_CosmeticPartLoadInfo partLoadInfo = _currentPartLoadInfos[value];
		if (loadOp.Status == AsyncOperationStatus.Failed)
		{
			Debug.Log("HeadModel: Failed to load a part for cosmetic \"" + partLoadInfo.playFabId + "\"! Waiting for 10 seconds before trying again.", this);
			GTDelayedExec.Add(this, 10f, value);
			return;
		}
		partLoadInfo.xform = loadOp.Result.transform;
		_manuallySpawnedCosmeticParts.Add(partLoadInfo.xform.gameObject);
		switch (bustType)
		{
		case BustType.GorillaTorso:
			PositionWithWardRobeOffsets(partLoadInfo);
			break;
		case BustType.GorillaHead:
			PositionWithWardRobeOffsets(partLoadInfo);
			break;
		case BustType.GorillaTorsoPost:
			PositionWithWardRobeOffsets(partLoadInfo);
			break;
		case BustType.PinDisplay:
			PositionWardRobeItems(partLoadInfo);
			break;
		case BustType.GorillaMannequin:
			_manuallySpawnedCosmeticParts.Remove(partLoadInfo.xform.gameObject);
			Object.DestroyImmediate(partLoadInfo.xform.gameObject);
			break;
		case BustType.GuitarStand:
			PositionWardRobeItems(partLoadInfo);
			break;
		case BustType.JewelryBox:
			PositionWardRobeItems(partLoadInfo);
			break;
		case BustType.Table:
			PositionWardRobeItems(partLoadInfo);
			break;
		case BustType.TagEffectDisplay:
			PositionWardRobeItems(partLoadInfo);
			break;
		case BustType.Disabled:
			PositionWithWardRobeOffsets(partLoadInfo);
			break;
		default:
			PositionWithWardRobeOffsets(partLoadInfo);
			break;
		}
		partLoadInfo.xform.gameObject.SetActive(value: true);
	}

	private void HandleLoadingFur(string playFabId, bool forRightSide, CosmeticInfoV2 cosmeticInfo)
	{
		CosmeticPart[] functionalParts = cosmeticInfo.functionalParts;
		for (int i = 0; i < functionalParts.Length; i++)
		{
			CosmeticPart cosmeticPart = functionalParts[i];
			CosmeticAttachInfo[] attachAnchors = cosmeticPart.attachAnchors;
			for (int j = 0; j < attachAnchors.Length; j++)
			{
				CosmeticAttachInfo attachInfo = attachAnchors[j];
				if ((!forRightSide || !(attachInfo.selectSide == ECosmeticSelectSide.Left)) && (forRightSide || !(attachInfo.selectSide == ECosmeticSelectSide.Right)))
				{
					_CosmeticPartLoadInfo item = new _CosmeticPartLoadInfo
					{
						playFabId = playFabId,
						prefabAssetRef = cosmeticPart.prefabAssetRef,
						attachInfo = attachInfo,
						loadOp = cosmeticPart.prefabAssetRef.InstantiateAsync(base.transform),
						xform = null
					};
					item.loadOp.Completed += _HandleLoadCosmeticPartsV2Fur;
					_loadOp_to_partInfoIndex[item.loadOp] = _currentPartLoadInfos.Count;
					_currentPartLoadInfos.Add(item);
				}
			}
		}
	}

	private void _HandleLoadCosmeticPartsV2Fur(AsyncOperationHandle<GameObject> loadOp)
	{
		if (!_loadOp_to_partInfoIndex.TryGetValue(loadOp, out var value))
		{
			if (loadOp.Status == AsyncOperationStatus.Succeeded && (bool)loadOp.Result)
			{
				Object.Destroy(loadOp.Result);
			}
			return;
		}
		_CosmeticPartLoadInfo cosmeticPartLoadInfo = _currentPartLoadInfos[value];
		if (loadOp.Status == AsyncOperationStatus.Failed)
		{
			Debug.Log("HeadModel: Failed to load a part for cosmetic \"" + cosmeticPartLoadInfo.playFabId + "\"! Waiting for 10 seconds before trying again.", this);
			GTDelayedExec.Add(this, 10f, value);
		}
		else
		{
			cosmeticPartLoadInfo.xform = loadOp.Result.transform;
			cosmeticPartLoadInfo.xform.GetComponent<GorillaSkinToggle>().ApplyToMannequin(mannequin);
			Object.DestroyImmediate(cosmeticPartLoadInfo.xform.gameObject);
		}
	}

	public void SetStandType(BustType newBustType)
	{
		bustType = newBustType;
	}

	private void PositionWardRobeItems(GameObject instantiateEdObject, _CosmeticPartLoadInfo partLoadInfo)
	{
		Transform transform = instantiateEdObject.transform.FindChildRecursive(mountID);
		if (transform != null)
		{
			Debug.Log("Dynamic Cosmetics - Mount Found: " + mountID);
			instantiateEdObject.transform.position = base.transform.position;
			instantiateEdObject.transform.rotation = base.transform.rotation;
			instantiateEdObject.transform.localPosition = transform.localPosition;
			instantiateEdObject.transform.localRotation = transform.localRotation;
		}
		else
		{
			BustType bustType = this.bustType;
			if ((uint)(bustType - 5) <= 2u || bustType == BustType.TagEffectDisplay)
			{
				instantiateEdObject.transform.position = base.transform.position;
				instantiateEdObject.transform.rotation = base.transform.rotation;
			}
			else
			{
				PositionWithWardRobeOffsets(partLoadInfo);
			}
		}
	}

	private void PositionWardRobeItems(_CosmeticPartLoadInfo partLoadInfo)
	{
		Transform transform = partLoadInfo.xform.FindChildRecursive(mountID);
		if (transform != null)
		{
			Debug.Log("Dynamic Cosmetics - Mount Found: " + mountID);
			partLoadInfo.xform.position = base.transform.position;
			partLoadInfo.xform.rotation = base.transform.rotation;
			partLoadInfo.xform.localPosition = transform.localPosition;
			partLoadInfo.xform.localRotation = transform.localRotation;
		}
		else
		{
			BustType bustType = this.bustType;
			if ((uint)(bustType - 5) <= 2u || bustType == BustType.TagEffectDisplay)
			{
				partLoadInfo.xform.position = base.transform.position;
				partLoadInfo.xform.rotation = base.transform.rotation;
			}
			else
			{
				PositionWithWardRobeOffsets(partLoadInfo);
			}
		}
	}

	private void PositionWithWardRobeOffsets(_CosmeticPartLoadInfo partLoadInfo)
	{
		Debug.Log("Dynamic Cosmetics - Mount Not Found: " + mountID);
		partLoadInfo.xform.localPosition = partLoadInfo.attachInfo.offset.pos;
		partLoadInfo.xform.localRotation = partLoadInfo.attachInfo.offset.rot;
		partLoadInfo.xform.localScale = partLoadInfo.attachInfo.offset.scale;
	}

	public void ClearManuallySpawnedCosmeticParts()
	{
		foreach (GameObject manuallySpawnedCosmeticPart in _manuallySpawnedCosmeticParts)
		{
			Object.DestroyImmediate(manuallySpawnedCosmeticPart);
		}
		_manuallySpawnedCosmeticParts.Clear();
	}

	public void ClearCosmetics()
	{
		ResetMannequinSkin();
		for (int num = base.transform.childCount - 1; num >= 0; num--)
		{
			Object.DestroyImmediate(base.transform.GetChild(num).gameObject);
		}
	}

	private GameObject LoadAndInstantiatePrefab(GTAssetRef<GameObject> prefabAssetRef, Transform parent)
	{
		return null;
	}

	public void UpdateCosmeticsMountPositions(CosmeticSO findCosmeticInAllCosmeticsArraySO)
	{
	}
}

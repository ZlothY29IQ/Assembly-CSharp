using System.Collections.Generic;
using Cysharp.Text;
using GorillaExtensions;
using GorillaNetworking;
using GorillaTag;
using GorillaTag.CosmeticSystem;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public class HeadModel : MonoBehaviour, IDelayedExecListener
{
	protected struct _CosmeticPartLoadInfo
	{
		public string playFabId;

		public GTAssetRef<GameObject> prefabAssetRef;

		public CosmeticAttachInfo attachInfo;

		public AsyncOperationHandle<GameObject> loadOp;

		public Transform xform;
	}

	[DebugReadout]
	protected readonly List<_CosmeticPartLoadInfo> _currentPartLoadInfos = new List<_CosmeticPartLoadInfo>(1);

	[DebugReadout]
	private readonly Dictionary<AsyncOperationHandle, int> _loadOp_to_partInfoIndex = new Dictionary<AsyncOperationHandle, int>(1);

	private Renderer _mannequinRenderer;

	public GameObject[] cosmetics;

	protected void Awake()
	{
		RefreshRenderer();
	}

	protected void RefreshRenderer()
	{
		_mannequinRenderer = GetComponentInChildren<Renderer>(includeInactive: true);
	}

	public void SetCosmeticActive(string playFabId, bool forRightSide = false)
	{
		_ClearCurrent();
		_AddPreviewCosmetic(playFabId, forRightSide);
	}

	public void SetCosmeticActiveArray(string[] playFabIds, bool[] forRightSideArray)
	{
		_ClearCurrent();
		for (int i = 0; i < playFabIds.Length; i++)
		{
			_AddPreviewCosmetic(playFabIds[i], forRightSideArray[i]);
		}
	}

	private void _AddPreviewCosmetic(string playFabId, bool forRightSide)
	{
		if (!CosmeticsController.instance.TryGetCosmeticInfoV2(playFabId, out var cosmeticInfo))
		{
			switch (playFabId)
			{
			case "NOTHING":
				return;
			case "Slingshot":
				return;
			}
			Debug.LogError(ZString.Concat("HeadModel._AddPreviewCosmetic: Cosmetic id \"", playFabId, "\" not found in `CosmeticsController`."), this);
			return;
		}
		if (cosmeticInfo.hideWardrobeMannequin)
		{
			if (_mannequinRenderer.IsNull())
			{
				RefreshRenderer();
			}
			if (_mannequinRenderer.IsNotNull())
			{
				_mannequinRenderer.enabled = false;
			}
		}
		CosmeticPart[] wardrobeParts = cosmeticInfo.wardrobeParts;
		for (int i = 0; i < wardrobeParts.Length; i++)
		{
			CosmeticPart cosmeticPart = wardrobeParts[i];
			if (!cosmeticPart.prefabAssetRef.RuntimeKeyIsValid())
			{
				GTDev.LogError("Cosmetic " + cosmeticInfo.displayName + " has missing object reference in wardrobe parts, skipping load");
				continue;
			}
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
					item.loadOp.Completed += _HandleLoadOpOnCompleted;
					_loadOp_to_partInfoIndex[item.loadOp] = _currentPartLoadInfos.Count;
					_currentPartLoadInfos.Add(item);
				}
			}
		}
	}

	private void _HandleLoadOpOnCompleted(AsyncOperationHandle<GameObject> loadOp)
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
			return;
		}
		cosmeticPartLoadInfo.xform = loadOp.Result.transform;
		cosmeticPartLoadInfo.xform.localPosition = cosmeticPartLoadInfo.attachInfo.offset.pos;
		cosmeticPartLoadInfo.xform.localRotation = cosmeticPartLoadInfo.attachInfo.offset.rot;
		cosmeticPartLoadInfo.xform.localScale = cosmeticPartLoadInfo.attachInfo.offset.scale;
		cosmeticPartLoadInfo.xform.gameObject.SetActive(value: true);
	}

	void IDelayedExecListener.OnDelayedAction(int partLoadInfosIndex)
	{
		if (partLoadInfosIndex >= 0 && partLoadInfosIndex < _currentPartLoadInfos.Count)
		{
			_CosmeticPartLoadInfo cosmeticPartLoadInfo = _currentPartLoadInfos[partLoadInfosIndex];
			if (cosmeticPartLoadInfo.loadOp.Status == AsyncOperationStatus.Failed)
			{
				cosmeticPartLoadInfo.loadOp.Completed += _HandleLoadOpOnCompleted;
				cosmeticPartLoadInfo.loadOp = cosmeticPartLoadInfo.prefabAssetRef.InstantiateAsync(base.transform);
				_loadOp_to_partInfoIndex[cosmeticPartLoadInfo.loadOp] = partLoadInfosIndex;
			}
		}
	}

	protected void _ClearCurrent()
	{
		for (int i = 0; i < _currentPartLoadInfos.Count; i++)
		{
			Object.Destroy(_currentPartLoadInfos[i].loadOp.Result);
		}
		_EnsureCapacityAndClear(_loadOp_to_partInfoIndex);
		_EnsureCapacityAndClear(_currentPartLoadInfos);
		if (_mannequinRenderer.IsNull())
		{
			RefreshRenderer();
		}
		_mannequinRenderer.enabled = true;
	}

	private void _EnsureCapacityAndClear<T>(List<T> list)
	{
		if (list.Count > list.Capacity)
		{
			list.Capacity = list.Count;
		}
		list.Clear();
	}

	private void _EnsureCapacityAndClear<T1, T2>(Dictionary<T1, T2> dict)
	{
		dict.EnsureCapacity(dict.Count);
		dict.Clear();
	}
}

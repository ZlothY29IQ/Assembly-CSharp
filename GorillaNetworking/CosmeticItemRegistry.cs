using System.Collections.Generic;
using GorillaTag.CosmeticSystem;
using UnityEngine;

namespace GorillaNetworking;

public class CosmeticItemRegistry
{
	private struct _AwaitCosmeticRequestCallback(AwaitableCompletionSource<CosmeticItemInstance> source) : ICosmeticRequestCallback
	{
		private AwaitableCompletionSource<CosmeticItemInstance> _source = source;

		public void OnCosmeticLoaded(string itemName, CosmeticItemInstance instance)
		{
			_source.SetResult(in instance);
		}
	}

	private Dictionary<string, CosmeticItemInstance> _nameToCosmeticMap = new Dictionary<string, CosmeticItemInstance>();

	private HashSet<GameObject> initializedCosmetics = new HashSet<GameObject>();

	private readonly Dictionary<string, List<ICosmeticRequestCallback>> _pendingCallbacks = new Dictionary<string, List<ICosmeticRequestCallback>>();

	private GameObject _nullItem;

	private VRRig rig;

	private static readonly List<string> _flushKeysBuffer = new List<string>(32);

	public VRRig Rig => rig;

	public void RefreshRig()
	{
		rig.RefreshCosmetics();
	}

	public CosmeticItemRegistry(VRRig _rig)
	{
		rig = _rig;
	}

	public void InitializeCosmetic(GameObject cosmeticGObj, bool isOverride)
	{
		if (initializedCosmetics.Contains(cosmeticGObj))
		{
			return;
		}
		initializedCosmetics.Add(cosmeticGObj);
		if (!isOverride)
		{
			foreach (GameObject overrideCosmetic in rig.overrideCosmetics)
			{
				if (cosmeticGObj.name == overrideCosmetic.name)
				{
					cosmeticGObj.name = "OVERRIDDEN";
					return;
				}
			}
		}
		CosmeticItemInstance cosmeticItemInstance = null;
		string text = cosmeticGObj.name.Replace("LEFT.", "").Replace("RIGHT.", "").TrimEnd();
		if (_nameToCosmeticMap.ContainsKey(text))
		{
			cosmeticItemInstance = _nameToCosmeticMap[text];
		}
		else
		{
			cosmeticItemInstance = new CosmeticItemInstance();
			CosmeticSO cosmeticSOFromDisplayName = CosmeticsController.instance.GetCosmeticSOFromDisplayName(text);
			cosmeticItemInstance.clippingOffsets = ((cosmeticSOFromDisplayName != null) ? cosmeticSOFromDisplayName.info.anchorAntiIntersectOffsets : CosmeticsController.instance.defaultClipOffsets);
			cosmeticItemInstance.isHoldableItem = cosmeticSOFromDisplayName != null && cosmeticSOFromDisplayName.info.hasHoldableParts;
			_nameToCosmeticMap.Add(text, cosmeticItemInstance);
		}
		HoldableObject component = cosmeticGObj.GetComponent<HoldableObject>();
		bool flag = cosmeticGObj.name.Contains("LEFT.");
		bool flag2 = cosmeticGObj.name.Contains("RIGHT.");
		if (cosmeticItemInstance.isHoldableItem && component != null)
		{
			if (component is SnowballThrowable || component is TransferrableObject)
			{
				cosmeticItemInstance.holdableObjects.Add(cosmeticGObj);
			}
			else if (flag)
			{
				cosmeticItemInstance.leftObjects.Add(cosmeticGObj);
			}
			else if (flag2)
			{
				cosmeticItemInstance.rightObjects.Add(cosmeticGObj);
			}
			else
			{
				cosmeticItemInstance.objects.Add(cosmeticGObj);
			}
		}
		else if (flag)
		{
			cosmeticItemInstance.leftObjects.Add(cosmeticGObj);
		}
		else if (flag2)
		{
			cosmeticItemInstance.rightObjects.Add(cosmeticGObj);
		}
		else
		{
			cosmeticItemInstance.objects.Add(cosmeticGObj);
		}
		cosmeticItemInstance.dbgname = text;
		Renderer[] componentsInChildren = cosmeticGObj.GetComponentsInChildren<Renderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (componentsInChildren[i].enabled)
			{
				cosmeticItemInstance.allRenderers.Add(componentsInChildren[i]);
			}
		}
		ParticleSystem[] componentsInChildren2 = cosmeticGObj.GetComponentsInChildren<ParticleSystem>();
		for (int j = 0; j < componentsInChildren2.Length; j++)
		{
			if (componentsInChildren2[j].emission.enabled)
			{
				cosmeticItemInstance.allParticles.Add(componentsInChildren2[j]);
			}
		}
	}

	public CosmeticItemInstance Cosmetic(string playfabId)
	{
		if (string.IsNullOrEmpty(playfabId) || playfabId == "NOTHING")
		{
			return null;
		}
		if (!_nameToCosmeticMap.TryGetValue(playfabId, out var value))
		{
			CosmeticsV2Spawner_Dirty.ProcessLoadOpInfos(rig, playfabId, this);
			return null;
		}
		return value;
	}

	public void RequestCosmetic(string playfabId, ICosmeticRequestCallback callback)
	{
		if (!CosmeticsV2Spawner_Dirty.isPrepared && !CosmeticsV2Spawner_Dirty.isFinalizingSetup)
		{
			Debug.LogError("[GT/CosmeticItemRegistry]  ERROR!!!  RequestCosmetic: Cannot request cosmetic before cosmetic spawner is prepared.");
			return;
		}
		if (string.IsNullOrEmpty(playfabId) || playfabId == "NOTHING")
		{
			callback?.OnCosmeticLoaded(playfabId, null);
			return;
		}
		if (_nameToCosmeticMap.TryGetValue(playfabId, out var value))
		{
			callback?.OnCosmeticLoaded(playfabId, value);
			return;
		}
		if (!_pendingCallbacks.TryGetValue(playfabId, out var value2))
		{
			value2 = new List<ICosmeticRequestCallback>(4);
			_pendingCallbacks.Add(playfabId, value2);
		}
		value2.Add(callback);
		CosmeticsV2Spawner_Dirty.ProcessLoadOpInfos(rig, playfabId, this);
	}

	public Awaitable<CosmeticItemInstance> AwaitCosmetic(string playfabId)
	{
		AwaitableCompletionSource<CosmeticItemInstance> awaitableCompletionSource = new AwaitableCompletionSource<CosmeticItemInstance>();
		if (!CosmeticsV2Spawner_Dirty.isPrepared && !CosmeticsV2Spawner_Dirty.isFinalizingSetup)
		{
			Debug.LogError("[GT/CosmeticItemRegistry]  ERROR!!!  AwaitCosmetic: Cannot request cosmetic before cosmetic spawner is prepared.");
			awaitableCompletionSource.SetResult((CosmeticItemInstance)null);
			return awaitableCompletionSource.Awaitable;
		}
		RequestCosmetic(playfabId, new _AwaitCosmeticRequestCallback(awaitableCompletionSource));
		return awaitableCompletionSource.Awaitable;
	}

	public void FlushPendingCallbacks()
	{
		if (_pendingCallbacks.Count == 0)
		{
			return;
		}
		_flushKeysBuffer.Clear();
		foreach (var (text2, list2) in _pendingCallbacks)
		{
			if (!_nameToCosmeticMap.TryGetValue(text2, out var value))
			{
				continue;
			}
			for (int i = 0; i < list2.Count; i++)
			{
				if (list2[i] != null)
				{
					list2[i].OnCosmeticLoaded(text2, value);
				}
			}
			list2.Clear();
			_flushKeysBuffer.Add(text2);
		}
		for (int j = 0; j < _flushKeysBuffer.Count; j++)
		{
			_pendingCallbacks.Remove(_flushKeysBuffer[j]);
		}
	}
}

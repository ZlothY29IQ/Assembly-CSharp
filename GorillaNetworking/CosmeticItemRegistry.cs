using System.Collections.Generic;
using GorillaTag.CosmeticSystem;
using UnityEngine;

namespace GorillaNetworking;

public class CosmeticItemRegistry
{
	private Dictionary<string, CosmeticItemInstance> _nameToCosmeticMap = new Dictionary<string, CosmeticItemInstance>();

	private HashSet<GameObject> initializedCosmetics = new HashSet<GameObject>();

	private GameObject _nullItem;

	private VRRig rig;

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

	public CosmeticItemInstance Cosmetic(string itemName)
	{
		if (string.IsNullOrEmpty(itemName) || itemName == "NOTHING")
		{
			return null;
		}
		if (!_nameToCosmeticMap.TryGetValue(itemName, out var value))
		{
			CosmeticsV2Spawner_Dirty.ProcessLoadOpInfos(rig, itemName, this);
			return null;
		}
		return value;
	}
}

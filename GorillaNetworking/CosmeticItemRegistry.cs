using System.Collections.Generic;
using GorillaTag.CosmeticSystem;
using UnityEngine;

namespace GorillaNetworking;

public class CosmeticItemRegistry
{
	private bool _isInitialized;

	private Dictionary<string, CosmeticItemInstance> nameToCosmeticMap = new Dictionary<string, CosmeticItemInstance>();

	private GameObject nullItem;

	public void Initialize(GameObject[] cosmeticGObjs)
	{
		if (_isInitialized)
		{
			return;
		}
		_isInitialized = true;
		foreach (GameObject gameObject in cosmeticGObjs)
		{
			CosmeticItemInstance cosmeticItemInstance = null;
			string text = gameObject.name.Replace("LEFT.", "").Replace("RIGHT.", "").TrimEnd();
			if (nameToCosmeticMap.ContainsKey(text))
			{
				cosmeticItemInstance = nameToCosmeticMap[text];
			}
			else
			{
				cosmeticItemInstance = new CosmeticItemInstance();
				CosmeticSO cosmeticSOFromDisplayName = CosmeticsController.instance.GetCosmeticSOFromDisplayName(text);
				cosmeticItemInstance.clippingOffsets = ((cosmeticSOFromDisplayName != null) ? cosmeticSOFromDisplayName.info.anchorAntiIntersectOffsets : CosmeticsController.instance.defaultClipOffsets);
				cosmeticItemInstance.isHoldableItem = cosmeticSOFromDisplayName != null && cosmeticSOFromDisplayName.info.hasHoldableParts;
				nameToCosmeticMap.Add(text, cosmeticItemInstance);
			}
			HoldableObject component = gameObject.GetComponent<HoldableObject>();
			bool flag = gameObject.name.Contains("LEFT.");
			bool flag2 = gameObject.name.Contains("RIGHT.");
			if (cosmeticItemInstance.isHoldableItem && component != null)
			{
				if (component is SnowballThrowable || component is TransferrableObject)
				{
					cosmeticItemInstance.holdableObjects.Add(gameObject);
				}
				else if (flag)
				{
					cosmeticItemInstance.leftObjects.Add(gameObject);
				}
				else if (flag2)
				{
					cosmeticItemInstance.rightObjects.Add(gameObject);
				}
				else
				{
					cosmeticItemInstance.objects.Add(gameObject);
				}
			}
			else if (flag)
			{
				cosmeticItemInstance.leftObjects.Add(gameObject);
			}
			else if (flag2)
			{
				cosmeticItemInstance.rightObjects.Add(gameObject);
			}
			else
			{
				cosmeticItemInstance.objects.Add(gameObject);
			}
			cosmeticItemInstance.dbgname = text;
		}
	}

	public CosmeticItemInstance Cosmetic(string itemName)
	{
		if (!_isInitialized)
		{
			Debug.LogError("Tried to use CosmeticItemRegistry before it was initialized!");
			return null;
		}
		if (string.IsNullOrEmpty(itemName) || itemName == "NOTHING")
		{
			return null;
		}
		if (!nameToCosmeticMap.TryGetValue(itemName, out var value))
		{
			return null;
		}
		return value;
	}
}

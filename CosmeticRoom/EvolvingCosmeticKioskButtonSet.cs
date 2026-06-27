using System;
using DefaultNamespace;
using GorillaNetworking;
using GorillaNetworking.Store;
using UnityEngine;

namespace CosmeticRoom;

public class EvolvingCosmeticKioskButtonSet : MonoBehaviour
{
	[SerializeField]
	private DynamicCosmeticStand _cosmeticStand;

	[SerializeField]
	private GorillaPressableButton _plusButton;

	[SerializeField]
	private GorillaPressableButton _minusButton;

	private EvolvingCosmeticKiosk _kiosk;

	private EvolvingCosmetic _cosmetic;

	private string _playfabId;

	public void RegisterKiosk(EvolvingCosmeticKiosk kiosk)
	{
		if (_kiosk != null)
		{
			throw new Exception("Attempted to double-register EvolvingCosmeticKiosk to a button.");
		}
		_kiosk = kiosk;
	}

	public void Reset()
	{
		_cosmeticStand.ClearCosmetics();
		_playfabId = null;
		_cosmetic = null;
	}

	public void SetCosmetic(string playfabId, EvolvingCosmetic evolvingCosmetic)
	{
		_cosmeticStand.SpawnItemOntoStand(playfabId);
		_playfabId = playfabId;
		_cosmetic = evolvingCosmetic;
	}

	public void GoForward()
	{
		if (!(_cosmetic == null) && _cosmetic.CanGoForward())
		{
			_cosmetic.GoForward();
			RefreshOnPlayer();
		}
	}

	public void GoBackward()
	{
		if (!(_cosmetic == null) && _cosmetic.CanGoBack())
		{
			_cosmetic.GoBack();
			RefreshOnPlayer();
		}
	}

	private void RefreshOnPlayer()
	{
		if (_kiosk == null || _playfabId == null || _cosmetic == null)
		{
			return;
		}
		bool flag = false;
		CosmeticsController.CosmeticItem[] items = CosmeticsController.instance.currentWornSet.items;
		for (int i = 0; i < items.Length; i++)
		{
			if (items[i].itemName != _playfabId)
			{
				continue;
			}
			CosmeticItemInstance cosmeticItemInstance = _kiosk.VRRig.cosmeticsObjectRegistry.Cosmetic(_playfabId);
			if (cosmeticItemInstance == null)
			{
				continue;
			}
			foreach (GameObject @object in cosmeticItemInstance.objects)
			{
				EvolvingCosmetic component = @object.GetComponent<EvolvingCosmetic>();
				if ((object)component != null)
				{
					component.MatchStage(_cosmetic);
					EvolvingCosmeticSaveData.Instance.SelectedIndices[component.PlayfabId] = component.SelectedObjectIndex;
					flag = true;
				}
			}
		}
		if (flag)
		{
			PlayerPrefs.SetString("EvolvingCosmeticSaveData", EvolvingCosmeticSaveData.Instance.Write());
		}
	}
}

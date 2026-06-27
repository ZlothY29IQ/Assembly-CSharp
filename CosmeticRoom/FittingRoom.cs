using System.Collections.Generic;
using GorillaNetworking;
using UnityEngine;

namespace CosmeticRoom;

public class FittingRoom : MonoBehaviour
{
	public FittingRoomButton[] fittingRoomButtons;

	public GameObject consoleMesh;

	private int iterator;

	public bool addOnEnable;

	public void InitializeForCustomMap(bool useCustomConsoleMesh = true)
	{
		consoleMesh?.SetActive(!useCustomConsoleMesh);
		CosmeticsController.instance.AddFittingRoom(this);
	}

	private void OnEnable()
	{
		if (addOnEnable)
		{
			CosmeticsController.instance.AddFittingRoom(this);
		}
	}

	private void OnDisable()
	{
		if (addOnEnable)
		{
			CosmeticsController.instance.RemoveFittingRoom(this);
		}
	}

	public void UpdateFromCart(List<CosmeticsController.CosmeticItem> currentCart, CosmeticsController.CosmeticSet tryOnSet)
	{
		for (iterator = 0; iterator < fittingRoomButtons.Length; iterator++)
		{
			if (iterator < currentCart.Count)
			{
				bool isInTryOnSet = CosmeticsController.instance.AnyMatch(tryOnSet, currentCart[iterator]) || (!CosmeticsController.instance.tryOnCollectableItem.isNullItem && currentCart[iterator].itemName == CosmeticsController.instance.tryOnCollectableItem.itemName);
				fittingRoomButtons[iterator].SetItem(currentCart[iterator], isInTryOnSet);
			}
			else
			{
				fittingRoomButtons[iterator].ClearItem();
			}
		}
	}
}

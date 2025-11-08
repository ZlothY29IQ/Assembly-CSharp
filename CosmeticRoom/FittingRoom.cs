using System.Collections.Generic;
using GorillaNetworking;
using UnityEngine;

namespace CosmeticRoom;

public class FittingRoom : MonoBehaviour
{
	public FittingRoomButton[] fittingRoomButtons;

	public GameObject consoleMesh;

	private int iterator;

	public void InitializeForCustomMap(bool useCustomConsoleMesh = true)
	{
		consoleMesh?.SetActive(!useCustomConsoleMesh);
		CosmeticsController.instance.AddFittingRoom(this);
	}

	public void UpdateFromCart(List<CosmeticsController.CosmeticItem> currentCart, CosmeticsController.CosmeticSet tryOnSet)
	{
		for (iterator = 0; iterator < fittingRoomButtons.Length; iterator++)
		{
			if (iterator < currentCart.Count)
			{
				bool isInTryOnSet = CosmeticsController.instance.AnyMatch(tryOnSet, currentCart[iterator]);
				fittingRoomButtons[iterator].SetItem(currentCart[iterator], isInTryOnSet);
			}
			else
			{
				fittingRoomButtons[iterator].ClearItem();
			}
		}
	}
}

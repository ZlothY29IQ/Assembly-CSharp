using GorillaNetworking;
using UnityEngine;

public class NonCosmeticHandItem : MonoBehaviour
{
	public CosmeticsController.CosmeticSlots cosmeticSlots;

	public GameObject itemPrefab;

	public bool IsEnabled
	{
		get
		{
			if (!itemPrefab)
			{
				return false;
			}
			return itemPrefab.gameObject.activeSelf;
		}
	}

	public void EnableItem(bool enable)
	{
		if ((bool)itemPrefab)
		{
			itemPrefab.gameObject.SetActive(enable);
		}
	}
}

using UnityEngine;

namespace GorillaTag.CosmeticSystem;

public class AllCosmeticsArraySO : ScriptableObject
{
	[SerializeField]
	public GTDirectAssetRef<CosmeticSO>[] sturdyAssetRefs;

	public CosmeticSO SearchForCosmeticSO(string playfabId)
	{
		GTDirectAssetRef<CosmeticSO>[] array = sturdyAssetRefs;
		foreach (CosmeticSO cosmeticSO in array)
		{
			if (cosmeticSO.info.playFabID == playfabId)
			{
				return cosmeticSO;
			}
		}
		Debug.LogWarning("AllCosmeticsArraySO - SearchForCosmeticSO - No Cosmetic found with playfabId: " + playfabId, this);
		return null;
	}
}

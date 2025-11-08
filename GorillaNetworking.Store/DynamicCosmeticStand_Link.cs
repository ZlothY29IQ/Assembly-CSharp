using UnityEngine;

namespace GorillaNetworking.Store;

public class DynamicCosmeticStand_Link : MonoBehaviour
{
	public DynamicCosmeticStand stand;

	public void SetStandType(HeadModel_CosmeticStand.BustType type)
	{
		stand.SetStandType(type);
	}

	public void SpawnItemOntoStand(string PlayFabID)
	{
		stand.SpawnItemOntoStand(PlayFabID);
	}

	public void SaveCosmeticMountPosition()
	{
		stand.UpdateCosmeticsMountPositions();
	}

	public void ClearCosmeticItems()
	{
		stand.ClearCosmetics();
	}
}

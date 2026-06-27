using System;
using GT_CustomMapSupportRuntime;
using GorillaNetworking.Store;

namespace GorillaTagScripts.VirtualStumpCustomMaps;

[Serializable]
public struct CustomMapCosmeticItem
{
	public GTObjectPlaceholder.ECustomMapCosmeticItem customMapItemSlot;

	public HeadModel_CosmeticStand.BustType bustType;

	public string playFabID;
}

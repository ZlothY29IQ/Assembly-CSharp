using UnityEngine;

namespace GorillaNetworking.Store;

public class StoreDisplay : MonoBehaviour
{
	public string displayName = "";

	public DynamicCosmeticStand[] Stands;

	private void GetAllDynamicCosmeticStands()
	{
		Stands = GetComponentsInChildren<DynamicCosmeticStand>();
	}

	private void SetDisplayNameForAllStands()
	{
		DynamicCosmeticStand[] componentsInChildren = GetComponentsInChildren<DynamicCosmeticStand>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].CopyChildsName();
		}
	}
}

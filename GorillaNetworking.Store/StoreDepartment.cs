using System;
using UnityEngine;

namespace GorillaNetworking.Store;

public class StoreDepartment : MonoBehaviour
{
	public StoreDisplay[] Displays;

	public string departmentName = "";

	private void FindAllDisplays()
	{
		Displays = GetComponentsInChildren<StoreDisplay>();
		for (int num = Displays.Length - 1; num >= 0; num--)
		{
			if (string.IsNullOrEmpty(Displays[num].displayName))
			{
				Displays[num] = Displays[Displays.Length - 1];
				Array.Resize(ref Displays, Displays.Length - 1);
			}
		}
	}
}

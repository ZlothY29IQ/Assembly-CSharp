using System.Collections.Generic;
using UnityEngine;

public class GRReadyRoom : MonoBehaviour
{
	public List<GRNameDisplayPlate> nameDisplayPlates;

	public void RefreshRigs(List<VRRig> vrRigs)
	{
		for (int i = 0; i < nameDisplayPlates.Count; i++)
		{
			if (nameDisplayPlates != null)
			{
				if (i < vrRigs.Count && vrRigs[i] != null && vrRigs[i].OwningNetPlayer != null)
				{
					nameDisplayPlates[i].RefreshPlayerName(vrRigs[i]);
				}
				else
				{
					nameDisplayPlates[i].Clear();
				}
			}
		}
	}
}

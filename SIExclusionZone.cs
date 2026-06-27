using System.Collections.Generic;
using UnityEngine;

public class SIExclusionZone : MonoBehaviour
{
	public SIExclusionType exclusionType = SIExclusionType.AffectsOthers;

	private List<SIGadget> gadgetsInZone = new List<SIGadget>();

	private List<SIPlayer> playersInZone = new List<SIPlayer>();

	private void OnDisable()
	{
		foreach (SIGadget item in gadgetsInZone)
		{
			if (item != null)
			{
				item.LeaveExclusionZone(this);
			}
		}
		gadgetsInZone.Clear();
		if ((exclusionType & SIExclusionType.AffectsOthers) != 0)
		{
			foreach (SIPlayer item2 in playersInZone)
			{
				if (item2 != null)
				{
					item2.exclusionZoneCount--;
				}
			}
		}
		playersInZone.Clear();
	}

	private void OnTriggerEnter(Collider other)
	{
		SIGadget componentInParent = other.GetComponentInParent<SIGadget>();
		if (componentInParent != null)
		{
			if (!gadgetsInZone.Contains(componentInParent))
			{
				gadgetsInZone.Add(componentInParent);
			}
			componentInParent.ApplyExclusionZone(this);
		}
		SIPlayer componentInParent2 = other.GetComponentInParent<SIPlayer>();
		if (componentInParent2 != null && !playersInZone.Contains(componentInParent2))
		{
			playersInZone.Add(componentInParent2);
			if ((exclusionType & SIExclusionType.AffectsOthers) != 0)
			{
				componentInParent2.exclusionZoneCount++;
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		SIGadget componentInParent = other.GetComponentInParent<SIGadget>();
		if (componentInParent != null && gadgetsInZone.Contains(componentInParent))
		{
			componentInParent.LeaveExclusionZone(this);
			gadgetsInZone.Remove(componentInParent);
		}
		SIPlayer componentInParent2 = other.GetComponentInParent<SIPlayer>();
		if (componentInParent2 != null && playersInZone.Contains(componentInParent2))
		{
			playersInZone.Remove(componentInParent2);
			if ((exclusionType & SIExclusionType.AffectsOthers) != 0)
			{
				componentInParent2.exclusionZoneCount--;
			}
		}
	}

	public void ClearGadget(SIGadget gadget)
	{
		gadgetsInZone.Remove(gadget);
	}
}

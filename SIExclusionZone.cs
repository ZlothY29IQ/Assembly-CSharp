using System.Collections.Generic;
using UnityEngine;

public class SIExclusionZone : MonoBehaviour
{
	private List<SIGadget> gadgetsInZone = new List<SIGadget>();

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
	}

	private void OnTriggerEnter(Collider other)
	{
		SIGadget componentInParent = other.GetComponentInParent<SIGadget>();
		if (!(componentInParent == null))
		{
			if (!gadgetsInZone.Contains(componentInParent))
			{
				gadgetsInZone.Add(componentInParent);
			}
			componentInParent.ApplyExclusionZone(this);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		SIGadget componentInParent = other.GetComponentInParent<SIGadget>();
		if (!(componentInParent == null) && gadgetsInZone.Contains(componentInParent))
		{
			componentInParent.LeaveExclusionZone(this);
			gadgetsInZone.Remove(componentInParent);
		}
	}

	public void ClearGadget(SIGadget gadget)
	{
		gadgetsInZone.Remove(gadget);
	}
}

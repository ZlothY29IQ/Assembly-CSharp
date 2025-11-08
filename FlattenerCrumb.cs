using System.Collections.Generic;
using GorillaTag;
using UnityEngine;

public class FlattenerCrumb : MonoBehaviour
{
	[DebugReadout]
	private List<ObjectHierarchyFlattener> flattenerList = new List<ObjectHierarchyFlattener>();

	private void OnDisable()
	{
		for (int num = flattenerList.Count - 1; num >= 0; num--)
		{
			flattenerList[num].CrumbDisabled();
		}
	}

	public void AddFlattenerReference(ObjectHierarchyFlattener flattener)
	{
		flattenerList.AddIfNew(flattener);
	}
}

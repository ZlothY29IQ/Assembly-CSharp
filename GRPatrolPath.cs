using System;
using System.Collections.Generic;
using UnityEngine;

public class GRPatrolPath : MonoBehaviour
{
	[NonSerialized]
	public List<Transform> patrolNodes;

	public int index;

	private void Awake()
	{
		patrolNodes = new List<Transform>(base.transform.childCount);
		for (int i = 0; i < base.transform.childCount; i++)
		{
			patrolNodes.Add(base.transform.GetChild(i));
		}
	}

	public void OnDrawGizmosSelected()
	{
		if (patrolNodes == null || base.transform.childCount != patrolNodes.Count)
		{
			patrolNodes = new List<Transform>(base.transform.childCount);
			for (int i = 0; i < base.transform.childCount; i++)
			{
				patrolNodes.Add(base.transform.GetChild(i));
			}
		}
		if (patrolNodes == null)
		{
			return;
		}
		for (int j = 0; j < patrolNodes.Count; j++)
		{
			Gizmos.color = Color.magenta;
			Gizmos.DrawCube(patrolNodes[j].transform.position, Vector3.one * 0.5f);
			if (j < patrolNodes.Count - 1)
			{
				Gizmos.DrawLine(patrolNodes[j].transform.position, patrolNodes[j + 1].transform.position);
			}
		}
	}
}

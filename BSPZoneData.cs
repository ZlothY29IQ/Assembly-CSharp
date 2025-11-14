using System;
using System.Collections.Generic;
using UnityEngine;

public class BSPZoneData : MonoBehaviour
{
	[SerializeField]
	private int priority;

	[NonSerialized]
	public List<BoxCollider> boxList;

	public int Priority => priority;

	public string ZoneName => base.gameObject.name;
}

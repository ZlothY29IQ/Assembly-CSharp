using System;
using UnityEngine;

[Serializable]
internal class RoomCountForZone
{
	[SerializeField]
	private GTZone zone;

	[SerializeField]
	private int count;

	public int Count => count;

	public GTZone Zone => zone;
}

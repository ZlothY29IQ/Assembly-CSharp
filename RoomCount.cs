using System;
using GorillaGameModes;
using UnityEngine;

[Serializable]
internal class RoomCount : PrivateRoomCount
{
	[SerializeField]
	private RoomCountForZone[] zoneCountOverrides;

	public int GetRoomCount(GTZone zone)
	{
		for (int i = 0; i < zoneCountOverrides.Length; i++)
		{
			if (zoneCountOverrides[i].Zone == zone)
			{
				return zoneCountOverrides[i].Count;
			}
		}
		return count;
	}

	public override int GetRoomCount(GTZone zone, GameModeType mode)
	{
		return Mathf.Min(GetRoomCount(zone), GetRoomCount(mode));
	}
}

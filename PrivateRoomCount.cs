using System;
using GorillaGameModes;
using UnityEngine;

[Serializable]
internal class PrivateRoomCount
{
	[SerializeField]
	protected int count;

	[SerializeField]
	protected RoomCountForMode[] modeCountOverrides;

	public int GetRoomCount()
	{
		return count;
	}

	public int GetRoomCount(GameModeType mode)
	{
		for (int i = 0; i < modeCountOverrides.Length; i++)
		{
			if (modeCountOverrides[i].Mode == mode)
			{
				return modeCountOverrides[i].Count;
			}
		}
		return count;
	}

	public virtual int GetRoomCount(GTZone zone, GameModeType mode)
	{
		return GetRoomCount(mode);
	}
}

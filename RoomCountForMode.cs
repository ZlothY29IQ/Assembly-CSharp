using System;
using GorillaGameModes;
using UnityEngine;

[Serializable]
internal class RoomCountForMode
{
	[SerializeField]
	private GameModeType mode;

	[SerializeField]
	private int count;

	public int Count => count;

	public GameModeType Mode => mode;
}

using System;
using UnityEngine;

namespace GorillaGameModes;

public class GameModeString
{
	public string zone;

	public string queue;

	public string gameType;

	public string modId;

	public string modFileId;

	public override string ToString()
	{
		return zone + ";" + queue + ";" + gameType + ";" + modId + ";" + modFileId;
	}

	public static GameModeString FromString(string gameModeString)
	{
		string[] array = gameModeString.Split(";");
		if (array.Length != 5)
		{
			Debug.LogError("[GameModeString::FromString] Invalid game mode string: " + gameModeString);
			return null;
		}
		return new GameModeString
		{
			zone = array[0],
			queue = array[1],
			gameType = array[2],
			modId = array[3],
			modFileId = array[4]
		};
	}

	public static bool DoesPropertyStringContainGameMode(string propertyString, string gameMode)
	{
		return MemoryExtensions.Equals(GameTypeFromPropertyString(propertyString), gameMode, StringComparison.Ordinal);
	}

	public static ReadOnlySpan<char> GameTypeFromPropertyString(string propertyString)
	{
		if (string.IsNullOrEmpty(propertyString))
		{
			return null;
		}
		int num = propertyString.IndexOf(';');
		if (num < 0)
		{
			return null;
		}
		num = propertyString.IndexOf(';', ++num);
		if (num < 0)
		{
			return null;
		}
		int num2 = propertyString.IndexOf(';', ++num);
		if (num2 < 0)
		{
			return null;
		}
		return MemoryExtensions.AsSpan(propertyString, num, num2 - num);
	}
}

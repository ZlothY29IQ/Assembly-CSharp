using System.Collections.Generic;
using GorillaGameModes;
using UnityEngine;

public class GameModeSpecificObjectRegistry : MonoBehaviour
{
	private Dictionary<GameModeType, List<GameModeSpecificObject>> gameModeSpecificObjects = new Dictionary<GameModeType, List<GameModeSpecificObject>>();

	private GameModeType currentGameType = GameModeType.Count;

	private void OnEnable()
	{
		GameModeSpecificObject.OnAwake += GameModeSpecificObject_OnAwake;
		GameModeSpecificObject.OnDestroyed += GameModeSpecificObject_OnDestroyed;
		GameMode.OnStartGameMode += GameMode_OnStartGameMode;
	}

	private void OnDisable()
	{
		GameModeSpecificObject.OnAwake -= GameModeSpecificObject_OnAwake;
		GameModeSpecificObject.OnDestroyed -= GameModeSpecificObject_OnDestroyed;
		GameMode.OnStartGameMode -= GameMode_OnStartGameMode;
	}

	private void GameModeSpecificObject_OnAwake(GameModeSpecificObject obj)
	{
		foreach (GameModeType gameMode in obj.GameModes)
		{
			if (!gameModeSpecificObjects.ContainsKey(gameMode))
			{
				gameModeSpecificObjects.Add(gameMode, new List<GameModeSpecificObject>());
			}
			gameModeSpecificObjects[gameMode].Add(obj);
		}
		if (GameMode.ActiveGameMode == null)
		{
			obj.gameObject.SetActive(obj.Validation == GameModeSpecificObject.ValidationMethod.Exclusion);
		}
		else
		{
			obj.gameObject.SetActive(obj.CheckValid(GameMode.ActiveGameMode.GameType()));
		}
	}

	private void GameModeSpecificObject_OnDestroyed(GameModeSpecificObject obj)
	{
		foreach (GameModeType gameMode in obj.GameModes)
		{
			if (gameModeSpecificObjects.ContainsKey(gameMode))
			{
				gameModeSpecificObjects[gameMode].Remove(obj);
			}
		}
	}

	private void GameMode_OnStartGameMode(GameModeType newGameModeType)
	{
		if (currentGameType == newGameModeType)
		{
			return;
		}
		if (gameModeSpecificObjects.ContainsKey(currentGameType))
		{
			foreach (GameModeSpecificObject item in gameModeSpecificObjects[currentGameType])
			{
				item.gameObject.SetActive(item.CheckValid(newGameModeType));
			}
		}
		if (gameModeSpecificObjects.ContainsKey(newGameModeType))
		{
			foreach (GameModeSpecificObject item2 in gameModeSpecificObjects[newGameModeType])
			{
				item2.gameObject.SetActive(item2.CheckValid(newGameModeType));
			}
		}
		currentGameType = newGameModeType;
	}
}

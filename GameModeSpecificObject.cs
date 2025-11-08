using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GorillaGameModes;
using UnityEngine;

public class GameModeSpecificObject : MonoBehaviour
{
	public delegate void GameModeSpecificObjectDelegate(GameModeSpecificObject gameModeSpecificObject);

	[Serializable]
	public enum ValidationMethod
	{
		Inclusion,
		Exclusion
	}

	[SerializeField]
	private ValidationMethod validationMethod;

	[SerializeField]
	private GameModeType[] _gameModes;

	private List<GameModeType> gameModes;

	public ValidationMethod Validation => validationMethod;

	public List<GameModeType> GameModes => gameModes;

	public static event GameModeSpecificObjectDelegate OnAwake;

	public static event GameModeSpecificObjectDelegate OnDestroyed;

	private async void Awake()
	{
		gameModes = new List<GameModeType>(_gameModes);
		await Task.Yield();
		if (GameModeSpecificObject.OnAwake != null)
		{
			GameModeSpecificObject.OnAwake(this);
		}
	}

	private void OnDestroy()
	{
		if (GameModeSpecificObject.OnDestroyed != null)
		{
			GameModeSpecificObject.OnDestroyed(this);
		}
	}

	public bool CheckValid(GameModeType gameMode)
	{
		if (validationMethod == ValidationMethod.Exclusion)
		{
			return !gameModes.Contains(gameMode);
		}
		return gameModes.Contains(gameMode);
	}
}

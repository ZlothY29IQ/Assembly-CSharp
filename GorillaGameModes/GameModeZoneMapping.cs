using System.Collections.Generic;
using GameObjectScheduling;
using GorillaNetworking;
using UnityEngine;

namespace GorillaGameModes;

[CreateAssetMenu(fileName = "New Game Mode Zone Map", menuName = "Game Settings/Game Mode Zone Map", order = 2)]
public class GameModeZoneMapping : ScriptableObject
{
	[SerializeField]
	[TextArea(4, 40)]
	private string notes;

	[SerializeField]
	private GameModeNameOverrides[] gameModeNameOverrides;

	[SerializeField]
	private GameModeType[] defaultGameModes;

	[SerializeField]
	private GameModeType[] bigRoomGameModes;

	[SerializeField]
	private ZoneGameModes[] zoneGameModes;

	[SerializeField]
	private GameModeTypeCountdown[] gameModeTypeCountdowns;

	[SerializeField]
	private GameModeType[] newThisUpdate;

	private Dictionary<GTZone, HashSet<GameModeType>> bigRoomZoneGameModesLookup;

	private Dictionary<GTZone, HashSet<GameModeType>> publicZoneGameModesLookup;

	private Dictionary<GTZone, HashSet<GameModeType>> privateZoneGameModesLookup;

	private Dictionary<GameModeType, string> modeNameLookup;

	private HashSet<GameModeType> isNewLookup;

	private Dictionary<GameModeType, CountdownTextDate> gameModeTypeCountdownsLookup;

	private HashSet<GameModeType> allModes;

	public HashSet<GameModeType> AllModes
	{
		get
		{
			Init();
			return allModes;
		}
	}

	private void Init()
	{
		if (allModes != null)
		{
			return;
		}
		allModes = new HashSet<GameModeType>();
		for (int i = 0; i < defaultGameModes.Length; i++)
		{
			allModes.Add(defaultGameModes[i]);
		}
		bigRoomZoneGameModesLookup = new Dictionary<GTZone, HashSet<GameModeType>>();
		publicZoneGameModesLookup = new Dictionary<GTZone, HashSet<GameModeType>>();
		privateZoneGameModesLookup = new Dictionary<GTZone, HashSet<GameModeType>>();
		for (int j = 0; j < zoneGameModes.Length; j++)
		{
			for (int k = 0; k < zoneGameModes[j].zone.Length; k++)
			{
				publicZoneGameModesLookup.Add(zoneGameModes[j].zone[k], new HashSet<GameModeType>(zoneGameModes[j].modes));
				for (int l = 0; l < zoneGameModes[j].modes.Length; l++)
				{
					if (!allModes.Contains(zoneGameModes[j].modes[l]))
					{
						allModes.Add(zoneGameModes[j].modes[l]);
					}
				}
				if (zoneGameModes[j].privateModes.Length != 0)
				{
					privateZoneGameModesLookup.Add(zoneGameModes[j].zone[k], new HashSet<GameModeType>(zoneGameModes[j].privateModes));
					for (int m = 0; m < zoneGameModes[j].privateModes.Length; m++)
					{
						if (!allModes.Contains(zoneGameModes[j].privateModes[m]))
						{
							allModes.Add(zoneGameModes[j].privateModes[m]);
						}
					}
				}
				else
				{
					privateZoneGameModesLookup.Add(zoneGameModes[j].zone[k], new HashSet<GameModeType>(zoneGameModes[j].modes));
				}
			}
		}
		modeNameLookup = new Dictionary<GameModeType, string>();
		for (int n = 0; n < gameModeNameOverrides.Length; n++)
		{
			modeNameLookup.Add(gameModeNameOverrides[n].mode, gameModeNameOverrides[n].displayName);
		}
		isNewLookup = new HashSet<GameModeType>(newThisUpdate);
		gameModeTypeCountdownsLookup = new Dictionary<GameModeType, CountdownTextDate>();
		for (int num = 0; num < gameModeTypeCountdowns.Length; num++)
		{
			gameModeTypeCountdownsLookup.Add(gameModeTypeCountdowns[num].mode, gameModeTypeCountdowns[num].countdownTextDate);
		}
	}

	public HashSet<GameModeType> GetModesForZone(GTZone zone, bool isPrivate)
	{
		Init();
		if (isPrivate && privateZoneGameModesLookup.ContainsKey(zone))
		{
			return privateZoneGameModesLookup[zone];
		}
		if (publicZoneGameModesLookup.ContainsKey(zone))
		{
			return publicZoneGameModesLookup[zone];
		}
		return new HashSet<GameModeType>(defaultGameModes);
	}

	public bool IsBigRoomMode(GameModeType gameModeType)
	{
		for (int i = 0; i < bigRoomGameModes.Length; i++)
		{
			if (bigRoomGameModes[i] == gameModeType)
			{
				return true;
			}
		}
		return false;
	}

	internal string GetModeName(GameModeType mode)
	{
		Init();
		if (modeNameLookup.ContainsKey(mode))
		{
			return modeNameLookup[mode];
		}
		return mode.ToString().ToUpper();
	}

	internal bool IsNew(GameModeType mode)
	{
		Init();
		return isNewLookup.Contains(mode);
	}

	internal CountdownTextDate GetCountdown(GameModeType mode)
	{
		Init();
		if (gameModeTypeCountdownsLookup.ContainsKey(mode))
		{
			return gameModeTypeCountdownsLookup[mode];
		}
		return null;
	}

	internal GameModeType VerifyModeForZone(GTZone zone, GameModeType mode, bool isPrivate)
	{
		if (GorillaComputer.instance.IsPlayerInVirtualStump())
		{
			zone = GTZone.customMaps;
		}
		if (zone == GTZone.none)
		{
			if (allModes.Contains(mode))
			{
				return mode;
			}
			return GameModeType.Casual;
		}
		bool flag = PlayerPrefFlags.Check(PlayerPrefFlags.Flag.GAME_MODE_SELECTOR_IS_SUPER);
		if (!flag)
		{
			switch (mode)
			{
			case GameModeType.SuperCasual:
				mode = GameModeType.Casual;
				break;
			case GameModeType.SuperInfect:
				mode = GameModeType.Infection;
				break;
			}
		}
		HashSet<GameModeType> hashSet = ((isPrivate && privateZoneGameModesLookup.ContainsKey(zone)) ? privateZoneGameModesLookup[zone] : ((!publicZoneGameModesLookup.ContainsKey(zone)) ? new HashSet<GameModeType>(defaultGameModes) : publicZoneGameModesLookup[zone]));
		if (hashSet.Contains(mode))
		{
			return mode;
		}
		GameModeType result = GameModeType.Casual;
		foreach (GameModeType item in hashSet)
		{
			if (flag || (item != GameModeType.SuperCasual && item != GameModeType.SuperInfect))
			{
				return item;
			}
		}
		return result;
	}
}

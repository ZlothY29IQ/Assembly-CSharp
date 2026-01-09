using System;

namespace GorillaGameModes;

[Serializable]
public enum GameModeType
{
	Casual = 0,
	Infection = 1,
	HuntDown = 2,
	Paintbrawl = 3,
	Ambush = 4,
	FreezeTag = 5,
	Ghost = 6,
	Custom = 7,
	Guardian = 8,
	PropHunt = 9,
	InfectionCompetitive = 10,
	SuperInfect = 11,
	SuperCasual = 12,
	Count = 13,
	None = -1
}

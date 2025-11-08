using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

[Serializable]
[JsonConverter(typeof(StringEnumConverter))]
public enum QuestType
{
	none,
	gameModeObjective,
	gameModeRound,
	grabObject,
	dropObject,
	eatObject,
	tapObject,
	launchedProjectile,
	moveDistance,
	swimDistance,
	triggerHandEffect,
	enterLocation,
	misc,
	critter,
	fetchObject,
	playerInteraction
}

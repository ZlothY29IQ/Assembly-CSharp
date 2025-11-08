using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

[Serializable]
[JsonConverter(typeof(StringEnumConverter))]
public enum QuestCategory
{
	NONE,
	Social,
	Exploration,
	Gameplay,
	GameRound,
	Tag
}

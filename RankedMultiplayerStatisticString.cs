using System;
using UnityEngine;

[Serializable]
public class RankedMultiplayerStatisticString : RankedMultiplayerStatistic
{
	private string stringValue;

	public RankedMultiplayerStatisticString(string n, string val, SerializationType s = SerializationType.None)
		: base(n, s)
	{
		stringValue = val;
	}

	public static implicit operator string(RankedMultiplayerStatisticString stat)
	{
		if (stat.IsValid)
		{
			return stat.stringValue;
		}
		Debug.LogError("Attempting to retrieve value for user data that does not yet have a valid key: " + stat.name);
		return string.Empty;
	}

	public void Set(string val)
	{
		stringValue = val;
		Save();
	}

	public string Get()
	{
		return stringValue;
	}

	public override bool TrySetValue(string valAsString)
	{
		stringValue = valAsString;
		return true;
	}

	protected override void Save()
	{
		SerializationType serializationType = base.serializationType;
		if (serializationType != SerializationType.Mothership && serializationType == SerializationType.PlayerPrefs)
		{
			PlayerPrefs.SetString(name, stringValue);
			PlayerPrefs.Save();
		}
	}

	public override void Load()
	{
		switch (serializationType)
		{
		case SerializationType.PlayerPrefs:
			base.IsValid = true;
			stringValue = PlayerPrefs.GetString(name, stringValue);
			break;
		case SerializationType.Mothership:
			base.IsValid = false;
			break;
		}
	}

	public override string ToString()
	{
		return stringValue;
	}
}

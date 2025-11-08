using System;
using UnityEngine;

[Serializable]
public class RankedMultiplayerStatisticInt : RankedMultiplayerStatistic
{
	private int intValue;

	private int minValue;

	private int maxValue;

	public RankedMultiplayerStatisticInt(string n, int val, int min = 0, int max = int.MaxValue, SerializationType s = SerializationType.None)
		: base(n, s)
	{
		intValue = val;
		minValue = min;
		maxValue = max;
	}

	public static implicit operator int(RankedMultiplayerStatisticInt stat)
	{
		if (stat.IsValid)
		{
			return stat.intValue;
		}
		Debug.LogError("Attempting to retrieve value for user data that does not yet have a valid key: " + stat.name);
		return 0;
	}

	public void Set(int val)
	{
		intValue = Mathf.Clamp(val, minValue, maxValue);
		Save();
	}

	public int Get()
	{
		return intValue;
	}

	public override bool TrySetValue(string valAsString)
	{
		int result;
		bool num = int.TryParse(valAsString, out result);
		if (num)
		{
			intValue = Mathf.Clamp(result, minValue, maxValue);
		}
		return num;
	}

	public void Increment()
	{
		AddTo(1);
	}

	public void AddTo(int amount)
	{
		intValue += amount;
		intValue = Mathf.Clamp(intValue, minValue, maxValue);
		Save();
	}

	protected override void Save()
	{
		SerializationType serializationType = base.serializationType;
		if (serializationType != SerializationType.Mothership && serializationType == SerializationType.PlayerPrefs)
		{
			PlayerPrefs.SetInt(name, intValue);
			PlayerPrefs.Save();
		}
	}

	public override void Load()
	{
		switch (serializationType)
		{
		case SerializationType.PlayerPrefs:
			base.IsValid = true;
			intValue = PlayerPrefs.GetInt(name, intValue);
			break;
		case SerializationType.Mothership:
			base.IsValid = false;
			break;
		}
	}

	public override string ToString()
	{
		return intValue.ToString();
	}
}

using System;
using UnityEngine;

[Serializable]
public class RankedMultiplayerStatisticFloat : RankedMultiplayerStatistic
{
	private float floatValue;

	private float minValue;

	private float maxValue;

	public RankedMultiplayerStatisticFloat(string n, float val, float min = 0f, float max = float.MaxValue, SerializationType s = SerializationType.None)
		: base(n, s)
	{
		floatValue = val;
		minValue = min;
		maxValue = max;
	}

	public static implicit operator float(RankedMultiplayerStatisticFloat stat)
	{
		if (stat.IsValid)
		{
			return stat.floatValue;
		}
		Debug.LogError("Attempting to retrieve value for user data that does not yet have a valid key: " + stat.name);
		return 0f;
	}

	public void Set(float val)
	{
		floatValue = Mathf.Clamp(val, minValue, maxValue);
		Save();
	}

	public float Get()
	{
		return floatValue;
	}

	public override bool TrySetValue(string valAsString)
	{
		float result;
		bool num = float.TryParse(valAsString, out result);
		if (num)
		{
			floatValue = Mathf.Clamp(result, minValue, maxValue);
		}
		return num;
	}

	public void Increment()
	{
		AddTo(1f);
	}

	public void AddTo(float amount)
	{
		floatValue += amount;
		floatValue = Mathf.Clamp(floatValue, minValue, maxValue);
		Save();
	}

	protected override void Save()
	{
		SerializationType serializationType = base.serializationType;
		if (serializationType != SerializationType.Mothership && serializationType == SerializationType.PlayerPrefs)
		{
			PlayerPrefs.SetFloat(name, floatValue);
			PlayerPrefs.Save();
		}
	}

	public override void Load()
	{
		switch (serializationType)
		{
		case SerializationType.PlayerPrefs:
			base.IsValid = true;
			floatValue = PlayerPrefs.GetFloat(name, floatValue);
			break;
		case SerializationType.Mothership:
			base.IsValid = false;
			break;
		}
	}

	public override string ToString()
	{
		return floatValue.ToString();
	}
}

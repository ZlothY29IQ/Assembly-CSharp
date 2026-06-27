using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ServerTimeSyncRule", menuName = "Scriptable Objects/ServerTimeSyncRule")]
public class ServerTimeSyncRule : ScriptableObject
{
	private enum Unit
	{
		Hours,
		Minutes,
		Seconds
	}

	[SerializeField]
	private Unit unit;

	[SerializeField]
	private int value;

	public DateTime GetPrevious(DateTime dt)
	{
		DateTime result = DateTime.MinValue;
		switch (unit)
		{
		case Unit.Hours:
			result = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0).AddHours(-(dt.Hour % value));
			break;
		case Unit.Minutes:
			result = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0).AddMinutes(-(dt.Minute % value));
			break;
		case Unit.Seconds:
			result = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second).AddSeconds(-(dt.Second % value));
			break;
		}
		return result;
	}

	public DateTime GetNext(DateTime dt)
	{
		DateTime result = DateTime.MaxValue;
		switch (unit)
		{
		case Unit.Hours:
			result = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0).AddHours(value - dt.Hour % value);
			break;
		case Unit.Minutes:
			result = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0).AddMinutes(value - dt.Minute % value);
			break;
		case Unit.Seconds:
			result = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second).AddSeconds(value - dt.Second % value);
			break;
		}
		return result;
	}
}

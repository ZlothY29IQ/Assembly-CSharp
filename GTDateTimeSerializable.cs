using System;
using System.Globalization;
using UnityEngine;

[Serializable]
public struct GTDateTimeSerializable : ISerializationCallbackReceiver
{
	[HideInInspector]
	[SerializeField]
	private string _dateTimeString;

	private DateTime _dateTime;

	public DateTime dateTime
	{
		get
		{
			return _dateTime;
		}
		set
		{
			_dateTime = value;
			_dateTimeString = FormatDateTime(_dateTime);
		}
	}

	void ISerializationCallbackReceiver.OnBeforeSerialize()
	{
		_dateTimeString = FormatDateTime(_dateTime);
	}

	void ISerializationCallbackReceiver.OnAfterDeserialize()
	{
		if (TryParseDateTime(_dateTimeString, out var result))
		{
			_dateTime = result;
		}
	}

	public GTDateTimeSerializable(int dummyValue)
	{
		DateTime now = DateTime.Now;
		_dateTime = new DateTime(now.Year, now.Month, now.Day, 11, 0, 0);
		_dateTimeString = FormatDateTime(_dateTime);
	}

	private static string FormatDateTime(DateTime dateTime)
	{
		return dateTime.ToString("yyyy-MM-dd HH:mm");
	}

	private static bool TryParseDateTime(string value, out DateTime result)
	{
		if (DateTime.TryParseExact(value, new string[3] { "yyyy-MM-dd HH:mm", "yyyy-MM-dd", "yyyy-MM" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
		{
			DateTime dateTime = result;
			if (dateTime.Hour == 0 && dateTime.Minute == 0)
			{
				result = result.AddHours(11.0);
			}
			return true;
		}
		return false;
	}
}

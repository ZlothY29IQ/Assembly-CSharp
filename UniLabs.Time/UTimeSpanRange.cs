using System;
using Newtonsoft.Json;
using UnityEngine;

namespace UniLabs.Time;

[Serializable]
[JsonObject(MemberSerialization.OptIn)]
public class UTimeSpanRange
{
	[JsonProperty("Start")]
	[SerializeField]
	private UTimeSpan _Start;

	[JsonProperty("End")]
	[SerializeField]
	private UTimeSpan _End;

	public TimeSpan Start
	{
		get
		{
			return _Start;
		}
		set
		{
			_Start = value;
		}
	}

	public TimeSpan End
	{
		get
		{
			return _End;
		}
		set
		{
			_End = value;
		}
	}

	public TimeSpan Duration => End - Start;

	public bool IsInRange(TimeSpan time)
	{
		if (time >= Start)
		{
			return time <= End;
		}
		return false;
	}

	[JsonConstructor]
	public UTimeSpanRange()
	{
	}

	public UTimeSpanRange(TimeSpan start)
	{
		_Start = start;
		_End = start;
	}

	public UTimeSpanRange(TimeSpan start, TimeSpan end)
	{
		_Start = start;
		_End = end;
	}

	private void OnStartChanged()
	{
		if (_Start.CompareTo(_End) > 0)
		{
			_End.TimeSpan = _Start.TimeSpan;
		}
	}

	private void OnEndChanged()
	{
		if (_End.CompareTo(_Start) < 0)
		{
			_Start.TimeSpan = _End.TimeSpan;
		}
	}
}

using System;
using System.Globalization;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using UnityEngine;

namespace UniLabs.Time;

[Serializable]
[JsonObject(MemberSerialization.OptIn)]
public class UTimeSpan : ISerializationCallbackReceiver, IComparable<UTimeSpan>, IComparable<TimeSpan>
{
	[HideInInspector]
	[SerializeField]
	private string _TimeSpan;

	[JsonProperty("TimeSpan")]
	public TimeSpan TimeSpan { get; set; }

	[JsonConstructor]
	public UTimeSpan()
	{
		TimeSpan = TimeSpan.Zero;
	}

	public UTimeSpan(TimeSpan timeSpan)
	{
		TimeSpan = timeSpan;
	}

	public UTimeSpan(long ticks)
		: this(new TimeSpan(ticks))
	{
	}

	public UTimeSpan(int hours, int minutes, int seconds)
		: this(new TimeSpan(hours, minutes, seconds))
	{
	}

	public UTimeSpan(int days, int hours, int minutes, int seconds)
		: this(new TimeSpan(days, hours, minutes, seconds))
	{
	}

	public UTimeSpan(int days, int hours, int minutes, int seconds, int milliseconds)
		: this(new TimeSpan(days, hours, minutes, seconds, milliseconds))
	{
	}

	public static implicit operator TimeSpan(UTimeSpan uTimeSpan)
	{
		return uTimeSpan?.TimeSpan ?? TimeSpan.Zero;
	}

	public static implicit operator UTimeSpan(TimeSpan timeSpan)
	{
		return new UTimeSpan(timeSpan);
	}

	public int CompareTo(TimeSpan other)
	{
		return TimeSpan.CompareTo(other);
	}

	public int CompareTo(UTimeSpan other)
	{
		if (this == other)
		{
			return 0;
		}
		if (other == null)
		{
			return 1;
		}
		return TimeSpan.CompareTo(other.TimeSpan);
	}

	protected bool Equals(UTimeSpan other)
	{
		return TimeSpan.Equals(other.TimeSpan);
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (this == obj)
		{
			return true;
		}
		if (obj.GetType() != GetType())
		{
			return false;
		}
		return Equals((UTimeSpan)obj);
	}

	public override int GetHashCode()
	{
		return TimeSpan.GetHashCode();
	}

	public void OnAfterDeserialize()
	{
		TimeSpan = (TimeSpan.TryParse(_TimeSpan, CultureInfo.InvariantCulture, out var result) ? result : TimeSpan.Zero);
	}

	public void OnBeforeSerialize()
	{
		_TimeSpan = TimeSpan.ToString();
	}

	[OnSerializing]
	internal void OnSerializingMethod(StreamingContext context)
	{
		OnBeforeSerialize();
	}

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		OnAfterDeserialize();
	}
}

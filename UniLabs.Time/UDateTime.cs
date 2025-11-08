using System;
using System.Globalization;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using UnityEngine;

namespace UniLabs.Time;

[Serializable]
[JsonObject(MemberSerialization.OptIn)]
public class UDateTime : ISerializationCallbackReceiver, IComparable<UDateTime>, IComparable<DateTime>
{
	[HideInInspector]
	[SerializeField]
	private string _DateTime;

	[JsonProperty("DateTime")]
	public DateTime DateTime { get; set; }

	[JsonConstructor]
	public UDateTime()
	{
		DateTime = DateTime.UnixEpoch;
	}

	public UDateTime(DateTime dateTime)
	{
		DateTime = dateTime;
	}

	public static implicit operator DateTime(UDateTime udt)
	{
		return udt.DateTime;
	}

	public static implicit operator UDateTime(DateTime dt)
	{
		return new UDateTime
		{
			DateTime = dt
		};
	}

	public int CompareTo(DateTime other)
	{
		return DateTime.CompareTo(other);
	}

	public int CompareTo(UDateTime other)
	{
		if (this == other)
		{
			return 0;
		}
		if (other == null)
		{
			return 1;
		}
		return DateTime.CompareTo(other.DateTime);
	}

	protected bool Equals(UDateTime other)
	{
		return DateTime.Equals(other.DateTime);
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
		return Equals((UDateTime)obj);
	}

	public override int GetHashCode()
	{
		return DateTime.GetHashCode();
	}

	public override string ToString()
	{
		return DateTime.ToString(CultureInfo.InvariantCulture);
	}

	public void OnAfterDeserialize()
	{
		DateTime = (DateTime.TryParse(_DateTime, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result) ? result : DateTime.MinValue);
	}

	public void OnBeforeSerialize()
	{
		_DateTime = DateTime.ToString("o", CultureInfo.InvariantCulture);
	}

	[OnSerializing]
	internal void OnSerializing(StreamingContext context)
	{
		OnBeforeSerialize();
	}

	[OnDeserialized]
	internal void OnDeserialized(StreamingContext context)
	{
		OnAfterDeserialize();
	}
}

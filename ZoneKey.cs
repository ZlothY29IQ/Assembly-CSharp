using System;

[Serializable]
public struct ZoneKey : IEquatable<ZoneKey>, IComparable<ZoneKey>, IComparable
{
	public GTZone zoneId;

	public GTSubZone subZoneId;

	public static readonly ZoneKey Null = new ZoneKey(GTZone.none, GTSubZone.none);

	public int intValue => ToIntValue(zoneId, subZoneId);

	public string zoneName => zoneId.GetName();

	public string subZoneName => subZoneId.GetName();

	public ZoneKey(GTZone zone, GTSubZone subZone)
	{
		zoneId = zone;
		subZoneId = subZone;
	}

	public override int GetHashCode()
	{
		return intValue;
	}

	public override string ToString()
	{
		return "ZoneKey { " + zoneName + " : " + subZoneName + " }";
	}

	public static ZoneKey GetKey(GTZone zone, GTSubZone subZone)
	{
		return new ZoneKey(zone, subZone);
	}

	public static int ToIntValue(GTZone zone, GTSubZone subZone)
	{
		if (zone == GTZone.none && subZone == GTSubZone.none)
		{
			return 0;
		}
		return StaticHash.Compute(zone.GetLongValue(), subZone.GetLongValue());
	}

	public bool Equals(ZoneKey other)
	{
		if (intValue == other.intValue && zoneId == other.zoneId)
		{
			return subZoneId == other.subZoneId;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is ZoneKey other)
		{
			return Equals(other);
		}
		return false;
	}

	public static bool operator ==(ZoneKey x, ZoneKey y)
	{
		return x.Equals(y);
	}

	public static bool operator !=(ZoneKey x, ZoneKey y)
	{
		return !x.Equals(y);
	}

	public int CompareTo(ZoneKey other)
	{
		int num = intValue.CompareTo(other.intValue);
		if (num == 0)
		{
			num = string.CompareOrdinal(zoneName, other.zoneName);
		}
		if (num == 0)
		{
			num = string.CompareOrdinal(subZoneName, other.subZoneName);
		}
		return num;
	}

	public int CompareTo(object obj)
	{
		if (obj is ZoneKey other)
		{
			return CompareTo(other);
		}
		return 1;
	}

	public static bool operator <(ZoneKey x, ZoneKey y)
	{
		return x.CompareTo(y) < 0;
	}

	public static bool operator >(ZoneKey x, ZoneKey y)
	{
		return x.CompareTo(y) > 0;
	}

	public static bool operator <=(ZoneKey x, ZoneKey y)
	{
		return x.CompareTo(y) <= 0;
	}

	public static bool operator >=(ZoneKey x, ZoneKey y)
	{
		return x.CompareTo(y) >= 0;
	}

	public static explicit operator int(ZoneKey key)
	{
		return key.intValue;
	}
}

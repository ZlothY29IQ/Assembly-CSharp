using System;
using Cysharp.Text;
using UnityEngine;

[Serializable]
public struct ZoneNode : IEquatable<ZoneNode>
{
	public GTZone zoneId;

	public GTSubZone subZoneId;

	public Vector3 center;

	public Vector3 size;

	public Quaternion orientation;

	public Bounds AABB;

	public bool isValid;

	public static ZoneNode Null { get; } = new ZoneNode
	{
		zoneId = GTZone.none,
		subZoneId = GTSubZone.none,
		isValid = false
	};

	public int zoneKey => StaticHash.Compute((int)zoneId, (int)subZoneId);

	public bool ContainsPoint(Vector3 point)
	{
		return MathUtils.OrientedBoxContains(point, center, size, orientation);
	}

	public int SphereOverlap(Vector3 position, float radius)
	{
		return MathUtils.OrientedBoxSphereOverlap(position, radius, center, size, orientation);
	}

	public override string ToString()
	{
		if (subZoneId != 0)
		{
			return ZString.Concat(zoneId, ".", subZoneId);
		}
		return ZString.Concat(zoneId);
	}

	public override int GetHashCode()
	{
		int i = zoneKey;
		int hashCode = center.QuantizedId128().GetHashCode();
		int hashCode2 = size.QuantizedId128().GetHashCode();
		int hashCode3 = orientation.QuantizedId128().GetHashCode();
		return StaticHash.Compute(i, hashCode, hashCode2, hashCode3);
	}

	public static bool operator ==(ZoneNode x, ZoneNode y)
	{
		return x.Equals(y);
	}

	public static bool operator !=(ZoneNode x, ZoneNode y)
	{
		return !x.Equals(y);
	}

	public bool Equals(ZoneNode other)
	{
		if (zoneId == other.zoneId && subZoneId == other.subZoneId && center.Approx(other.center) && size.Approx(other.size))
		{
			return orientation.Approx(other.orientation);
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is ZoneNode other)
		{
			return Equals(other);
		}
		return false;
	}
}

using System;
using Photon.Pun;

namespace GorillaNetworking;

public readonly struct PhotonTimestamp(double raw) : IEquatable<PhotonTimestamp>, IComparable<PhotonTimestamp>
{
	public const double WrapPeriod = 4294967.296;

	public const double HalfWrapPeriod = 2147483.648;

	public readonly double Value = Normalize(raw);

	public static PhotonTimestamp Now => new PhotonTimestamp(PhotonNetwork.Time);

	private static double Normalize(double v)
	{
		double num = v % 4294967.296;
		if (num < 0.0)
		{
			num += 4294967.296;
		}
		return num;
	}

	public static double Delta(PhotonTimestamp a, PhotonTimestamp b)
	{
		double num = b.Value - a.Value;
		if (num > 2147483.648)
		{
			num -= 4294967.296;
		}
		else if (num <= -2147483.648)
		{
			num += 4294967.296;
		}
		return num;
	}

	public double SecondsUntil(PhotonTimestamp other)
	{
		return Delta(this, other);
	}

	public double SecondsSince(PhotonTimestamp other)
	{
		return Delta(other, this);
	}

	public PhotonTimestamp AddSeconds(double seconds)
	{
		return new PhotonTimestamp(Value + seconds);
	}

	public static PhotonTimestamp operator +(PhotonTimestamp t, double seconds)
	{
		return new PhotonTimestamp(t.Value + seconds);
	}

	public static PhotonTimestamp operator -(PhotonTimestamp t, double seconds)
	{
		return new PhotonTimestamp(t.Value - seconds);
	}

	public static double operator -(PhotonTimestamp a, PhotonTimestamp b)
	{
		return Delta(b, a);
	}

	public static bool operator <(PhotonTimestamp a, PhotonTimestamp b)
	{
		return Delta(a, b) > 0.0;
	}

	public static bool operator >(PhotonTimestamp a, PhotonTimestamp b)
	{
		return Delta(a, b) < 0.0;
	}

	public static bool operator <=(PhotonTimestamp a, PhotonTimestamp b)
	{
		return Delta(a, b) >= 0.0;
	}

	public static bool operator >=(PhotonTimestamp a, PhotonTimestamp b)
	{
		return Delta(a, b) <= 0.0;
	}

	public static bool operator ==(PhotonTimestamp a, PhotonTimestamp b)
	{
		return a.Value == b.Value;
	}

	public static bool operator !=(PhotonTimestamp a, PhotonTimestamp b)
	{
		return a.Value != b.Value;
	}

	public int CompareTo(PhotonTimestamp other)
	{
		double num = Delta(this, other);
		if (num > 0.0)
		{
			return -1;
		}
		if (num < 0.0)
		{
			return 1;
		}
		return 0;
	}

	public bool Equals(PhotonTimestamp other)
	{
		return Value == other.Value;
	}

	public override bool Equals(object obj)
	{
		if (obj is PhotonTimestamp other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Value.GetHashCode();
	}

	public override string ToString()
	{
		return Value.ToString("F3");
	}
}

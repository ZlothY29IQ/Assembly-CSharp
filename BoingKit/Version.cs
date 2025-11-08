using System;

namespace BoingKit;

public struct Version : IEquatable<Version>
{
	public static readonly Version Invalid = new Version(-1, -1, -1);

	public static readonly Version FirstTracked = new Version(1, 2, 33);

	public static readonly Version LastUntracked = new Version(1, 2, 32);

	public int MajorVersion { get; }

	public int MinorVersion { get; }

	public int Revision { get; }

	public override string ToString()
	{
		return MajorVersion + "." + MinorVersion + "." + Revision;
	}

	public bool IsValid()
	{
		if (MajorVersion >= 0 && MinorVersion >= 0)
		{
			return Revision >= 0;
		}
		return false;
	}

	public Version(int majorVersion = -1, int minorVersion = -1, int revision = -1)
	{
		MajorVersion = majorVersion;
		MinorVersion = minorVersion;
		Revision = revision;
	}

	public static bool operator ==(Version lhs, Version rhs)
	{
		if (!lhs.IsValid())
		{
			return false;
		}
		if (!rhs.IsValid())
		{
			return false;
		}
		if (lhs.MajorVersion == rhs.MajorVersion && lhs.MinorVersion == rhs.MinorVersion)
		{
			return lhs.Revision == rhs.Revision;
		}
		return false;
	}

	public static bool operator !=(Version lhs, Version rhs)
	{
		return !(lhs == rhs);
	}

	public override bool Equals(object obj)
	{
		if (obj is Version)
		{
			return Equals((Version)obj);
		}
		return false;
	}

	public bool Equals(Version other)
	{
		if (MajorVersion == other.MajorVersion && MinorVersion == other.MinorVersion)
		{
			return Revision == other.Revision;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ((366299368 * -1521134295 + MajorVersion.GetHashCode()) * -1521134295 + MinorVersion.GetHashCode()) * -1521134295 + Revision.GetHashCode();
	}
}

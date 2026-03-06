using System;

[Serializable]
public struct GroupJoinZoneAB
{
	public GroupJoinZoneA a;

	public GroupJoinZoneB b;

	public static GroupJoinZoneAB operator &(GroupJoinZoneAB one, GroupJoinZoneAB two)
	{
		return new GroupJoinZoneAB
		{
			a = (one.a & two.a),
			b = (one.b & two.b)
		};
	}

	public static GroupJoinZoneAB operator |(GroupJoinZoneAB one, GroupJoinZoneAB two)
	{
		return new GroupJoinZoneAB
		{
			a = (one.a | two.a),
			b = (one.b | two.b)
		};
	}

	public static GroupJoinZoneAB operator ~(GroupJoinZoneAB z)
	{
		return new GroupJoinZoneAB
		{
			a = ~z.a,
			b = ~z.b
		};
	}

	public static bool operator ==(GroupJoinZoneAB one, GroupJoinZoneAB two)
	{
		if (one.a == two.a)
		{
			return one.b == two.b;
		}
		return false;
	}

	public static bool operator !=(GroupJoinZoneAB one, GroupJoinZoneAB two)
	{
		if (one.a == two.a)
		{
			return one.b != two.b;
		}
		return true;
	}

	public bool HasAnyFlag(GroupJoinZoneAB other)
	{
		if ((a & other.a) == 0)
		{
			return (b & other.b) != 0;
		}
		return true;
	}

	public override bool Equals(object other)
	{
		return this == (GroupJoinZoneAB)other;
	}

	public override int GetHashCode()
	{
		return a.GetHashCode() ^ b.GetHashCode();
	}

	public static implicit operator GroupJoinZoneAB(int d)
	{
		return new GroupJoinZoneAB
		{
			a = (GroupJoinZoneA)d
		};
	}

	public override string ToString()
	{
		if (b != 0)
		{
			if (a != 0)
			{
				return a.ToString() + "," + b;
			}
			return b.ToString();
		}
		return a.ToString();
	}
}

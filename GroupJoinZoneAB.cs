using System;

[Serializable]
public struct GroupJoinZoneAB
{
	public GroupJoinZoneA a;

	public GroupJoinZoneB b;

	public static GroupJoinZoneAB operator &(GroupJoinZoneAB one, GroupJoinZoneAB two)
	{
		GroupJoinZoneAB result = default(GroupJoinZoneAB);
		result.a = one.a & two.a;
		result.b = one.b & two.b;
		return result;
	}

	public static GroupJoinZoneAB operator |(GroupJoinZoneAB one, GroupJoinZoneAB two)
	{
		GroupJoinZoneAB result = default(GroupJoinZoneAB);
		result.a = one.a | two.a;
		result.b = one.b | two.b;
		return result;
	}

	public static GroupJoinZoneAB operator ~(GroupJoinZoneAB z)
	{
		GroupJoinZoneAB result = default(GroupJoinZoneAB);
		result.a = ~z.a;
		result.b = ~z.b;
		return result;
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
		GroupJoinZoneAB result = default(GroupJoinZoneAB);
		result.a = (GroupJoinZoneA)d;
		return result;
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

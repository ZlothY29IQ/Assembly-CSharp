public struct HandLinkAuthorityStatus
{
	public HandLinkAuthorityType type;

	public float timestamp;

	public int tiebreak;

	public HandLinkAuthorityStatus(HandLinkAuthorityType authority)
	{
		type = authority;
		timestamp = -1f;
		tiebreak = -1;
	}

	public HandLinkAuthorityStatus(HandLinkAuthorityType authority, float timestamp, int tiebreak)
	{
		type = authority;
		this.timestamp = timestamp;
		this.tiebreak = tiebreak;
	}

	public static bool operator >(HandLinkAuthorityStatus a, HandLinkAuthorityStatus b)
	{
		if (a.type > b.type)
		{
			return true;
		}
		if (b.type > a.type)
		{
			return false;
		}
		if (a.timestamp > b.timestamp)
		{
			return true;
		}
		if (b.timestamp > a.timestamp)
		{
			return false;
		}
		return a.tiebreak > b.tiebreak;
	}

	public static bool operator <(HandLinkAuthorityStatus a, HandLinkAuthorityStatus b)
	{
		if (a.type < b.type)
		{
			return true;
		}
		if (b.type < a.type)
		{
			return false;
		}
		if (a.timestamp < b.timestamp)
		{
			return true;
		}
		if (b.timestamp < a.timestamp)
		{
			return false;
		}
		return a.tiebreak < b.tiebreak;
	}

	public int CompareTo(HandLinkAuthorityStatus b)
	{
		int num = type.CompareTo(b.type);
		if (num != 0)
		{
			return num;
		}
		int num2 = timestamp.CompareTo(b.timestamp);
		if (num2 != 0)
		{
			return num2;
		}
		return tiebreak.CompareTo(b.tiebreak);
	}

	public static bool operator ==(HandLinkAuthorityStatus a, HandLinkAuthorityStatus b)
	{
		if (a.type == b.type && a.timestamp == b.timestamp)
		{
			return a.tiebreak == b.tiebreak;
		}
		return false;
	}

	public static bool operator !=(HandLinkAuthorityStatus a, HandLinkAuthorityStatus b)
	{
		if (a.timestamp == b.timestamp)
		{
			return a.tiebreak != b.tiebreak;
		}
		return true;
	}

	public override string ToString()
	{
		return string.Format("{0}/{1}", timestamp.ToString("0.0000"), tiebreak);
	}
}

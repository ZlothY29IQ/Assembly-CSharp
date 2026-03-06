using System;

[Serializable]
public struct GTSignalID : IEquatable<GTSignalID>, IEquatable<int>
{
	private int _id;

	public override bool Equals(object obj)
	{
		if (obj is GTSignalID other)
		{
			return Equals(other);
		}
		if (obj is int other2)
		{
			return Equals(other2);
		}
		return false;
	}

	public bool Equals(GTSignalID other)
	{
		return _id == other._id;
	}

	public bool Equals(int other)
	{
		return _id == other;
	}

	public override int GetHashCode()
	{
		return _id;
	}

	public static bool operator ==(GTSignalID x, GTSignalID y)
	{
		return x.Equals(y);
	}

	public static bool operator !=(GTSignalID x, GTSignalID y)
	{
		return !x.Equals(y);
	}

	public static implicit operator int(GTSignalID sid)
	{
		return sid._id;
	}

	public static implicit operator GTSignalID(string s)
	{
		return new GTSignalID
		{
			_id = GTSignal.ComputeID(s)
		};
	}
}

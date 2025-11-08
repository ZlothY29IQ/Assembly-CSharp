using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

[Serializable]
[StructLayout(LayoutKind.Explicit)]
public struct Id128 : IEquatable<Id128>, IComparable<Id128>, IEquatable<Guid>, IEquatable<Hash128>
{
	[FieldOffset(0)]
	[SerializeField]
	public long x;

	[FieldOffset(8)]
	[SerializeField]
	public long y;

	[FieldOffset(0)]
	[NonSerialized]
	public int a;

	[FieldOffset(4)]
	[NonSerialized]
	public int b;

	[FieldOffset(8)]
	[NonSerialized]
	public int c;

	[FieldOffset(12)]
	[NonSerialized]
	public int d;

	[FieldOffset(0)]
	[NonSerialized]
	public Guid guid;

	[FieldOffset(0)]
	[NonSerialized]
	public Hash128 h128;

	public static readonly Id128 Empty;

	public Id128(int a, int b, int c, int d)
	{
		guid = Guid.Empty;
		h128 = default(Hash128);
		x = (y = 0L);
		this.a = a;
		this.b = b;
		this.c = c;
		this.d = d;
	}

	public Id128(long x, long y)
	{
		a = (b = (c = (d = 0)));
		guid = Guid.Empty;
		h128 = default(Hash128);
		this.x = x;
		this.y = y;
	}

	public Id128(Hash128 hash)
	{
		x = (y = 0L);
		a = (b = (c = (d = 0)));
		guid = Guid.Empty;
		h128 = hash;
	}

	public Id128(Guid guid)
	{
		a = (b = (c = (d = 0)));
		x = (y = 0L);
		h128 = default(Hash128);
		this.guid = guid;
	}

	public Id128(string guid)
	{
		if (string.IsNullOrWhiteSpace(guid))
		{
			throw new ArgumentNullException("guid");
		}
		a = (b = (c = (d = 0)));
		x = (y = 0L);
		h128 = default(Hash128);
		this.guid = Guid.Parse(guid);
	}

	public Id128(byte[] bytes)
	{
		if (bytes == null)
		{
			throw new ArgumentNullException("bytes");
		}
		if (bytes.Length != 16)
		{
			throw new ArgumentException("Input buffer must be exactly 16 bytes", "bytes");
		}
		a = (b = (c = (d = 0)));
		x = (y = 0L);
		h128 = default(Hash128);
		guid = new Guid(bytes);
	}

	public (long l1, long l2) ToLongs()
	{
		return (l1: x, l2: y);
	}

	public (int i1, int i2, int i3, int i4) ToInts()
	{
		return (i1: a, i2: b, i3: c, i4: d);
	}

	public byte[] ToByteArray()
	{
		return guid.ToByteArray();
	}

	public bool Equals(Id128 id)
	{
		if (x == id.x)
		{
			return y == id.y;
		}
		return false;
	}

	public bool Equals(Guid g)
	{
		return guid == g;
	}

	public bool Equals(Hash128 h)
	{
		return h128 == h;
	}

	public override bool Equals(object obj)
	{
		if (obj is Id128 id)
		{
			return Equals(id);
		}
		if (obj is Guid g)
		{
			return Equals(g);
		}
		if (obj is Hash128 h)
		{
			return Equals(h);
		}
		return false;
	}

	public override string ToString()
	{
		return guid.ToString();
	}

	public override int GetHashCode()
	{
		return StaticHash.Compute(a, b, c, d);
	}

	public int CompareTo(Id128 id)
	{
		int num = x.CompareTo(id.x);
		if (num == 0)
		{
			num = y.CompareTo(id.y);
		}
		return num;
	}

	public int CompareTo(object obj)
	{
		if (obj is Id128 id)
		{
			return CompareTo(id);
		}
		if (obj is Guid value)
		{
			return guid.CompareTo(value);
		}
		if (obj is Hash128 rhs)
		{
			return h128.CompareTo(rhs);
		}
		throw new ArgumentException("Object must be of type Id128 or Guid");
	}

	public static Id128 NewId()
	{
		return new Id128(Guid.NewGuid());
	}

	public static Id128 ComputeMD5(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return Empty;
		}
		using MD5 mD = MD5.Create();
		return new Guid(mD.ComputeHash(Encoding.UTF8.GetBytes(s)));
	}

	public static Id128 ComputeSHV2(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return Empty;
		}
		return Hash128.Compute(s);
	}

	public static bool operator ==(Id128 j, Id128 k)
	{
		return j.Equals(k);
	}

	public static bool operator !=(Id128 j, Id128 k)
	{
		return !j.Equals(k);
	}

	public static bool operator ==(Id128 j, Guid k)
	{
		return j.Equals(k);
	}

	public static bool operator !=(Id128 j, Guid k)
	{
		return !j.Equals(k);
	}

	public static bool operator ==(Guid j, Id128 k)
	{
		return j.Equals(k.guid);
	}

	public static bool operator !=(Guid j, Id128 k)
	{
		return !j.Equals(k.guid);
	}

	public static bool operator ==(Id128 j, Hash128 k)
	{
		return j.Equals(k);
	}

	public static bool operator !=(Id128 j, Hash128 k)
	{
		return !j.Equals(k);
	}

	public static bool operator ==(Hash128 j, Id128 k)
	{
		return j.Equals(k.h128);
	}

	public static bool operator !=(Hash128 j, Id128 k)
	{
		return !j.Equals(k.h128);
	}

	public static bool operator <(Id128 j, Id128 k)
	{
		return j.CompareTo(k) < 0;
	}

	public static bool operator >(Id128 j, Id128 k)
	{
		return j.CompareTo(k) > 0;
	}

	public static bool operator <=(Id128 j, Id128 k)
	{
		return j.CompareTo(k) <= 0;
	}

	public static bool operator >=(Id128 j, Id128 k)
	{
		return j.CompareTo(k) >= 0;
	}

	public static implicit operator Guid(Id128 id)
	{
		return id.guid;
	}

	public static implicit operator Id128(Guid guid)
	{
		return new Id128(guid);
	}

	public static implicit operator Id128(Hash128 h)
	{
		return new Id128(h);
	}

	public static implicit operator Hash128(Id128 id)
	{
		return id.h128;
	}

	public static explicit operator Id128(string s)
	{
		return ComputeMD5(s);
	}
}

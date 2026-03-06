using System;
using UnityEngine;

[Serializable]
public struct StringEnum<TEnum> where TEnum : struct, Enum
{
	[SerializeField]
	private TEnum m_EnumValue;

	public TEnum Value => m_EnumValue;

	public static implicit operator StringEnum<TEnum>(TEnum e)
	{
		return new StringEnum<TEnum>
		{
			m_EnumValue = e
		};
	}

	public static implicit operator TEnum(StringEnum<TEnum> se)
	{
		return se.m_EnumValue;
	}

	public static bool operator ==(StringEnum<TEnum> left, StringEnum<TEnum> right)
	{
		return left.m_EnumValue.Equals(right.m_EnumValue);
	}

	public static bool operator !=(StringEnum<TEnum> left, StringEnum<TEnum> right)
	{
		return !(left == right);
	}

	public override bool Equals(object obj)
	{
		if (obj is StringEnum<TEnum> stringEnum)
		{
			return m_EnumValue.Equals(stringEnum.m_EnumValue);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return m_EnumValue.GetHashCode();
	}

	public override string ToString()
	{
		return Value.ToString();
	}
}

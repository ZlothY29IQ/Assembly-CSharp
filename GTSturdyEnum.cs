using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct GTSturdyEnum<TEnum> : ISerializationCallbackReceiver where TEnum : struct, Enum
{
	[Serializable]
	private struct EnumPair
	{
		public string Name;

		public TEnum FallbackValue;
	}

	[SerializeField]
	private EnumPair[] m_stringValuePairs;

	public TEnum Value { get; private set; }

	public static implicit operator GTSturdyEnum<TEnum>(TEnum value)
	{
		return new GTSturdyEnum<TEnum>
		{
			Value = value
		};
	}

	public static implicit operator TEnum(GTSturdyEnum<TEnum> sturdyEnum)
	{
		return sturdyEnum.Value;
	}

	public void OnBeforeSerialize()
	{
		EnumData<TEnum> shared = EnumData<TEnum>.Shared;
		if (shared.IsBitMaskCompatible)
		{
			long num = Convert.ToInt64(Value);
			if (num == 0L)
			{
				m_stringValuePairs = new EnumPair[1]
				{
					new EnumPair
					{
						Name = Value.ToString(),
						FallbackValue = Value
					}
				};
				return;
			}
			List<EnumPair> list = new List<EnumPair>(shared.Values.Length);
			for (int i = 0; i < shared.Values.Length; i++)
			{
				long num2 = shared.LongValues[i];
				if (num2 != 0L && (num & num2) == num2)
				{
					TEnum fallbackValue = shared.Values[i];
					list.Add(new EnumPair
					{
						Name = fallbackValue.ToString(),
						FallbackValue = fallbackValue
					});
				}
			}
			m_stringValuePairs = list.ToArray();
		}
		else
		{
			m_stringValuePairs = new EnumPair[1];
			m_stringValuePairs[0] = new EnumPair
			{
				Name = Value.ToString(),
				FallbackValue = Value
			};
		}
	}

	public void OnAfterDeserialize()
	{
		EnumData<TEnum> shared = EnumData<TEnum>.Shared;
		if (m_stringValuePairs == null || m_stringValuePairs.Length == 0)
		{
			if (shared.IsBitMaskCompatible)
			{
				Value = (TEnum)Enum.ToObject(typeof(TEnum), 0L);
			}
			else
			{
				Value = default(TEnum);
			}
		}
		else if (shared.IsBitMaskCompatible)
		{
			long num = 0L;
			EnumPair[] stringValuePairs = m_stringValuePairs;
			for (int i = 0; i < stringValuePairs.Length; i++)
			{
				EnumPair enumPair = stringValuePairs[i];
				long value2;
				if (shared.NameToEnum.TryGetValue(enumPair.Name, out var value))
				{
					num |= shared.EnumToLong[value];
				}
				else if (shared.EnumToLong.TryGetValue(enumPair.FallbackValue, out value2))
				{
					num |= value2;
				}
			}
			Value = (TEnum)Enum.ToObject(typeof(TEnum), num);
		}
		else
		{
			EnumPair enumPair2 = m_stringValuePairs[0];
			if (shared.NameToEnum.TryGetValue(enumPair2.Name, out var value3))
			{
				Value = value3;
			}
			else
			{
				Value = enumPair2.FallbackValue;
			}
		}
	}
}

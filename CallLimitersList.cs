using System;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class CallLimitersList<Titem, Tenum> where Titem : CallLimiter, new() where Tenum : Enum
{
	[RequiredListLength("GetMaxLength")]
	[SerializeField]
	private Titem[] m_callLimiters;

	public CallLimitersList()
	{
	}

	public CallLimitersList(CallLimitersList<Titem, Tenum> source)
	{
		Titem[] callLimiters = source.m_callLimiters;
		m_callLimiters = new Titem[callLimiters.Length];
		for (int i = 0; i < m_callLimiters.Length; i++)
		{
			Titem val = callLimiters[i];
			m_callLimiters[i] = (Titem)val.GetCopy();
		}
	}

	public CallLimitersList<Titem, Tenum> GetCopy()
	{
		return new CallLimitersList<Titem, Tenum>(this);
	}

	public bool IsSpamming(Tenum index)
	{
		return IsSpamming((int)(object)index);
	}

	public bool IsSpamming(int index)
	{
		return !m_callLimiters[index].CheckCallTime(Time.unscaledTime);
	}

	public bool IsSpamming(Tenum index, double serverTime)
	{
		return IsSpamming((int)(object)index, serverTime);
	}

	public bool IsSpamming(int index, double serverTime)
	{
		return !m_callLimiters[index].CheckCallServerTime(serverTime);
	}

	public void Reset()
	{
		Titem[] callLimiters = m_callLimiters;
		for (int i = 0; i < callLimiters.Length; i++)
		{
			callLimiters[i].Reset();
		}
	}
}

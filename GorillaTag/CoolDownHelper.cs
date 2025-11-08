using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GorillaTag;

[Serializable]
public class CoolDownHelper
{
	public float coolDown;

	[NonSerialized]
	public float checkTime;

	public CoolDownHelper()
	{
		coolDown = 1f;
		checkTime = 0f;
	}

	public CoolDownHelper(float cd)
	{
		coolDown = cd;
		checkTime = 0f;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool CheckCooldown()
	{
		float unscaledTime = Time.unscaledTime;
		if (unscaledTime < checkTime)
		{
			return false;
		}
		OnCheckPass();
		checkTime = unscaledTime + coolDown;
		return true;
	}

	public virtual void Start()
	{
		checkTime = Time.unscaledTime + coolDown;
	}

	public virtual void Stop()
	{
		checkTime = float.MaxValue;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public virtual void OnCheckPass()
	{
	}
}

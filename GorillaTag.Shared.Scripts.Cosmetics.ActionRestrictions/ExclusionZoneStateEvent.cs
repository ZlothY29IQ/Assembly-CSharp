using System;
using UnityEngine.Events;

namespace GorillaTag.Shared.Scripts.Cosmetics.ActionRestrictions;

[Serializable]
public class ExclusionZoneStateEvent
{
	public UnityEvent OnNormal;

	public UnityEvent OnRestricted;

	public void Invoke(VRRig vrRig)
	{
		if (CosmeticExclusionZoneRegistry.IsRestricted(vrRig))
		{
			OnRestricted?.Invoke();
		}
		else
		{
			OnNormal?.Invoke();
		}
	}
}
[Serializable]
public class ExclusionZoneStateEvent<T> : ZoneStateEventBase
{
	[Serializable]
	public class TypedEvent : UnityEvent<T>
	{
	}

	public TypedEvent onNormal;

	public TypedEvent onRestricted;

	public void Invoke(VRRig vrRig, T arg)
	{
		if (IsRestricted(vrRig))
		{
			onRestricted?.Invoke(arg);
		}
		else
		{
			onNormal?.Invoke(arg);
		}
	}
}
[Serializable]
public class ExclusionZoneStateEvent<T0, T1> : ZoneStateEventBase
{
	[Serializable]
	public class TypedEvent : UnityEvent<T0, T1>
	{
	}

	public TypedEvent onNormal;

	public TypedEvent onRestricted;

	public void Invoke(VRRig vrRig, T0 arg0, T1 arg1)
	{
		if (IsRestricted(vrRig))
		{
			onRestricted?.Invoke(arg0, arg1);
		}
		else
		{
			onNormal?.Invoke(arg0, arg1);
		}
	}
}

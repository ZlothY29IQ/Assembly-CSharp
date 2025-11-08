using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GorillaTag.GuidedRefs;

public static class GRef
{
	[Flags]
	public enum EResolveModes
	{
		None = 0,
		Runtime = 1,
		SceneProcessing = 2
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool ShouldResolveNow(EResolveModes mode)
	{
		if (Application.isPlaying)
		{
			return (mode & EResolveModes.Runtime) == EResolveModes.Runtime;
		}
		return false;
	}

	public static bool IsAnyResolveModeOn(EResolveModes mode)
	{
		return mode != EResolveModes.None;
	}
}

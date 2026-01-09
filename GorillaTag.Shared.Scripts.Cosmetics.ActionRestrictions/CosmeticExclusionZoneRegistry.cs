using System.Collections.Generic;
using UnityEngine;

namespace GorillaTag.Shared.Scripts.Cosmetics.ActionRestrictions;

public static class CosmeticExclusionZoneRegistry
{
	private static readonly HashSet<VRRig> restrictedRigs = new HashSet<VRRig>();

	public static void Enter(VRRig rig)
	{
		if (rig != null)
		{
			restrictedRigs.Add(rig);
		}
	}

	public static void Exit(VRRig rig)
	{
		if (rig != null)
		{
			restrictedRigs.Remove(rig);
		}
	}

	public static bool IsRestricted(VRRig rig)
	{
		if (rig != null)
		{
			return restrictedRigs.Contains(rig);
		}
		return false;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void Reset()
	{
		restrictedRigs.Clear();
	}
}

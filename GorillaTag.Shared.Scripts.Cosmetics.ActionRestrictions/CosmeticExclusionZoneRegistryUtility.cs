using System.Collections.Generic;
using UnityEngine;

namespace GorillaTag.Shared.Scripts.Cosmetics.ActionRestrictions;

public static class CosmeticExclusionZoneRegistryUtility
{
	private static readonly List<Collider> exclusionZones = new List<Collider>();

	public static void RegisterZone(Collider zone)
	{
		if (zone != null && !exclusionZones.Contains(zone))
		{
			exclusionZones.Add(zone);
		}
	}

	public static void UnregisterZone(Collider zone)
	{
		exclusionZones.Remove(zone);
	}

	public static bool IsPositionRestricted(Vector3 worldPos)
	{
		for (int i = 0; i < exclusionZones.Count; i++)
		{
			Collider collider = exclusionZones[i];
			if (collider != null && collider.bounds.Contains(worldPos))
			{
				return true;
			}
		}
		return false;
	}
}

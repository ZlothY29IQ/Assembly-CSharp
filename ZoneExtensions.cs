using System.Collections.Generic;

public static class ZoneExtensions
{
	public static bool IsAnyPlayerInZone(this GTZone zone)
	{
		foreach (VRRig activeRig in VRRigCache.ActiveRigs)
		{
			if (activeRig.zoneEntity.currentZone == zone)
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsAnyPlayerInZones(this IList<GTZone> zones)
	{
		if (zones == null)
		{
			return false;
		}
		foreach (VRRig activeRig in VRRigCache.ActiveRigs)
		{
			if (zones.Contains(activeRig.zoneEntity.currentZone))
			{
				return true;
			}
		}
		return false;
	}
}

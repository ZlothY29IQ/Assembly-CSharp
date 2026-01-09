namespace GorillaTag.Shared.Scripts.Cosmetics.ActionRestrictions;

public class ZoneStateEventBase
{
	protected bool IsRestricted(VRRig vrRig)
	{
		return CosmeticExclusionZoneRegistry.IsRestricted(vrRig);
	}
}

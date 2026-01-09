using UnityEngine;

namespace GorillaTag.Shared.Scripts.Cosmetics.ActionRestrictions;

public static class CosmeticExclusionQuery
{
	public static bool IsRestricted(VRRig ownerRig = null, GameObject effectSource = null)
	{
		if (ownerRig != null && CosmeticExclusionZoneRegistry.IsRestricted(ownerRig))
		{
			return true;
		}
		if (effectSource != null && effectSource.TryGetComponent<CosmeticExclusionSource>(out var component) && component.IsRestricted())
		{
			return true;
		}
		return false;
	}
}

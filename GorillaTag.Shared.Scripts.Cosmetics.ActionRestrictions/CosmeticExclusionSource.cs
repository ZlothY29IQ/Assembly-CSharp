using UnityEngine;

namespace GorillaTag.Shared.Scripts.Cosmetics.ActionRestrictions;

public class CosmeticExclusionSource : MonoBehaviour
{
	public bool IsRestricted()
	{
		return CosmeticExclusionZoneRegistryUtility.IsPositionRestricted(base.transform.position);
	}
}

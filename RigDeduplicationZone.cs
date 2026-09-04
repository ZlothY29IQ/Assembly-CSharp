using UnityEngine;

public class RigDeduplicationZone : RigDisplacementZone
{
	public override bool IsDisplacingRig(VRRig rig)
	{
		return rig.portalShenanigansBit != VRRig.LocalRig.portalShenanigansBit;
	}

	public override Vector3 GetDisplacementForRig(VRRig rig, Vector3 undisplacedPosition)
	{
		if (!IsDisplacingRig(rig))
		{
			return Vector3.zero;
		}
		return -undisplacedPosition;
	}
}

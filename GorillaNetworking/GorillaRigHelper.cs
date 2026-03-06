using System;

namespace GorillaNetworking;

[Serializable]
internal struct GorillaRigHelper : IComparable
{
	public VRRig rig;

	public CosmeticsThrottler.RigDrawState state;

	public float sqrDistance;

	public float prevSqrDistance;

	public int CompareTo(object obj)
	{
		return sqrDistance.CompareTo(((GorillaRigHelper)obj).sqrDistance);
	}
}

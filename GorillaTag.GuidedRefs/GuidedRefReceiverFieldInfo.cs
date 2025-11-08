using System;
using UnityEngine;

namespace GorillaTag.GuidedRefs;

[Serializable]
public struct GuidedRefReceiverFieldInfo
{
	[SerializeField]
	public GRef.EResolveModes resolveModes;

	[SerializeField]
	public GuidedRefTargetIdSO targetId;

	[Tooltip("(Required) Used to filter down which relay the target can belong to. Only one GuidedRefRelayHub will be used.")]
	[SerializeField]
	public GuidedRefHubIdSO hubId;

	[NonSerialized]
	public int fieldId;

	public GuidedRefReceiverFieldInfo(bool useRecommendedDefaults)
	{
		resolveModes = (useRecommendedDefaults ? (GRef.EResolveModes.Runtime | GRef.EResolveModes.SceneProcessing) : GRef.EResolveModes.None);
		targetId = null;
		hubId = null;
		fieldId = 0;
	}
}

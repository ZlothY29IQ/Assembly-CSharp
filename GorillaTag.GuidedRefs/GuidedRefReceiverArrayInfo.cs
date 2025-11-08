using System;
using UnityEngine;

namespace GorillaTag.GuidedRefs;

[Serializable]
public struct GuidedRefReceiverArrayInfo
{
	[Tooltip("Controls whether the array should be overridden by the guided refs.")]
	[SerializeField]
	public GRef.EResolveModes resolveModes;

	[Tooltip("(Required) Used to filter down which relay the target can belong to. Only one GuidedRefRelayHub will be used.")]
	[SerializeField]
	public GuidedRefHubIdSO hubId;

	[SerializeField]
	public GuidedRefTargetIdSO[] targets;

	[NonSerialized]
	public int fieldId;

	[NonSerialized]
	public int resolveCount;

	public GuidedRefReceiverArrayInfo(bool useRecommendedDefaults)
	{
		resolveModes = (useRecommendedDefaults ? (GRef.EResolveModes.Runtime | GRef.EResolveModes.SceneProcessing) : GRef.EResolveModes.None);
		targets = Array.Empty<GuidedRefTargetIdSO>();
		hubId = null;
		fieldId = 0;
		resolveCount = 0;
	}
}

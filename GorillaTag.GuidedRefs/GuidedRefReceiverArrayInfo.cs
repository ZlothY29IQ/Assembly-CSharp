using System;
using UnityEngine;

namespace GorillaTag.GuidedRefs;

[Serializable]
public struct GuidedRefReceiverArrayInfo(bool useRecommendedDefaults)
{
	[Tooltip("Controls whether the array should be overridden by the guided refs.")]
	[SerializeField]
	public GRef.EResolveModes resolveModes = (useRecommendedDefaults ? (GRef.EResolveModes.Runtime | GRef.EResolveModes.SceneProcessing) : GRef.EResolveModes.None);

	[Tooltip("(Required) Used to filter down which relay the target can belong to. Only one GuidedRefRelayHub will be used.")]
	[SerializeField]
	public GuidedRefHubIdSO hubId = null;

	[SerializeField]
	public GuidedRefTargetIdSO[] targets = Array.Empty<GuidedRefTargetIdSO>();

	[NonSerialized]
	public int fieldId = 0;

	[NonSerialized]
	public int resolveCount = 0;
}

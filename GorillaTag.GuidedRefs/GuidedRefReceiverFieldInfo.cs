using System;
using UnityEngine;

namespace GorillaTag.GuidedRefs;

[Serializable]
public struct GuidedRefReceiverFieldInfo(bool useRecommendedDefaults)
{
	[SerializeField]
	public GRef.EResolveModes resolveModes = (useRecommendedDefaults ? (GRef.EResolveModes.Runtime | GRef.EResolveModes.SceneProcessing) : GRef.EResolveModes.None);

	[SerializeField]
	public GuidedRefTargetIdSO targetId = null;

	[Tooltip("(Required) Used to filter down which relay the target can belong to. Only one GuidedRefRelayHub will be used.")]
	[SerializeField]
	public GuidedRefHubIdSO hubId = null;

	[NonSerialized]
	public int fieldId = 0;
}

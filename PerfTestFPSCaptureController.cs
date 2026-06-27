using GorillaTag;
using UnityEngine;

[GTStripGameObjectFromBuild("!GT_AUTOMATED_PERF_TEST && !BETA")]
public class PerfTestFPSCaptureController : MonoBehaviour
{
	[SerializeField]
	private SerializablePerformanceReport<ScenePerformanceData> performanceSummary;
}

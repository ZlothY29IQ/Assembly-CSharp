using GorillaTag;
using UnityEngine;

[GTStripGameObjectFromBuild("!GT_AUTOMATED_PERF_TEST && !BETA")]
public class PerfTestObjectDestroyer : MonoBehaviour
{
	private void Start()
	{
		Object.DestroyImmediate(base.gameObject, allowDestroyingAssets: true);
	}
}

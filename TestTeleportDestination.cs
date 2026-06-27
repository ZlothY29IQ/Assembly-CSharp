using GorillaTag;
using UnityEngine;

[GTStripGameObjectFromBuild("!GT_AUTOMATED_PERF_TEST && !BETA")]
public class TestTeleportDestination : MonoBehaviour
{
	public GTZone[] zones;

	public GameObject teleportTransform;

	private void OnDrawGizmosSelected()
	{
		Debug.DrawRay(base.transform.position, base.transform.forward * 2f, Color.magenta);
	}
}

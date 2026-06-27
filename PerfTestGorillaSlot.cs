using GorillaTag;
using UnityEngine;

[GTStripGameObjectFromBuild("!GT_AUTOMATED_PERF_TEST && !BETA")]
public class PerfTestGorillaSlot : MonoBehaviour
{
	public enum SlotType
	{
		VR_PLAYER,
		DUMMY
	}

	public SlotType slotType;

	public Vector3 localStartPosition;

	private void Start()
	{
		localStartPosition = base.transform.localPosition;
	}
}

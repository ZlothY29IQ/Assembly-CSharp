using System.Collections.Generic;
using GorillaTag;
using UnityEngine;

[GTStripGameObjectFromBuild("!GT_AUTOMATED_PERF_TEST && !BETA")]
public class PerfTestGorillaHarness : MonoBehaviour
{
	public PerfTestGorillaSlot _vrSlot;

	public List<PerfTestGorillaSlot> dummySlots = new List<PerfTestGorillaSlot>(19);

	private bool _isRecording;

	private float _nextRandomMoveTime;

	private float bounceSpeed = 5f;

	private float bounceAmplitude = 0.5f;

	private void Awake()
	{
		PerfTestGorillaSlot[] componentsInChildren = GetComponentsInChildren<PerfTestGorillaSlot>();
		foreach (PerfTestGorillaSlot perfTestGorillaSlot in componentsInChildren)
		{
			if (perfTestGorillaSlot.slotType == PerfTestGorillaSlot.SlotType.VR_PLAYER)
			{
				_vrSlot = perfTestGorillaSlot;
			}
			else
			{
				dummySlots.Add(perfTestGorillaSlot);
			}
		}
	}

	private void Update()
	{
		if (!_isRecording)
		{
			return;
		}
		foreach (PerfTestGorillaSlot dummySlot in dummySlots)
		{
			float y = dummySlot.localStartPosition.y + Mathf.Sin(Time.time * bounceSpeed) * bounceAmplitude;
			dummySlot.transform.localPosition = new Vector3(dummySlot.localStartPosition.x, y, dummySlot.localStartPosition.z);
		}
	}

	public void StartRecording()
	{
		_isRecording = true;
	}

	public void StopRecording()
	{
		foreach (PerfTestGorillaSlot dummySlot in dummySlots)
		{
			dummySlot.transform.localPosition = dummySlot.localStartPosition;
		}
		_isRecording = false;
	}
}

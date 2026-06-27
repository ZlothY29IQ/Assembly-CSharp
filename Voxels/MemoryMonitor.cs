using System;
using UnityEngine;
using UnityEngine.Profiling;

namespace Voxels;

public class MemoryMonitor : MonoBehaviour
{
	public float logInterval = 1f;

	private float nextLog;

	private void Update()
	{
		if (!(Time.time < nextLog))
		{
			nextLog = Time.time + logInterval;
		}
	}

	private void Collect()
	{
		Profiler.GetMonoUsedSizeLong();
		GC.GetTotalMemory(forceFullCollection: false);
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.GetTotalMemory(forceFullCollection: false);
		Profiler.GetMonoUsedSizeLong();
		GC.GetTotalMemory(forceFullCollection: false);
	}
}

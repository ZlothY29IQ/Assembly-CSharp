using System;
using System.Collections.Generic;
using System.Text;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;

namespace GorillaTag.Dev.Benchmarks;

public class VisualBenchmark : MonoBehaviour
{
	private struct StatInfo
	{
		public string name;

		public ProfilerMarkerDataUnit unit;
	}

	private enum EState
	{
		Setup,
		WaitingBeforeCollectingGarbage,
		WaitingBeforeRecordingStats,
		TearDown
	}

	[Tooltip("the camera will be moved and rotated to these spots and record stats.")]
	public Transform[] benchmarkLocations;

	[Tooltip("How long to wait before calling GC.Collect() to clean up memory.")]
	public float collectGarbageDelay = 2f;

	[Tooltip("How long to wait before recording stats after the camera was moved to a new location.\nThis + collectGarbageDelay is the total time spent at each location.")]
	private float recordStatsDelay = 2f;

	[Tooltip("The camera to use for profiling. If null, a new camera will be created.")]
	private Camera cam;

	private StatInfo[] availableRenderStats;

	private ProfilerRecorder[] renderStatsRecorders;

	private static bool isQuitting = true;

	private int currentLocationIndex;

	private EState state = EState.WaitingBeforeCollectingGarbage;

	private float lastTime;

	private readonly StringBuilder sb = new StringBuilder(1024);

	protected void Awake()
	{
		Application.quitting += delegate
		{
			isQuitting = true;
		};
		List<ProfilerRecorderHandle> list = new List<ProfilerRecorderHandle>(5500);
		ProfilerRecorderHandle.GetAvailable(list);
		List<StatInfo> list2 = new List<StatInfo>(600);
		foreach (ProfilerRecorderHandle item in list)
		{
			ProfilerRecorderDescription description = ProfilerRecorderHandle.GetDescription(item);
			if ((ushort)description.Category == (ushort)ProfilerCategory.Render)
			{
				list2.Add(new StatInfo
				{
					name = description.Name,
					unit = description.UnitType
				});
			}
		}
		availableRenderStats = list2.ToArray();
		List<Transform> list3 = new List<Transform>(benchmarkLocations.Length);
		Transform[] array = benchmarkLocations;
		foreach (Transform transform in array)
		{
			if (transform != null)
			{
				list3.Add(transform);
			}
		}
		benchmarkLocations = list3.ToArray();
	}

	protected void OnEnable()
	{
		renderStatsRecorders = new ProfilerRecorder[availableRenderStats.Length];
		for (int i = 0; i < availableRenderStats.Length; i++)
		{
			renderStatsRecorders[i] = ProfilerRecorder.StartNew(ProfilerCategory.Render, availableRenderStats[i].name);
		}
		state = EState.Setup;
	}

	protected void OnDisable()
	{
		ProfilerRecorder[] array = renderStatsRecorders;
		foreach (ProfilerRecorder profilerRecorder in array)
		{
			profilerRecorder.Dispose();
		}
	}

	protected void LateUpdate()
	{
		if (isQuitting)
		{
			return;
		}
		switch (state)
		{
		case EState.Setup:
			sb.Clear();
			currentLocationIndex = 0;
			lastTime = Time.realtimeSinceStartup;
			state = EState.WaitingBeforeCollectingGarbage;
			break;
		case EState.WaitingBeforeCollectingGarbage:
			if (!(Time.realtimeSinceStartup - lastTime < collectGarbageDelay))
			{
				lastTime = Time.time;
				GC.Collect();
				state = EState.WaitingBeforeRecordingStats;
			}
			break;
		case EState.WaitingBeforeRecordingStats:
			if (!(Time.time - lastTime < recordStatsDelay))
			{
				lastTime = Time.time;
				RecordLocationStats(benchmarkLocations[currentLocationIndex]);
				if (currentLocationIndex < benchmarkLocations.Length - 1)
				{
					currentLocationIndex++;
					state = EState.WaitingBeforeCollectingGarbage;
				}
				else
				{
					state = EState.TearDown;
				}
			}
			break;
		case EState.TearDown:
			Debug.Log(sb.ToString());
			state = EState.Setup;
			if (sb.Length > sb.Capacity)
			{
				Debug.Log("Capacity exceeded on string builder, increase string builder's capacity. " + $"capacity={sb.Capacity}, length={sb.Length}", this);
			}
			break;
		}
	}

	private void RecordLocationStats(Transform xform)
	{
		sb.Append("Location: ");
		sb.Append(xform.name);
		sb.Append("\n");
		sb.Append("pos=");
		sb.Append(xform.position.ToString("F3"));
		sb.Append(" rot=");
		sb.Append(xform.rotation.ToString("F3"));
		sb.Append(" scale=");
		sb.Append(xform.lossyScale.ToString("F3"));
		sb.Append("\n");
		for (int i = 0; i < renderStatsRecorders.Length; i++)
		{
			sb.Append(availableRenderStats[i].name);
			sb.Append(": ");
			switch (availableRenderStats[i].unit)
			{
			case ProfilerMarkerDataUnit.Bytes:
				sb.Append((double)renderStatsRecorders[i].LastValue / 1024.0);
				sb.Append("kb");
				break;
			case ProfilerMarkerDataUnit.TimeNanoseconds:
				sb.Append((double)renderStatsRecorders[i].LastValue / 1000000.0);
				sb.Append("ms");
				break;
			default:
				sb.Append(renderStatsRecorders[i].LastValue);
				sb.Append(' ');
				sb.Append(availableRenderStats[i].unit.ToString());
				break;
			}
			sb.Append('\n');
		}
	}
}

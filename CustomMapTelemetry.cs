using System;
using System.Collections;
using Unity.Profiling;
using UnityEngine;

public class CustomMapTelemetry : MonoBehaviour
{
	[OnEnterPlay_SetNull]
	private static volatile CustomMapTelemetry instance;

	private static string mapName;

	private static long mapModId;

	private static string mapCreatorUsername;

	private static bool metricsCaptureStarted;

	private static float mapEnterTime;

	private static int runningPlayerCount;

	private static int minPlayersInMap;

	private static int maxPlayersInMap;

	private static bool inPrivateRoom;

	private const int minimumPlaytimeForTracking = 30;

	private static int LowestFPS = int.MaxValue;

	private static int LowestFPSDrawCalls;

	private static int LowestFPSPlayerCount;

	private static int AverageFPS;

	private static int AverageDrawCalls;

	private static int AveragePlayerCount;

	private static int HighestFPS = int.MinValue;

	private static int HighestFPSDrawCalls;

	private static int HighestFPSPlayerCount;

	private static int totalFPS;

	private static int totalDrawCalls;

	private static int totalPlayerCount;

	private static int frameCounter;

	private Coroutine perfCaptureCoroutine;

	private static ProfilerRecorder drawCallsRecorder;

	private static bool perfCaptureStarted;

	public static bool IsActive
	{
		get
		{
			if (!metricsCaptureStarted)
			{
				return perfCaptureStarted;
			}
			return true;
		}
	}

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
		else if (instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private static void OnPlayerJoinedRoom(NetPlayer obj)
	{
		runningPlayerCount++;
		maxPlayersInMap = Math.Max(runningPlayerCount, maxPlayersInMap);
	}

	private static void OnPlayerLeftRoom(NetPlayer obj)
	{
		runningPlayerCount--;
		minPlayersInMap = Math.Min(runningPlayerCount, minPlayersInMap);
	}

	public static void StartMapTracking()
	{
		if (!metricsCaptureStarted && !perfCaptureStarted)
		{
			mapEnterTime = Time.realtimeSinceStartup;
			float value = UnityEngine.Random.value;
			if (value <= 0.01f)
			{
				StartMetricsCapture();
			}
			else if (value >= 0.99f)
			{
				StartPerfCapture();
			}
			if (!metricsCaptureStarted)
			{
				_ = perfCaptureStarted;
			}
		}
	}

	public static void EndMapTracking()
	{
		EndMetricsCapture();
		EndPerfCapture();
		mapName = "NULL";
		mapCreatorUsername = "NULL";
		mapEnterTime = -1f;
		mapModId = 0L;
	}

	private static void StartMetricsCapture()
	{
		if (!metricsCaptureStarted)
		{
			metricsCaptureStarted = true;
			NetworkSystem.Instance.OnPlayerJoined -= new Action<NetPlayer>(OnPlayerJoinedRoom);
			NetworkSystem.Instance.OnPlayerJoined += new Action<NetPlayer>(OnPlayerJoinedRoom);
			NetworkSystem.Instance.OnPlayerLeft -= new Action<NetPlayer>(OnPlayerLeftRoom);
			NetworkSystem.Instance.OnPlayerLeft += new Action<NetPlayer>(OnPlayerLeftRoom);
			runningPlayerCount = NetworkSystem.Instance.RoomPlayerCount;
			minPlayersInMap = runningPlayerCount;
			maxPlayersInMap = runningPlayerCount;
		}
	}

	private static void EndMetricsCapture()
	{
		if (!metricsCaptureStarted)
		{
			return;
		}
		metricsCaptureStarted = false;
		NetworkSystem.Instance.OnPlayerJoined -= new Action<NetPlayer>(OnPlayerJoinedRoom);
		NetworkSystem.Instance.OnPlayerLeft -= new Action<NetPlayer>(OnPlayerLeftRoom);
		inPrivateRoom = NetworkSystem.Instance.InRoom && NetworkSystem.Instance.SessionIsPrivate;
		int num = Mathf.RoundToInt(Time.realtimeSinceStartup - mapEnterTime);
		if (num >= 30)
		{
			if (mapName.Equals("NULL") || mapModId == 0L)
			{
				Debug.LogError("[CustomMapTelemetry::EndMetricsCapture] mapName or mapModID is invalid, throwing out this capture data...");
			}
			else
			{
				GorillaTelemetry.PostCustomMapTracking(mapName, mapModId, mapCreatorUsername, minPlayersInMap, maxPlayersInMap, num, inPrivateRoom);
			}
		}
	}

	private static void StartPerfCapture()
	{
		if (!perfCaptureStarted)
		{
			perfCaptureStarted = true;
			if (instance.perfCaptureCoroutine != null)
			{
				EndPerfCapture();
			}
			drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
			LowestFPS = int.MaxValue;
			HighestFPS = int.MinValue;
			totalFPS = 0;
			totalDrawCalls = 0;
			totalPlayerCount = 0;
			frameCounter = 0;
			instance.perfCaptureCoroutine = instance.StartCoroutine(instance.CaptureMapPerformance());
		}
	}

	private static void EndPerfCapture()
	{
		if (!perfCaptureStarted)
		{
			return;
		}
		perfCaptureStarted = false;
		if (instance.perfCaptureCoroutine != null)
		{
			instance.StopAllCoroutines();
			instance.perfCaptureCoroutine = null;
		}
		drawCallsRecorder.Dispose();
		if (frameCounter == 0)
		{
			return;
		}
		int num = Mathf.RoundToInt(Time.realtimeSinceStartup - mapEnterTime);
		AverageFPS = totalFPS / frameCounter;
		AverageDrawCalls = totalDrawCalls / frameCounter;
		AveragePlayerCount = totalPlayerCount / frameCounter;
		if (num >= 30)
		{
			if (mapName.Equals("NULL") || mapModId == 0L)
			{
				Debug.LogError("[CustomMapTelemetry::EndPerfCapture] mapName or mapModID is invalid, throwing out this capture data...");
			}
			else
			{
				GorillaTelemetry.PostCustomMapPerformance(mapName, mapModId, LowestFPS, LowestFPSDrawCalls, LowestFPSPlayerCount, AverageFPS, AverageDrawCalls, AveragePlayerCount, HighestFPS, HighestFPSDrawCalls, HighestFPSPlayerCount, num);
			}
		}
	}

	private IEnumerator CaptureMapPerformance()
	{
		while (true)
		{
			int num = Mathf.RoundToInt(1f / Time.unscaledDeltaTime);
			int num2 = Mathf.RoundToInt(drawCallsRecorder.LastValue);
			int roomPlayerCount = NetworkSystem.Instance.RoomPlayerCount;
			totalFPS += num;
			totalDrawCalls += num2;
			totalPlayerCount += roomPlayerCount;
			if (num > HighestFPS)
			{
				HighestFPS = num;
				HighestFPSDrawCalls = num2;
				HighestFPSPlayerCount = roomPlayerCount;
			}
			if (num < LowestFPS)
			{
				LowestFPS = num;
				LowestFPSDrawCalls = num2;
				LowestFPSPlayerCount = roomPlayerCount;
			}
			frameCounter++;
			yield return null;
		}
	}

	private void OnDestroy()
	{
		if (perfCaptureCoroutine != null)
		{
			EndMapTracking();
		}
	}
}

using GorillaLocomotion;
using Modio.Mods;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTagScripts.VirtualStumpCustomMaps;

public static class CustomMapDevWatchMode
{
	private static ModId watchedMapId = ModId.Null;

	[OnEnterPlay_Set(true)]
	private static bool returnToPosition = true;

	[OnEnterPlay_Set(false)]
	private static bool listenersAdded;

	private static Vector3 savedPlayerPosition;

	private static Quaternion savedPlayerRotation;

	[OnEnterPlay_Set(false)]
	private static bool hasSavedPosition;

	[OnEnterPlay_SetNew]
	public static UnityEvent<ModId> OnWatchStarted = new UnityEvent<ModId>();

	[OnEnterPlay_SetNew]
	public static UnityEvent<ModId> OnWatchStopped = new UnityEvent<ModId>();

	[OnEnterPlay_SetNew]
	public static UnityEvent<bool> OnReturnToPositionChanged = new UnityEvent<bool>();

	public static bool IsWatching => watchedMapId != ModId.Null;

	public static ModId WatchedMapId => watchedMapId;

	public static bool ReturnToPosition => returnToPosition;

	public static bool IsWatchingMap(ModId modId)
	{
		if (IsWatching)
		{
			return watchedMapId == modId;
		}
		return false;
	}

	public static void StartWatching(ModId modId)
	{
		if (!(modId == ModId.Null) && !(watchedMapId == modId))
		{
			AddListeners();
			StopWatching();
			watchedMapId = modId;
			GTDev.Log($"[CustomMapDevWatchMode::StartWatching] Watching map {modId} " + $"(return to position: {returnToPosition})");
			OnWatchStarted?.Invoke(modId);
		}
	}

	public static void StopWatching()
	{
		if (IsWatching)
		{
			ModId modId = watchedMapId;
			watchedMapId = ModId.Null;
			hasSavedPosition = false;
			GTDev.Log($"[CustomMapDevWatchMode::StopWatching] Stopped watching map {modId}");
			OnWatchStopped?.Invoke(modId);
		}
	}

	public static void SetReturnToPosition(bool enabled)
	{
		if (returnToPosition != enabled)
		{
			returnToPosition = enabled;
			if (!enabled)
			{
				hasSavedPosition = false;
			}
			OnReturnToPositionChanged?.Invoke(enabled);
		}
	}

	public static void CapturePlayerPosition()
	{
		hasSavedPosition = false;
		if (returnToPosition)
		{
			GTPlayer instance = GTPlayer.Instance;
			if (!(instance == null))
			{
				savedPlayerPosition = instance.transform.position;
				savedPlayerRotation = instance.transform.rotation;
				hasSavedPosition = true;
			}
		}
	}

	public static bool RestorePlayerPosition()
	{
		if (!hasSavedPosition || !returnToPosition)
		{
			hasSavedPosition = false;
			return false;
		}
		hasSavedPosition = false;
		GTPlayer instance = GTPlayer.Instance;
		if (instance == null)
		{
			return false;
		}
		instance.TeleportTo(savedPlayerPosition, savedPlayerRotation);
		return true;
	}

	public static void ClearSavedPosition()
	{
		hasSavedPosition = false;
	}

	private static void AddListeners()
	{
		if (!listenersAdded)
		{
			listenersAdded = true;
			ModIOManager.OnModIOLoggedOut.AddListener(StopWatching);
		}
	}

	[OnEnterPlay_Run]
	private static void ResetWatchedMap()
	{
		watchedMapId = ModId.Null;
	}
}

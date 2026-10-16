using System.Collections;
using System.Threading.Tasks;
using GorillaNetworking;
using Modio.Mods;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTagScripts.VirtualStumpCustomMaps;

public class CustomMapUpdateWatcher : MonoBehaviour
{
	private const float PollIntervalSeconds = 10f;

	private const float UnloadTimeoutSeconds = 30f;

	private const float ReloadTimeoutSeconds = 300f;

	private static CustomMapUpdateWatcher instance;

	private ModId watchedModId = ModId.Null;

	private Coroutine pollCoroutine;

	private bool checkInProgress;

	private bool reloadInProgress;

	private bool hmdCallbackHooked;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Bootstrap()
	{
		if (!(instance != null))
		{
			GameObject obj = new GameObject("CustomMapUpdateWatcher");
			Object.DontDestroyOnLoad(obj);
			instance = obj.AddComponent<CustomMapUpdateWatcher>();
		}
	}

	private void OnEnable()
	{
		CustomMapDevWatchMode.OnWatchStarted.AddListener(OnWatchStarted);
		CustomMapDevWatchMode.OnWatchStopped.AddListener(OnWatchStopped);
		if (CustomMapDevWatchMode.IsWatching)
		{
			OnWatchStarted(CustomMapDevWatchMode.WatchedMapId);
		}
	}

	private void OnDisable()
	{
		CustomMapDevWatchMode.OnWatchStarted.RemoveListener(OnWatchStarted);
		CustomMapDevWatchMode.OnWatchStopped.RemoveListener(OnWatchStopped);
		TeardownPolling();
	}

	private void OnWatchStarted(ModId modId)
	{
		if (!(modId == ModId.Null))
		{
			watchedModId = modId;
			CustomMapManager.OnMapUnloadComplete.RemoveListener(OnMapUnloaded);
			CustomMapManager.OnMapUnloadComplete.AddListener(OnMapUnloaded);
			if (!hmdCallbackHooked)
			{
				OVRManager.HMDMounted += OnHmdMounted;
				hmdCallbackHooked = true;
			}
			if (pollCoroutine != null)
			{
				StopCoroutine(pollCoroutine);
			}
			pollCoroutine = StartCoroutine(PollLoop());
			GTDev.Log($"[CustomMapUpdateWatcher] Watching map {modId} for updates.");
		}
	}

	private void OnWatchStopped(ModId modId)
	{
		TeardownPolling();
		GTDev.Log("[CustomMapUpdateWatcher] Stopped watching.");
	}

	private void TeardownPolling()
	{
		watchedModId = ModId.Null;
		CustomMapManager.OnMapUnloadComplete.RemoveListener(OnMapUnloaded);
		if (hmdCallbackHooked)
		{
			OVRManager.HMDMounted -= OnHmdMounted;
			hmdCallbackHooked = false;
		}
		if (pollCoroutine != null)
		{
			StopCoroutine(pollCoroutine);
			pollCoroutine = null;
		}
	}

	private static void StopWatch()
	{
		CustomMapDevWatchMode.StopWatching();
	}

	private void OnMapUnloaded()
	{
		if (!reloadInProgress)
		{
			GTDev.Log("[CustomMapUpdateWatcher] Watched map unloaded; stopping watch.");
			StopWatch();
		}
	}

	private void OnHmdMounted()
	{
		TriggerCheck();
	}

	private IEnumerator PollLoop()
	{
		WaitForSeconds wait = new WaitForSeconds(10f);
		while (CustomMapDevWatchMode.IsWatching)
		{
			yield return wait;
			if (CustomMapDevWatchMode.IsWatching)
			{
				if (!IsPlayerInStump())
				{
					StopWatch();
					break;
				}
				TriggerCheck();
				continue;
			}
			break;
		}
	}

	private void TriggerCheck()
	{
		if (CustomMapDevWatchMode.IsWatching && !checkInProgress && !reloadInProgress && CanReloadNow() && !CustomMapManager.IsLoading() && !CustomMapManager.IsUnloading())
		{
			CheckForUpdate();
		}
	}

	private async Task CheckForUpdate()
	{
		checkInProgress = true;
		try
		{
			ModId modId = watchedModId;
			bool flag = await ModIOManager.HasNewerModfileAvailable(modId);
			if (CustomMapDevWatchMode.IsWatchingMap(modId) && !reloadInProgress && flag && CanReloadNow() && IsPlayerInStump() && !CustomMapManager.IsLoading() && !CustomMapManager.IsUnloading())
			{
				GTDev.Log($"[CustomMapUpdateWatcher] Newer version of {modId} found; reloading.");
				StartCoroutine(ReloadSequence(modId));
			}
		}
		finally
		{
			checkInProgress = false;
		}
	}

	private IEnumerator ReloadSequence(ModId modId)
	{
		reloadInProgress = true;
		CustomMapDevWatchMode.CapturePlayerPosition();
		CustomMapManager.ReturnToVirtualStump();
		yield return null;
		bool unloadComplete = false;
		UnityAction onUnloaded = delegate
		{
			unloadComplete = true;
		};
		CustomMapManager.OnMapUnloadComplete.AddListener(onUnloaded);
		if (CustomMapManager.UnloadMap(returnToSinglePlayerIfInPublic: false))
		{
			float unloadDeadline = Time.time + 30f;
			while (!unloadComplete && Time.time < unloadDeadline)
			{
				yield return null;
			}
		}
		CustomMapManager.OnMapUnloadComplete.RemoveListener(onUnloaded);
		bool loadComplete = false;
		bool loadSucceeded = false;
		UnityAction<bool> onLoaded = delegate(bool ok)
		{
			loadComplete = true;
			loadSucceeded = ok;
		};
		CustomMapManager.OnMapLoadComplete.AddListener(onLoaded);
		CustomMapManager.SetRoomMap(modId);
		CustomMapManager.LoadMap(modId, GTMapLoadSource.room_reload);
		float reloadDeadline = Time.time + 300f;
		while (!loadComplete && Time.time < reloadDeadline)
		{
			yield return null;
		}
		CustomMapManager.OnMapLoadComplete.RemoveListener(onLoaded);
		if (loadSucceeded)
		{
			yield return null;
			GTDev.Log(CustomMapDevWatchMode.RestorePlayerPosition() ? "[CustomMapUpdateWatcher] Reload complete; restored player position." : "[CustomMapUpdateWatcher] Reload complete (no position restore).");
		}
		else
		{
			GTDev.LogWarning("[CustomMapUpdateWatcher] Reload did not complete successfully; leaving the player where they are.");
			CustomMapDevWatchMode.ClearSavedPosition();
		}
		reloadInProgress = false;
		if (CustomMapDevWatchMode.IsWatching && !IsPlayerInStump())
		{
			StopWatch();
		}
	}

	private static bool CanReloadNow()
	{
		NetworkSystem networkSystem = NetworkSystem.Instance;
		if (networkSystem == null)
		{
			return true;
		}
		if (!networkSystem.InRoom)
		{
			return true;
		}
		if (networkSystem.SessionIsPrivate && networkSystem.AllNetPlayers != null)
		{
			return networkSystem.AllNetPlayers.Length <= 1;
		}
		return false;
	}

	private static bool IsPlayerInStump()
	{
		if (GorillaComputer.hasInstance)
		{
			return GorillaComputer.instance.IsPlayerInVirtualStump();
		}
		return false;
	}

	private void OnDestroy()
	{
		CustomMapDevWatchMode.OnWatchStarted.RemoveListener(OnWatchStarted);
		CustomMapDevWatchMode.OnWatchStopped.RemoveListener(OnWatchStopped);
		TeardownPolling();
	}
}

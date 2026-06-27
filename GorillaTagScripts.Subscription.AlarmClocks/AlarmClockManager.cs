using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GorillaLocomotion;
using GorillaNetworking;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTagScripts.Subscription.AlarmClocks;

[DefaultExecutionOrder(10000)]
public sealed class AlarmClockManager : MonoBehaviour
{
	[Serializable]
	public class AlarmClockData
	{
		public string Key;

		public GTZone[] Zones;

		public XSceneRef[] Objects;

		public Transform SpawnPoint;
	}

	public const string SaveDataKey = "AlarmClock";

	[SerializeField]
	private string _loadingMessage = "";

	[SerializeField]
	private float _wrongWarpTolerance = 0.1f;

	[SerializeField]
	private AlarmClockData[] _clockData = new AlarmClockData[0];

	[SerializeField]
	private Transform _defaultSpawn;

	public UnityEvent OnWakeUp;

	private Transform _teleportTarget;

	private AlarmClockData _activeClockData;

	[CanBeNull]
	private AlarmClock _activeClock;

	private float _trackingEndTime;

	public static AlarmClockManager Instance { get; private set; }

	public bool Initialized { get; private set; }

	public string ActiveKey { get; private set; } = "";

	private void Start()
	{
		if (Instance != null)
		{
			Debug.LogError("Duplicate instance of singleton class AlarmClockManager.");
			UnityEngine.Object.Destroy(this);
			return;
		}
		if (_defaultSpawn == null)
		{
			Debug.LogError("No default spawn set in AlarmClockManager.");
			UnityEngine.Object.Destroy(this);
			return;
		}
		Instance = this;
		ActiveKey = PlayerPrefs.GetString("AlarmClock");
		if (!string.IsNullOrEmpty(ActiveKey))
		{
			AlarmClockData alarmClockData = _clockData.FirstOrDefault((AlarmClockData c) => c.Key == ActiveKey);
			if (alarmClockData != null && alarmClockData.SpawnPoint != _defaultSpawn)
			{
				_activeClockData = alarmClockData;
				_teleportTarget = alarmClockData.SpawnPoint;
				StartCoroutine(PerformWakeUpSequence());
				return;
			}
		}
		Initialized = true;
	}

	private IEnumerator PerformWakeUpSequence()
	{
		while (!GTPlayer.hasInstance)
		{
			yield return null;
		}
		GTPlayer.Instance.disableMovement = true;
		while (!GorillaTagger.hasInstance || !GorillaTagger.Instance.mainCamera)
		{
			yield return null;
		}
		PrivateUIRoom.ForceStartOverlay(PrivateUIRoom.OverlaySource.AlarmClock, _loadingMessage);
		PersistLog.Log($"[AC][F{Time.frameCount}] Waiting for game systems");
		while (!GameSystemsLoaded())
		{
			yield return null;
		}
		PersistLog.Log($"[AC][F{Time.frameCount}] Game systems loaded");
		if (PlayFabAuthenticator.instance != null && SubscriptionManager.IsLocalSubscribed())
		{
			RequestLoadZones();
			yield return null;
			while (!AllZonesLoaded())
			{
				while (ZoneManagement.instance.AnyActiveLoadOps())
				{
					yield return null;
				}
				if (!AllZonesLoaded())
				{
					PersistLog.Log(string.Format("[AC][F{0}] Missing zones.  Requested:{1} Active: {2}", Time.frameCount, string.Join(", ", _activeClockData.Zones), string.Join(", ", GetActiveZones())));
					RequestLoadZones();
					yield return null;
				}
			}
			PersistLog.Log($"[AC][F{Time.frameCount}] All zones loaded.");
			XSceneRef[] objects = _activeClockData.Objects;
			foreach (XSceneRef xSceneRef in objects)
			{
				if (xSceneRef.TryResolve(out GameObject result))
				{
					result.SetActive(value: true);
				}
			}
			yield return null;
			GTPlayer.Instance.TeleportTo(_teleportTarget, matchDestinationRotation: true, maintainVelocity: false);
			yield return null;
			int fixAttempts = 0;
			while ((GTPlayer.Instance.mainCamera.transform.position - _teleportTarget.position).sqrMagnitude > _wrongWarpTolerance * _wrongWarpTolerance)
			{
				int i = fixAttempts + 1;
				fixAttempts = i;
				if (i > 10)
				{
					break;
				}
				PersistLog.Log($"[AC][F{Time.frameCount}] AlarmClockManager attempting wrong warp fix. (Off by {GTPlayer.Instance.mainCamera.transform.position - _teleportTarget.position:F2})");
				GTPlayer.Instance.TeleportTo(_teleportTarget, matchDestinationRotation: true, maintainVelocity: false);
				yield return null;
			}
			GTPlayer.Instance.disableMovement = false;
			PrivateUIRoom.StopForcedOverlay(PrivateUIRoom.OverlaySource.AlarmClock);
			OnWakeUp?.Invoke();
		}
		else
		{
			if (PlayFabAuthenticator.instance == null)
			{
				PersistLog.Log($"[AC][F{Time.frameCount}] AlarmClockManager failed wake up because PlayFabAuthenticator was null.");
			}
			else if (PlayFabAuthenticator.instance.loginFailed)
			{
				PersistLog.Log($"[AC][F{Time.frameCount}] AlarmClockManager failed wake up because login failed.");
			}
			PersistLog.Log("No subscription.  Clearing clock data.");
			PlayerPrefs.SetString("AlarmClock", "");
			StartCoroutine(ClearUnsubPlayerData());
		}
		Initialized = true;
		GTPlayer.Instance.disableMovement = false;
		PrivateUIRoom.StopForcedOverlay(PrivateUIRoom.OverlaySource.AlarmClock);
	}

	public static void ToggleAlarmClock(AlarmClock clock)
	{
		Instance.ToggleAlarmClockInternal(clock);
	}

	private void ToggleAlarmClockInternal(AlarmClock clock)
	{
		if (ActiveKey == clock.Key)
		{
			_activeClock?.OnDeactivate?.Invoke();
			_activeClock = null;
			ActiveKey = "";
		}
		else
		{
			_activeClock?.OnDeactivate?.Invoke();
			_activeClock = clock;
			_activeClock?.OnActivate?.Invoke();
			ActiveKey = clock.Key;
		}
		PlayerPrefs.SetString("AlarmClock", ActiveKey);
		Debug.Log("Alarm clock data set to \"" + ActiveKey + "\".");
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	[UsedImplicitly]
	private bool AllUniqueClockKeys()
	{
		HashSet<string> hashSet = new HashSet<string>();
		AlarmClockData[] clockData = _clockData;
		foreach (AlarmClockData alarmClockData in clockData)
		{
			if (!hashSet.Add(alarmClockData.Key))
			{
				return false;
			}
		}
		return true;
	}

	private static bool GameSystemsLoaded()
	{
		if ((!ZoneManagement.instance || !ZoneManagement.instance.Initialized || !CosmeticsController.instance || !CosmeticsController.instance.v2_isCosmeticPlayFabCatalogDataLoaded || !CosmeticsV2Spawner_Dirty.isPrepared || !SubscriptionManager.LocalSubscriptionDataInitialized) && (!(PlayFabAuthenticator.instance != null) || !PlayFabAuthenticator.instance.loginFailed))
		{
			return PlayFabAuthenticator.instance == null;
		}
		return true;
	}

	private bool AllZonesLoaded()
	{
		if (ZoneManagement.instance.AnyActiveLoadOps())
		{
			return false;
		}
		GTZone[] zones = _activeClockData.Zones;
		foreach (GTZone gTZone in zones)
		{
			if (!ZoneManagement.instance.IsSceneLoaded(gTZone))
			{
				return false;
			}
			if (!ZoneManagement.IsZoneLoaded(gTZone))
			{
				PersistLog.Log($"[AC][F{Time.frameCount}] ZoneManagement reports Zone {gTZone} is loaded but SceneManager says no.");
				return false;
			}
		}
		return true;
	}

	private List<GTZone> GetActiveZones()
	{
		List<GTZone> list = new List<GTZone>();
		foreach (GTZone value in Enum.GetValues(typeof(GTZone)))
		{
			if (ZoneManagement.IsInZone(value))
			{
				list.Add(value);
			}
		}
		return list;
	}

	private void RequestLoadZones()
	{
		PersistLog.Log(string.Format("[AC][F{0}] Requesting zones: {1}", Time.frameCount, string.Join(", ", _activeClockData.Zones)));
		ZoneManagement.SetActiveZones(_activeClockData.Zones);
	}

	private void StartTracking()
	{
		StartCoroutine(DoTracking());
	}

	private void StopTracking(float delay)
	{
		Debug.Log($"[AC][F{Time.frameCount}] STOP TRACKING");
		_trackingEndTime = Time.time + delay;
	}

	private IEnumerator DoTracking()
	{
		_trackingEndTime = float.PositiveInfinity;
		while (Time.time < _trackingEndTime)
		{
			Debug.Log($"[AC][F{Time.frameCount}] Pos: {GTPlayer.Instance.transform.position} Off:[{GTPlayer.Instance.LastPosition - _teleportTarget.position}] Distance: {(GTPlayer.Instance.LastPosition - _teleportTarget.position).magnitude}[R{(GTPlayer.Instance.playerRigidBody.position - _teleportTarget.position).magnitude}][C{(GTPlayer.Instance.mainCamera.transform.position - _teleportTarget.position).magnitude}]");
			yield return null;
		}
	}

	private IEnumerator ClearUnsubPlayerData()
	{
		PersistLog.Log("No subscription, warping home.");
		PlayerPrefs.SetString("AlarmClock", "");
		GTPlayer.Instance.TeleportTo(_defaultSpawn, matchDestinationRotation: true, maintainVelocity: false);
		Initialized = true;
		yield return null;
		GTPlayer.Instance.disableMovement = false;
		PrivateUIRoom.StopForcedOverlay(PrivateUIRoom.OverlaySource.AlarmClock);
	}
}

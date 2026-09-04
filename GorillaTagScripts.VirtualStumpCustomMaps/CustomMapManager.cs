using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GT_CustomMapSupportRuntime;
using GorillaExtensions;
using GorillaGameModes;
using GorillaLocomotion;
using GorillaLocomotion.Swimming;
using GorillaNetworking;
using GorillaTag.Rendering;
using GorillaTagScripts.CustomMapSupport;
using GorillaTagScripts.UI.ModIO;
using Modio;
using Modio.Mods;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTagScripts.VirtualStumpCustomMaps;

public class CustomMapManager : MonoBehaviour, IBuildValidation
{
	[OnEnterPlay_SetNull]
	private static volatile CustomMapManager instance;

	[OnEnterPlay_Set(false)]
	private static bool hasInstance = false;

	[SerializeField]
	private GameObject virtualStumpToggleableRoot;

	[SerializeField]
	private Transform returnToVirtualStumpTeleportLocation;

	[SerializeField]
	private List<Transform> virtualStumpTeleportLocations;

	[SerializeField]
	private GameObject[] rootObjectsToDeactivateAfterTeleport;

	[Tooltip("Objects visually hidden (renderers only, so their behaviour keeps running) while in a Featured map (A/B), and shown again on exit / when in the Custom lobby.")]
	[SerializeField]
	private List<GameObject> featuredMapDisabledObjects;

	[SerializeField]
	private GorillaFriendCollider virtualStumpPlayerDetector;

	[SerializeField]
	private ZoneShaderSettings virtualStumpZoneShaderSettings;

	[SerializeField]
	private BetterDayNightManager dayNightManager;

	[SerializeField]
	private GhostReactorManager ghostReactorManager;

	[SerializeField]
	private GRReviveStation defaultReviveStation;

	[SerializeField]
	private ZoneShaderSettings customMapDefaultZoneShaderSettings;

	[SerializeField]
	private GameObject teleportingHUDPrefab;

	[SerializeField]
	private AudioSource localTeleportSFXSource;

	[SerializeField]
	private VirtualStumpTeleporter defaultTeleporter;

	[SerializeField]
	private float maxPostTeleportRoomProcessingTime = 15f;

	private static VirtualStumpTeleporter lastUsedTeleporter;

	private static string preVStumpGamemode = "";

	private static bool activateSkipTeleport;

	private static bool activateDeferZoneToNode;

	private static bool activateHasAutoLoadOverride;

	private static ModId activateAutoLoadModIdOverride = ModId.Null;

	private static bool activateIsActive;

	private static VirtualStumpActivateMode activateCurrentMode;

	private static ModId pendingRoomChangeReloadModId = ModId.Null;

	private static bool customMapDefaultZoneShaderSettingsInitialized;

	private static ZoneShaderSettings loadedCustomMapDefaultZoneShaderSettings;

	private static CMSZoneShaderSettings.CMSZoneShaderProperties customMapDefaultZoneShaderProperties;

	private static readonly List<ZoneShaderSettings> allCustomMapZoneShaderSettings = new List<ZoneShaderSettings>();

	private static bool loadInProgress = false;

	private static ModId loadingMapId = ModId.Null;

	private static GTMapLoadSource pendingMapLoadSource = GTMapLoadSource.none;

	private static bool unloadInProgress = false;

	private static ModId unloadingMapId = ModId.Null;

	private static List<ModId> abortModLoadIds = new List<ModId>();

	private static bool waitingForModDownload = false;

	private static bool waitingForModInstall = false;

	private static ModId waitingForModInstallId = ModId.Null;

	private static MapLoadStatus currentLoadStatus = MapLoadStatus.None;

	private static int currentLoadProgress = 0;

	private static string currentLoadMessage = "";

	private static MapLoadStatus lastBroadcastFileStatus = MapLoadStatus.None;

	private static int lastBroadcastFilePercent = -1;

	private static ModId trackedDownloadMapId = ModId.Null;

	private static bool preTeleportInPrivateRoom = false;

	private static string pendingNewPrivateRoomName = "";

	private static Action<bool> currentTeleportCallback;

	private static bool waitingForLoginDisconnect = false;

	private static bool waitingForDisconnect = false;

	private static bool waitingForRoomJoin = false;

	private static bool shouldRetryJoin = false;

	private static short pendingTeleportVFXIdx = -1;

	private static bool exitVirtualStumpPending = false;

	private static ModId currentRoomMapModId = ModId.Null;

	private static bool currentRoomMapApproved = false;

	private static VirtualStumpTeleportingHUD teleportingHUD;

	private static Coroutine delayedEndTeleportCoroutine;

	private static Coroutine delayedJoinCoroutine;

	private static Coroutine delayedTryAutoLoadCoroutine;

	public static UnityEvent<ModId> OnRoomMapChanged = new UnityEvent<ModId>();

	public static UnityEvent<MapLoadStatus, int, string> OnMapLoadStatusChanged = new UnityEvent<MapLoadStatus, int, string>();

	public static UnityEvent<bool> OnMapLoadComplete = new UnityEvent<bool>();

	public static UnityEvent OnMapUnloadComplete = new UnityEvent();

	private const ModChangeType ModFileProgressChanges = ModChangeType.DownloadProgress | ModChangeType.FileState;

	private const string PreparingMessage = "PREPARING MAP";

	private const string DownloadQueuedMessage = "WAITING FOR DOWNLOAD";

	private const string DownloadingMessage = "DOWNLOADING MAP FILES";

	private const string InstallQueuedMessage = "WAITING TO INSTALL";

	private const string InstallingMessage = "INSTALLING MAP FILES";

	public static bool WaitingForRoomJoin => waitingForRoomJoin;

	public static bool WaitingForDisconnect => waitingForDisconnect;

	public static long LoadingMapId => loadingMapId;

	public static long UnloadingMapId => unloadingMapId;

	public static MapLoadStatus CurrentLoadStatus => currentLoadStatus;

	public static int CurrentLoadProgress => currentLoadProgress;

	public static string CurrentLoadMessage => currentLoadMessage;

	public static VirtualStumpActivateMode CurrentActivateMode => activateCurrentMode;

	public static ModId FeaturedLockedMapId => activateAutoLoadModIdOverride;

	public bool BuildValidationCheck()
	{
		if (defaultTeleporter.IsNull())
		{
			Debug.LogError("CustomMapManager does not have its \"Default Teleporter\" property.");
			return false;
		}
		return true;
	}

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
			hasInstance = true;
		}
		else if (instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	public void OnEnable()
	{
		UGCPermissionManager.UnsubscribeFromUGCEnabled(OnUGCEnabled);
		UGCPermissionManager.SubscribeToUGCEnabled(OnUGCEnabled);
		UGCPermissionManager.UnsubscribeFromUGCDisabled(OnUGCDisabled);
		UGCPermissionManager.SubscribeToUGCDisabled(OnUGCDisabled);
		CMSSerializer.OnTriggerHistoryProcessedForScene.RemoveListener(OnSceneTriggerHistoryProcessed);
		CMSSerializer.OnTriggerHistoryProcessedForScene.AddListener(OnSceneTriggerHistoryProcessed);
		ModIOManager.OnModManagementEvent.RemoveListener(HandleModManagementEvent);
		ModIOManager.OnModManagementEvent.AddListener(HandleModManagementEvent);
		Mod.RemoveChangeListener(ModChangeType.DownloadProgress | ModChangeType.FileState, HandleModFileProgress);
		Mod.AddChangeListener(ModChangeType.DownloadProgress | ModChangeType.FileState, HandleModFileProgress);
		RoomSystem.JoinedRoomEvent -= new Action(OnJoinedRoom);
		RoomSystem.JoinedRoomEvent += new Action(OnJoinedRoom);
		NetworkSystem.Instance.OnReturnedToSinglePlayer -= new Action(OnDisconnected);
		NetworkSystem.Instance.OnReturnedToSinglePlayer += new Action(OnDisconnected);
	}

	public void OnDisable()
	{
		UGCPermissionManager.UnsubscribeFromUGCEnabled(OnUGCEnabled);
		UGCPermissionManager.UnsubscribeFromUGCDisabled(OnUGCDisabled);
		CMSSerializer.OnTriggerHistoryProcessedForScene.RemoveListener(OnSceneTriggerHistoryProcessed);
		ModIOManager.OnModManagementEvent.RemoveListener(HandleModManagementEvent);
		Mod.RemoveChangeListener(ModChangeType.DownloadProgress | ModChangeType.FileState, HandleModFileProgress);
		RoomSystem.JoinedRoomEvent -= new Action(OnJoinedRoom);
		NetworkSystem.Instance.OnReturnedToSinglePlayer -= new Action(OnDisconnected);
	}

	private void OnUGCEnabled()
	{
	}

	private void OnUGCDisabled()
	{
	}

	private void Start()
	{
		CustomMapLoader.Initialize(OnMapLoadProgress, OnMapLoadFinished, OnSceneLoaded, OnSceneUnloaded);
		for (int num = virtualStumpTeleportLocations.Count - 1; num >= 0; num--)
		{
			if (virtualStumpTeleportLocations[num] == null)
			{
				virtualStumpTeleportLocations.RemoveAt(num);
			}
		}
		if (defaultTeleporter.IsNull())
		{
			GTDev.LogError("[CustomMapManager::Start] \"Default Teleporter\" property is invalid.");
		}
		virtualStumpToggleableRoot.SetActive(value: false);
		base.gameObject.SetActive(value: false);
	}

	private void OnDestroy()
	{
		if (instance == this)
		{
			instance = null;
			hasInstance = false;
		}
		UGCPermissionManager.UnsubscribeFromUGCEnabled(OnUGCEnabled);
		UGCPermissionManager.UnsubscribeFromUGCDisabled(OnUGCDisabled);
		CMSSerializer.OnTriggerHistoryProcessedForScene.RemoveListener(OnSceneTriggerHistoryProcessed);
		ModIOManager.OnModManagementEvent.RemoveListener(HandleModManagementEvent);
		Mod.RemoveChangeListener(ModChangeType.DownloadProgress | ModChangeType.FileState, HandleModFileProgress);
		RoomSystem.JoinedRoomEvent -= new Action(OnJoinedRoom);
		NetworkSystem.Instance.OnReturnedToSinglePlayer -= new Action(OnDisconnected);
	}

	public static void TrackMapDownload(ModId modId)
	{
		ResetModFileProgressTracking();
		trackedDownloadMapId = modId;
		BroadcastMapLoadProgress(MapLoadStatus.Downloading, 0, "WAITING FOR DOWNLOAD");
	}

	public static void StopTrackingMapDownload()
	{
		trackedDownloadMapId = ModId.Null;
		if (!loadInProgress)
		{
			ResetModFileProgressTracking();
			BroadcastMapLoadProgress(MapLoadStatus.None, 0, "");
		}
	}

	private static bool IsPlayerWaitingOnMap(ModId modId)
	{
		if (loadInProgress && loadingMapId == modId)
		{
			return true;
		}
		if (trackedDownloadMapId != ModId.Null)
		{
			return trackedDownloadMapId == modId;
		}
		return false;
	}

	private static void HandleModFileProgress(Mod mod, ModChangeType changeType)
	{
		if (mod != null && mod.File != null && IsPlayerWaitingOnMap(mod.Id))
		{
			BroadcastModFileState(mod);
		}
	}

	private static void BroadcastModFileState(Mod mod)
	{
		if (mod?.File == null)
		{
			return;
		}
		switch (mod.File.State)
		{
		case ModFileState.None:
		case ModFileState.Queued:
			BroadcastModFileProgress(MapLoadStatus.Downloading, 0, "WAITING FOR DOWNLOAD");
			break;
		case ModFileState.Downloading:
			BroadcastModFileProgress(MapLoadStatus.Downloading, GetFileStatePercent(mod), "DOWNLOADING MAP FILES");
			break;
		case ModFileState.Downloaded:
			BroadcastModFileProgress(MapLoadStatus.Installing, 0, "WAITING TO INSTALL");
			break;
		case ModFileState.Installing:
		case ModFileState.Updating:
			BroadcastModFileProgress(MapLoadStatus.Installing, GetFileStatePercent(mod), "INSTALLING MAP FILES");
			break;
		case ModFileState.Installed:
			if (!loadInProgress)
			{
				trackedDownloadMapId = ModId.Null;
				BroadcastModFileProgress(MapLoadStatus.None, 0, "");
			}
			break;
		case ModFileState.FileOperationFailed:
			trackedDownloadMapId = ModId.Null;
			if (!loadInProgress)
			{
				BroadcastModFileProgress(MapLoadStatus.Error, 0, mod.File.FileStateErrorCause.GetMessage() ?? "MAP DOWNLOAD FAILED");
			}
			break;
		case ModFileState.Uninstalling:
			break;
		}
	}

	private static int GetFileStatePercent(Mod mod)
	{
		return Mathf.Clamp(Mathf.RoundToInt(mod.File.FileStateProgress * 100f), 0, 100);
	}

	private static void ResetModFileProgressTracking()
	{
		lastBroadcastFileStatus = MapLoadStatus.None;
		lastBroadcastFilePercent = -1;
		trackedDownloadMapId = ModId.Null;
	}

	private static void BroadcastModFileProgress(MapLoadStatus status, int percent, string message)
	{
		if (status != lastBroadcastFileStatus || percent != lastBroadcastFilePercent)
		{
			lastBroadcastFileStatus = status;
			lastBroadcastFilePercent = percent;
			BroadcastMapLoadProgress(status, percent, message);
		}
	}

	private void HandleModManagementEvent(Mod mod, Modfile modfile, ModInstallationManagement.OperationType jobType, ModInstallationManagement.OperationPhase jobPhase)
	{
		if (!waitingForModInstall || !(waitingForModInstallId == mod.Id))
		{
			return;
		}
		if (abortModLoadIds.Contains(mod.Id))
		{
			abortModLoadIds.Remove(mod.Id);
			if (waitingForModInstallId.Equals(mod.Id))
			{
				waitingForModInstall = false;
				waitingForModDownload = false;
				waitingForModInstallId = ModId.Null;
			}
			return;
		}
		switch (modfile.State)
		{
		case ModFileState.Downloading:
		case ModFileState.Updating:
			waitingForModDownload = true;
			break;
		case ModFileState.Downloaded:
			waitingForModDownload = false;
			break;
		case ModFileState.FileOperationFailed:
			switch (jobType)
			{
			case ModInstallationManagement.OperationType.Download:
				Debug.LogError("[CustomMapManager::HandleModManagementEvent] Failed to download map with modID " + mod.Id.ToString() + ", error: " + modfile.FileStateErrorCause.GetMessage());
				HandleMapLoadFailed("FAILED TO DOWNLOAD MAP: " + modfile.FileStateErrorCause.GetMessage());
				waitingForModDownload = false;
				break;
			case ModInstallationManagement.OperationType.Install:
				Debug.LogError("[CustomMapManager::HandleModManagementEvent] Failed to install map with modID " + mod.Id.ToString() + ", error: " + modfile.FileStateErrorCause.GetMessage());
				HandleMapLoadFailed("FAILED TO INSTALL MAP: " + modfile.FileStateErrorCause.GetMessage());
				break;
			case ModInstallationManagement.OperationType.Update:
				Debug.LogError("[CustomMapManager::HandleModManagementEvent] Failed to update map with modID " + mod.Id.ToString() + ", error: " + modfile.FileStateErrorCause.GetMessage());
				HandleMapLoadFailed("FAILED TO UPDATE MAP: " + modfile.FileStateErrorCause.GetMessage());
				break;
			}
			break;
		case ModFileState.Installed:
			waitingForModDownload = false;
			LoadInstalledMap(mod);
			break;
		case ModFileState.Installing:
		case ModFileState.Uninstalling:
			break;
		}
	}

	internal static void TeleportToVirtualStump(VirtualStumpTeleporter fromTeleporter, Action<bool> callback)
	{
		if (!UGCPermissionManager.HasNoMapAccess)
		{
			if (!hasInstance || fromTeleporter == null)
			{
				callback?.Invoke(obj: false);
				return;
			}
			activateSkipTeleport = false;
			activateDeferZoneToNode = false;
			activateHasAutoLoadOverride = false;
			activateIsActive = false;
			instance.gameObject.SetActive(value: true);
			instance.StartCoroutine(Internal_TeleportToVirtualStump(fromTeleporter, callback));
		}
	}

	public static async void Activate(VirtualStumpActivateMode mode, bool hasEntryTeleportNode = true)
	{
		if (UGCPermissionManager.HasNoMapAccess || !hasInstance || activateIsActive)
		{
			return;
		}
		activateIsActive = true;
		activateCurrentMode = mode;
		SetFeaturedMapObjectsHidden(IsInFeaturedMode());
		if (GorillaComputer.hasInstance)
		{
			GorillaComputer.instance.SetVStumpRoomModePrefix(GetActivateRoomModePrefix());
		}
		ModId autoLoadModId = ModId.Null;
		if (mode != VirtualStumpActivateMode.Custom)
		{
			int index = ((mode != VirtualStumpActivateMode.FeatureA) ? 1 : 0);
			var (error, list) = await ModIOManager.GetFeaturedMaps();
			if ((bool)error || list == null || index >= list.Count)
			{
				GTDev.LogWarning("[CustomMapManager::Activate] Could not resolve featured map index " + $"{index} for {mode}; opening the stump without an auto-load.");
			}
			else
			{
				autoLoadModId = list[index].Id;
			}
		}
		if (!hasInstance)
		{
			activateIsActive = false;
			return;
		}
		if (instance.defaultTeleporter.IsNull())
		{
			GTDev.LogError("[CustomMapManager::Activate] Default Teleporter is not set; cannot activate.");
			activateIsActive = false;
			return;
		}
		activateSkipTeleport = true;
		activateDeferZoneToNode = hasEntryTeleportNode;
		activateHasAutoLoadOverride = true;
		activateAutoLoadModIdOverride = autoLoadModId;
		instance.gameObject.SetActive(value: true);
		instance.StartCoroutine(Internal_TeleportToVirtualStump(instance.defaultTeleporter, null));
	}

	public static void Deactivate()
	{
		if (hasInstance && GorillaComputer.hasInstance && GorillaComputer.instance.IsPlayerInVirtualStump())
		{
			activateIsActive = false;
			activateSkipTeleport = true;
			ExitVirtualStump(null);
		}
	}

	public static bool IsInFeaturedMode()
	{
		if (activateCurrentMode != VirtualStumpActivateMode.FeatureA)
		{
			return activateCurrentMode == VirtualStumpActivateMode.FeatureB;
		}
		return true;
	}

	public static bool IsFeaturedMapLocked()
	{
		if (activateIsActive && IsInFeaturedMode())
		{
			return activateAutoLoadModIdOverride != ModId.Null;
		}
		return false;
	}

	private static void SetFeaturedMapObjectsHidden(bool hidden)
	{
		if (!hasInstance || instance.featuredMapDisabledObjects == null)
		{
			return;
		}
		foreach (GameObject featuredMapDisabledObject in instance.featuredMapDisabledObjects)
		{
			if (featuredMapDisabledObject == null)
			{
				continue;
			}
			Renderer[] componentsInChildren = featuredMapDisabledObject.GetComponentsInChildren<Renderer>(includeInactive: true);
			foreach (Renderer renderer in componentsInChildren)
			{
				if (renderer != null)
				{
					renderer.forceRenderingOff = hidden;
				}
			}
			Collider[] componentsInChildren2 = featuredMapDisabledObject.GetComponentsInChildren<Collider>(includeInactive: true);
			foreach (Collider collider in componentsInChildren2)
			{
				if (collider != null)
				{
					collider.enabled = !hidden;
				}
			}
		}
	}

	public static void PrepareFeaturedMapReloadOnRoomChange()
	{
		pendingRoomChangeReloadModId = GetRoomMapId();
	}

	public static void EnterVirtualStumpZone()
	{
		if (!hasInstance)
		{
			return;
		}
		if (VRRig.LocalRig.IsNotNull() && VRRig.LocalRig.zoneEntity.IsNotNull())
		{
			VRRig.LocalRig.zoneEntity.DisableZoneChanges();
		}
		ZoneManagement.SetActiveZone(GTZone.customMaps);
		GameObject[] array = instance.rootObjectsToDeactivateAfterTeleport;
		foreach (GameObject gameObject in array)
		{
			if (gameObject != null && gameObject.gameObject != null)
			{
				gameObject.gameObject.SetActive(value: false);
			}
		}
		if (instance.virtualStumpZoneShaderSettings.IsNotNull())
		{
			instance.virtualStumpZoneShaderSettings.BecomeActiveInstance();
		}
		else
		{
			ZoneShaderSettings.ActivateDefaultSettings();
		}
	}

	public void OnEnteredVirtualStumpZone()
	{
		EnterVirtualStumpZone();
	}

	private static string GetActivateRoomModePrefix()
	{
		return activateCurrentMode switch
		{
			VirtualStumpActivateMode.FeatureA => "A", 
			VirtualStumpActivateMode.FeatureB => "B", 
			_ => "C", 
		};
	}

	private static ModId GetEffectiveAutoLoadModId()
	{
		if (activateHasAutoLoadOverride)
		{
			return activateAutoLoadModIdOverride;
		}
		if (!lastUsedTeleporter.IsNotNull())
		{
			return ModId.Null;
		}
		return lastUsedTeleporter.GetAutoLoadMapModId();
	}

	private static IEnumerator Internal_TeleportToVirtualStump(VirtualStumpTeleporter fromTeleporter, Action<bool> callback)
	{
		lastUsedTeleporter = fromTeleporter;
		preVStumpGamemode = GorillaComputer.instance.currentGameMode.Value;
		if (lastUsedTeleporter.GetAutoLoadGamemode() != GameModeType.None && lastUsedTeleporter.GetAutoLoadGamemode() != GameModeType.Count)
		{
			GorillaComputer.instance.SetGameModeWithoutButton(lastUsedTeleporter.GetAutoLoadGamemode().ToString());
		}
		GTDev.Log("[CustomMapManager::TeleportToVirtualStump] Teleporting to Virtual Stump...");
		if (!activateSkipTeleport)
		{
			PrivateUIRoom.ForceStartOverlay(PrivateUIRoom.OverlaySource.CustomMap);
			GorillaTagger.Instance.overrideNotInFocus = true;
		}
		GreyZoneManager greyZoneManager = GreyZoneManager.Instance;
		if (greyZoneManager != null)
		{
			greyZoneManager.ForceStopGreyZone();
		}
		if (instance.virtualStumpTeleportLocations.Count > 0)
		{
			int index = UnityEngine.Random.Range(0, instance.virtualStumpTeleportLocations.Count);
			Transform randTeleportTarget = instance.virtualStumpTeleportLocations[index];
			if (!activateSkipTeleport)
			{
				instance.EnableTeleportHUD(enteringVirtualStump: true);
				lastUsedTeleporter.PlayTeleportEffects(forLocalPlayer: true, toVStump: true, instance.localTeleportSFXSource, sendRPC: true);
			}
			yield return new WaitForSeconds(0.75f);
			CosmeticsController.instance.ClearCheckoutAndCart(sendEvent: false);
			instance.virtualStumpToggleableRoot.SetActive(value: true);
			if (!activateSkipTeleport)
			{
				GTPlayer.Instance.TeleportTo(randTeleportTarget, matchDestinationRotation: true, maintainVelocity: false);
			}
			GorillaComputer.instance.SetInVirtualStump(inVirtualStump: true);
			yield return null;
			if (!activateDeferZoneToNode)
			{
				EnterVirtualStumpZone();
			}
			instance.ghostReactorManager.reactor.EnableGhostReactorForVirtualStump();
			currentTeleportCallback = callback;
			pendingNewPrivateRoomName = "";
			preTeleportInPrivateRoom = false;
			if (NetworkSystem.Instance.InRoom)
			{
				if (NetworkSystem.Instance.SessionIsPrivate)
				{
					preTeleportInPrivateRoom = true;
					waitingForRoomJoin = true;
					pendingNewPrivateRoomName = GetActivateRoomModePrefix() + GorillaComputer.instance.VStumpRoomPrepend + NetworkSystem.Instance.RoomName;
				}
				GTDev.Log("[CustomMapManager::TeleportToVirtualStump] Returning to singleplayer...");
				waitingForLoginDisconnect = true;
				NetworkSystem.Instance.ReturnToSinglePlayer();
			}
			else
			{
				GTDev.Log("[CustomMapManager::TeleportToVirtualStump] Attempting auto-login to mod.io...");
				AttemptAutoLogin();
			}
		}
		else
		{
			GTDev.Log("[CustomMapManager::TeleportToVirtualStump] Not Teleporting, virtualStumpTeleportLocations is empty!");
			EndTeleport(teleportSuccessful: false);
		}
	}

	private static void OnAutoLoginComplete(Error error)
	{
		GTDev.Log($"[CustomMapManager::OnAutoLoginComplete] Error: {error}");
		if (!hasInstance)
		{
			Debug.LogError("[CustomMapManager::OnAutoLoginComplete] CustomMapManager not initialized!");
			return;
		}
		GTDev.Log($"[CustomMapManager::OnAutoLoginComplete] Needs to rejoin private room: {preTeleportInPrivateRoom}");
		if (preTeleportInPrivateRoom)
		{
			if (NetworkSystem.Instance.netState != NetSystemState.Idle)
			{
				GTDev.Log($"[CustomMapManager::OnAutoLoginComplete] Netstate not Idle, delaying join attempt. CurrentStatus: {NetworkSystem.Instance.netState}");
				delayedJoinCoroutine = instance.StartCoroutine(DelayedJoinVStumpPrivateRoom());
			}
			else
			{
				GTDev.Log("[CustomMapManager::OnAutoLoginComplete] joining @ version of private room: " + pendingNewPrivateRoomName);
				PhotonNetworkController.Instance.AttemptToJoinSpecificRoomWithCallback(pendingNewPrivateRoomName, JoinType.Solo, OnJoinSpecificRoomResult);
			}
		}
		GTDev.Log($"[CustomMapManager::OnAutoLoginComplete] Waiting For D/C? {waitingForDisconnect}");
		if (!preTeleportInPrivateRoom && !waitingForDisconnect)
		{
			GTDev.Log("[CustomMapManager::OnAutoLoginComplete] Ending teleport...");
			EndTeleport(teleportSuccessful: true);
		}
		preTeleportInPrivateRoom = false;
	}

	private static IEnumerator DelayedJoinVStumpPrivateRoom()
	{
		GTDev.Log("[CustomMapManager::DelayedJoinVStumpPrivateRoom] waiting for netstate to be Idle");
		while (NetworkSystem.Instance.netState != NetSystemState.Idle)
		{
			yield return null;
		}
		GTDev.Log("[CustomMapManager::DelayedJoinVStumpPrivateRoom] joining @ version of private room: " + pendingNewPrivateRoomName);
		PhotonNetworkController.Instance.AttemptToJoinSpecificRoomWithCallback(pendingNewPrivateRoomName, JoinType.Solo, OnJoinSpecificRoomResult);
	}

	public static void ExitVirtualStump(Action<bool> callback)
	{
		if (!hasInstance)
		{
			return;
		}
		if (lastUsedTeleporter.IsNull())
		{
			if (instance.defaultTeleporter.IsNull())
			{
				callback?.Invoke(obj: false);
			}
			else
			{
				lastUsedTeleporter = instance.defaultTeleporter;
			}
		}
		if (delayedJoinCoroutine != null)
		{
			instance.StopCoroutine(delayedJoinCoroutine);
			delayedJoinCoroutine = null;
		}
		if (delayedTryAutoLoadCoroutine != null)
		{
			instance.StopCoroutine(delayedTryAutoLoadCoroutine);
			delayedTryAutoLoadCoroutine = null;
		}
		instance.dayNightManager.RequestRepopulateLightmaps();
		if (!activateSkipTeleport)
		{
			PrivateUIRoom.ForceStartOverlay(PrivateUIRoom.OverlaySource.CustomMap);
			GorillaTagger.Instance.overrideNotInFocus = true;
		}
		if (!activateSkipTeleport)
		{
			instance.EnableTeleportHUD(enteringVirtualStump: false);
		}
		currentTeleportCallback = callback;
		exitVirtualStumpPending = true;
		if (!UnloadMap(returnToSinglePlayerIfInPublic: false))
		{
			FinalizeExitVirtualStump();
		}
	}

	private static void FinalizeExitVirtualStump()
	{
		if (!hasInstance)
		{
			return;
		}
		GTPlayer.Instance.SetHoverActive(enable: false);
		VRRig.LocalRig.hoverboardVisual.SetNotHeld();
		RoomSystem.ClearOverridenRoomSize();
		CosmeticsController.instance.ClearCheckoutAndCart(sendEvent: false);
		GameObject[] array = instance.rootObjectsToDeactivateAfterTeleport;
		foreach (GameObject gameObject in array)
		{
			if (gameObject != null)
			{
				gameObject.gameObject.SetActive(value: true);
			}
		}
		if (lastUsedTeleporter.GetReturnGamemode() != GameModeType.None && lastUsedTeleporter.GetReturnGamemode() != GameModeType.Count)
		{
			GorillaComputer.instance.SetGameModeWithoutButton(lastUsedTeleporter.GetReturnGamemode().ToString());
		}
		else if (preVStumpGamemode != "")
		{
			GorillaComputer.instance.SetGameModeWithoutButton(preVStumpGamemode);
			preVStumpGamemode = "";
		}
		if (VRRig.LocalRig.IsNotNull())
		{
			GRPlayer component = VRRig.LocalRig.GetComponent<GRPlayer>();
			if (component != null && component.State == GRPlayer.GRPlayerState.Ghost)
			{
				instance.defaultReviveStation.RevivePlayer(component);
			}
		}
		if (!activateSkipTeleport)
		{
			ZoneManagement.SetActiveZone(lastUsedTeleporter.GetZone());
		}
		if (VRRig.LocalRig.IsNotNull() && VRRig.LocalRig.zoneEntity.IsNotNull())
		{
			VRRig.LocalRig.zoneEntity.EnableZoneChanges();
		}
		GorillaComputer.instance.SetInVirtualStump(inVirtualStump: false);
		activateIsActive = false;
		SetFeaturedMapObjectsHidden(hidden: false);
		if (!activateSkipTeleport)
		{
			GTPlayer.Instance.TeleportTo(lastUsedTeleporter.GetReturnTransform(), matchDestinationRotation: true, maintainVelocity: false);
		}
		instance.virtualStumpToggleableRoot.SetActive(value: false);
		ZoneShaderSettings.ActivateDefaultSettings();
		VRRig.LocalRig.EnableVStumpReturnWatch(on: false);
		GTPlayer.Instance.ForceHoverDisallowed();
		exitVirtualStumpPending = false;
		if (delayedEndTeleportCoroutine != null)
		{
			instance.StopCoroutine(delayedEndTeleportCoroutine);
		}
		delayedEndTeleportCoroutine = instance.StartCoroutine(DelayedEndTeleport());
		if (preTeleportInPrivateRoom)
		{
			waitingForRoomJoin = true;
			pendingNewPrivateRoomName = GorillaComputer.instance.StripVStumpRoomPrefix(pendingNewPrivateRoomName);
			PhotonNetworkController.Instance.AttemptToJoinSpecificRoomWithCallback(pendingNewPrivateRoomName, JoinType.Solo, OnJoinSpecificRoomResult);
		}
		else if (NetworkSystem.Instance.InRoom)
		{
			if (NetworkSystem.Instance.SessionIsPrivate)
			{
				waitingForRoomJoin = true;
				pendingNewPrivateRoomName = GorillaComputer.instance.StripVStumpRoomPrefix(NetworkSystem.Instance.RoomName);
				PhotonNetworkController.Instance.AttemptToJoinSpecificRoomWithCallback(pendingNewPrivateRoomName, JoinType.Solo, OnJoinSpecificRoomResult);
			}
			else if (lastUsedTeleporter.GetExitVStumpJoinTrigger() != null)
			{
				waitingForRoomJoin = true;
				GorillaComputer.instance.allowedMapsToJoin = lastUsedTeleporter.GetExitVStumpJoinTrigger().myCollider.myAllowedMapsToJoin;
				Debug.Log($"[CustomMapManager::FinalizeExit] allowedMaps: {GorillaComputer.instance.allowedMapsToJoin}");
				PhotonNetworkController.Instance.AttemptToJoinPublicRoom(lastUsedTeleporter.GetExitVStumpJoinTrigger());
			}
			else
			{
				NetworkSystem.Instance.ReturnToSinglePlayer();
			}
		}
		else if (lastUsedTeleporter.GetExitVStumpJoinTrigger() != null)
		{
			GorillaComputer.instance.allowedMapsToJoin = lastUsedTeleporter.GetExitVStumpJoinTrigger().myCollider.myAllowedMapsToJoin;
			Debug.Log($"[CustomMapManager::FinalizeExit] allowedMaps: {GorillaComputer.instance.allowedMapsToJoin}");
			waitingForRoomJoin = true;
			PhotonNetworkController.Instance.AttemptToJoinPublicRoom(lastUsedTeleporter.GetExitVStumpJoinTrigger());
		}
		else
		{
			EndTeleport(teleportSuccessful: true);
		}
	}

	private static void OnJoinSpecificRoomResult(NetJoinResult result)
	{
		GTDev.Log("[CustomMapManager::OnJoinSpecificRoomResult] Result: " + result);
		switch (result)
		{
		case NetJoinResult.AlreadyInRoom:
			instance.OnJoinedRoom();
			break;
		case NetJoinResult.Failed_Full:
			instance.OnJoinRoomFailed();
			break;
		case NetJoinResult.Failed_Other:
			GTDev.Log("[CustomMapManager::OnJoinSpecificRoomResult] Joining " + pendingNewPrivateRoomName + " failed, marking for retry... ");
			waitingForDisconnect = true;
			shouldRetryJoin = true;
			break;
		}
	}

	private static void OnJoinSpecificRoomResultFailureAllowed(NetJoinResult result)
	{
		if (hasInstance)
		{
			GTDev.Log("[CustomMapManager::OnJoinSpecificRoomResultFailureAllowed] Result: " + result);
			switch (result)
			{
			case NetJoinResult.Success:
			case NetJoinResult.FallbackCreated:
				break;
			case NetJoinResult.AlreadyInRoom:
				instance.OnJoinedRoom();
				break;
			case NetJoinResult.Failed_Full:
			case NetJoinResult.Failed_Other:
				instance.OnJoinRoomFailed();
				break;
			}
		}
	}

	public static bool AreAllPlayersInVirtualStump()
	{
		if (!hasInstance)
		{
			return false;
		}
		foreach (VRRig activeRig in VRRigCache.ActiveRigs)
		{
			if (!instance.virtualStumpPlayerDetector.playerIDsCurrentlyTouching.Contains(activeRig.creator.UserId))
			{
				return false;
			}
		}
		return true;
	}

	public static bool IsRemotePlayerInVirtualStump(string playerID)
	{
		if (!hasInstance || instance.virtualStumpPlayerDetector.IsNull())
		{
			return false;
		}
		return instance.virtualStumpPlayerDetector.playerIDsCurrentlyTouching.Contains(playerID);
	}

	public static bool IsLocalPlayerInVirtualStump()
	{
		if (!hasInstance || instance.virtualStumpPlayerDetector.IsNull() || VRRig.LocalRig.IsNull())
		{
			return false;
		}
		if (!instance.virtualStumpPlayerDetector.playerIDsCurrentlyTouching.Contains(VRRig.LocalRig.creator.UserId))
		{
			return false;
		}
		return true;
	}

	private void OnDisconnected()
	{
		if (!hasInstance)
		{
			return;
		}
		if (GorillaComputer.hasInstance)
		{
			GorillaComputer.instance.IsPlayerInVirtualStump();
		}
		else
			_ = 0;
		ClearRoomMap();
		if (waitingForLoginDisconnect)
		{
			waitingForLoginDisconnect = false;
			GTDev.Log("[CustomMapManager::OnDisconnected] Attempting auto-login to mod.io...");
			AttemptAutoLogin();
		}
		else if (waitingForDisconnect)
		{
			waitingForDisconnect = false;
			if (shouldRetryJoin)
			{
				shouldRetryJoin = false;
				GTDev.Log("[CustomMapManager::OnDisconnected] Joining " + pendingNewPrivateRoomName + " failed previously, retrying once... ");
				PhotonNetworkController.Instance.AttemptToJoinSpecificRoomWithCallback(pendingNewPrivateRoomName, JoinType.Solo, OnJoinSpecificRoomResultFailureAllowed);
			}
			else
			{
				GTDev.Log("[CustomMapManager::OnDisconnected] Ending teleport...");
				EndTeleport(teleportSuccessful: true);
			}
		}
	}

	private static async Task AttemptAutoLogin()
	{
		GTDev.Log($"[CustomMapManager::AttemptAutoLogin] delayed end teleport coroutine == null : {delayedJoinCoroutine == null}");
		if (delayedEndTeleportCoroutine != null)
		{
			instance.StopCoroutine(delayedEndTeleportCoroutine);
		}
		delayedEndTeleportCoroutine = instance.StartCoroutine(DelayedEndTeleport());
		Error error = await ModIOManager.Initialize();
		if ((bool)error)
		{
			OnAutoLoginComplete(error);
			return;
		}
		ModIOManager.IsAuthenticated(sendEvents: true);
		OnAutoLoginComplete(Error.None);
	}

	private void OnJoinRoomFailed()
	{
		if (hasInstance && waitingForRoomJoin)
		{
			GTDev.Log("[CustomMapManager::OnJoinRoomFailed] Currently waiting for room join, resetting state, ending teleport...");
			waitingForRoomJoin = false;
			EndTeleport(teleportSuccessful: false);
		}
	}

	private static void EndTeleport(bool teleportSuccessful)
	{
		if (hasInstance)
		{
			if (delayedEndTeleportCoroutine != null)
			{
				instance.StopCoroutine(delayedEndTeleportCoroutine);
				delayedEndTeleportCoroutine = null;
			}
			if (delayedJoinCoroutine != null)
			{
				instance.StopCoroutine(delayedJoinCoroutine);
				delayedJoinCoroutine = null;
			}
		}
		DisableTeleportHUD();
		GorillaTagger.Instance.overrideNotInFocus = false;
		PrivateUIRoom.StopForcedOverlay(PrivateUIRoom.OverlaySource.CustomMap);
		currentTeleportCallback?.Invoke(teleportSuccessful);
		currentTeleportCallback = null;
		if (hasInstance && !GorillaComputer.instance.IsPlayerInVirtualStump())
		{
			GTDev.Log("[CustomMapManager::EndTeleport] Player is not in VStump, disabling VStump_Lobby GameObject");
			instance.gameObject.SetActive(value: false);
		}
		if (teleportSuccessful && GorillaComputer.instance.IsPlayerInVirtualStump())
		{
			TryAutoLoadMap();
		}
	}

	private static void TryAutoLoadMap()
	{
		ModId effectiveAutoLoadModId = GetEffectiveAutoLoadModId();
		if (effectiveAutoLoadModId == ModId.Null)
		{
			return;
		}
		bool flag = false;
		if (waitingForRoomJoin)
		{
			GTDev.Log("[CustomMapManager::TryAutoLoadMap] Still waiting for room join, delaying auto-load...");
			flag = true;
		}
		else if (NetworkSystem.Instance.InRoom && !NetworkSystem.Instance.IsMasterClient && VirtualStumpSerializer.IsWaitingForRoomInit())
		{
			GTDev.Log("[CustomMapManager::TryAutoLoadMap] Still waiting for room init, delaying auto-load...");
			flag = true;
		}
		if (flag)
		{
			delayedTryAutoLoadCoroutine = instance.StartCoroutine(DelayedTryAutoLoad());
			return;
		}
		GTDev.Log("[CustomMapManager::TryAutoLoadMap] Attempting auto-load...");
		GTMapLoadSource autoLoadSource = GetAutoLoadSource();
		if (!NetworkSystem.Instance.InRoom || (NetworkSystem.Instance.InRoom && NetworkSystem.Instance.IsMasterClient))
		{
			SetRoomMap(effectiveAutoLoadModId);
			LoadMap(effectiveAutoLoadModId, autoLoadSource);
		}
		else if (GetRoomMapId() == effectiveAutoLoadModId)
		{
			LoadMap(effectiveAutoLoadModId, autoLoadSource);
		}
	}

	private static GTMapLoadSource GetAutoLoadSource()
	{
		if (!activateHasAutoLoadOverride)
		{
			return GTMapLoadSource.teleporter;
		}
		return GTMapLoadSource.featured_hallway;
	}

	private static IEnumerator DelayedEndTeleport()
	{
		yield return new WaitForSecondsRealtime(instance.maxPostTeleportRoomProcessingTime);
		GTDev.Log("[CustomMapManager::DelayedEndTeleport] Timer expired, force ending teleport...");
		EndTeleport(teleportSuccessful: false);
	}

	private static IEnumerator DelayedTryAutoLoad()
	{
		while (waitingForRoomJoin || VirtualStumpSerializer.IsWaitingForRoomInit())
		{
			yield return new WaitForSeconds(0.1f);
		}
		GTDev.Log("[CustomMapManager::DelayedTryAutoLoad] Room Init finished, attempting auto-load...");
		ModId effectiveAutoLoadModId = GetEffectiveAutoLoadModId();
		GTMapLoadSource autoLoadSource = GetAutoLoadSource();
		if (!NetworkSystem.Instance.InRoom || (NetworkSystem.Instance.InRoom && NetworkSystem.Instance.IsMasterClient))
		{
			SetRoomMap(effectiveAutoLoadModId);
			LoadMap(effectiveAutoLoadModId, autoLoadSource);
		}
		else if (GetRoomMapId() == effectiveAutoLoadModId)
		{
			LoadMap(effectiveAutoLoadModId, autoLoadSource);
		}
	}

	private void OnJoinedRoom()
	{
		if (!hasInstance)
		{
			return;
		}
		if (pendingRoomChangeReloadModId != ModId.Null)
		{
			ModId modId = pendingRoomChangeReloadModId;
			pendingRoomChangeReloadModId = ModId.Null;
			if (!NetworkSystem.Instance.InRoom || NetworkSystem.Instance.IsMasterClient)
			{
				SetRoomMap(modId);
				LoadMap(modId, GTMapLoadSource.room_reload);
			}
		}
		if (waitingForRoomJoin)
		{
			waitingForRoomJoin = false;
			GTDev.Log("[CustomMapManager::OnJoinedRoom] Ending teleport...");
			EndTeleport(teleportSuccessful: true);
			if (lastUsedTeleporter.IsNotNull())
			{
				lastUsedTeleporter.PlayTeleportEffects(forLocalPlayer: true, toVStump: false, null, sendRPC: true);
			}
		}
	}

	public static bool UnloadMap(bool returnToSinglePlayerIfInPublic = true)
	{
		if (unloadInProgress)
		{
			return false;
		}
		if (!CustomMapLoader.IsMapLoaded() && !CustomMapLoader.IsLoading())
		{
			if (loadInProgress)
			{
				GTDev.Log("[CustomMapManager::UnloadMap] Map load is currently in progress... aborting...");
				abortModLoadIds.AddIfNew(loadingMapId);
				_ = waitingForModDownload;
				loadInProgress = false;
				loadingMapId = ModId.Null;
				waitingForModDownload = false;
				waitingForModInstall = false;
				waitingForModInstallId = ModId.Null;
				ClearRoomMap();
			}
			else
			{
				ClearRoomMap();
			}
			return false;
		}
		unloadInProgress = true;
		unloadingMapId = new ModId(CustomMapLoader.IsMapLoaded() ? ((long)CustomMapLoader.LoadedMapModId) : CustomMapLoader.GetLoadingMapModId());
		OnMapLoadProgress(MapLoadStatus.Unloading, 0, "");
		loadInProgress = false;
		loadingMapId = ModId.Null;
		waitingForModDownload = false;
		waitingForModInstall = false;
		waitingForModInstallId = ModId.Null;
		ClearRoomMap();
		CustomGameMode.LuaScript = "";
		if (CustomGameMode.gameScriptRunner != null)
		{
			CustomGameMode.StopScript();
		}
		customMapDefaultZoneShaderSettingsInitialized = false;
		customMapDefaultZoneShaderProperties = default(CMSZoneShaderSettings.CMSZoneShaderProperties);
		loadedCustomMapDefaultZoneShaderSettings = null;
		if (hasInstance)
		{
			instance.customMapDefaultZoneShaderSettings.CopySettings(instance.virtualStumpZoneShaderSettings);
			instance.virtualStumpZoneShaderSettings.BecomeActiveInstance();
			allCustomMapZoneShaderSettings.Clear();
		}
		CustomMapLoader.CloseDoorAndUnloadMap(OnMapUnloadCompleted);
		if (returnToSinglePlayerIfInPublic && NetworkSystem.Instance.InRoom && !NetworkSystem.Instance.SessionIsPrivate)
		{
			NetworkSystem.Instance.ReturnToSinglePlayer();
		}
		return true;
	}

	private static void OnMapUnloadCompleted()
	{
		unloadInProgress = false;
		currentLoadStatus = MapLoadStatus.None;
		currentLoadProgress = 0;
		currentLoadMessage = "";
		ResetModFileProgressTracking();
		OnMapUnloadComplete.Invoke();
		currentRoomMapModId = ModId.Null;
		currentRoomMapApproved = false;
		OnRoomMapChanged.Invoke(ModId.Null);
		if (exitVirtualStumpPending)
		{
			FinalizeExitVirtualStump();
		}
	}

	public static async Task LoadMap(ModId modId, GTMapLoadSource loadSource = GTMapLoadSource.none)
	{
		if (!hasInstance || loadInProgress)
		{
			return;
		}
		if (IsFeaturedMapLocked() && modId != FeaturedLockedMapId)
		{
			GTDev.LogWarning($"[CustomMapManager::LoadMap] Blocked map change to {modId} - Featured lobby " + $"is locked to {FeaturedLockedMapId}.");
			return;
		}
		if (abortModLoadIds.Contains(modId))
		{
			abortModLoadIds.Remove(modId);
		}
		if (CustomMapLoader.IsMapLoaded(modId))
		{
			return;
		}
		loadInProgress = true;
		loadingMapId = modId;
		pendingMapLoadSource = loadSource;
		waitingForModDownload = false;
		waitingForModInstall = false;
		waitingForModInstallId = ModId.Null;
		ResetModFileProgressTracking();
		BroadcastMapLoadProgress(MapLoadStatus.Loading, 0, "PREPARING MAP");
		_ = Error.None;
		var (error, mod) = await ModIOManager.GetMod(modId);
		if ((bool)error)
		{
			Debug.LogError("[CustomMapManager::LoadMap] Failed to get details for Mod with modID " + modId.ToString() + ", error: " + error.GetMessage());
			HandleMapLoadFailed("FAILED TO GET MAP DETAILS: " + error.GetMessage());
		}
		else if (mod.Creator == null)
		{
			loadInProgress = false;
			loadingMapId = ModId.Null;
		}
		else if (UGCPermissionManager.FeaturedMapsOnly && !ModIOManager.IsFeaturedMap(mod))
		{
			GTDev.Log("[CustomMapManager::LoadMap] Blocked loading non-featured map " + modId.ToString() + " ");
			HandleMapLoadFailed("THIS MAP IS NOT AVAILABLE FOR YOUR ACCOUNT");
		}
		else if (abortModLoadIds.Contains(modId))
		{
			GTDev.Log("[CustomMapManager::LoadMap] Aborting load...");
			abortModLoadIds.Remove(modId);
		}
		else
		{
			if (mod.File == null)
			{
				return;
			}
			switch (mod.File.State)
			{
			case ModFileState.None:
			case ModFileState.Queued:
			{
				GTDev.Log($"[CustomMapManager::LoadMap] Downloading mod {modId}...");
				waitingForModDownload = true;
				waitingForModInstall = true;
				waitingForModInstallId = mod.Id;
				BroadcastMapLoadProgress(MapLoadStatus.Downloading, 0, "WAITING FOR DOWNLOAD");
				bool flag = await ModIOManager.DownloadMod(modId);
				if (abortModLoadIds.Contains(modId))
				{
					GTDev.Log("[CustomMapManager::LoadMap] Aborting load...");
					abortModLoadIds.Remove(modId);
				}
				else if (!flag)
				{
					HandleMapLoadFailed("FAILED TO START MAP DOWNLOAD");
				}
				break;
			}
			case ModFileState.Downloading:
			case ModFileState.Updating:
				waitingForModDownload = true;
				waitingForModInstallId = modId;
				BroadcastModFileState(mod);
				break;
			case ModFileState.Downloaded:
			case ModFileState.Installing:
				waitingForModInstall = true;
				waitingForModInstallId = modId;
				BroadcastModFileState(mod);
				break;
			case ModFileState.Installed:
				instance.LoadInstalledMap(mod);
				break;
			case ModFileState.Uninstalling:
			case ModFileState.FileOperationFailed:
				Debug.LogError("[CustomMapManager::LoadMap] Failed to load map with modID " + modId.ToString() + ", error: " + mod.File.State);
				HandleMapLoadFailed("FAILED TO LOAD MAP: " + mod.File.State);
				break;
			}
		}
	}

	private async Task LoadInstalledMap(Mod installedMod)
	{
		waitingForModInstall = false;
		waitingForModInstallId = ModId.Null;
		CustomMapTelemetry.SetLoadingMapInfo(installedMod, pendingMapLoadSource);
		if (installedMod.File.State != ModFileState.Installed)
		{
			Debug.LogError("[CustomMapManager::LoadInstalledMap] Requested map is not installed!");
			HandleMapLoadFailed("MAP IS NOT INSTALLED");
			return;
		}
		if (ModIOManager.ValidateInstalledMod(installedMod) && !string.IsNullOrEmpty(installedMod.File.InstallLocation))
		{
			try
			{
				FileInfo[] files = new DirectoryInfo(installedMod.File.InstallLocation).GetFiles("package.json");
				if (files.Length == 0)
				{
					Debug.LogError("[CustomMapManager::LoadInstalledMap] Directory (" + installedMod.File.InstallLocation + ") for mod " + installedMod.Name + " does not contain a package.json file!");
					HandleMapLoadFailed("COULD NOT FIND PACKAGE.JSON IN MAP FILES");
				}
				else
				{
					GTDev.Log("[CustomMapManager::LoadInstalledMap] Loading map file: " + files[0].FullName);
					CustomMapLoader.LoadMap(installedMod.Id, files[0].FullName);
				}
				return;
			}
			catch (Exception arg)
			{
				Debug.LogError($"[CustomMapManager::LoadInstalledMap] Failed to load installed map: {arg}");
				HandleMapLoadFailed($"FAILED TO LOAD: {arg}");
				return;
			}
		}
		waitingForModDownload = true;
		waitingForModInstall = true;
		waitingForModInstallId = installedMod.Id;
		ResetModFileProgressTracking();
		BroadcastMapLoadProgress(MapLoadStatus.Downloading, 0, "WAITING FOR DOWNLOAD");
		bool flag = await ModIOManager.DownloadMod(installedMod.Id);
		if (abortModLoadIds.Contains(installedMod.Id))
		{
			GTDev.Log("[CustomMapManager::LoadInstalledMap] Aborting load...");
			abortModLoadIds.Remove(installedMod.Id);
		}
		else if (!flag)
		{
			HandleMapLoadFailed("FAILED TO START MAP DOWNLOAD");
		}
	}

	private static void OnMapLoadProgress(MapLoadStatus loadStatus, int progress, string message)
	{
		BroadcastMapLoadProgress(loadStatus, progress, message);
	}

	private static void BroadcastMapLoadProgress(MapLoadStatus loadStatus, int progress, string message)
	{
		currentLoadStatus = loadStatus;
		currentLoadProgress = progress;
		currentLoadMessage = message ?? "";
		OnMapLoadStatusChanged.Invoke(loadStatus, progress, message);
	}

	private static void OnMapLoadFinished(bool success)
	{
		loadInProgress = false;
		loadingMapId = ModId.Null;
		waitingForModDownload = false;
		waitingForModInstall = false;
		waitingForModInstallId = ModId.Null;
		currentLoadStatus = MapLoadStatus.None;
		currentLoadProgress = 0;
		currentLoadMessage = "";
		ResetModFileProgressTracking();
		if (success)
		{
			CustomMapTelemetry.OnMapLoadCompleted();
			CustomMapLoader.OpenDoorToMap();
			if (!CustomMapLoader.GetLuauGamemodeScript().IsNullOrEmpty())
			{
				CustomGameMode.LuaScript = CustomMapLoader.GetLuauGamemodeScript();
				if (CustomGameMode.LuaScript != "" && CustomGameMode.GameModeInitialized && CustomGameMode.gameScriptRunner == null)
				{
					CustomGameMode.LuaStart();
				}
			}
		}
		OnMapLoadComplete.Invoke(success);
	}

	private static void HandleMapLoadFailed(string message = null)
	{
		loadInProgress = false;
		loadingMapId = ModId.Null;
		waitingForModInstall = false;
		waitingForModInstallId = ModId.Null;
		pendingMapLoadSource = GTMapLoadSource.none;
		CustomMapTelemetry.ClearLoadingMapInfo();
		ResetModFileProgressTracking();
		BroadcastMapLoadProgress(MapLoadStatus.Error, 0, message ?? "UNKNOWN ERROR");
		OnMapLoadComplete.Invoke(arg0: false);
	}

	public static bool IsUnloading()
	{
		return unloadInProgress;
	}

	public static bool IsLoading()
	{
		return IsLoading(ModId.Null);
	}

	public static bool IsLoading(ModId modId)
	{
		if (!modId.IsValid())
		{
			if (!loadInProgress)
			{
				return CustomMapLoader.IsLoading();
			}
			return true;
		}
		if (loadInProgress)
		{
			return loadingMapId == modId;
		}
		return false;
	}

	public static ModId GetRoomMapId()
	{
		if (NetworkSystem.Instance.InRoom)
		{
			if (currentRoomMapModId == ModId.Null && NetworkSystem.Instance.IsMasterClient && CustomMapLoader.IsMapLoaded())
			{
				currentRoomMapModId = new ModId(CustomMapLoader.LoadedMapModId);
			}
			return currentRoomMapModId;
		}
		if (IsLoading())
		{
			return loadingMapId;
		}
		if (CustomMapLoader.IsMapLoaded())
		{
			return new ModId(CustomMapLoader.LoadedMapModId);
		}
		return ModId.Null;
	}

	public static void SetRoomMap(long modId)
	{
		if (hasInstance && modId != currentRoomMapModId._id)
		{
			if (IsFeaturedMapLocked() && modId != FeaturedLockedMapId._id)
			{
				GTDev.LogWarning($"[CustomMapManager::SetRoomMap] Blocked room-map change to {modId} - Featured " + $"lobby is locked to {FeaturedLockedMapId}.");
				return;
			}
			currentRoomMapModId = new ModId(modId);
			currentRoomMapApproved = false;
			OnRoomMapChanged.Invoke(currentRoomMapModId);
		}
	}

	public static void ClearRoomMap()
	{
		if (hasInstance && !currentRoomMapModId.Equals(ModId.Null) && !IsFeaturedMapLocked())
		{
			currentRoomMapModId = ModId.Null;
			currentRoomMapApproved = false;
			OnRoomMapChanged.Invoke(ModId.Null);
		}
	}

	public static bool CanLoadRoomMap()
	{
		if (currentRoomMapModId != ModId.Null)
		{
			return true;
		}
		return false;
	}

	public static void ApproveAndLoadRoomMap()
	{
		currentRoomMapApproved = true;
		CMSSerializer.ResetSyncedMapObjects();
		LoadMap(currentRoomMapModId, GTMapLoadSource.room_sync);
	}

	public static void RequestEnableTeleportHUD(bool enteringVirtualStump)
	{
		if (hasInstance)
		{
			instance.EnableTeleportHUD(enteringVirtualStump);
		}
	}

	private void EnableTeleportHUD(bool enteringVirtualStump)
	{
		if (teleportingHUD != null)
		{
			teleportingHUD.gameObject.SetActive(value: true);
			teleportingHUD.Initialize(enteringVirtualStump);
		}
		else
		{
			if (!(teleportingHUDPrefab != null))
			{
				return;
			}
			Camera main = Camera.main;
			if (!(main != null))
			{
				return;
			}
			GameObject gameObject = UnityEngine.Object.Instantiate(teleportingHUDPrefab, main.transform);
			if (gameObject != null)
			{
				teleportingHUD = gameObject.GetComponent<VirtualStumpTeleportingHUD>();
				if (teleportingHUD != null)
				{
					teleportingHUD.Initialize(enteringVirtualStump);
				}
			}
		}
	}

	public static void DisableTeleportHUD()
	{
		if (teleportingHUD != null)
		{
			teleportingHUD.gameObject.SetActive(value: false);
		}
	}

	public static void LoadZoneTriggered(int[] scenesToLoad, int[] scenesToUnload)
	{
		CustomMapLoader.LoadZoneTriggered(scenesToLoad, scenesToUnload, OnSceneLoaded, OnSceneUnloaded);
	}

	private static void OnSceneLoaded(string sceneName)
	{
		CMSSerializer.ProcessSceneLoad(sceneName);
		ProcessZoneShaderSettings(sceneName);
	}

	private static void OnSceneUnloaded(string sceneName)
	{
		CMSSerializer.UnregisterTriggers(sceneName);
		for (int num = allCustomMapZoneShaderSettings.Count - 1; num >= 0; num--)
		{
			if (allCustomMapZoneShaderSettings[num].IsNull())
			{
				allCustomMapZoneShaderSettings.RemoveAt(num);
			}
		}
	}

	private static void OnSceneTriggerHistoryProcessed(string sceneName)
	{
		CapsuleCollider bodyCollider = GTPlayer.Instance.bodyCollider;
		SphereCollider headCollider = GTPlayer.Instance.headCollider;
		Vector3 position = bodyCollider.transform.TransformPoint(bodyCollider.center);
		float radius = Mathf.Max(bodyCollider.height, bodyCollider.radius) * GTPlayer.Instance.scale;
		Collider[] array = new Collider[100];
		Physics.OverlapSphereNonAlloc(position, radius, array);
		foreach (Collider collider in array)
		{
			if (!(collider != null) || !collider.gameObject.scene.name.Equals(sceneName))
			{
				continue;
			}
			CMSTrigger[] components = collider.gameObject.GetComponents<CMSTrigger>();
			for (int j = 0; j < components.Length; j++)
			{
				if (components[j] != null)
				{
					components[j].OnTriggerEnter(bodyCollider);
					components[j].OnTriggerEnter(headCollider);
				}
			}
			CMSLoadingZone[] components2 = collider.gameObject.GetComponents<CMSLoadingZone>();
			for (int k = 0; k < components2.Length; k++)
			{
				if (components2[k] != null)
				{
					components2[k].OnTriggerEnter(bodyCollider);
				}
			}
			CMSZoneShaderSettingsTrigger[] components3 = collider.gameObject.GetComponents<CMSZoneShaderSettingsTrigger>();
			for (int l = 0; l < components3.Length; l++)
			{
				if (components3[l] != null)
				{
					components3[l].OnTriggerEnter(bodyCollider);
				}
			}
			HoverboardAreaTrigger[] components4 = collider.gameObject.GetComponents<HoverboardAreaTrigger>();
			for (int m = 0; m < components4.Length; m++)
			{
				if (components4[m] != null)
				{
					components4[m].OnTriggerEnter(headCollider);
				}
			}
			WaterVolume[] components5 = collider.gameObject.GetComponents<WaterVolume>();
			for (int n = 0; n < components5.Length; n++)
			{
				if (components5[n] != null)
				{
					components5[n].OnTriggerEnter(bodyCollider);
					components5[n].OnTriggerEnter(headCollider);
				}
			}
		}
	}

	public static void SetDefaultZoneShaderSettings(ZoneShaderSettings defaultCustomMapShaderSettings, CMSZoneShaderSettings.CMSZoneShaderProperties defaultZoneShaderProperties)
	{
		if (hasInstance)
		{
			instance.customMapDefaultZoneShaderSettings.CopySettings(defaultCustomMapShaderSettings, rerunAwake: true);
			loadedCustomMapDefaultZoneShaderSettings = defaultCustomMapShaderSettings;
			customMapDefaultZoneShaderProperties = defaultZoneShaderProperties;
			customMapDefaultZoneShaderSettingsInitialized = true;
		}
	}

	private static void ProcessZoneShaderSettings(string loadedSceneName)
	{
		if (hasInstance && customMapDefaultZoneShaderSettingsInitialized && customMapDefaultZoneShaderProperties.isInitialized)
		{
			for (int i = 0; i < allCustomMapZoneShaderSettings.Count; i++)
			{
				if (allCustomMapZoneShaderSettings[i].IsNotNull() && allCustomMapZoneShaderSettings[i] != loadedCustomMapDefaultZoneShaderSettings && allCustomMapZoneShaderSettings[i].gameObject.scene.name.Equals(loadedSceneName))
				{
					allCustomMapZoneShaderSettings[i].ReplaceDefaultValues(customMapDefaultZoneShaderProperties, rerunAwake: true);
				}
			}
		}
		else
		{
			if (!hasInstance || !instance.virtualStumpZoneShaderSettings.IsNotNull())
			{
				return;
			}
			for (int j = 0; j < allCustomMapZoneShaderSettings.Count; j++)
			{
				if (allCustomMapZoneShaderSettings[j].IsNotNull() && allCustomMapZoneShaderSettings[j].gameObject.scene.name.Equals(loadedSceneName))
				{
					allCustomMapZoneShaderSettings[j].ReplaceDefaultValues(instance.virtualStumpZoneShaderSettings, rerunAwake: true);
				}
			}
		}
	}

	public static void AddZoneShaderSettings(ZoneShaderSettings zoneShaderSettings)
	{
		allCustomMapZoneShaderSettings.AddIfNew(zoneShaderSettings);
	}

	public static void ActivateDefaultZoneShaderSettings()
	{
		if (hasInstance && customMapDefaultZoneShaderSettingsInitialized)
		{
			instance.customMapDefaultZoneShaderSettings.BecomeActiveInstance(force: true);
		}
		else if (hasInstance)
		{
			instance.virtualStumpZoneShaderSettings.BecomeActiveInstance(force: true);
		}
	}

	public static void ReturnToVirtualStump()
	{
		if (hasInstance && GorillaComputer.instance.IsPlayerInVirtualStump() && instance.returnToVirtualStumpTeleportLocation.IsNotNull())
		{
			GTPlayer gTPlayer = GTPlayer.Instance;
			if (gTPlayer != null)
			{
				CustomMapLoader.ResetToInitialZone(OnSceneLoaded, OnSceneUnloaded);
				gTPlayer.TeleportTo(instance.returnToVirtualStumpTeleportLocation, matchDestinationRotation: true, maintainVelocity: false);
			}
		}
	}

	public static bool WantsHoldingHandsDisabled()
	{
		if (GorillaComputer.instance.IsPlayerInVirtualStump())
		{
			if (!CustomMapLoader.IsMapLoaded())
			{
				return true;
			}
			if (CustomMapLoader.LoadedMapWantsHoldingHandsDisabled())
			{
				return true;
			}
		}
		return false;
	}
}

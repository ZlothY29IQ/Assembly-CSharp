using System;
using System.Collections.Generic;
using GorillaNetworking;
using GorillaTagScripts.Subscription.AlarmClocks;
using GorillaTagScripts.VirtualStumpCustomMaps;
using GorillaTagScripts.VirtualStumpCustomMaps.UI;
using Modio;
using Modio.Mods;
using Modio.Users;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class CustomMapsDevTerminal : CustomMapsTerminalScreen
{
	[Tooltip("Everything visible (monitor, text, buttons). Turned on only when the player may use the terminal.")]
	[SerializeField]
	private GameObject contentRoot;

	[Header("Browse screen (gallery of the player's maps)")]
	[SerializeField]
	private GameObject browseScreen;

	[SerializeField]
	private CustomMapsGalleryView mapGallery;

	[SerializeField]
	private TMP_Text titleText;

	[Tooltip("Watch mode and Return To Position state.")]
	[SerializeField]
	private TMP_Text watchText;

	[SerializeField]
	private TMP_Text pageText;

	[SerializeField]
	private TMP_Text loadingText;

	[Header("Details screen (one map, opened from a gallery tile)")]
	[SerializeField]
	private GameObject detailsScreen;

	[SerializeField]
	private SpriteRenderer detailsThumbnail;

	[SerializeField]
	private TMP_Text detailsNameText;

	[SerializeField]
	private TMP_Text detailsCreatorText;

	[SerializeField]
	private TMP_Text detailsDescriptionText;

	[SerializeField]
	private TMP_Text detailsStatusText;

	[Tooltip("Watch mode state for this map.")]
	[SerializeField]
	private TMP_Text detailsWatchText;

	[Tooltip("Load progress and messages; replaces the description while shown.")]
	[SerializeField]
	private TMP_Text detailsMessageText;

	[SerializeField]
	private TMP_Text detailsPlayerCountText;

	[SerializeField]
	private CustomMapsScreenButton loadButton;

	[SerializeField]
	private CustomMapsScreenButton watchButton;

	[SerializeField]
	private CustomMapsScreenButton returnToPositionButton;

	[Header("In-room screen (shown instead of the browser while in any networked room)")]
	[Tooltip("Shown when the player is in a room; the dev terminal is singleplayer-only. Disconnecting back to singleplayer is handled by the existing world path, not this screen.")]
	[SerializeField]
	private GameObject inRoomScreen;

	[Tooltip("Minimum seconds between refreshes from the refresh button; the list crawls every page of /me/mods.")]
	[SerializeField]
	private float refreshCooldown = 5f;

	[Header("Watch mode events")]
	[SerializeField]
	private UnityEvent<long> onWatchStarted = new UnityEvent<long>();

	[SerializeField]
	private UnityEvent<long> onWatchStopped = new UnityEvent<long>();

	[SerializeField]
	private UnityEvent<bool> onReturnToPositionChanged = new UnityEvent<bool>();

	private const int MaxMapNameLength = 25;

	private readonly List<Mod> ownMaps = new List<Mod>();

	private readonly List<Mod> pageMaps = new List<Mod>();

	private int selectedIndex;

	private int currentPage;

	private int displayedPage = -1;

	private bool showingDetails;

	private bool showingInRoom;

	private ModId detailsLogoMapId = ModId.Null;

	private Sprite defaultThumbnail;

	private const float LogoWidth = 320f;

	private const float LogoHeight = 180f;

	private int refreshRequestId;

	private bool isRefreshing;

	private bool isVisible;

	private float lastManualRefreshTime = float.MinValue;

	private string statusMessage = "";

	private const string StumpAlarmClockKey = "virtualStumpCustom";

	private int MapsPerPage
	{
		get
		{
			if (!(mapGallery != null))
			{
				return 1;
			}
			return Mathf.Max(1, mapGallery.TileCount);
		}
	}

	private int PageCount => Mathf.Max(1, Mathf.CeilToInt((float)ownMaps.Count / (float)MapsPerPage));

	public override void Initialize()
	{
	}

	private void Awake()
	{
		if (detailsThumbnail != null)
		{
			defaultThumbnail = detailsThumbnail.sprite;
		}
		if (detailsWatchText != null)
		{
			detailsWatchText.overflowMode = TextOverflowModes.Overflow;
		}
		if (watchText != null)
		{
			watchText.overflowMode = TextOverflowModes.Overflow;
		}
		ShowBrowseScreen();
		SetVisible(visible: false);
		CustomMapDevWatchMode.OnWatchStarted.AddListener(ForwardWatchStarted);
		CustomMapDevWatchMode.OnWatchStopped.AddListener(ForwardWatchStopped);
		CustomMapDevWatchMode.OnReturnToPositionChanged.AddListener(ForwardReturnToPositionChanged);
	}

	private void OnDestroy()
	{
		CustomMapDevWatchMode.OnWatchStarted.RemoveListener(ForwardWatchStarted);
		CustomMapDevWatchMode.OnWatchStopped.RemoveListener(ForwardWatchStopped);
		CustomMapDevWatchMode.OnReturnToPositionChanged.RemoveListener(ForwardReturnToPositionChanged);
	}

	private void ForwardWatchStarted(ModId modId)
	{
		onWatchStarted?.Invoke(modId._id);
	}

	private void ForwardWatchStopped(ModId modId)
	{
		onWatchStopped?.Invoke(modId._id);
	}

	private void ForwardReturnToPositionChanged(bool _enabled)
	{
		onReturnToPositionChanged?.Invoke(_enabled);
	}

	private void OnEnable()
	{
		AlarmClockManager.OnActiveKeyChanged += HandleActiveAlarmChanged;
		CustomMapManager.OnMapLoadStatusChanged.AddListener(OnMapLoadStatusChanged);
		CustomMapManager.OnMapLoadComplete.AddListener(OnMapLoadComplete);
		CustomMapManager.OnMapUnloadComplete.AddListener(OnMapUnloadComplete);
		CustomMapManager.OnRoomMapChanged.AddListener(OnRoomMapChanged);
		ModIOManager.OnModIOLoggedIn.AddListener(OnModIOLoggedIn);
		ModIOManager.OnModIOLoggedOut.AddListener(OnModIOLoggedOut);
		ModIOManager.OnModIOUserChanged.AddListener(OnModIOUserChanged);
		CustomMapDevWatchMode.OnWatchStarted.AddListener(OnWatchChanged);
		CustomMapDevWatchMode.OnWatchStopped.AddListener(OnWatchChanged);
		CustomMapDevWatchMode.OnReturnToPositionChanged.AddListener(OnReturnToPositionChanged);
		NetworkSystem.Instance.OnJoinedRoomEvent += new Action(OnRoomStateChanged);
		NetworkSystem.Instance.OnReturnedToSinglePlayer += new Action(OnRoomStateChanged);
		RefreshAvailability(forceRefresh: false);
	}

	private void OnDisable()
	{
		AlarmClockManager.OnActiveKeyChanged -= HandleActiveAlarmChanged;
		CustomMapManager.OnMapLoadStatusChanged.RemoveListener(OnMapLoadStatusChanged);
		CustomMapManager.OnMapLoadComplete.RemoveListener(OnMapLoadComplete);
		CustomMapManager.OnMapUnloadComplete.RemoveListener(OnMapUnloadComplete);
		CustomMapManager.OnRoomMapChanged.RemoveListener(OnRoomMapChanged);
		ModIOManager.OnModIOLoggedIn.RemoveListener(OnModIOLoggedIn);
		ModIOManager.OnModIOLoggedOut.RemoveListener(OnModIOLoggedOut);
		ModIOManager.OnModIOUserChanged.RemoveListener(OnModIOUserChanged);
		CustomMapDevWatchMode.OnWatchStarted.RemoveListener(OnWatchChanged);
		CustomMapDevWatchMode.OnWatchStopped.RemoveListener(OnWatchChanged);
		CustomMapDevWatchMode.OnReturnToPositionChanged.RemoveListener(OnReturnToPositionChanged);
		NetworkSystem.Instance.OnJoinedRoomEvent -= new Action(OnRoomStateChanged);
		NetworkSystem.Instance.OnReturnedToSinglePlayer -= new Action(OnRoomStateChanged);
		CustomMapNetworkJoinTrigger.SuppressJoins = false;
		refreshRequestId++;
		isRefreshing = false;
		SetVisible(visible: false);
	}

	private static bool MeetsEntryRequirements()
	{
		if (IsStumpAlarmSet())
		{
			return IsLinkedToModIO();
		}
		return false;
	}

	private static bool IsStumpAlarmSet()
	{
		if (AlarmClockManager.Instance != null)
		{
			return AlarmClockManager.Instance.ActiveKey == "virtualStumpCustom";
		}
		return false;
	}

	private static bool IsLinkedToModIO()
	{
		if (ModIOManager.IsLoggedIn() && !ModIOManager.IsLoggingOut())
		{
			return ModIOManager.GetLastAuthMethod() == ModIOManager.ModIOAuthMethod.LinkedAccount;
		}
		return false;
	}

	private async void RefreshAvailability(bool forceRefresh)
	{
		int requestId = ++refreshRequestId;
		if (!MeetsEntryRequirements())
		{
			ownMaps.Clear();
			isRefreshing = false;
			SetVisible(visible: false);
			CustomMapDevWatchMode.StopWatching();
			return;
		}
		if (IsInRoom())
		{
			CustomMapDevWatchMode.StopWatching();
			isRefreshing = false;
			SetVisible(visible: true);
			ShowInRoomScreen();
			return;
		}
		if (showingInRoom)
		{
			ShowBrowseScreen();
		}
		isRefreshing = true;
		Redraw();
		var (error, collection) = await ModIOManager.GetCurrentUserMaps(forceRefresh);
		if (this == null || requestId != refreshRequestId)
		{
			return;
		}
		isRefreshing = false;
		if (!MeetsEntryRequirements())
		{
			ownMaps.Clear();
			SetVisible(visible: false);
			CustomMapDevWatchMode.StopWatching();
			return;
		}
		if ((bool)error)
		{
			if (isVisible)
			{
				statusMessage = "COULDN'T REFRESH YOUR MAPS: " + error.GetMessage();
				Redraw();
			}
			return;
		}
		ModId selectedMapId = GetSelectedMapId();
		ownMaps.Clear();
		ownMaps.AddRange(collection);
		ownMaps.Sort((Mod a, Mod b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
		selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, ownMaps.Count - 1));
		for (int num = 0; num < ownMaps.Count; num++)
		{
			if (ownMaps[num].Id == selectedMapId)
			{
				selectedIndex = num;
				break;
			}
		}
		currentPage = Mathf.Clamp(currentPage, 0, PageCount - 1);
		if (showingDetails && !ContainsMap(selectedMapId))
		{
			ShowBrowseScreen();
		}
		detailsLogoMapId = ModId.Null;
		if (CustomMapDevWatchMode.IsWatching && !ContainsMap(CustomMapDevWatchMode.WatchedMapId))
		{
			CustomMapDevWatchMode.StopWatching();
		}
		statusMessage = "";
		SetVisible(ownMaps.Count > 0);
		displayedPage = -1;
		Redraw();
	}

	private void SetVisible(bool visible)
	{
		isVisible = visible;
		if (contentRoot != null && contentRoot.activeSelf != visible)
		{
			contentRoot.SetActive(visible);
		}
		if (visible)
		{
			showTime = Time.time;
		}
		else
		{
			displayedPage = -1;
		}
	}

	public override void PressButton(CustomMapKeyboardBinding pressedButton)
	{
		if (!isVisible || Time.time < showTime + activationTime || showingInRoom)
		{
			return;
		}
		if (pressedButton == CustomMapKeyboardBinding.option3)
		{
			ManualRefresh();
			return;
		}
		if (!showingDetails)
		{
			if (CustomMapsListScreen.TryGetTileIndex(pressedButton, out var tileIndex))
			{
				OpenDetailsForTile(tileIndex);
				return;
			}
			switch (pressedButton)
			{
			case CustomMapKeyboardBinding.up:
				ChangePage(-1);
				break;
			case CustomMapKeyboardBinding.down:
				ChangePage(1);
				break;
			}
			return;
		}
		switch (pressedButton)
		{
		case CustomMapKeyboardBinding.goback:
			ShowBrowseScreen();
			statusMessage = "";
			Redraw();
			break;
		case CustomMapKeyboardBinding.enter:
			LoadOrUnload();
			break;
		case CustomMapKeyboardBinding.option1:
			ToggleWatch();
			break;
		case CustomMapKeyboardBinding.option2:
			CustomMapDevWatchMode.SetReturnToPosition(!CustomMapDevWatchMode.ReturnToPosition);
			break;
		}
	}

	private void OpenDetailsForTile(int tileIndex)
	{
		int num = currentPage * MapsPerPage + tileIndex;
		if (tileIndex >= 0 && num < ownMaps.Count)
		{
			selectedIndex = num;
			statusMessage = "";
			ShowDetailsScreen();
			Redraw();
		}
	}

	private void ShowBrowseScreen()
	{
		showingDetails = false;
		showingInRoom = false;
		if (inRoomScreen != null)
		{
			inRoomScreen.SetActive(value: false);
		}
		if (detailsScreen != null)
		{
			detailsScreen.SetActive(value: false);
		}
		if (browseScreen != null)
		{
			browseScreen.SetActive(value: true);
		}
	}

	private void ShowDetailsScreen()
	{
		showingDetails = true;
		showingInRoom = false;
		if (inRoomScreen != null)
		{
			inRoomScreen.SetActive(value: false);
		}
		if (browseScreen != null)
		{
			browseScreen.SetActive(value: false);
		}
		if (detailsScreen != null)
		{
			detailsScreen.SetActive(value: true);
		}
		showTime = Time.time;
	}

	private void ShowInRoomScreen()
	{
		showingDetails = false;
		showingInRoom = true;
		if (browseScreen != null)
		{
			browseScreen.SetActive(value: false);
		}
		if (detailsScreen != null)
		{
			detailsScreen.SetActive(value: false);
		}
		if (inRoomScreen != null)
		{
			inRoomScreen.SetActive(value: true);
		}
		showTime = Time.time;
	}

	private static bool IsInRoom()
	{
		if (NetworkSystem.Instance != null)
		{
			return NetworkSystem.Instance.InRoom;
		}
		return false;
	}

	private void OnRoomStateChanged()
	{
		if (base.isActiveAndEnabled)
		{
			RefreshAvailability(forceRefresh: false);
		}
	}

	private void ChangePage(int delta)
	{
		int pageCount = PageCount;
		if (pageCount > 1)
		{
			currentPage = (currentPage + delta + pageCount) % pageCount;
			statusMessage = "";
			Redraw();
		}
	}

	private void LoadOrUnload()
	{
		if (!CanChangeMap(out var reason))
		{
			statusMessage = reason;
			Redraw();
			return;
		}
		if (CustomMapLoader.IsMapLoaded() || CustomMapManager.IsLoading())
		{
			VirtualStumpSerializer.Instance.UnloadMapSynced();
			statusMessage = "UNLOADING MAP...";
			Redraw();
			return;
		}
		Mod selectedMap = GetSelectedMap();
		if (selectedMap != null)
		{
			if (NetworkSystem.Instance.InRoom && !NetworkSystem.Instance.SessionIsPrivate)
			{
				NetworkSystem.Instance.ReturnToSinglePlayer();
			}
			CustomMapNetworkJoinTrigger.SuppressJoins = true;
			VirtualStumpSerializer.Instance.LoadMapSynced(selectedMap.Id, GTMapLoadSource.dev_terminal);
			statusMessage = "LOADING " + FormatMapName(selectedMap) + "...";
			Redraw();
		}
	}

	private bool CanChangeMap(out string reason)
	{
		reason = "";
		if (VirtualStumpSerializer.Instance == null)
		{
			reason = "MAP LOADING IS NOT AVAILABLE RIGHT NOW";
			return false;
		}
		if (CustomMapManager.IsUnloading())
		{
			reason = "WAIT FOR THE CURRENT MAP TO FINISH UNLOADING";
			return false;
		}
		if (!CustomMapsTerminal.IsLocalOnly && CustomMapsTerminal.IsLocked() && !CustomMapsTerminal.IsDriver)
		{
			reason = "THE MAP TERMINAL IS IN USE BY " + CustomMapsTerminal.GetDriverNickname();
			return false;
		}
		return true;
	}

	private void ToggleWatch()
	{
		Mod selectedMap = GetSelectedMap();
		if (selectedMap != null)
		{
			if (CustomMapDevWatchMode.IsWatchingMap(selectedMap.Id))
			{
				CustomMapDevWatchMode.StopWatching();
				statusMessage = "WATCH MODE OFF";
			}
			else
			{
				CustomMapDevWatchMode.StartWatching(selectedMap.Id);
				statusMessage = "WATCHING " + FormatMapName(selectedMap);
			}
			Redraw();
		}
	}

	private void ManualRefresh()
	{
		if (!isRefreshing)
		{
			if (Time.time < lastManualRefreshTime + refreshCooldown)
			{
				statusMessage = "PLEASE WAIT BEFORE REFRESHING AGAIN";
				Redraw();
			}
			else
			{
				lastManualRefreshTime = Time.time;
				RefreshAvailability(forceRefresh: true);
			}
		}
	}

	private void Redraw()
	{
		if (isVisible && !showingInRoom)
		{
			if (showingDetails)
			{
				RedrawDetails();
			}
			else
			{
				RedrawBrowse();
			}
		}
	}

	private void RedrawBrowse()
	{
		if (displayedPage != currentPage)
		{
			ShowCurrentPage();
		}
		if (titleText != null)
		{
			titleText.text = "MY MAPS - " + ModIOManager.GetCurrentUsername().ToUpperInvariant();
		}
		if (watchText != null)
		{
			watchText.text = BuildWatchText();
		}
		if (pageText != null)
		{
			pageText.text = BuildPageText();
		}
		if (loadingText != null)
		{
			bool active = isRefreshing && ownMaps.Count == 0;
			loadingText.gameObject.SetActive(active);
		}
	}

	private void RedrawDetails()
	{
		Mod selectedMap = GetSelectedMap();
		if (selectedMap == null)
		{
			ShowBrowseScreen();
			RedrawBrowse();
			return;
		}
		if (detailsLogoMapId != selectedMap.Id)
		{
			RequestDetailsLogoAndPlayerCount(selectedMap);
		}
		bool flag = CustomMapDevWatchMode.IsWatchingMap(selectedMap.Id);
		string text = (CustomMapDevWatchMode.ReturnToPosition ? "ON" : "OFF");
		SetText(detailsNameText, FormatMapName(selectedMap));
		SetText(detailsCreatorText, (selectedMap.Creator != null) ? selectedMap.Creator.Username : "");
		SetText(detailsStatusText, BuildMapStatus(selectedMap));
		SetText(detailsWatchText, flag ? ("WATCHING\nRETURN TO POS: " + text) : ("NOT WATCHING\nRETURN TO POS: " + text));
		string value = BuildProgressText();
		bool flag2 = !string.IsNullOrEmpty(value);
		SetText(detailsMessageText, value);
		if (detailsMessageText != null)
		{
			detailsMessageText.gameObject.SetActive(flag2);
		}
		SetText(detailsDescriptionText, selectedMap.Description ?? "");
		if (detailsDescriptionText != null)
		{
			detailsDescriptionText.gameObject.SetActive(!flag2);
		}
		if (loadButton != null)
		{
			loadButton.SetButtonText((CustomMapLoader.IsMapLoaded() || CustomMapManager.IsLoading()) ? "UNLOAD" : "LOAD");
		}
		if (watchButton != null)
		{
			watchButton.SetButtonText(flag ? "STOP WATCH" : "WATCH");
			watchButton.SetButtonActive(flag);
		}
		if (returnToPositionButton != null)
		{
			returnToPositionButton.SetButtonText("RETURN TO POSITION");
			returnToPositionButton.SetButtonActive(CustomMapDevWatchMode.ReturnToPosition);
		}
	}

	private async void RequestDetailsLogoAndPlayerCount(Mod map)
	{
		detailsLogoMapId = map.Id;
		if (detailsPlayerCountText != null)
		{
			detailsPlayerCountText.text = "-";
			PlayerCountHelper.GetPlayerCount(map, delegate(string count)
			{
				if (this != null && detailsLogoMapId == map.Id && detailsPlayerCountText != null)
				{
					detailsPlayerCountText.text = count;
				}
			});
		}
		if (!(detailsThumbnail == null))
		{
			detailsThumbnail.sprite = defaultThumbnail;
			var (error, texture2D) = await ModIOManager.GetModLogo(map, null);
			if (!(this == null) && !(detailsLogoMapId != map.Id) && !error && !(texture2D == null))
			{
				detailsThumbnail.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, 320f, 180f), new Vector2(0.5f, 0.5f));
			}
		}
	}

	private void ShowCurrentPage()
	{
		displayedPage = currentPage;
		if (!(mapGallery == null))
		{
			pageMaps.Clear();
			int num = currentPage * MapsPerPage;
			int num2 = Mathf.Min(num + MapsPerPage, ownMaps.Count);
			for (int i = num; i < num2; i++)
			{
				pageMaps.Add(ownMaps[i]);
			}
			mapGallery.ResetGallery();
			if (!mapGallery.DisplayGallery(pageMaps, useMapName: true, out var error))
			{
				statusMessage = error;
			}
		}
	}

	private string BuildWatchText()
	{
		string text = (CustomMapDevWatchMode.ReturnToPosition ? "ON" : "OFF");
		if (!CustomMapDevWatchMode.IsWatching)
		{
			return "WATCH: OFF\nRETURN TO POS: " + text;
		}
		Mod mod = FindMap(CustomMapDevWatchMode.WatchedMapId);
		string text2 = ((mod != null) ? FormatMapName(mod) : CustomMapDevWatchMode.WatchedMapId.ToString());
		return "WATCHING: " + text2 + "\nRETURN TO POS: " + text;
	}

	private string BuildPageText()
	{
		return $"PAGE {currentPage + 1}/{PageCount}";
	}

	private string BuildProgressText()
	{
		if (!string.IsNullOrEmpty(statusMessage))
		{
			return statusMessage;
		}
		if (isRefreshing)
		{
			return "REFRESHING YOUR MAPS...";
		}
		switch (CustomMapManager.CurrentLoadStatus)
		{
		case MapLoadStatus.Downloading:
		case MapLoadStatus.Loading:
		case MapLoadStatus.Installing:
			return CustomMapManager.CurrentLoadStatus.ToString().ToUpperInvariant() + ": " + $"{CustomMapManager.CurrentLoadProgress}%";
		case MapLoadStatus.Unloading:
			return "UNLOADING...";
		case MapLoadStatus.Error:
			return CustomMapManager.CurrentLoadMessage;
		default:
			return "";
		}
	}

	private static string BuildMapStatus(Mod map)
	{
		if (CustomMapLoader.IsMapLoaded(map.Id))
		{
			return "LOADED";
		}
		if (CustomMapManager.IsLoading(map.Id))
		{
			return "LOADING";
		}
		if (map.File == null)
		{
			return "NOT DOWNLOADED";
		}
		switch (map.File.State)
		{
		case ModFileState.Installed:
			return "DOWNLOADED";
		case ModFileState.Queued:
			if (!ModInstallationManagement.DoesModNeedUpdate(map))
			{
				return "NOT DOWNLOADED";
			}
			return "NEEDS UPDATE";
		case ModFileState.Downloading:
		case ModFileState.Updating:
			return "DOWNLOADING";
		case ModFileState.Downloaded:
		case ModFileState.Installing:
			return "INSTALLING";
		case ModFileState.Uninstalling:
			return "UNINSTALLING";
		case ModFileState.FileOperationFailed:
			return "DOWNLOAD FAILED";
		default:
			return "NOT DOWNLOADED";
		}
	}

	private static void SetText(TMP_Text text, string value)
	{
		if (text != null)
		{
			text.text = value;
		}
	}

	private static string FormatMapName(Mod map)
	{
		string text = (string.IsNullOrEmpty(map.Name) ? map.Id.ToString() : map.Name);
		if (text.Length > 25)
		{
			text = text.Substring(0, 22) + "...";
		}
		return text.Replace("<", "").Replace(">", "").ToUpperInvariant();
	}

	private Mod GetSelectedMap()
	{
		if (selectedIndex < 0 || selectedIndex >= ownMaps.Count)
		{
			return null;
		}
		return ownMaps[selectedIndex];
	}

	private ModId GetSelectedMapId()
	{
		return GetSelectedMap()?.Id ?? ModId.Null;
	}

	private Mod FindMap(ModId modId)
	{
		foreach (Mod ownMap in ownMaps)
		{
			if (ownMap.Id == modId)
			{
				return ownMap;
			}
		}
		return null;
	}

	private bool ContainsMap(ModId modId)
	{
		return FindMap(modId) != null;
	}

	private void HandleActiveAlarmChanged(string activeKey)
	{
		RefreshAvailability(forceRefresh: false);
	}

	private void OnModIOLoggedIn()
	{
		RefreshAvailability(forceRefresh: false);
	}

	private void OnModIOLoggedOut()
	{
		RefreshAvailability(forceRefresh: false);
	}

	private void OnModIOUserChanged(User user)
	{
		RefreshAvailability(forceRefresh: false);
	}

	private void OnMapLoadStatusChanged(MapLoadStatus status, int progress, string message)
	{
		statusMessage = "";
		Redraw();
	}

	private void OnMapLoadComplete(bool success)
	{
		statusMessage = "";
		Redraw();
	}

	private void OnMapUnloadComplete()
	{
		statusMessage = "";
		Redraw();
	}

	private void OnRoomMapChanged(ModId roomMapId)
	{
		Redraw();
	}

	private void OnWatchChanged(ModId modId)
	{
		Redraw();
	}

	private void OnReturnToPositionChanged(bool enabled)
	{
		Redraw();
	}
}

using System;
using System.Collections.Generic;
using System.Text;
using GorillaNetworking;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

namespace GorillaTagScripts.Builder;

public class SharedBlocksTerminal : MonoBehaviour
{
	public enum ScreenType
	{
		NO_DRIVER,
		SEARCH,
		LOADING,
		ERROR,
		SCAN_INFO,
		OTHER_DRIVER
	}

	public enum TerminalState
	{
		NoStatus,
		Searching,
		NotFound,
		Found,
		Loading,
		LoadSuccess,
		LoadFail
	}

	public class SharedBlocksTerminalState
	{
		public ScreenType currentScreen;

		public TerminalState state;

		public int driverID;
	}

	public const string SHARE_BLOCKS_TERMINAL_PROMPT_KEY = "SHARE_BLOCKS_TERMINAL_PROMPT";

	public const string SHARE_BLOCKS_TERMINAL_CONTROL_BUTTON_KEY = "SHARE_BLOCKS_TERMINAL_CONTROL_BUTTON";

	public const string SHARE_BLOCKS_TERMINAL_CONTROL_BUTTON_AVAILABLE_KEY = "SHARE_BLOCKS_TERMINAL_CONTROL_BUTTON_AVAILABLE";

	public const string SHARE_BLOCKS_TERMINAL_CONTROL_BUTTON_LOCKED_KEY = "SHARE_BLOCKS_TERMINAL_CONTROL_BUTTON_LOCKED";

	public const string SHARE_BLOCKS_TERMINAL_STATUS_SEARCH_KEY = "SHARE_BLOCKS_TERMINAL_STATUS_SEARCH";

	public const string SHARE_BLOCKS_TERMINAL_STATUS_MAP_FOUND_KEY = "SHARE_BLOCKS_TERMINAL_STATUS_MAP_FOUND";

	public const string SHARE_BLOCKS_TERMINAL_STATUS_MAP_NOT_FOUND_KEY = "SHARE_BLOCKS_TERMINAL_STATUS_MAP_NOT_FOUND";

	public const string SHARE_BLOCKS_TERMINAL_STATUS_LOADING_KEY = "SHARE_BLOCKS_TERMINAL_STATUS_LOADING";

	public const string SHARE_BLOCKS_TERMINAL_STATUS_LOAD_SUCCESS_KEY = "SHARE_BLOCKS_TERMINAL_STATUS_LOAD_SUCCESS";

	public const string SHARE_BLOCKS_TERMINAL_STATUS_LOAD_FAILED_KEY = "SHARE_BLOCKS_TERMINAL_STATUS_LOAD_FAILED";

	public const string SHARE_BLOCKS_TERMINAL_STATUS_NOT_CONTROLLER_KEY = "SHARE_BLOCKS_TERMINAL_STATUS_NOT_CONTROLLER";

	public const string SHARE_BLOCKS_TERMINAL_STATUS_NO_SELECTION_KEY = "SHARE_BLOCKS_TERMINAL_STATUS_NO_SELECTION";

	public const string SHARE_BLOCKS_TERMINAL_STATUS_IN_PROGRESS_KEY = "SHARE_BLOCKS_TERMINAL_STATUS_IN_PROGRESS";

	public const string SHARE_BLOCKS_TERMINAL_STATUS_WAIT_KEY = "SHARE_BLOCKS_TERMINAL_STATUS_WAIT";

	public const string SHARE_BLOCKS_TERMINAL_STATUS_DISALLOWED_LOBBY_LOAD_KEY = "SHARE_BLOCKS_TERMINAL_STATUS_DISALLOWED_LOBBY_LOAD";

	public const string SHARE_BLOCKS_TERMINAL_STATUS_DISALLOWED_LOBBY_UNLOAD_KEY = "SHARE_BLOCKS_TERMINAL_STATUS_DISALLOWED_LOBBY_UNLOAD";

	public const string SHARE_BLOCKS_TERMINAL_STATUS_DISALLOWED_ROOM_LOAD_KEY = "SHARE_BLOCKS_TERMINAL_STATUS_DISALLOWED_ROOM_LOAD";

	public const string SHARE_BLOCKS_TERMINAL_STATUS_DISALLOWED_ROOM_UNLOAD_KEY = "SHARE_BLOCKS_TERMINAL_STATUS_DISALLOWED_ROOM_UNLOAD";

	public const string SHARE_BLOCKS_TERMINAL_SEARCH_LOADED_LABEL_KEY = "SHARE_BLOCKS_TERMINAL_SEARCH_LOADED_LABEL";

	public const string SHARE_BLOCKS_TERMINAL_SEARCH_LOADED_NONE_KEY = "SHARE_BLOCKS_TERMINAL_SEARCH_LOADED_NONE";

	public const string SHARE_BLOCKS_TERMINAL_SEARCH_MAP_SEARCH_KEY = "SHARE_BLOCKS_TERMINAL_SEARCH_MAP_SEARCH";

	public const string SHARE_BLOCKS_TERMINAL_SEARCH_VOTES_KEY = "SHARE_BLOCKS_TERMINAL_SEARCH_VOTES";

	public const string SHARE_BLOCKS_TERMINAL_SEARCH_MAPS_LABEL_KEY = "SHARE_BLOCKS_TERMINAL_SEARCH_MAPS_LABEL";

	public const string SHARE_BLOCKS_TERMINAL_SEARCH_LOBBY_TEXT_KEY = "SHARE_BLOCKS_TERMINAL_SEARCH_LOBBY_TEXT";

	public const string SHARE_BLOCKS_TERMINAL_ERROR_TITLE_KEY = "SHARE_BLOCKS_TERMINAL_ERROR_TITLE";

	public const string SHARE_BLOCKS_TERMINAL_ERROR_INSTRUCTIONS_KEY = "SHARE_BLOCKS_TERMINAL_ERROR_INSTRUCTIONS";

	public const string SHARE_BLOCKS_TERMINAL_ERROR_BACK_KEY = "SHARE_BLOCKS_TERMINAL_ERROR_BACK";

	public const string SHARE_BLOCKS_TERMINAL_INFO_TITLE_KEY = "SHARE_BLOCKS_TERMINAL_INFO_TITLE";

	public const string SHARE_BLOCKS_TERMINAL_INFO_DATA_KEY = "SHARE_BLOCKS_TERMINAL_INFO_DATA";

	public const string SHARE_BLOCKS_TERMINAL_INFO_ENTER_KEY = "SHARE_BLOCKS_TERMINAL_INFO_ENTER";

	public const string SHARE_BLOCKS_TERMINAL_OTHER_DRIVER_KEY = "SHARE_BLOCKS_TERMINAL_OTHER_DRIVER";

	public const string SHARE_BLOCKS_TERMINAL_CONTROLLER_LABEL_KEY = "SHARE_BLOCKS_TERMINAL_CONTROLLER_LABEL";

	public const string SHARE_BLOCKS_TERMINAL_SEARCH_LOBBY_TEXT_FORMAT_KEY = "SHARE_BLOCKS_TERMINAL_SEARCH_LOBBY_TEXT_FORMAT";

	public const string SHARE_BLOCKS_TERMINAL_SEARCH_ERROR_INVALID_LENGTH_KEY = "SHARE_BLOCKS_TERMINAL_SEARCH_ERROR_INVALID_LENGTH";

	public const string SHARE_BLOCKS_TERMINAL_SEARCH_ERROR_INVALID_ID_KEY = "SHARE_BLOCKS_TERMINAL_SEARCH_ERROR_INVALID_ID";

	[SerializeField]
	private GTZone tableZone = GTZone.monkeBlocksShared;

	[SerializeField]
	private TMP_Text currentMapSelectionText;

	[SerializeField]
	private TMP_Text statusMessageText;

	[SerializeField]
	private TMP_Text currentDriverText;

	[SerializeField]
	private TMP_Text currentDriverLabel;

	[SerializeField]
	private LocalizedText _currentDriverLoc;

	[SerializeField]
	private SharedBlocksScreen noDriverScreen;

	[SerializeField]
	private SharedBlocksScreenSearch searchScreen;

	[SerializeField]
	private GorillaPressableButton terminalControlButton;

	[SerializeField]
	private float loadMapCooldown = 30f;

	[SerializeField]
	private GorillaFriendCollider lobbyTrigger;

	private SharedBlocksManager.SharedBlocksMap selectedMap;

	private SharedBlocksScreen currentScreen;

	private BuilderTable linkedTable;

	public const int NO_DRIVER_ID = -2;

	private bool awaitingWebRequest;

	private string requestedMapID;

	public const string POINTER = "> ";

	public Action<bool> OnMapLoadComplete;

	private bool isTerminalLocked;

	private SharedBlocksTerminalState localState;

	private int cachedLocalPlayerID = -1;

	private bool isLoadingMap;

	private float lastLoadTime;

	private bool useNametags;

	private bool hasInitialized;

	private static StringBuilder sb = new StringBuilder();

	private VRRig driverRig;

	private static List<VRRig> tempRigs = new List<VRRig>(16);

	private int playersInRoom;

	public SharedBlocksManager.SharedBlocksMap SelectedMap => selectedMap;

	public bool IsTerminalLocked => isTerminalLocked;

	private int playersInLobby => lobbyTrigger.playerIDsCurrentlyTouching.Count;

	public bool IsDriver => localState.driverID == NetworkSystem.Instance.LocalPlayer.ActorNumber;

	public int GetDriverID => localState.driverID;

	public BuilderTable GetTable()
	{
		return linkedTable;
	}

	public static string MapIDToDisplayedString(string mapID)
	{
		if (mapID.IsNullOrEmpty())
		{
			return "____-____";
		}
		int num = 4;
		sb.Clear();
		if (mapID.Length > num)
		{
			sb.Append(mapID.Substring(0, num));
			sb.Append("-");
			sb.Append(mapID.Substring(num));
			int repeatCount = 9 - sb.Length;
			sb.Append('_', repeatCount);
		}
		else
		{
			sb.Append(mapID.Substring(0));
			int repeatCount2 = num - sb.Length;
			sb.Append('_', repeatCount2);
			sb.Append("-____");
		}
		return sb.ToString();
	}

	public void Init(BuilderTable table)
	{
		if (!hasInitialized)
		{
			localState = new SharedBlocksTerminalState
			{
				state = TerminalState.NoStatus,
				driverID = -2
			};
			GameEvents.OnSharedBlocksKeyboardButtonPressedEvent.AddListener(PressButton);
			terminalControlButton.onPressButton.AddListener(OnTerminalControlPressed);
			SetTerminalState(TerminalState.NoStatus);
			RefreshActiveScreen();
			linkedTable = table;
			table.linkedTerminal = this;
			linkedTable.OnMapLoaded.AddListener(OnSharedBlocksMapLoaded);
			linkedTable.OnMapLoadFailed.AddListener(OnSharedBlocksMapLoadFailed);
			linkedTable.OnMapCleared.AddListener(OnSharedBlocksMapLoadStart);
			NetworkSystem.Instance.OnMultiplayerStarted += new Action(OnJoinedRoom);
			NetworkSystem.Instance.OnReturnedToSinglePlayer += new Action(OnReturnedToSinglePlayer);
			hasInitialized = true;
		}
	}

	private void Start()
	{
		if (!hasInitialized && BuilderTable.TryGetBuilderTableForZone(tableZone, out var table))
		{
			Init(table);
		}
		else
		{
			Debug.LogWarning("Could not find builder table for zone " + tableZone);
		}
	}

	private void LateUpdate()
	{
		if (localState.driverID != -2 && !(GorillaComputer.instance == null) && useNametags != GorillaComputer.instance.NametagsEnabled)
		{
			useNametags = GorillaComputer.instance.NametagsEnabled;
			RefreshDriverNickname();
		}
	}

	private void OnDestroy()
	{
		GameEvents.OnSharedBlocksKeyboardButtonPressedEvent.RemoveListener(PressButton);
		if (terminalControlButton != null)
		{
			terminalControlButton.onPressButton.RemoveListener(OnTerminalControlPressed);
		}
		if (NetworkSystem.Instance != null)
		{
			NetworkSystem.Instance.OnMultiplayerStarted -= new Action(OnJoinedRoom);
			NetworkSystem.Instance.OnReturnedToSinglePlayer -= new Action(OnReturnedToSinglePlayer);
		}
		if (linkedTable != null)
		{
			linkedTable.OnMapLoaded.RemoveListener(OnSharedBlocksMapLoaded);
			linkedTable.OnMapLoadFailed.RemoveListener(OnSharedBlocksMapLoadFailed);
			linkedTable.OnMapCleared.RemoveListener(OnSharedBlocksMapLoadStart);
		}
	}

	private void RefreshActiveScreen()
	{
		if (localState.driverID == -2)
		{
			if (currentScreen != noDriverScreen)
			{
				if (currentScreen != null)
				{
					currentScreen.Hide();
				}
				currentScreen = noDriverScreen;
				currentScreen.Show();
			}
			statusMessageText.gameObject.SetActive(value: false);
		}
		else if (currentScreen != searchScreen)
		{
			if (currentScreen != null)
			{
				currentScreen.Hide();
			}
			currentScreen = searchScreen;
			currentScreen.Show();
		}
	}

	private void SetTerminalState(TerminalState state)
	{
		localState.state = state;
		string result = "";
		string text = "";
		if (localState.driverID != -2)
		{
			switch (state)
			{
			case TerminalState.NoStatus:
				statusMessageText.gameObject.SetActive(value: false);
				break;
			case TerminalState.Searching:
				text = "SEARCHING...";
				if (!LocalisationManager.TryGetKeyForCurrentLocale("SHARE_BLOCKS_TERMINAL_STATUS_SEARCH", out result, text))
				{
					Debug.LogError("[LOCALIZATION::BUILDER_SCAN_KIOSK] Failed to get key for MONKE BLOCKS SCAN KIOSK localization [SHARE_BLOCKS_TERMINAL_STATUS_SEARCH]");
				}
				SetStatusText(result);
				break;
			case TerminalState.Found:
				text = "MAP FOUND. PRESS 'ENTER' TO LOAD";
				if (!LocalisationManager.TryGetKeyForCurrentLocale("SHARE_BLOCKS_TERMINAL_STATUS_MAP_FOUND", out result, text))
				{
					Debug.LogError("[LOCALIZATION::BUILDER_SCAN_KIOSK] Failed to get key for MONKE BLOCKS SCAN KIOSK localization [SHARE_BLOCKS_TERMINAL_STATUS_MAP_FOUND]");
				}
				SetStatusText(result);
				break;
			case TerminalState.NotFound:
				text = "MAP NOT FOUND";
				if (!LocalisationManager.TryGetKeyForCurrentLocale("SHARE_BLOCKS_TERMINAL_STATUS_MAP_NOT_FOUND", out result, text))
				{
					Debug.LogError("[LOCALIZATION::BUILDER_SCAN_KIOSK] Failed to get key for MONKE BLOCKS SCAN KIOSK localization [SHARE_BLOCKS_TERMINAL_STATUS_MAP_NOT_FOUND]");
				}
				SetStatusText(result);
				break;
			case TerminalState.Loading:
				text = "LOADING...";
				if (!LocalisationManager.TryGetKeyForCurrentLocale("SHARE_BLOCKS_TERMINAL_STATUS_LOADING", out result, text))
				{
					Debug.LogError("[LOCALIZATION::BUILDER_SCAN_KIOSK] Failed to get key for MONKE BLOCKS SCAN KIOSK localization [SHARE_BLOCKS_TERMINAL_STATUS_LOADING]");
				}
				SetStatusText(result);
				break;
			case TerminalState.LoadSuccess:
				text = "LOAD SUCCESS";
				if (!LocalisationManager.TryGetKeyForCurrentLocale("SHARE_BLOCKS_TERMINAL_STATUS_LOAD_SUCCESS", out result, text))
				{
					Debug.LogError("[LOCALIZATION::BUILDER_SCAN_KIOSK] Failed to get key for MONKE BLOCKS SCAN KIOSK localization [SHARE_BLOCKS_TERMINAL_STATUS_LOAD_SUCCESS]");
				}
				SetStatusText(result);
				break;
			case TerminalState.LoadFail:
				text = "LOAD FAILED";
				if (!LocalisationManager.TryGetKeyForCurrentLocale("SHARE_BLOCKS_TERMINAL_STATUS_LOAD_FAILED", out result, text))
				{
					Debug.LogError("[LOCALIZATION::BUILDER_SCAN_KIOSK] Failed to get key for MONKE BLOCKS SCAN KIOSK localization [SHARE_BLOCKS_TERMINAL_STATUS_LOAD_FAILED]");
				}
				SetStatusText(result);
				break;
			}
		}
		else
		{
			statusMessageText.gameObject.SetActive(value: false);
		}
	}

	public void SelectMapIDAndOpenInfo(string mapID)
	{
		if (!awaitingWebRequest)
		{
			selectedMap = null;
			awaitingWebRequest = true;
			requestedMapID = mapID;
			SetTerminalState(TerminalState.Searching);
			SharedBlocksManager.instance.RequestMapDataFromID(mapID, OnPlayerMapRequestComplete);
		}
	}

	private void OnPlayerMapRequestComplete(SharedBlocksManager.SharedBlocksMap response)
	{
		if (!awaitingWebRequest)
		{
			return;
		}
		awaitingWebRequest = false;
		requestedMapID = null;
		if (IsDriver)
		{
			if (response == null || response.MapID == null)
			{
				SetTerminalState(TerminalState.NotFound);
				return;
			}
			selectedMap = response;
			SetTerminalState(TerminalState.Found);
		}
	}

	private bool CanChangeMapState(bool load, out string disallowedReason)
	{
		disallowedReason = "";
		if (NetworkSystem.Instance.InRoom)
		{
			RefreshLobbyCount();
			if (!AreAllPlayersInLobby())
			{
				disallowedReason = "ALL PLAYERS IN THE ROOM MUST BE INSIDE THE LOBBY BEFORE " + (load ? "" : "UN") + "LOADING A MAP.";
				string text = (load ? "SHARE_BLOCKS_TERMINAL_STATUS_DISALLOWED_LOBBY_LOAD" : "SHARE_BLOCKS_TERMINAL_STATUS_DISALLOWED_LOBBY_UNLOAD");
				if (!LocalisationManager.TryGetKeyForCurrentLocale(text, out var result, disallowedReason))
				{
					Debug.LogError("[LOCALIZATION::BUILDER_SCAN_KIOSK] Failed to get key for MONKE BLOCKS SCAN KIOSK localization [" + text + "]");
				}
				disallowedReason = result;
				return false;
			}
			return true;
		}
		disallowedReason = "MUST BE IN A ROOM BEFORE  " + (load ? "" : "UN") + "LOADING A MAP.";
		string text2 = (load ? "SHARE_BLOCKS_TERMINAL_STATUS_DISALLOWED_ROOM_LOAD" : "SHARE_BLOCKS_TERMINAL_STATUS_DISALLOWED_ROOM_UNLOAD");
		if (!LocalisationManager.TryGetKeyForCurrentLocale(text2, out var result2, disallowedReason))
		{
			Debug.LogError("[LOCALIZATION::BUILDER_SCAN_KIOSK] Failed to get key for MONKE BLOCKS SCAN KIOSK localization [" + text2 + "]");
		}
		disallowedReason = result2;
		return false;
	}

	public void SetStatusText(string text)
	{
		statusMessageText.text = text;
		statusMessageText.gameObject.SetActive(value: true);
	}

	private bool IsLocalPlayerInLobby()
	{
		if (!base.isActiveAndEnabled)
		{
			return false;
		}
		if (!lobbyTrigger.playerIDsCurrentlyTouching.Contains(VRRig.LocalRig.creator.UserId))
		{
			return false;
		}
		return true;
	}

	public bool AreAllPlayersInLobby()
	{
		if (!base.isActiveAndEnabled)
		{
			return false;
		}
		return playersInLobby == playersInRoom;
	}

	public string GetLobbyText()
	{
		string defaultResult = "PLAYERS IN ROOM {0}\nPLAYERS IN LOBBY {1}";
		if (!LocalisationManager.TryGetKeyForCurrentLocale("SHARE_BLOCKS_TERMINAL_SEARCH_LOBBY_TEXT_FORMAT", out var result, defaultResult))
		{
			Debug.LogError("[LOCALIZATION::BUILDER_SCAN_KIOSK] Failed to get key for MONKE BLOCKS SCAN KIOSK localization [SHARE_BLOCKS_TERMINAL_SEARCH_LOBBY_TEXT_FORMAT]");
		}
		return string.Format(result, playersInRoom, playersInLobby);
	}

	public void RefreshLobbyCount()
	{
		if (NetworkSystem.Instance != null && NetworkSystem.Instance.InRoom)
		{
			playersInRoom = NetworkSystem.Instance.RoomPlayerCount;
		}
		else
		{
			playersInRoom = 0;
		}
	}

	public void PressButton(SharedBlocksKeyboardBindings buttonPressed)
	{
		if (!IsDriver)
		{
			if (!LocalisationManager.TryGetKeyForCurrentLocale("SHARE_BLOCKS_TERMINAL_STATUS_NOT_CONTROLLER", out var result, "NOT TERMINAL CONTROLLER"))
			{
				Debug.LogError("[LOCALIZATION::BUILDER_SCAN_KIOSK] Failed to get key for SHARE MY BLOCKS TERMINAL localization [SHARE_BLOCKS_TERMINAL_STATUS_NOT_CONTROLLER]");
			}
			SetStatusText(result);
		}
		else
		{
			if (localState.state == TerminalState.Searching || localState.state == TerminalState.Loading)
			{
				return;
			}
			switch (buttonPressed)
			{
			case SharedBlocksKeyboardBindings.up:
				OnUpButtonPressed();
				return;
			case SharedBlocksKeyboardBindings.down:
				OnDownButtonPressed();
				return;
			case SharedBlocksKeyboardBindings.delete:
				OnDeleteButtonPressed();
				return;
			case SharedBlocksKeyboardBindings.enter:
				OnSelectButtonPressed();
				return;
			case SharedBlocksKeyboardBindings.zero:
			case SharedBlocksKeyboardBindings.one:
			case SharedBlocksKeyboardBindings.two:
			case SharedBlocksKeyboardBindings.three:
			case SharedBlocksKeyboardBindings.four:
			case SharedBlocksKeyboardBindings.five:
			case SharedBlocksKeyboardBindings.six:
			case SharedBlocksKeyboardBindings.seven:
			case SharedBlocksKeyboardBindings.eight:
			case SharedBlocksKeyboardBindings.nine:
				OnNumberPressed((int)buttonPressed);
				return;
			}
			if (buttonPressed >= SharedBlocksKeyboardBindings.A && buttonPressed <= SharedBlocksKeyboardBindings.Z)
			{
				OnLetterPressed(buttonPressed.ToString());
			}
		}
	}

	private void OnUpButtonPressed()
	{
		if (currentScreen != null)
		{
			currentScreen.OnUpPressed();
		}
	}

	private void OnDownButtonPressed()
	{
		if (currentScreen != null)
		{
			currentScreen.OnDownPressed();
		}
	}

	private void OnSelectButtonPressed()
	{
		if (localState.state == TerminalState.Found)
		{
			OnLoadMapPressed();
		}
		else if (currentScreen != null)
		{
			currentScreen.OnSelectPressed();
		}
	}

	private void OnDeleteButtonPressed()
	{
		if (localState.state != TerminalState.Loading && localState.state != TerminalState.Searching)
		{
			SetTerminalState(TerminalState.NoStatus);
		}
		if (currentScreen != null)
		{
			currentScreen.OnDeletePressed();
		}
	}

	private void OnBackButtonPressed()
	{
	}

	private void OnNumberPressed(int number)
	{
		if (currentScreen != null)
		{
			currentScreen.OnNumberPressed(number);
		}
	}

	private void OnLetterPressed(string letter)
	{
		if (currentScreen != null)
		{
			currentScreen.OnLetterPressed(letter);
		}
	}

	private void OnTerminalControlPressed()
	{
		if (isTerminalLocked)
		{
			if (IsDriver)
			{
				if (NetworkSystem.Instance.InRoom)
				{
					linkedTable.builderNetworking.RequestBlocksTerminalControl(locked: false);
				}
				else
				{
					SetTerminalDriver(-2);
				}
			}
		}
		else if (NetworkSystem.Instance.InRoom)
		{
			linkedTable.builderNetworking.RequestBlocksTerminalControl(locked: true);
		}
		else
		{
			SetTerminalDriver(NetworkSystem.Instance.LocalPlayer.ActorNumber);
		}
	}

	public void OnLoadMapPressed()
	{
		string disallowedReason;
		if (!IsDriver)
		{
			if (!LocalisationManager.TryGetKeyForCurrentLocale("SHARE_BLOCKS_TERMINAL_STATUS_NOT_CONTROLLER", out var result, "NOT TERMINAL CONTROLLER"))
			{
				Debug.LogError("[LOCALIZATION::BUILDER_SCAN_KIOSK] Failed to get key for SHARE MY BLOCKS TERMINAL localization [SHARE_BLOCKS_TERMINAL_STATUS_NOT_CONTROLLER]");
			}
			SetStatusText(result);
		}
		else if (currentScreen == null || selectedMap == null)
		{
			if (!LocalisationManager.TryGetKeyForCurrentLocale("SHARE_BLOCKS_TERMINAL_STATUS_NO_SELECTION", out var result2, "NO MAP SELECTED"))
			{
				Debug.LogError("[LOCALIZATION::BUILDER_SCAN_KIOSK] Failed to get key for SHARE MY BLOCKS TERMINAL localization [SHARE_BLOCKS_TERMINAL_STATUS_NO_SELECTION]");
			}
			SetStatusText(result2);
		}
		else if (awaitingWebRequest || isLoadingMap)
		{
			if (!LocalisationManager.TryGetKeyForCurrentLocale("SHARE_BLOCKS_TERMINAL_STATUS_IN_PROGRESS", out var _, "BLOCKS LOAD ALREADY IN PROGRESS"))
			{
				Debug.LogError("[LOCALIZATION::BUILDER_SCAN_KIOSK] Failed to get key for SHARE MY BLOCKS TERMINAL localization [SHARE_BLOCKS_TERMINAL_STATUS_IN_PROGRESS]");
			}
			SetStatusText("BLOCKS LOAD ALREADY IN PROGRESS");
		}
		else if (!CanChangeMapState(load: true, out disallowedReason))
		{
			SetStatusText(disallowedReason);
		}
		else
		{
			if (!(linkedTable != null))
			{
				return;
			}
			if (Time.time > lastLoadTime + loadMapCooldown)
			{
				if (!LocalisationManager.TryGetKeyForCurrentLocale("SHARE_BLOCKS_TERMINAL_STATUS_LOADING", out var _, "LOADING BLOCKS ..."))
				{
					Debug.LogError("[LOCALIZATION::BUILDER_SCAN_KIOSK] Failed to get key for SHARE MY BLOCKS TERMINAL localization [SHARE_BLOCKS_TERMINAL_STATUS_LOADING]");
				}
				SetStatusText("LOADING BLOCKS ...");
				isLoadingMap = true;
				lastLoadTime = Time.time;
				linkedTable.LoadSharedMap(selectedMap);
			}
			else
			{
				int num = Mathf.RoundToInt(lastLoadTime + loadMapCooldown - Time.time);
				string defaultResult = $"PLEASE WAIT {num} SECONDS BEFORE LOADING ANOTHER MAP";
				if (!LocalisationManager.TryGetKeyForCurrentLocale("SHARE_BLOCKS_TERMINAL_STATUS_WAIT", out var result5, defaultResult))
				{
					Debug.LogError("[LOCALIZATION::BUILDER_SCAN_KIOSK] Failed to get key for SHARE MY BLOCKS TERMINAL localization [SHARE_BLOCKS_TERMINAL_STATUS_LOADING]");
				}
				result5 = result5.Replace("{time}", num.ToString());
				SetStatusText(result5);
			}
		}
	}

	public bool IsPlayerDriver(Player player)
	{
		return player.ActorNumber == localState.driverID;
	}

	public bool ValidateTerminalControlRequest(bool locked, int playerNumber)
	{
		if (locked && playerNumber == -2)
		{
			return false;
		}
		if (localState.driverID == -2)
		{
			return locked;
		}
		return localState.driverID == playerNumber;
	}

	private void OnDriverNameChanged()
	{
		RefreshDriverNickname();
	}

	public void SetTerminalDriver(int playerNum)
	{
		if (playerNum != -2)
		{
			if (localState.driverID != -2 && localState.driverID != playerNum)
			{
				GTDev.LogWarning($"Shared BlocksTerminal SetTerminalDriver cannot set {playerNum} as driver while {localState.driverID} is driver");
				return;
			}
			localState.driverID = playerNum;
			NetPlayer netPlayerByID = NetworkSystem.Instance.GetNetPlayerByID(playerNum);
			if (netPlayerByID != null && VRRigCache.Instance.TryGetVrrig(netPlayerByID, out var playerRig))
			{
				driverRig = playerRig.Rig;
				driverRig.OnPlayerNameVisibleChanged += OnDriverNameChanged;
			}
			isTerminalLocked = true;
			UpdateTerminalButton();
			RefreshActiveScreen();
			searchScreen.SetInputTextEnabled(IsDriver);
			if (IsDriver && awaitingWebRequest)
			{
				SetTerminalState(TerminalState.Searching);
				searchScreen.SetMapCode(requestedMapID);
			}
			else if (isLoadingMap)
			{
				SetTerminalState(TerminalState.Loading);
				searchScreen.SetMapCode(linkedTable.GetPendingMap());
			}
			else
			{
				SetTerminalState(TerminalState.NoStatus);
			}
		}
		else
		{
			if (driverRig != null)
			{
				driverRig.OnPlayerNameVisibleChanged -= OnDriverNameChanged;
				driverRig = null;
			}
			localState.driverID = -2;
			isTerminalLocked = false;
			UpdateTerminalButton();
			SetTerminalState(TerminalState.NoStatus);
			RefreshActiveScreen();
		}
		RefreshDriverNickname();
	}

	private void RefreshDriverNickname()
	{
		StringVariable stringVariable = _currentDriverLoc.StringReference["playerName"] as StringVariable;
		if (localState.driverID == -2)
		{
			currentDriverLabel.gameObject.SetActive(value: false);
			stringVariable.Value = "";
			currentDriverText.text = "";
			currentDriverText.gameObject.SetActive(value: false);
			return;
		}
		bool flag = KIDManager.HasPermissionToUseFeature(EKIDFeatures.Custom_Nametags);
		if (NetworkSystem.Instance.InRoom)
		{
			NetPlayer player = NetworkSystem.Instance.GetPlayer(localState.driverID);
			if (player != null && useNametags && flag)
			{
				RigContainer playerRig;
				if (player.IsLocal)
				{
					stringVariable.Value = player.NickName;
					currentDriverText.text = player.NickName;
				}
				else if (VRRigCache.Instance.TryGetVrrig(player, out playerRig))
				{
					stringVariable.Value = playerRig.Rig.playerNameVisible;
					currentDriverText.text = playerRig.Rig.playerNameVisible;
				}
				else
				{
					stringVariable.Value = player.DefaultName;
					currentDriverText.text = player.DefaultName;
				}
			}
			else
			{
				stringVariable.Value = "";
				currentDriverText.text = "";
			}
		}
		else
		{
			stringVariable.Value = ((useNametags && flag) ? NetworkSystem.Instance.LocalPlayer.NickName : NetworkSystem.Instance.LocalPlayer.DefaultName);
			currentDriverText.text = ((useNametags && flag) ? NetworkSystem.Instance.LocalPlayer.NickName : NetworkSystem.Instance.LocalPlayer.DefaultName);
		}
		currentDriverLabel.gameObject.SetActive(value: true);
	}

	public bool ValidateLoadMapRequest(string mapID, int playerNum)
	{
		if (playerNum != localState.driverID)
		{
			return false;
		}
		if (!AreAllPlayersInLobby())
		{
			return false;
		}
		return SharedBlocksManager.IsMapIDValid(mapID);
	}

	private void OnJoinedRoom()
	{
		cachedLocalPlayerID = NetworkSystem.Instance.LocalPlayer.ActorNumber;
		ResetTerminalControl();
	}

	private void OnReturnedToSinglePlayer()
	{
		if (localState.driverID != cachedLocalPlayerID)
		{
			ResetTerminalControl();
		}
		else
		{
			localState.driverID = NetworkSystem.Instance.LocalPlayer.ActorNumber;
		}
		cachedLocalPlayerID = -1;
	}

	public void ResetTerminalControl()
	{
		localState.driverID = -2;
		isTerminalLocked = false;
		selectedMap = null;
		SetTerminalState(TerminalState.NoStatus);
		RefreshActiveScreen();
		UpdateTerminalButton();
	}

	private void UpdateTerminalButton()
	{
		terminalControlButton.isOn = isTerminalLocked;
		terminalControlButton.UpdateColor();
	}

	private void OnSharedBlocksMapLoaded(string mapID)
	{
		if (!IsDriver)
		{
			searchScreen.SetMapCode(mapID);
		}
		if (SharedBlocksManager.IsMapIDValid(mapID))
		{
			SetTerminalState(TerminalState.LoadSuccess);
		}
		else if (localState.state != TerminalState.LoadFail)
		{
			SetTerminalState(TerminalState.LoadFail);
		}
		isLoadingMap = false;
	}

	private void OnSharedBlocksMapLoadFailed(string message)
	{
		SetTerminalState(TerminalState.LoadFail);
		SetStatusText(message);
		isLoadingMap = false;
	}

	private void OnSharedBlocksMapLoadStart()
	{
		if (!(linkedTable == null) && !IsDriver)
		{
			searchScreen.SetMapCode(linkedTable.GetPendingMap());
			SetTerminalState(TerminalState.Loading);
			isLoadingMap = true;
			lastLoadTime = Time.time;
		}
	}
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GorillaGameModes;
using GorillaTagScripts;
using GorillaTagScripts.VirtualStumpCustomMaps;
using KID.Model;
using Photon.Pun;
using Photon.Realtime;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.CloudScriptModels;
using PlayFab.Json;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace GorillaNetworking;

public class GorillaComputer : MonoBehaviour, IMatchmakingCallbacks, IGorillaSliceableSimple
{
	public enum ComputerState
	{
		Startup,
		Color,
		Name,
		Turn,
		Mic,
		Room,
		Queue,
		Group,
		Voice,
		AutoMute,
		Credits,
		Visuals,
		Time,
		NameWarning,
		Loading,
		Support,
		Troop,
		KID,
		Redemption,
		Language
	}

	private enum NameCheckResult
	{
		Success,
		Warning,
		Ban
	}

	public enum RedemptionResult
	{
		Empty,
		Invalid,
		Checking,
		AlreadyUsed,
		TooEarly,
		TooLate,
		Success
	}

	[Serializable]
	public class StateOrderItem
	{
		public ComputerState State;

		[Tooltip("Case not important - ToUpper applied at runtime")]
		public string OverrideName = "";

		public LocalizedString StringReference;

		private Locale _previousLocale;

		private string _cachedTranslation = "";

		public StateOrderItem()
		{
		}

		public StateOrderItem(ComputerState state)
		{
			State = state;
		}

		public StateOrderItem(ComputerState state, string overrideName)
		{
			State = state;
			OverrideName = overrideName;
		}

		public string GetName()
		{
			if (_previousLocale == LocalizationSettings.SelectedLocale && !string.IsNullOrEmpty(_cachedTranslation))
			{
				return _cachedTranslation;
			}
			if (StringReference == null || StringReference.IsEmpty)
			{
				return GetPreLocalisedName();
			}
			_previousLocale = LocalizationSettings.SelectedLocale;
			_cachedTranslation = StringReference.GetLocalizedString()?.ToUpper();
			if (string.IsNullOrEmpty(_cachedTranslation))
			{
				if (LocalisationManager.ApplicationRunning)
				{
					Debug.LogError("[LOCALIZATION::STATE_ORDER_ITEM] Failed to get translation for selected locale [" + (_previousLocale?.LocaleName ?? "NULL") + ", for item [" + State.GetName() + "]");
				}
				_cachedTranslation = "";
			}
			return _cachedTranslation;
		}

		public string GetPreLocalisedName()
		{
			if (!string.IsNullOrEmpty(OverrideName))
			{
				return OverrideName.ToUpper();
			}
			return State.ToString().ToUpper();
		}
	}

	private enum EKidScreenState
	{
		Ready,
		Show_OTP,
		Show_Setup_Screen
	}

	private const string VERSION_MISMATCH_KEY = "VERSION_MISMATCH";

	private const string CONNECTION_ISSUE_KEY = "CONNECTION_ISSUE";

	private const string NO_CONNECTION_KEY = "NO_CONNECTION";

	private const string STARTUP_INTRO_KEY = "STARTUP_INTRO";

	private const string STARTUP_PLAYERS_ONLINE_KEY = "STARTUP_PLAYERS_ONLINE";

	private const string STARTUP_USERS_BANNED_KEY = "STARTUP_USERS_BANNED";

	private const string STARTUP_PRESS_KEY_KEY = "STARTUP_PRESS_KEY";

	private const string STARTUP_PRESS_KEY_SHORT_KEY = "STARTUP_PRESS_KEY_SHORT";

	private const string STARTUP_MANAGED_KEY = "STARTUP_MANAGED";

	private const string COLOR_SELECT_INTRO_KEY = "COLOR_SELECT_INTRO";

	private const string CURRENT_SELECTED_LANGUAGE_KEY = "CURRENT_SELECTED_LANGUAGE";

	private const string CHANGE_TO_KEY = "CHANGE_TO";

	private const string CONFIRM_LANGUAGE_KEY = "CONFIRM_LANGUAGE";

	private const string COLOR_RED_KEY = "COLOR_RED";

	private const string COLOR_GREEN_KEY = "COLOR_GREEN";

	private const string COLOR_BLUE_KEY = "COLOR_BLUE";

	private const string ROOM_INTRO_KEY = "ROOM_INTRO";

	private const string ROOM_OPTION_KEY = "ROOM_OPTION";

	private const string ROOM_TEXT_CURRENT_ROOM_KEY = "ROOM_TEXT_CURRENT_ROOM";

	private const string PLAYERS_IN_ROOM_KEY = "PLAYERS_IN_ROOM";

	private const string NOT_IN_ROOM_KEY = "NOT_IN_ROOM";

	private const string PLAYERS_ONLINE_KEY = "PLAYERS_ONLINE";

	private const string ROOM_TO_JOIN_KEY = "ROOM_TO_JOIN";

	private const string ROOM_FULL_KEY = "ROOM_FULL";

	private const string ROOM_JOIN_NOT_ALLOWED_KEY = "ROOM_JOIN_NOT_ALLOWED";

	private const string LANGUAGE_KEY = "LANGUAGE";

	private const string NAME_SCREEN_KEY = "NAME_SCREEN";

	private const string CURRENT_NAME_KEY = "CURRENT_NAME";

	private const string NEW_NAME_KEY = "NEW_NAME";

	private const string TURN_SCREEN_KEY = "TURN_SCREEN";

	private const string TURN_SCREEN_TURNING_SPEED_KEY = "TURN_SCREEN_TURNING_SPEED";

	private const string TURN_SCREEN_TURN_TYPE_KEY = "TURN_SCREEN_TURN_TYPE";

	private const string TURN_SCREEN_TURN_SPEED_KEY = "TURN_SCREEN_TURN_SPEED";

	private const string TURN_TYPE_SNAP_TURN_KEY = "TURN_TYPE_SNAP_TURN";

	private const string TURN_TYPE_SMOOTH_TURN_KEY = "TURN_TYPE_SMOOTH_TURN";

	private const string TURN_TYPE_NO_TURN_KEY = "TURN_TYPE_NO_TURN";

	private const string QUEUE_SCREEN_KEY = "QUEUE_SCREEN";

	private const string BEAT_OBSTACLE_COURSE_KEY = "BEAT_OBSTACLE_COURSE";

	private const string COMPETITIVE_DESC_KEY = "COMPETITIVE_DESC";

	private const string QUEUE_SCREEN_ALL_QUEUES_KEY = "QUEUE_SCREEN_ALL_QUEUES";

	private const string QUEUE_SCREEN_DEFAULT_QUEUES_KEY = "QUEUE_SCREEN_DEFAULT_QUEUES";

	private const string CURRENT_QUEUE_KEY = "CURRENT_QUEUE";

	private const string DEFAULT_QUEUE_KEY = "DEFAULT_QUEUE";

	private const string MINIGAMES_QUEUE_KEY = "MINIGAMES_QUEUE";

	private const string COMPETITIVE_QUEUE_KEY = "COMPETITIVE_QUEUE";

	private const string MIC_SCREEN_INTRO_KEY = "MIC_SCREEN_INTRO";

	private const string MIC_SCREEN_OPTIONS_KEY = "MIC_SCREEN_OPTIONS";

	private const string MIC_SCREEN_CURRENT_KEY = "MIC_SCREEN_CURRENT";

	private const string MIC_SCREEN_PUSH_TO_MUTE_TOOLTIP_KEY = "MIC_SCREEN_PUSH_TO_MUTE_TOOLTIP";

	private const string MIC_SCREEN_MIC_DISABLED_KEY = "MIC_SCREEN_MIC_DISABLED";

	private const string MIC_SCREEN_NO_MIC_KEY = "MIC_SCREEN_NO_MIC";

	private const string MIC_SCREEN_NO_PERMISSIONS_KEY = "MIC_SCREEN_NO_PERMISSIONS";

	private const string MIC_SCREEN_PUSH_TO_TALK_TOOLTIP_KEY = "MIC_SCREEN_PUSH_TO_TALK_TOOLTIP";

	private const string MIC_SCREEN_INPUT_TEST_LABEL_KEY = "MIC_SCREEN_INPUT_TEST_LABEL";

	private const string MIC_SCREEN_INPUT_TEST_NO_MIC_KEY = "MIC_SCREEN_INPUT_TEST_NO_MIC";

	private const string ALL_CHAT_MIC_KEY = "ALL_CHAT_MIC";

	private const string PUSH_TO_TALK_MIC_KEY = "PUSH_TO_TALK_MIC";

	private const string PUSH_TO_MUTE_MIC_KEY = "PUSH_TO_MUTE_MIC";

	private const string OPEN_MIC_KEY = "OPEN_MIC";

	private const string AUTOMOD_SCREEN_INTRO_KEY = "AUTOMOD_SCREEN_INTRO";

	private const string AUTOMOD_SCREEN_OPTIONS_KEY = "AUTOMOD_SCREEN_OPTIONS";

	private const string AUTOMOD_SCREEN_CURRENT_KEY = "AUTOMOD_SCREEN_CURRENT";

	private const string AUTOMOD_AGGRESSIVE_KEY = "AUTOMOD_AGGRESSIVE";

	private const string AUTOMOD_MODERATE_KEY = "AUTOMOD_MODERATE";

	private const string AUTOMOD_OFF_KEY = "AUTOMOD_OFF";

	private const string VOICE_CHAT_SCREEN_INTRO_OLD_KEY = "VOICE_CHAT_SCREEN_INTRO_OLD";

	private const string VOICE_CHAT_SCREEN_OPTIONS_OLD_KEY = "VOICE_CHAT_SCREEN_OPTIONS_OLD";

	private const string VOICE_CHAT_SCREEN_CURRENT_OLD_KEY = "VOICE_CHAT_SCREEN_CURRENT_OLD";

	private const string TRUE_KEY = "TRUE";

	private const string FALSE_KEY = "FALSE";

	private const string VOICE_CHAT_SCREEN_INTRO_KEY = "VOICE_CHAT_SCREEN_INTRO";

	private const string VOICE_CHAT_SCREEN_OPTIONS_KEY = "VOICE_CHAT_SCREEN_OPTIONS";

	private const string VOICE_CHAT_SCREEN_CURRENT_KEY = "VOICE_CHAT_SCREEN_CURRENT";

	private const string VOICE_OPTION_HUMAN_KEY = "VOICE_OPTION_HUMAN";

	private const string VOICE_OPTION_MONKE_KEY = "VOICE_OPTION_MONKE";

	private const string VOICE_OPTION_OFF_KEY = "VOICE_OPTION_OFF";

	private const string VISUALS_SCREEN_INTRO_KEY = "VISUALS_SCREEN_INTRO";

	private const string VISUALS_SCREEN_OPTIONS_KEY = "VISUALS_SCREEN_OPTIONS";

	private const string VISUALS_SCREEN_CURRENT_KEY = "VISUALS_SCREEN_CURRENT";

	private const string VISUALS_SCREEN_VOLUME_KEY = "VISUALS_SCREEN_VOLUME";

	private const string CREDITS_KEY = "CREDITS";

	private const string CREDITS_PRESS_ENTER_KEY = "CREDITS_PRESS_ENTER";

	private const string CREDITS_CONTINUED_KEY = "CREDITS_CONTINUED";

	private const string TIME_SCREEN_KEY = "TIME_SCREEN";

	private const string GROUP_SCREEN_LIMITED_OLD_KEY = "GROUP_SCREEN_LIMITED_OLD";

	private const string GROUP_SCREEN_FULL_OLD_KEY = "GROUP_SCREEN_FULL_OLD";

	private const string GROUP_SCREEN_SELECTION_OLD_KEY = "GROUP_SCREEN_SELECTION_OLD";

	private const string PLATFORM_STEAM_KEY = "PLATFORM_STEAM";

	private const string PLATFORM_QUEST_KEY = "PLATFORM_QUEST";

	private const string PLATFORM_PSVR_KEY = "PLATFORM_PSVR";

	private const string PLATFORM_PICO_KEY = "PLATFORM_PICO";

	private const string PLATFORM_OCULUS_PC_KEY = "PLATFORM_OCULUS_PC";

	private const string SUPPORT_SCREEN_INTRO_KEY = "SUPPORT_SCREEN_INTRO";

	private const string SUPPORT_SCREEN_DETAILS_PLAYER_ID_KEY = "SUPPORT_SCREEN_DETAILS_PLAYERID";

	private const string SUPPORT_SCREEN_DETAILS_VERSION_KEY = "SUPPORT_SCREEN_DETAILS_VERSION";

	private const string SUPPORT_SCREEN_DETAILS_PLATFORM_KEY = "SUPPORT_SCREEN_DETAILS_PLATFORM";

	private const string SUPPORT_SCREEN_DETAILS_BUILD_DATE_KEY = "SUPPORT_SCREEN_DETAILS_BUILD_DATE";

	private const string SUPPORT_SCREEN_DETAILS_MOTHERSHIP_SESSION_ID_KEY = "SUPPORT_SCREEN_DETAILS_MOTHERSHIP_SESSION_ID";

	private const string SUPPORT_SCREEN_INITIAL_KEY = "SUPPORT_SCREEN_INITIAL";

	private const string SUPPORT_SCREEN_INITIAL_WARNING_KEY = "SUPPORT_SCREEN_INITIAL_WARNING";

	private const string OCULUS_BUILD_CODE_KEY = "OCULUS_BUILD_CODE";

	private const string LOADING_SCREEN_KEY = "LOADING_SCREEN";

	private const string WARNING_SCREEN_KEY = "WARNING_SCREEN";

	private const string WARNING_SCREEN_CONFIRMATION_KEY = "WARNING_SCREEN_CONFIRMATION";

	private const string WARNING_SCREEN_TYPE_YES_KEY = "WARNING_SCREEN_TYPE_YES";

	private const string FUNCTION_ROOM_KEY = "FUNCTION_ROOM";

	private const string FUNCTION_NAME_KEY = "FUNCTION_NAME";

	private const string FUNCTION_COLOR_KEY = "FUNCTION_COLOR";

	private const string FUNCTION_TURN_KEY = "FUNCTION_TURN";

	private const string FUNCTION_MIC_KEY = "FUNCTION_MIC";

	private const string FUNCTION_QUEUE_KEY = "FUNCTION_QUEUE";

	private const string FUNCTION_GROUP_KEY = "FUNCTION_GROUP";

	private const string FUNCTION_VOICE_KEY = "FUNCTION_VOICE";

	private const string FUNCTION_AUTOMOD_KEY = "FUNCTION_AUTOMOD";

	private const string FUNCTION_ITEMS_KEY = "FUNCTION_ITEMS";

	private const string FUNCTION_CREDITS_KEY = "FUNCTION_CREDITS";

	private const string FUNCTION_LANGUAGE_KEY = "FUNCTION_LANGUAGE";

	private const string FUNCTION_SUPPORT_KEY = "FUNCTION_SUPPORT";

	private const string COMPUTER_KEYBOARD_DELETE_KEY = "COMPUTER_KEYBOARD_DELETE";

	private const string COMPUTER_KEYBOARD_ENTER_KEY = "COMPUTER_KEYBOARD_ENTER";

	private const string COMPUTER_KEYBOARD_OPTION1_KEY = "COMPUTER_KEYBOARD_OPTION1";

	private const string COMPUTER_KEYBOARD_OPTION2_KEY = "COMPUTER_KEYBOARD_OPTION2";

	private const string COMPUTER_KEYBOARD_OPTION3_KEY = "COMPUTER_KEYBOARD_OPTION3";

	private const string WARNING_SCREEN_YES_INPUT_KEY = "WARNING_SCREEN_YES_INPUT";

	private const string GROUP_SCREEN_ENTER_PARTY_KEY = "GROUP_SCREEN_ENTER_PARTY";

	private const string GROUP_SCREEN_ENTER_NOPARTY_KEY = "GROUP_SCREEN_ENTER_NOPARTY";

	private const string GROUP_SCREEN_CANNOT_JOIN_KEY = "GROUP_SCREEN_CANNOT_JOIN";

	private const string GROUP_SCREEN_ACTIVE_ZONES_KEY = "GROUP_SCREEN_ACTIVE_ZONES";

	private const string GROUP_SCREEN_DESTINATIONS_KEY = "GROUP_SCREEN_DESTINATIONS";

	private const string NAME_SCREEN_TOGGLE_NAMETAGS_KEY = "NAME_SCREEN_TOGGLE_NAMETAGS";

	private const string NAME_SCREEN_KID_PROHIBITED_VERB_KEY = "NAME_SCREEN_KID_PROHIBITED_VERB";

	private const string NAME_SCREEN_DISABLED_KEY = "NAME_SCREEN_DISABLED";

	private const string ON_KEY = "ON_KEY";

	private const string OFF_KEY = "OFF_KEY";

	private const string KID_PROHIBITED_MESSAGE_KEY = "KID_PROHIBITED_MESSAGE";

	private const string KID_PERMISSION_NEEDED_KEY = "KID_PERMISSION_NEEDED";

	private const string KID_WAITING_PERMISSION_KEY = "KID_WAITING_PERMISSION";

	private const string KID_REFRESH_PERMISSIONS_KEY = "KID_REFRESH_PERMISSIONS";

	private const string KID_CHECK_AGAIN_COOLDOWN_KEY = "KID_CHECK_AGAIN_COOLDOWN";

	private const string STARTUP_TROOP_TEXT_KEY = "STARTUP_TROOP_TEXT";

	private const string ROOM_GROUP_TRAVEL_KEY = "ROOM_GROUP_TRAVEL";

	private const string ROOM_PARTY_WARNING_KEY = "ROOM_PARTY_WARNING";

	private const string ROOM_GAME_LABEL_KEY = "ROOM_GAME_LABEL";

	private const string ROOM_SCREEN_KID_PROHIBITED_VERB_KEY = "ROOM_SCREEN_KID_PROHIBITED_VERB";

	private const string ROOM_SCREEN_DISABLED_KEY = "ROOM_SCREEN_DISABLED";

	private const string REDEMPTION_INTRO_KEY = "REDEMPTION_INTRO";

	private const string REDEMPTION_CODE_LABEL_KEY = "REDEMPTION_CODE_LABEL";

	private const string REDEMPTION_CODE_INVALID_KEY = "REDEMPTION_CODE_INVALID";

	private const string REDEMPTION_CODE_VALIDATING_KEY = "REDEMPTION_CODE_VALIDATING";

	private const string REDEMPTION_CODE_ALREADY_USED_KEY = "REDEMPTION_CODE_ALREADY_USED";

	private const string REDEMPTION_CODE_TOO_EARLY_KEY = "REDEMPTION_CODE_TOO_EARLY";

	private const string REDEMPTION_CODE_TOO_LATE_KEY = "REDEMPTION_CODE_TOO_LATE";

	private const string REDEMPTION_CODE_SUCCESS_KEY = "REDEMPTION_CODE_SUCCESS";

	private const string LIMITED_ONLINE_FUNC_KEY = "LIMITED_ONLINE_FUNC";

	private const string CURRENT_MODE_KEY = "CURRENT_MODE";

	private const string SUPPORT_META_ACCOUNT_TYPE_KEY = "SUPPORT_META_ACCOUNT_TYPE";

	private const string SUPPORT_FINAL_QUEST_ONE_KEY = "SUPPORT_FINAL_QUEST_ONE";

	private const string SUPPORT_KID_ACCOUNT_TYPE_KEY = "SUPPORT_KID_ACCOUNT_TYPE";

	private const string VOICE_SCREEN_KID_PROHIBITED_VERB_KEY = "VOICE_SCREEN_KID_PROHIBITED_VERB";

	private const string VOICE_SCREEN_DISABLED_KEY = "VOICE_SCREEN_DISABLED";

	private const string MIC_SCREEN_GUARDIAN_FEATURE_DESC_KEY = "VOICE_SCREEN_GUARDIAN_FEATURE_DESC";

	private const string VOICE_SCREEN_KID_CURRENT_VOICE_KEY = "VOICE_SCREEN_KID_CURRENT_VOICE";

	private const string MIC_SCREEN_PUSH_KEY_INSTRUCTIONS_KEY = "MIC_SCREEN_PUSH_KEY_INSTRUCTIONS";

	private const string TROOP_SCREEN_INTRO_KEY = "TROOP_SCREEN_INTRO";

	private const string TROOP_SCREEN_INSTRUCTIONS_KEY = "TROOP_SCREEN_INSTRUCTIONS";

	private const string TROOP_SCREEN_CURRENT_TROOP_KEY = "TROOP_SCREEN_CURRENT_TROOP";

	private const string TROOP_SCREEN_IN_QUEUE_KEY = "TROOP_SCREEN_IN_QUEUE";

	private const string TROOP_SCREEN_PLAYERS_IN_TROOP_KEY = "TROOP_SCREEN_PLAYERS_IN_TROOP";

	private const string TROOP_SCREEN_DEFAULT_QUEUE_KEY = "TROOP_SCREEN_DEFAULT_QUEUE";

	private const string TROOP_SCREEN_CURRENT_QUEUE_KEY = "TROOP_SCREEN_CURRENT_QUEUE";

	private const string TROOP_SCREEN_TROOP_QUEUE_KEY = "TROOP_SCREEN_TROOP_QUEUE";

	private const string TROOP_SCREEN_LEAVE_KEY = "TROOP_SCREEN_LEAVE";

	private const string TROOP_SCREEN_NOT_IN_TROOP_KEY = "TROOP_SCREEN_NOT_IN_TROOP";

	private const string TROOP_SCREEN_JOIN_TROOP_KEY = "TROOP_SCREEN_JOIN_TROOP";

	private const string TROOP_SCREEN_KID_PROHIBITED_VERB_KEY = "TROOP_SCREEN_KID_PROHIBITED_VERB";

	private const string TROOP_SCREEN_DISABLED_KEY = "TROOP_SCREEN_DISABLED";

	private const string TROOP_SCREEN_KID_DESC_KEY = "TROOP_SCREEN_KID_DESC";

	private const bool HIDE_SCREENS = false;

	public const string NAMETAG_PLAYER_PREF_KEY = "nameTagsOn";

	[OnEnterPlay_SetNull]
	public static volatile GorillaComputer instance;

	[OnEnterPlay_Set(false)]
	public static bool hasInstance = false;

	[OnEnterPlay_SetNull]
	private static Action<bool> onNametagSettingChangedAction;

	public bool tryGetTimeAgain;

	public Material unpressedMaterial;

	public Material pressedMaterial;

	public string currentTextField;

	public float buttonFadeTime;

	public string offlineTextInitialString;

	public GorillaText screenText;

	public GorillaText functionSelectText;

	public GorillaText wallScreenText;

	private Locale _lastLocaleChecked_Version;

	private Locale _lastLocaleChecked_Connect;

	private string _cachedVersionMismatch = "PLEASE UPDATE TO THE LATEST VERSION OF GORILLA TAG. YOU'RE ON AN OLD VERSION. FEEL FREE TO RUN AROUND, BUT YOU WON'T BE ABLE TO PLAY WITH ANYONE ELSE.";

	private string _cachedUnableToConnect = "UNABLE TO CONNECT TO THE INTERNET. PLEASE CHECK YOUR CONNECTION AND RESTART THE GAME.";

	public Material wrongVersionMaterial;

	public MeshRenderer wallScreenRenderer;

	public MeshRenderer computerScreenRenderer;

	public long startupMillis;

	public DateTime startupTime;

	public GameModeType lastPressedGameModeType;

	public string lastPressedGameMode;

	public WatchableStringSO currentGameMode;

	public WatchableStringSO currentGameModeText;

	public int includeUpdatedServerSynchTest;

	public PhotonNetworkController networkController;

	public float updateCooldown = 1f;

	private float defaultUpdateCooldown;

	private float micUpdateCooldown = 0.01f;

	public float lastUpdateTime;

	private float deltaTime;

	public bool isConnectedToMaster;

	public bool internetFailure;

	public string[] _allowedMapsToJoin;

	public bool limitOnlineScreens;

	[Header("State vars")]
	public bool stateUpdated;

	public bool screenChanged;

	public bool initialized;

	public List<StateOrderItem> OrderList = new List<StateOrderItem>
	{
		new StateOrderItem(ComputerState.Room),
		new StateOrderItem(ComputerState.Name),
		new StateOrderItem(ComputerState.Language, "Lang"),
		new StateOrderItem(ComputerState.Turn),
		new StateOrderItem(ComputerState.Mic),
		new StateOrderItem(ComputerState.Queue),
		new StateOrderItem(ComputerState.Troop),
		new StateOrderItem(ComputerState.Group),
		new StateOrderItem(ComputerState.Voice),
		new StateOrderItem(ComputerState.AutoMute, "Automod"),
		new StateOrderItem(ComputerState.Visuals, "Items"),
		new StateOrderItem(ComputerState.Credits),
		new StateOrderItem(ComputerState.Support)
	};

	public string Pointer = "<-";

	public int highestCharacterCount;

	public List<string> FunctionNames = new List<string>();

	public int FunctionsCount;

	[Header("Room vars")]
	public string roomToJoin;

	public bool roomFull;

	public bool roomNotAllowed;

	[Header("Mic vars")]
	public string pttType;

	private GorillaSpeakerLoudness speakerLoudness;

	private float micInputTestTimer;

	public float micInputTestTimerThreshold = 10f;

	[Header("Automute vars")]
	public string autoMuteType;

	[Header("Queue vars")]
	public string currentQueue;

	public bool allowedInCompetitive;

	[Header("Group Vars")]
	public string groupMapJoin;

	public int groupMapJoinIndex;

	public GorillaFriendCollider friendJoinCollider;

	[Header("Troop vars")]
	public string troopName;

	public bool troopQueueActive;

	public string troopToJoin;

	private bool rememberTroopQueueState;

	[Header("Join Triggers")]
	public Dictionary<string, GorillaNetworkJoinTrigger> primaryTriggersByZone = new Dictionary<string, GorillaNetworkJoinTrigger>();

	public string voiceChatOn;

	[Header("Mode select vars")]
	public ModeSelectButton[] modeSelectButtons;

	public string version;

	public string buildDate;

	public string buildCode;

	[Header("Cosmetics")]
	public bool disableParticles;

	public float instrumentVolume;

	public bool perfMode;

	public bool isSubcribed;

	[Header("Credits")]
	public CreditsView creditsView;

	[Header("Handedness")]
	public bool leftHanded;

	[Header("Name state vars")]
	public string savedName;

	public string currentName;

	public TextAsset exactOneWeekFile;

	public TextAsset anywhereOneWeekFile;

	public TextAsset anywhereTwoWeekFile;

	private List<ComputerState> _filteredStates = new List<ComputerState>();

	private List<StateOrderItem> _activeOrderList = new List<StateOrderItem>();

	private Stack<ComputerState> stateStack = new Stack<ComputerState>();

	private ComputerState currentComputerState;

	private ComputerState previousComputerState;

	private int currentStateIndex;

	private int usersBanned;

	private float redValue;

	private string redText;

	private float blueValue;

	private string blueText;

	private float greenValue;

	private string greenText;

	private int colorCursorLine;

	private string warningConfirmationInputString = string.Empty;

	private bool displaySupport;

	private string[] exactOneWeek;

	private string[] anywhereOneWeek;

	private string[] anywhereTwoWeek;

	private RedemptionResult redemptionResult;

	private string redemptionCode = "";

	private bool playerInVirtualStump;

	private string virtualStumpRoomPrepend = "";

	private WaitForSeconds waitOneSecond = new WaitForSeconds(1f);

	private Coroutine LoadingRoutine;

	private List<string> topTroops = new List<string>();

	private bool hasRequestedInitialTroopPopulation;

	private int currentTroopPopulation = -1;

	private float lastCheckedWifi;

	private float checkIfDisconnectedSeconds = 10f;

	private float checkIfConnectedSeconds = 1f;

	private bool didInitializeGameMode;

	private static int sessionCount = -1;

	private const bool k_debug_shouldResetSessionCount = false;

	private const bool k_debug_shouldResetGameMode = false;

	private const string k_sessionCountKey = "sessionCount";

	internal const GameModeType k_defaultGameMode = GameModeType.SuperInfect;

	internal const GameModeType k_noobGameMode = GameModeType.Infection;

	private const int k_noobSessionCountThreshold = 4;

	private float troopPopulationCheckCooldown = 3f;

	private float nextPopulationCheckTime;

	public Action OnServerTimeUpdated;

	private const string ENABLED_COLOUR = "#85ffa5";

	private const string DISABLED_COLOUR = "\"RED\"";

	private const string FAMILY_PORTAL_URL = "k-id.com/code";

	private float _updateAttemptCooldown = 15f;

	private float _nextUpdateAttemptTime;

	private bool _waitingForUpdatedSession;

	private EKidScreenState _currentScreentState = EKidScreenState.Show_OTP;

	private string[] _interestedPermissionNames = new string[3] { "custom-username", "voice-chat", "join-groups" };

	private const string LANG_SCREEN_TITLE_KEY = "LANG_SCREEN_TITLE";

	private const string LANG_SCREEN_INSTRUCTIONS_KEY = "LANG_SCREEN_INSTRUCTIONS";

	private const string LANG_SCREEN_CURRENT_LANGUAGE_KEY = "LANG_SCREEN_CURRENT_LANGUAGE";

	private StringBuilder _languagesDisplaySB = new StringBuilder();

	private Locale _previousLocalisationSetting;

	public string versionMismatch
	{
		get
		{
			if (_lastLocaleChecked_Version != null && _lastLocaleChecked_Version == LocalisationManager.CurrentLanguage && !string.IsNullOrEmpty(_cachedVersionMismatch))
			{
				return _cachedVersionMismatch;
			}
			string defaultResult = "PLEASE UPDATE TO THE LATEST VERSION OF GORILLA TAG. YOU'RE ON AN OLD VERSION. FEEL FREE TO RUN AROUND, BUT YOU WON'T BE ABLE TO PLAY WITH ANYONE ELSE.";
			LocalisationManager.TryGetKeyForCurrentLocale("VERSION_MISMATCH", out var result, defaultResult);
			_lastLocaleChecked_Version = LocalisationManager.CurrentLanguage;
			_cachedVersionMismatch = result;
			return _cachedVersionMismatch;
		}
	}

	public string unableToConnect
	{
		get
		{
			if (_lastLocaleChecked_Connect != null && _lastLocaleChecked_Connect == LocalisationManager.CurrentLanguage && !string.IsNullOrEmpty(_cachedUnableToConnect))
			{
				return _cachedUnableToConnect;
			}
			string defaultResult = "UNABLE TO CONNECT TO THE INTERNET. PLEASE CHECK YOUR CONNECTION AND RESTART THE GAME.";
			LocalisationManager.TryGetKeyForCurrentLocale("CONNECTION_ISSUE", out var result, defaultResult);
			_lastLocaleChecked_Connect = LocalisationManager.CurrentLanguage;
			_cachedUnableToConnect = result;
			return _cachedUnableToConnect;
		}
	}

	public string[] allowedMapsToJoin
	{
		get
		{
			return _allowedMapsToJoin;
		}
		set
		{
			_allowedMapsToJoin = value;
		}
	}

	public string VStumpRoomPrepend => virtualStumpRoomPrepend;

	public ComputerState currentState
	{
		get
		{
			stateStack.TryPeek(out var result);
			return result;
		}
	}

	public string NameTagPlayerPref
	{
		get
		{
			if (PlayFabAuthenticator.instance == null)
			{
				Debug.LogError("Trying to access PlayFab Authenticator Instance, but it is null. Will use a shared key for the nametag instead");
				return "nameTagsOn";
			}
			return "nameTagsOn-" + PlayFabAuthenticator.instance.GetPlayFabPlayerId();
		}
	}

	public bool NametagsEnabled { get; private set; }

	public RedemptionResult RedemptionStatus
	{
		get
		{
			return redemptionResult;
		}
		set
		{
			redemptionResult = value;
			UpdateScreen();
		}
	}

	public string RedemptionCode
	{
		get
		{
			return redemptionCode;
		}
		set
		{
			redemptionCode = value;
		}
	}

	public DateTimeOffset? RedemptionRestrictionTime { get; set; }

	public DateTime GetServerTime()
	{
		return startupTime + TimeSpan.FromSeconds(Time.realtimeSinceStartup);
	}

	public void AddSeverTime(int m)
	{
		startupTime = startupTime.AddMinutes(m);
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
		Debug.Log("==== GORILLA TAG - VERSION: " + version + ", BUILD NUMBER: " + buildCode + ", BUILD DATE: " + buildDate + " ====\r\n.\r\n.               _______\r\n.              /       \\\r\n.             /  _____  \\\r\n.            / / _   _ \\ \\\r\n.           [ | (O) (O) | ]\r\n.            | \\  . .  / |\r\n.     _______|  | _._ |  |_______\r\n.    /        \\  \\___/  /        \\\r\n.\r\n.\r\n");
		_activeOrderList = OrderList;
		defaultUpdateCooldown = updateCooldown;
	}

	private void Start()
	{
		Debug.Log("Computer Init");
		Initialise();
	}

	public void OnEnable()
	{
		KIDManager.RegisterSessionUpdatedCallback_VoiceChat(SetVoiceChatBySafety);
		KIDManager.RegisterSessionUpdatedCallback_CustomUsernames(OnKIDSessionUpdated_CustomNicknames);
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void OnDisable()
	{
		KIDManager.UnregisterSessionUpdatedCallback_VoiceChat(SetVoiceChatBySafety);
		KIDManager.UnregisterSessionUpdatedCallback_CustomUsernames(OnKIDSessionUpdated_CustomNicknames);
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	protected void OnDestroy()
	{
		if (instance == this)
		{
			hasInstance = false;
			instance = null;
		}
		KIDManager.UnregisterSessionUpdateCallback_AnyPermission(OnSessionUpdate_GorillaComputer);
	}

	public void SliceUpdate()
	{
		if ((internetFailure && Time.realtimeSinceStartup < lastCheckedWifi + checkIfConnectedSeconds) || (!internetFailure && Time.realtimeSinceStartup < lastCheckedWifi + checkIfDisconnectedSeconds))
		{
			if (!internetFailure && isConnectedToMaster && Time.realtimeSinceStartup > lastUpdateTime + updateCooldown)
			{
				deltaTime = Time.realtimeSinceStartup - lastUpdateTime;
				lastUpdateTime = Time.realtimeSinceStartup;
				UpdateScreen();
			}
			return;
		}
		lastCheckedWifi = Time.realtimeSinceStartup;
		stateUpdated = false;
		if (!CheckInternetConnection())
		{
			string defaultResult = "NO WIFI OR LAN CONNECTION DETECTED.";
			LocalisationManager.TryGetKeyForCurrentLocale("NO_CONNECTION", out var result, defaultResult);
			UpdateFailureText(result);
			internetFailure = true;
		}
		else if (internetFailure)
		{
			if (CheckInternetConnection())
			{
				internetFailure = false;
			}
			RestoreFromFailureState();
			UpdateScreen();
		}
		else if (isConnectedToMaster && Time.realtimeSinceStartup > lastUpdateTime + updateCooldown)
		{
			deltaTime = Time.realtimeSinceStartup - lastUpdateTime;
			lastUpdateTime = Time.realtimeSinceStartup;
			UpdateScreen();
		}
	}

	private void Initialise()
	{
		GameEvents.OnGorrillaKeyboardButtonPressedEvent.AddListener(PressButton);
		RoomSystem.JoinedRoomEvent += new Action(OnFirstJoinedRoom_IncrementSessionCount);
		RoomSystem.JoinedRoomEvent += new Action(UpdateScreen);
		RoomSystem.LeftRoomEvent += new Action(UpdateScreen);
		RoomSystem.PlayerJoinedEvent += new Action<NetPlayer>(PlayerCountChangedCallback);
		RoomSystem.PlayerLeftEvent += new Action<NetPlayer>(PlayerCountChangedCallback);
		LocalisationManager.RegisterOnLanguageChanged(delegate
		{
			RefreshFunctionNames();
			UpdateGameModeText();
		});
		RefreshFunctionNames();
		InitialiseRoomScreens();
		InitialiseStrings();
		InitialiseAllRoomStates();
		UpdateScreen();
		byte[] bytes = new byte[1] { Convert.ToByte(64) };
		virtualStumpRoomPrepend = Encoding.ASCII.GetString(bytes);
		initialized = true;
	}

	private void InitialiseRoomScreens()
	{
		screenText.Initialize(computerScreenRenderer.materials, wrongVersionMaterial, GameEvents.ScreenTextChangedEvent, GameEvents.ScreenTextMaterialsEvent);
		functionSelectText.Initialize(computerScreenRenderer.materials, wrongVersionMaterial, GameEvents.FunctionSelectTextChangedEvent);
	}

	private void InitialiseStrings()
	{
		roomToJoin = "";
		redText = "";
		blueText = "";
		greenText = "";
		currentName = "";
		savedName = "";
	}

	private void InitialiseAllRoomStates()
	{
		SwitchState(ComputerState.Startup);
		InitialiseLanguageScreen();
		InitializeNameState();
		InitializeRoomState();
		InitializeTurnState();
		InitializeStartupState();
		InitializeQueueState();
		InitializeMicState();
		InitializeGroupState();
		InitializeVoiceState();
		InitializeAutoMuteState();
		InitializeGameMode();
		InitializeVisualsState();
		InitializeCreditsState();
		InitializeTimeState();
		InitializeSupportState();
		InitializeTroopState();
		InitializeKIdState();
		InitializeRedeemState();
	}

	private void InitializeStartupState()
	{
	}

	private void InitializeRoomState()
	{
	}

	private void InitializeColorState()
	{
		redValue = PlayerPrefs.GetFloat("redValue", 0f);
		greenValue = PlayerPrefs.GetFloat("greenValue", 0f);
		blueValue = PlayerPrefs.GetFloat("blueValue", 0f);
		blueText = Mathf.Floor(blueValue * 9f).ToString();
		redText = Mathf.Floor(redValue * 9f).ToString();
		greenText = Mathf.Floor(greenValue * 9f).ToString();
		colorCursorLine = 0;
		GorillaTagger.Instance.UpdateColor(redValue, greenValue, blueValue);
	}

	private void InitializeNameState()
	{
		int num = PlayerPrefs.GetInt("nameTagsOn", -1);
		Permission permissionDataByFeature = KIDManager.GetPermissionDataByFeature(EKIDFeatures.Custom_Nametags);
		switch (permissionDataByFeature.ManagedBy)
		{
		case Permission.ManagedByEnum.PLAYER:
			if (num == -1)
			{
				NametagsEnabled = permissionDataByFeature.Enabled;
			}
			else
			{
				NametagsEnabled = num > 0;
			}
			break;
		case Permission.ManagedByEnum.GUARDIAN:
			NametagsEnabled = permissionDataByFeature.Enabled && num > 0;
			break;
		case Permission.ManagedByEnum.PROHIBITED:
			NametagsEnabled = false;
			break;
		}
		savedName = PlayerPrefs.GetString("playerName", "gorilla");
		NetworkSystem.Instance.SetMyNickName(savedName);
		currentName = savedName;
		VRRigCache.Instance.localRig.Rig.UpdateName();
		exactOneWeek = exactOneWeekFile.text.Split('\n');
		anywhereOneWeek = anywhereOneWeekFile.text.Split('\n');
		anywhereTwoWeek = anywhereTwoWeekFile.text.Split('\n');
		for (int i = 0; i < exactOneWeek.Length; i++)
		{
			exactOneWeek[i] = exactOneWeek[i].ToLower().TrimEnd('\r', '\n');
		}
		for (int j = 0; j < anywhereOneWeek.Length; j++)
		{
			anywhereOneWeek[j] = anywhereOneWeek[j].ToLower().TrimEnd('\r', '\n');
		}
		for (int k = 0; k < anywhereTwoWeek.Length; k++)
		{
			anywhereTwoWeek[k] = anywhereTwoWeek[k].ToLower().TrimEnd('\r', '\n');
		}
	}

	private void InitializeTurnState()
	{
		GorillaSnapTurn.LoadSettingsFromPlayerPrefs();
	}

	private void InitializeMicState()
	{
		pttType = PlayerPrefs.GetString("pttType", "OPEN MIC");
		if (pttType == "ALL CHAT")
		{
			pttType = "OPEN MIC";
			PlayerPrefs.SetString("pttType", pttType);
			PlayerPrefs.Save();
		}
	}

	private void InitializeAutoMuteState()
	{
		switch (PlayerPrefs.GetInt("autoMute", 1))
		{
		case 0:
			autoMuteType = "OFF";
			break;
		case 1:
			autoMuteType = "MODERATE";
			break;
		case 2:
			autoMuteType = "AGGRESSIVE";
			break;
		}
	}

	private void InitializeQueueState()
	{
		currentQueue = PlayerPrefs.GetString("currentQueue", "DEFAULT");
		allowedInCompetitive = PlayerPrefs.GetInt("allowedInCompetitive", 0) == 1;
		if (!allowedInCompetitive && currentQueue == "COMPETITIVE")
		{
			PlayerPrefs.SetString("currentQueue", "DEFAULT");
			PlayerPrefs.Save();
			currentQueue = "DEFAULT";
		}
	}

	private void InitializeGroupState()
	{
		groupMapJoin = PlayerPrefs.GetString("groupMapJoin", "FOREST");
		groupMapJoinIndex = PlayerPrefs.GetInt("groupMapJoinIndex", 0);
		allowedMapsToJoin = friendJoinCollider.myAllowedMapsToJoin;
	}

	private void InitializeTroopState()
	{
		bool flag = false;
		troopToJoin = (troopName = PlayerPrefs.GetString("troopName", string.Empty));
		if (!rememberTroopQueueState)
		{
			bool num = PlayerPrefs.GetInt("troopQueueActive", 0) == 1;
			bool flag2 = currentQueue != "DEFAULT" && currentQueue != "COMPETITIVE" && currentQueue != "MINIGAMES";
			if (num || flag2)
			{
				currentQueue = "DEFAULT";
				PlayerPrefs.SetInt("troopQueueActive", 0);
				PlayerPrefs.SetString("currentQueue", currentQueue);
				PlayerPrefs.Save();
			}
		}
		troopQueueActive = PlayerPrefs.GetInt("troopQueueActive", 0) == 1;
		if (troopQueueActive && !IsValidTroopName(troopName))
		{
			troopQueueActive = false;
			PlayerPrefs.SetInt("troopQueueActive", troopQueueActive ? 1 : 0);
			currentQueue = "DEFAULT";
			PlayerPrefs.SetString("currentQueue", currentQueue);
			flag = true;
		}
		if (troopQueueActive)
		{
			StartCoroutine(HandleInitialTroopQueueState());
		}
		if (flag)
		{
			PlayerPrefs.Save();
		}
	}

	private IEnumerator HandleInitialTroopQueueState()
	{
		Debug.Log("HandleInitialTroopQueueState()");
		while (!PlayFabCloudScriptAPI.IsEntityLoggedIn())
		{
			yield return null;
		}
		RequestTroopPopulation();
		while (currentTroopPopulation < 0)
		{
			yield return null;
		}
		if (currentTroopPopulation < 2)
		{
			Debug.Log("Low population - starting in DEFAULT queue");
			JoinDefaultQueue();
		}
	}

	private void InitializeVoiceState()
	{
		Permission permissionDataByFeature = KIDManager.GetPermissionDataByFeature(EKIDFeatures.Voice_Chat);
		string text = PlayerPrefs.GetString("voiceChatOn", "");
		string defaultValue = "FALSE";
		switch (permissionDataByFeature.ManagedBy)
		{
		case Permission.ManagedByEnum.PLAYER:
			defaultValue = ((!string.IsNullOrEmpty(text)) ? text : (permissionDataByFeature.Enabled ? "TRUE" : "FALSE"));
			break;
		case Permission.ManagedByEnum.GUARDIAN:
			if (permissionDataByFeature.Enabled)
			{
				text = (string.IsNullOrEmpty(text) ? "FALSE" : text);
				defaultValue = text;
			}
			else
			{
				defaultValue = "FALSE";
			}
			break;
		case Permission.ManagedByEnum.PROHIBITED:
			defaultValue = "FALSE";
			break;
		}
		voiceChatOn = PlayerPrefs.GetString("voiceChatOn", defaultValue);
	}

	public void InitializeGameMode(string gameMode)
	{
		leftHanded = PlayerPrefs.GetInt("leftHanded", 0) == 1;
		OnModeSelectButtonPress(gameMode, leftHanded);
		GameModePages.SetSelectedGameModeShared(gameMode);
		didInitializeGameMode = true;
	}

	private void InitializeGameMode()
	{
		if (didInitializeGameMode)
		{
			return;
		}
		sessionCount = PlayerPrefs.GetInt("sessionCount", -1);
		string text = PlayerPrefs.GetString("currentGameModePostSI");
		if (sessionCount == -1)
		{
			sessionCount = ((text.Length != 0) ? 100 : 0);
			PlayerPrefs.SetInt("sessionCount", sessionCount);
			text = GameModeType.Infection.ToString();
			PlayerPrefs.SetString("currentGameModePostSI", text);
			PlayerPrefs.Save();
		}
		else if (sessionCount == 3)
		{
			sessionCount++;
			PlayerPrefs.SetInt("sessionCount", sessionCount);
			if (!text.StartsWith("Super"))
			{
				text = ((text == GameModeType.Casual.ToString()) ? GameModeType.SuperCasual.ToString() : GameModeType.SuperInfect.ToString());
				PlayerPrefs.SetString("currentGameModePostSI", text);
			}
			PlayerPrefs.Save();
		}
		GameModeType gameModeType;
		try
		{
			gameModeType = Enum.Parse<GameModeType>(text, ignoreCase: true);
		}
		catch
		{
			gameModeType = GameModeType.SuperInfect;
			text = GameModeType.SuperInfect.ToString();
		}
		if (!GameMode.GameModeZoneMapping.AllModes.Contains(gameModeType) || gameModeType == GameModeType.None || gameModeType == GameModeType.Count)
		{
			Debug.Log("[GT/GorillaComputer]  InitializeGameMode: Falling back to default game mode " + $"\"{GameModeType.SuperInfect}\" because stored game mode \"{gameModeType}\" is not available in any zone.");
			PlayerPrefs.SetString("currentGameModePostSI", GameModeType.SuperInfect.ToString());
			PlayerPrefs.Save();
			text = GameModeType.SuperInfect.ToString();
		}
		leftHanded = PlayerPrefs.GetInt("leftHanded", 0) == 1;
		OnModeSelectButtonPress(text, leftHanded);
		GameModePages.SetSelectedGameModeShared(text);
	}

	private void InitializeCreditsState()
	{
	}

	private void InitializeTimeState()
	{
		BetterDayNightManager.instance.currentSetting = TimeSettings.Normal;
	}

	private void InitializeSupportState()
	{
		displaySupport = false;
	}

	private void InitializeVisualsState()
	{
		disableParticles = PlayerPrefs.GetString("disableParticles", "FALSE") == "TRUE";
		GorillaTagger.Instance.ShowCosmeticParticles(!disableParticles);
		instrumentVolume = PlayerPrefs.GetFloat("instrumentVolume", 0.1f);
	}

	private void InitializeRedeemState()
	{
		RedemptionStatus = RedemptionResult.Empty;
	}

	private bool CheckInternetConnection()
	{
		return Application.internetReachability != NetworkReachability.NotReachable;
	}

	public void OnConnectedToMasterStuff()
	{
		if (!isConnectedToMaster)
		{
			isConnectedToMaster = true;
			GorillaServer.Instance.ReturnCurrentVersion(new ReturnCurrentVersionRequest
			{
				CurrentVersion = NetworkSystemConfig.AppVersionStripped,
				UpdatedSynchTest = includeUpdatedServerSynchTest
			}, OnReturnCurrentVersion, OnErrorShared);
			if (startupMillis == 0L && !tryGetTimeAgain)
			{
				GetCurrentTime();
			}
			bool safety = PlayFabAuthenticator.instance.GetSafety();
			if (!KIDManager.KidEnabledAndReady && !KIDManager.HasSession)
			{
				SetComputerSettingsBySafety(safety, new ComputerState[4]
				{
					ComputerState.Voice,
					ComputerState.AutoMute,
					ComputerState.Name,
					ComputerState.Group
				}, shouldHide: false);
			}
		}
	}

	private void OnReturnCurrentVersion(ExecuteFunctionResult result)
	{
		JsonObject jsonObject = (JsonObject)result.FunctionResult;
		if (jsonObject != null)
		{
			if (jsonObject.TryGetValue("SynchTime", out var value))
			{
				Debug.Log("message value is: " + (string)value);
			}
			if (jsonObject.TryGetValue("Fail", out value) && (bool)value)
			{
				GeneralFailureMessage(versionMismatch);
				return;
			}
			if (jsonObject.TryGetValue("ResultCode", out value) && (ulong)value != 0L)
			{
				GeneralFailureMessage(versionMismatch);
				return;
			}
			if (jsonObject.TryGetValue("QueueStats", out value) && ((JsonObject)value).TryGetValue("TopTroops", out value))
			{
				topTroops.Clear();
				foreach (object item in (JsonArray)value)
				{
					topTroops.Add(item.ToString());
				}
			}
			if (jsonObject.TryGetValue("BannedUsers", out value))
			{
				usersBanned = int.Parse((string)value);
			}
			UpdateScreen();
		}
		else
		{
			GeneralFailureMessage(versionMismatch);
		}
	}

	public void PressButton(GorillaKeyboardBindings buttonPressed)
	{
		if (currentState == ComputerState.Startup)
		{
			ProcessStartupState(buttonPressed);
			UpdateScreen();
			return;
		}
		RequestTroopPopulation();
		bool flag = true;
		switch (buttonPressed)
		{
		case GorillaKeyboardBindings.up:
			flag = false;
			DecreaseState();
			break;
		case GorillaKeyboardBindings.down:
			flag = false;
			IncreaseState();
			break;
		}
		if (flag)
		{
			switch (currentState)
			{
			case ComputerState.Language:
				ProcessLanguageState(buttonPressed);
				break;
			case ComputerState.Room:
				ProcessRoomState(buttonPressed);
				break;
			case ComputerState.Name:
				ProcessNameState(buttonPressed);
				break;
			case ComputerState.Turn:
				ProcessTurnState(buttonPressed);
				break;
			case ComputerState.Mic:
				ProcessMicState(buttonPressed);
				break;
			case ComputerState.Queue:
				ProcessQueueState(buttonPressed);
				break;
			case ComputerState.Group:
				ProcessGroupState(buttonPressed);
				break;
			case ComputerState.Voice:
				ProcessVoiceState(buttonPressed);
				break;
			case ComputerState.AutoMute:
				ProcessAutoMuteState(buttonPressed);
				break;
			case ComputerState.Credits:
				ProcessCreditsState(buttonPressed);
				break;
			case ComputerState.Support:
				ProcessSupportState(buttonPressed);
				break;
			case ComputerState.Visuals:
				ProcessVisualsState(buttonPressed);
				break;
			case ComputerState.NameWarning:
				ProcessNameWarningState(buttonPressed);
				break;
			case ComputerState.Troop:
				ProcessTroopState(buttonPressed);
				break;
			case ComputerState.KID:
				ProcessKIdState(buttonPressed);
				break;
			case ComputerState.Redemption:
				ProcessRedemptionState(buttonPressed);
				break;
			}
		}
		UpdateScreen();
	}

	public void OnModeSelectButtonPress(string gameMode, bool leftHand)
	{
		lastPressedGameMode = gameMode;
		lastPressedGameModeType = (GameModeType)GameMode.gameModeKeyByName.GetValueOrDefault(gameMode, 11);
		PlayerPrefs.SetString("currentGameModePostSI", gameMode);
		if (leftHand != leftHanded)
		{
			PlayerPrefs.SetInt("leftHanded", leftHand ? 1 : 0);
			leftHanded = leftHand;
		}
		PlayerPrefs.Save();
		if (FriendshipGroupDetection.Instance.IsInParty)
		{
			FriendshipGroupDetection.Instance.SendRequestPartyGameMode(gameMode);
		}
		else
		{
			SetGameModeWithoutButton(gameMode);
		}
	}

	public void SetGameModeWithoutButton(string gameMode)
	{
		currentGameMode.Value = gameMode;
		UpdateGameModeText();
		PhotonNetworkController.Instance.UpdateTriggerScreens();
	}

	public void RegisterPrimaryJoinTrigger(GorillaNetworkJoinTrigger trigger)
	{
		primaryTriggersByZone[trigger.networkZone] = trigger;
	}

	private GorillaNetworkJoinTrigger GetSelectedMapJoinTrigger()
	{
		primaryTriggersByZone.TryGetValue(allowedMapsToJoin[Mathf.Min(allowedMapsToJoin.Length - 1, groupMapJoinIndex)], out var value);
		return value;
	}

	public GorillaNetworkJoinTrigger GetJoinTriggerForZone(string zone)
	{
		primaryTriggersByZone.TryGetValue(zone, out var value);
		return value;
	}

	public GorillaNetworkJoinTrigger GetJoinTriggerFromFullGameModeString(string gameModeString)
	{
		foreach (KeyValuePair<string, GorillaNetworkJoinTrigger> item in primaryTriggersByZone)
		{
			if (gameModeString.StartsWith(item.Key))
			{
				return item.Value;
			}
		}
		return null;
	}

	public void OnGroupJoinButtonPress(int mapJoinIndex, GorillaFriendCollider chosenFriendJoinCollider)
	{
		Debug.Log("On Group button press. Map:" + mapJoinIndex + " - collider: " + chosenFriendJoinCollider.name);
		if (mapJoinIndex >= allowedMapsToJoin.Length)
		{
			roomNotAllowed = true;
			currentStateIndex = 0;
			SwitchState(GetState(currentStateIndex));
			return;
		}
		GorillaNetworkJoinTrigger selectedMapJoinTrigger = GetSelectedMapJoinTrigger();
		if (FriendshipGroupDetection.Instance.IsInParty)
		{
			if (selectedMapJoinTrigger != null && selectedMapJoinTrigger.CanPartyJoin())
			{
				PhotonNetworkController.Instance.AttemptToJoinPublicRoom(selectedMapJoinTrigger, JoinType.ForceJoinWithParty);
				currentStateIndex = 0;
				SwitchState(GetState(currentStateIndex));
			}
			else
			{
				UpdateScreen();
			}
		}
		else
		{
			if (!NetworkSystem.Instance.InRoom || !NetworkSystem.Instance.SessionIsPrivate)
			{
				return;
			}
			PhotonNetworkController.Instance.FriendIDList = new List<string>(chosenFriendJoinCollider.playerIDsCurrentlyTouching);
			foreach (string friendID in networkController.FriendIDList)
			{
				Debug.Log("Friend ID:" + friendID);
			}
			PhotonNetworkController.Instance.shuffler = UnityEngine.Random.Range(0, 99).ToString().PadLeft(2, '0') + UnityEngine.Random.Range(0, 99999999).ToString().PadLeft(8, '0');
			PhotonNetworkController.Instance.keyStr = UnityEngine.Random.Range(0, 99999999).ToString().PadLeft(8, '0');
			RoomSystem.SendNearbyFollowCommand(chosenFriendJoinCollider, PhotonNetworkController.Instance.shuffler, PhotonNetworkController.Instance.keyStr);
			PhotonNetwork.SendAllOutgoingCommands();
			PhotonNetworkController.Instance.AttemptToJoinPublicRoom(selectedMapJoinTrigger, JoinType.JoinWithNearby);
			currentStateIndex = 0;
			SwitchState(GetState(currentStateIndex));
		}
	}

	public void CompQueueUnlockButtonPress()
	{
		allowedInCompetitive = true;
		PlayerPrefs.SetInt("allowedInCompetitive", 1);
		PlayerPrefs.Save();
		if (RankedProgressionManager.Instance != null)
		{
			RankedProgressionManager.Instance.RequestUnlockCompetitiveQueue(unlock: true);
		}
	}

	private void SwitchState(ComputerState newState, bool clearStack = true)
	{
		if (currentComputerState == ComputerState.Mic && currentComputerState != newState)
		{
			updateCooldown = defaultUpdateCooldown;
		}
		else if (newState == ComputerState.Mic)
		{
			updateCooldown = micUpdateCooldown;
		}
		if (previousComputerState != currentComputerState)
		{
			previousComputerState = currentComputerState;
		}
		currentComputerState = newState;
		if (LoadingRoutine != null)
		{
			StopCoroutine(LoadingRoutine);
		}
		if (clearStack)
		{
			stateStack.Clear();
		}
		stateStack.Push(newState);
	}

	private void PopState()
	{
		currentComputerState = previousComputerState;
		if (stateStack.Count <= 1)
		{
			Debug.LogError("Can't pop into an empty stack");
			return;
		}
		stateStack.Pop();
		UpdateScreen();
	}

	private void SwitchToWarningState()
	{
		warningConfirmationInputString = string.Empty;
		SwitchState(ComputerState.NameWarning, clearStack: false);
	}

	private void SwitchToLoadingState()
	{
		SwitchState(ComputerState.Loading, clearStack: false);
	}

	private void ProcessStartupState(GorillaKeyboardBindings buttonPressed)
	{
		SwitchState(GetState(currentStateIndex));
	}

	private void ProcessColorState(GorillaKeyboardBindings buttonPressed)
	{
		switch (buttonPressed)
		{
		case GorillaKeyboardBindings.option1:
			colorCursorLine = 0;
			return;
		case GorillaKeyboardBindings.option2:
			colorCursorLine = 1;
			return;
		case GorillaKeyboardBindings.option3:
			colorCursorLine = 2;
			return;
		case GorillaKeyboardBindings.enter:
			return;
		}
		int num = (int)buttonPressed;
		if (num < 10)
		{
			switch (colorCursorLine)
			{
			case 0:
				redText = num.ToString();
				redValue = (float)num / 9f;
				PlayerPrefs.SetFloat("redValue", redValue);
				break;
			case 1:
				greenText = num.ToString();
				greenValue = (float)num / 9f;
				PlayerPrefs.SetFloat("greenValue", greenValue);
				break;
			case 2:
				blueText = num.ToString();
				blueValue = (float)num / 9f;
				PlayerPrefs.SetFloat("blueValue", blueValue);
				break;
			}
			GorillaTagger.Instance.UpdateColor(redValue, greenValue, blueValue);
			PlayerPrefs.Save();
			if (NetworkSystem.Instance.InRoom)
			{
				GorillaTagger.Instance.myVRRig.SendRPC("RPC_InitializeNoobMaterial", RpcTarget.All, redValue, greenValue, blueValue);
			}
		}
	}

	public void ProcessNameState(GorillaKeyboardBindings buttonPressed)
	{
		if (!KIDManager.HasPermissionToUseFeature(EKIDFeatures.Custom_Nametags))
		{
			return;
		}
		switch (buttonPressed)
		{
		case GorillaKeyboardBindings.enter:
			if (currentName != savedName && currentName != "" && NametagsEnabled)
			{
				CheckAutoBanListForPlayerName(currentName);
			}
			return;
		case GorillaKeyboardBindings.delete:
			if (currentName.Length > 0 && NametagsEnabled)
			{
				currentName = currentName.Substring(0, currentName.Length - 1);
			}
			return;
		case GorillaKeyboardBindings.option1:
			UpdateNametagSetting(!NametagsEnabled);
			return;
		}
		if (NametagsEnabled && currentName.Length < 12 && (buttonPressed < GorillaKeyboardBindings.up || buttonPressed > GorillaKeyboardBindings.option3))
		{
			string text = currentName;
			string text2;
			if (buttonPressed >= GorillaKeyboardBindings.up)
			{
				text2 = buttonPressed.ToString();
			}
			else
			{
				int num = (int)buttonPressed;
				text2 = num.ToString();
			}
			currentName = text + text2;
		}
	}

	private void ProcessRoomState(GorillaKeyboardBindings buttonPressed)
	{
		if (limitOnlineScreens)
		{
			return;
		}
		bool flag = KIDManager.HasPermissionToUseFeature(EKIDFeatures.Groups) && KIDManager.HasPermissionToUseFeature(EKIDFeatures.Multiplayer);
		switch (buttonPressed)
		{
		case GorillaKeyboardBindings.option1:
			if (FriendshipGroupDetection.Instance.IsInParty)
			{
				FriendshipGroupDetection.Instance.LeaveParty();
				DisconnectAfterDelay(1f);
			}
			else
			{
				NetworkSystem.Instance.ReturnToSinglePlayer();
			}
			return;
		case GorillaKeyboardBindings.option2:
			RequestUpdatedPermissions();
			return;
		case GorillaKeyboardBindings.enter:
			if (flag && ((!playerInVirtualStump && roomToJoin != "") || (playerInVirtualStump && roomToJoin.Length > 1)))
			{
				CheckAutoBanListForRoomName(roomToJoin);
			}
			return;
		case GorillaKeyboardBindings.delete:
			if (flag && ((playerInVirtualStump && roomToJoin.Length > 1) || (!playerInVirtualStump && roomToJoin.Length > 0)))
			{
				roomToJoin = roomToJoin.Substring(0, roomToJoin.Length - 1);
			}
			return;
		case GorillaKeyboardBindings.option3:
			return;
		}
		if (flag && roomToJoin.Length < 10)
		{
			string text = roomToJoin;
			string text2;
			if (buttonPressed >= GorillaKeyboardBindings.up)
			{
				text2 = buttonPressed.ToString();
			}
			else
			{
				int num = (int)buttonPressed;
				text2 = num.ToString();
			}
			roomToJoin = text + text2;
		}
	}

	private async void DisconnectAfterDelay(float seconds)
	{
		await Task.Delay((int)(1000f * seconds));
		await NetworkSystem.Instance.ReturnToSinglePlayer();
	}

	private void ProcessTurnState(GorillaKeyboardBindings buttonPressed)
	{
		if (buttonPressed < GorillaKeyboardBindings.up)
		{
			GorillaSnapTurn.UpdateAndSaveTurnFactor((int)buttonPressed);
			return;
		}
		string text = string.Empty;
		switch (buttonPressed)
		{
		case GorillaKeyboardBindings.option1:
			text = "SNAP";
			break;
		case GorillaKeyboardBindings.option2:
			text = "SMOOTH";
			break;
		case GorillaKeyboardBindings.option3:
			text = "NONE";
			break;
		}
		if (text.Length > 0)
		{
			GorillaSnapTurn.UpdateAndSaveTurnType(text);
		}
	}

	private void ProcessMicState(GorillaKeyboardBindings buttonPressed)
	{
		switch (buttonPressed)
		{
		case GorillaKeyboardBindings.option1:
			pttType = "OPEN MIC";
			PlayerPrefs.SetString("pttType", pttType);
			PlayerPrefs.Save();
			break;
		case GorillaKeyboardBindings.option2:
			pttType = "PUSH TO TALK";
			PlayerPrefs.SetString("pttType", pttType);
			PlayerPrefs.Save();
			break;
		case GorillaKeyboardBindings.option3:
			pttType = "PUSH TO MUTE";
			PlayerPrefs.SetString("pttType", pttType);
			PlayerPrefs.Save();
			break;
		}
	}

	private void ProcessQueueState(GorillaKeyboardBindings buttonPressed)
	{
		if (limitOnlineScreens)
		{
			return;
		}
		switch (buttonPressed)
		{
		case GorillaKeyboardBindings.option1:
			JoinQueue("DEFAULT");
			break;
		case GorillaKeyboardBindings.option2:
			JoinQueue("MINIGAMES");
			break;
		case GorillaKeyboardBindings.option3:
			if (allowedInCompetitive)
			{
				JoinQueue("COMPETITIVE");
			}
			break;
		}
	}

	public void JoinTroop(string newTroopName)
	{
		if (IsValidTroopName(newTroopName))
		{
			currentTroopPopulation = -1;
			troopName = newTroopName;
			PlayerPrefs.SetString("troopName", troopName);
			if (troopQueueActive)
			{
				currentQueue = GetQueueNameForTroop(troopName);
				PlayerPrefs.SetString("currentQueue", currentQueue);
			}
			PlayerPrefs.Save();
			JoinTroopQueue();
		}
	}

	public void JoinTroopQueue()
	{
		if (IsValidTroopName(troopName))
		{
			currentTroopPopulation = -1;
			JoinQueue(GetQueueNameForTroop(troopName), isTroopQueue: true);
			RequestTroopPopulation(forceUpdate: true);
		}
	}

	private void RequestTroopPopulation(bool forceUpdate = false)
	{
		if (!PlayFabCloudScriptAPI.IsEntityLoggedIn() || !(!hasRequestedInitialTroopPopulation || forceUpdate) || nextPopulationCheckTime > Time.realtimeSinceStartup)
		{
			return;
		}
		nextPopulationCheckTime = Time.realtimeSinceStartup + troopPopulationCheckCooldown;
		hasRequestedInitialTroopPopulation = true;
		GorillaServer.Instance.ReturnQueueStats(new ReturnQueueStatsRequest
		{
			queueName = troopName
		}, delegate(ExecuteFunctionResult result)
		{
			Debug.Log("Troop pop received");
			if (((JsonObject)result.FunctionResult).TryGetValue("PlayerCount", out var value))
			{
				currentTroopPopulation = int.Parse(value.ToString());
				if (currentComputerState == ComputerState.Queue)
				{
					UpdateScreen();
				}
			}
			else
			{
				currentTroopPopulation = 0;
			}
		}, delegate(PlayFabError error)
		{
			Debug.LogError($"Error requesting troop population: {error}");
			currentTroopPopulation = -1;
		});
	}

	public void JoinDefaultQueue()
	{
		JoinQueue("DEFAULT");
	}

	public void LeaveTroop()
	{
		if (IsValidTroopName(troopName))
		{
			troopToJoin = troopName;
		}
		currentTroopPopulation = -1;
		troopName = string.Empty;
		PlayerPrefs.SetString("troopName", troopName);
		if (troopQueueActive)
		{
			JoinDefaultQueue();
		}
		PlayerPrefs.Save();
	}

	public string GetCurrentTroop()
	{
		if (troopQueueActive)
		{
			return troopName;
		}
		return currentQueue;
	}

	public int GetCurrentTroopPopulation()
	{
		if (troopQueueActive)
		{
			return currentTroopPopulation;
		}
		return -1;
	}

	private void JoinQueue(string queueName, bool isTroopQueue = false)
	{
		currentQueue = queueName;
		troopQueueActive = isTroopQueue;
		currentTroopPopulation = -1;
		PlayerPrefs.SetString("currentQueue", currentQueue);
		PlayerPrefs.SetInt("troopQueueActive", troopQueueActive ? 1 : 0);
		PlayerPrefs.Save();
	}

	private void ProcessGroupState(GorillaKeyboardBindings buttonPressed)
	{
		if (!limitOnlineScreens)
		{
			switch (buttonPressed)
			{
			case GorillaKeyboardBindings.one:
				groupMapJoin = "FOREST";
				groupMapJoinIndex = 0;
				PlayerPrefs.SetString("groupMapJoin", groupMapJoin);
				PlayerPrefs.SetInt("groupMapJoinIndex", groupMapJoinIndex);
				PlayerPrefs.Save();
				break;
			case GorillaKeyboardBindings.two:
				groupMapJoin = "CAVE";
				groupMapJoinIndex = 1;
				PlayerPrefs.SetString("groupMapJoin", groupMapJoin);
				PlayerPrefs.SetInt("groupMapJoinIndex", groupMapJoinIndex);
				PlayerPrefs.Save();
				break;
			case GorillaKeyboardBindings.three:
				groupMapJoin = "CANYON";
				groupMapJoinIndex = 2;
				PlayerPrefs.SetString("groupMapJoin", groupMapJoin);
				PlayerPrefs.SetInt("groupMapJoinIndex", groupMapJoinIndex);
				PlayerPrefs.Save();
				break;
			case GorillaKeyboardBindings.four:
				groupMapJoin = "CITY";
				groupMapJoinIndex = 3;
				PlayerPrefs.SetString("groupMapJoin", groupMapJoin);
				PlayerPrefs.SetInt("groupMapJoinIndex", groupMapJoinIndex);
				PlayerPrefs.Save();
				break;
			case GorillaKeyboardBindings.five:
				groupMapJoin = "CLOUDS";
				groupMapJoinIndex = 4;
				PlayerPrefs.SetString("groupMapJoin", groupMapJoin);
				PlayerPrefs.SetInt("groupMapJoinIndex", groupMapJoinIndex);
				PlayerPrefs.Save();
				break;
			case GorillaKeyboardBindings.enter:
				OnGroupJoinButtonPress(Mathf.Min(allowedMapsToJoin.Length - 1, groupMapJoinIndex), friendJoinCollider);
				break;
			}
			roomFull = false;
		}
	}

	private void ProcessTroopState(GorillaKeyboardBindings buttonPressed)
	{
		if (limitOnlineScreens)
		{
			return;
		}
		bool num = KIDManager.HasPermissionToUseFeature(EKIDFeatures.Groups);
		bool flag = IsValidTroopName(troopName);
		if (num)
		{
			switch (buttonPressed)
			{
			case GorillaKeyboardBindings.option1:
				JoinTroopQueue();
				return;
			case GorillaKeyboardBindings.option2:
				JoinDefaultQueue();
				return;
			case GorillaKeyboardBindings.option3:
				LeaveTroop();
				return;
			case GorillaKeyboardBindings.enter:
				if (!flag)
				{
					CheckAutoBanListForTroopName(troopToJoin);
				}
				return;
			case GorillaKeyboardBindings.delete:
				if (!flag && troopToJoin.Length > 0)
				{
					troopToJoin = troopToJoin.Substring(0, troopToJoin.Length - 1);
				}
				return;
			}
			if (!flag && troopToJoin.Length < 12)
			{
				string text = troopToJoin;
				string text2;
				if (buttonPressed >= GorillaKeyboardBindings.up)
				{
					text2 = buttonPressed.ToString();
				}
				else
				{
					int num2 = (int)buttonPressed;
					text2 = num2.ToString();
				}
				troopToJoin = text + text2;
			}
			return;
		}
		switch (buttonPressed)
		{
		case GorillaKeyboardBindings.option2:
			if (_currentScreentState != EKidScreenState.Ready)
			{
				ProcessScreen_SetupKID();
			}
			else
			{
				RequestUpdatedPermissions();
			}
			break;
		case GorillaKeyboardBindings.option3:
			if (_currentScreentState == EKidScreenState.Show_OTP)
			{
				ProcessScreen_SetupKID();
			}
			break;
		case GorillaKeyboardBindings.option1:
			break;
		}
	}

	private bool IsValidTroopName(string troop)
	{
		if (!string.IsNullOrEmpty(troop) && troop.Length <= 12)
		{
			if (!allowedInCompetitive)
			{
				return troop != "COMPETITIVE";
			}
			return true;
		}
		return false;
	}

	private string GetQueueNameForTroop(string troop)
	{
		return troop;
	}

	private void ProcessVoiceState(GorillaKeyboardBindings buttonPressed)
	{
		if (KIDManager.HasPermissionToUseFeature(EKIDFeatures.Voice_Chat))
		{
			switch (buttonPressed)
			{
			case GorillaKeyboardBindings.option1:
				SetVoice(setting: true);
				break;
			case GorillaKeyboardBindings.option2:
				SetVoice(setting: false);
				break;
			}
		}
		else
		{
			switch (buttonPressed)
			{
			case GorillaKeyboardBindings.option2:
				if (_currentScreentState != EKidScreenState.Ready)
				{
					ProcessScreen_SetupKID();
				}
				else
				{
					RequestUpdatedPermissions();
				}
				break;
			case GorillaKeyboardBindings.option3:
				if (_currentScreentState != EKidScreenState.Show_OTP)
				{
					return;
				}
				ProcessScreen_SetupKID();
				break;
			}
		}
		RigContainer.RefreshAllRigVoices();
	}

	private void ProcessAutoMuteState(GorillaKeyboardBindings buttonPressed)
	{
		switch (buttonPressed)
		{
		case GorillaKeyboardBindings.option1:
			autoMuteType = "AGGRESSIVE";
			PlayerPrefs.SetInt("autoMute", 2);
			PlayerPrefs.Save();
			RigContainer.RefreshAllRigVoices();
			break;
		case GorillaKeyboardBindings.option2:
			autoMuteType = "MODERATE";
			PlayerPrefs.SetInt("autoMute", 1);
			PlayerPrefs.Save();
			RigContainer.RefreshAllRigVoices();
			break;
		case GorillaKeyboardBindings.option3:
			autoMuteType = "OFF";
			PlayerPrefs.SetInt("autoMute", 0);
			PlayerPrefs.Save();
			RigContainer.RefreshAllRigVoices();
			break;
		}
		UpdateScreen();
	}

	private void ProcessVisualsState(GorillaKeyboardBindings buttonPressed)
	{
		if (buttonPressed < GorillaKeyboardBindings.up)
		{
			instrumentVolume = (float)buttonPressed / 50f;
			PlayerPrefs.SetFloat("instrumentVolume", instrumentVolume);
			PlayerPrefs.Save();
			return;
		}
		switch (buttonPressed)
		{
		case GorillaKeyboardBindings.option1:
			disableParticles = false;
			PlayerPrefs.SetString("disableParticles", "FALSE");
			PlayerPrefs.Save();
			GorillaTagger.Instance.ShowCosmeticParticles(!disableParticles);
			break;
		case GorillaKeyboardBindings.option2:
			disableParticles = true;
			PlayerPrefs.SetString("disableParticles", "TRUE");
			PlayerPrefs.Save();
			GorillaTagger.Instance.ShowCosmeticParticles(!disableParticles);
			break;
		case GorillaKeyboardBindings.option3:
			break;
		}
	}

	private void ProcessCreditsState(GorillaKeyboardBindings buttonPressed)
	{
		if (buttonPressed == GorillaKeyboardBindings.enter)
		{
			creditsView.ProcessButtonPress(buttonPressed);
		}
	}

	private void ProcessSupportState(GorillaKeyboardBindings buttonPressed)
	{
		if (buttonPressed == GorillaKeyboardBindings.enter)
		{
			displaySupport = true;
		}
	}

	private void ProcessRedemptionState(GorillaKeyboardBindings buttonPressed)
	{
		if (RedemptionStatus == RedemptionResult.Checking)
		{
			return;
		}
		switch (buttonPressed)
		{
		case GorillaKeyboardBindings.enter:
			if (redemptionCode != "")
			{
				if (redemptionCode.Length < 8)
				{
					RedemptionStatus = RedemptionResult.Invalid;
					return;
				}
				CodeRedemption.Instance.HandleCodeRedemption(redemptionCode);
				RedemptionStatus = RedemptionResult.Checking;
			}
			else if (RedemptionStatus != RedemptionResult.Success)
			{
				RedemptionStatus = RedemptionResult.Empty;
			}
			return;
		case GorillaKeyboardBindings.delete:
			if (redemptionCode.Length > 0)
			{
				redemptionCode = redemptionCode.Substring(0, redemptionCode.Length - 1);
			}
			return;
		}
		if (redemptionCode.Length < 8 && (buttonPressed < GorillaKeyboardBindings.up || buttonPressed > GorillaKeyboardBindings.option3))
		{
			string text = redemptionCode;
			string text2;
			if (buttonPressed >= GorillaKeyboardBindings.up)
			{
				text2 = buttonPressed.ToString();
			}
			else
			{
				int num = (int)buttonPressed;
				text2 = num.ToString();
			}
			redemptionCode = text + text2;
		}
	}

	private void ProcessNameWarningState(GorillaKeyboardBindings buttonPressed)
	{
		if (warningConfirmationInputString.ToLower() == "yes")
		{
			PopState();
		}
		else if (buttonPressed == GorillaKeyboardBindings.delete)
		{
			if (warningConfirmationInputString.Length > 0)
			{
				warningConfirmationInputString = warningConfirmationInputString.Substring(0, warningConfirmationInputString.Length - 1);
			}
		}
		else if (warningConfirmationInputString.Length < 3)
		{
			warningConfirmationInputString += buttonPressed;
		}
	}

	public void UpdateScreen()
	{
		if (NetworkSystem.Instance != null && !NetworkSystem.Instance.WrongVersion)
		{
			UpdateFunctionScreen();
			switch (currentState)
			{
			case ComputerState.Startup:
				StartupScreen();
				break;
			case ComputerState.Language:
				LanguageScreen();
				break;
			case ComputerState.Room:
				RoomScreen();
				break;
			case ComputerState.Name:
				NameScreen();
				break;
			case ComputerState.Turn:
				TurnScreen();
				break;
			case ComputerState.Queue:
				QueueScreen();
				break;
			case ComputerState.Mic:
				MicScreen();
				break;
			case ComputerState.Group:
				GroupScreen();
				break;
			case ComputerState.Voice:
				VoiceScreen();
				break;
			case ComputerState.AutoMute:
				AutomuteScreen();
				break;
			case ComputerState.Visuals:
				VisualsScreen();
				break;
			case ComputerState.Credits:
				CreditsScreen();
				break;
			case ComputerState.Time:
				TimeScreen();
				break;
			case ComputerState.Support:
				SupportScreen();
				break;
			case ComputerState.NameWarning:
				NameWarningScreen();
				break;
			case ComputerState.Loading:
				LoadingScreen();
				break;
			case ComputerState.Troop:
				TroopScreen();
				break;
			case ComputerState.KID:
				KIdScreen();
				break;
			case ComputerState.Redemption:
				RedemptionScreen();
				break;
			}
		}
		UpdateGameModeText();
	}

	private void LoadingScreen()
	{
		string defaultResult = "LOADING";
		LocalisationManager.TryGetKeyForCurrentLocale("LOADING_SCREEN", out var result, defaultResult);
		screenText.Set(result);
		LoadingRoutine = StartCoroutine(LoadingScreenLocal());
		IEnumerator LoadingScreenLocal()
		{
			int dotsCount = 0;
			while (currentState == ComputerState.Loading)
			{
				dotsCount++;
				if (dotsCount == 3)
				{
					screenText.Set(result);
					dotsCount = 0;
				}
				for (int i = 0; i < dotsCount; i++)
				{
					screenText.Append(". ");
				}
				yield return waitOneSecond;
			}
		}
	}

	private void NameWarningScreen()
	{
		string defaultResult = "<color=red>WARNING: PLEASE CHOOSE A BETTER NAME\n\nENTERING ANOTHER BAD NAME WILL RESULT IN A BAN</color>";
		LocalisationManager.TryGetKeyForCurrentLocale("WARNING_SCREEN", out var result, defaultResult);
		screenText.Set(result);
		if (warningConfirmationInputString.ToLower() == "yes")
		{
			defaultResult = "\n\nPRESS ANY KEY TO CONTINUE";
			LocalisationManager.TryGetKeyForCurrentLocale("WARNING_SCREEN_CONFIRMATION", out result, defaultResult);
			screenText.Append(result);
		}
		else
		{
			defaultResult = "\n\nTYPE 'YES' TO CONFIRM:";
			LocalisationManager.TryGetKeyForCurrentLocale("WARNING_SCREEN_TYPE_YES", out result, defaultResult);
			screenText.Append(result.TrailingSpace());
			screenText.Append(warningConfirmationInputString);
		}
	}

	private void SupportScreen()
	{
		screenText.Set("");
		if (displaySupport)
		{
			string text = PlayFabAuthenticator.instance.platform.ToString().ToUpper();
			string text2 = ((!(text == "PC")) ? text : "OCULUS PC");
			text = text2;
			string text3 = "";
			LocalisationManager.TryGetKeyForCurrentLocale(text switch
			{
				"OCULUS PC" => "PLATFORM_OCULUS_PC", 
				"STEAM" => "PLATFORM_STEAM", 
				"PSVR" => "PLATFORM_PSVR", 
				"PICO" => "PLATFORM_PICO", 
				"QUEST" => "PLATFORM_QUEST", 
				_ => "UNKNOWN_PLATFORM", 
			}, out var result, text);
			text = result;
			string defaultResult = "SUPPORT";
			LocalisationManager.TryGetKeyForCurrentLocale("SUPPORT_SCREEN_INTRO", out result, defaultResult);
			screenText.Append(result);
			defaultResult = "\n\nPLAYER ID";
			LocalisationManager.TryGetKeyForCurrentLocale("SUPPORT_SCREEN_DETAILS_PLAYERID", out result, defaultResult);
			screenText.Append(result + "  ");
			screenText.Append(PlayFabAuthenticator.instance.GetPlayFabPlayerId());
			defaultResult = "\nVERSION";
			LocalisationManager.TryGetKeyForCurrentLocale("SUPPORT_SCREEN_DETAILS_VERSION", out result, defaultResult);
			screenText.Append(result + " ");
			screenText.Append(version.ToUpper());
			defaultResult = "\nPLATFORM";
			LocalisationManager.TryGetKeyForCurrentLocale("SUPPORT_SCREEN_DETAILS_PLATFORM", out result, defaultResult);
			screenText.Append(result + " ");
			screenText.Append(text);
			defaultResult = "\nBUILD DATE";
			LocalisationManager.TryGetKeyForCurrentLocale("SUPPORT_SCREEN_DETAILS_BUILD_DATE", out result, defaultResult);
			screenText.Append(result + " ");
			screenText.Append(buildDate);
			defaultResult = "\nSESSION ID";
			LocalisationManager.TryGetKeyForCurrentLocale("SUPPORT_SCREEN_DETAILS_MOTHERSHIP_SESSION_ID", out result, defaultResult);
			string sessionId = MothershipClientApiUnity.SessionId;
			string str = sessionId;
			int num = sessionId.LastIndexOf('-');
			if (num >= 0)
			{
				string text4 = sessionId.Substring(0, num);
				text2 = sessionId;
				int num2 = num + 1;
				str = text4 + "\n            " + text2.Substring(num2, text2.Length - num2);
			}
			screenText.Append(result + " ");
			screenText.Append(str);
			if (KIDManager.KidEnabled)
			{
				defaultResult = "\nk-ID ACCOUNT TYPE:";
				LocalisationManager.TryGetKeyForCurrentLocale("SUPPORT_KID_ACCOUNT_TYPE", out result, defaultResult);
				screenText.Append(result.TrailingSpace());
				screenText.Append(KIDManager.GetActiveAccountStatusNiceString().ToUpper());
			}
		}
		else
		{
			string defaultResult2 = "SUPPORT";
			LocalisationManager.TryGetKeyForCurrentLocale("SUPPORT_SCREEN_INTRO", out var result2, defaultResult2);
			screenText.Append(result2);
			defaultResult2 = "\n\nPRESS ENTER TO DISPLAY SUPPORT AND ACCOUNT INFORMATION";
			LocalisationManager.TryGetKeyForCurrentLocale("SUPPORT_SCREEN_INITIAL", out result2, defaultResult2);
			screenText.Append(result2);
			defaultResult2 = "\n\n\n\n<color=red>DO NOT SHARE ACCOUNT INFORMATION WITH ANYONE OTHER THAN ANOTHER AXIOM</color>";
			LocalisationManager.TryGetKeyForCurrentLocale("SUPPORT_SCREEN_INITIAL_WARNING", out result2, defaultResult2);
			screenText.Append(result2);
		}
	}

	private void TimeScreen()
	{
		string defaultResult = "UPDATE TIME SETTINGS. (LOCALLY ONLY). \nPRESS OPTION 1 FOR NORMAL MODE. \nPRESS OPTION 2 FOR STATIC MODE. \nPRESS 1-10 TO CHANGE TIME OF DAY. \nCURRENT MODE: {currentSetting}.\nTIME OF DAY: {currentTimeOfDay}.\n";
		LocalisationManager.TryGetKeyForCurrentLocale("TIME_SCREEN", out var result, defaultResult);
		result = result.Replace("{currentSetting}", BetterDayNightManager.instance.currentSetting.ToString().ToUpper()).Replace("{currentTimeOfDay}", BetterDayNightManager.instance.currentTimeOfDay.ToUpper());
		screenText.Set(result);
	}

	private void CreditsScreen()
	{
		screenText.Set(creditsView.GetScreenText());
	}

	private void VisualsScreen()
	{
		string defaultResult = "UPDATE ITEMS SETTINGS.";
		LocalisationManager.TryGetKeyForCurrentLocale("VISUALS_SCREEN_INTRO", out var result, defaultResult);
		screenText.Set(result.TrailingSpace());
		defaultResult = "PRESS OPTION 1 TO ENABLE ITEM PARTICLES. PRESS OPTION 2 TO DISABLE ITEM PARTICLES. PRESS 1-10 TO CHANGE INSTRUMENT VOLUME FOR OTHER PLAYERS.";
		LocalisationManager.TryGetKeyForCurrentLocale("VISUALS_SCREEN_OPTIONS", out result, defaultResult);
		screenText.Append(result);
		defaultResult = "\n\nITEM PARTICLES ON:";
		LocalisationManager.TryGetKeyForCurrentLocale("VISUALS_SCREEN_CURRENT", out result, defaultResult);
		screenText.Append(result.TrailingSpace());
		string text = (disableParticles ? "FALSE" : "TRUE");
		LocalisationManager.TryGetKeyForCurrentLocale(text, out result, text);
		screenText.Append(result);
		defaultResult = "\nINSTRUMENT VOLUME:";
		LocalisationManager.TryGetKeyForCurrentLocale("VISUALS_SCREEN_VOLUME", out result, defaultResult);
		screenText.Append(result.TrailingSpace());
		screenText.Append(Mathf.CeilToInt(instrumentVolume * 50f).ToString());
	}

	private void VoiceScreen()
	{
		Permission permissionDataByFeature = KIDManager.GetPermissionDataByFeature(EKIDFeatures.Voice_Chat);
		if (KIDManager.HasPermissionToUseFeature(EKIDFeatures.Voice_Chat))
		{
			string defaultResult = "CHOOSE WHICH TYPE OF VOICE YOU WANT TO HEAR AND SPEAK.";
			LocalisationManager.TryGetKeyForCurrentLocale("VOICE_CHAT_SCREEN_INTRO", out var result, defaultResult);
			screenText.Set(result);
			defaultResult = "\nPRESS OPTION 1 = HUMAN VOICES.\nPRESS OPTION 2 = MONKE VOICES.";
			LocalisationManager.TryGetKeyForCurrentLocale("VOICE_CHAT_SCREEN_OPTIONS", out result, defaultResult);
			screenText.Append(result);
			defaultResult = "\n\nVOICE TYPE:";
			LocalisationManager.TryGetKeyForCurrentLocale("VOICE_CHAT_SCREEN_CURRENT", out result, defaultResult);
			screenText.Append(result.TrailingSpace());
			string key = ((voiceChatOn == "TRUE") ? "VOICE_OPTION_HUMAN" : ((voiceChatOn == "FALSE") ? "VOICE_OPTION_MONKE" : "VOICE_OPTION_OFF"));
			defaultResult = ((voiceChatOn == "TRUE") ? "HUMAN" : ((voiceChatOn == "FALSE") ? "MONKE" : "OFF"));
			LocalisationManager.TryGetKeyForCurrentLocale(key, out result, defaultResult);
			screenText.Append(result);
		}
		else if (permissionDataByFeature.ManagedBy == Permission.ManagedByEnum.PROHIBITED)
		{
			VoiceScreen_KIdProhibited();
		}
		else
		{
			VoiceScreen_Permission();
		}
	}

	private void AutomuteScreen()
	{
		string defaultResult = "AUTOMOD AUTOMATICALLY MUTES PLAYERS WHEN THEY JOIN YOUR ROOM IF A LOT OF OTHER PLAYERS HAVE MUTED THEM";
		LocalisationManager.TryGetKeyForCurrentLocale("AUTOMOD_SCREEN_INTRO", out var result, defaultResult);
		screenText.Set(result);
		defaultResult = "\nPRESS OPTION 1 FOR AGGRESSIVE MUTING\nPRESS OPTION 2 FOR MODERATE MUTING\nPRESS OPTION 3 TO TURN AUTOMOD OFF";
		LocalisationManager.TryGetKeyForCurrentLocale("AUTOMOD_SCREEN_OPTIONS", out result, defaultResult);
		screenText.Append(result);
		defaultResult = "\n\nCURRENT AUTOMOD LEVEL: ";
		LocalisationManager.TryGetKeyForCurrentLocale("AUTOMOD_SCREEN_CURRENT", out result, defaultResult);
		screenText.Append(result.TrailingSpace());
		string key = "AUTOMOD_OFF";
		switch (autoMuteType)
		{
		case "OFF":
			key = "AUTOMOD_OFF";
			break;
		case "MODERATE":
			key = "AUTOMOD_MODERATE";
			break;
		case "AGGRESSIVE":
			key = "AUTOMOD_AGGRESSIVE";
			break;
		}
		LocalisationManager.TryGetKeyForCurrentLocale(key, out result, autoMuteType);
		screenText.Append(result);
	}

	private void GroupScreen()
	{
		if (limitOnlineScreens)
		{
			LimitedOnlineFunctionalityScreen();
			return;
		}
		string text = "";
		string result = "";
		string text2 = ((allowedMapsToJoin.Length > 1) ? groupMapJoin : allowedMapsToJoin[0].ToUpper());
		string text3 = "";
		if (allowedMapsToJoin.Length > 1)
		{
			text = "\n\nUSE NUMBER KEYS TO SELECT DESTINATION\n1: FOREST, 2: CAVE, 3: CANYON, 4: CITY, 5: CLOUDS.";
			LocalisationManager.TryGetKeyForCurrentLocale("GROUP_SCREEN_DESTINATIONS", out result, text);
			text3 = result;
		}
		text = "\n\nACTIVE ZONE WILL BE:";
		LocalisationManager.TryGetKeyForCurrentLocale("GROUP_SCREEN_ACTIVE_ZONES", out result, text);
		string text4 = result.TrailingSpace();
		text4 = text4 + text2 + text3;
		if (FriendshipGroupDetection.Instance.IsInParty)
		{
			GorillaNetworkJoinTrigger selectedMapJoinTrigger = GetSelectedMapJoinTrigger();
			string text5 = "";
			if (selectedMapJoinTrigger.CanPartyJoin())
			{
				text = "\n\n<color=red>CANNOT JOIN BECAUSE YOUR GROUP IS NOT HERE</color>";
				LocalisationManager.TryGetKeyForCurrentLocale("GROUP_SCREEN_CANNOT_JOIN", out result, text);
				text5 = result;
			}
			text = "PRESS ENTER TO JOIN A PUBLIC GAME WITH YOUR FRIENDSHIP GROUP.";
			LocalisationManager.TryGetKeyForCurrentLocale("GROUP_SCREEN_ENTER_PARTY", out result, text);
			screenText.Set(result);
			text4 += text5;
			screenText.Append(text4);
		}
		else
		{
			text = "PRESS ENTER TO JOIN A PUBLIC GAME AND BRING EVERYONE IN THIS ROOM WITH YOU.";
			LocalisationManager.TryGetKeyForCurrentLocale("GROUP_SCREEN_ENTER_NOPARTY", out result, text);
			screenText.Set(result);
			screenText.Append(text4);
		}
	}

	private void MicScreen()
	{
		if (KIDManager.GetPermissionDataByFeature(EKIDFeatures.Voice_Chat).ManagedBy == Permission.ManagedByEnum.PROHIBITED)
		{
			MicScreen_KIdProhibited();
			return;
		}
		bool flag = false;
		string text = "";
		if (Microphone.devices.Length == 0)
		{
			flag = true;
			text = "NO MICROPHONE DETECTED";
		}
		if (flag)
		{
			LocalisationManager.TryGetKeyForCurrentLocale("MIC_SCREEN_MIC_DISABLED", out var result, "MIC DISABLED: ");
			screenText.Set(result + text);
			return;
		}
		string defaultResult = "PRESS OPTION 1 = ALL CHAT.\nPRESS OPTION 2 = PUSH TO TALK.\nPRESS OPTION 3 = PUSH TO MUTE.";
		LocalisationManager.TryGetKeyForCurrentLocale("MIC_SCREEN_OPTIONS", out var result2, defaultResult);
		screenText.Set(result2);
		defaultResult = "\n\nCURRENT MIC SETTING:";
		LocalisationManager.TryGetKeyForCurrentLocale("MIC_SCREEN_CURRENT", out result2, defaultResult);
		screenText.Append(result2.TrailingSpace());
		string key = "";
		switch (pttType)
		{
		case "PUSH TO MUTE":
			key = "PUSH_TO_MUTE_MIC";
			break;
		case "PUSH TO TALK":
			key = "PUSH_TO_TALK_MIC";
			break;
		case "OPEN MIC":
			key = "OPEN_MIC";
			break;
		case "ALL CHAT":
			key = "OPEN_MIC";
			break;
		}
		LocalisationManager.TryGetKeyForCurrentLocale(key, out result2, pttType);
		screenText.Append(result2);
		if (pttType == "PUSH TO MUTE")
		{
			defaultResult = "- MIC IS OPEN.\n- HOLD ANY FACE BUTTON TO MUTE.\n\n";
			LocalisationManager.TryGetKeyForCurrentLocale("MIC_SCREEN_PUSH_TO_MUTE_TOOLTIP", out result2, defaultResult);
			screenText.Append(result2);
		}
		else if (pttType == "PUSH TO TALK")
		{
			defaultResult = "- MIC IS MUTED.\n- HOLD ANY FACE BUTTON TO TALK.\n\n";
			LocalisationManager.TryGetKeyForCurrentLocale("MIC_SCREEN_PUSH_TO_TALK_TOOLTIP", out result2, defaultResult);
			screenText.Append(result2);
		}
		else
		{
			screenText.Append("\n\n\n");
		}
		if (speakerLoudness == null)
		{
			speakerLoudness = GorillaTagger.Instance.offlineVRRig.GetComponent<GorillaSpeakerLoudness>();
		}
		if (!(speakerLoudness != null))
		{
			return;
		}
		float num = Mathf.Sqrt(speakerLoudness.LoudnessNormalized);
		if (num <= 0.01f)
		{
			micInputTestTimer += deltaTime;
		}
		else
		{
			micInputTestTimer = 0f;
		}
		if (pttType != "OPEN MIC")
		{
			bool flag2 = false;
			bool flag3 = false;
			bool flag4 = false;
			bool num2 = ControllerInputPoller.PrimaryButtonPress(XRNode.RightHand);
			flag2 = ControllerInputPoller.SecondaryButtonPress(XRNode.RightHand);
			flag3 = ControllerInputPoller.PrimaryButtonPress(XRNode.LeftHand);
			flag4 = ControllerInputPoller.SecondaryButtonPress(XRNode.LeftHand);
			bool flag5 = num2 || flag2 || flag3 || flag4;
			if (flag5 && pttType == "PUSH TO MUTE")
			{
				defaultResult = "INPUT TEST: ";
				LocalisationManager.TryGetKeyForCurrentLocale("MIC_SCREEN_INPUT_TEST_LABEL", out result2, defaultResult);
				screenText.Append(result2);
				return;
			}
			if (!flag5 && pttType == "PUSH TO TALK")
			{
				defaultResult = "INPUT TEST: ";
				LocalisationManager.TryGetKeyForCurrentLocale("MIC_SCREEN_INPUT_TEST_LABEL", out result2, defaultResult);
				screenText.Append(result2);
				return;
			}
		}
		if (micInputTestTimer >= micInputTestTimerThreshold)
		{
			defaultResult = "NO MIC INPUT DETECTED. CHECK MIC SETTINGS IN THE OPERATING SYSTEM.";
			LocalisationManager.TryGetKeyForCurrentLocale("MIC_SCREEN_INPUT_TEST_NO_MIC", out result2, defaultResult);
			screenText.Append(result2);
			return;
		}
		defaultResult = "INPUT TEST: ";
		LocalisationManager.TryGetKeyForCurrentLocale("MIC_SCREEN_INPUT_TEST_LABEL", out result2, defaultResult);
		screenText.Append(result2);
		for (int i = 0; i < Mathf.FloorToInt(num * 50f); i++)
		{
			screenText.Append("|");
		}
	}

	private void QueueScreen()
	{
		if (limitOnlineScreens)
		{
			LimitedOnlineFunctionalityScreen();
			return;
		}
		string defaultResult = "THIS OPTION AFFECTS WHO YOU PLAY WITH. DEFAULT IS FOR ANYONE TO PLAY NORMALLY. MINIGAMES IS FOR PEOPLE LOOKING TO PLAY WITH THEIR OWN MADE UP RULES.";
		LocalisationManager.TryGetKeyForCurrentLocale("QUEUE_SCREEN", out var result, defaultResult);
		screenText.Set(result.TrailingSpace());
		if (allowedInCompetitive)
		{
			defaultResult = "COMPETITIVE IS FOR PLAYERS WHO WANT TO PLAY THE GAME AND TRY AS HARD AS THEY CAN.";
			LocalisationManager.TryGetKeyForCurrentLocale("COMPETITIVE_DESC", out result, defaultResult);
			screenText.Append(result.TrailingSpace());
			defaultResult = "PRESS OPTION 1 FOR DEFAULT, OPTION 2 FOR MINIGAMES, OR OPTION 3 FOR COMPETITIVE.";
			LocalisationManager.TryGetKeyForCurrentLocale("QUEUE_SCREEN_ALL_QUEUES", out result, defaultResult);
			screenText.Append(result);
		}
		else
		{
			defaultResult = "BEAT THE OBSTACLE COURSE IN CITY TO ALLOW COMPETITIVE PLAY.";
			LocalisationManager.TryGetKeyForCurrentLocale("BEAT_OBSTACLE_COURSE", out result, defaultResult);
			screenText.Append(result.TrailingSpace());
			defaultResult = "PRESS OPTION 1 FOR DEFAULT, OR OPTION 2 FOR MINIGAMES.";
			LocalisationManager.TryGetKeyForCurrentLocale("QUEUE_SCREEN_DEFAULT_QUEUES", out result, defaultResult);
			screenText.Append(result);
		}
		defaultResult = "\n\nCURRENT QUEUE:";
		LocalisationManager.TryGetKeyForCurrentLocale("CURRENT_QUEUE", out result, defaultResult);
		screenText.Append(result.TrailingSpace());
		string text = "DEFAULT_QUEUE";
		text = currentQueue switch
		{
			"COMPETITIVE" => "COMPETITIVE_QUEUE", 
			"MINIGAMES" => "MINIGAMES_QUEUE", 
			_ => "DEFAULT_QUEUE", 
		};
		defaultResult = currentQueue;
		LocalisationManager.TryGetKeyForCurrentLocale(text, out result, defaultResult);
		screenText.Append(result);
	}

	private void TroopScreen()
	{
		if (limitOnlineScreens)
		{
			LimitedOnlineFunctionalityScreen();
			return;
		}
		Permission permissionDataByFeature = KIDManager.GetPermissionDataByFeature(EKIDFeatures.Groups);
		Permission permissionDataByFeature2 = KIDManager.GetPermissionDataByFeature(EKIDFeatures.Multiplayer);
		bool flag = KIDManager.HasPermissionToUseFeature(EKIDFeatures.Groups) && KIDManager.HasPermissionToUseFeature(EKIDFeatures.Multiplayer);
		bool flag2 = IsValidTroopName(troopName);
		screenText.Set(string.Empty);
		string text = "";
		string result = "";
		if (flag)
		{
			text = "PLAY WITH A PERSISTENT GROUP ACROSS MULTIPLE ROOMS.";
			LocalisationManager.TryGetKeyForCurrentLocale("TROOP_SCREEN_INTRO", out result, text);
			screenText.Set(result);
			if (!flag2)
			{
				text = " PRESS ENTER TO JOIN OR CREATE A TROOP.";
				LocalisationManager.TryGetKeyForCurrentLocale("TROOP_SCREEN_INSTRUCTIONS", out result, text);
				screenText.Append(result);
			}
		}
		text = "\n\nCURRENT TROOP: ";
		LocalisationManager.TryGetKeyForCurrentLocale("TROOP_SCREEN_CURRENT_TROOP", out result, text);
		screenText.Append(result.TrailingSpace());
		if (flag2)
		{
			screenText.Append(troopName ?? "");
			if (flag)
			{
				bool flag3 = currentTroopPopulation > -1;
				if (troopQueueActive)
				{
					text = "\n  -IN TROOP QUEUE-";
					LocalisationManager.TryGetKeyForCurrentLocale("TROOP_SCREEN_IN_QUEUE", out result, text);
					screenText.Append(result);
					if (flag3)
					{
						text = "\n\nPLAYERS IN TROOP: ";
						LocalisationManager.TryGetKeyForCurrentLocale("TROOP_SCREEN_PLAYERS_IN_TROOP", out result, text);
						screenText.Append(result.TrailingSpace());
						screenText.Append(Mathf.Max(1, currentTroopPopulation).ToString());
					}
					text = "\n\nPRESS OPTION 2 FOR DEFAULT QUEUE.";
					LocalisationManager.TryGetKeyForCurrentLocale("TROOP_SCREEN_DEFAULT_QUEUE", out result, text);
					screenText.Append(result);
				}
				else
				{
					text = "\n  -IN {currentQueue} QUEUE-";
					LocalisationManager.TryGetKeyForCurrentLocale("TROOP_SCREEN_CURRENT_QUEUE", out result, text);
					string text2 = "DEFAULT_QUEUE";
					text2 = currentQueue switch
					{
						"MINIGAMES" => "MINIGAMES_QUEUE", 
						"COMPETITIVE" => "COMPETITIVE_QUEUE", 
						_ => "DEFAULT_QUEUE", 
					};
					text = currentQueue;
					LocalisationManager.TryGetKeyForCurrentLocale(text2, out var result2, text);
					result = result.Replace("{currentQueue}", result2);
					screenText.Append(result);
					if (flag3)
					{
						text = "\n\nPLAYERS IN TROOP: ";
						LocalisationManager.TryGetKeyForCurrentLocale("TROOP_SCREEN_PLAYERS_IN_TROOP", out result, text);
						screenText.Append(result.TrailingSpace());
						screenText.Append(Mathf.Max(1, currentTroopPopulation).ToString());
					}
					text = "\n\nPRESS OPTION 1 FOR TROOP QUEUE.";
					LocalisationManager.TryGetKeyForCurrentLocale("TROOP_SCREEN_TROOP_QUEUE", out result, text);
					screenText.Append(result);
				}
				text = "\nPRESS OPTION 3 TO LEAVE YOUR TROOP.";
				LocalisationManager.TryGetKeyForCurrentLocale("TROOP_SCREEN_LEAVE", out result, text);
				screenText.Append(result);
			}
		}
		else
		{
			text = "-NOT IN TROOP-";
			LocalisationManager.TryGetKeyForCurrentLocale("TROOP_SCREEN_NOT_IN_TROOP", out result, text);
			screenText.Append(result);
		}
		if (flag)
		{
			if (!flag2)
			{
				text = "\n\nTROOP TO JOIN: ";
				LocalisationManager.TryGetKeyForCurrentLocale("TROOP_SCREEN_JOIN_TROOP", out result, text);
				screenText.Append(result.TrailingSpace());
				screenText.Append(troopToJoin);
			}
		}
		else if (permissionDataByFeature.ManagedBy == Permission.ManagedByEnum.PROHIBITED || permissionDataByFeature2.ManagedBy == Permission.ManagedByEnum.PROHIBITED)
		{
			TroopScreen_KIdProhibited();
		}
		else
		{
			TroopScreen_Permission();
		}
	}

	private void TurnScreen()
	{
		string defaultResult = "PRESS OPTION 1 TO USE SNAP TURN. PRESS OPTION 2 TO USE SMOOTH TURN. PRESS OPTION 3 TO USE NO ARTIFICIAL TURNING.";
		string text = "";
		LocalisationManager.TryGetKeyForCurrentLocale("TURN_SCREEN", out var result, defaultResult);
		text += result.TrailingSpace();
		defaultResult = "PRESS THE NUMBER KEYS TO CHOOSE A TURNING SPEED.";
		LocalisationManager.TryGetKeyForCurrentLocale("TURN_SCREEN_TURNING_SPEED", out result, defaultResult);
		text += result;
		defaultResult = "\n CURRENT TURN TYPE: ";
		LocalisationManager.TryGetKeyForCurrentLocale("TURN_SCREEN_TURN_TYPE", out result, defaultResult);
		text += result;
		string key = "TURN_TYPE_NO_TURN";
		switch (GorillaSnapTurn.CachedSnapTurnRef.turnType)
		{
		case "SNAP":
			key = "TURN_TYPE_SNAP_TURN";
			break;
		case "SMOOTH":
			key = "TURN_TYPE_SMOOTH_TURN";
			break;
		case "NONE":
			key = "TURN_TYPE_NO_TURN";
			break;
		default:
			Debug.LogError("[LOCALIZATION::GORILLA_COMPUTER::TURN] Could not match [" + GorillaSnapTurn.CachedSnapTurnRef.turnType + "] to any case. Defaulting to NO_TURN");
			break;
		}
		LocalisationManager.TryGetKeyForCurrentLocale(key, out result, GorillaSnapTurn.CachedSnapTurnRef.turnType);
		text += result;
		defaultResult = "\nCURRENT TURN SPEED: ";
		LocalisationManager.TryGetKeyForCurrentLocale("TURN_SCREEN_TURN_SPEED", out result, defaultResult);
		text += result;
		text += GorillaSnapTurn.CachedSnapTurnRef.turnFactor;
		screenText.Set(text);
	}

	private void NameScreen()
	{
		Permission permissionDataByFeature = KIDManager.GetPermissionDataByFeature(EKIDFeatures.Custom_Nametags);
		if (KIDManager.HasPermissionToUseFeature(EKIDFeatures.Custom_Nametags))
		{
			string defaultResult = "PRESS ENTER TO CHANGE YOUR NAME TO THE ENTERED NEW NAME.\n\n";
			LocalisationManager.TryGetKeyForCurrentLocale("NAME_SCREEN", out var result, defaultResult);
			screenText.Set(result);
			defaultResult = "CURRENT NAME: ";
			LocalisationManager.TryGetKeyForCurrentLocale("CURRENT_NAME", out result, defaultResult);
			screenText.Append(result.TrailingSpace());
			screenText.Append(savedName);
			if (NametagsEnabled)
			{
				defaultResult = "NEW NAME: ";
				LocalisationManager.TryGetKeyForCurrentLocale("NEW_NAME", out result, defaultResult);
				screenText.Append(result.TrailingSpace());
				screenText.Append(currentName);
			}
			defaultResult = "PRESS OPTION 1 TO TOGGLE NAMETAGS.\nCURRENTLY NAMETAGS ARE: ";
			LocalisationManager.TryGetKeyForCurrentLocale("NAME_SCREEN_TOGGLE_NAMETAGS", out result, defaultResult);
			string key = (NametagsEnabled ? "ON_KEY" : "OFF_KEY");
			screenText.Append(result.TrailingSpace());
			defaultResult = (NametagsEnabled ? "ON" : "OFF");
			LocalisationManager.TryGetKeyForCurrentLocale(key, out result, defaultResult);
			screenText.Append(result);
		}
		else if (permissionDataByFeature.ManagedBy == Permission.ManagedByEnum.PROHIBITED)
		{
			NameScreen_KIdProhibited();
		}
		else
		{
			NameScreen_Permission();
		}
	}

	private void StartupScreen()
	{
		string text = string.Empty;
		if (KIDManager.GetActiveAccountStatus() == AgeStatusType.DIGITALMINOR)
		{
			text = "YOU ARE PLAYING ON A MANAGED ACCOUNT. SOME SETTINGS MAY BE DISABLED WITHOUT PARENT OR GUARDIAN APPROVAL\n\n";
			if (LocalisationManager.TryGetKeyForCurrentLocale("STARTUP_MANAGED", out var result, text))
			{
				text = result;
			}
		}
		_ = string.Empty;
		LocalisationManager.TryGetKeyForCurrentLocale("STARTUP_INTRO", out var result2, "GORILLA OS\n\n");
		screenText.Set(result2);
		screenText.Append(text);
		LocalisationManager.TryGetKeyForCurrentLocale("STARTUP_PLAYERS_ONLINE", out result2, "{playersOnline} PLAYERS ONLINE\n\n");
		screenText.Append(result2.Replace("{playersOnline}", HowManyMonke.ThisMany.ToString()));
		LocalisationManager.TryGetKeyForCurrentLocale("STARTUP_USERS_BANNED", out result2, "{usersBanned} USERS BANNED YESTERDAY\n\n");
		screenText.Append(result2.Replace("{usersBanned}", usersBanned.ToString()));
		LocalisationManager.TryGetKeyForCurrentLocale("STARTUP_PRESS_KEY", out result2, "PRESS ANY KEY TO BEGIN");
		screenText.Append(result2);
	}

	private void ColourScreen()
	{
		LocalisationManager.TryGetKeyForCurrentLocale("COLOR_SELECT_INTRO", out var result, "USE THE OPTIONS BUTTONS TO SELECT THE COLOR TO UPDATE, THEN PRESS 0-9 TO SET A NEW VALUE.");
		screenText.Set(result);
		LocalisationManager.TryGetKeyForCurrentLocale("COLOR_RED", out result, "RED");
		screenText.Append("\n\n");
		screenText.Append(result);
		screenText.Append(Mathf.FloorToInt(redValue * 9f) + ((colorCursorLine == 0) ? "<--" : ""));
		LocalisationManager.TryGetKeyForCurrentLocale("COLOR_GREEN", out result, "GREEN");
		screenText.Append("\n\n");
		screenText.Append(result);
		screenText.Append(Mathf.FloorToInt(greenValue * 9f) + ((colorCursorLine == 1) ? "<--" : ""));
		LocalisationManager.TryGetKeyForCurrentLocale("COLOR_BLUE", out result, "BLUE");
		screenText.Append("\n\n");
		screenText.Append(result);
		screenText.Append(Mathf.FloorToInt(blueValue * 9f) + ((colorCursorLine == 2) ? "<--" : ""));
	}

	private void RoomScreen()
	{
		if (limitOnlineScreens)
		{
			LimitedOnlineFunctionalityScreen();
			return;
		}
		Permission permissionDataByFeature = KIDManager.GetPermissionDataByFeature(EKIDFeatures.Groups);
		Permission permissionDataByFeature2 = KIDManager.GetPermissionDataByFeature(EKIDFeatures.Multiplayer);
		bool item = KIDManager.CheckFeatureOptIn(EKIDFeatures.Multiplayer).hasOptedInPreviously;
		bool num = KIDManager.HasPermissionToUseFeature(EKIDFeatures.Groups) && KIDManager.HasPermissionToUseFeature(EKIDFeatures.Multiplayer) && item;
		screenText.Set("");
		string result = "";
		string text = "";
		if (num)
		{
			text = "PRESS ENTER TO JOIN OR CREATE A CUSTOM ROOM WITH THE ENTERED CODE.";
			LocalisationManager.TryGetKeyForCurrentLocale("ROOM_INTRO", out result, text);
			screenText.Append(result.TrailingSpace());
		}
		text = "PRESS OPTION 1 TO DISCONNECT FROM THE CURRENT ROOM.";
		LocalisationManager.TryGetKeyForCurrentLocale("ROOM_OPTION", out result, text);
		screenText.Append(result.TrailingSpace());
		if (FriendshipGroupDetection.Instance.IsInParty)
		{
			if (FriendshipGroupDetection.Instance.IsPartyWithinCollider(friendJoinCollider))
			{
				text = "YOUR GROUP WILL TRAVEL WITH YOU.";
				LocalisationManager.TryGetKeyForCurrentLocale("ROOM_GROUP_TRAVEL", out result, text);
				screenText.Append(result.TrailingSpace());
			}
			else
			{
				text = "<color=red>YOU WILL LEAVE YOUR PARTY UNLESS YOU GATHER THEM HERE FIRST!</color> ";
				LocalisationManager.TryGetKeyForCurrentLocale("ROOM_PARTY_WARNING", out result, text);
				screenText.Append(result);
			}
		}
		text = "\n\nCURRENT ROOM:";
		LocalisationManager.TryGetKeyForCurrentLocale("ROOM_TEXT_CURRENT_ROOM", out result, text);
		screenText.Append(result.TrailingSpace());
		if (NetworkSystem.Instance.InRoom)
		{
			screenText.Append(NetworkSystem.Instance.RoomName.TrailingSpace());
			if (NetworkSystem.Instance.SessionIsPrivate)
			{
				string text2 = GameMode.ActiveGameMode?.GameModeNameRoomLabel();
				if (!string.IsNullOrEmpty(text2))
				{
					screenText.Append(text2 ?? "");
				}
			}
			text = "\n\nPLAYERS IN ROOM:";
			LocalisationManager.TryGetKeyForCurrentLocale("PLAYERS_IN_ROOM", out result, text);
			screenText.Append(result.TrailingSpace());
			screenText.Append(NetworkSystem.Instance.RoomPlayerCount.ToString());
		}
		else
		{
			text = "-NOT IN ROOM-";
			LocalisationManager.TryGetKeyForCurrentLocale("NOT_IN_ROOM", out result, text);
			screenText.Append(result);
			text = "\n\nPLAYERS ONLINE:";
			LocalisationManager.TryGetKeyForCurrentLocale("PLAYERS_ONLINE", out result, text);
			screenText.Append(result.TrailingSpace());
			screenText.Append(HowManyMonke.ThisMany.ToString());
		}
		if (num)
		{
			text = "\n\nROOM TO JOIN:";
			LocalisationManager.TryGetKeyForCurrentLocale("ROOM_TO_JOIN", out result, text);
			screenText.Append(result.TrailingSpace());
			screenText.Append(roomToJoin);
			if (roomFull)
			{
				text = "\n\nROOM FULL. JOIN ROOM FAILED.";
				LocalisationManager.TryGetKeyForCurrentLocale("ROOM_FULL", out result, text);
				screenText.Append(result);
			}
			else if (roomNotAllowed)
			{
				text = "\n\nCANNOT JOIN ROOM TYPE FROM HERE.";
				LocalisationManager.TryGetKeyForCurrentLocale("ROOM_JOIN_NOT_ALLOWED", out result, text);
				screenText.Append(result);
			}
		}
		else if (permissionDataByFeature.ManagedBy == Permission.ManagedByEnum.PROHIBITED || permissionDataByFeature2.ManagedBy == Permission.ManagedByEnum.PROHIBITED)
		{
			RoomScreen_KIdProhibited();
		}
		else
		{
			RoomScreen_Permission();
		}
	}

	private void RedemptionScreen()
	{
		string defaultResult = "TYPE REDEMPTION CODE AND PRESS ENTER";
		LocalisationManager.TryGetKeyForCurrentLocale("REDEMPTION_INTRO", out var result, defaultResult);
		screenText.Set(result);
		defaultResult = "\n\nCODE: " + redemptionCode;
		LocalisationManager.TryGetKeyForCurrentLocale("REDEMPTION_CODE_LABEL", out result, defaultResult);
		screenText.Append(result.TrailingSpace());
		screenText.Append(redemptionCode);
		switch (RedemptionStatus)
		{
		case RedemptionResult.Invalid:
			defaultResult = "\n\nINVALID CODE";
			LocalisationManager.TryGetKeyForCurrentLocale("REDEMPTION_CODE_INVALID", out result, defaultResult);
			screenText.Append(result);
			break;
		case RedemptionResult.Checking:
			defaultResult = "\n\nVALIDATING...";
			LocalisationManager.TryGetKeyForCurrentLocale("REDEMPTION_CODE_VALIDATING", out result, defaultResult);
			screenText.Append(result);
			break;
		case RedemptionResult.AlreadyUsed:
			defaultResult = "\n\nCODE ALREADY CLAIMED";
			LocalisationManager.TryGetKeyForCurrentLocale("REDEMPTION_CODE_ALREADY_USED", out result, defaultResult);
			screenText.Append(result);
			break;
		case RedemptionResult.TooEarly:
			defaultResult = "CODE IS NOT REDEEMABLE UNTIL";
			LocalisationManager.TryGetKeyForCurrentLocale("REDEMPTION_CODE_TOO_EARLY", out result, defaultResult);
			screenText.Append(RedemptionRestrictionTime.HasValue ? ("\n\n" + result + "\n" + RedemptionRestrictionTime.Value.ToLocalTime().ToString("f").ToUpper()) : ("\n\n" + result + "\n[MISSING]"));
			break;
		case RedemptionResult.TooLate:
			defaultResult = "CODE EXPIRED";
			LocalisationManager.TryGetKeyForCurrentLocale("REDEMPTION_CODE_TOO_LATE", out result, defaultResult);
			screenText.Append(RedemptionRestrictionTime.HasValue ? ("\n\n" + result + "\n" + RedemptionRestrictionTime.Value.ToLocalTime().ToString("f").ToUpper()) : ("\n\n" + result + "\n[MISSING]"));
			break;
		case RedemptionResult.Success:
			defaultResult = "\n\nSUCCESSFULLY CLAIMED!";
			LocalisationManager.TryGetKeyForCurrentLocale("REDEMPTION_CODE_SUCCESS", out result, defaultResult);
			screenText.Append(result);
			break;
		case RedemptionResult.Empty:
			break;
		}
	}

	private void LimitedOnlineFunctionalityScreen()
	{
		string defaultResult = "NOT AVAILABLE IN RANKED PLAY";
		LocalisationManager.TryGetKeyForCurrentLocale("LIMITED_ONLINE_FUNC", out var result, defaultResult);
		screenText.Set(result);
	}

	private void UpdateGameModeText()
	{
		string defaultResult = "CURRENT MODE";
		LocalisationManager.TryGetKeyForCurrentLocale("CURRENT_MODE", out var result, defaultResult);
		currentGameModeText.Value = result;
		if (!NetworkSystem.Instance.InRoom || GorillaGameManager.instance == null)
		{
			defaultResult = "-NOT IN ROOM-";
			LocalisationManager.TryGetKeyForCurrentLocale("NOT_IN_ROOM", out result, defaultResult);
			currentGameModeText.Value += result;
		}
		else
		{
			WatchableStringSO watchableStringSO = currentGameModeText;
			watchableStringSO.Value = watchableStringSO.Value + "\n" + GorillaGameManager.instance.GameModeName();
		}
	}

	private void UpdateFunctionScreen()
	{
		functionSelectText.Set(GetOrderListForScreen(currentState));
	}

	private void CheckAutoBanListForRoomName(string nameToCheck)
	{
		SwitchToLoadingState();
		CheckForBadRoomName(nameToCheck);
	}

	private void CheckAutoBanListForPlayerName(string nameToCheck)
	{
		SwitchToLoadingState();
		CheckForBadPlayerName(nameToCheck);
	}

	private void CheckAutoBanListForTroopName(string nameToCheck)
	{
		if (IsValidTroopName(troopToJoin))
		{
			SwitchToLoadingState();
			CheckForBadTroopName(nameToCheck);
		}
	}

	private void CheckForBadRoomName(string nameToCheck)
	{
		GorillaServer.Instance.CheckForBadName(new CheckForBadNameRequest
		{
			name = nameToCheck,
			forRoom = true,
			forTroop = false
		}, OnRoomNameChecked, OnErrorNameCheck);
	}

	private void CheckForBadPlayerName(string nameToCheck)
	{
		GorillaServer.Instance.CheckForBadName(new CheckForBadNameRequest
		{
			name = nameToCheck,
			forRoom = false,
			forTroop = false
		}, OnPlayerNameChecked, OnErrorNameCheck);
	}

	private void CheckForBadTroopName(string nameToCheck)
	{
		GorillaServer.Instance.CheckForBadName(new CheckForBadNameRequest
		{
			name = nameToCheck,
			forRoom = false,
			forTroop = true
		}, OnTroopNameChecked, OnErrorNameCheck);
	}

	private void OnRoomNameChecked(ExecuteFunctionResult result)
	{
		if (((JsonObject)result.FunctionResult).TryGetValue("result", out var value))
		{
			switch ((NameCheckResult)int.Parse(value.ToString()))
			{
			case NameCheckResult.Success:
				if (FriendshipGroupDetection.Instance.IsInParty && !FriendshipGroupDetection.Instance.IsPartyWithinCollider(friendJoinCollider))
				{
					FriendshipGroupDetection.Instance.LeaveParty();
				}
				if (playerInVirtualStump)
				{
					CustomMapManager.UnloadMap(returnToSinglePlayerIfInPublic: false);
				}
				networkController.AttemptToJoinSpecificRoom(roomToJoin, FriendshipGroupDetection.Instance.IsInParty ? JoinType.JoinWithParty : JoinType.Solo);
				break;
			case NameCheckResult.Warning:
				roomToJoin = "";
				roomToJoin += (playerInVirtualStump ? virtualStumpRoomPrepend : "");
				SwitchToWarningState();
				break;
			case NameCheckResult.Ban:
				roomToJoin = "";
				roomToJoin += (playerInVirtualStump ? virtualStumpRoomPrepend : "");
				GorillaGameManager.ForceStopGame_DisconnectAndDestroy();
				break;
			}
		}
		if (currentState == ComputerState.Loading)
		{
			PopState();
		}
	}

	private void OnPlayerNameChecked(ExecuteFunctionResult result)
	{
		if (((JsonObject)result.FunctionResult).TryGetValue("result", out var value))
		{
			switch ((NameCheckResult)int.Parse(value.ToString()))
			{
			case NameCheckResult.Success:
				NetworkSystem.Instance.SetMyNickName(currentName);
				CustomMapsTerminal.RequestDriverNickNameRefresh();
				break;
			case NameCheckResult.Warning:
				NetworkSystem.Instance.SetMyNickName("gorilla");
				CustomMapsTerminal.RequestDriverNickNameRefresh();
				currentName = "gorilla";
				SwitchToWarningState();
				break;
			case NameCheckResult.Ban:
				NetworkSystem.Instance.SetMyNickName("gorilla");
				CustomMapsTerminal.RequestDriverNickNameRefresh();
				currentName = "gorilla";
				GorillaGameManager.ForceStopGame_DisconnectAndDestroy();
				break;
			}
		}
		SetLocalNameTagText(currentName);
		savedName = currentName;
		PlayerPrefs.SetString("playerName", currentName);
		PlayerPrefs.Save();
		if (NetworkSystem.Instance.InRoom)
		{
			GorillaTagger.Instance.myVRRig.SendRPC("RPC_InitializeNoobMaterial", RpcTarget.All, redValue, greenValue, blueValue);
		}
		if (currentState == ComputerState.Loading)
		{
			PopState();
		}
	}

	private void OnTroopNameChecked(ExecuteFunctionResult result)
	{
		if (((JsonObject)result.FunctionResult).TryGetValue("result", out var value))
		{
			switch ((NameCheckResult)int.Parse(value.ToString()))
			{
			case NameCheckResult.Success:
				JoinTroop(troopToJoin);
				break;
			case NameCheckResult.Warning:
				troopToJoin = string.Empty;
				SwitchToWarningState();
				break;
			case NameCheckResult.Ban:
				troopToJoin = string.Empty;
				GorillaGameManager.ForceStopGame_DisconnectAndDestroy();
				break;
			}
		}
		if (currentState == ComputerState.Loading)
		{
			PopState();
		}
	}

	private void OnErrorNameCheck(PlayFabError error)
	{
		if (currentState == ComputerState.Loading)
		{
			PopState();
		}
		OnErrorShared(error);
	}

	public bool CheckAutoBanListForName(string nameToCheck)
	{
		nameToCheck = nameToCheck.ToLower();
		nameToCheck = new string(Array.FindAll(nameToCheck.ToCharArray(), (char c) => char.IsLetterOrDigit(c)));
		string[] array = anywhereTwoWeek;
		foreach (string value in array)
		{
			if (nameToCheck.IndexOf(value) >= 0)
			{
				return false;
			}
		}
		array = anywhereOneWeek;
		foreach (string value2 in array)
		{
			if (nameToCheck.IndexOf(value2) >= 0 && !nameToCheck.Contains("fagol"))
			{
				return false;
			}
		}
		array = exactOneWeek;
		for (int num = 0; num < array.Length; num++)
		{
			if (array[num] == nameToCheck)
			{
				return false;
			}
		}
		return true;
	}

	public void UpdateColor(float red, float green, float blue)
	{
		redValue = Mathf.Clamp(red, 0f, 1f);
		greenValue = Mathf.Clamp(green, 0f, 1f);
		blueValue = Mathf.Clamp(blue, 0f, 1f);
	}

	public void UpdateFailureText(string failMessage)
	{
		GorillaScoreboardTotalUpdater.instance.SetOfflineFailureText(failMessage);
		PhotonNetworkController.Instance.UpdateTriggerScreens();
		screenText.EnableFailedState(failMessage);
		functionSelectText.EnableFailedState(failMessage);
	}

	private void RestoreFromFailureState()
	{
		GorillaScoreboardTotalUpdater.instance.ClearOfflineFailureText();
		PhotonNetworkController.Instance.UpdateTriggerScreens();
		screenText.DisableFailedState();
		functionSelectText.DisableFailedState();
	}

	public void GeneralFailureMessage(string failMessage)
	{
		isConnectedToMaster = false;
		NetworkSystem.Instance.SetWrongVersion();
		UpdateFailureText(failMessage);
		UpdateScreen();
	}

	private static void OnErrorShared(PlayFabError error)
	{
		if (error.Error == PlayFabErrorCode.NotAuthenticated)
		{
			PlayFabAuthenticator.instance.AuthenticateWithPlayFab();
		}
		else if (error.Error == PlayFabErrorCode.AccountBanned)
		{
			GorillaGameManager.ForceStopGame_DisconnectAndDestroy();
		}
		if (error.ErrorMessage == "The account making this request is currently banned")
		{
			using (Dictionary<string, List<string>>.Enumerator enumerator = error.ErrorDetails.GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					KeyValuePair<string, List<string>> current = enumerator.Current;
					if (current.Value[0] != "Indefinite")
					{
						instance.GeneralFailureMessage("YOUR ACCOUNT " + PlayFabAuthenticator.instance.GetPlayFabPlayerId() + " HAS BEEN BANNED. YOU WILL NOT BE ABLE TO PLAY UNTIL THE BAN EXPIRES.\nREASON: " + current.Key + "\nHOURS LEFT: " + (int)((DateTime.Parse(current.Value[0]) - DateTime.UtcNow).TotalHours + 1.0));
					}
					else
					{
						instance.GeneralFailureMessage("YOUR ACCOUNT " + PlayFabAuthenticator.instance.GetPlayFabPlayerId() + " HAS BEEN BANNED INDEFINITELY.\nREASON: " + current.Key);
					}
				}
				return;
			}
		}
		if (!(error.ErrorMessage == "The IP making this request is currently banned"))
		{
			return;
		}
		using Dictionary<string, List<string>>.Enumerator enumerator = error.ErrorDetails.GetEnumerator();
		if (enumerator.MoveNext())
		{
			KeyValuePair<string, List<string>> current2 = enumerator.Current;
			if (current2.Value[0] != "Indefinite")
			{
				instance.GeneralFailureMessage("THIS IP HAS BEEN BANNED. YOU WILL NOT BE ABLE TO PLAY UNTIL THE BAN EXPIRES.\nREASON: " + current2.Key + "\nHOURS LEFT: " + (int)((DateTime.Parse(current2.Value[0]) - DateTime.UtcNow).TotalHours + 1.0));
			}
			else
			{
				instance.GeneralFailureMessage("THIS IP HAS BEEN BANNED INDEFINITELY.\nREASON: " + current2.Key);
			}
		}
	}

	private void DecreaseState()
	{
		currentStateIndex--;
		if (GetState(currentStateIndex) == ComputerState.Time)
		{
			currentStateIndex--;
		}
		if (currentStateIndex < 0)
		{
			currentStateIndex = FunctionsCount - 1;
		}
		SwitchState(GetState(currentStateIndex));
	}

	private void IncreaseState()
	{
		currentStateIndex++;
		if (GetState(currentStateIndex) == ComputerState.Time)
		{
			currentStateIndex++;
		}
		if (currentStateIndex >= FunctionsCount)
		{
			currentStateIndex = 0;
		}
		SwitchState(GetState(currentStateIndex));
	}

	public ComputerState GetState(int index)
	{
		try
		{
			return _activeOrderList[index].State;
		}
		catch
		{
			return _activeOrderList[0].State;
		}
	}

	public int GetStateIndex(ComputerState state)
	{
		return _activeOrderList.FindIndex((StateOrderItem s) => s.State == state);
	}

	public string GetOrderListForScreen(ComputerState currentState)
	{
		StringBuilder stringBuilder = new StringBuilder();
		int stateIndex = GetStateIndex(currentState);
		for (int i = 0; i < FunctionsCount; i++)
		{
			stringBuilder.Append(FunctionNames[i]);
			if (i == stateIndex)
			{
				stringBuilder.Append(Pointer);
			}
			if (i < FunctionsCount - 1)
			{
				stringBuilder.Append("\n");
			}
		}
		return stringBuilder.ToString();
	}

	private void GetCurrentTime()
	{
		tryGetTimeAgain = true;
		PlayFabClientAPI.GetTime(new GetTimeRequest(), OnGetTimeSuccess, OnGetTimeFailure);
	}

	private void OnGetTimeSuccess(GetTimeResult result)
	{
		startupMillis = (long)(TimeSpan.FromTicks(result.Time.Ticks).TotalMilliseconds - (double)(Time.realtimeSinceStartup * 1000f));
		startupTime = result.Time - TimeSpan.FromSeconds(Time.realtimeSinceStartup);
		OnServerTimeUpdated?.Invoke();
	}

	private void OnGetTimeFailure(PlayFabError error)
	{
		startupMillis = (long)(TimeSpan.FromTicks(DateTime.UtcNow.Ticks).TotalMilliseconds - (double)(Time.realtimeSinceStartup * 1000f));
		startupTime = DateTime.UtcNow - TimeSpan.FromSeconds(Time.realtimeSinceStartup);
		OnServerTimeUpdated?.Invoke();
		if (error.Error == PlayFabErrorCode.NotAuthenticated)
		{
			PlayFabAuthenticator.instance.AuthenticateWithPlayFab();
		}
		else if (error.Error == PlayFabErrorCode.AccountBanned)
		{
			GorillaGameManager.ForceStopGame_DisconnectAndDestroy();
		}
	}

	private void PlayerCountChangedCallback(NetPlayer player)
	{
		UpdateScreen();
	}

	private static void OnFirstJoinedRoom_IncrementSessionCount()
	{
		RoomSystem.JoinedRoomEvent -= new Action(OnFirstJoinedRoom_IncrementSessionCount);
		sessionCount++;
		PlayerPrefs.SetInt("sessionCount", sessionCount);
		PlayerPrefs.Save();
	}

	public void SetNameBySafety(bool isSafety)
	{
		if (isSafety)
		{
			PlayerPrefs.SetString("playerNameBackup", currentName);
			currentName = "gorilla" + UnityEngine.Random.Range(0, 9999).ToString().PadLeft(4, '0');
			savedName = currentName;
			NetworkSystem.Instance.SetMyNickName(currentName);
			SetLocalNameTagText(currentName);
			PlayerPrefs.SetString("playerName", currentName);
			PlayerPrefs.Save();
			if (NetworkSystem.Instance.InRoom)
			{
				GorillaTagger.Instance.myVRRig.SendRPC("RPC_InitializeNoobMaterial", RpcTarget.All, redValue, greenValue, blueValue);
			}
		}
	}

	public void SetLocalNameTagText(string newName)
	{
		VRRig.LocalRig.SetNameTagText(newName);
	}

	public void SetComputerSettingsBySafety(bool isSafety, ComputerState[] toFilterOut, bool shouldHide)
	{
		_activeOrderList = OrderList;
		if (!isSafety)
		{
			_activeOrderList = OrderList;
			if (_filteredStates.Count > 0 && toFilterOut.Length != 0)
			{
				for (int i = 0; i < toFilterOut.Length; i++)
				{
					if (_filteredStates.Contains(toFilterOut[i]))
					{
						_filteredStates.Remove(toFilterOut[i]);
					}
				}
			}
		}
		else if (shouldHide)
		{
			for (int j = 0; j < toFilterOut.Length; j++)
			{
				if (!_filteredStates.Contains(toFilterOut[j]))
				{
					_filteredStates.Add(toFilterOut[j]);
				}
			}
		}
		if (_filteredStates.Count > 0)
		{
			int k = 0;
			for (int num = _activeOrderList.Count; k < num; k++)
			{
				if (_filteredStates.Contains(_activeOrderList[k].State))
				{
					_activeOrderList.RemoveAt(k);
					k--;
					num--;
				}
			}
		}
		FunctionsCount = _activeOrderList.Count;
		FunctionNames.Clear();
		_activeOrderList.ForEach(delegate(StateOrderItem s)
		{
			string text = s.GetName();
			if (text.Length > highestCharacterCount)
			{
				highestCharacterCount = text.Length;
			}
			FunctionNames.Add(text);
		});
		for (int num2 = 0; num2 < FunctionsCount; num2++)
		{
			int num3 = highestCharacterCount - FunctionNames[num2].Length;
			for (int num4 = 0; num4 < num3; num4++)
			{
				FunctionNames[num2] += " ";
			}
		}
		UpdateScreen();
	}

	public void KID_SetVoiceChatSettingOnStart(bool voiceChatEnabled, Permission.ManagedByEnum managedBy, bool hasOptedInPreviously)
	{
		if (managedBy != Permission.ManagedByEnum.PROHIBITED)
		{
			SetVoice(voiceChatEnabled, !hasOptedInPreviously);
		}
	}

	private void SetVoice(bool setting, bool saveSetting = true)
	{
		voiceChatOn = (setting ? "TRUE" : "FALSE");
		if (setting && !KIDManager.CheckFeatureOptIn(EKIDFeatures.Voice_Chat).hasOptedInPreviously)
		{
			KIDManager.SetFeatureOptIn(EKIDFeatures.Voice_Chat, optedIn: true);
			KIDManager.SendOptInPermissions();
		}
		if (saveSetting)
		{
			PlayerPrefs.SetString("voiceChatOn", voiceChatOn);
			PlayerPrefs.Save();
		}
	}

	public bool CheckVoiceChatEnabled()
	{
		return voiceChatOn == "TRUE";
	}

	private void SetVoiceChatBySafety(bool voiceChatEnabled, Permission.ManagedByEnum managedBy)
	{
		bool isSafety = !voiceChatEnabled;
		SetComputerSettingsBySafety(isSafety, new ComputerState[3]
		{
			ComputerState.Voice,
			ComputerState.AutoMute,
			ComputerState.Mic
		}, shouldHide: false);
		string value = PlayerPrefs.GetString("voiceChatOn", "");
		if (KIDManager.KidEnabledAndReady)
		{
			Permission permissionDataByFeature = KIDManager.GetPermissionDataByFeature(EKIDFeatures.Voice_Chat);
			if (permissionDataByFeature != null)
			{
				(bool, bool) tuple = KIDManager.CheckFeatureOptIn(EKIDFeatures.Voice_Chat, permissionDataByFeature);
				if (tuple.Item1 && !tuple.Item2)
				{
					value = "FALSE";
				}
			}
			else
			{
				Debug.LogErrorFormat("[KID] Could not find permission data for [" + EKIDFeatures.Voice_Chat.ToStandardisedString() + "]");
			}
		}
		switch (managedBy)
		{
		case Permission.ManagedByEnum.GUARDIAN:
			if (KIDManager.GetPermissionDataByFeature(EKIDFeatures.Voice_Chat).Enabled)
			{
				if (string.IsNullOrEmpty(value))
				{
					voiceChatOn = "TRUE";
				}
				else
				{
					voiceChatOn = value;
				}
			}
			else
			{
				voiceChatOn = "FALSE";
			}
			break;
		case Permission.ManagedByEnum.PROHIBITED:
			voiceChatOn = "FALSE";
			break;
		case Permission.ManagedByEnum.PLAYER:
			if (string.IsNullOrEmpty(value))
			{
				voiceChatOn = (voiceChatEnabled ? "TRUE" : "FALSE");
			}
			else
			{
				voiceChatOn = value;
			}
			break;
		}
		RigContainer.RefreshAllRigVoices();
		Debug.Log("[KID] On Session Update - Voice Chat Permission changed - Has enabled voiceChat? [" + voiceChatEnabled + "]");
	}

	public void SetNametagSetting(bool setting, Permission.ManagedByEnum managedBy, bool hasOptedInPreviously)
	{
		switch (managedBy)
		{
		case Permission.ManagedByEnum.PROHIBITED:
			break;
		case Permission.ManagedByEnum.GUARDIAN:
		{
			int num = PlayerPrefs.GetInt(NameTagPlayerPref, 1);
			setting = setting && num == 1;
			UpdateNametagSetting(setting, saveSetting: false);
			break;
		}
		default:
			setting = PlayerPrefs.GetInt(NameTagPlayerPref, setting ? 1 : 0) == 1;
			UpdateNametagSetting(setting, !hasOptedInPreviously && setting);
			break;
		}
	}

	public static void RegisterOnNametagSettingChanged(Action<bool> callback)
	{
		onNametagSettingChangedAction = (Action<bool>)Delegate.Combine(onNametagSettingChangedAction, callback);
	}

	public static void UnregisterOnNametagSettingChanged(Action<bool> callback)
	{
		onNametagSettingChangedAction = (Action<bool>)Delegate.Remove(onNametagSettingChangedAction, callback);
	}

	private void UpdateNametagSetting(bool newSettingValue, bool saveSetting = true)
	{
		if (newSettingValue)
		{
			KIDManager.SetFeatureOptIn(EKIDFeatures.Custom_Nametags, optedIn: true);
		}
		NametagsEnabled = newSettingValue;
		NetworkSystem.Instance.SetMyNickName(NametagsEnabled ? savedName : NetworkSystem.Instance.GetMyDefaultName());
		if (NetworkSystem.Instance.InRoom)
		{
			GorillaTagger.Instance.myVRRig.SendRPC("RPC_InitializeNoobMaterial", RpcTarget.All, redValue, greenValue, blueValue);
		}
		onNametagSettingChangedAction?.Invoke(NametagsEnabled);
		if (saveSetting)
		{
			int value = (NametagsEnabled ? 1 : 0);
			PlayerPrefs.SetInt(NameTagPlayerPref, value);
			PlayerPrefs.Save();
		}
	}

	void IMatchmakingCallbacks.OnFriendListUpdate(List<Photon.Realtime.FriendInfo> friendList)
	{
	}

	void IMatchmakingCallbacks.OnCreatedRoom()
	{
	}

	void IMatchmakingCallbacks.OnCreateRoomFailed(short returnCode, string message)
	{
	}

	void IMatchmakingCallbacks.OnJoinedRoom()
	{
	}

	void IMatchmakingCallbacks.OnJoinRandomFailed(short returnCode, string message)
	{
	}

	void IMatchmakingCallbacks.OnLeftRoom()
	{
	}

	void IMatchmakingCallbacks.OnPreLeavingRoom()
	{
	}

	void IMatchmakingCallbacks.OnJoinRoomFailed(short returnCode, string message)
	{
		if (returnCode == 32765)
		{
			roomFull = true;
		}
	}

	public void SetInVirtualStump(bool inVirtualStump)
	{
		playerInVirtualStump = inVirtualStump;
		roomToJoin = (playerInVirtualStump ? (virtualStumpRoomPrepend + roomToJoin) : roomToJoin.RemoveAll(virtualStumpRoomPrepend));
	}

	public bool IsPlayerInVirtualStump()
	{
		return playerInVirtualStump;
	}

	public void SetLimitOnlineScreens(bool isLimited)
	{
		limitOnlineScreens = isLimited;
		UpdateScreen();
	}

	private void InitializeKIdState()
	{
		KIDManager.RegisterSessionUpdateCallback_AnyPermission(OnSessionUpdate_GorillaComputer);
	}

	private void UpdateKidState()
	{
		_currentScreentState = EKidScreenState.Ready;
	}

	private void RequestUpdatedPermissions()
	{
		if (KIDManager.KidEnabledAndReady && !_waitingForUpdatedSession && !(Time.realtimeSinceStartup < _nextUpdateAttemptTime))
		{
			_waitingForUpdatedSession = true;
			UpdateSession();
		}
	}

	private async void UpdateSession()
	{
		_nextUpdateAttemptTime = Time.realtimeSinceStartup + _updateAttemptCooldown;
		await KIDManager.UpdateSession();
		_waitingForUpdatedSession = false;
	}

	private void OnSessionUpdate_GorillaComputer()
	{
		UpdateKidState();
		UpdateScreen();
	}

	private void ProcessScreen_SetupKID()
	{
		if (!KIDManager.KidEnabledAndReady)
		{
			Debug.LogError("[KID] Unable to start k-ID Flow. Kid is disabled");
		}
	}

	private bool GuardianConsentMessage(string setupKIDButtonName, string featureDescription)
	{
		string defaultResult = "PARENT/GUARDIAN PERMISSION REQUIRED TO ";
		LocalisationManager.TryGetKeyForCurrentLocale("KID_PERMISSION_NEEDED", out var result, defaultResult);
		screenText.Append(result);
		screenText.Append(featureDescription + "!");
		if (_waitingForUpdatedSession)
		{
			defaultResult = "\n\nWAITING FOR PARENT/GUARDIAN CONSENT!";
			LocalisationManager.TryGetKeyForCurrentLocale("KID_WAITING_PERMISSION", out result, defaultResult);
			screenText.Append(result);
			return true;
		}
		if (Time.realtimeSinceStartup >= _nextUpdateAttemptTime)
		{
			defaultResult = "\n\nPRESS OPTION 2 TO REFRESH PERMISSIONS!";
			LocalisationManager.TryGetKeyForCurrentLocale("KID_REFRESH_PERMISSIONS", out result, defaultResult);
			screenText.Append(result);
		}
		else
		{
			defaultResult = "CHECK AGAIN IN {time} SECONDS!";
			LocalisationManager.TryGetKeyForCurrentLocale("KID_CHECK_AGAIN_COOLDOWN", out result, defaultResult);
			result = result.Replace("{time}", ((int)(_nextUpdateAttemptTime - Time.realtimeSinceStartup)).ToString());
			screenText.Append(result);
		}
		return false;
	}

	private void ProhibitedMessage(string verb)
	{
		_ = "\n\nYOU ARE NOT ALLOWED TO " + verb + " IN YOUR JURISDICTION.";
		LocalisationManager.TryGetKeyForCurrentLocale("KID_PROHIBITED_MESSAGE", out var result, "SET CUSTOM NICKNAMES");
		result = result.Replace("{verb}", verb);
		screenText.Append(result);
	}

	private void RoomScreen_Permission()
	{
		if (!KIDManager.KidEnabled)
		{
			string defaultResult = "YOU CANNOT USE THE PRIVATE ROOM FEATURE RIGHT NOW";
			LocalisationManager.TryGetKeyForCurrentLocale("ROOM_SCREEN_DISABLED", out var result, defaultResult);
			screenText.Set(result);
		}
		else
		{
			screenText.Set("");
			string defaultResult2 = "CREATE OR JOIN PRIVATE ROOMS";
			LocalisationManager.TryGetKeyForCurrentLocale("ROOM_SCREEN_KID_PROHIBITED_VERB", out var result2, defaultResult2);
			GuardianConsentMessage("OPTION 3", result2);
		}
	}

	private void RoomScreen_KIdProhibited()
	{
		string defaultResult = "CREATE OR JOIN PRIVATE ROOMS";
		LocalisationManager.TryGetKeyForCurrentLocale("ROOM_SCREEN_KID_PROHIBITED_VERB", out var result, defaultResult);
		ProhibitedMessage(result);
	}

	private void VoiceScreen_Permission()
	{
		string defaultResult = "VOICE TYPE: \"MONKE\"\n\n";
		LocalisationManager.TryGetKeyForCurrentLocale("VOICE_SCREEN_KID_CURRENT_VOICE", out var result, defaultResult);
		screenText.Set(result);
		if (!KIDManager.KidEnabled)
		{
			defaultResult = "YOU CANNOT USE THE HUMAN VOICE TYPE FEATURE RIGHT NOW";
			LocalisationManager.TryGetKeyForCurrentLocale("VOICE_SCREEN_DISABLED", out result, defaultResult);
			screenText.Append(result);
		}
		else
		{
			defaultResult = "ENABLE HUMAN VOICE CHAT";
			LocalisationManager.TryGetKeyForCurrentLocale("VOICE_SCREEN_GUARDIAN_FEATURE_DESC", out result, defaultResult);
			GuardianConsentMessage("OPTION 3", result);
		}
	}

	private void VoiceScreen_KIdProhibited()
	{
		string defaultResult = "USE THE VOICE CHAT";
		LocalisationManager.TryGetKeyForCurrentLocale("VOICE_SCREEN_KID_PROHIBITED_VERB", out var result, defaultResult);
		ProhibitedMessage(result);
	}

	private void MicScreen_Permission()
	{
		screenText.Set("");
		string defaultResult = "ENABLE HUMAN VOICE CHAT";
		LocalisationManager.TryGetKeyForCurrentLocale("VOICE_SCREEN_GUARDIAN_FEATURE_DESC", out var result, defaultResult);
		GuardianConsentMessage("OPTION 3", result);
	}

	private void MicScreen_KIdProhibited()
	{
		VoiceScreen_KIdProhibited();
	}

	private void NameScreen_Permission()
	{
		if (!KIDManager.KidEnabled)
		{
			string defaultResult = "YOU CANNOT USE THE CUSTOM NICKNAME FEATURE RIGHT NOW";
			LocalisationManager.TryGetKeyForCurrentLocale("NAME_SCREEN_DISABLED", out var result, defaultResult);
			screenText.Append(result);
		}
		else
		{
			screenText.Set("");
			LocalisationManager.TryGetKeyForCurrentLocale("NAME_SCREEN_KID_PROHIBITED_VERB", out var result2, "SET CUSTOM NICKNAMES");
			GuardianConsentMessage("OPTION 3", result2);
		}
	}

	private void NameScreen_KIdProhibited()
	{
		LocalisationManager.TryGetKeyForCurrentLocale("NAME_SCREEN_KID_PROHIBITED_VERB", out var result, "SET CUSTOM NICKNAMES");
		ProhibitedMessage(result);
	}

	private void OnKIDSessionUpdated_CustomNicknames(bool showCustomNames, Permission.ManagedByEnum managedBy)
	{
		bool flag = (showCustomNames || managedBy == Permission.ManagedByEnum.PLAYER) && managedBy != Permission.ManagedByEnum.PROHIBITED;
		SetComputerSettingsBySafety(!flag, new ComputerState[1] { ComputerState.Name }, shouldHide: false);
		int num = PlayerPrefs.GetInt(NameTagPlayerPref, -1);
		bool flag2 = num > 0;
		switch (managedBy)
		{
		case Permission.ManagedByEnum.GUARDIAN:
			NametagsEnabled = showCustomNames && (flag2 || num == -1);
			break;
		case Permission.ManagedByEnum.PLAYER:
			if (showCustomNames)
			{
				NametagsEnabled = num == -1 || flag2;
			}
			else
			{
				NametagsEnabled = num != -1 && flag2;
			}
			break;
		case Permission.ManagedByEnum.PROHIBITED:
			NametagsEnabled = false;
			break;
		}
		if (NametagsEnabled)
		{
			NetworkSystem.Instance.SetMyNickName(savedName);
		}
		onNametagSettingChangedAction?.Invoke(NametagsEnabled);
	}

	private void TroopScreen_Permission()
	{
		screenText.Set("");
		if (!KIDManager.KidEnabled)
		{
			LocalisationManager.TryGetKeyForCurrentLocale("TROOP_SCREEN_DISABLED", out var result, "YOU CANNOT USE THE TROOPS FEATURE RIGHT NOW");
			screenText.Append(result);
		}
		else
		{
			LocalisationManager.TryGetKeyForCurrentLocale("TROOP_SCREEN_KID_DESC", out var result2, "JOIN TROOPS");
			GuardianConsentMessage("OPTION 3", result2);
		}
	}

	private void TroopScreen_KIdProhibited()
	{
		LocalisationManager.TryGetKeyForCurrentLocale("TROOP_SCREEN_KID_PROHIBITED_VERB", out var result, "CREATE OR JOIN TROOPS");
		ProhibitedMessage(result);
	}

	private void ProcessKIdState(GorillaKeyboardBindings buttonPressed)
	{
		if (buttonPressed == GorillaKeyboardBindings.option1 && _currentScreentState == EKidScreenState.Ready)
		{
			RequestUpdatedPermissions();
		}
	}

	private void KIdScreen()
	{
		if (KIDManager.KidEnabledAndReady)
		{
			if (!KIDManager.HasSession)
			{
				GuardianConsentMessage("OPTION 3", "");
			}
			else
			{
				KIdScreen_DisplayPermissions();
			}
		}
	}

	private void KIdScreen_DisplayPermissions()
	{
		AgeStatusType activeAccountStatus = KIDManager.GetActiveAccountStatus();
		string text = ((!KIDManager.InitialisationSuccessful) ? "NOT READY" : activeAccountStatus.ToString());
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("k-ID Account Status:\t" + text);
		if (activeAccountStatus == (AgeStatusType)0)
		{
			stringBuilder.AppendLine("\nPress 'OPTION 1' to get permissions!");
			screenText.Set(stringBuilder.ToString());
			return;
		}
		if (_waitingForUpdatedSession)
		{
			stringBuilder.AppendLine("\nWAITING FOR PARENT/GUARDIAN CONSENT!");
			screenText.Set(stringBuilder.ToString());
			return;
		}
		stringBuilder.AppendLine("\nPermissions:");
		List<Permission> allPermissionsData = KIDManager.GetAllPermissionsData();
		int count = allPermissionsData.Count;
		int num = 1;
		for (int i = 0; i < count; i++)
		{
			if (Enumerable.Contains(_interestedPermissionNames, allPermissionsData[i].Name))
			{
				string text2 = (allPermissionsData[i].Enabled ? "<color=#85ffa5>" : "<color=\"RED\">");
				stringBuilder.AppendLine("[" + num + "] " + text2 + allPermissionsData[i].Name + "</color>");
				num++;
			}
		}
		stringBuilder.AppendLine("\nTO REFRESH PERMISSIONS PRESS OPTION 1!");
		screenText.Set(stringBuilder.ToString());
	}

	private string GetLocalisedLanguageScreen()
	{
		return GetLanguageScreenLocalisation();
	}

	private void GetLangaugesList(ref string langStr)
	{
		_languagesDisplaySB.Clear();
		int maxLength = 12;
		int num = 3;
		int num2 = 0;
		StringBuilder stringBuilder = new StringBuilder();
		foreach (KeyValuePair<int, Locale> allBinding in LocalisationManager.GetAllBindings())
		{
			num2++;
			string text = LocalisationManager.LocaleToFriendlyString(allBinding.Value).ToUpper();
			string value = $"{allBinding.Key}) {text}";
			stringBuilder.Append(value);
			int remainingChars = GetRemainingChars(text, maxLength);
			stringBuilder.Append(' ', remainingChars);
			if (num2 >= num)
			{
				_languagesDisplaySB.AppendLine(stringBuilder.ToString());
				stringBuilder.Clear();
				num2 = 0;
			}
		}
		_languagesDisplaySB.AppendLine(stringBuilder.ToString());
		langStr = langStr + _languagesDisplaySB.ToString() + "\n";
	}

	private int GetRemainingChars(string value, int maxLength)
	{
		int num = 0;
		if (value == "日本語")
		{
			return (LocalisationManager.CurrentLanguage.Identifier.Code == "ja") ? 7 : 7;
		}
		return Mathf.Clamp(maxLength - value.Length, 0, maxLength);
	}

	private string GetLanguageScreenLocalisation()
	{
		string text = "";
		LocalisationManager.TryGetKeyForCurrentLocale("LANG_SCREEN_TITLE", out var result, "CHOOSE YOUR LANGUAGE\n");
		text += result;
		GetLangaugesList(ref text);
		LocalisationManager.TryGetKeyForCurrentLocale("LANG_SCREEN_INSTRUCTIONS", out result, "PRESS NUMBER KEYS TO CHOOSE A LANGUAGE\n");
		text += result;
		LocalisationManager.TryGetKeyForCurrentLocale("LANG_SCREEN_CURRENT_LANGUAGE", out result, "CURRENT LANGUAGE: ");
		return text + result.TrailingSpace() + LocalisationManager.LocaleToFriendlyString().ToUpper();
	}

	private void InitialiseLanguageScreen()
	{
		_previousLocalisationSetting = LocalisationManager.CurrentLanguage;
		LocalisationManager.RegisterOnLanguageChanged(OnLanguageChanged);
	}

	private void LanguageScreen()
	{
		screenText.Set(GetLocalisedLanguageScreen());
	}

	private void ProcessLanguageState(GorillaKeyboardBindings buttonPressed)
	{
		if (buttonPressed.FromNumberBindingToInt(out var result) && LocalisationManager.TryGetLocaleBinding(result, out var loc))
		{
			LocalisationManager.Instance.OnLanguageButtonPressed(loc.Identifier.Code, saveLanguage: true);
			RefreshFunctionNames();
		}
	}

	private void OnLanguageChanged()
	{
		if (_previousLocalisationSetting == LocalisationManager.CurrentLanguage)
		{
			Debug.Log("[LOCALISATION::GORILLA_COMPUTER] Language changed, but no different to previous setting [" + _previousLocalisationSetting.ToString() + "]");
			return;
		}
		_previousLocalisationSetting = LocalisationManager.CurrentLanguage;
		RefreshFunctionNames();
	}

	private void RefreshFunctionNames()
	{
		FunctionNames.Clear();
		FunctionsCount = OrderList.Count;
		highestCharacterCount = int.MinValue;
		OrderList.ForEach(delegate(StateOrderItem s)
		{
			string text = s.GetName();
			if (text.Length > highestCharacterCount)
			{
				highestCharacterCount = text.Length;
			}
			FunctionNames.Add(text);
		});
		for (int num = 0; num < FunctionsCount; num++)
		{
			int num2 = highestCharacterCount - FunctionNames[num].Length;
			for (int num3 = 0; num3 < num2; num3++)
			{
				FunctionNames[num] += " ";
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GorillaNetworking;
using KID.Model;
using Newtonsoft.Json;
using PlayFab;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.Interaction.Toolkit;

public class KIDManager : MonoBehaviour
{
	public delegate void OnEmailResultReceived(bool result);

	public const string MULTIPLAYER_PERMISSION_NAME = "multiplayer";

	public const string UGC_PERMISSION_NAME = "mods";

	public const string PRIVATE_ROOM_PERMISSION_NAME = "join-groups";

	public const string VOICE_CHAT_PERMISSION_NAME = "voice-chat";

	public const string CUSTOM_USERNAME_PERMISSION_NAME = "custom-username";

	public const string PREVIOUS_STATUS_PREF_KEY_PREFIX = "previous-status-";

	public const string KID_DATA_KEY = "KIDData";

	private const string KID_EMAIL_KEY = "k-id_EmailAddress";

	private const int SECONDS_BETWEEN_UPDATE_ATTEMPTS = 30;

	private const string KID_SETUP_FLAG = "KID-Setup-";

	[OnEnterPlay_SetNull]
	private static KIDManager _instance;

	private static string _emailAddress;

	private static CancellationTokenSource _requestCancellationSource = new CancellationTokenSource();

	private static bool _titleDataReady = false;

	private static bool _useKid = false;

	private static int _kIDPhase = 0;

	private static DateTime? _kIDNewPlayerDateTime = null;

	private static string _debugKIDLocalePlayerPrefRef = "KID_SPOOF_LOCALE";

	private static string parentEmailForUserPlayerPrefRef;

	[OnEnterPlay_SetNull]
	private static Action _sessionUpdatedCallback = null;

	[OnEnterPlay_SetNull]
	private static Action _onKIDInitialisationComplete = null;

	public static OnEmailResultReceived onEmailResultReceived;

	private const string KID_GET_SESSION = "GetPlayerData";

	private const string KID_VERIFY_AGE = "VerifyAge";

	private const string KID_UPGRADE_SESSION = "UpgradeSession";

	private const string KID_SEND_CHALLENGE_EMAIL = "SendChallengeEmail";

	private const string KID_ATTEMPT_AGE_UPDATE = "AttemptAgeUpdate";

	private const string KID_APPEAL_AGE = "AppealAge";

	private const string KID_OPT_IN = "OptIn";

	private const string KID_GET_REQUIREMENTS = "GetRequirements";

	private const string KID_SET_CONFIRMED_STATUS = "SetConfirmedStatus";

	private const string KID_SET_OPT_IN_PERMISSIONS = "SetOptInPermissions";

	private const string KID_FORCE_REFRESH = "sessionRefresh";

	private const int MAX_RETRIES_FOR_CRITICAL_KID_SERVER_REQUESTS = 3;

	private const int MAX_RETRIES_FOR_NORMAL_KID_SERVER_REQUESTS = 2;

	public const string KID_PERMISSION__VOICE_CHAT = "voice-chat";

	public const string KID_PERMISSION__CUSTOM_NAMES = "custom-username";

	public const string KID_PERMISSION__PRIVATE_ROOMS = "join-groups";

	public const string KID_PERMISSION__MULTIPLAYER = "multiplayer";

	public const string KID_PERMISSION__UGC = "mods";

	private const float MAX_SESSION_UPDATE_TIME = 600f;

	private const int TIME_BETWEEN_SESSION_UPDATE_ATTEMPTS = 30;

	[OnEnterPlay_SetNull]
	private static Action _onSessionUpdated_AnyPermission;

	[OnEnterPlay_SetNull]
	private static Action<bool, Permission.ManagedByEnum> _onSessionUpdated_VoiceChat;

	[OnEnterPlay_SetNull]
	private static Action<bool, Permission.ManagedByEnum> _onSessionUpdated_CustomUsernames;

	[OnEnterPlay_SetNull]
	private static Action<bool, Permission.ManagedByEnum> _onSessionUpdated_PrivateRooms;

	[OnEnterPlay_SetNull]
	private static Action<bool, Permission.ManagedByEnum> _onSessionUpdated_Multiplayer;

	[OnEnterPlay_SetNull]
	private static Action<bool, Permission.ManagedByEnum> _onSessionUpdated_UGC;

	private static bool _isUpdatingNewSession = false;

	[OnEnterPlay_SetNull]
	private static Dictionary<string, Permission> _previousPermissionSettings = new Dictionary<string, Permission>();

	public static KIDManager Instance => _instance;

	public static bool InitialisationComplete { get; private set; } = false;

	public static bool InitialisationSuccessful { get; private set; } = false;

	public static TMPSession CurrentSession { get; private set; }

	public static SessionStatus PreviousStatus { get; private set; }

	public static GetRequirementsData _ageGateRequirements { get; private set; }

	public static bool KidTitleDataReady => _titleDataReady;

	public static bool KidEnabled
	{
		get
		{
			if (KidTitleDataReady)
			{
				return _useKid;
			}
			return false;
		}
	}

	public static bool KidEnabledAndReady
	{
		get
		{
			if (KidEnabled)
			{
				return InitialisationSuccessful;
			}
			return false;
		}
	}

	public static bool HasSession
	{
		get
		{
			if (CurrentSession != null)
			{
				return CurrentSession.SessionId != Guid.Empty;
			}
			return false;
		}
	}

	public static string PreviousStatusPlayerPrefRef => "previous-status-" + PlayFabAuthenticator.instance.GetPlayFabPlayerId();

	public static bool HasOptedInToKID { get; private set; }

	private static string KIDSetupPlayerPref => "KID-Setup-";

	public static string DbgLocale { get; set; }

	public static string DebugKIDLocalePlayerPrefRef => _debugKIDLocalePlayerPrefRef;

	public static string GetEmailForUserPlayerPrefRef
	{
		get
		{
			if (string.IsNullOrEmpty(parentEmailForUserPlayerPrefRef))
			{
				parentEmailForUserPlayerPrefRef = "k-id_EmailAddress" + PlayFabAuthenticator.instance.GetPlayFabPlayerId();
			}
			return parentEmailForUserPlayerPrefRef;
		}
	}

	public static string GetChallengedBeforePlayerPrefRef => "k-id_ChallengedBefore" + PlayFabAuthenticator.instance.GetPlayFabPlayerId();

	private void Awake()
	{
		if (_instance != null)
		{
			Debug.LogError("Trying to create new instance of [KIDManager], but one already exists. Destroying object [" + base.gameObject.name + "].");
			UnityEngine.Object.Destroy(base.gameObject);
		}
		else
		{
			_instance = this;
			DbgLocale = PlayerPrefs.GetString(_debugKIDLocalePlayerPrefRef, "");
		}
	}

	private async void Start()
	{
		_useKid = await UseKID();
		_kIDPhase = await CheckKIDPhase();
		_kIDNewPlayerDateTime = await CheckKIDNewPlayerDateTime();
		_titleDataReady = true;
	}

	private void OnDestroy()
	{
		_requestCancellationSource.Cancel();
	}

	public static string GetActiveAccountStatusNiceString()
	{
		return GetActiveAccountStatus() switch
		{
			AgeStatusType.DIGITALMINOR => "Digital Minor", 
			AgeStatusType.DIGITALYOUTH => "Digital Youth", 
			AgeStatusType.LEGALADULT => "Legal Adult", 
			_ => "UNKNOWN", 
		};
	}

	public static AgeStatusType GetActiveAccountStatus()
	{
		if (CurrentSession == null)
		{
			if (!PlayFabAuthenticator.instance.GetSafety())
			{
				return AgeStatusType.LEGALADULT;
			}
			return AgeStatusType.DIGITALMINOR;
		}
		return CurrentSession.AgeStatus;
	}

	public static List<Permission> GetAllPermissionsData()
	{
		if (CurrentSession == null)
		{
			Debug.LogError("[KID::MANAGER] There is no current session. Unless the age-gate has not yet finished there should always be a session even if it is the default session");
			return new List<Permission>();
		}
		return CurrentSession.GetAllPermissions();
	}

	public static bool TryGetAgeStatusTypeFromAge(int age, out AgeStatusType ageType)
	{
		if (_ageGateRequirements == null)
		{
			Debug.LogError("[KID::MANAGER] [_ageGateRequirements] is not set - need to Get AgeGate Requirements first");
			ageType = AgeStatusType.DIGITALMINOR;
			return false;
		}
		if (age < _ageGateRequirements.AgeGateRequirements.DigitalConsentAge)
		{
			ageType = AgeStatusType.DIGITALMINOR;
			return true;
		}
		if (age < _ageGateRequirements.AgeGateRequirements.CivilAge)
		{
			ageType = AgeStatusType.DIGITALYOUTH;
			return true;
		}
		ageType = AgeStatusType.LEGALADULT;
		return true;
	}

	public static (bool requiresOptIn, bool hasOptedInPreviously) CheckFeatureOptIn(EKIDFeatures feature, Permission permissionData = null)
	{
		if (permissionData == null)
		{
			permissionData = GetPermissionDataByFeature(feature);
			if (permissionData == null)
			{
				Debug.LogError("[KID::MANAGER] Unable to retrieve permission data for feature [" + feature.ToStandardisedString() + "]");
				return (requiresOptIn: false, hasOptedInPreviously: false);
			}
		}
		if (permissionData.ManagedBy == Permission.ManagedByEnum.PROHIBITED)
		{
			return (requiresOptIn: false, hasOptedInPreviously: false);
		}
		bool item = true;
		if (CurrentSession != null)
		{
			item = CurrentSession.HasOptedInToPermission(feature);
		}
		if (permissionData.ManagedBy == Permission.ManagedByEnum.GUARDIAN)
		{
			return (requiresOptIn: false, hasOptedInPreviously: item);
		}
		if (permissionData.ManagedBy == Permission.ManagedByEnum.PLAYER && permissionData.Enabled)
		{
			return (requiresOptIn: false, hasOptedInPreviously: true);
		}
		return (requiresOptIn: true, hasOptedInPreviously: item);
	}

	public static void SetFeatureOptIn(EKIDFeatures feature, bool optedIn)
	{
		Permission permissionDataByFeature = GetPermissionDataByFeature(feature);
		if (permissionDataByFeature == null)
		{
			Debug.LogErrorFormat("[KID] Trying to set Feature Opt in for feature [" + feature.ToStandardisedString() + "] but permission data could not be found. Assumed is opt-in");
			return;
		}
		if (CurrentSession == null)
		{
			Debug.Log("[KID::MANAGER] CurrentSession is null, cannot set feature opt-in. Returning.");
			return;
		}
		switch (permissionDataByFeature.ManagedBy)
		{
		case Permission.ManagedByEnum.GUARDIAN:
			CurrentSession.OptInToPermission(feature, permissionDataByFeature.Enabled);
			break;
		case Permission.ManagedByEnum.PLAYER:
			CurrentSession.OptInToPermission(feature, optedIn);
			break;
		case Permission.ManagedByEnum.PROHIBITED:
			CurrentSession.OptInToPermission(feature, optIn: false);
			break;
		}
	}

	public static bool CheckFeatureSettingEnabled(EKIDFeatures feature)
	{
		Permission permissionDataByFeature = GetPermissionDataByFeature(feature);
		if (permissionDataByFeature == null)
		{
			Debug.LogError("[KID::MANAGER] Unable to permissions for feature [" + feature.ToStandardisedString() + "]");
			return false;
		}
		if (permissionDataByFeature.ManagedBy == Permission.ManagedByEnum.PROHIBITED)
		{
			return false;
		}
		bool item = CheckFeatureOptIn(feature).hasOptedInPreviously;
		switch (feature)
		{
		case EKIDFeatures.Groups:
			if (permissionDataByFeature.ManagedBy == Permission.ManagedByEnum.GUARDIAN)
			{
				return permissionDataByFeature.Enabled;
			}
			return true;
		case EKIDFeatures.Multiplayer:
		case EKIDFeatures.Mods:
			return item;
		case EKIDFeatures.Custom_Nametags:
			if (item)
			{
				return GorillaComputer.instance.NametagsEnabled;
			}
			return false;
		case EKIDFeatures.Voice_Chat:
			if (item)
			{
				return GorillaComputer.instance.CheckVoiceChatEnabled();
			}
			return false;
		default:
			Debug.LogError("[KID::MANAGER] Tried finding feature setting for [" + feature.ToStandardisedString() + "] but failed.");
			return false;
		}
	}

	private static async Task<GetPlayerData_Data> TryGetPlayerData(bool forceRefresh)
	{
		return await Server_GetPlayerData(forceRefresh, null);
	}

	private static async Task<GetRequirementsData> TryGetRequirements()
	{
		return await Server_GetRequirements();
	}

	private static async Task<VerifyAgeData> TryVerifyAgeResponse()
	{
		PlayerPlatform value = PlayerPlatform.Steam;
		VerifyAgeRequest request = new VerifyAgeRequest
		{
			Age = KIDAgeGate.UserAge,
			Platform = value
		};
		Debug.Log($"[KID::MANAGER] Sending verify age request for age: [{KIDAgeGate.UserAge}]");
		return await Server_VerifyAge(request, null);
	}

	private static async Task<(bool success, string exception)> TrySendChallengeEmailRequest()
	{
		do
		{
			await Task.Yield();
		}
		while (string.IsNullOrEmpty(_emailAddress));
		var (flag, item) = await Server_SendChallengeEmail(new SendChallengeEmailRequest
		{
			Email = _emailAddress,
			Locale = (string.IsNullOrEmpty(DbgLocale) ? CultureInfo.CurrentCulture.Name : DbgLocale)
		});
		if (flag)
		{
			onEmailResultReceived?.Invoke(result: true);
		}
		else
		{
			onEmailResultReceived?.Invoke(result: false);
		}
		return (success: flag, exception: item);
	}

	private static async Task<bool> TrySendOptInPermissions()
	{
		string[] optedInPermissions = CurrentSession.GetOptedInPermissions();
		if (optedInPermissions == null)
		{
			Debug.LogError("[KID::MANAGER::OptInRefactor] Tried to set opt-in permissions but no permissions were provided");
			return false;
		}
		Debug.Log("[KID::MANAGER::OptInRefactor] Setting Opt-in Permissions: " + string.Join(", ", optedInPermissions));
		return await Server_SetOptInPermissions(new SetOptInPermissionsRequest
		{
			OptInPermissions = optedInPermissions
		}, null);
	}

	public static async Task<(bool, string)> TrySendUpgradeSessionChallengeEmail()
	{
		var (item, item2) = await Server_SendChallengeEmail(new SendChallengeEmailRequest());
		return (item, item2);
	}

	public static async Task<bool> TrySetHasConfirmedStatus()
	{
		return await Server_SetConfirmedStatus();
	}

	public static async Task<UpgradeSessionData> TryUpgradeSession(List<string> requestedPermissions)
	{
		UpgradeSessionData upgradeSessionData = await Server_UpgradeSession(new UpgradeSessionRequest
		{
			Permissions = requestedPermissions.Select((string name) => new RequestedPermission(name)).ToList()
		});
		if (upgradeSessionData == null)
		{
			Debug.LogError("[KID::MANAGER] Failed to upgrade session. Data is null.");
			return null;
		}
		UpdatePermissions(upgradeSessionData.session);
		return upgradeSessionData;
	}

	public static async Task<AttemptAgeUpdateData> TryAttemptAgeUpdate(int age)
	{
		PlayerPlatform platform = PlayerPlatform.Steam;
		AttemptAgeUpdateRequest request = new AttemptAgeUpdateRequest
		{
			Age = age,
			Platform = platform
		};
		Debug.Log($"[KID::MANAGER] Sending age update request for age: [{age}]");
		return await Server_AttemptAgeUpdate(request, null);
	}

	public static async Task<bool> TryAppealAge(string email, int newAge)
	{
		string locale = (string.IsNullOrEmpty(DbgLocale) ? CultureInfo.CurrentCulture.Name : DbgLocale);
		AppealAgeRequest request = new AppealAgeRequest
		{
			Age = newAge,
			Email = email,
			Locale = locale
		};
		Debug.Log($"[KID::MANAGER] Sending age appeal request for age: [{newAge}] at email [{email}]");
		return await Server_AppealAge(request, null);
	}

	public static async Task UpdateSession(Action<bool> getDataCompleted = null)
	{
		GetPlayerData_Data getPlayerData_Data = await TryGetPlayerData(forceRefresh: true);
		if (getPlayerData_Data == null)
		{
			getDataCompleted?.Invoke(obj: false);
			Debug.LogError("[KID::MANAGER] Failed to retrieve session");
		}
		else if (getPlayerData_Data.responseType == GetSessionResponseType.ERROR)
		{
			getDataCompleted?.Invoke(obj: false);
			Debug.LogError("[KID::MANAGER] Failed to get session. Resulted in error. Cannot update session");
		}
		else
		{
			getDataCompleted?.Invoke(obj: true);
			UpdatePermissions(getPlayerData_Data.session);
		}
	}

	private static async Task<bool> CheckWarningScreensOptedIn()
	{
		if (GorillaServer.Instance.CheckOptedInKID())
		{
			return true;
		}
		PrivateUIRoom.ForceStartOverlay(PrivateUIRoom.OverlaySource.KID);
		switch (await WarningScreens.StartWarningScreen(_requestCancellationSource.Token))
		{
		case WarningButtonResult.None:
			if (_requestCancellationSource.IsCancellationRequested)
			{
				return false;
			}
			GorillaServer.Instance.CheckIsInKIDOptInCohort();
			GorillaServer.Instance.CheckIsInKIDRequiredCohort();
			return false;
		case WarningButtonResult.CloseWarning:
			_ = _requestCancellationSource.IsCancellationRequested;
			return false;
		case WarningButtonResult.OptIn:
			if (!(await Server_OptIn()))
			{
				Debug.LogError("[KID::MANAGER] PHASE ONE (A) -- FAILURE - Opting in to k-ID failed!");
				return false;
			}
			if (CosmeticsController.instance != null)
			{
				CosmeticsController.instance.GetCurrencyBalance();
			}
			await WarningScreens.StartOptInFollowUpScreen(_requestCancellationSource.Token);
			break;
		}
		return true;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	public static void InitialiseBootFlow()
	{
		if (PlayerPrefs.GetInt(KIDSetupPlayerPref, 0) == 0)
		{
			PrivateUIRoom.ForceStartOverlay(PrivateUIRoom.OverlaySource.KID);
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
	public static async void InitialiseKID()
	{
		bool snapTurnDisabled = false;
		float? cachedTapHapticsStrength = null;
		object obj = null;
		int num = 0;
		try
		{
			bool num2 = !(await WaitForAuthentication());
			UGCPermissionManager.UsePlayFabSafety();
			if (!num2 && _useKid)
			{
				GorillaSnapTurn.DisableSnapTurn();
				snapTurnDisabled = true;
				if (GorillaTagger.Instance != null)
				{
					cachedTapHapticsStrength = GorillaTagger.Instance.tapHapticStrength;
					GorillaTagger.Instance.tapHapticStrength = 0f;
				}
				GetPlayerData_Data newSessionData = await TryGetPlayerData(forceRefresh: true);
				if (!_requestCancellationSource.IsCancellationRequested)
				{
					if (newSessionData == null)
					{
						Debug.LogError("[KID::MANAGER] [newSessionData] returned NULL. Something went wrong, we should always get a [GetPlayerData_Data]. Disabling k-ID");
					}
					else if (newSessionData.responseType == GetSessionResponseType.ERROR)
					{
						Debug.LogError("[KID::MANAGER] Failed to retrieve Player Data, response type: [" + newSessionData.responseType.ToString() + "]. Unable to proceed. Will default to Using Safeties");
					}
					else
					{
						HasOptedInToKID = newSessionData.responseType != GetSessionResponseType.NOT_FOUND;
						bool flag = await CheckWarningScreensOptedIn();
						if (!_requestCancellationSource.IsCancellationRequested && flag)
						{
							PreviousStatus = (SessionStatus)PlayerPrefs.GetInt(PreviousStatusPlayerPrefRef, 0);
							TMPSession newSession = newSessionData.session;
							_ = newSessionData.session?.AgeStatus;
							_ageGateRequirements = await TryGetRequirements();
							KIDAgeGate.SetAgeGateConfig(_ageGateRequirements);
							if (_ageGateRequirements != null)
							{
								_ = _ageGateRequirements.AgeGateRequirements;
							}
							if (newSessionData.status == SessionStatus.PROHIBITED || newSessionData.status == SessionStatus.PENDING_AGE_APPEAL)
							{
								PrivateUIRoom.ForceStartOverlay(PrivateUIRoom.OverlaySource.KID);
								KIDUI_AgeAppealController.Instance.StartAgeAppealScreens(newSessionData.status);
							}
							else
							{
								TMPSession session = newSessionData.session;
								int num3;
								if (session == null)
								{
									num3 = 1;
								}
								else
								{
									_ = session.AgeStatus;
									num3 = 0;
								}
								if (num3 != 0)
								{
									PrivateUIRoom.ForceStartOverlay(PrivateUIRoom.OverlaySource.KID);
									(AgeStatusType, TMPSession) obj2 = await AgeGateFlow(newSessionData);
									_ = obj2.Item1;
									TMPSession item = obj2.Item2;
									if (_requestCancellationSource.IsCancellationRequested)
									{
										goto IL_06bd;
									}
									newSession = item;
								}
								if (LegalAgreements.instance != null)
								{
									await LegalAgreements.instance.StartLegalAgreements();
									if (_requestCancellationSource.IsCancellationRequested)
									{
										goto IL_06bd;
									}
								}
								if (UpdatePermissions(newSession) && CurrentSession != null)
								{
									if (CurrentSession.IsDefault)
									{
										WaitForAndUpdateNewSession(forceRefresh: true);
									}
									if (!_requestCancellationSource.IsCancellationRequested)
									{
										UGCPermissionManager.UseKID();
										await KIDUI_Controller.Instance.StartKIDScreens(_requestCancellationSource.Token);
										while (!_requestCancellationSource.IsCancellationRequested)
										{
											await Task.Yield();
											if (KIDUI_Controller.IsKIDUIActive)
											{
												continue;
											}
											if (_requestCancellationSource.IsCancellationRequested)
											{
												break;
											}
											if (CurrentSession == null)
											{
												Debug.LogError("[KID::MANAGER] PHASE SEVEN -- FAILURE -- CurrentSession is NULL, should at least have a default session!");
												Debug.Log($"[KID::MANAGER] Safeties is: [{PlayFabAuthenticator.instance.GetSafety()}");
												break;
											}
											if (!newSessionData.HasConfirmedSetup)
											{
												await KIDMessagingController.StartKIDConfirmationScreen(_requestCancellationSource.Token);
											}
											PlayerPrefs.SetInt(PreviousStatusPlayerPrefRef, (int)PreviousStatus);
											PlayerPrefs.Save();
											InitialisationSuccessful = true;
											goto end_IL_00b2;
										}
									}
								}
							}
						}
					}
				}
			}
			goto IL_06bd;
			IL_06bd:
			num = 1;
			end_IL_00b2:;
		}
		catch (object obj3)
		{
			obj = obj3;
		}
		InitialisationComplete = true;
		if (!InitialisationSuccessful)
		{
			if (cachedTapHapticsStrength.HasValue && GorillaTagger.Instance != null)
			{
				GorillaTagger.Instance.tapHapticStrength = cachedTapHapticsStrength.Value;
			}
			if (snapTurnDisabled)
			{
				GorillaSnapTurn.LoadSettingsFromCache();
			}
			if (LegalAgreements.instance != null)
			{
				await LegalAgreements.instance.StartLegalAgreements();
			}
			PrivateUIRoom.StopForcedOverlay(PrivateUIRoom.OverlaySource.KID);
		}
		object obj4 = obj;
		if (obj4 != null)
		{
			ExceptionDispatchInfo.Capture((obj4 as Exception) ?? throw obj4).Throw();
		}
		if (num != 1)
		{
			UGCPermissionManager.UseKID();
			if (CurrentSession == null)
			{
				PlayFabAuthenticator.instance.GetSafety();
			}
			if (cachedTapHapticsStrength.HasValue && GorillaTagger.Instance != null)
			{
				GorillaTagger.Instance.tapHapticStrength = cachedTapHapticsStrength.Value;
			}
			if (snapTurnDisabled)
			{
				GorillaSnapTurn.LoadSettingsFromCache();
			}
			PrivateUIRoom.StopForcedOverlay(PrivateUIRoom.OverlaySource.KID);
		}
	}

	private static bool UpdatePermissions(TMPSession newSession)
	{
		if (newSession == null || !newSession.IsValidSession)
		{
			Debug.LogError("[KID::MANAGER] A NULL or Invalid Session was received!");
			return false;
		}
		CurrentSession = newSession;
		if (KIDUI_Controller.IsKIDUIActive)
		{
			PreviousStatus = CurrentSession.SessionStatus;
			PlayerPrefs.SetInt(PreviousStatusPlayerPrefRef, (int)PreviousStatus);
			PlayerPrefs.Save();
		}
		if (!CurrentSession.IsDefault)
		{
			PlayerPrefs.SetInt(KIDSetupPlayerPref, 1);
			PlayerPrefs.Save();
		}
		OnSessionUpdated();
		if ((bool)KIDUI_Controller.Instance)
		{
			KIDUI_Controller.Instance.UpdateScreenStatus();
		}
		return true;
	}

	private static void ClearSession()
	{
		CurrentSession = null;
		DeleteStoredPermissions();
	}

	private static void DeleteStoredPermissions()
	{
	}

	public static CancellationTokenSource ResetCancellationToken()
	{
		_requestCancellationSource.Dispose();
		_requestCancellationSource = new CancellationTokenSource();
		return _requestCancellationSource;
	}

	public static Permission GetPermissionDataByFeature(EKIDFeatures feature)
	{
		if (CurrentSession == null)
		{
			if (!PlayFabAuthenticator.instance.GetSafety())
			{
				return new Permission(feature.ToStandardisedString(), enabled: true, Permission.ManagedByEnum.PLAYER);
			}
			return new Permission(feature.ToStandardisedString(), enabled: false, Permission.ManagedByEnum.GUARDIAN);
		}
		if (!CurrentSession.TryGetPermission(feature, out var permission))
		{
			Debug.LogError("[KID::MANAGER] Failed to retreive permission from session for [" + feature.ToStandardisedString() + "]. Assuming disabled permission");
			return new Permission(feature.ToStandardisedString(), enabled: false, Permission.ManagedByEnum.GUARDIAN);
		}
		return permission;
	}

	public static void CancelToken()
	{
		_requestCancellationSource.Cancel();
	}

	public static async Task<bool> UseKID()
	{
		if (_titleDataReady)
		{
			return _useKid;
		}
		int state = 0;
		bool isEnabled = false;
		PlayFabTitleDataCache.Instance.GetTitleData("KIDData", delegate(string res)
		{
			state = 1;
			isEnabled = GetIsEnabled(res);
		}, delegate(PlayFabError err)
		{
			state = -1;
			Debug.LogError("[KID_MANAGER::UseKID] Something went wrong trying to get title data for key: [KIDData]. Error:\n" + err.ErrorMessage);
		});
		do
		{
			await Task.Yield();
		}
		while (state == 0);
		if (PlayFabAuthenticator.instance.postAuthSetSafety && isEnabled)
		{
			PlayFabAuthenticator.instance.DefaultSafetiesByAgeCategory();
		}
		return isEnabled;
	}

	public static async Task<int> CheckKIDPhase()
	{
		if (_titleDataReady)
		{
			return _kIDPhase;
		}
		int state = 0;
		int phase = 0;
		PlayFabTitleDataCache.Instance.GetTitleData("KIDData", delegate(string res)
		{
			state = 1;
			phase = GetPhase(res);
		}, delegate(PlayFabError err)
		{
			state = -1;
			Debug.LogError("[KID_MANAGER] Something went wrong trying to get title data for key: [KIDData]. Error:\n" + err.ErrorMessage);
		});
		do
		{
			await Task.Yield();
		}
		while (state == 0);
		return phase;
	}

	public static async Task<DateTime?> CheckKIDNewPlayerDateTime()
	{
		if (_titleDataReady)
		{
			return _kIDNewPlayerDateTime;
		}
		int state = 0;
		DateTime? newPlayerDateTime = null;
		PlayFabTitleDataCache.Instance.GetTitleData("KIDData", delegate(string res)
		{
			state = 1;
			newPlayerDateTime = GetNewPlayerDateTime(res);
		}, delegate(PlayFabError err)
		{
			state = -1;
			Debug.LogError("[KID_MANAGER] Something went wrong trying to get title data for key: [KIDData]. Error:\n" + err.ErrorMessage);
		});
		do
		{
			await Task.Yield();
		}
		while (state == 0);
		return newPlayerDateTime;
	}

	private static bool GetIsEnabled(string jsonTxt)
	{
		KIDTitleData kIDTitleData = JsonConvert.DeserializeObject<KIDTitleData>(jsonTxt);
		if (kIDTitleData == null)
		{
			Debug.LogError("[KID_MANAGER] Failed to parse json to [KIDTitleData]. Json: \n" + jsonTxt);
			return false;
		}
		if (!bool.TryParse(kIDTitleData.KIDEnabled, out var result))
		{
			Debug.LogError("[KID_MANAGER] Failed to parse 'KIDEnabled': [KIDEnabled] to bool.");
			return false;
		}
		return result;
	}

	private static int GetPhase(string jsonTxt)
	{
		KIDTitleData kIDTitleData = JsonConvert.DeserializeObject<KIDTitleData>(jsonTxt);
		if (kIDTitleData == null)
		{
			Debug.LogError("[KID_MANAGER] Failed to parse json to [KIDTitleData]. Json: \n" + jsonTxt);
			return 0;
		}
		return kIDTitleData.KIDPhase;
	}

	private static DateTime? GetNewPlayerDateTime(string jsonTxt)
	{
		KIDTitleData kIDTitleData = JsonConvert.DeserializeObject<KIDTitleData>(jsonTxt);
		if (kIDTitleData == null)
		{
			Debug.LogError("[KID_MANAGER] Failed to parse json to [KIDTitleData]. Json: \n" + jsonTxt);
			return null;
		}
		if (!DateTime.TryParse(kIDTitleData.KIDNewPlayerIsoTimestamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result))
		{
			Debug.LogError("[KID_MANAGER] Failed to parse 'KIDNewPlayerIsoTimestamp': [KIDNewPlayerIsoTimestamp] to DateTime.");
			return null;
		}
		return result;
	}

	public static bool IsAdult()
	{
		if (CurrentSession.IsValidSession)
		{
			return CurrentSession.AgeStatus == AgeStatusType.LEGALADULT;
		}
		return false;
	}

	public static bool HasAllPermissions()
	{
		List<Permission> allPermissions = CurrentSession.GetAllPermissions();
		for (int i = 0; i < allPermissions.Count; i++)
		{
			if (allPermissions[i].ManagedBy == Permission.ManagedByEnum.GUARDIAN || !allPermissions[i].Enabled)
			{
				return false;
			}
		}
		return true;
	}

	public static async Task<bool> SetKIDOptIn()
	{
		return await Server_OptIn();
	}

	public static async Task<(bool success, string message)> SetAndSendEmail(string email)
	{
		_emailAddress = email;
		return await TrySendChallengeEmailRequest();
	}

	public static async Task<bool> SendOptInPermissions()
	{
		return await TrySendOptInPermissions();
	}

	public static bool HasPermissionToUseFeature(EKIDFeatures feature)
	{
		if (!KidEnabledAndReady)
		{
			return !PlayFabAuthenticator.instance.GetSafety();
		}
		Permission permissionDataByFeature = GetPermissionDataByFeature(feature);
		if (permissionDataByFeature.Enabled || permissionDataByFeature.ManagedBy == Permission.ManagedByEnum.PLAYER)
		{
			return permissionDataByFeature.ManagedBy != Permission.ManagedByEnum.PROHIBITED;
		}
		return false;
	}

	private static async Task<bool> WaitForAuthentication()
	{
		while (!PlayFabClientAPI.IsClientLoggedIn())
		{
			if (_requestCancellationSource.IsCancellationRequested)
			{
				return false;
			}
			if ((bool)PlayFabAuthenticator.instance && PlayFabAuthenticator.instance.loginFailed)
			{
				return false;
			}
			await Task.Yield();
		}
		while (!GorillaServer.Instance.FeatureFlagsReady)
		{
			if (_requestCancellationSource.IsCancellationRequested)
			{
				return false;
			}
			await Task.Yield();
		}
		while (!_titleDataReady)
		{
			if (_requestCancellationSource.IsCancellationRequested)
			{
				return false;
			}
			await Task.Yield();
		}
		return true;
	}

	private static async Task<(AgeStatusType ageStatus, TMPSession resp)> AgeGateFlow(GetPlayerData_Data newPlayerData)
	{
		TMPSession session = newPlayerData.session;
		AgeStatusType? ageStatusType = newPlayerData.session?.AgeStatus;
		if (!ageStatusType.HasValue)
		{
			VerifyAgeData verifyAgeData = await ProcessAgeGate();
			if (verifyAgeData == null)
			{
				return (ageStatus: AgeStatusType.DIGITALMINOR, resp: null);
			}
			session = verifyAgeData.Session;
			ageStatusType = session.AgeStatus;
			_ = session.IsDefault;
		}
		if (!ageStatusType.HasValue)
		{
			Debug.LogError("[KID::MANAGER] PHASE THREE (A) -- FAILURE - Age Gate completed, but age status is null. Defaulting to MINOR");
			ageStatusType = AgeStatusType.DIGITALMINOR;
		}
		return (ageStatus: ageStatusType.Value, resp: session);
	}

	private static async Task<VerifyAgeData> ProcessAgeGate()
	{
		await KIDAgeGate.BeginAgeGate();
		if (_requestCancellationSource.IsCancellationRequested)
		{
			return null;
		}
		VerifyAgeData verifyResponse = await TryVerifyAgeResponse();
		if (_requestCancellationSource.IsCancellationRequested)
		{
			return null;
		}
		if (verifyResponse.Status == SessionStatus.PROHIBITED || verifyResponse.Status == SessionStatus.PENDING_AGE_APPEAL)
		{
			KIDUI_AgeAppealController.Instance.StartAgeAppealScreens(verifyResponse.Status);
			GetPlayerData_Data getPlayerData_Data = await TryGetPlayerData(forceRefresh: true);
			while (getPlayerData_Data.status == SessionStatus.PROHIBITED || getPlayerData_Data.status == SessionStatus.PENDING_AGE_APPEAL)
			{
				await Task.Delay(30000);
				getPlayerData_Data = await TryGetPlayerData(forceRefresh: true);
			}
			return verifyResponse;
		}
		return verifyResponse;
	}

	public static string GetOptInKey(EKIDFeatures feature)
	{
		return feature.ToStandardisedString() + "-opt-in-" + PlayFabAuthenticator.instance.GetPlayFabPlayerId();
	}

	private static async Task<GetPlayerData_Data> Server_GetPlayerData(bool forceRefresh, Action failureCallback)
	{
		string queryParams = string.Format("sessionRefresh={0}", forceRefresh ? "true" : "false");
		(long, GetPlayerDataResponse, string) obj = await KIDServerWebRequest<GetPlayerDataResponse, KIDRequestData>("GetPlayerData", "GET", null, queryParams, 3);
		long item = obj.Item1;
		GetPlayerDataResponse item2 = obj.Item2;
		GetSessionResponseType type = GetSessionResponseType.ERROR;
		switch (item)
		{
		case 200L:
			type = GetSessionResponseType.OK;
			break;
		case 404L:
			type = GetSessionResponseType.LOST;
			break;
		case 204L:
			type = GetSessionResponseType.NOT_FOUND;
			break;
		}
		GetPlayerData_Data result = new GetPlayerData_Data(type, item2);
		if (item < 200 || item >= 300)
		{
			failureCallback?.Invoke();
		}
		return result;
	}

	private static async Task<bool> Server_SetConfirmedStatus()
	{
		long num = await KIDServerWebRequestNoResponse<KIDRequestData>("SetConfirmedStatus", "POST", null);
		if (num == 200)
		{
			return true;
		}
		Debug.LogError($"[KID::SERVER_ROUTER] SetConfirmedStatus request failed. Code: {num}");
		return false;
	}

	private static async Task<UpgradeSessionData> Server_UpgradeSession(UpgradeSessionRequest request)
	{
		var (num, upgradeSessionResponse, _) = await KIDServerWebRequest<UpgradeSessionResponse, UpgradeSessionRequest>("UpgradeSession", "POST", request);
		if (num != 200)
		{
			Debug.LogError($"[KID::SERVER_ROUTER] Upgrade session request failed. Code: {num}");
		}
		if (upgradeSessionResponse == null)
		{
			Debug.LogError("[KID::SERVER_ROUTER] Upgrade session response is NULL. This is unexpected.");
			return null;
		}
		return new UpgradeSessionData(upgradeSessionResponse);
	}

	private static async Task<VerifyAgeData> Server_VerifyAge(VerifyAgeRequest request, Action failureCallback)
	{
		(long, VerifyAgeResponse, string) obj = await KIDServerWebRequest<VerifyAgeResponse, VerifyAgeRequest>("VerifyAge", "POST", request);
		long item = obj.Item1;
		VerifyAgeData result = new VerifyAgeData(obj.Item2);
		if (item < 200 || item >= 300)
		{
			failureCallback?.Invoke();
		}
		return result;
	}

	private static async Task<AttemptAgeUpdateData> Server_AttemptAgeUpdate(AttemptAgeUpdateRequest request, Action failureCallback)
	{
		var (num, attemptAgeUpdateResponse, _) = await KIDServerWebRequest<AttemptAgeUpdateResponse, AttemptAgeUpdateRequest>("AttemptAgeUpdate", "POST", request);
		if (num != 200)
		{
			Debug.LogError($"[KID::SERVER_ROUTER] Attempt age update request failed. Code: {num}");
		}
		return new AttemptAgeUpdateData(attemptAgeUpdateResponse.Status);
	}

	private static async Task<bool> Server_AppealAge(AppealAgeRequest request, Action failureCallback)
	{
		bool success = false;
		long num = await KIDServerWebRequestNoResponse("AppealAge", "POST", request);
		if (num == 200)
		{
			success = true;
		}
		else
		{
			Debug.LogError($"[KID::SERVER_ROUTER] Appeal age request failed. Code: {num}");
		}
		return success;
	}

	private static async Task<(bool, string)> Server_SendChallengeEmail(SendChallengeEmailRequest request)
	{
		bool success = false;
		var (num, _, text) = await KIDServerWebRequest<object, SendChallengeEmailRequest>("SendChallengeEmail", "POST", request);
		if (num >= 200 && num < 300)
		{
			success = true;
			return (success, string.Empty);
		}
		Debug.Log($"[KID::SERVER_ROUTER::Server_SendChallengeEmail] Send challenge email request failed. Code: [{num}] ErrorMessage: [{text}]");
		string item = "Oops, something went wrong.";
		ErrorContent errorContent = new ErrorContent
		{
			Error = "Unhandled",
			Message = "This error is unhandled"
		};
		try
		{
			errorContent = JsonConvert.DeserializeObject<ErrorContent>(text);
		}
		catch (Exception)
		{
			Debug.LogError("Could not deserialize error message");
		}
		switch (num)
		{
		case 400L:
			if (errorContent.Error.ToLower().Contains("BadRequest-InvalidEmail".ToLower()))
			{
				item = "This email doesn't seem right. Please check and try again.";
				Debug.LogError("[KID::SERVER_ROUTER::Server_SendChallengeEmail] Invalid email format: [" + request.Email + "]");
			}
			else if (errorContent.Error.ToLower().Contains("BadRequest-PlayerDataNotFound".ToLower()))
			{
				item = "Something went wrong. Please reboot the game and try again.";
				Debug.LogError("[KID::SERVER_ROUTER::Server_SendChallengeEmail] Player data not found for player ID: [" + PlayFabAuthenticator.instance.GetPlayFabPlayerId() + "]");
			}
			else if (errorContent.Error.ToLower().Contains("BadRequest-ChallengeNotFound".ToLower()))
			{
				item = "Couldn't find your challenge. If this keeps happening, contact Customer Support.";
				Debug.LogError("[KID::SERVER_ROUTER::Server_SendChallengeEmail] Challenge not found for player ID: [" + PlayFabAuthenticator.instance.GetPlayFabPlayerId() + "]");
			}
			break;
		case 403L:
			item = "This account has been banned. Please contact Customer Support.";
			Debug.LogError("[KID::SERVER_ROUTER::Server_SendChallengeEmail] Account is banned for player ID: [" + PlayFabAuthenticator.instance.GetPlayFabPlayerId() + "]");
			break;
		case 429L:
			item = "You've sent too many! Please wait a moment and try again.";
			Debug.LogError("[KID::SERVER_ROUTER::Server_SendChallengeEmail] Too many requests for player ID: [" + PlayFabAuthenticator.instance.GetPlayFabPlayerId() + "]");
			break;
		case 500L:
			if (errorContent.Error.ToLower().Contains("InternalServerError-FailedToRetrievePlayerData".ToLower()))
			{
				item = "We couldn't find your player data. Please reboot the game and try again.";
				Debug.LogError("[KID::SERVER_ROUTER::Server_SendChallengeEmail] Failed to retrieve player data for player ID: [" + PlayFabAuthenticator.instance.GetPlayFabPlayerId() + "]");
			}
			else if (errorContent.Error.ToLower().Contains("InternalServerError-UnhandledException".ToLower()))
			{
				item = "Something went wrong. Please reboot the game and try again.";
				Debug.LogError("[KID::SERVER_ROUTER::Server_SendChallengeEmail] Unhandled exception for player ID: [" + PlayFabAuthenticator.instance.GetPlayFabPlayerId() + "]");
			}
			else if (errorContent.Error.ToLower().Contains("InternalServerError-SendEmail".ToLower()))
			{
				item = "Something went wrong while sending the email. Please reboot the game and try again.";
				Debug.LogError("[KID::SERVER_ROUTER::Server_SendChallengeEmail] Failed to send email for player ID: [" + PlayFabAuthenticator.instance.GetPlayFabPlayerId() + "]");
			}
			break;
		}
		return (success, item);
	}

	private static async Task<bool> Server_SetOptInPermissions(SetOptInPermissionsRequest request, Action failureCallback)
	{
		bool success = false;
		Debug.Log("[KID::SERVER_ROUTER::OptInRefactor] Setting opt-in permissions with request: " + JsonConvert.SerializeObject(request));
		long num = await KIDServerWebRequestNoResponse("SetOptInPermissions", "POST", request);
		if (num >= 200 && num < 300)
		{
			success = true;
		}
		else
		{
			Debug.LogError($"[KID::SERVER_ROUTER] SetOptInPermissions request failed. Code: {num}");
			failureCallback?.Invoke();
		}
		Debug.Log($"[KID::SERVER_ROUTER::OptInRefactor] SetOptInPermissions request completed with success: {success} - code: {num}");
		return success;
	}

	private static async Task<bool> Server_OptIn()
	{
		long num = await KIDServerWebRequestNoResponse<KIDRequestData>("OptIn", "POST", null);
		if (num == 200)
		{
			return true;
		}
		Debug.LogError($"[KID::SERVER_ROUTER] Opt in request failed. Code: {num}");
		return false;
	}

	private static async Task<GetRequirementsData> Server_GetRequirements()
	{
		(long, GetRequirementsResponse, string) obj = await KIDServerWebRequest<GetRequirementsResponse, KIDRequestData>("GetRequirements", "GET", null, null, 3);
		long item = obj.Item1;
		GetRequirementsResponse item2 = obj.Item2;
		GetRequirementsData result = new GetRequirementsData
		{
			AgeGateRequirements = item2
		};
		if (item == 200)
		{
			return result;
		}
		Debug.LogError($"[KID::SERVER_ROUTER] Get Age-gate Requirements FAILED. Code: {item}");
		return result;
	}

	private static async Task<(long code, T responseModel, string errorMessage)> KIDServerWebRequest<T, Q>(string endpoint, string operationType, Q requestData, string queryParams = null, int maxRetries = 2, Func<long, bool> responseCodeIsRetryable = null) where T : class where Q : KIDRequestData
	{
		int retryCount = 0;
		string URL = "/api/" + endpoint;
		if (!string.IsNullOrEmpty(queryParams))
		{
			URL = URL + "?" + queryParams;
		}
		Debug.Log("[KID::MANAGER::SERVER_ROUTER] URL: " + URL);
		while (true)
		{
			using UnityWebRequest request = new UnityWebRequest(PlayFabAuthenticatorSettings.KidApiBaseUrl + URL, operationType);
			byte[] data = Array.Empty<byte>();
			string json = "";
			if (requestData != null)
			{
				json = JsonConvert.SerializeObject(requestData);
				data = Encoding.UTF8.GetBytes(json);
			}
			request.uploadHandler = new UploadHandlerRaw(data);
			request.downloadHandler = new DownloadHandlerBuffer();
			request.SetRequestHeader("Content-Type", "application/json");
			request.SetRequestHeader("X-Authorization", PlayFabSettings.staticPlayer.ClientSessionTicket);
			request.SetRequestHeader("X-PlayerId", PlayFabSettings.staticPlayer.PlayFabId);
			request.SetRequestHeader("X-Mothership-Token", MothershipClientContext.Token);
			request.SetRequestHeader("X-Mothership-Player-Id", MothershipClientContext.MothershipId);
			request.SetRequestHeader("X-Mothership-Env-Id", MothershipClientApiUnity.EnvironmentId);
			request.SetRequestHeader("X-Mothership-Deployment-Id", MothershipClientApiUnity.DeploymentId);
			if (!PlayFabAuthenticatorSettings.KidApiBaseUrl.Contains("gtag-cf.com"))
			{
				request.SetRequestHeader("CF-IPCountry", RegionInfo.CurrentRegion.TwoLetterISORegionName);
			}
			request.timeout = 15;
			UnityWebRequest unityWebRequest = await request.SendWebRequest();
			if (unityWebRequest.result == UnityWebRequest.Result.Success)
			{
				if (typeof(T) == typeof(object))
				{
					return (code: unityWebRequest.responseCode, responseModel: null, errorMessage: unityWebRequest.error);
				}
				try
				{
					T item = JsonConvert.DeserializeObject<T>(unityWebRequest.downloadHandler.text);
					return (code: unityWebRequest.responseCode, responseModel: item, errorMessage: unityWebRequest.error);
				}
				catch (Exception)
				{
					Debug.LogError("[KID::SERVER_ROUTER] Failed to convert to class type [T] via JSON:\n[" + unityWebRequest.downloadHandler.text + "]");
					return (code: unityWebRequest.responseCode, responseModel: null, errorMessage: unityWebRequest.error);
				}
			}
			bool flag = request.result != UnityWebRequest.Result.ProtocolError;
			bool flag2;
			bool flag3;
			if (!flag)
			{
				if (responseCodeIsRetryable != null)
				{
					flag2 = responseCodeIsRetryable(request.responseCode);
					goto IL_0365;
				}
				long responseCode = request.responseCode;
				if (responseCode >= 500)
				{
					if (responseCode < 600)
					{
						goto IL_0359;
					}
				}
				else if (responseCode == 408 || responseCode == 429)
				{
					goto IL_0359;
				}
				flag3 = false;
				goto IL_0361;
			}
			goto IL_0369;
			IL_0369:
			if (!flag)
			{
				goto IL_04f1;
			}
			if (retryCount < maxRetries)
			{
				int num = retryCount + 1;
				retryCount = num;
				float num2 = UnityEngine.Random.Range(0.5f, Mathf.Pow(2f, num));
				Debug.LogWarning("[KID::SERVER_ROUTER] Tried sending request [" + operationType + " - " + endpoint + "] but it failed:\n" + unityWebRequest.error + "\n\nRequest:\n" + json);
				Debug.LogWarning($"[KID::SERVER_ROUTER] Retrying {endpoint}... Retry attempt #{retryCount}, waiting for {num2} seconds");
				await Task.Delay(TimeSpan.FromSeconds(num2));
				continue;
			}
			Debug.LogError("[KID::SERVER_ROUTER] Tried sending request [" + operationType + " - " + endpoint + "] but it failed:\n" + unityWebRequest.error + "\n\nRequest:\n" + json);
			Debug.LogError("[KID::SERVER_ROUTER] Maximum retries attempted. Please check your network connection.");
			goto IL_04f1;
			IL_0365:
			flag = flag2;
			goto IL_0369;
			IL_0361:
			flag2 = flag3;
			goto IL_0365;
			IL_04f1:
			if (request.result == UnityWebRequest.Result.ProtocolError)
			{
				Debug.LogError($"[KID::SERVER_ROUTER] HTTP {request.responseCode} ERROR: {request.error}\nMessage: {request.downloadHandler.text}");
			}
			else if (request.result == UnityWebRequest.Result.ConnectionError)
			{
				Debug.LogError("[KID::SERVER_ROUTER] NETWORK ERROR: " + request.error + "\nMessage: " + request.downloadHandler.text);
				if (KIDUI_Controller.Instance != null)
				{
					KIDMessagingController.ShowConnectionErrorScreen();
				}
			}
			else
			{
				Debug.LogError("[KID::SERVER_ROUTER] ERROR: " + request.error + "\nMessage: " + request.downloadHandler.text);
			}
			return (code: unityWebRequest.responseCode, responseModel: null, errorMessage: unityWebRequest.downloadHandler.text);
			IL_0359:
			flag3 = true;
			goto IL_0361;
		}
	}

	private static async Task<long> KIDServerWebRequestNoResponse<Q>(string endpoint, string operationType, Q requestData, int maxRetries = 2, Func<long, bool> responseCodeIsRetryable = null) where Q : KIDRequestData
	{
		return (await KIDServerWebRequest<object, Q>(endpoint, operationType, requestData, null, maxRetries, responseCodeIsRetryable)).Item1;
	}

	public static void RegisterSessionUpdateCallback_AnyPermission(Action callback)
	{
		_onSessionUpdated_AnyPermission = (Action)Delegate.Combine(_onSessionUpdated_AnyPermission, callback);
	}

	public static void UnregisterSessionUpdateCallback_AnyPermission(Action callback)
	{
		_onSessionUpdated_AnyPermission = (Action)Delegate.Remove(_onSessionUpdated_AnyPermission, callback);
	}

	public static void RegisterSessionUpdatedCallback_VoiceChat(Action<bool, Permission.ManagedByEnum> callback)
	{
		_onSessionUpdated_VoiceChat = (Action<bool, Permission.ManagedByEnum>)Delegate.Combine(_onSessionUpdated_VoiceChat, callback);
	}

	public static void UnregisterSessionUpdatedCallback_VoiceChat(Action<bool, Permission.ManagedByEnum> callback)
	{
		_onSessionUpdated_VoiceChat = (Action<bool, Permission.ManagedByEnum>)Delegate.Remove(_onSessionUpdated_VoiceChat, callback);
	}

	public static void RegisterSessionUpdatedCallback_CustomUsernames(Action<bool, Permission.ManagedByEnum> callback)
	{
		_onSessionUpdated_CustomUsernames = (Action<bool, Permission.ManagedByEnum>)Delegate.Combine(_onSessionUpdated_CustomUsernames, callback);
	}

	public static void UnregisterSessionUpdatedCallback_CustomUsernames(Action<bool, Permission.ManagedByEnum> callback)
	{
		_onSessionUpdated_CustomUsernames = (Action<bool, Permission.ManagedByEnum>)Delegate.Remove(_onSessionUpdated_CustomUsernames, callback);
	}

	public static void RegisterSessionUpdatedCallback_PrivateRooms(Action<bool, Permission.ManagedByEnum> callback)
	{
		_onSessionUpdated_PrivateRooms = (Action<bool, Permission.ManagedByEnum>)Delegate.Combine(_onSessionUpdated_PrivateRooms, callback);
	}

	public static void UnregisterSessionUpdatedCallback_PrivateRooms(Action<bool, Permission.ManagedByEnum> callback)
	{
		_onSessionUpdated_PrivateRooms = (Action<bool, Permission.ManagedByEnum>)Delegate.Remove(_onSessionUpdated_PrivateRooms, callback);
	}

	public static void RegisterSessionUpdatedCallback_Multiplayer(Action<bool, Permission.ManagedByEnum> callback)
	{
		_onSessionUpdated_Multiplayer = (Action<bool, Permission.ManagedByEnum>)Delegate.Combine(_onSessionUpdated_Multiplayer, callback);
	}

	public static void UnregisterSessionUpdatedCallback_Multiplayer(Action<bool, Permission.ManagedByEnum> callback)
	{
		_onSessionUpdated_Multiplayer = (Action<bool, Permission.ManagedByEnum>)Delegate.Remove(_onSessionUpdated_Multiplayer, callback);
	}

	public static void RegisterSessionUpdatedCallback_UGC(Action<bool, Permission.ManagedByEnum> callback)
	{
		_onSessionUpdated_UGC = (Action<bool, Permission.ManagedByEnum>)Delegate.Combine(_onSessionUpdated_UGC, callback);
	}

	public static async Task<bool> WaitForAndUpdateNewSession(bool forceRefresh)
	{
		if (_isUpdatingNewSession)
		{
			return false;
		}
		_isUpdatingNewSession = true;
		float updateTimeout = Time.realtimeSinceStartup + 600f;
		GetPlayerData_Data getPlayerData_Data = await TryGetPlayerData(forceRefresh);
		TMPSession tMPSession = getPlayerData_Data?.session;
		bool flag = HasSessionChanged(tMPSession);
		while (Time.realtimeSinceStartup < updateTimeout && (tMPSession == null || tMPSession.Age == 0 || !flag))
		{
			await Task.Delay(30000);
			if (_requestCancellationSource.IsCancellationRequested)
			{
				_isUpdatingNewSession = false;
				return false;
			}
			getPlayerData_Data = await TryGetPlayerData(forceRefresh);
			tMPSession = getPlayerData_Data?.session;
			flag = HasSessionChanged(tMPSession);
			if (flag)
			{
				break;
			}
			if (getPlayerData_Data == null)
			{
				Debug.LogError("[KID::MANAGER] UpdateNewSession -- LOOP - Tried getting Player Data but returned NULL");
			}
			else if (getPlayerData_Data.responseType == GetSessionResponseType.ERROR)
			{
				Debug.LogError("[KID::MANAGER] UpdateNewSession -- LOOP - Tried getting a new Session but playerData returned with ERROR");
			}
			else if (tMPSession == null)
			{
				Debug.LogError("[KID::MANAGER] UpdateNewSession -- LOOP - Found Player Data, but SESSION was NULL");
			}
		}
		_isUpdatingNewSession = false;
		if (getPlayerData_Data == null || getPlayerData_Data.responseType != GetSessionResponseType.OK || tMPSession == null)
		{
			return false;
		}
		return UpdatePermissions(tMPSession);
	}

	private static bool HasSessionChanged(TMPSession newSession)
	{
		if (newSession == null)
		{
			return false;
		}
		if (CurrentSession == null)
		{
			return true;
		}
		if (!newSession.IsValidSession)
		{
			return false;
		}
		if (newSession.IsDefault)
		{
			Debug.LogError($"[KID::MANAGER] DEBUG - New Session Is Default! Age: [{newSession.Age}]");
			return false;
		}
		if (CurrentSession.IsDefault)
		{
			return true;
		}
		if (newSession.Etag.Equals(CurrentSession.Etag))
		{
			return false;
		}
		return true;
	}

	private static void OnSessionUpdated()
	{
		_onSessionUpdated_AnyPermission?.Invoke();
		bool voiceChatEnabled = false;
		bool joinGroupsEnabled = false;
		bool customUsernamesEnabled = false;
		List<Permission> allPermissionsData = GetAllPermissionsData();
		int count = allPermissionsData.Count;
		for (int i = 0; i < count; i++)
		{
			Permission permission = allPermissionsData[i];
			switch (permission.Name)
			{
			case "voice-chat":
				if (HasPermissionChanged(permission))
				{
					_onSessionUpdated_VoiceChat?.Invoke(permission.Enabled, permission.ManagedBy);
					_previousPermissionSettings[permission.Name] = permission;
				}
				voiceChatEnabled = permission.Enabled;
				break;
			case "custom-username":
				if (HasPermissionChanged(permission))
				{
					_onSessionUpdated_CustomUsernames?.Invoke(permission.Enabled, permission.ManagedBy);
					_previousPermissionSettings[permission.Name] = permission;
				}
				customUsernamesEnabled = permission.Enabled;
				break;
			case "join-groups":
				if (HasPermissionChanged(permission))
				{
					_onSessionUpdated_PrivateRooms?.Invoke(permission.Enabled, permission.ManagedBy);
					_previousPermissionSettings[permission.Name] = permission;
				}
				joinGroupsEnabled = permission.Enabled;
				break;
			case "multiplayer":
				if (HasPermissionChanged(permission))
				{
					_onSessionUpdated_Multiplayer?.Invoke(permission.Enabled, permission.ManagedBy);
					_previousPermissionSettings[permission.Name] = permission;
				}
				_ = permission.Enabled;
				break;
			case "mods":
				if (HasPermissionChanged(permission))
				{
					_onSessionUpdated_UGC?.Invoke(permission.Enabled, permission.ManagedBy);
					_previousPermissionSettings[permission.Name] = permission;
				}
				break;
			default:
				Debug.Log("[KID] Tried updating permission with name [" + permission.Name + "] but did not match any of the set cases. Unable to process");
				break;
			}
		}
		GorillaTelemetry.PostKidEvent(joinGroupsEnabled, voiceChatEnabled, customUsernamesEnabled, CurrentSession.AgeStatus, GTKidEventType.permission_update);
	}

	private static bool HasPermissionChanged(Permission newValue)
	{
		if (_previousPermissionSettings.TryGetValue(newValue.Name, out var value))
		{
			if (value.Enabled == newValue.Enabled)
			{
				return value.ManagedBy != newValue.ManagedBy;
			}
			return true;
		}
		_previousPermissionSettings.Add(newValue.Name, newValue);
		return true;
	}
}

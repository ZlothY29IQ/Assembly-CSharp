using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GorillaNetworking;
using KID.Model;
using UnityEngine;
using UnityEngine.Localization;

public class KIDUI_MainScreen : MonoBehaviour
{
	[Serializable]
	public struct FeatureToggleSetup
	{
		public EKIDFeatures linkedFeature;

		public string permissionName;

		public LocalizedString featureName;

		public bool requiresToggle;

		public bool alwaysCheckFeatureSetting;

		public LocalizedString enabledText;

		public LocalizedString disabledText;
	}

	public const string OPT_IN_SUFFIX = "-opt-in";

	public static bool ShownSettingsScreen = false;

	[SerializeField]
	private GameObject _kidScreensGroup;

	[SerializeField]
	private KIDUI_SetupScreen _setupKidScreen;

	[SerializeField]
	private KIDUI_SendUpgradeEmailScreen _sendUpgradeEmailScreen;

	[SerializeField]
	private KIDUI_AnimatedEllipsis _animatedEllipsis;

	[Header("Permission Request Buttons")]
	[SerializeField]
	private KIDUIButton _getPermissionsButton;

	[SerializeField]
	private KIDUIButton _gettingPermissionsButton;

	[SerializeField]
	private KIDUIButton _requestPermissionsButton;

	[SerializeField]
	private GameObject _defaultButtonsContainer;

	[SerializeField]
	private GameObject _permissionsRequestingButtonContainer;

	[SerializeField]
	private GameObject _permissionsRequestedButtonContainer;

	private bool _hasAllPermissions;

	[Header("Dynamic Feature Settings Setup")]
	[SerializeField]
	private GameObject _featurePrefab;

	[SerializeField]
	private Transform _featureRootTransform;

	[SerializeField]
	private EKIDFeatures[] _displayOrder = new EKIDFeatures[4];

	[SerializeField]
	private List<FeatureToggleSetup> _featureSetups = new List<FeatureToggleSetup>();

	[Header("Additional Feature-Specific Setup")]
	[SerializeField]
	private GameObject _voiceChatLabel;

	[Header("Hide Permissions Tip")]
	[SerializeField]
	private GameObject _permissionsTip;

	[Header("Titles")]
	[SerializeField]
	private GameObject _titleFeaturePermissions;

	[SerializeField]
	private GameObject _titleGameFeatures;

	[Header("Game Status Setup")]
	[SerializeField]
	private GameObject _missingStatus;

	[SerializeField]
	private GameObject _updatedStatus;

	[SerializeField]
	private GameObject _declinedStatus;

	[SerializeField]
	private GameObject _pendingStatus;

	[SerializeField]
	private GameObject _timeoutStatus;

	[SerializeField]
	private GameObject _setupRequiredStatus;

	[SerializeField]
	private GameObject _fullPlayerControlStatus;

	private string _emailAddress;

	private bool _multiplayerEnabled;

	private bool _customNameEnabled;

	private bool _voiceChatEnabled;

	private bool _initialised;

	private KIDUI_Controller.Metrics_ShowReason _mainScreenOpenedReason;

	private EMainScreenStatus _screenStatus;

	private GameObject _eventSystemObj;

	private static Dictionary<EKIDFeatures, List<KIDUIFeatureSetting>> _featuresList = new Dictionary<EKIDFeatures, List<KIDUIFeatureSetting>>();

	private void Awake()
	{
		_featuresList.Clear();
		if (_setupKidScreen == null)
		{
			Debug.LogErrorFormat("[KID::UI::Setup] Setup K-ID Screen is NULL");
		}
		else if (!_initialised)
		{
			InitialiseMainScreen();
		}
	}

	private void OnEnable()
	{
		KIDManager.RegisterSessionUpdateCallback_AnyPermission(UpdatePermissionsAndFeaturesScreen);
		LocalisationManager.RegisterOnLanguageChanged(OnLanguageChanged);
		UpdatePermissionsAndFeaturesScreen();
	}

	private void OnDisable()
	{
		KIDManager.UnregisterSessionUpdateCallback_AnyPermission(UpdatePermissionsAndFeaturesScreen);
		KIDAudioManager.Instance?.PlaySoundWithDelay(KIDAudioManager.KIDSoundType.PageTransition);
		LocalisationManager.UnregisterOnLanguageChanged(OnLanguageChanged);
	}

	private void OnDestroy()
	{
	}

	private void ConstructFeatureSettings()
	{
		for (int i = 0; i < _displayOrder.Length; i++)
		{
			for (int j = 0; j < _featureSetups.Count; j++)
			{
				if (_featureSetups[j].linkedFeature == _displayOrder[i])
				{
					CreateNewFeatureDisplay(_featureSetups[j]);
					break;
				}
			}
		}
		UpdatePermissionsAndFeaturesScreen();
	}

	private void CreateNewFeatureDisplay(FeatureToggleSetup setup)
	{
		Permission permissionDataByFeature = KIDManager.GetPermissionDataByFeature(setup.linkedFeature);
		if (permissionDataByFeature == null)
		{
			Debug.LogErrorFormat("[KID::UI::MAIN] Failed to retrieve permission data for feature; [" + setup.linkedFeature.ToString() + "]");
		}
		else
		{
			if (permissionDataByFeature.ManagedBy == Permission.ManagedByEnum.PROHIBITED || (permissionDataByFeature.ManagedBy == Permission.ManagedByEnum.PLAYER && (permissionDataByFeature.Enabled || KIDManager.CheckFeatureOptIn(setup.linkedFeature).hasOptedInPreviously)) || (setup.alwaysCheckFeatureSetting && KIDManager.CheckFeatureSettingEnabled(setup.linkedFeature)))
			{
				return;
			}
			GameObject gameObject = UnityEngine.Object.Instantiate(_featurePrefab, _featureRootTransform);
			KIDUIFeatureSetting component = gameObject.GetComponent<KIDUIFeatureSetting>();
			if (permissionDataByFeature.ManagedBy == Permission.ManagedByEnum.GUARDIAN)
			{
				Debug.LogFormat($"[KID::UI::MAIN_SCREEN] Adding new Locked Feature:  {setup.linkedFeature.ToString()} Is enabled: {permissionDataByFeature.Enabled}");
				component.CreateNewFeatureSettingGuardianManaged(setup, permissionDataByFeature.Enabled);
				if (!_featuresList.ContainsKey(setup.linkedFeature))
				{
					_featuresList.Add(setup.linkedFeature, new List<KIDUIFeatureSetting>());
				}
				_featuresList[setup.linkedFeature].Add(component);
				return;
			}
			if (setup.requiresToggle)
			{
				component.CreateNewFeatureSettingWithToggle(setup, initialState: false, setup.alwaysCheckFeatureSetting);
			}
			else
			{
				component.CreateNewFeatureSettingWithoutToggle(setup, setup.alwaysCheckFeatureSetting);
			}
			if (!_featuresList.ContainsKey(setup.linkedFeature))
			{
				_featuresList.Add(setup.linkedFeature, new List<KIDUIFeatureSetting>());
			}
			_featuresList[setup.linkedFeature].Add(component);
			ConstructAdditionalSetup(setup.linkedFeature, gameObject);
		}
	}

	private void ConstructAdditionalSetup(EKIDFeatures feature, GameObject featureObject)
	{
		_ = 2;
	}

	private void UpdatePermissionsAndFeaturesScreen()
	{
		int num = 0;
		Debug.LogFormat($"[KID::UI::MAIN] Updated Feature listings. To Update: [{_featuresList.Count}]");
		foreach (KeyValuePair<EKIDFeatures, List<KIDUIFeatureSetting>> features in _featuresList)
		{
			for (int i = 0; i < features.Value.Count; i++)
			{
				Permission permissionDataByFeature = KIDManager.GetPermissionDataByFeature(features.Key);
				if (permissionDataByFeature == null)
				{
					Debug.LogErrorFormat("[KID::UI::MAIN] Failed to find permission data for feature: [" + features.Key.ToString() + "]");
					continue;
				}
				if (permissionDataByFeature.ManagedBy == Permission.ManagedByEnum.GUARDIAN)
				{
					features.Value[i].SetGuardianManagedState(permissionDataByFeature.Enabled);
					continue;
				}
				bool isOptedIn = KIDManager.CheckFeatureOptIn(features.Key, permissionDataByFeature).hasOptedInPreviously;
				if (features.Value[i].AlwaysCheckFeatureSetting)
				{
					isOptedIn = KIDManager.CheckFeatureSettingEnabled(features.Key);
				}
				if (!features.Value[i].GetHasToggle())
				{
					features.Value[i].SetPlayerManagedState(permissionDataByFeature.Enabled, isOptedIn);
				}
			}
		}
		int num2 = 0;
		foreach (KeyValuePair<EKIDFeatures, List<KIDUIFeatureSetting>> features2 in _featuresList)
		{
			for (int j = 0; j < features2.Value.Count; j++)
			{
				num2++;
				Permission permissionDataByFeature2 = KIDManager.GetPermissionDataByFeature(features2.Key);
				if (features2.Value[j].GetFeatureToggleState() || permissionDataByFeature2.ManagedBy == Permission.ManagedByEnum.PLAYER)
				{
					num++;
				}
			}
		}
		if (num >= num2)
		{
			if (!_initialised)
			{
				_titleFeaturePermissions.SetActive(value: false);
				_titleGameFeatures.SetActive(value: true);
			}
			_hasAllPermissions = true;
			_getPermissionsButton.gameObject.SetActive(value: false);
			_gettingPermissionsButton.gameObject.SetActive(value: false);
			_requestPermissionsButton.gameObject.SetActive(value: false);
			_permissionsTip.SetActive(value: false);
			SetButtonContainersVisibility(EGetPermissionsStatus.RequestedPermission);
		}
	}

	private bool IsFeatureToggledOn(EKIDFeatures permissionFeature)
	{
		if (!_featuresList.TryGetValue(permissionFeature, out var value))
		{
			return true;
		}
		KIDUIFeatureSetting kIDUIFeatureSetting = value.FirstOrDefault();
		if (kIDUIFeatureSetting == null)
		{
			Debug.LogErrorFormat($"[KID::UI::MAIN] Empty list for permission Name [{permissionFeature}]");
			return false;
		}
		return kIDUIFeatureSetting.GetFeatureToggleState();
	}

	public void InitialiseMainScreen()
	{
		if (_initialised)
		{
			Debug.Log("[KID::MAIN_SCREEN] Already Initialised");
			return;
		}
		ConstructFeatureSettings();
		_declinedStatus.SetActive(value: false);
		_timeoutStatus.SetActive(value: false);
		_pendingStatus.SetActive(value: false);
		_updatedStatus.SetActive(value: false);
		_setupRequiredStatus.SetActive(value: false);
		_missingStatus.SetActive(value: false);
		_fullPlayerControlStatus.SetActive(value: false);
		_initialised = true;
	}

	public void ShowMainScreen(EMainScreenStatus showStatus, KIDUI_Controller.Metrics_ShowReason reason)
	{
		ShowMainScreen(showStatus);
		_mainScreenOpenedReason = reason;
		string value = reason.ToString().Replace("_", "-").ToLower();
		TelemetryData telemetryData = new TelemetryData
		{
			EventName = "kid_game_settings",
			CustomTags = new string[4]
			{
				"kid_setup",
				KIDTelemetry.GameVersionCustomTag,
				KIDTelemetry.GameEnvironment,
				KIDTelemetry.Open_MetricActionCustomTag
			},
			BodyData = new Dictionary<string, string> { { "screen_shown_reason", value } }
		};
		foreach (Permission allPermissionsDatum in KIDManager.GetAllPermissionsData())
		{
			telemetryData.BodyData.Add(KIDTelemetry.GetPermissionManagedByBodyData(allPermissionsDatum.Name), allPermissionsDatum.ManagedBy.ToString().ToLower());
			telemetryData.BodyData.Add(KIDTelemetry.GetPermissionEnabledBodyData(allPermissionsDatum.Name), allPermissionsDatum.Enabled.ToString().ToLower());
		}
		GorillaTelemetry.EnqueueTelemetryEvent(telemetryData.EventName, telemetryData.BodyData, telemetryData.CustomTags);
	}

	public void ShowMainScreen(EMainScreenStatus showStatus)
	{
		ShownSettingsScreen = true;
		base.gameObject.SetActive(value: true);
		ConfigurePermissionsButtons();
		UpdateScreenStatus(showStatus);
	}

	public void UpdateScreenStatus(EMainScreenStatus showStatus, bool sendMetrics = false)
	{
		if (sendMetrics && showStatus == EMainScreenStatus.Updated)
		{
			string value = _mainScreenOpenedReason.ToString().Replace("_", "-").ToLower();
			TelemetryData telemetryData = new TelemetryData
			{
				EventName = "kid_game_settings",
				CustomTags = new string[4]
				{
					"kid_setup",
					KIDTelemetry.GameVersionCustomTag,
					KIDTelemetry.GameEnvironment,
					KIDTelemetry.Updated_MetricActionCustomTag
				},
				BodyData = new Dictionary<string, string> { { "screen_shown_reason", value } }
			};
			foreach (Permission allPermissionsDatum in KIDManager.GetAllPermissionsData())
			{
				telemetryData.BodyData.Add(KIDTelemetry.GetPermissionManagedByBodyData(allPermissionsDatum.Name), allPermissionsDatum.ManagedBy.ToString().ToLower());
				telemetryData.BodyData.Add(KIDTelemetry.GetPermissionEnabledBodyData(allPermissionsDatum.Name), allPermissionsDatum.Enabled.ToString().ToLower());
			}
			GorillaTelemetry.EnqueueTelemetryEvent(telemetryData.EventName, telemetryData.BodyData, telemetryData.CustomTags);
		}
		GameObject activeStatusObject = GetActiveStatusObject();
		_declinedStatus.SetActive(value: false);
		_timeoutStatus.SetActive(value: false);
		_pendingStatus.SetActive(value: false);
		_updatedStatus.SetActive(value: false);
		_setupRequiredStatus.SetActive(value: false);
		_missingStatus.SetActive(value: false);
		_fullPlayerControlStatus.SetActive(value: false);
		switch (showStatus)
		{
		default:
			if (!_hasAllPermissions)
			{
				_missingStatus.SetActive(value: true);
			}
			else if (_hasAllPermissions)
			{
				_fullPlayerControlStatus.SetActive(value: true);
			}
			else
			{
				_screenStatus = showStatus;
			}
			break;
		case EMainScreenStatus.Declined:
			_declinedStatus.SetActive(value: true);
			_screenStatus = showStatus;
			break;
		case EMainScreenStatus.Pending:
			_pendingStatus.SetActive(value: true);
			_screenStatus = showStatus;
			break;
		case EMainScreenStatus.Timedout:
			_timeoutStatus.SetActive(value: true);
			_screenStatus = showStatus;
			break;
		case EMainScreenStatus.Setup:
			_setupRequiredStatus.SetActive(value: true);
			_screenStatus = showStatus;
			break;
		case EMainScreenStatus.Previous:
			if (activeStatusObject != null)
			{
				activeStatusObject.SetActive(value: true);
			}
			else
			{
				_updatedStatus.SetActive(value: true);
			}
			break;
		case EMainScreenStatus.FullControl:
			_fullPlayerControlStatus.SetActive(value: true);
			break;
		}
		SetButtonContainersVisibility(GetPermissionState());
	}

	public void HideMainScreen()
	{
		base.gameObject.SetActive(value: false);
	}

	public async void OnAskForPermission()
	{
		_requestPermissionsButton.interactable = false;
		_getPermissionsButton.interactable = false;
		_gettingPermissionsButton.interactable = false;
		await _animatedEllipsis.StartAnimation();
		bool missingPermissionsPostUpdate = await UpdateAndCheckForMissingPermissions();
		_requestPermissionsButton.interactable = true;
		_getPermissionsButton.interactable = true;
		_gettingPermissionsButton.interactable = true;
		await _animatedEllipsis.StopAnimation();
		if (!missingPermissionsPostUpdate)
		{
			return;
		}
		base.gameObject.SetActive(value: false);
		if (KIDManager.CurrentSession.IsDefault)
		{
			_setupKidScreen.OnStartSetup();
			return;
		}
		List<string> requestedPermissions = new List<string>(CollectPermissionsToUpgrade());
		await _sendUpgradeEmailScreen.SendUpgradeEmail(requestedPermissions);
		if (KIDManager.CurrentSession.ManagedBy == Session.ManagedByEnum.PLAYER)
		{
			_setupKidScreen.OnStartSetup();
		}
		KIDManager.WaitForAndUpdateNewSession(forceRefresh: true);
	}

	public void OnSaveAndExit()
	{
		if (KIDManager.CurrentSession == null)
		{
			Debug.LogError("[KID::KID_UI_MAINSCREEN] There is no session as such cannot opt into anything");
			KIDUI_Controller.Instance.CloseKIDScreens();
			return;
		}
		List<Permission> allPermissionsData = KIDManager.GetAllPermissionsData();
		for (int i = 0; i < allPermissionsData.Count; i++)
		{
			switch (allPermissionsData[i].Name)
			{
			case "multiplayer":
				UpdateOptInSetting(allPermissionsData[i], EKIDFeatures.Multiplayer, null);
				break;
			case "mods":
				UpdateOptInSetting(allPermissionsData[i], EKIDFeatures.Mods, null);
				break;
			case "voice-chat":
				UpdateOptInSetting(allPermissionsData[i], EKIDFeatures.Voice_Chat, delegate(bool b, Permission p, bool hasOptedInPreviously)
				{
					GorillaComputer.instance.KID_SetVoiceChatSettingOnStart(b, p.ManagedBy, hasOptedInPreviously);
				});
				break;
			case "custom-username":
				UpdateOptInSetting(allPermissionsData[i], EKIDFeatures.Custom_Nametags, delegate(bool b, Permission p, bool hasOptedInPreviously)
				{
					GorillaComputer.instance.SetNametagSetting(b, p.ManagedBy, hasOptedInPreviously);
				});
				break;
			default:
				Debug.LogError("[KID::UI::MainScreen] Unhandled permission when saving and exiting: [" + allPermissionsData[i].Name + "]");
				break;
			case "join-groups":
				break;
			}
		}
		KIDManager.SendOptInPermissions();
		if (_screenStatus != EMainScreenStatus.None)
		{
			string value = _mainScreenOpenedReason.ToString().Replace("_", "-").ToLower();
			TelemetryData telemetryData = new TelemetryData
			{
				EventName = "kid_game_settings",
				CustomTags = new string[3]
				{
					"kid_setup",
					KIDTelemetry.GameVersionCustomTag,
					KIDTelemetry.GameEnvironment
				},
				BodyData = new Dictionary<string, string>
				{
					{ "screen_shown_reason", value },
					{
						"kid_status",
						_screenStatus.ToString().ToLower()
					},
					{ "button_pressed", "save_and_continue" }
				}
			};
			GorillaTelemetry.EnqueueTelemetryEvent(telemetryData.EventName, telemetryData.BodyData, telemetryData.CustomTags);
		}
		else
		{
			Debug.LogError("[KID::UI::MAIN_SCREEN] Trying to close k-ID Main Screen, but screen status is set to [None] - Invalid status, will not submit analytics");
		}
		KIDUI_Controller.Instance.CloseKIDScreens();
	}

	public int GetFeatureListingCount()
	{
		int num = 0;
		foreach (List<KIDUIFeatureSetting> value in _featuresList.Values)
		{
			num += value.Count;
		}
		return num;
	}

	private async Task<bool> UpdateAndCheckForMissingPermissions()
	{
		bool hasUpdated = false;
		bool wasSuccess = false;
		float cutOffDuration = Time.realtimeSinceStartup + 15f;
		KIDManager.UpdateSession(delegate(bool success)
		{
			hasUpdated = true;
			wasSuccess = success;
		});
		do
		{
			await Task.Yield();
		}
		while (Time.realtimeSinceStartup < cutOffDuration && !hasUpdated);
		UpdatePermissionsAndFeaturesScreen();
		if (wasSuccess)
		{
			bool flag = false;
			foreach (Permission allPermission in KIDManager.CurrentSession.GetAllPermissions())
			{
				if (allPermission.ManagedBy == Permission.ManagedByEnum.GUARDIAN && !allPermission.Enabled)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				UpdateScreenStatus(EMainScreenStatus.FullControl);
				return false;
			}
		}
		return true;
	}

	private void OnLanguageChanged()
	{
		foreach (KeyValuePair<EKIDFeatures, List<KIDUIFeatureSetting>> features in _featuresList)
		{
			List<KIDUIFeatureSetting> value = features.Value;
			if (value == null)
			{
				continue;
			}
			for (int i = 0; i < value.Count; i++)
			{
				if (value[i] != null)
				{
					value[i].RefreshTextOnLanguageChanged();
				}
			}
		}
	}

	private void UpdateOptInSetting(Permission permissionData, EKIDFeatures feature, Action<bool, Permission, bool> onOptedIn)
	{
		bool item = KIDManager.CheckFeatureOptIn(feature, permissionData).hasOptedInPreviously;
		bool flag = IsFeatureToggledOn(feature);
		Debug.Log($"[KID::UI::MainScreen] Update opt in for {feature.ToString()}. Has opted in: {item}. Toggled on: {flag}");
		KIDManager.SetFeatureOptIn(feature, flag);
		onOptedIn?.Invoke(flag, permissionData, item);
	}

	public void OnConfirmedEmailAddress(string emailAddress)
	{
		_emailAddress = emailAddress;
		Debug.LogFormat("[KID::UI::Main] Email has been confirmed: " + _emailAddress);
	}

	private IEnumerable<string> CollectPermissionsToUpgrade()
	{
		return from permission in KIDManager.GetAllPermissionsData()
			where permission.ManagedBy == Permission.ManagedByEnum.GUARDIAN && !permission.Enabled
			select permission.Name;
	}

	private void ConfigurePermissionsButtons()
	{
		Debug.Log("[KID::MAIN_SCREEN] CONFIGURE BUTTONS");
		if (!_getPermissionsButton.gameObject.activeSelf && !_gettingPermissionsButton.gameObject.activeSelf)
		{
			Debug.Log("[KID::MAIN_SCREEN] CONFIGURE BUTTONS - GET PERMISSIONS IS DISABLED");
			return;
		}
		Debug.Log("[KID::MAIN_SCREEN] CONFIGURE BUTTONS - CHECK SESSION STATUS: Is Default: [" + KIDManager.CurrentSession.IsDefault + "]");
		SetButtonContainersVisibility(GetPermissionState());
	}

	private void SetButtonContainersVisibility(EGetPermissionsStatus permissionStatus)
	{
		Debug.Log("[KID::MAIN_SCREEN] CONFIGURE BUTTONS - PERMISSION STATE: [" + permissionStatus.ToString() + "]");
		_defaultButtonsContainer.SetActive(permissionStatus == EGetPermissionsStatus.GetPermission);
		_permissionsRequestingButtonContainer.SetActive(permissionStatus == EGetPermissionsStatus.RequestingPermission);
		_permissionsRequestedButtonContainer.SetActive(permissionStatus == EGetPermissionsStatus.RequestedPermission);
	}

	private GameObject GetActiveStatusObject()
	{
		foreach (GameObject item in new List<GameObject> { _declinedStatus, _timeoutStatus, _pendingStatus, _updatedStatus, _setupRequiredStatus, _fullPlayerControlStatus })
		{
			if (item.activeInHierarchy)
			{
				return item;
			}
		}
		return null;
	}

	private static EGetPermissionsStatus GetPermissionState()
	{
		if (KIDManager.CurrentSession.IsDefault)
		{
			if (PlayerPrefs.GetInt(KIDManager.GetChallengedBeforePlayerPrefRef, 0) == 0)
			{
				Debug.Log("[KID::MAIN_SCREEN] CONFIGURE BUTTONS - SHOW DEFAULT");
				return EGetPermissionsStatus.GetPermission;
			}
			Debug.Log("[KID::MAIN_SCREEN] CONFIGURE BUTTONS - SHOW SWAPPED DEFAULT");
			return EGetPermissionsStatus.RequestingPermission;
		}
		Debug.Log("[KID::MAIN_SCREEN] CONFIGURE BUTTONS - SHOW REQUESTED");
		return EGetPermissionsStatus.RequestedPermission;
	}

	private void OnFeatureToggleChanged(EKIDFeatures feature)
	{
		switch (feature)
		{
		case EKIDFeatures.Multiplayer:
			OnMultiplayerToggled();
			break;
		case EKIDFeatures.Voice_Chat:
			OnVoiceChatToggled();
			break;
		case EKIDFeatures.Groups:
			OnGroupToggleChanged();
			break;
		case EKIDFeatures.Mods:
			OnModToggleChanged();
			break;
		case EKIDFeatures.Custom_Nametags:
			OnCustomNametagsToggled();
			break;
		default:
			Debug.LogErrorFormat("[KID::UI::MAIN_SCREEN] Toggle NOT YET IMPLEMENTED for Feature: " + feature.ToString() + ".");
			break;
		}
	}

	private void OnMultiplayerToggled()
	{
		Debug.LogErrorFormat("[KID::UI::MAIN_SCREEN] MULTIPLAYER Toggle NOT YET IMPLEMENTED.");
	}

	private void OnVoiceChatToggled()
	{
		Debug.LogErrorFormat("[KID::UI::MAIN_SCREEN] VOICE CHAT Toggle NOT YET IMPLEMENTED.");
	}

	private void OnGroupToggleChanged()
	{
		Debug.LogErrorFormat("[KID::UI::MAIN_SCREEN] GROUPS Toggle NOT YET IMPLEMENTED.");
	}

	private void OnModToggleChanged()
	{
		Debug.LogErrorFormat("[KID::UI::MAIN_SCREEN] MODS Toggle NOT YET IMPLEMENTED.");
	}

	private void OnCustomNametagsToggled()
	{
		Debug.LogErrorFormat("[KID::UI::MAIN_SCREEN] CUSTOM USERNAMES Toggle NOT YET IMPLEMENTED.");
	}
}

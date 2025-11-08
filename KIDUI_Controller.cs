using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GorillaNetworking;
using KID.Model;
using UnityEngine;

public class KIDUI_Controller : MonoBehaviour
{
	public enum Metrics_ShowReason
	{
		None,
		Inaccessible,
		Guardian_Disabled,
		Permissions_Changed,
		Default_Session,
		No_Session
	}

	private const string CLOSE_BLACK_SCREEN_ETAG_PLAYER_PREF_PREFIX = "closeBlackScreen-";

	private const string FIRST_TIME_POST_CHANGE_PLAYER_PREF = "hasShownFirstTimePostChange-";

	private static KIDUI_Controller _instance;

	[SerializeField]
	private KIDUI_MainScreen _mainKIDScreen;

	[SerializeField]
	private KIDUI_ConfirmScreen _confirmScreen;

	[SerializeField]
	private List<string> _PermissionsWithToggles = new List<string>();

	[SerializeField]
	private List<EKIDFeatures> _inaccessibleSettings = new List<EKIDFeatures>
	{
		EKIDFeatures.Multiplayer,
		EKIDFeatures.Mods
	};

	private Metrics_ShowReason _showReason;

	private bool _isKidUIActive;

	private static string etagOnCloseBlackScreenPlayerPrefStr;

	private string _lastEtagOnClose;

	public static KIDUI_Controller Instance => _instance;

	public static bool IsKIDUIActive
	{
		get
		{
			if (Instance == null)
			{
				return false;
			}
			return Instance._isKidUIActive;
		}
	}

	private static string EtagOnCloseBlackScreenPlayerPrefRef
	{
		get
		{
			if (string.IsNullOrEmpty(etagOnCloseBlackScreenPlayerPrefStr))
			{
				etagOnCloseBlackScreenPlayerPrefStr = "closeBlackScreen-" + PlayFabAuthenticator.instance.GetPlayFabPlayerId();
			}
			return etagOnCloseBlackScreenPlayerPrefStr;
		}
	}

	private void Awake()
	{
		_instance = this;
		Debug.LogFormat("[KID::UI::CONTROLLER] Controller Initialised");
	}

	private void OnDestroy()
	{
		KIDManager.onEmailResultReceived = (KIDManager.OnEmailResultReceived)Delegate.Remove(KIDManager.onEmailResultReceived, new KIDManager.OnEmailResultReceived(NotifyOfEmailResult));
	}

	public async Task StartKIDScreens(CancellationToken cancellationToken)
	{
		Debug.LogFormat("[KID::UI::CONTROLLER] Starting k-ID Screens");
		bool flag = await ShouldShowKIDScreen(cancellationToken);
		if (cancellationToken.IsCancellationRequested)
		{
			return;
		}
		if (!flag)
		{
			Debug.LogFormat("[KID::UI::CONTROLLER] Should NOT Show k-ID Screens");
			return;
		}
		PrivateUIRoom.ForceStartOverlay();
		Debug.LogFormat("[KID::UI::CONTROLLER] Showing k-ID Screens");
		while (HandRayController.Instance == null)
		{
			await Task.Yield();
		}
		HandRayController.Instance.EnableHandRays();
		PrivateUIRoom.AddUI(base.transform);
		EMainScreenStatus screenStatusFromSession = GetScreenStatusFromSession();
		_mainKIDScreen.ShowMainScreen(screenStatusFromSession, _showReason);
		_isKidUIActive = true;
		KIDManager.onEmailResultReceived = (KIDManager.OnEmailResultReceived)Delegate.Combine(KIDManager.onEmailResultReceived, new KIDManager.OnEmailResultReceived(NotifyOfEmailResult));
	}

	public void CloseKIDScreens()
	{
		SaveEtagOnCloseScreen();
		_isKidUIActive = false;
		_mainKIDScreen.HideMainScreen();
		KIDAudioManager.Instance?.PlaySoundWithDelay(KIDAudioManager.KIDSoundType.PageTransition);
		PrivateUIRoom.RemoveUI(base.transform);
		HandRayController.Instance.DisableHandRays();
		UnityEngine.Object.DestroyImmediate(base.gameObject);
		KIDManager.onEmailResultReceived = (KIDManager.OnEmailResultReceived)Delegate.Remove(KIDManager.onEmailResultReceived, new KIDManager.OnEmailResultReceived(NotifyOfEmailResult));
	}

	public void UpdateScreenStatus()
	{
		EMainScreenStatus screenStatusFromSession = GetScreenStatusFromSession();
		_mainKIDScreen?.UpdateScreenStatus(screenStatusFromSession, sendMetrics: true);
	}

	public void NotifyOfEmailResult(bool success)
	{
		if (_confirmScreen == null)
		{
			Debug.LogError("[KID::UI_CONTROLLER] _confirmScreen has not been set yet and is NULL. Cannot inform of result");
			return;
		}
		if (success)
		{
			PlayerPrefs.SetInt(KIDManager.GetChallengedBeforePlayerPrefRef, 1);
			PlayerPrefs.Save();
		}
		Debug.Log("[KID::UI_CONTROLLER] Notifying user about email result. Showing confirm screen.");
		_confirmScreen.NotifyOfResult(success);
	}

	private EMainScreenStatus GetScreenStatusFromSession()
	{
		EMainScreenStatus eMainScreenStatus = EMainScreenStatus.None;
		switch (KIDManager.CurrentSession.SessionStatus)
		{
		case SessionStatus.CHALLENGE:
		case SessionStatus.CHALLENGE_SESSION_UPGRADE:
		case SessionStatus.PENDING_AGE_APPEAL:
			if (string.IsNullOrEmpty(PlayerPrefs.GetString(KIDManager.GetEmailForUserPlayerPrefRef, "")))
			{
				return EMainScreenStatus.Setup;
			}
			return EMainScreenStatus.Pending;
		case SessionStatus.PASS:
			if (ShouldShowScreenOnPermissionChange())
			{
				return EMainScreenStatus.Updated;
			}
			if (KIDManager.PreviousStatus == SessionStatus.CHALLENGE_SESSION_UPGRADE)
			{
				return EMainScreenStatus.Declined;
			}
			return EMainScreenStatus.Missing;
		case SessionStatus.PROHIBITED:
			Debug.LogError("[KID::KIDUI_CONTROLLER] Status is PROHIBITED but is trying to show k-ID screens");
			return EMainScreenStatus.Declined;
		default:
			Debug.LogError("[KID::KIDUI_CONTROLLER] Unknown status");
			return EMainScreenStatus.None;
		}
	}

	private async Task<bool> ShouldShowKIDScreen(CancellationToken cancellationToken)
	{
		if (KIDManager.CurrentSession == null)
		{
			_showReason = Metrics_ShowReason.No_Session;
			return true;
		}
		if (!KIDManager.CurrentSession.IsValidSession)
		{
			while (!KIDManager.CurrentSession.IsValidSession)
			{
				Debug.Log("[KID::UI::CONTROLLER] K-ID Session not found yet");
				await Task.Delay(100, cancellationToken);
			}
		}
		Debug.Log("[KID::UI::CONTROLLER] K-ID Session has been found and is proceeding ");
		if (KIDManager.HasAllPermissions())
		{
			return false;
		}
		for (int i = 0; i < _inaccessibleSettings.Count; i++)
		{
			Permission permissionDataByFeature = KIDManager.GetPermissionDataByFeature(_inaccessibleSettings[i]);
			if (permissionDataByFeature == null)
			{
				Debug.LogErrorFormat($"[KID::UI::CONTROLLER] Failed to get Permission with name [{_inaccessibleSettings[i]}]");
				return true;
			}
			if (permissionDataByFeature.ManagedBy != Permission.ManagedByEnum.PROHIBITED && !KIDManager.CheckFeatureSettingEnabled(_inaccessibleSettings[i]))
			{
				_showReason = Metrics_ShowReason.Inaccessible;
				if (KIDManager.CurrentSession.IsDefault)
				{
					_showReason = Metrics_ShowReason.Default_Session;
				}
				return true;
			}
		}
		List<Permission> allPermissionsData = KIDManager.GetAllPermissionsData();
		for (int j = 0; j < allPermissionsData.Count; j++)
		{
			if (allPermissionsData[j].ManagedBy == Permission.ManagedByEnum.GUARDIAN && !allPermissionsData[j].Enabled)
			{
				_showReason = Metrics_ShowReason.Guardian_Disabled;
				if (KIDManager.CurrentSession.IsDefault)
				{
					_showReason = Metrics_ShowReason.Default_Session;
				}
				return true;
			}
		}
		_mainKIDScreen.InitialiseMainScreen();
		if (_mainKIDScreen.GetFeatureListingCount() == 0)
		{
			Debug.Log("[KID::CONTROLLER] Nothing to show on k-ID UI. Skipping");
			return false;
		}
		if (ShouldShowScreenOnPermissionChange())
		{
			_showReason = Metrics_ShowReason.Permissions_Changed;
			return true;
		}
		return false;
	}

	private bool ShouldShowScreenOnPermissionChange()
	{
		_lastEtagOnClose = GetLastBlackScreenEtag();
		return _lastEtagOnClose != (KIDManager.CurrentSession?.Etag ?? string.Empty);
	}

	private string GetLastBlackScreenEtag()
	{
		return PlayerPrefs.GetString(EtagOnCloseBlackScreenPlayerPrefRef, "");
	}

	private void SaveEtagOnCloseScreen()
	{
		if (KIDManager.CurrentSession == null)
		{
			Debug.Log("[KID::MANAGER] Trying to save Pre-Game Screen ETAG, but [CurrentSession] is null");
			return;
		}
		PlayerPrefs.SetString(EtagOnCloseBlackScreenPlayerPrefRef, KIDManager.CurrentSession.Etag);
		PlayerPrefs.Save();
	}

	public void OnDisable()
	{
		KIDAudioManager.Instance?.PlaySoundWithDelay(KIDAudioManager.KIDSoundType.PageTransition);
	}
}

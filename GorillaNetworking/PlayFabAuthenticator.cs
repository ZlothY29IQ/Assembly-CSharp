using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using GorillaExtensions;
using JetBrains.Annotations;
using PlayFab;
using PlayFab.ClientModels;
using Steamworks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GorillaNetworking;

public class PlayFabAuthenticator : MonoBehaviour
{
	public enum SafetyType
	{
		None,
		Auto,
		OptIn
	}

	[Serializable]
	public class CachePlayFabIdRequest
	{
		public string Platform;

		public string SessionTicket;

		public string PlayFabId;

		public string TitleId;

		public string MothershipEnvId;

		public string MothershipDeploymentId;

		public string MothershipToken;

		public string MothershipId;
	}

	[Serializable]
	public class PlayfabAuthRequestData
	{
		public string AppId;

		public string Nonce;

		public string OculusId;

		public string Platform;

		public string AgeCategory;

		public string MothershipEnvId;

		public string MothershipDeploymentId;

		public string MothershipToken;

		public string MothershipId;
	}

	[Serializable]
	public class PlayfabAuthResponseData
	{
		public string SessionTicket;

		public string EntityToken;

		public string PlayFabId;

		public string EntityId;

		public string EntityType;

		public string AccountCreationIsoTimestamp;
	}

	[Serializable]
	public class CachePlayFabIdResponse
	{
		public string PlayFabId;

		public string SteamAuthIdForPhoton;

		public string AccountCreationIsoTimestamp;
	}

	private class ErrorInfo
	{
		public string Message;

		public string Error;
	}

	private class BanInfo
	{
		public string BanMessage;

		public string BanExpirationTime;
	}

	public static volatile PlayFabAuthenticator instance;

	private const int PlayFabAuthRequestTimeout = 30;

	private string _playFabPlayerIdCache;

	private string _sessionTicket;

	private string _displayName;

	private string _nonce;

	public string userID;

	private string userToken;

	public PlatformTagJoin platform;

	private bool isSafeAccount;

	public Action<bool> OnSafetyUpdate;

	private SafetyType safetyType;

	private byte[] m_Ticket;

	private uint m_pcbTicket;

	public Text debugText;

	public bool screenDebugMode;

	public bool loginFailed;

	[FormerlySerializedAs("loginDisplayID")]
	public GameObject emptyObject;

	private int playFabAuthRetryCount;

	private int playFabMaxRetries = 5;

	private int playFabCacheRetryCount;

	private int playFabCacheMaxRetries = 5;

	public MetaAuthenticator metaAuthenticator;

	public SteamAuthenticator steamAuthenticator;

	public MothershipAuthenticator mothershipAuthenticator;

	public PhotonAuthenticator photonAuthenticator;

	[SerializeField]
	private bool dbg_isReturningPlayer;

	private SteamAuthTicket steamAuthTicketForPlayFab;

	private SteamAuthTicket steamAuthTicketForPhoton;

	private string steamAuthIdForPhoton;

	public GorillaComputer gorillaComputer => GorillaComputer.instance;

	public bool IsReturningPlayer { get; private set; }

	public bool postAuthSetSafety { get; private set; }

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
		else if (instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
		if (instance.photonAuthenticator == null)
		{
			instance.photonAuthenticator = instance.gameObject.GetOrAddComponent<PhotonAuthenticator>();
		}
		platform = ScriptableObject.CreateInstance<PlatformTagJoin>();
		PlayFabSettings.CompressApiData = false;
		_ = new byte[1];
		if (screenDebugMode)
		{
			debugText.text = "";
		}
		Debug.Log("doing steam thing");
		if (instance.steamAuthenticator == null)
		{
			instance.steamAuthenticator = instance.gameObject.GetOrAddComponent<SteamAuthenticator>();
		}
		platform.PlatformTag = "Steam";
		PlayFabSettings.TitleId = PlayFabAuthenticatorSettings.TitleId;
		PlayFabSettings.DisableFocusTimeCollection = true;
		BeginLoginFlow();
	}

	public void BeginLoginFlow()
	{
		if (!MothershipClientApiUnity.IsEnabled())
		{
			AuthenticateWithPlayFab();
		}
		else if (instance.mothershipAuthenticator == null)
		{
			instance.mothershipAuthenticator = MothershipAuthenticator.Instance ?? instance.gameObject.GetOrAddComponent<MothershipAuthenticator>();
			MothershipAuthenticator obj = instance.mothershipAuthenticator;
			obj.OnLoginSuccess = (Action)Delegate.Combine(obj.OnLoginSuccess, (Action)delegate
			{
				instance.AuthenticateWithPlayFab();
			});
			MothershipAuthenticator obj2 = instance.mothershipAuthenticator;
			obj2.OnLoginFailure = (Action<string, string, string>)Delegate.Combine(obj2.OnLoginFailure, (Action<string, string, string>)delegate(string errorMessage, string errorCode, string traceId)
			{
				SetLoginFailed();
				ShowMothershipAuthErrorMessage(errorMessage, errorCode, traceId);
			});
			instance.mothershipAuthenticator.BeginLoginFlow();
		}
	}

	private void SetLoginFailed()
	{
		loginFailed = true;
		NetworkSystem.Instance?.FinishAuthenticating();
	}

	private void Start()
	{
	}

	private void OnEnable()
	{
		NetworkSystem.Instance.OnCustomAuthenticationResponse += OnCustomAuthenticationResponse;
	}

	private void OnDisable()
	{
		NetworkSystem.Instance.OnCustomAuthenticationResponse -= OnCustomAuthenticationResponse;
		steamAuthTicketForPhoton?.Dispose();
		steamAuthTicketForPlayFab?.Dispose();
	}

	public void RefreshSteamAuthTicketForPhoton(Action<string> successCallback, Action<EResult> failureCallback)
	{
		steamAuthTicketForPhoton?.Dispose();
		steamAuthTicketForPhoton = steamAuthenticator.GetAuthTicketForWebApi(steamAuthIdForPhoton, successCallback, failureCallback);
	}

	private void OnCustomAuthenticationResponse(Dictionary<string, object> response)
	{
		steamAuthTicketForPhoton?.Dispose();
		if (response.TryGetValue("SteamAuthIdForPhoton", out var value) && value is string text)
		{
			steamAuthIdForPhoton = text;
		}
		else
		{
			steamAuthIdForPhoton = null;
		}
	}

	public void AuthenticateWithPlayFab()
	{
		Debug.Log("authenticating with playFab!");
		GorillaServer gorillaServer = GorillaServer.Instance;
		if ((object)gorillaServer != null && gorillaServer.FeatureFlagsReady)
		{
			if (KIDManager.KidEnabled)
			{
				Debug.Log("[KID] Is Enabled - Enabling safeties by platform and age category");
				DefaultSafetiesByAgeCategory();
			}
		}
		else
		{
			postAuthSetSafety = true;
		}
		if (SteamManager.Initialized)
		{
			userID = SteamUser.GetSteamID().ToString();
			Debug.Log("trying to auth with steam");
			steamAuthTicketForPlayFab = steamAuthenticator.GetAuthTicket(delegate(string ticket)
			{
				Debug.Log("Got steam auth session ticket!");
				PlayFabClientAPI.LoginWithSteam(new LoginWithSteamRequest
				{
					CreateAccount = true,
					SteamTicket = ticket
				}, OnLoginWithSteamResponse, OnPlayFabError);
			}, delegate
			{
				StartCoroutine(DisplayGeneralFailureMessageOnGorillaComputerAfter1Frame());
			});
		}
		else
		{
			StartCoroutine(DisplayGeneralFailureMessageOnGorillaComputerAfter1Frame());
		}
	}

	private IEnumerator VerifyKidAuthenticated(DateTime accountCreationDateTime)
	{
		Task<DateTime?> getNewPlayerDateTimeTask = KIDManager.CheckKIDNewPlayerDateTime();
		yield return new WaitUntil(() => getNewPlayerDateTimeTask.IsCompleted);
		DateTime? result = getNewPlayerDateTimeTask.Result;
		if (result.HasValue && KIDManager.KidEnabled)
		{
			PlayFabAuthenticator playFabAuthenticator = this;
			DateTime? dateTime = result;
			playFabAuthenticator.IsReturningPlayer = accountCreationDateTime < dateTime;
		}
	}

	private IEnumerator DisplayGeneralFailureMessageOnGorillaComputerAfter1Frame()
	{
		yield return null;
		if (gorillaComputer != null)
		{
			gorillaComputer.GeneralFailureMessage("UNABLE TO AUTHENTICATE YOUR STEAM ACCOUNT! PLEASE MAKE SURE STEAM IS RUNNING AND YOU ARE LAUNCHING THE GAME DIRECTLY FROM STEAM.");
			gorillaComputer.screenText.Set("UNABLE TO AUTHENTICATE YOUR STEAM ACCOUNT! PLEASE MAKE SURE STEAM IS RUNNING AND YOU ARE LAUNCHING THE GAME DIRECTLY FROM STEAM.");
			Debug.Log("Couldn't authenticate steam account");
		}
		else
		{
			Debug.LogError("PlayFabAuthenticator: gorillaComputer is null, so could not set GeneralFailureMessage notifying user that the steam account could not be authenticated.", this);
		}
	}

	private void OnLoginWithSteamResponse(LoginResult obj)
	{
		_playFabPlayerIdCache = obj.PlayFabId;
		_sessionTicket = obj.SessionTicket;
		StartCoroutine(CachePlayFabId(new CachePlayFabIdRequest
		{
			Platform = platform.ToString(),
			SessionTicket = _sessionTicket,
			PlayFabId = _playFabPlayerIdCache,
			TitleId = PlayFabSettings.TitleId,
			MothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			MothershipDeploymentId = MothershipClientApiUnity.DeploymentId,
			MothershipToken = MothershipClientContext.Token,
			MothershipId = MothershipClientContext.MothershipId
		}, OnCachePlayFabIdRequest));
	}

	private void OnCachePlayFabIdRequest([CanBeNull] CachePlayFabIdResponse response)
	{
		if (response != null)
		{
			steamAuthIdForPhoton = response.SteamAuthIdForPhoton;
			if (DateTime.TryParse(response.AccountCreationIsoTimestamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result))
			{
				StartCoroutine(VerifyKidAuthenticated(result));
			}
			Debug.Log("Successfully cached PlayFab Id.  Continuing!");
			AdvanceLogin();
		}
		else
		{
			Debug.LogError("Could not cache PlayFab Id.  Cannot continue.");
		}
	}

	private void AdvanceLogin()
	{
		LogMessage("PlayFab authenticated ... Getting Nonce");
		RefreshSteamAuthTicketForPhoton(delegate(string ticket)
		{
			_nonce = ticket;
			Debug.Log("Got nonce!  Authenticating...");
			AuthenticateWithPhoton();
		}, delegate
		{
			Debug.LogWarning("Failed to get nonce!");
			AuthenticateWithPhoton();
		});
	}

	private void AuthenticateWithPhoton()
	{
		photonAuthenticator.SetCustomAuthenticationParameters(new Dictionary<string, object>
		{
			{
				"AppId",
				PlayFabSettings.TitleId
			},
			{
				"AppVersion",
				NetworkSystemConfig.AppVersion ?? "-1"
			},
			{ "Ticket", _sessionTicket },
			{ "Nonce", _nonce },
			{
				"MothershipEnvId",
				MothershipClientApiUnity.EnvironmentId
			},
			{
				"MothershipDeploymentId",
				MothershipClientApiUnity.DeploymentId
			},
			{
				"MothershipToken",
				MothershipClientContext.Token
			}
		});
		GetPlayerDisplayName(_playFabPlayerIdCache);
		GorillaServer.Instance.AddOrRemoveDLCOwnership(delegate
		{
			Debug.Log("got results! updating!");
			if (GorillaTagger.Instance != null)
			{
				GorillaTagger.Instance.offlineVRRig.GetCosmeticsPlayFabCatalogData();
			}
		}, delegate(PlayFabError error)
		{
			Debug.Log("Got error retrieving user data:");
			Debug.Log(error.GenerateErrorReport());
			if (GorillaTagger.Instance != null)
			{
				GorillaTagger.Instance.offlineVRRig.GetCosmeticsPlayFabCatalogData();
			}
		});
		if (CosmeticsController.instance != null)
		{
			Debug.Log("initializing cosmetics");
			CosmeticsController.instance.Initialize();
		}
		if (gorillaComputer != null)
		{
			gorillaComputer.OnConnectedToMasterStuff();
		}
		else
		{
			StartCoroutine(ComputerOnConnectedToMaster());
		}
		if (RankedProgressionManager.Instance != null)
		{
			RankedProgressionManager.Instance.LoadStats();
		}
		if (PhotonNetworkController.Instance != null)
		{
			Debug.Log("Finish authenticating");
			NetworkSystem.Instance.FinishAuthenticating();
		}
	}

	private IEnumerator ComputerOnConnectedToMaster()
	{
		WaitForEndOfFrame frameYield = new WaitForEndOfFrame();
		while (gorillaComputer == null)
		{
			yield return frameYield;
		}
		gorillaComputer.OnConnectedToMasterStuff();
	}

	private void OnPlayFabError(PlayFabError obj)
	{
		LogMessage(obj.ErrorMessage);
		Debug.Log("OnPlayFabError(): " + obj.ErrorMessage);
		SetLoginFailed();
		if (obj.ErrorMessage == "The account making this request is currently banned")
		{
			using (Dictionary<string, List<string>>.Enumerator enumerator = obj.ErrorDetails.GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					KeyValuePair<string, List<string>> current = enumerator.Current;
					if (current.Value[0] != "Indefinite")
					{
						gorillaComputer.GeneralFailureMessage("YOUR ACCOUNT HAS BEEN BANNED. YOU WILL NOT BE ABLE TO PLAY UNTIL THE BAN EXPIRES.\nREASON: " + current.Key + "\nHOURS LEFT: " + (int)((DateTime.Parse(current.Value[0]) - DateTime.UtcNow).TotalHours + 1.0));
					}
					else
					{
						gorillaComputer.GeneralFailureMessage("YOUR ACCOUNT HAS BEEN BANNED INDEFINITELY.\nREASON: " + current.Key);
					}
				}
				return;
			}
		}
		if (obj.ErrorMessage == "The IP making this request is currently banned")
		{
			using (Dictionary<string, List<string>>.Enumerator enumerator = obj.ErrorDetails.GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					KeyValuePair<string, List<string>> current2 = enumerator.Current;
					if (current2.Value[0] != "Indefinite")
					{
						gorillaComputer.GeneralFailureMessage("THIS IP HAS BEEN BANNED. YOU WILL NOT BE ABLE TO PLAY UNTIL THE BAN EXPIRES.\nREASON: " + current2.Key + "\nHOURS LEFT: " + (int)((DateTime.Parse(current2.Value[0]) - DateTime.UtcNow).TotalHours + 1.0));
					}
					else
					{
						gorillaComputer.GeneralFailureMessage("THIS IP HAS BEEN BANNED INDEFINITELY.\nREASON: " + current2.Key);
					}
				}
				return;
			}
		}
		if (gorillaComputer != null)
		{
			gorillaComputer.GeneralFailureMessage(gorillaComputer.unableToConnect);
		}
	}

	private void LogMessage(string message)
	{
	}

	private void GetPlayerDisplayName(string playFabId)
	{
		PlayFabClientAPI.GetPlayerProfile(new GetPlayerProfileRequest
		{
			PlayFabId = playFabId,
			ProfileConstraints = new PlayerProfileViewConstraints
			{
				ShowDisplayName = true
			}
		}, delegate(GetPlayerProfileResult result)
		{
			_displayName = result.PlayerProfile.DisplayName;
		}, delegate(PlayFabError error)
		{
			Debug.LogError(error.GenerateErrorReport());
		});
	}

	public void SetDisplayName(string playerName)
	{
		if (_displayName == null || (_displayName.Length > 4 && _displayName.Substring(0, _displayName.Length - 4) != playerName && _displayName != playerName))
		{
			PlayFabClientAPI.UpdateUserTitleDisplayName(new UpdateUserTitleDisplayNameRequest
			{
				DisplayName = playerName
			}, delegate
			{
				_displayName = playerName;
			}, delegate(PlayFabError error)
			{
				Debug.LogError("Error with name: " + playerName + ". Error is " + error.GenerateErrorReport());
			});
		}
	}

	public void ScreenDebug(string debugString)
	{
		Debug.Log(debugString);
		if (screenDebugMode)
		{
			Text text = debugText;
			text.text = text.text + debugString + "\n";
		}
	}

	public void ScreenDebugClear()
	{
		debugText.text = "";
	}

	public IEnumerator PlayfabAuthenticate(PlayfabAuthRequestData data, Action<PlayfabAuthResponseData> callback)
	{
		UnityWebRequest request = new UnityWebRequest(PlayFabAuthenticatorSettings.AuthApiBaseUrl + "/api/PlayFabAuthentication", "POST");
		byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data));
		bool retry = false;
		request.uploadHandler = new UploadHandlerRaw(bytes);
		request.downloadHandler = new DownloadHandlerBuffer();
		request.SetRequestHeader("Content-Type", "application/json");
		request.timeout = 30;
		yield return request.SendWebRequest();
		if (request.result != UnityWebRequest.Result.ConnectionError && request.result != UnityWebRequest.Result.ProtocolError)
		{
			PlayfabAuthResponseData obj = JsonUtility.FromJson<PlayfabAuthResponseData>(request.downloadHandler.text);
			callback(obj);
		}
		else
		{
			if (request.responseCode == 403)
			{
				Debug.LogError($"HTTP {request.responseCode}: {request.error}, with body: {request.downloadHandler.text}");
				BanInfo banInfo = JsonUtility.FromJson<BanInfo>(request.downloadHandler.text);
				ShowBanMessage(banInfo);
				callback(null);
			}
			if (request.result == UnityWebRequest.Result.ProtocolError && request.responseCode != 400)
			{
				retry = true;
				Debug.LogError($"HTTP {request.responseCode} error: {request.error} message:{request.downloadHandler.text}");
			}
			else if (request.result == UnityWebRequest.Result.ConnectionError)
			{
				retry = true;
				Debug.LogError("NETWORK ERROR: " + request.error + "\nMessage: " + request.downloadHandler.text);
			}
			else
			{
				Debug.LogError("HTTP ERROR: " + request.error + "\nMessage: " + request.downloadHandler.text);
				retry = true;
			}
		}
		if (retry)
		{
			if (playFabAuthRetryCount < playFabMaxRetries)
			{
				int num = (int)Mathf.Pow(2f, playFabAuthRetryCount + 1);
				Debug.LogWarning($"Retrying PlayFab auth... Retry attempt #{playFabAuthRetryCount + 1}, waiting for {num} seconds");
				playFabAuthRetryCount++;
				yield return new WaitForSecondsRealtime(num);
			}
			else
			{
				Debug.LogError("Maximum retries attempted. Please check your network connection.");
				callback(null);
				ShowPlayFabAuthErrorMessage(request.downloadHandler.text);
			}
		}
	}

	private void ShowMothershipAuthErrorMessage(string errorMessage, string errorCode, string traceId)
	{
		try
		{
			StringBuilder stringBuilder = new StringBuilder("UNABLE TO AUTHENTICATE WITH MOTHERSHIP.\nREASON: " + errorMessage);
			if (!char.IsPunctuation(stringBuilder[stringBuilder.Length - 1]))
			{
				stringBuilder.Append('.');
			}
			if (!string.IsNullOrEmpty(errorCode))
			{
				stringBuilder.Append("\nERROR CODE: " + errorCode);
			}
			if (!string.IsNullOrEmpty(traceId))
			{
				stringBuilder.Append("\nTRACE ID: " + traceId);
			}
			gorillaComputer.GeneralFailureMessage(stringBuilder.ToString());
		}
		catch (Exception arg)
		{
			Debug.LogError($"Failed to show Mothership auth error message: {arg}");
		}
	}

	private void ShowPlayFabAuthErrorMessage(string errorJson)
	{
		try
		{
			ErrorInfo errorInfo = JsonUtility.FromJson<ErrorInfo>(errorJson);
			StringBuilder stringBuilder = new StringBuilder("UNABLE TO AUTHENTICATE WITH PLAYFAB.\nREASON: " + errorInfo.Message);
			if (!char.IsPunctuation(stringBuilder[stringBuilder.Length - 1]))
			{
				stringBuilder.Append('.');
			}
			gorillaComputer.GeneralFailureMessage(stringBuilder.ToString());
		}
		catch (Exception arg)
		{
			Debug.LogError($"Failed to show PlayFab auth error message: {arg}");
		}
	}

	private void ShowBanMessage(BanInfo banInfo)
	{
		try
		{
			if (banInfo.BanExpirationTime != null && banInfo.BanMessage != null)
			{
				if (banInfo.BanExpirationTime != "Indefinite")
				{
					gorillaComputer.GeneralFailureMessage("YOUR ACCOUNT HAS BEEN BANNED. YOU WILL NOT BE ABLE TO PLAY UNTIL THE BAN EXPIRES.\nREASON: " + banInfo.BanMessage + "\nHOURS LEFT: " + (int)((DateTime.Parse(banInfo.BanExpirationTime) - DateTime.UtcNow).TotalHours + 1.0));
				}
				else
				{
					gorillaComputer.GeneralFailureMessage("YOUR ACCOUNT HAS BEEN BANNED INDEFINITELY.\nREASON: " + banInfo.BanMessage);
				}
			}
		}
		catch (Exception arg)
		{
			Debug.LogError($"Failed to show ban message: {arg}");
		}
	}

	public IEnumerator CachePlayFabId(CachePlayFabIdRequest data, Action<CachePlayFabIdResponse> callback)
	{
		Debug.Log("Trying to cache playfab Id");
		UnityWebRequest request = new UnityWebRequest(PlayFabAuthenticatorSettings.AuthApiBaseUrl + "/api/CachePlayFabId", "POST");
		byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data));
		bool retry = false;
		request.uploadHandler = new UploadHandlerRaw(bytes);
		request.downloadHandler = new DownloadHandlerBuffer();
		request.SetRequestHeader("Content-Type", "application/json");
		request.timeout = 30;
		yield return request.SendWebRequest();
		if (request.result != UnityWebRequest.Result.ConnectionError && request.result != UnityWebRequest.Result.ProtocolError)
		{
			if (request.responseCode == 200)
			{
				CachePlayFabIdResponse obj = JsonUtility.FromJson<CachePlayFabIdResponse>(request.downloadHandler.text);
				callback(obj);
			}
		}
		else if (request.result != UnityWebRequest.Result.ProtocolError || request.responseCode == 400)
		{
			retry = request.result != UnityWebRequest.Result.ConnectionError || true;
		}
		else
		{
			retry = true;
			Debug.LogError($"HTTP {request.responseCode} error: {request.error}");
		}
		if (retry)
		{
			if (playFabCacheRetryCount < playFabCacheMaxRetries)
			{
				int num = (int)Mathf.Pow(2f, playFabCacheRetryCount + 1);
				Debug.LogWarning($"Retrying PlayFab auth... Retry attempt #{playFabCacheRetryCount + 1}, waiting for {num} seconds");
				playFabCacheRetryCount++;
				yield return new WaitForSecondsRealtime(num);
				StartCoroutine(CachePlayFabId(new CachePlayFabIdRequest
				{
					Platform = platform.ToString(),
					SessionTicket = _sessionTicket,
					PlayFabId = _playFabPlayerIdCache,
					TitleId = PlayFabSettings.TitleId,
					MothershipEnvId = MothershipClientApiUnity.EnvironmentId,
					MothershipDeploymentId = MothershipClientApiUnity.DeploymentId,
					MothershipToken = MothershipClientContext.Token,
					MothershipId = MothershipClientContext.MothershipId
				}, OnCachePlayFabIdRequest));
			}
			else
			{
				Debug.LogError("Maximum retries attempted. Please check your network connection.");
				callback(null);
				ShowPlayFabAuthErrorMessage(request.downloadHandler.text);
			}
		}
	}

	public void DefaultSafetiesByAgeCategory()
	{
		Debug.Log("[KID::PLAYFAB_AUTHENTICATOR] Defaulting Safety Settings to Disabled because age category data unavailable on this platform");
		SetSafety(isSafety: false, isAutoSet: true);
	}

	public void SetSafety(bool isSafety, bool isAutoSet, bool setPlayfab = false)
	{
		postAuthSetSafety = false;
		OnSafetyUpdate?.Invoke(isSafety);
		Debug.Log("[KID] Setting safety to: [" + isSafety + "]");
		isSafeAccount = isSafety;
		safetyType = SafetyType.None;
		if (isSafety)
		{
			if (isAutoSet)
			{
				PlayerPrefs.SetInt("autoSafety", 1);
				safetyType = SafetyType.Auto;
			}
			else
			{
				PlayerPrefs.SetInt("optSafety", 1);
				safetyType = SafetyType.OptIn;
			}
		}
		else
		{
			if (isAutoSet)
			{
				PlayerPrefs.SetInt("autoSafety", 0);
			}
			else
			{
				PlayerPrefs.SetInt("optSafety", 0);
			}
			PlayerPrefs.Save();
		}
	}

	public string GetPlayFabSessionTicket()
	{
		return _sessionTicket;
	}

	public string GetPlayFabPlayerId()
	{
		return _playFabPlayerIdCache;
	}

	public bool GetSafety()
	{
		return isSafeAccount;
	}

	public SafetyType GetSafetyType()
	{
		return safetyType;
	}

	public string GetUserID()
	{
		return userID;
	}
}

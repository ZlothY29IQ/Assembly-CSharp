using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GorillaNetworking;
using GorillaTagScripts.VirtualStumpCustomMaps;
using GorillaTagScripts.VirtualStumpCustomMaps.ModIO;
using GT_CustomMapSupportRuntime;
using Modio;
using Modio.API;
using Modio.Authentication;
using Modio.Customizations;
using Modio.Errors;
using Modio.FileIO;
using Modio.Mods;
using Modio.Unity;
using Modio.Users;
using Newtonsoft.Json;
using Steamworks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class ModIOManager : MonoBehaviour, ISteamCredentialProvider, IOculusCredentialProvider
{
	public enum ModIOAuthMethod
	{
		Invalid,
		LinkedAccount,
		Steam,
		Oculus
	}

	private const string MODIO_ACCEPTED_TERMS_KEY = "modIOAcceptedTermsHash";

	private const string MODIO_ACCEPTED_TERMS_OF_USE_ID_KEY = "modIOAcceptedTermsOfUseId";

	private const string MODIO_ACCEPTED_PRIVACY_POLICY_ID_KEY = "modIOAcceptedPrivacyPolicyId";

	private const string MODIO_LAST_AUTH_METHOD_KEY = "modIOLassSuccessfulAuthMethod";

	private const string FAVORITES_FILE_NAME = "favoriteMods.json";

	private const float REFRESH_RATE_LIMIT = 5f;

	[OnEnterPlay_SetNull]
	private static volatile ModIOManager instance;

	[OnEnterPlay_Set(false)]
	private static bool hasInstance;

	private static string ModIODirectory;

	private static ModioWssAuthService accountLinkingAuthService = new ModioWssAuthService();

	private static bool initialized;

	private static bool refreshing;

	private static bool modManagementEnabled;

	private static bool loggingIn;

	private static bool loggingOut;

	private static bool refreshingModCache;

	private static bool favoriteModsLoaded;

	private static bool restartRefreshModCache;

	private static Coroutine refreshDisabledCoroutine;

	private static float lastRefreshTime;

	private static List<Action<bool>> currentRefreshCallbacks = new List<Action<bool>>();

	private static Action<ModIORequestResultAnd<bool>> modIOTermsAcknowledgedCallback;

	private static Dictionary<ModId, Mod> favoriteMods = new Dictionary<ModId, Mod>();

	private static Dictionary<ModId, int> outdatedModCMSVersions = new Dictionary<ModId, int>();

	private static byte[] ticketBlob = new byte[1024];

	private static uint ticketSize;

	protected static CallResult<EncryptedAppTicketResponse_t> requestEncryptedAppTicketResponse = null;

	private Action<bool, string> requestEncryptedAppTicketCallback;

	private static ModioSteamAuthService steamAuthService = new ModioSteamAuthService();

	[SerializeField]
	private GameObject modIOTermsOfUsePrefab;

	[SerializeField]
	private long newMapsModId;

	public static UnityEvent OnModIOLoginStarted = new UnityEvent();

	public static UnityEvent OnModIOLoggedIn = new UnityEvent();

	public static UnityEvent<string> OnModIOLoginFailed = new UnityEvent<string>();

	public static UnityEvent OnModIOLoggedOut = new UnityEvent();

	public static UnityEvent<User> OnModIOUserChanged = new UnityEvent<User>();

	public static UnityEvent<Mod, Modfile, ModInstallationManagement.OperationType, ModInstallationManagement.OperationPhase> OnModManagementEvent = new UnityEvent<Mod, Modfile, ModInstallationManagement.OperationType, ModInstallationManagement.OperationPhase>();

	public static UnityEvent OnModIOCacheRefreshing = new UnityEvent();

	public static UnityEvent OnModIOCacheRefreshed = new UnityEvent();

	private static int associationMaxRetries = 5;

	private static int currentAssociationRetries = 0;

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
			hasInstance = true;
			UGCPermissionManager.SubscribeToUGCEnabled(OnUGCEnabled);
			UGCPermissionManager.SubscribeToUGCDisabled(OnUGCDisabled);
			ModioServices.Bind<IModioAuthService>().FromInstance(accountLinkingAuthService, (ModioServicePriority)41);
			ModioServices.Bind<IModioAuthService>().FromInstance(steamAuthService);
			long gameId = ModioServices.Resolve<ModioSettings>().GameId;
			ModIODirectory = Path.Combine(ModioServices.Resolve<IModioRootPathProvider>().Path, "mod.io", gameId.ToString()) + Path.DirectorySeparatorChar;
		}
		else if (instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private void Start()
	{
		NetworkSystem.Instance.OnMultiplayerStarted += new Action(OnJoinedRoom);
	}

	private void OnDestroy()
	{
		if (instance == this)
		{
			instance = null;
			hasInstance = false;
			UGCPermissionManager.UnsubscribeFromUGCEnabled(OnUGCEnabled);
			UGCPermissionManager.UnsubscribeFromUGCDisabled(OnUGCDisabled);
		}
		NetworkSystem.Instance.OnMultiplayerStarted -= new Action(OnJoinedRoom);
	}

	private void Update()
	{
		_ = hasInstance;
	}

	private static void OnUGCEnabled()
	{
	}

	private static void OnUGCDisabled()
	{
	}

	public static bool IsInitialized()
	{
		return initialized;
	}

	public static async Task<Error> Initialize()
	{
		if (UGCPermissionManager.IsUGCDisabled)
		{
			return new Error(ErrorCode.UNKNOWN, "MOD.IO FUNCTIONALITY IS CURRENTLY DISABLED.");
		}
		if (initialized)
		{
			return Error.None;
		}
		return await InitInternal();
	}

	private static async Task<Error> InitInternal()
	{
		if (UGCPermissionManager.IsUGCDisabled)
		{
			return new Error(ErrorCode.UNKNOWN, "MOD.IO FUNCTIONALITY IS CURRENTLY DISABLED.");
		}
		if (initialized)
		{
			return Error.None;
		}
		User.OnUserChanged -= ModIOUserChanged;
		User.OnUserChanged += ModIOUserChanged;
		User.OnUserSyncComplete -= ModIOUserSyncComplete;
		User.OnUserSyncComplete += ModIOUserSyncComplete;
		Error error = await ModioClient.Init();
		if ((bool)error)
		{
			ModioLog.Error?.Log($"[ModIOManager::InitInternal] Error initializing mod.io: {error}");
			return error;
		}
		EnableModManagement();
		initialized = true;
		await GetFavoriteMods();
		return Error.None;
	}

	private async Task<(Error, bool, bool)> HasAcceptedLatestTerms()
	{
		if (!initialized)
		{
			ModioLog.Error?.Log("[ModIOManager] HasAcceptedLatestTerms called before ModIO has been initialized!");
			return (new Error(ErrorCode.NOT_INITIALIZED, "ModIOManager has not been initialized!"), false, false);
		}
		ModioLog.Verbose?.Log("[ModIOManager::HasAcceptedLatestTerms] Retrieving terms of use from mod.io...");
		var (error, fullTermsOfUse) = await Agreement.GetAgreement(AgreementType.TermsOfUse);
		if ((bool)error)
		{
			ModioLog.Error?.Log($"[ModIOManager::HasAcceptedLatestTerms] Failed to get Mod.io Terms of Use: {error} ");
			return (error, false, false);
		}
		var (error2, agreement) = await Agreement.GetAgreement(AgreementType.PrivacyPolicy);
		if ((bool)error2)
		{
			ModioLog.Error?.Log("[ModIOManager::HasAcceptedLatestTerms] Failed to get Mod.io Privacy Policy: " + $"{error2} ");
			return (error2, false, false);
		}
		ModioLog.Verbose?.Log("[ModIOManager::OnReceivedTermsOfUse] retrieved terms of use from mod.io, checking if already accepted...");
		long.TryParse(PlayerPrefs.GetString("modIOAcceptedTermsOfUseId"), out var result);
		bool flag = fullTermsOfUse.Id == result;
		long.TryParse(PlayerPrefs.GetString("modIOAcceptedPrivacyPolicyId"), out var result2);
		bool flag2 = agreement.Id == result2;
		ModioLog.Verbose?.Log("[ModIOManager::OnReceivedTermsOfUse] Pre-Editor Skip: " + $"Terms already accepted: {flag} | " + $"Privacy Policy already accepted: {flag2}");
		ModioLog.Verbose?.Log("[ModIOManager::OnReceivedTermsOfUse] Post-Editor Skip: " + $"Terms already accepted: {flag} | " + $"Privacy Policy already accepted: {flag2}");
		return (Error.None, flag, flag2);
	}

	private async Task<Error> ShowModIOTermsOfUse()
	{
		if (!initialized)
		{
			return new Error(ErrorCode.NOT_INITIALIZED, "ModIOManager has not been initialized!");
		}
		if (modIOTermsOfUsePrefab != null)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(modIOTermsOfUsePrefab, base.transform);
			if (gameObject != null)
			{
				ModIOTermsOfUse_v2 component = gameObject.GetComponent<ModIOTermsOfUse_v2>();
				if (component != null)
				{
					CustomMapManager.DisableTeleportHUD();
					gameObject.SetActive(value: true);
					return await component.ShowTerms();
				}
				ModioLog.Error?.Log("[ModIOManager::ShowModIOTermsOfUse] TermsOfUsePrefab doesn't contain a ModIOTermsOfUse component!");
				return new Error(ErrorCode.NOT_INITIALIZED, "ModIOManager property 'ModIOTermsOfUsePrefab' object is missing the 'ModIOTermsOfUse_v2' script component.");
			}
			ModioLog.Error?.Log("[ModIOManager::ShowModIOTermsOfUse] Failed to create termsOfUseObject!");
			return new Error(ErrorCode.UNKNOWN, "ModIOManager failed to instantiate the 'ModIOTermsOfUsePrefab'.");
		}
		ModioLog.Error?.Log("[ModIOManager::ShowModIOTermsOfUse] ModIOTermsOfUsePrefab is not set!");
		return new Error(ErrorCode.UNKNOWN, "ModIOManager property 'ModIOTermsOfUsePrefab' is NULL!");
	}

	private void OnModIOTermsOfUseAcknowledged(bool accepted)
	{
		if (accepted)
		{
			CustomMapManager.RequestEnableTeleportHUD(enteringVirtualStump: true);
			modIOTermsAcknowledgedCallback?.Invoke(ModIORequestResultAnd<bool>.CreateSuccessResult(payload: true));
		}
		else
		{
			modIOTermsAcknowledgedCallback?.Invoke(ModIORequestResultAnd<bool>.CreateFailureResult("MOD.IO TERMS OF USE HAVE NOT BEEN ACCEPTED. YOU MUST ACCEPT THE MOD.IO TERMS OF USE TO LOGIN WITH YOUR PLATFORM CREDENTIALS OR YOU CAN LOGIN WITH AN EXISTING MOD.IO ACCOUNT BY PRESSING THE 'LINK MOD.IO ACCOUNT' BUTTON AND FOLLOWING THE INSTRUCTIONS."));
		}
		modIOTermsAcknowledgedCallback = null;
	}

	private static void EnableModManagement()
	{
		if (!modManagementEnabled)
		{
			ModInstallationManagement.ManagementEvents += HandleModManagementEvent;
			ModInstallationManagement.Activate();
			modManagementEnabled = true;
			ModioLog.Verbose?.Log("[ModIOManager::EnableModManagement] Mod Management enabled.");
		}
	}

	private static void DisableModManagement()
	{
		if (modManagementEnabled)
		{
			ModioLog.Verbose?.Log("[ModIOManager::EnableModManagement] Mod Management disabled!");
			ModInstallationManagement.ManagementEvents -= HandleModManagementEvent;
			ModInstallationManagement.Deactivate(cancelCurrentJob: false);
			modManagementEnabled = false;
		}
	}

	private static void HandleModManagementEvent(Mod mod, Modfile modfile, ModInstallationManagement.OperationType jobType, ModInstallationManagement.OperationPhase jobPhase)
	{
		ModioLog.Verbose?.Log("[ModIOManager::HandleModManagementEvent] Mod " + mod.Id.ToString() + " | FileState: " + $"{modfile.State.ToString()} | JobType: {jobType} | JobPhase: {jobPhase}");
		try
		{
			if ((jobType == ModInstallationManagement.OperationType.Install || jobType == ModInstallationManagement.OperationType.Download) && jobPhase == ModInstallationManagement.OperationPhase.Completed && modfile.State == ModFileState.Installed)
			{
				outdatedModCMSVersions.Remove(mod.Id);
				IsModOutdated(mod);
			}
			if (jobPhase == ModInstallationManagement.OperationPhase.Started && (jobType == ModInstallationManagement.OperationType.Download || jobType == ModInstallationManagement.OperationType.Update || jobType == ModInstallationManagement.OperationType.Uninstall))
			{
				outdatedModCMSVersions.Remove(mod.Id);
			}
		}
		catch (Exception arg)
		{
			ModioLog.Error?.Log($"[ModIOManager::HandleModManagementEvent] Exception: {arg}");
		}
		OnModManagementEvent?.Invoke(mod, modfile, jobType, jobPhase);
	}

	public static async Task RefreshModCache()
	{
		if (refreshingModCache)
		{
			restartRefreshModCache = true;
			return;
		}
		refreshingModCache = true;
		OnModIOCacheRefreshing?.Invoke();
		restartRefreshModCache = true;
		while (restartRefreshModCache)
		{
			restartRefreshModCache = false;
			await Mod.RefreshPotentiallyHiddenCachedMods();
		}
		refreshingModCache = false;
		OnModIOCacheRefreshed?.Invoke();
	}

	public static bool IsRefreshing()
	{
		return refreshingModCache;
	}

	public static async Task<(bool, int)> IsModOutdated(ModId modId)
	{
		if (!hasInstance)
		{
			return (false, -1);
		}
		if (outdatedModCMSVersions.TryGetValue(modId, out var value))
		{
			return (true, value);
		}
		var (error, mod) = await GetMod(modId);
		if ((bool)error)
		{
			ModioLog.Error?.Log($"[ModIOManager::IsModOutdated] Failed to retrieve mod: {error}");
			return (false, -1);
		}
		return IsModOutdated(mod);
	}

	public static (bool, int) IsModOutdated(Mod mod)
	{
		if (outdatedModCMSVersions.TryGetValue(mod.Id, out var value))
		{
			return (true, value);
		}
		if (mod.File != null)
		{
			if (mod.File.State == ModFileState.Installed)
			{
				var (item, item2) = IsInstalledModOutdated(mod);
				return (item, item2);
			}
			ModioLog.Error?.Log("[ModIOManager::IsModOutdated] Mod File for " + mod.Name + " is not installed. " + $"State: {mod.File.State}.");
		}
		else
		{
			ModioLog.Error?.Log("[ModIOManager::IsModOutdated] Mod File for " + mod.Name + " is null.");
		}
		return (false, -1);
	}

	public static void SaveFavoriteMods()
	{
		if (!initialized || !modManagementEnabled)
		{
			return;
		}
		try
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(ModIODirectory);
			if (!directoryInfo.Exists)
			{
				ModioLog.Error?.Log("[ModIOManager::SaveFavoriteMods] ModIO Directory for GorillaTag does not exist!");
				return;
			}
			long[] array = new long[favoriteMods.Count];
			int num = 0;
			foreach (KeyValuePair<ModId, Mod> favoriteMod in favoriteMods)
			{
				array[num++] = favoriteMod.Key;
			}
			string contents = JsonConvert.SerializeObject(array);
			File.WriteAllText(Path.Join(directoryInfo.FullName, "favoriteMods.json"), contents);
		}
		catch (Exception)
		{
		}
	}

	public static async Task<(Error error, List<Mod> favoriteMods)> GetFavoriteMods(bool forceRefresh = false)
	{
		if (!initialized || !modManagementEnabled)
		{
			return (error: new Error(ErrorCode.NOT_INITIALIZED), favoriteMods: null);
		}
		if (forceRefresh)
		{
			favoriteModsLoaded = false;
			favoriteMods.Clear();
		}
		if (favoriteModsLoaded)
		{
			return (error: Error.None, favoriteMods: favoriteMods.Values.ToList());
		}
		while (ModInstallationManagement.IsRunning)
		{
			await Task.Yield();
		}
		try
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(ModIODirectory);
			if (!directoryInfo.Exists)
			{
				GTDev.LogWarning("ModIOManager::GetFavoriteMods Directory " + directoryInfo.ToString() + " does not exist");
				favoriteModsLoaded = true;
				return (error: new Error(ErrorCode.FILE_NOT_FOUND), favoriteMods: favoriteMods.Values.ToList());
			}
			FileInfo[] files = directoryInfo.GetFiles("favoriteMods.json");
			if (files.Length == 0)
			{
				GTDev.LogWarning("ModIOManager::GetFavoriteMods could not find file " + ModIODirectory + "favoriteMods.json");
				favoriteModsLoaded = true;
				return (error: new Error(ErrorCode.FILE_NOT_FOUND), favoriteMods: favoriteMods.Values.ToList());
			}
			var (error, collection) = await GetMods(JsonConvert.DeserializeObject<long[]>(File.ReadAllText(files[0].FullName)), forceRefresh);
			if (!error)
			{
				foreach (Mod item in collection)
				{
					favoriteMods[item.Id] = item;
				}
			}
			favoriteModsLoaded = true;
			return (error: error, favoriteMods: favoriteMods.Values.ToList());
		}
		catch (Exception msg)
		{
			GTDev.LogError(msg);
			favoriteModsLoaded = true;
			return (error: new Error(ErrorCode.READ_ERROR), favoriteMods: favoriteMods.Values.ToList());
		}
	}

	public static async Task<Error> AddFavorite(ModId modId, Action<Error> callback = null)
	{
		if (favoriteMods.ContainsKey(modId))
		{
			return new Error(ErrorCode.UNKNOWN, "MOD ALREADY FAVORITED");
		}
		var (error, value) = await GetMod(modId);
		if (!error)
		{
			favoriteMods.Add(modId, value);
			SaveFavoriteMods();
		}
		callback?.Invoke(error);
		return error;
	}

	public static Error RemoveFavorite(ModId modId)
	{
		if (!favoriteMods.ContainsKey(modId))
		{
			return new Error(ErrorCode.UNKNOWN, "MOD NOT FAVORITED");
		}
		favoriteMods.Remove(modId);
		SaveFavoriteMods();
		return Error.None;
	}

	public static bool IsModFavorited(ModId modId)
	{
		return favoriteMods.ContainsKey(modId);
	}

	public static async Task<(Error error, Mod[] installedMods)> GetInstalledMods(bool forceRefresh = false)
	{
		if (!initialized || !modManagementEnabled)
		{
			return (error: new Error(ErrorCode.NOT_INITIALIZED), installedMods: null);
		}
		while (ModInstallationManagement.IsRunning)
		{
			await Task.Yield();
		}
		ICollection<Mod> obj = await ModInstallationManagement.GetAllInstalledMods(forceRefresh);
		List<Mod> list = new List<Mod>();
		foreach (Mod item in obj)
		{
			if (item.File.State == ModFileState.Installed)
			{
				list.AddIfNew(item);
			}
			else if (item.File.State == ModFileState.Queued && !item.File.InstallLocation.IsNullOrEmpty())
			{
				list.AddIfNew(item);
			}
		}
		return (error: Error.None, installedMods: list.ToArray());
	}

	public static bool ValidateInstalledMod(Mod mod)
	{
		if (!initialized)
		{
			return false;
		}
		return ModInstallationManagement.ValidateInstalledMod(mod);
	}

	private static (bool, int) IsInstalledModOutdated(Mod mod)
	{
		int item = -1;
		if (!hasInstance)
		{
			return (false, item);
		}
		if (mod.File == null || mod.File.State != ModFileState.Installed)
		{
			ModioLog.Message?.Log("[ModIOManager::IsInstalledModOutdated] Mod " + mod.Id.ToString() + " is not currently installed.");
			return (false, item);
		}
		try
		{
			FileInfo[] files = new DirectoryInfo(mod.File.InstallLocation).GetFiles("package.json");
			if (files.Length == 0)
			{
				ModioLog.Error?.Log("[ModIOManager::IsInstalledModOutdated] Directory (" + mod.File.InstallLocation + ") for mod " + mod.Name + " does not contain a package.json file!");
			}
			if (files.Length > 1)
			{
				ModioLog.Warning?.Log("[ModIOManager::IsInstalledModOutdated] Directory (" + mod.File.InstallLocation + ") for mod " + mod.Name + " contains more than one package.json file! Only the first one found will be used!");
			}
			MapPackageInfo packageInfo = CustomMapLoader.GetPackageInfo(files[0].FullName);
			if (packageInfo.customMapSupportVersion != GT_CustomMapSupportRuntime.Constants.customMapSupportVersion)
			{
				outdatedModCMSVersions.Add(mod.Id, packageInfo.customMapSupportVersion);
				return (true, packageInfo.customMapSupportVersion);
			}
		}
		catch (Exception arg)
		{
			ModioLog.Error?.Log($"[ModIOManager::IsInstalledModOutdated] Exception while reading package.json: {arg}");
			ModInstallationManagement.RefreshMod(mod);
			return (false, item);
		}
		return (false, item);
	}

	public static async Task RefreshUserProfile(Action<bool> callback = null, bool force = false)
	{
		if (!hasInstance || !IsLoggedIn())
		{
			callback?.Invoke(obj: false);
		}
		else if (refreshing && callback != null)
		{
			currentRefreshCallbacks.Add(callback);
		}
		else if (force || Mathf.Approximately(0f, lastRefreshTime) || Time.realtimeSinceStartup - lastRefreshTime >= 5f)
		{
			currentRefreshCallbacks.Add(callback);
			lastRefreshTime = Time.realtimeSinceStartup;
			refreshing = true;
			if (User.Current.IsUpdating)
			{
				ModioLog.Verbose?.Log("[ModIOManager::Refresh] Profile already updating, waiting for Sync to finish...");
				while (User.Current.IsUpdating)
				{
					await Task.Yield();
				}
			}
			else
			{
				ModioLog.Verbose?.Log("[ModIOManager::Refresh] Syncing user profile...");
				await User.Current.Sync();
			}
			refreshing = false;
			foreach (Action<bool> currentRefreshCallback in currentRefreshCallbacks)
			{
				currentRefreshCallback?.Invoke(obj: true);
			}
			currentRefreshCallbacks.Clear();
		}
		else
		{
			callback?.Invoke(obj: false);
		}
	}

	public static async Task<(Error error, ICollection<Mod> mods)> GetMods(ICollection<long> modIds, bool forceRefresh = false, Action<Error, ICollection<Mod>> callback = null)
	{
		Error error;
		if (!hasInstance)
		{
			error = new Error(ErrorCode.NOT_INITIALIZED);
			callback?.Invoke(error, null);
			return (error: error, mods: null);
		}
		ICollection<Mod> collection;
		(error, collection) = await Mod.GetMods(modIds, forceRefresh);
		if ((bool)error)
		{
			ModioLog.Error?.Log("[ModIOManager::GetMod] Failed to get requested Mods. Error: " + error.GetMessage());
			callback?.Invoke(error, null);
			return (error: error, mods: null);
		}
		callback?.Invoke(error, collection);
		return (error: Error.None, mods: collection);
	}

	public static async Task<(Error error, Mod result)> GetMod(ModId modId, bool forceUpdate = false, Action<Error, Mod> callback = null)
	{
		Error error;
		if (!hasInstance)
		{
			error = new Error(ErrorCode.NOT_INITIALIZED);
			callback?.Invoke(error, null);
			return (error: error, result: null);
		}
		Mod retrievedMod;
		(error, retrievedMod) = await Mod.GetMod(modId, forceUpdate);
		if ((bool)error)
		{
			ModioLog.Error?.Log("[ModIOManager::GetMod] Failed to get Mod " + modId.ToString() + ". Error: " + error.GetMessage());
			callback?.Invoke(error, retrievedMod);
			return (error: error, result: retrievedMod);
		}
		if (forceUpdate)
		{
			if (IsLoggedIn())
			{
				await RefreshUserProfile();
			}
			else
			{
				ModInstallationManagement.RefreshMod(retrievedMod);
			}
		}
		callback?.Invoke(error, retrievedMod);
		return (error: Error.None, result: retrievedMod);
	}

	public static async Task<(Error error, Texture2D logo)> GetModLogo(Mod mod, Action<Error, Texture2D> callback)
	{
		if (mod == null || !mod.Id.IsValid())
		{
			return (error: new Error(ErrorCode.BAD_PARAMETER), logo: null);
		}
		Error error;
		if (!hasInstance)
		{
			error = new Error(ErrorCode.NOT_INITIALIZED);
			callback?.Invoke(error, null);
			return (error: error, logo: null);
		}
		if (mod.Logo == null)
		{
			error = new Error(ErrorCode.UNKNOWN, "Mod Logo is null!");
			callback?.Invoke(error, null);
			return (error: error, logo: null);
		}
		ModioLog.Verbose?.Log("[ModIOManager::GetModLogo] Getting logo for Mod " + mod.Id.ToString() + "...");
		Texture2D texture2D;
		(error, texture2D) = await mod.Logo.DownloadAsTexture2D(Mod.LogoResolution.X320_Y180);
		if ((bool)error)
		{
			ModioLog.Error?.Log("[ModIOManager::GetModLogo] Failed to download logo for Mod " + mod.Id.ToString() + ". Error: " + error.GetMessage());
		}
		callback?.Invoke(error, texture2D);
		return (error: error, logo: texture2D);
	}

	public static async Task<(Error error, ModioPage<Mod> modsPage)> GetMods(ModioAPI.Mods.GetModsFilter searchFilter)
	{
		if (!hasInstance)
		{
			Error item = new Error(ErrorCode.NOT_INITIALIZED);
			return (error: item, modsPage: null);
		}
		return await Mod.GetMods(searchFilter);
	}

	private static void ModIOUserChanged(User currentUser)
	{
		ModioLog.Verbose?.Log("[ModIOManager::ModIOUserChanged] CurrentUser: " + ((currentUser == null) ? "NULL" : currentUser.Profile.Username));
		OnModIOUserChanged?.Invoke(currentUser);
	}

	private static void ModIOUserSyncComplete()
	{
		ModioLog.Verbose?.Log("[ModIOManager::ModIOUserSyncComplete] Refreshing mod cache...");
		RefreshModCache();
	}

	public static bool IsLoggedIn()
	{
		if (User.Current != null)
		{
			return User.Current.IsAuthenticated;
		}
		return false;
	}

	public static bool IsLoggingIn()
	{
		return loggingIn;
	}

	public static bool IsLoggingOut()
	{
		return loggingOut;
	}

	public static string GetCurrentUsername()
	{
		if (!IsLoggedIn())
		{
			return "";
		}
		ModioLog.Verbose?.Log("[ModIOManager::GetCurrentUsername] Username: " + User.Current.Profile.Username);
		return User.Current.Profile.Username;
	}

	public static string GetCurrentUserId()
	{
		if (!IsLoggedIn())
		{
			return "";
		}
		ModioLog.Verbose?.Log($"[ModIOManager::GetCurrentUserId] User ID: {User.Current.Profile.UserId}");
		return User.Current.Profile.UserId.ToString();
	}

	public static string GetCurrentAuthToken()
	{
		if (!IsLoggedIn())
		{
			return "";
		}
		return User.Current.Token;
	}

	public static bool IsAuthenticated(bool sendEvents = false)
	{
		if (!hasInstance)
		{
			return false;
		}
		bool isAuthenticated = User.Current.IsAuthenticated;
		if (isAuthenticated)
		{
			loggingIn = false;
			ModioLog.Verbose?.Log("[ModIOManager::IsAuthenticated] User already authenticated...");
			if (sendEvents)
			{
				OnModIOLoggedIn?.Invoke();
			}
		}
		else
		{
			try
			{
				ModioLog.Verbose?.Log("[ModIOManager::IsAuthenticated] User not authenticated");
				if (sendEvents)
				{
					OnModIOLoggedOut?.Invoke();
				}
			}
			catch (Exception arg)
			{
				ModioLog.Verbose?.Log($"[ModIOManager::IsAuthenticated] error {arg}");
			}
		}
		ModioLog.Verbose?.Log($"[ModIOManager::IsAuthenticated] returning {isAuthenticated}");
		return isAuthenticated;
	}

	public static void LogoutFromModIO()
	{
		if (hasInstance && !loggingIn && IsLoggedIn())
		{
			loggingOut = true;
			ModioLog.Verbose?.Log("[ModIOManager::LogoutFromModIO] Logging out of mod.io...");
			CancelExternalAuthentication();
			loggingIn = false;
			User.DeleteUserData();
			ModioLog.Verbose?.Log("[ModIOManager::LogoutFromModIO] User data deleted...");
			PlayerPrefs.SetInt("modIOLassSuccessfulAuthMethod", ModIOAuthMethod.Invalid.GetIndex());
			ModioLog.Verbose?.Log("[ModIOManager::LogoutFromModIO] User fully logged out.");
			loggingOut = false;
			OnModIOLoggedOut?.Invoke();
			RefreshModCache();
		}
	}

	public static void SetAccountLinkPrompter(IWssAuthPrompter prompter)
	{
		if (accountLinkingAuthService != null)
		{
			accountLinkingAuthService.SetPrompter(prompter);
		}
	}

	public static async Task<Error> RequestAccountLinkCode()
	{
		if (!hasInstance)
		{
			return new Error(ErrorCode.NOT_INITIALIZED);
		}
		if (loggingIn)
		{
			return new Error(ErrorCode.USER_AUTHENTICATION_IN_PROGRESS);
		}
		if (IsLoggedIn())
		{
			return new Error(ErrorCode.ALREADY_AUTHENTICATED);
		}
		loggingIn = true;
		ModioLog.Verbose?.Log("[ModIOManager::RequestAccountLinkCode] Requesting Link Code...");
		Error obj = await accountLinkingAuthService.Authenticate(displayedTerms: false);
		if (!obj)
		{
			ModioLog.Verbose?.Log("[ModIOManager::RequestAccountLinkCode] Account linked successfully!");
			PlayerPrefs.SetInt("modIOLassSuccessfulAuthMethod", ModIOAuthMethod.LinkedAccount.GetIndex());
		}
		OnAuthenticationComplete(obj);
		return obj;
	}

	public static void CancelExternalAuthentication()
	{
		if (hasInstance && accountLinkingAuthService != null && accountLinkingAuthService.InProgress())
		{
			ModioLog.Verbose?.Log("[ModIOManager::CancelExternalAuthentication] Cancelling Mod.io Account Linking process...");
			accountLinkingAuthService.Cancel();
		}
	}

	public static async Task<Error> RequestPlatformLogin()
	{
		if (!hasInstance)
		{
			ModioLog.Error?.Log("[ModIOManager::RequestPlatformLogin] has no instance");
			return new Error(ErrorCode.NOT_INITIALIZED, "ModIOManager has not been initialized!");
		}
		if (loggingIn)
		{
			ModioLog.Message?.Log("[ModIOManager::RequestPlatformLogin] is already logging in");
			return new Error(ErrorCode.USER_AUTHENTICATION_IN_PROGRESS);
		}
		loggingIn = true;
		ModioLog.Verbose?.Log("[ModIOManager::RequestPlatformLogin] calling IsAuthenticated");
		if (IsAuthenticated(sendEvents: true))
		{
			ModioLog.Verbose?.Log("[ModIOManager::RequestPlatformLogin] User already authenticated!");
			return Error.None;
		}
		ModioLog.Verbose?.Log("[ModIOManager::RequestPlatformLogin] calling InitializePlatformLogin");
		Error error = new Error(ErrorCode.NONE);
		try
		{
			error = await instance.InitiatePlatformLogin();
		}
		catch (Exception arg)
		{
			ModioLog.Error?.Log($"[ModIOManager::RequestPlatformLogin] exception initializing platform login {arg}");
		}
		return error;
	}

	private async Task<Error> InitiatePlatformLogin()
	{
		OnModIOLoginStarted?.Invoke();
		ModioLog.Verbose?.Log("[ModIOManager::InitiatePlatformLogin] Attempting to login using platform credentials...");
		bool flag;
		bool flag2;
		Error error;
		(error, flag, flag2) = await HasAcceptedLatestTerms();
		if ((bool)error)
		{
			loggingIn = false;
			OnModIOLoginFailed?.Invoke($"FAILED TO LOGIN TO MOD.IO:\nFAILED TO CHECK TERMS OF USE ACCEPTANCE STATUS: {error}");
			return error;
		}
		if (!flag || !flag2)
		{
			error = await ShowModIOTermsOfUse();
			if ((bool)error)
			{
				OnAuthenticationComplete(error);
				return error;
			}
			Agreement agreement;
			(error, agreement) = await Agreement.GetAgreement(AgreementType.TermsOfUse);
			if (!error)
			{
				PlayerPrefs.SetString("modIOAcceptedTermsOfUseId", agreement.Id.ToString());
			}
			Agreement agreement2;
			(error, agreement2) = await Agreement.GetAgreement(AgreementType.PrivacyPolicy);
			if (!error)
			{
				PlayerPrefs.SetString("modIOAcceptedPrivacyPolicyId", agreement2.Id.ToString());
			}
		}
		return await ContinuePlatformLogin();
	}

	private async Task<Error> ContinuePlatformLogin()
	{
		if (SteamManager.Initialized)
		{
			steamAuthService.SetCredentialProvider(this);
			Error error = await steamAuthService.Authenticate(displayedTerms: true);
			if ((bool)error)
			{
				ModioLog.Error?.Log($"[ModIOManager::ContinuePlatformLogin] Failed to authenticate via Steam: {error}");
				OnAuthenticationComplete(error);
				return error;
			}
			ModioLog.Verbose?.Log("[ModIOManager::ContinuePlatformLogin] Successfully authenticated via Steam!");
			PlayerPrefs.SetInt("modIOLassSuccessfulAuthMethod", ModIOAuthMethod.Steam.GetIndex());
			OnAuthenticationComplete(Error.None);
			return Error.None;
		}
		ModioLog.Error?.Log("[ModIOManager::ContinuePlatformLogin] Steam enabled but not initialized...");
		OnAuthenticationComplete(new Error(ErrorCode.NOT_INITIALIZED, "STEAM IS ENABLED BUT NOT INITIALIZED."));
		return new Error(ErrorCode.NOT_INITIALIZED, "Steam is enabled, but has not been initialized.");
	}

	public void RequestEncryptedAppTicket(Action<bool, string> callback)
	{
		if (requestEncryptedAppTicketCallback != null)
		{
			ModioLog.Warning?.Log("[ModIOManager::RequestEncryptedAppTicket] Callback already set, Encrypted App Ticket request already in progress!");
			callback?.Invoke(arg1: false, "AN ENCRYPTED APP TICKET REQUEST IS ALREADY IN PROGRESS");
			return;
		}
		requestEncryptedAppTicketCallback = callback;
		if (requestEncryptedAppTicketResponse == null)
		{
			requestEncryptedAppTicketResponse = CallResult<EncryptedAppTicketResponse_t>.Create(OnRequestEncryptedAppTicketFinished);
		}
		ModioLog.Verbose?.Log("[ModIOManager::RequestEncryptedAppTicket] Requesting Steam Encrypted App Ticket...");
		SteamAPICall_t hAPICall = SteamUser.RequestEncryptedAppTicket(null, 0);
		requestEncryptedAppTicketResponse.Set(hAPICall);
	}

	private void OnRequestEncryptedAppTicketFinished(EncryptedAppTicketResponse_t response, bool bIOFailure)
	{
		if (bIOFailure)
		{
			ModioLog.Error?.Log("Failed to retrieve EncryptedAppTicket due to a Steam API IO failure...");
			requestEncryptedAppTicketCallback?.Invoke(arg1: false, "FAILED TO RETRIEVE 'EncryptedAppTicket' DUE TO A STEAM API IO FAILURE.");
			requestEncryptedAppTicketCallback = null;
			return;
		}
		switch (response.m_eResult)
		{
		case EResult.k_EResultOK:
			if (!SteamUser.GetEncryptedAppTicket(ticketBlob, ticketBlob.Length, out ticketSize))
			{
				ModioLog.Error?.Log("[ModIOManager::OnRequestEncryptedAppTicketFinished] Failed to retrieve " + $"EncryptedAppTicket! Needed size: {ticketSize}");
				requestEncryptedAppTicketCallback?.Invoke(arg1: false, "FAILED TO RETRIEVE 'EncryptedAppTicket'.");
				requestEncryptedAppTicketCallback = null;
			}
			else
			{
				Array.Resize(ref ticketBlob, (int)ticketSize);
				string text = Convert.ToBase64String(ticketBlob);
				ModioLog.Verbose?.Log("[ModIOManager::OnRequestEncryptedAppTicketFinished] Successfully retrieved Steam Encrypted App Ticket: " + text);
				requestEncryptedAppTicketCallback?.Invoke(arg1: true, text);
				requestEncryptedAppTicketCallback = null;
			}
			break;
		case EResult.k_EResultNoConnection:
			ModioLog.Error?.Log("[ModIOManager::OnRequestEncryptedAppTicketFinished] Not connected to steam.");
			requestEncryptedAppTicketCallback?.Invoke(arg1: false, "NOT CONNECTED TO STEAM.");
			requestEncryptedAppTicketCallback = null;
			break;
		case EResult.k_EResultDuplicateRequest:
			ModioLog.Error?.Log("[ModIOManager::OnRequestEncryptedAppTicketFinished] There is already a pending EncryptedAppTicket request.");
			requestEncryptedAppTicketCallback?.Invoke(arg1: false, "THERE IS ALREADY AN 'EncryptedAppTicket' REQUEST IN PROGRESS.");
			requestEncryptedAppTicketCallback = null;
			break;
		case EResult.k_EResultLimitExceeded:
			ModioLog.Error?.Log("[ModIOManager::OnRequestEncryptedAppTicketFinished] Rate Limit exceeded, this function should not be called more than once per minute.");
			requestEncryptedAppTicketCallback?.Invoke(arg1: false, "RATE LIMIT EXCEEDED, CAN ONLY REQUEST ONE 'EncryptedAppTicket' PER MINUTE.");
			requestEncryptedAppTicketCallback = null;
			break;
		default:
			ModioLog.Error?.Log($"[ModIOManager::OnRequestEncryptedAppTicketFinished] Unknown Error: {response.m_eResult}");
			requestEncryptedAppTicketCallback?.Invoke(arg1: false, $"{response.m_eResult}");
			requestEncryptedAppTicketCallback = null;
			break;
		}
	}

	public async Task<(Error, string)> GetOculusUserId()
	{
		return (Error.Unknown, "OCULUS is not enabled for this build");
	}

	public async Task<string> GetOculusAccessToken()
	{
		return "";
	}

	public async Task<string> GetOculusUserProof()
	{
		return "";
	}

	public string GetOculusDevice()
	{
		return "";
	}

	private static void OnAuthenticationComplete(Error error)
	{
		loggingIn = false;
		if ((bool)error)
		{
			OnModIOLoginFailed?.Invoke($"FAILED TO LOGIN TO MOD.IO: {error}");
		}
		else
		{
			OnModIOLoggedIn?.Invoke();
		}
	}

	public static ModIOAuthMethod GetLastAuthMethod()
	{
		int num = PlayerPrefs.GetInt("modIOLassSuccessfulAuthMethod", -1);
		if (num == -1)
		{
			return ModIOAuthMethod.Invalid;
		}
		return (ModIOAuthMethod)num;
	}

	public static async Task<(Error, Mod[])> GetSubscribedMods()
	{
		if (!IsLoggedIn())
		{
			return (new Error(ErrorCode.USER_NOT_AUTHENTICATED), null);
		}
		if (User.Current.IsUpdating)
		{
			while (User.Current.IsUpdating)
			{
				await Task.Yield();
			}
		}
		return (Error.None, User.Current.ModRepository.GetSubscribed().ToArray());
	}

	public static async Task<Error> SubscribeToMod(ModId modId, Action<Error> callback)
	{
		if (!IsLoggedIn())
		{
			ModioLog.Error?.Log("[ModIOManager::SubscribeToMod] Called while not logged in!");
			return new Error(ErrorCode.USER_NOT_AUTHENTICATED);
		}
		if (User.Current.IsUpdating)
		{
			ModioLog.Verbose?.Log("[ModIOManager::SubscribeToMod] User currently updating... waiting");
			while (User.Current.IsUpdating)
			{
				await Task.Yield();
			}
		}
		if (User.Current.ModRepository.IsSubscribed(modId))
		{
			ModioLog.Message?.Log($"[ModIOManager::SubscribeToMod] Already subscribed to Mod {modId}");
			callback?.Invoke(Error.None);
			return Error.None;
		}
		Mod mod;
		Error error;
		(error, mod) = await Mod.GetMod(modId);
		if ((bool)error)
		{
			ModioLog.Error?.Log($"[ModIOManager::SubscribeToMod] Failed to retrieve mod details for Mod {modId}: " + error.GetMessage());
			callback?.Invoke(error);
			return error;
		}
		ModioLog.Verbose?.Log($"[ModIOManager::SubscribeToMod] Subscribing to mod with ID: {modId}");
		error = await mod.Subscribe();
		if ((bool)error)
		{
			ModioLog.Error?.Log($"[ModIOManager::SubscribeToMod] Failed to subscribe to Mod {modId}: " + error.GetMessage());
		}
		callback?.Invoke(error);
		return error;
	}

	public static async Task<Error> UnsubscribeFromMod(ModId modId, Action<Error> callback)
	{
		if (!IsLoggedIn())
		{
			ModioLog.Error?.Log("[ModIOManager::UnsubscribeFromMod] Called while not logged in!");
			return new Error(ErrorCode.USER_NOT_AUTHENTICATED);
		}
		if (User.Current.IsUpdating)
		{
			ModioLog.Verbose?.Log("[ModIOManager::UnsubscribeFromMod] User currently updating... waiting");
			while (User.Current.IsUpdating)
			{
				await Task.Yield();
			}
		}
		if (!User.Current.ModRepository.IsSubscribed(modId))
		{
			ModioLog.Message?.Log($"[ModIOManager::UnsubscribeFromMod] Not currently subscribed to Mod {modId}");
			callback?.Invoke(Error.None);
			return Error.None;
		}
		Mod mod;
		Error error;
		(error, mod) = await Mod.GetMod(modId);
		if ((bool)error)
		{
			ModioLog.Error?.Log("[ModIOManager::UnsubscribeFromMod] Failed to retrieve mod details for Mod " + $"{modId}: {error.GetMessage()}");
			callback?.Invoke(error);
			return error;
		}
		ModioLog.Verbose?.Log("[ModIOManager::UnsubscribeFromMod] Unsubscribing from Mod " + modId);
		error = await mod.Unsubscribe();
		if ((bool)error)
		{
			ModioLog.Error?.Log($"[ModIOManager::UnsubscribeToMod] Failed to unsubscribe from Mod {modId}: " + error.GetMessage());
		}
		callback?.Invoke(error);
		return error;
	}

	public static async Task<(bool, ModFileState)> GetSubscribedModStatus(ModId modId)
	{
		if (!hasInstance)
		{
			return (false, ModFileState.None);
		}
		if (!IsLoggedIn())
		{
			return (false, ModFileState.None);
		}
		if (User.Current.IsUpdating)
		{
			while (User.Current.IsUpdating)
			{
				await Task.Yield();
			}
		}
		if (!User.Current.ModRepository.IsSubscribed(modId))
		{
			return (false, ModFileState.None);
		}
		var (error, mod) = await Mod.GetMod(modId);
		if ((bool)error)
		{
			ModioLog.Error?.Log("[ModIOManager::GetSubscribedModStatus] Failed to retrieve Mod " + modId.ToString() + "'s status: " + error.GetMessage());
			return (true, ModFileState.None);
		}
		return (true, mod.File.State);
	}

	public static async Task<(bool, Mod)> GetSubscribedModProfile(ModId modId, Action<bool, Mod> callback = null)
	{
		if (!hasInstance)
		{
			callback?.Invoke(arg1: false, null);
			return (false, null);
		}
		if (!IsLoggedIn())
		{
			callback?.Invoke(arg1: false, null);
			return (false, null);
		}
		if (User.Current.IsUpdating)
		{
			ModioLog.Verbose?.Log("[ModIOManager::GetSubscribedModProfile] Subscriptions currently updating, waiting for Sync to finish...");
			while (User.Current.IsUpdating)
			{
				await Task.Yield();
			}
		}
		ModioLog.Verbose?.Log("[ModIOManager::GetSubscribedModProfile] Checking Subscribed Mod list for Mod " + modId);
		foreach (Mod item in User.Current.ModRepository.GetSubscribed())
		{
			if (item.Id.Equals(modId))
			{
				ModioLog.Verbose?.Log("[ModIOManager::GetSubscribedModProfile] Found Mod " + modId.ToString() + " in Subscribed Mod list.");
				callback?.Invoke(arg1: true, item);
				return (true, item);
			}
		}
		ModioLog.Verbose?.Log("[ModIOManager::GetSubscribedModProfile] Mod " + modId.ToString() + " not present in Subscribed Mod list.");
		callback?.Invoke(arg1: false, null);
		return (false, null);
	}

	public static async Task<ModFileState> GetModStatus(ModId modId)
	{
		if (!hasInstance)
		{
			return ModFileState.None;
		}
		var (error, mod) = await Mod.GetMod(modId);
		if ((bool)error)
		{
			ModioLog.Error?.Log("[ModIOManager::GetModStatus] Failed to retrieve Mod " + modId.ToString() + "'s status: " + error.GetMessage());
			return ModFileState.None;
		}
		return mod.File.State;
	}

	public static async Task<bool> DownloadMod(ModId modId, Action<bool> callback = null)
	{
		if (!hasInstance)
		{
			return false;
		}
		bool flag = await ModInstallationManagement.DownloadAndInstallMod(modId);
		callback?.Invoke(flag);
		return flag;
	}

	private void OnJoinedRoom()
	{
		if (NetworkSystem.Instance.RoomName.Contains(GorillaComputer.instance.VStumpRoomPrepend) && !GorillaComputer.instance.IsPlayerInVirtualStump() && !CustomMapManager.IsLocalPlayerInVirtualStump())
		{
			Debug.LogError("[ModIOManager::OnJoinedRoom] Player joined @ room while not in the VStump! Leaving the room...");
			NetworkSystem.Instance.ReturnToSinglePlayer();
		}
	}

	public static bool TryGetNewMapsModId(out ModId newMapsModId)
	{
		newMapsModId = ModId.Null;
		if (!hasInstance)
		{
			return false;
		}
		newMapsModId = new ModId(instance.newMapsModId);
		return true;
	}

	public static IEnumerator AssociateMothershipAndModIOAccounts(AssociateMotherhsipAndModIOAccountsRequest data, Action<AssociateMotherhsipAndModIOAccountsResponse> callback)
	{
		UnityWebRequest request = new UnityWebRequest(PlayFabAuthenticatorSettings.AuthApiBaseUrl + "/api/AssociatePlayFabAndModIO", "POST");
		string s = JsonUtility.ToJson(data);
		byte[] bytes = Encoding.UTF8.GetBytes(s);
		bool retry = false;
		request.uploadHandler = new UploadHandlerRaw(bytes);
		request.downloadHandler = new DownloadHandlerBuffer();
		request.SetRequestHeader("Content-Type", "application/json");
		request.timeout = 15;
		yield return request.SendWebRequest();
		if (request.result != UnityWebRequest.Result.ConnectionError && request.result != UnityWebRequest.Result.ProtocolError)
		{
			AssociateMotherhsipAndModIOAccountsResponse obj = JsonUtility.FromJson<AssociateMotherhsipAndModIOAccountsResponse>(request.downloadHandler.text);
			callback(obj);
		}
		else if (request.result == UnityWebRequest.Result.ProtocolError && request.responseCode != 400)
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
		if (retry)
		{
			if (currentAssociationRetries < associationMaxRetries)
			{
				int num = (int)Mathf.Pow(2f, currentAssociationRetries + 1);
				Debug.LogWarning($"Retrying Account Association... Retry attempt #{currentAssociationRetries + 1}, waiting for {num} seconds");
				currentAssociationRetries++;
				yield return new WaitForSecondsRealtime(num);
				AssociateMothershipAndModIOAccounts(data, callback);
			}
			else
			{
				Debug.LogError("Maximum retries attempted. Please check your network connection.");
				callback(null);
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GorillaTagScripts.VirtualStumpCustomMaps;
using GorillaTagScripts.VirtualStumpCustomMaps.UI;
using Modio;
using Modio.Mods;
using Modio.Users;
using TMPro;
using UnityEngine;

public class CustomMapsDetailsScreen : CustomMapsTerminalScreen
{
	[SerializeField]
	private SpriteRenderer mapScreenshotImage;

	[SerializeField]
	private Sprite hiddenMapLogo;

	[SerializeField]
	private TMP_Text loadingText;

	[SerializeField]
	private TMP_Text modNameText;

	[SerializeField]
	private TMP_Text modCreatorLabelText;

	[SerializeField]
	private TMP_Text modCreatorText;

	[SerializeField]
	private TMP_Text modDescriptionText;

	[SerializeField]
	private TMP_Text modStatusText;

	[SerializeField]
	private TMP_Text modStatusLabelText;

	[SerializeField]
	private TMP_Text modSubscriptionStatusText;

	[SerializeField]
	private TMP_Text loadingMapLabelText;

	[SerializeField]
	private TMP_Text loadingMapMessageText;

	[SerializeField]
	private TMP_Text hiddenRoomMapText;

	[SerializeField]
	private TMP_Text mapReadyText;

	[SerializeField]
	private TMP_Text unloadPromptText;

	[SerializeField]
	private TMP_Text errorText;

	[SerializeField]
	private TMP_Text outdatedText;

	[SerializeField]
	private CustomMapsScreenButton subscriptionToggleButton;

	[SerializeField]
	private CustomMapsScreenButton favoriteToggleButton;

	[SerializeField]
	private CustomMapsScreenButton rateUpButton;

	[SerializeField]
	private CustomMapsScreenButton rateDownButton;

	[SerializeField]
	private CustomMapsScreenButton loadButton;

	[SerializeField]
	private CustomMapsScreenButton deleteButton;

	[SerializeField]
	private string modAvailableString = "AVAILABLE";

	[SerializeField]
	private string mapAutoDownloadingString = "DOWNLOADING...";

	[SerializeField]
	private string mapDownloadQueuedString = "DOWNLOAD QUEUED";

	[SerializeField]
	private string mapLoadingString = "LOADING:";

	[SerializeField]
	private string mapUnloadingString = "UNLOADING...";

	[SerializeField]
	private string mapLoadingErrorString = "ERROR:";

	[SerializeField]
	private string mapLoadingErrorDriverString = "PRESS THE 'BACK' BUTTON TO TRY AGAIN";

	[SerializeField]
	private string mapLoadingErrorNonDriverString = "LEAVE AND REJOIN THE VIRTUAL STUMP TO TRY AGAIN";

	[SerializeField]
	private string mapLoadingErrorInvalidModFile = "INSTALL FAILED DUE TO INVALID MAP FILE";

	[SerializeField]
	private VirtualStumpSerializer networkObject;

	public static Dictionary<ModFileState, string> modStatusStrings = new Dictionary<ModFileState, string>
	{
		{
			ModFileState.Installed,
			"READY"
		},
		{
			ModFileState.Queued,
			"QUEUED"
		},
		{
			ModFileState.Downloading,
			"DOWNLOADING"
		},
		{
			ModFileState.Installing,
			"INSTALLING"
		},
		{
			ModFileState.Uninstalling,
			"UNINSTALLING"
		},
		{
			ModFileState.Updating,
			"UPDATING"
		},
		{
			ModFileState.FileOperationFailed,
			"ERROR"
		},
		{
			ModFileState.None,
			"AVAILABLE"
		}
	};

	[SerializeField]
	private string mapNotDownloadedString = "NOT DOWNLOADED";

	[SerializeField]
	private string mapNeedsUpdateString = "NEEDS UPDATE";

	[SerializeField]
	private string subscribeString = "SUBSCRIBE";

	[SerializeField]
	private string unsubscribeString = "UNSUBSCRIBE";

	[SerializeField]
	private string subscribedStatusString = "SUBSCRIBED";

	[SerializeField]
	private string unsubscribedStatusString = "NOT SUBSCRIBED";

	[SerializeField]
	private string loadMapString = "LOAD";

	[SerializeField]
	private string downloadMapString = "DOWNLOAD";

	[SerializeField]
	private string updateMapString = "UPDATE";

	[SerializeField]
	private string hiddenMapTitle = "HIDDEN MAP";

	[SerializeField]
	private string hiddenMapDesc = "YOU DON'T CURRENTLY HAVE ACCESS TO THIS HIDDEN MAP.\nCHECK THAT YOU'RE LOGGED IN TO THE CORRECT MOD.IO ACCOUNT.";

	private const float LOGO_WIDTH = 320f;

	private const float LOGO_HEIGHT = 180f;

	public long pendingModId;

	private bool hasModProfile;

	private bool mapLoadError;

	private bool isFavorite;

	public Mod currentMapMod { get; private set; }

	public override void Initialize()
	{
	}

	public override void Show()
	{
		base.Show();
		ModIOManager.OnModIOLoggedIn.RemoveListener(OnModIOLoggedIn);
		ModIOManager.OnModIOLoggedIn.AddListener(OnModIOLoggedIn);
		ModIOManager.OnModIOLoggedOut.RemoveListener(OnModIOLoggedOut);
		ModIOManager.OnModIOLoggedOut.AddListener(OnModIOLoggedOut);
		ModIOManager.OnModIOUserChanged.RemoveListener(OnModIOUserChanged);
		ModIOManager.OnModIOUserChanged.AddListener(OnModIOUserChanged);
		ModIOManager.OnModManagementEvent.RemoveListener(HandleModManagementEvent);
		ModIOManager.OnModManagementEvent.AddListener(HandleModManagementEvent);
		CustomMapManager.OnMapLoadStatusChanged.RemoveListener(OnMapLoadProgress);
		CustomMapManager.OnMapLoadStatusChanged.AddListener(OnMapLoadProgress);
		CustomMapManager.OnMapLoadComplete.RemoveListener(OnMapLoadComplete);
		CustomMapManager.OnMapLoadComplete.AddListener(OnMapLoadComplete);
		CustomMapManager.OnRoomMapChanged.RemoveListener(OnRoomMapChanged);
		CustomMapManager.OnRoomMapChanged.AddListener(OnRoomMapChanged);
		CustomMapManager.OnMapUnloadComplete.RemoveListener(OnMapUnloaded);
		CustomMapManager.OnMapUnloadComplete.AddListener(OnMapUnloaded);
		if (!ModIOManager.IsLoggedIn())
		{
			subscriptionToggleButton.gameObject.SetActive(value: false);
		}
		deleteButton.gameObject.SetActive(value: false);
		ResetToDefaultView();
	}

	public override void Hide()
	{
		base.Hide();
		ModIOManager.OnModIOLoggedIn.RemoveListener(OnModIOLoggedIn);
		ModIOManager.OnModIOLoggedOut.RemoveListener(OnModIOLoggedOut);
		ModIOManager.OnModIOUserChanged.RemoveListener(OnModIOUserChanged);
		ModIOManager.OnModManagementEvent.RemoveListener(HandleModManagementEvent);
		CustomMapManager.OnMapLoadStatusChanged.RemoveListener(OnMapLoadProgress);
		CustomMapManager.OnMapLoadComplete.RemoveListener(OnMapLoadComplete);
		CustomMapManager.OnRoomMapChanged.RemoveListener(OnRoomMapChanged);
		CustomMapManager.OnMapUnloadComplete.RemoveListener(OnMapUnloaded);
	}

	private void OnModUpdated()
	{
		ModRating currentUserRating = currentMapMod.CurrentUserRating;
		rateUpButton.SetButtonActive(currentUserRating == ModRating.Positive);
		rateDownButton.SetButtonActive(currentUserRating == ModRating.Negative);
	}

	private void OnModIOLoggedIn()
	{
		if (currentMapMod.Creator == null)
		{
			RefreshCurrentMapMod();
		}
		else if (currentMapMod.IsHidden())
		{
			UpdateMapDetails();
		}
		else
		{
			UpdateStatus();
		}
	}

	private void OnModIOLoggedOut()
	{
		if (currentMapMod.IsHidden())
		{
			UpdateMapDetails();
		}
		else
		{
			UpdateStatus();
		}
	}

	private void OnModIOUserChanged(User user)
	{
		UpdateStatus();
	}

	private void HandleModManagementEvent(Mod mod, Modfile modfile, ModInstallationManagement.OperationType jobType, ModInstallationManagement.OperationPhase jobPhase)
	{
		if (base.isActiveAndEnabled && hasModProfile && GetModId() == mod.Id)
		{
			UpdateStatus(jobPhase == ModInstallationManagement.OperationPhase.Cancelled || jobPhase == ModInstallationManagement.OperationPhase.Failed);
			if (jobPhase == ModInstallationManagement.OperationPhase.Failed)
			{
				modDescriptionText.gameObject.SetActive(value: false);
				loadingMapLabelText.text = mapLoadingErrorString;
				loadingMapLabelText.gameObject.SetActive(value: true);
				loadingMapMessageText.text = mapLoadingErrorInvalidModFile;
				loadingMapMessageText.gameObject.SetActive(value: true);
			}
		}
	}

	private void Update()
	{
		if (base.isActiveAndEnabled && GetModId().IsValid() && ModInstallationManagement.CurrentOperationOnMod != null && ModInstallationManagement.CurrentOperationOnMod.Id == GetModId() && ModInstallationManagement.CurrentOperationOnMod.File.State != ModFileState.Installed && modStatusStrings.TryGetValue(ModInstallationManagement.CurrentOperationOnMod.File.State, out var value))
		{
			float f = currentMapMod.File.FileStateProgress * 100f;
			modStatusText.text = value + $" {Mathf.RoundToInt(f)}%";
		}
	}

	public void RetrieveModFromModIO(long id, bool forceUpdate = false, Action<Error, Mod> callback = null)
	{
		if (hasModProfile && GetModId()._id == id)
		{
			UpdateMapDetails();
			return;
		}
		pendingModId = id;
		ModIOManager.GetMod(new ModId(id), forceUpdate, (callback != null) ? callback : new Action<Error, Mod>(OnProfileReceived));
	}

	public void SetModProfile(Mod mod)
	{
		if (mod.Id != ModId.Null)
		{
			pendingModId = 0L;
			currentMapMod = mod;
			hasModProfile = true;
			currentMapMod.OnModUpdated += OnModUpdated;
			isFavorite = ModIOManager.IsModFavorited(mod.Id);
			favoriteToggleButton.SetButtonActive(isFavorite);
			UpdateMapDetails();
		}
	}

	public override void PressButton(CustomMapKeyboardBinding buttonPressed)
	{
		if (Time.time < showTime + activationTime)
		{
			return;
		}
		GTDev.Log("[CustomMapsDetailsScreen::PressButton] Is Driver: " + CustomMapsTerminal.IsDriver + ", Button Pressed: " + buttonPressed);
		if (!base.isActiveAndEnabled || !CustomMapsTerminal.IsDriver)
		{
			return;
		}
		if (buttonPressed == CustomMapKeyboardBinding.goback)
		{
			if (CustomMapManager.IsLoading() || CustomMapManager.IsUnloading())
			{
				return;
			}
			if (mapLoadError)
			{
				mapLoadError = false;
				loadingMapMessageText.fontSize = 40f;
				CustomMapManager.ClearRoomMap();
				ResetToDefaultView();
			}
			else if (CustomMapLoader.IsMapLoaded() || CustomMapManager.GetRoomMapId() != ModId.Null)
			{
				if (!CanChangeMapState(load: false, out var disallowedReason))
				{
					modDescriptionText.gameObject.SetActive(value: false);
					errorText.text = disallowedReason;
					errorText.gameObject.SetActive(value: true);
				}
				else
				{
					UnloadMap();
				}
			}
			else if (ModInstallationManagement.CurrentOperationOnMod != null && ModInstallationManagement.CurrentOperationOnMod.Id == GetModId())
			{
				GTDev.Log("[CustomMapsDetailsScreen::PressButton] Attempted to go back while this mod is " + ModInstallationManagement.CurrentOperationOnMod.File.State.ToString() + ", ignoring...");
			}
			else
			{
				CustomMapsTerminal.ReturnFromDetailsScreen();
				hasModProfile = false;
				currentMapMod.OnModUpdated -= OnModUpdated;
				currentMapMod = null;
			}
			return;
		}
		if (!hasModProfile || mapLoadError)
		{
			_ = mapLoadError;
			return;
		}
		switch (buttonPressed)
		{
		case CustomMapKeyboardBinding.option3:
			RefreshCurrentMapMod();
			return;
		case CustomMapKeyboardBinding.map:
			if (currentMapMod == null || CustomMapLoader.IsMapLoaded() || CustomMapManager.IsLoading() || CustomMapManager.IsUnloading())
			{
				return;
			}
			errorText.gameObject.SetActive(value: false);
			errorText.text = "";
			loadingMapLabelText.gameObject.SetActive(value: false);
			loadingMapMessageText.gameObject.SetActive(value: false);
			modDescriptionText.gameObject.SetActive(value: true);
			ModIOManager.RefreshUserProfile(delegate
			{
				if (currentMapMod.IsSubscribed)
				{
					ModIOManager.UnsubscribeFromMod(GetModId(), delegate(Error error)
					{
						if (!error)
						{
							UpdateMapDetails(refreshScreenState: false);
						}
					});
				}
				else
				{
					ModIOManager.SubscribeToMod(GetModId(), delegate(Error error)
					{
						if (!error)
						{
							UpdateMapDetails(refreshScreenState: false);
						}
					});
				}
			});
			break;
		}
		if (buttonPressed == CustomMapKeyboardBinding.enter && !CustomMapManager.IsLoading() && !CustomMapManager.IsUnloading() && !CustomMapLoader.IsMapLoaded() && currentMapMod != null && !IsCurrentModHidden())
		{
			if (currentMapMod.File.State == ModFileState.Installed)
			{
				if (!CanChangeMapState(load: true, out var disallowedReason2))
				{
					modDescriptionText.gameObject.SetActive(value: false);
					errorText.text = disallowedReason2;
					errorText.gameObject.SetActive(value: true);
				}
				else
				{
					LoadMap();
				}
			}
			else
			{
				ModFileState state = currentMapMod.File.State;
				if (state == ModFileState.Queued || state == ModFileState.None)
				{
					ModIOManager.DownloadMod(GetModId(), delegate(bool modDownloadStarted)
					{
						if (modDownloadStarted)
						{
							UpdateStatus();
						}
					});
				}
				else
				{
					Debug.Log($"[CustomMapsDetailsScreen::PressButton] mod has status: {currentMapMod.File.State}, " + "cannot start download or attempt to load map...");
				}
			}
		}
		if (buttonPressed == CustomMapKeyboardBinding.fav && currentMapMod != null)
		{
			if (isFavorite)
			{
				ModIOManager.RemoveFavorite(currentMapMod.Id);
				isFavorite = ModIOManager.IsModFavorited(currentMapMod.Id);
				favoriteToggleButton.SetButtonActive(isFavorite);
				if (IsCurrentModHidden())
				{
					favoriteToggleButton.gameObject.SetActive(value: false);
				}
			}
			else if (!IsCurrentModHidden())
			{
				ModIOManager.AddFavorite(currentMapMod.Id, delegate
				{
					isFavorite = ModIOManager.IsModFavorited(currentMapMod.Id);
					favoriteToggleButton.SetButtonActive(isFavorite);
				});
			}
		}
		bool flag;
		if (buttonPressed == CustomMapKeyboardBinding.delete)
		{
			if (CustomMapManager.IsLoading() || CustomMapManager.IsUnloading() || CustomMapLoader.IsMapLoaded())
			{
				return;
			}
			Mod mod = currentMapMod;
			if (mod != null)
			{
				Modfile file = mod.File;
				if (file != null)
				{
					ModFileState state = file.State;
					if (state == ModFileState.Queued || state == ModFileState.Installed)
					{
						flag = true;
						goto IL_03ec;
					}
				}
			}
			flag = false;
			goto IL_03ec;
		}
		goto IL_0403;
		IL_0403:
		if (buttonPressed == CustomMapKeyboardBinding.rateUp)
		{
			currentMapMod.RateMod((currentMapMod.CurrentUserRating != ModRating.Positive) ? ModRating.Positive : ModRating.None);
		}
		if (buttonPressed == CustomMapKeyboardBinding.rateDown)
		{
			currentMapMod.RateMod((currentMapMod.CurrentUserRating != ModRating.Negative) ? ModRating.Negative : ModRating.None);
		}
		return;
		IL_03ec:
		if (flag)
		{
			currentMapMod.UninstallOtherUserMod(force: true);
			UpdateStatus();
		}
		goto IL_0403;
	}

	private void RefreshCurrentMapMod()
	{
		if (!CustomMapLoader.IsMapLoaded() && !CustomMapManager.IsLoading() && !CustomMapManager.IsUnloading() && hasModProfile)
		{
			long id = GetModId()._id;
			hasModProfile = false;
			currentMapMod.OnModUpdated -= OnModUpdated;
			currentMapMod = null;
			ResetToDefaultView();
			RetrieveModFromModIO(id, forceUpdate: true);
		}
	}

	private void OnProfileReceived(Error error, Mod mod)
	{
		if ((bool)error)
		{
			modDescriptionText.gameObject.SetActive(value: false);
			errorText.text = $"FAILED TO RETRIEVE MOD DETAILS FOR MOD: {GetModId()}";
			errorText.gameObject.SetActive(value: true);
		}
		else
		{
			SetModProfile(mod);
		}
	}

	private void ResetToDefaultView()
	{
		loadingMapLabelText.gameObject.SetActive(value: false);
		loadingMapMessageText.gameObject.SetActive(value: false);
		mapReadyText.gameObject.SetActive(value: false);
		errorText.gameObject.SetActive(value: false);
		modNameText.gameObject.SetActive(value: false);
		modCreatorLabelText.gameObject.SetActive(value: false);
		modCreatorText.gameObject.SetActive(value: false);
		modDescriptionText.gameObject.SetActive(value: false);
		modStatusText.gameObject.SetActive(value: false);
		modSubscriptionStatusText.gameObject.SetActive(value: false);
		mapScreenshotImage.gameObject.SetActive(value: false);
		hiddenRoomMapText.gameObject.SetActive(value: false);
		outdatedText.gameObject.SetActive(value: false);
		unloadPromptText.gameObject.SetActive(value: false);
		loadingText.gameObject.SetActive(value: true);
		if (CustomMapLoader.IsMapLoaded() || CustomMapManager.IsLoading() || CustomMapManager.IsUnloading())
		{
			ModId modId = new ModId(CustomMapLoader.IsMapLoaded() ? ((long)CustomMapLoader.LoadedMapModId) : (CustomMapManager.IsLoading() ? CustomMapManager.LoadingMapId : CustomMapManager.UnloadingMapId));
			if (hasModProfile && GetModId() == modId)
			{
				UpdateMapDetails();
				return;
			}
			RetrieveModFromModIO(modId, forceUpdate: false, delegate(Error error, Mod mod)
			{
				OnProfileReceived(error, mod);
			});
		}
		else if (CustomMapManager.GetRoomMapId() != ModId.Null)
		{
			OnRoomMapChanged(CustomMapManager.GetRoomMapId());
		}
		else if (hasModProfile)
		{
			UpdateMapDetails();
		}
	}

	private void UpdateMapDetails(bool refreshScreenState = true)
	{
		if (!hasModProfile)
		{
			return;
		}
		if (IsCurrentModHidden())
		{
			modNameText.text = hiddenMapTitle;
			modDescriptionText.text = hiddenMapDesc;
			modCreatorLabelText.gameObject.SetActive(value: false);
			modCreatorText.text = "";
			mapScreenshotImage.sprite = hiddenMapLogo;
			mapScreenshotImage.gameObject.SetActive(value: true);
		}
		else
		{
			modNameText.text = currentMapMod.Name;
			modDescriptionText.text = currentMapMod.Description;
			modCreatorLabelText.gameObject.SetActive(value: true);
			modCreatorText.text = currentMapMod.Creator.Username;
			ModIOManager.GetModLogo(currentMapMod, OnGetModLogo);
		}
		UpdateStatus();
		if (!refreshScreenState)
		{
			return;
		}
		loadingText.gameObject.SetActive(value: false);
		loadingMapLabelText.gameObject.SetActive(value: false);
		loadingMapMessageText.gameObject.SetActive(value: false);
		hiddenRoomMapText.gameObject.SetActive(value: false);
		mapReadyText.gameObject.SetActive(value: false);
		unloadPromptText.gameObject.SetActive(value: false);
		errorText.gameObject.SetActive(value: false);
		modNameText.gameObject.SetActive(value: true);
		modDescriptionText.gameObject.SetActive(value: true);
		if (!IsCurrentModHidden())
		{
			modCreatorLabelText.gameObject.SetActive(value: true);
			modCreatorText.gameObject.SetActive(value: true);
		}
		if (CustomMapLoader.IsMapLoaded())
		{
			ModId modId = new ModId(CustomMapLoader.LoadedMapModId);
			if (GetModId() == modId)
			{
				OnMapLoadComplete_UIUpdate();
				return;
			}
			RetrieveModFromModIO(modId, forceUpdate: false, delegate(Error error, Mod mod)
			{
				OnProfileReceived(error, mod);
			});
		}
		else if (CustomMapManager.IsLoading() && !mapLoadError)
		{
			modDescriptionText.gameObject.SetActive(value: false);
			if (!CustomMapManager.IsUnloading())
			{
				loadingMapLabelText.text = mapLoadingString + " 0%";
			}
			else
			{
				loadingMapLabelText.text = mapUnloadingString;
			}
			loadingMapLabelText.gameObject.SetActive(value: true);
		}
		else if (CustomMapManager.IsUnloading())
		{
			modDescriptionText.gameObject.SetActive(value: false);
			loadingMapLabelText.text = mapUnloadingString;
			loadingMapLabelText.gameObject.SetActive(value: true);
		}
		else if (CustomMapManager.GetRoomMapId() != ModId.Null)
		{
			ShowLoadRoomMapPrompt();
		}
		else if (mapLoadError)
		{
			modDescriptionText.gameObject.SetActive(value: false);
			loadingMapLabelText.gameObject.SetActive(value: true);
			loadingMapMessageText.gameObject.SetActive(value: true);
		}
	}

	private void OnGetModLogo(Error error, Texture2D modLogo)
	{
		if ((bool)error)
		{
			Debug.LogError($"[CustomMapsDetailsScreen::OnGetModLogo] Failed to retrieve logo for Mod {GetModId()}");
			return;
		}
		mapScreenshotImage.sprite = Sprite.Create(modLogo, new Rect(0f, 0f, 320f, 180f), new Vector2(0.5f, 0.5f));
		mapScreenshotImage.gameObject.SetActive(value: true);
	}

	private async Task UpdateStatus(bool errorEncountered = false)
	{
		if (!base.isActiveAndEnabled || currentMapMod == null)
		{
			return;
		}
		outdatedText.gameObject.SetActive(value: false);
		deleteButton.gameObject.SetActive(value: false);
		subscriptionToggleButton.gameObject.SetActive(value: false);
		favoriteToggleButton.gameObject.SetActive(value: false);
		rateUpButton.gameObject.SetActive(value: false);
		rateDownButton.gameObject.SetActive(value: false);
		modSubscriptionStatusText.gameObject.SetActive(value: false);
		modStatusLabelText?.gameObject.SetActive(value: false);
		modStatusText.gameObject.SetActive(value: false);
		if (mapLoadError || CustomMapManager.IsUnloading() || CustomMapManager.IsLoading() || CustomMapLoader.IsMapLoaded() || CustomMapManager.GetRoomMapId() != ModId.Null || IsCurrentModHidden())
		{
			loadButton?.gameObject.SetActive(value: false);
			if (ModIOManager.IsModFavorited(currentMapMod.Id))
			{
				favoriteToggleButton.SetButtonActive(active: true);
				favoriteToggleButton.gameObject.SetActive(CustomMapsTerminal.IsDriver);
			}
			if (currentMapMod.File.State == ModFileState.Installed)
			{
				deleteButton.gameObject.SetActive(CustomMapsTerminal.IsDriver);
			}
			return;
		}
		loadButton?.gameObject.SetActive(value: true);
		isFavorite = ModIOManager.IsModFavorited(currentMapMod.Id);
		favoriteToggleButton.SetButtonActive(isFavorite);
		favoriteToggleButton.gameObject.SetActive(CustomMapsTerminal.IsDriver);
		rateUpButton.gameObject.SetActive(CustomMapsTerminal.IsDriver);
		rateDownButton.gameObject.SetActive(CustomMapsTerminal.IsDriver);
		ModFileState modFileState = (errorEncountered ? ModFileState.FileOperationFailed : currentMapMod.File.State);
		modStatusText.text = modStatusStrings.GetValueOrDefault(modFileState, "STATUS STRING MISSING!");
		if (ModIOManager.IsLoggedIn())
		{
			modSubscriptionStatusText.text = (currentMapMod.IsSubscribed ? subscribedStatusString : unsubscribedStatusString);
			modSubscriptionStatusText.gameObject.SetActive(value: true);
			subscriptionToggleButton.SetButtonActive(currentMapMod.IsSubscribed);
			subscriptionToggleButton.SetButtonText(currentMapMod.IsSubscribed ? unsubscribeString : subscribeString);
			subscriptionToggleButton.gameObject.SetActive(CustomMapsTerminal.IsDriver);
		}
		switch (modFileState)
		{
		case ModFileState.Installed:
		{
			loadButton?.SetButtonText(loadMapString);
			deleteButton.gameObject.SetActive(CustomMapsTerminal.IsDriver);
			bool item = (await ModIOManager.IsModOutdated(GetModId())).Item1;
			outdatedText.gameObject.SetActive(item);
			break;
		}
		case ModFileState.Queued:
		{
			bool flag = ModInstallationManagement.DoesModNeedUpdate(currentMapMod);
			loadButton?.SetButtonText(flag ? updateMapString : downloadMapString);
			modStatusText.text = (flag ? mapNeedsUpdateString : mapNotDownloadedString);
			break;
		}
		case ModFileState.None:
			loadButton?.SetButtonText(downloadMapString);
			break;
		}
		modStatusLabelText?.gameObject.SetActive(value: true);
		modStatusText.gameObject.SetActive(value: true);
	}

	private bool CanChangeMapState(bool load, out string disallowedReason)
	{
		disallowedReason = "";
		if (NetworkSystem.Instance.InRoom && NetworkSystem.Instance.SessionIsPrivate)
		{
			if (!CustomMapManager.AreAllPlayersInVirtualStump())
			{
				disallowedReason = "ALL PLAYERS IN THE ROOM MUST BE INSIDE THE VIRTUAL STUMP BEFORE " + (load ? "" : "UN") + "LOADING A MAP.";
				return false;
			}
			return true;
		}
		if (!CustomMapManager.IsLocalPlayerInVirtualStump())
		{
			disallowedReason = "YOU MUST BE INSIDE THE VIRTUAL STUMP TO " + (load ? "" : "UN") + "LOAD A MAP.";
			return false;
		}
		return true;
	}

	private void LoadMap()
	{
		modDescriptionText.gameObject.SetActive(value: false);
		modStatusText.gameObject.SetActive(value: false);
		modSubscriptionStatusText.gameObject.SetActive(value: false);
		outdatedText.gameObject.SetActive(value: false);
		loadingMapLabelText.gameObject.SetActive(value: true);
		if (NetworkSystem.Instance.InRoom && !NetworkSystem.Instance.SessionIsPrivate)
		{
			NetworkSystem.Instance.ReturnToSinglePlayer();
		}
		deleteButton.gameObject.SetActive(value: false);
		subscriptionToggleButton.gameObject.SetActive(value: false);
		networkObject.LoadMapSynced(GetModId());
	}

	private void UnloadMap()
	{
		networkObject.UnloadMapSynced();
	}

	public void OnMapLoadComplete(bool success)
	{
		if (success)
		{
			OnMapLoadComplete_UIUpdate();
		}
	}

	private void OnMapLoadComplete_UIUpdate()
	{
		modDescriptionText.gameObject.SetActive(value: false);
		loadingMapLabelText.gameObject.SetActive(value: false);
		loadingMapMessageText.gameObject.SetActive(value: false);
		hiddenRoomMapText.gameObject.SetActive(value: false);
		errorText.gameObject.SetActive(value: false);
		mapReadyText.gameObject.SetActive(value: true);
		unloadPromptText.gameObject.SetActive(value: true);
	}

	private void OnMapUnloaded()
	{
		mapLoadError = false;
		loadingMapMessageText.fontSize = 40f;
		UpdateMapDetails();
	}

	private void OnRoomMapChanged(ModId roomMapID)
	{
		if (roomMapID == ModId.Null)
		{
			UpdateMapDetails();
		}
		else if (GetModId() != roomMapID)
		{
			RetrieveModFromModIO(roomMapID, forceUpdate: false, OnRoomMapRetrieved);
		}
		else
		{
			ShowLoadRoomMapPrompt();
		}
	}

	private void OnRoomMapRetrieved(Error error, Mod mod)
	{
		OnProfileReceived(error, mod);
		if (!error)
		{
			ShowLoadRoomMapPrompt();
		}
	}

	private void ShowLoadRoomMapPrompt()
	{
		if (!CustomMapManager.IsUnloading() && !CustomMapManager.IsLoading() && !CustomMapLoader.IsMapLoaded(GetModId()))
		{
			modDescriptionText.gameObject.SetActive(value: false);
			loadingText.gameObject.SetActive(value: false);
			loadingMapLabelText.gameObject.SetActive(value: false);
			mapReadyText.gameObject.SetActive(value: false);
			unloadPromptText.gameObject.SetActive(value: false);
			hiddenRoomMapText.gameObject.SetActive(value: false);
			if (IsCurrentModHidden())
			{
				hiddenRoomMapText.gameObject.SetActive(value: true);
			}
		}
	}

	public void OnMapLoadProgress(MapLoadStatus loadStatus, int progress, string message)
	{
		if (loadStatus != MapLoadStatus.None)
		{
			mapLoadError = false;
			loadingMapMessageText.fontSize = 40f;
			hiddenRoomMapText.gameObject.SetActive(value: false);
			modDescriptionText.gameObject.SetActive(value: false);
		}
		switch (loadStatus)
		{
		case MapLoadStatus.Downloading:
			loadingMapLabelText.text = mapAutoDownloadingString;
			loadingMapLabelText.gameObject.SetActive(value: true);
			loadingMapMessageText.gameObject.SetActive(value: false);
			loadingMapMessageText.text = "";
			break;
		case MapLoadStatus.Loading:
			loadingMapLabelText.text = mapLoadingString + " " + progress + "%";
			loadingMapLabelText.gameObject.SetActive(value: true);
			loadingMapMessageText.text = message;
			loadingMapMessageText.gameObject.SetActive(value: true);
			break;
		case MapLoadStatus.Unloading:
			mapReadyText.gameObject.SetActive(value: false);
			unloadPromptText.gameObject.SetActive(value: false);
			loadingMapLabelText.text = mapUnloadingString;
			loadingMapLabelText.gameObject.SetActive(value: true);
			loadingMapMessageText.gameObject.SetActive(value: false);
			loadingMapMessageText.text = "";
			break;
		case MapLoadStatus.Error:
			mapLoadError = true;
			loadingMapLabelText.text = mapLoadingErrorString;
			loadingMapLabelText.gameObject.SetActive(value: true);
			if (CustomMapsTerminal.IsDriver)
			{
				loadingMapMessageText.text = message + "\n" + mapLoadingErrorDriverString;
			}
			else
			{
				loadingMapMessageText.text = message + "\n" + mapLoadingErrorNonDriverString;
			}
			if (loadingMapMessageText.text.Length > 150)
			{
				loadingMapMessageText.fontSize = 30f;
			}
			else
			{
				loadingMapMessageText.fontSize = 40f;
			}
			loadingMapMessageText.gameObject.SetActive(value: true);
			break;
		}
	}

	public ModId GetModId()
	{
		return currentMapMod?.Id ?? ModId.Null;
	}

	public bool IsCurrentModHidden()
	{
		if (!hasModProfile)
		{
			return false;
		}
		if (!(currentMapMod.Creator == null))
		{
			if (!ModIOManager.IsLoggedIn())
			{
				return currentMapMod.IsHidden();
			}
			return false;
		}
		return true;
	}
}

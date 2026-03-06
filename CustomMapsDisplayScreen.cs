using System;
using System.Threading.Tasks;
using GorillaTagScripts.VirtualStumpCustomMaps;
using Modio;
using Modio.Mods;
using Modio.Users;
using TMPro;
using UnityEngine;

public class CustomMapsDisplayScreen : CustomMapsTerminalScreen
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
	private TMP_Text loadingMapLabelText;

	[SerializeField]
	private TMP_Text loadingMapMessageText;

	[SerializeField]
	private TMP_Text loadRoomMapPromptText;

	[SerializeField]
	private TMP_Text hiddenRoomMapText;

	[SerializeField]
	private TMP_Text mapReadyText;

	[SerializeField]
	private TMP_Text errorText;

	[SerializeField]
	private TMP_Text outdatedText;

	[SerializeField]
	private string mapAutoDownloadingString = "DOWNLOADING...";

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
	private string mapNotDownloadedString = "NOT DOWNLOADED";

	[SerializeField]
	private string mapNeedsUpdateString = "NEEDS UPDATE";

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
			UpdateMapDetails();
		}
	}

	private void RefreshCurrentMapMod()
	{
		if (!CustomMapLoader.IsMapLoaded() && !CustomMapManager.IsLoading() && !CustomMapManager.IsUnloading() && hasModProfile)
		{
			long id = GetModId()._id;
			hasModProfile = false;
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
		mapScreenshotImage.gameObject.SetActive(value: false);
		loadRoomMapPromptText.gameObject.SetActive(value: false);
		hiddenRoomMapText.gameObject.SetActive(value: false);
		outdatedText.gameObject.SetActive(value: false);
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
		loadRoomMapPromptText.gameObject.SetActive(value: false);
		hiddenRoomMapText.gameObject.SetActive(value: false);
		mapReadyText.gameObject.SetActive(value: false);
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
		if (base.isActiveAndEnabled && currentMapMod != null)
		{
			outdatedText.gameObject.SetActive(value: false);
			ModFileState modFileState = (errorEncountered ? ModFileState.FileOperationFailed : currentMapMod.File.State);
			if ((uint)modFileState > 1u && modFileState == ModFileState.Installed)
			{
				bool item = (await ModIOManager.IsModOutdated(GetModId())).Item1;
				outdatedText.gameObject.SetActive(item);
			}
		}
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
		loadRoomMapPromptText.gameObject.SetActive(value: false);
		hiddenRoomMapText.gameObject.SetActive(value: false);
		errorText.gameObject.SetActive(value: false);
		mapReadyText.gameObject.SetActive(value: true);
	}

	private void OnMapUnloaded()
	{
		mapLoadError = false;
		loadingMapMessageText.fontSize = 80f;
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
			hiddenRoomMapText.gameObject.SetActive(value: false);
			loadRoomMapPromptText.gameObject.SetActive(value: false);
			if (IsCurrentModHidden())
			{
				hiddenRoomMapText.gameObject.SetActive(value: true);
			}
			else
			{
				loadRoomMapPromptText.gameObject.SetActive(value: true);
			}
		}
	}

	public void OnMapLoadProgress(MapLoadStatus loadStatus, int progress, string message)
	{
		if (loadStatus != MapLoadStatus.None)
		{
			mapLoadError = false;
			loadingMapMessageText.fontSize = 80f;
			hiddenRoomMapText.gameObject.SetActive(value: false);
			loadRoomMapPromptText.gameObject.SetActive(value: false);
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
				loadingMapMessageText.fontSize = 60f;
			}
			else
			{
				loadingMapMessageText.fontSize = 80f;
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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GorillaExtensions;
using GorillaNetworking;
using GorillaTagScripts.VirtualStumpCustomMaps.UI;
using Modio;
using Modio.Errors;
using Modio.Mods;
using Modio.Users;
using TMPro;
using UnityEngine;

public class CustomMapsListScreen : CustomMapsTerminalScreen
{
	public enum ListScreenState
	{
		AvailableMods,
		InstalledMods,
		FavoriteMods,
		SubscribedMods,
		CustomModList
	}

	[SerializeField]
	private TMP_Text loadingText;

	[SerializeField]
	private TMP_Text errorText;

	[SerializeField]
	private TMP_Text modPageText;

	[SerializeField]
	private TMP_Text titleText;

	[SerializeField]
	private TMP_Text sortTypeText;

	[SerializeField]
	private GameObject sortByButton;

	[SerializeField]
	private CustomMapsScreenButton allMapsButton;

	[SerializeField]
	private CustomMapsScreenButton officialMapsButton;

	[SerializeField]
	private CustomMapsScreenButton favoriteMapsButton;

	[SerializeField]
	private CustomMapsScreenButton installedMapsButton;

	[SerializeField]
	private CustomMapsScreenButton subscribedMapsButton;

	[SerializeField]
	private CustomMapsScreenButton searchButton;

	[SerializeField]
	private CustomMapsScreenButton pageUpButton;

	[SerializeField]
	private CustomMapsScreenButton pageDownButton;

	[SerializeField]
	private CustomMapsGalleryView customMapsGalleryView;

	[SerializeField]
	private string browseModsTitle = "AVAILABLE MODS";

	[SerializeField]
	private string officialModsTitle = "OFFICIAL MODS";

	[SerializeField]
	private string installedModsTitle = "INSTALLED MODS";

	[SerializeField]
	private string favoriteModsTitle = "FAVORITE MODS";

	[SerializeField]
	private string subscribedModsTitle = "SUBSCRIBED MODS";

	[SerializeField]
	private string noModsAvailableString = "NO MODS AVAILABLE";

	[SerializeField]
	private string noModsFoundGenericString = "NO MODS FOUND";

	[SerializeField]
	private string noSubscribedModsString = "NOT SUBSCRIBED TO ANY MODS";

	[SerializeField]
	private string noInstalledModsString = "NO MODS INSTALLED";

	[SerializeField]
	private string noFavoriteModsString = "NO FAVORITE MODS FOUND";

	[SerializeField]
	private string failedToRetrieveModsString = "FAILED TO RETRIEVE MODS FROM MOD.IO \nPRESS THE 'REFRESH' BUTTON TO RETRY";

	[SerializeField]
	private int modsPerPage = 12;

	[SerializeField]
	private int numModsPerRequest = 24;

	[SerializeField]
	private int maxModListItemLength = 25;

	[SerializeField]
	private string officialMapsTag = "Official Maps";

	[SerializeField]
	private string featuredModsPlayFabKey = "VStumpFeaturedMaps";

	private bool loadingFeaturedMods;

	private bool displayFeaturedMods = true;

	private int totalFeaturedMods;

	private List<long> featuredModIds = new List<long>();

	private List<Mod> featuredMods = new List<Mod>();

	private int currentAvailableModsRequestPage;

	private bool loadingAvailableMods;

	private int totalAvailableMods;

	private bool errorLoadingAvailableMods;

	private List<Mod> availableMods = new List<Mod>();

	private List<Mod> filteredAvailableMods = new List<Mod>();

	private bool loadingInstalledMods;

	private bool errorLoadingInstalledMods;

	private int totalInstalledMods;

	private Mod[] installedMods;

	private List<Mod> filteredInstalledMods = new List<Mod>();

	private bool loadingFavoriteMods;

	private bool errorLoadingFavoriteMods;

	private int totalFavoriteMods;

	private List<Mod> favoriteMods = new List<Mod>();

	private List<Mod> filteredFavoriteMods = new List<Mod>();

	private bool loadingSubscribedMods;

	private bool errorLoadingSubscribedMods;

	private int totalSubscribedMods;

	private Mod[] subscribedMods;

	private List<Mod> filteredSubscribedMods = new List<Mod>();

	private int currentModPage;

	private int totalModCount;

	private List<Mod> displayedModProfiles = new List<Mod>();

	private int sortTypeIndex;

	private SortModsBy sortType = SortModsBy.Popular;

	private const int MAX_SORT_TYPES = 6;

	private List<string> searchTags = new List<string>();

	private bool isAscendingOrder;

	private bool officialMapsOnly;

	private bool useMapName = true;

	private Vector3 subscribedBttnPosition;

	private Vector3 searchBttnPosition;

	private bool restartCustomModListRetrieval;

	private bool restartCustomModListRetrievalForceRefresh;

	private bool restartInstalledModsRetrieval;

	private bool restartInstalledModsRetrievalForceRefresh;

	private bool restartFavoriteModsRetrieval;

	private bool restartFavoriteModsRetrievalForceRefresh;

	private bool restartSubscribedModsRetrieval;

	public ListScreenState currentState;

	public bool OfficialMapsOnly => officialMapsOnly;

	public int CurrentModPage => currentModPage;

	public int ModsPerPage => modsPerPage;

	public SortModsBy SortType
	{
		get
		{
			return sortType;
		}
		set
		{
			if (sortType != value)
			{
				currentAvailableModsRequestPage = 0;
			}
			sortType = value;
			switch (sortType)
			{
			case SortModsBy.Popular:
				isAscendingOrder = false;
				break;
			case SortModsBy.Name:
				isAscendingOrder = true;
				break;
			case SortModsBy.Rating:
				isAscendingOrder = false;
				break;
			case SortModsBy.Downloads:
				isAscendingOrder = false;
				break;
			case SortModsBy.Subscribers:
				isAscendingOrder = false;
				break;
			case SortModsBy.DateSubmitted:
				isAscendingOrder = false;
				break;
			case SortModsBy.Price:
				break;
			}
		}
	}

	private void Awake()
	{
		subscribedBttnPosition = subscribedMapsButton.transform.position;
		searchBttnPosition = searchButton.transform.position;
	}

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
		ModIOManager.OnModIOCacheRefreshing.RemoveListener(OnModCacheRefreshing);
		ModIOManager.OnModIOCacheRefreshing.AddListener(OnModCacheRefreshing);
		ModIOManager.OnModIOCacheRefreshed.RemoveListener(OnModCacheRefreshed);
		ModIOManager.OnModIOCacheRefreshed.AddListener(OnModCacheRefreshed);
		if (featuredMods.IsNullOrEmpty())
		{
			RetrieveFeaturedMods();
		}
		if (availableMods.IsNullOrEmpty())
		{
			RetrieveAvailableMods();
		}
		RetrieveInstalledMods();
		RetrieveFavoriteMods();
		RetrieveSubscribedMods();
		RefreshScreenState();
	}

	public override void Hide()
	{
		base.Hide();
		ModIOManager.OnModIOLoggedIn.RemoveListener(OnModIOLoggedIn);
		ModIOManager.OnModIOLoggedOut.RemoveListener(OnModIOLoggedOut);
		ModIOManager.OnModIOUserChanged.RemoveListener(OnModIOUserChanged);
		ModIOManager.OnModIOCacheRefreshing.RemoveListener(OnModCacheRefreshing);
		ModIOManager.OnModIOCacheRefreshed.RemoveListener(OnModCacheRefreshed);
	}

	private void OnModIOLoggedIn()
	{
		if (CustomMapsTerminal.IsDriver)
		{
			subscribedMapsButton.gameObject.SetActive(value: true);
		}
		subscribedMods = null;
		filteredSubscribedMods.Clear();
		totalSubscribedMods = 0;
		RetrieveSubscribedMods();
	}

	private void OnModIOLoggedOut()
	{
		subscribedMapsButton.gameObject.SetActive(value: false);
		subscribedMods = null;
		filteredSubscribedMods.Clear();
		totalSubscribedMods = 0;
	}

	private void OnModIOUserChanged(User user)
	{
	}

	private void OnModCacheRefreshing()
	{
		RefreshScreenState();
	}

	private void OnModCacheRefreshed()
	{
		RetrieveFavoriteMods();
		RetrieveInstalledMods();
		if (ModIOManager.IsLoggedIn())
		{
			RetrieveSubscribedMods();
		}
	}

	public override void PressButton(CustomMapKeyboardBinding buttonPressed)
	{
		if (Time.time < showTime + activationTime)
		{
			return;
		}
		GTDev.Log("[CustomMapsListScreen::PressButton] Is Driver: " + CustomMapsTerminal.IsDriver + ", Button Pressed: " + buttonPressed);
		if (!CustomMapsTerminal.IsDriver || buttonPressed == CustomMapKeyboardBinding.goback || loadingText.gameObject.activeSelf)
		{
			return;
		}
		switch (buttonPressed)
		{
		case CustomMapKeyboardBinding.option3:
			ModIOManager.RefreshUserProfile(delegate(bool result)
			{
				if (result)
				{
					Refresh();
				}
			});
			break;
		case CustomMapKeyboardBinding.option4:
			CustomMapsTerminal.ShowSearchScreen();
			break;
		case CustomMapKeyboardBinding.up:
			currentModPage--;
			RefreshScreenState();
			break;
		case CustomMapKeyboardBinding.down:
			currentModPage++;
			RefreshScreenState();
			break;
		case CustomMapKeyboardBinding.all:
		{
			bool flag = officialMapsOnly;
			officialMapsOnly = false;
			displayFeaturedMods = sortType == SortModsBy.Popular;
			if (flag)
			{
				RefreshModSearch();
			}
			SwapListDisplay(ListScreenState.AvailableMods, flag);
			break;
		}
		case CustomMapKeyboardBinding.mustplay:
		{
			bool flag2 = !officialMapsOnly;
			officialMapsOnly = true;
			displayFeaturedMods = false;
			if (flag2)
			{
				RefreshModSearch();
			}
			SwapListDisplay(ListScreenState.AvailableMods, flag2);
			break;
		}
		case CustomMapKeyboardBinding.sub:
			SwapListDisplay(ListScreenState.SubscribedMods);
			break;
		case CustomMapKeyboardBinding.fav:
			SwapListDisplay(ListScreenState.FavoriteMods);
			break;
		case CustomMapKeyboardBinding.inst:
			SwapListDisplay(ListScreenState.InstalledMods);
			break;
		case CustomMapKeyboardBinding.sort:
			SetSortType();
			RefreshModSearch();
			break;
		default:
			if (CustomMapKeyboardBinding.one <= buttonPressed && buttonPressed <= CustomMapKeyboardBinding.nine && !customMapsGalleryView.IsNull())
			{
				customMapsGalleryView.ShowDetailsForEntry((int)(buttonPressed - 1));
			}
			break;
		}
	}

	private void SetSortType()
	{
		currentAvailableModsRequestPage = 0;
		sortTypeIndex++;
		if (sortTypeIndex >= 6)
		{
			sortTypeIndex = 0;
		}
		switch (sortTypeIndex)
		{
		case 0:
			SortType = SortModsBy.Popular;
			useMapName = true;
			displayFeaturedMods = !officialMapsOnly;
			break;
		case 1:
			SortType = SortModsBy.DateSubmitted;
			useMapName = true;
			displayFeaturedMods = false;
			break;
		case 2:
			SortType = SortModsBy.Rating;
			useMapName = false;
			displayFeaturedMods = false;
			break;
		case 3:
			SortType = SortModsBy.Downloads;
			useMapName = true;
			displayFeaturedMods = false;
			break;
		case 4:
			SortType = SortModsBy.Subscribers;
			useMapName = true;
			displayFeaturedMods = false;
			break;
		case 5:
			SortType = SortModsBy.Name;
			useMapName = true;
			displayFeaturedMods = false;
			break;
		default:
			sortTypeIndex = 0;
			SortType = SortModsBy.Popular;
			useMapName = true;
			displayFeaturedMods = !officialMapsOnly;
			break;
		}
	}

	public void SwapListDisplay(ListScreenState newState, bool force = false)
	{
		if ((currentState != newState || force) && (newState != ListScreenState.SubscribedMods || ModIOManager.IsLoggedIn()))
		{
			currentState = newState;
			currentModPage = 0;
			switch (currentState)
			{
			case ListScreenState.AvailableMods:
				allMapsButton.SetButtonActive(!officialMapsOnly);
				officialMapsButton.SetButtonActive(officialMapsOnly);
				favoriteMapsButton.SetButtonActive(active: false);
				installedMapsButton.SetButtonActive(active: false);
				subscribedMapsButton.SetButtonActive(active: false);
				searchButton.SetButtonActive(active: false);
				break;
			case ListScreenState.InstalledMods:
				allMapsButton.SetButtonActive(active: false);
				officialMapsButton.SetButtonActive(active: false);
				favoriteMapsButton.SetButtonActive(active: false);
				subscribedMapsButton.SetButtonActive(active: false);
				searchButton.SetButtonActive(active: false);
				installedMapsButton.SetButtonActive(active: true);
				break;
			case ListScreenState.FavoriteMods:
				allMapsButton.SetButtonActive(active: false);
				officialMapsButton.SetButtonActive(active: false);
				installedMapsButton.SetButtonActive(active: false);
				subscribedMapsButton.SetButtonActive(active: false);
				searchButton.SetButtonActive(active: false);
				favoriteMapsButton.SetButtonActive(active: true);
				break;
			case ListScreenState.SubscribedMods:
				allMapsButton.SetButtonActive(active: false);
				officialMapsButton.SetButtonActive(active: false);
				installedMapsButton.SetButtonActive(active: false);
				favoriteMapsButton.SetButtonActive(active: false);
				searchButton.SetButtonActive(active: false);
				subscribedMapsButton.SetButtonActive(active: true);
				break;
			}
			RefreshScreenState();
		}
	}

	public void RefreshModSearch()
	{
		if (!loadingAvailableMods && !loadingFavoriteMods && !loadingInstalledMods && !loadingSubscribedMods)
		{
			currentModPage = 0;
			availableMods.Clear();
			filteredAvailableMods.Clear();
			currentAvailableModsRequestPage = 0;
			errorLoadingAvailableMods = false;
			totalAvailableMods = 0;
			RetrieveAvailableMods();
		}
	}

	public void Refresh()
	{
		if (!loadingAvailableMods && !loadingFavoriteMods && !loadingFeaturedMods && !loadingInstalledMods && !loadingSubscribedMods)
		{
			currentModPage = 0;
			switch (currentState)
			{
			case ListScreenState.AvailableMods:
				featuredMods.Clear();
				availableMods.Clear();
				filteredAvailableMods.Clear();
				currentAvailableModsRequestPage = 0;
				errorLoadingAvailableMods = false;
				totalAvailableMods = 0;
				RetrieveFeaturedMods();
				RetrieveAvailableMods();
				break;
			case ListScreenState.InstalledMods:
				RetrieveInstalledMods(forceRefresh: true);
				break;
			case ListScreenState.FavoriteMods:
				RetrieveFavoriteMods(forceRefresh: true);
				break;
			case ListScreenState.SubscribedMods:
				RetrieveSubscribedMods();
				break;
			}
		}
	}

	private void RetrieveFeaturedMods()
	{
		if (!loadingFeaturedMods && featuredMods.Count <= 0)
		{
			loadingFeaturedMods = true;
			PlayFabTitleDataCache.Instance.GetTitleData(featuredModsPlayFabKey, OnGetFeaturedModsTitleData, delegate
			{
				loadingFeaturedMods = false;
				RefreshScreenState();
			});
		}
	}

	private async void OnGetFeaturedModsTitleData(string data)
	{
		if (data.IsNullOrEmpty())
		{
			RefreshScreenState();
			return;
		}
		featuredModIds.Clear();
		featuredMods.Clear();
		if (data[0] == '"' && data[data.Length - 1] == '"')
		{
			data = data.Substring(1, data.Length - 2);
		}
		string[] array = data.Split(',');
		string[] array2 = array;
		foreach (string s in array2)
		{
			if (!s.IsNullOrEmpty())
			{
				long featuredModId;
				try
				{
					featuredModId = long.Parse(s);
				}
				catch (Exception)
				{
					continue;
				}
				(Error, Mod) tuple = await ModIOManager.GetMod(new ModId(featuredModId));
				if (!tuple.Item1)
				{
					featuredModIds.Add(featuredModId);
					featuredMods.Add(tuple.Item2);
				}
			}
		}
		totalFeaturedMods = featuredMods.Count;
		GTDev.Log($"CustomMapsListScreen::OnGetFeaturedModsTitleData totalFeaturedMods {totalFeaturedMods}");
		FilterAvailableMods();
		loadingFeaturedMods = false;
		if (currentState == ListScreenState.AvailableMods)
		{
			RefreshScreenState();
		}
	}

	private async void RetrieveAvailableMods()
	{
		if (!loadingAvailableMods)
		{
			loadingAvailableMods = true;
			ModSearchFilter modSearchFilter = new ModSearchFilter(currentAvailableModsRequestPage++, numModsPerRequest);
			modSearchFilter.SortBy = sortType;
			if (officialMapsOnly)
			{
				modSearchFilter.AddTag(officialMapsTag);
			}
			modSearchFilter.IsSortAscending = isAscendingOrder;
			var (error, modioPage) = await ModIOManager.GetMods(modSearchFilter.GetModsFilter());
			if ((bool)error)
			{
				errorLoadingAvailableMods = true;
				loadingAvailableMods = false;
				GTDev.LogError("[CustomMapsListScreen::OnAvailableModsRetrieved] Failed to retrieve mods. Error: " + error.GetMessage());
			}
			else
			{
				totalAvailableMods = (int)modioPage.TotalSearchResults;
				availableMods.AddRange(modioPage.Data);
				FilterAvailableMods();
			}
			loadingAvailableMods = false;
			if (currentState == ListScreenState.AvailableMods)
			{
				RefreshScreenState();
			}
		}
	}

	private void FilterAvailableMods()
	{
		filteredAvailableMods.Clear();
		if (availableMods.IsNullOrEmpty())
		{
			return;
		}
		totalAvailableMods = Mathf.Max(0, totalAvailableMods - 1);
		foreach (Mod availableMod in availableMods)
		{
			ModIOManager.TryGetNewMapsModId(out var newMapsModId);
			if (!(availableMod.Id == newMapsModId) && (!displayFeaturedMods || featuredModIds.IsNullOrEmpty() || !featuredModIds.Contains(availableMod.Id)))
			{
				filteredAvailableMods.Add(availableMod);
			}
		}
		if (displayFeaturedMods && !featuredMods.IsNullOrEmpty())
		{
			filteredAvailableMods.InsertRange(0, featuredMods);
		}
	}

	private async Task RetrieveSubscribedMods()
	{
		if (!ModIOManager.IsLoggedIn())
		{
			return;
		}
		if (loadingSubscribedMods)
		{
			restartSubscribedModsRetrieval = true;
			return;
		}
		subscribedMods = null;
		filteredSubscribedMods.Clear();
		totalSubscribedMods = 0;
		errorLoadingSubscribedMods = false;
		loadingSubscribedMods = true;
		Error error;
		(error, subscribedMods) = await ModIOManager.GetSubscribedMods();
		if (restartSubscribedModsRetrieval)
		{
			restartSubscribedModsRetrieval = false;
			RetrieveSubscribedMods();
			return;
		}
		if ((bool)error)
		{
			errorLoadingSubscribedMods = true;
			loadingSubscribedMods = false;
			Debug.LogError("[CustomMapsListScreen::RetrieveSubscribedMods] Failed to get subscribed mods. Error: " + error.GetMessage());
			return;
		}
		FilterSubscribedMods();
		totalSubscribedMods = filteredSubscribedMods.Count;
		loadingSubscribedMods = false;
		if (currentState == ListScreenState.SubscribedMods)
		{
			RefreshScreenState();
		}
	}

	private void FilterSubscribedMods()
	{
		filteredSubscribedMods.Clear();
		if (subscribedMods.IsNullOrEmpty())
		{
			return;
		}
		Mod[] array = subscribedMods;
		foreach (Mod mod in array)
		{
			ModIOManager.TryGetNewMapsModId(out var newMapsModId);
			if (!(mod.Id == newMapsModId))
			{
				filteredSubscribedMods.Add(mod);
			}
		}
	}

	private async Task RetrieveInstalledMods(bool forceRefresh = false)
	{
		if (loadingInstalledMods)
		{
			restartInstalledModsRetrieval = true;
			restartInstalledModsRetrievalForceRefresh = forceRefresh;
			return;
		}
		installedMods = null;
		filteredInstalledMods.Clear();
		totalInstalledMods = 0;
		errorLoadingInstalledMods = false;
		loadingInstalledMods = true;
		Error error;
		(error, installedMods) = await ModIOManager.GetInstalledMods(forceRefresh);
		if (restartInstalledModsRetrieval)
		{
			restartInstalledModsRetrieval = false;
			RetrieveInstalledMods(restartInstalledModsRetrievalForceRefresh);
			restartInstalledModsRetrievalForceRefresh = false;
			return;
		}
		if ((bool)error)
		{
			errorLoadingInstalledMods = true;
			loadingInstalledMods = false;
			GTDev.LogError("[CustomMapsListScreen::RetrieveInstalledMods] Failed to get Installed Mods. Error: " + error.GetMessage());
			return;
		}
		FilterInstalledMods();
		totalInstalledMods = filteredInstalledMods.Count;
		loadingInstalledMods = false;
		if (currentState == ListScreenState.InstalledMods)
		{
			RefreshScreenState();
		}
	}

	private void FilterInstalledMods()
	{
		filteredInstalledMods.Clear();
		if (installedMods.IsNullOrEmpty())
		{
			return;
		}
		Mod[] array = installedMods;
		foreach (Mod mod in array)
		{
			if (!ModIOManager.TryGetNewMapsModId(out var newMapsModId) || !(mod.Id == newMapsModId))
			{
				filteredInstalledMods.Add(mod);
			}
		}
	}

	private async Task RetrieveFavoriteMods(bool forceRefresh = false)
	{
		if (loadingFavoriteMods)
		{
			restartFavoriteModsRetrieval = true;
			restartFavoriteModsRetrievalForceRefresh = forceRefresh;
			return;
		}
		favoriteMods.Clear();
		filteredFavoriteMods.Clear();
		totalFavoriteMods = 0;
		errorLoadingFavoriteMods = false;
		loadingFavoriteMods = true;
		Error error;
		(error, favoriteMods) = await ModIOManager.GetFavoriteMods(forceRefresh);
		if (restartFavoriteModsRetrieval)
		{
			restartFavoriteModsRetrieval = false;
			RetrieveFavoriteMods(restartFavoriteModsRetrievalForceRefresh);
			restartFavoriteModsRetrievalForceRefresh = false;
			return;
		}
		if ((bool)error)
		{
			if (error.Code != ErrorCode.FILE_NOT_FOUND)
			{
				errorLoadingFavoriteMods = true;
			}
			loadingFavoriteMods = false;
			GTDev.LogError("[CustomMapsListScreen::RetrieveFavoriteMods] Failed to get Favorite mods. Error: " + error.GetMessage());
			return;
		}
		FilterFavoriteMods();
		totalFavoriteMods = filteredFavoriteMods.Count;
		loadingFavoriteMods = false;
		if (currentState == ListScreenState.FavoriteMods)
		{
			RefreshScreenState();
		}
	}

	private void FilterFavoriteMods()
	{
		filteredFavoriteMods.Clear();
		if (favoriteMods.IsNullOrEmpty())
		{
			return;
		}
		foreach (Mod favoriteMod in favoriteMods)
		{
			if (!ModIOManager.TryGetNewMapsModId(out var newMapsModId) || !(favoriteMod.Id == newMapsModId))
			{
				filteredFavoriteMods.Add(favoriteMod);
			}
		}
	}

	public void GetDisplayedModList(out long[] modList)
	{
		if (displayedModProfiles.IsNullOrEmpty())
		{
			modList = Array.Empty<long>();
			return;
		}
		modList = new long[displayedModProfiles.Count];
		for (int i = 0; i < displayedModProfiles.Count; i++)
		{
			modList[i] = displayedModProfiles[i].Id;
		}
	}

	private void RefreshScreenState()
	{
		displayedModProfiles.Clear();
		errorText.gameObject.SetActive(value: false);
		sortTypeText.gameObject.SetActive(value: false);
		modPageText.gameObject.SetActive(value: false);
		titleText.text = GetTitleForCurrentState();
		loadingText.gameObject.SetActive(value: true);
		if (CustomMapsTerminal.IsDriver && ModIOManager.IsLoggedIn())
		{
			subscribedMapsButton.gameObject.SetActive(value: true);
			subscribedMapsButton.transform.position = subscribedBttnPosition;
			searchButton.transform.position = searchBttnPosition;
		}
		else
		{
			subscribedMapsButton.gameObject.SetActive(value: false);
			subscribedMapsButton.transform.position = searchBttnPosition;
			searchButton.transform.position = subscribedBttnPosition;
		}
		if (currentState == ListScreenState.AvailableMods)
		{
			RefreshScreenForAvailableMods();
			return;
		}
		sortByButton.SetActive(value: false);
		RefreshScreenForCurrentState();
	}

	private void RefreshScreenForAvailableMods()
	{
		string text = ((sortType == SortModsBy.DateSubmitted) ? "NEWEST" : sortType.ToString().ToUpper());
		sortByButton.SetActive(value: true);
		sortTypeText.gameObject.SetActive(value: true);
		sortTypeText.text = text;
		customMapsGalleryView.ResetGallery();
		if (loadingAvailableMods)
		{
			return;
		}
		if (errorLoadingAvailableMods)
		{
			errorText.text = failedToRetrieveModsString;
			loadingText.gameObject.SetActive(value: false);
			errorText.gameObject.SetActive(value: true);
			return;
		}
		UpdatePageCount(totalAvailableMods);
		int num = 0;
		int num2 = modsPerPage - 1;
		if (!IsOnFirstPage())
		{
			num = currentModPage * modsPerPage;
			num2 = num + modsPerPage - 1;
			pageUpButton.gameObject.SetActive(value: true);
		}
		else
		{
			pageUpButton.gameObject.SetActive(value: false);
		}
		if (!IsOnLastPage())
		{
			pageDownButton.gameObject.SetActive(value: true);
		}
		else
		{
			pageDownButton.gameObject.SetActive(value: false);
		}
		if (filteredAvailableMods.Count <= num2 && totalAvailableMods > availableMods.Count)
		{
			displayedModProfiles.Clear();
			RetrieveAvailableMods();
			return;
		}
		for (int i = num; i <= num2 && filteredAvailableMods.Count > i; i++)
		{
			displayedModProfiles.Add(filteredAvailableMods[i]);
		}
		if (!customMapsGalleryView.DisplayGallery(displayedModProfiles, useMapName, out var error))
		{
			errorText.text = error;
			loadingText.gameObject.SetActive(value: false);
			errorText.gameObject.SetActive(value: true);
			return;
		}
		if (displayFeaturedMods && !featuredModIds.IsNullOrEmpty())
		{
			for (int j = 0; j < displayedModProfiles.Count; j++)
			{
				if (featuredModIds.Contains(displayedModProfiles[j].Id))
				{
					customMapsGalleryView.HighlightTileAtIndex(num + j);
				}
			}
		}
		loadingText.gameObject.SetActive(value: false);
	}

	private void RefreshScreenForCurrentState()
	{
		customMapsGalleryView.ResetGallery();
		if (GetLoadingStatusForCurrentState())
		{
			return;
		}
		if (HasModLoadingErrorForCurrentState())
		{
			modPageText.gameObject.SetActive(value: false);
			if (CustomMapsTerminal.IsDriver)
			{
				currentModPage = -1;
			}
			errorText.text = failedToRetrieveModsString;
			loadingText.gameObject.SetActive(value: false);
			errorText.gameObject.SetActive(value: true);
			return;
		}
		UpdatePageCount(GetTotalModsForCurrentState());
		if (!IsOnFirstPage())
		{
			pageUpButton.gameObject.SetActive(value: true);
		}
		else
		{
			pageUpButton.gameObject.SetActive(value: false);
		}
		if (!IsOnLastPage())
		{
			pageDownButton.gameObject.SetActive(value: true);
		}
		else
		{
			pageDownButton.gameObject.SetActive(value: false);
		}
		List<Mod> modListForCurrentState = GetModListForCurrentState();
		if (modListForCurrentState != null)
		{
			if (currentState == ListScreenState.CustomModList)
			{
				displayedModProfiles.AddRange(modListForCurrentState);
			}
			else
			{
				int num = currentModPage * modsPerPage;
				for (int i = num; i < num + modsPerPage && modListForCurrentState.Count > i; i++)
				{
					displayedModProfiles.Add(modListForCurrentState[i]);
				}
			}
		}
		if (!customMapsGalleryView.DisplayGallery(displayedModProfiles, useMapName: true, out var error))
		{
			errorText.text = error;
			loadingText.gameObject.SetActive(value: false);
			errorText.gameObject.SetActive(value: true);
		}
		else
		{
			loadingText.gameObject.SetActive(value: false);
		}
	}

	private bool GetLoadingStatusForCurrentState()
	{
		if (ModIOManager.IsRefreshing())
		{
			return true;
		}
		return currentState switch
		{
			ListScreenState.AvailableMods => loadingAvailableMods, 
			ListScreenState.InstalledMods => loadingInstalledMods, 
			ListScreenState.FavoriteMods => loadingFavoriteMods, 
			ListScreenState.SubscribedMods => loadingSubscribedMods, 
			_ => false, 
		};
	}

	private bool HasModLoadingErrorForCurrentState()
	{
		return currentState switch
		{
			ListScreenState.AvailableMods => errorLoadingAvailableMods, 
			ListScreenState.InstalledMods => errorLoadingInstalledMods, 
			ListScreenState.FavoriteMods => errorLoadingFavoriteMods, 
			ListScreenState.SubscribedMods => errorLoadingSubscribedMods, 
			_ => false, 
		};
	}

	private List<Mod> GetModListForCurrentState()
	{
		return currentState switch
		{
			ListScreenState.AvailableMods => filteredAvailableMods, 
			ListScreenState.InstalledMods => filteredInstalledMods, 
			ListScreenState.FavoriteMods => filteredFavoriteMods, 
			ListScreenState.SubscribedMods => filteredSubscribedMods, 
			_ => null, 
		};
	}

	private int GetTotalModsForCurrentState()
	{
		return currentState switch
		{
			ListScreenState.AvailableMods => totalAvailableMods, 
			ListScreenState.InstalledMods => totalInstalledMods, 
			ListScreenState.FavoriteMods => totalFavoriteMods, 
			ListScreenState.SubscribedMods => totalSubscribedMods, 
			_ => 0, 
		};
	}

	private string GetTitleForCurrentState()
	{
		switch (currentState)
		{
		case ListScreenState.AvailableMods:
			if (officialMapsOnly)
			{
				return officialModsTitle;
			}
			return browseModsTitle;
		case ListScreenState.InstalledMods:
			return installedModsTitle;
		case ListScreenState.FavoriteMods:
			return favoriteModsTitle;
		case ListScreenState.SubscribedMods:
			return subscribedModsTitle;
		default:
			return "";
		}
	}

	private void UpdatePageCount(int totalMods)
	{
		totalModCount = totalMods;
		modPageText.gameObject.SetActive(value: false);
		if (totalModCount == 0)
		{
			switch (currentState)
			{
			case ListScreenState.AvailableMods:
				errorText.text = noModsAvailableString;
				break;
			case ListScreenState.InstalledMods:
				errorText.text = noInstalledModsString;
				break;
			case ListScreenState.FavoriteMods:
				errorText.text = noFavoriteModsString;
				break;
			case ListScreenState.SubscribedMods:
				errorText.text = noSubscribedModsString;
				break;
			case ListScreenState.CustomModList:
				errorText.text = noModsFoundGenericString;
				break;
			}
		}
		else
		{
			int numPages = GetNumPages();
			if (numPages > 1)
			{
				modPageText.text = $"{currentModPage + 1} / {numPages}";
				modPageText.gameObject.SetActive(value: true);
			}
		}
	}

	public int GetNumPages()
	{
		int num = totalModCount % modsPerPage;
		int num2 = totalModCount / modsPerPage;
		if (num > 0)
		{
			num2++;
		}
		return num2;
	}

	private bool IsOnFirstPage()
	{
		return currentModPage == 0;
	}

	private bool IsOnLastPage()
	{
		long num = GetNumPages();
		if (currentModPage + 1 == num)
		{
			return true;
		}
		return false;
	}

	public void RefreshDriverNickname(string driverNickname)
	{
		if (currentState == ListScreenState.CustomModList)
		{
			titleText.text = driverNickname;
		}
	}
}

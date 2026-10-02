using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GorillaExtensions;
using GorillaNetworking;
using GorillaTagScripts.VirtualStumpCustomMaps.UI;
using Modio.Customizations;
using Modio.Mods;
using TMPro;
using UnityEngine;

public class CustomMapsFeaturedScreen : CustomMapsTerminalScreen
{
	[Serializable]
	public class FeaturedRowView
	{
		public TMP_Text titleText;

		public TMP_Text pageText;

		public CustomMapsGalleryView gallery;

		public GameObject leftPageButton;

		public GameObject rightPageButton;
	}

	[Serializable]
	public class FallbackRow
	{
		public string title = "TRENDING";

		public string sort = "-downloads_today";

		public string timespan = "all_time";

		public int total = 20;
	}

	private class RowState
	{
		public GamePlacement placement;

		public bool playerCountRow;

		public GamePlacement replacedPlacement;

		public PlayerCountSort.Ranking ranking;

		public readonly List<Mod> mods = new List<Mod>();

		public int page;

		public bool loading;

		public bool error;
	}

	[SerializeField]
	private TMP_Text loadingText;

	[SerializeField]
	private TMP_Text errorText;

	[SerializeField]
	private GameObject browseMoreButton;

	[SerializeField]
	private FeaturedRowView[] rows = new FeaturedRowView[3];

	[Tooltip("Tiles shown per row page. Must match the number of tiles in each row gallery.")]
	[SerializeField]
	private int modsPerRow = 4;

	[Tooltip("Upper bound for the number of mods requested per row, regardless of the placement's configured total.")]
	[SerializeField]
	private int maxModsPerRow = 20;

	[SerializeField]
	private bool useMapName = true;

	[SerializeField]
	private string mostPlayersRowTitle = "MOST PLAYERS";

	[Tooltip("How long the front page waits on the feature flags to decide whether the top row is the MOST PLAYERS row.")]
	[SerializeField]
	private float featureFlagsTimeoutSeconds = 5f;

	[SerializeField]
	private string rowLoadingString = "LOADING...";

	[SerializeField]
	private string rowErrorString = "FAILED TO LOAD";

	[SerializeField]
	private string noModsAvailableString = "NO MAPS AVAILABLE";

	[SerializeField]
	private string failedToRetrieveModsString = "FAILED TO RETRIEVE MAPS FROM MOD.IO \nPRESS THE 'REFRESH' BUTTON TO RETRY";

	[Tooltip("Used when mod.io returns no enabled placements (or the request fails). Mirrors the default mod.io Featured page rows.")]
	[SerializeField]
	private FallbackRow[] fallbackRows = new FallbackRow[3]
	{
		new FallbackRow
		{
			title = "TRENDING",
			sort = "-downloads_today",
			timespan = "all_time"
		},
		new FallbackRow
		{
			title = "TOP RATED",
			sort = "-ratings_weighted_aggregate",
			timespan = "year"
		},
		new FallbackRow
		{
			title = "RECENTLY ADDED",
			sort = "-date_live",
			timespan = "all_time"
		}
	};

	private RowState[] rowStates = Array.Empty<RowState>();

	private bool loadingPlacements;

	private bool placementsLoaded;

	private bool usingFallbackRows;

	private int loadRequestId;

	public int RowCount
	{
		get
		{
			FeaturedRowView[] array = rows;
			if (array == null)
			{
				return 0;
			}
			return array.Length;
		}
	}

	public int ModsPerRow => modsPerRow;

	public override void Initialize()
	{
	}

	public override void Show()
	{
		base.Show();
		ModIOManager.OnModIOCacheRefreshing.RemoveListener(OnModCacheRefreshing);
		ModIOManager.OnModIOCacheRefreshing.AddListener(OnModCacheRefreshing);
		ModIOManager.OnModIOCacheRefreshed.RemoveListener(OnModCacheRefreshed);
		ModIOManager.OnModIOCacheRefreshed.AddListener(OnModCacheRefreshed);
		if (!browseMoreButton.IsNull())
		{
			browseMoreButton.SetActive(value: true);
		}
		if (!placementsLoaded && !loadingPlacements)
		{
			LoadFeaturedRows(forceRefresh: false);
		}
		else if (placementsLoaded)
		{
			for (int i = 0; i < rowStates.Length; i++)
			{
				RerankIfStale(i);
			}
		}
		RefreshScreenState();
	}

	public override void Hide()
	{
		base.Hide();
		ModIOManager.OnModIOCacheRefreshing.RemoveListener(OnModCacheRefreshing);
		ModIOManager.OnModIOCacheRefreshed.RemoveListener(OnModCacheRefreshed);
	}

	private void OnModCacheRefreshing()
	{
		RefreshScreenState();
	}

	private void OnModCacheRefreshed()
	{
		RefreshScreenState();
	}

	public override void PressButton(CustomMapKeyboardBinding buttonPressed)
	{
		if (Time.time < showTime + activationTime)
		{
			return;
		}
		GTDev.Log("[CustomMapsFeaturedScreen::PressButton] Is Driver: " + CustomMapsTerminal.IsDriver + ", Button Pressed: " + buttonPressed);
		if (!CustomMapsTerminal.IsDriver)
		{
			return;
		}
		if (buttonPressed == CustomMapKeyboardBinding.browse)
		{
			CustomMapsTerminal.ShowListScreen();
		}
		else
		{
			if (loadingPlacements || loadingText.gameObject.activeSelf)
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
				return;
			case CustomMapKeyboardBinding.row1Left:
				ChangeRowPage(0, -1);
				return;
			case CustomMapKeyboardBinding.row1Right:
				ChangeRowPage(0, 1);
				return;
			case CustomMapKeyboardBinding.row2Left:
				ChangeRowPage(1, -1);
				return;
			case CustomMapKeyboardBinding.row2Right:
				ChangeRowPage(1, 1);
				return;
			case CustomMapKeyboardBinding.row3Left:
				ChangeRowPage(2, -1);
				return;
			case CustomMapKeyboardBinding.row3Right:
				ChangeRowPage(2, 1);
				return;
			}
			if (TryGetTileIndex(buttonPressed, out var tileIndex))
			{
				int num = tileIndex / Mathf.Max(1, modsPerRow);
				int entryIndex = tileIndex % Mathf.Max(1, modsPerRow);
				if (num < rows.Length && num < rowStates.Length && !rows[num].gallery.IsNull())
				{
					rows[num].gallery.ShowDetailsForEntry(entryIndex);
				}
			}
		}
	}

	private static bool TryGetTileIndex(CustomMapKeyboardBinding binding, out int tileIndex)
	{
		if (binding >= CustomMapKeyboardBinding.tile1 && binding <= CustomMapKeyboardBinding.tile12)
		{
			tileIndex = (int)(binding - 62);
			return true;
		}
		tileIndex = -1;
		return false;
	}

	public void Refresh()
	{
		if (!loadingPlacements)
		{
			placementsLoaded = false;
			LoadFeaturedRows(forceRefresh: true);
		}
	}

	private void ChangeRowPage(int rowIndex, int delta)
	{
		if (rowIndex < 0 || rowIndex >= rowStates.Length)
		{
			return;
		}
		RowState rowState = rowStates[rowIndex];
		if (rowState.loading || rowState.error)
		{
			return;
		}
		int numPages = GetNumPages(rowState);
		if (numPages > 1)
		{
			rowState.page = Mathf.Clamp(rowState.page + delta, 0, numPages - 1);
			if (!RerankIfStale(rowIndex))
			{
				RefreshRow(rowIndex);
			}
		}
	}

	private async void LoadFeaturedRows(bool forceRefresh)
	{
		int requestId = ++loadRequestId;
		loadingPlacements = true;
		usingFallbackRows = false;
		RefreshScreenState();
		var (error, array) = await ModIOManager.GetGamePlacements(forceRefresh);
		if (requestId != loadRequestId)
		{
			return;
		}
		List<GamePlacement> selectedPlacements = new List<GamePlacement>();
		if (!error && !array.IsNullOrEmpty())
		{
			selectedPlacements = (from p in array
				where p != null && p.Enabled && p.IsFilterPlacement
				orderby p.DisplayPosition
				select p).Take(rows.Length).ToList();
		}
		else if ((bool)error)
		{
			GTDev.LogError("[CustomMapsFeaturedScreen::LoadFeaturedRows] Failed to retrieve featured placements from mod.io, falling back to the default rows. Error: " + error.GetMessage());
		}
		if (selectedPlacements.Count == 0 && !fallbackRows.IsNullOrEmpty())
		{
			usingFallbackRows = true;
			for (int num = 0; num < fallbackRows.Length && num < rows.Length; num++)
			{
				FallbackRow fallbackRow = fallbackRows[num];
				selectedPlacements.Add(GamePlacement.CreateLocal(fallbackRow.title, fallbackRow.sort, fallbackRow.timespan, fallbackRow.total, num + 1));
			}
		}
		GTDev.Log($"[CustomMapsFeaturedScreen::LoadFeaturedRows] Showing {selectedPlacements.Count} featured rows" + (usingFallbackRows ? " (fallback)" : "") + ": " + string.Join(", ", selectedPlacements.Select((GamePlacement p) => p.Name + " [" + p.Sort + " " + p.Timespan + "]")));
		bool flag = await IsMostPlayersRowEnabled();
		if (requestId != loadRequestId)
		{
			return;
		}
		rowStates = selectedPlacements.Select((GamePlacement p) => new RowState
		{
			placement = p,
			loading = true
		}).ToArray();
		if (flag && rows.Length != 0)
		{
			RowState rowState = new RowState
			{
				placement = GamePlacement.CreateLocal(mostPlayersRowTitle, null, null, maxModsPerRow, 1),
				playerCountRow = true,
				replacedPlacement = ((rowStates.Length != 0) ? rowStates[0].placement : null),
				loading = true
			};
			if (rowStates.Length != 0)
			{
				rowStates[0] = rowState;
			}
			else
			{
				rowStates = new RowState[1] { rowState };
			}
		}
		loadingPlacements = false;
		RefreshScreenState();
		List<Task> list = new List<Task>(rowStates.Length);
		for (int num2 = 0; num2 < rowStates.Length; num2++)
		{
			list.Add(LoadRowMods(num2, requestId, forceRefresh));
		}
		await Task.WhenAll(list);
		if (requestId == loadRequestId)
		{
			placementsLoaded = true;
		}
	}

	private async Task LoadRowMods(int rowIndex, int requestId, bool forceRefresh)
	{
		RowState state = rowStates[rowIndex];
		GamePlacement placement = state.placement;
		int requestCount = GetRequestCount(placement);
		if (state.playerCountRow)
		{
			await LoadPlayerCountRowMods(rowIndex, requestId, requestCount, forceRefresh);
			return;
		}
		ModSearchFilter modSearchFilter = new ModSearchFilter(0, requestCount);
		if (UGCPermissionManager.FeaturedMapsOnly)
		{
			modSearchFilter.AddTag("Featured");
		}
		var (error, modioPage) = await ModIOManager.GetPlacementMods(placement, modSearchFilter, forceRefresh);
		if (requestId != loadRequestId || rowIndex >= rowStates.Length || rowStates[rowIndex] != state)
		{
			return;
		}
		state.mods.Clear();
		state.page = 0;
		state.loading = false;
		state.error = false;
		if ((bool)error || modioPage == null)
		{
			state.error = true;
			GTDev.LogError("[CustomMapsFeaturedScreen::LoadRowMods] Failed to retrieve mods for featured row '" + placement.Name + "'. Error: " + error.GetMessage());
		}
		else if (modioPage.Data != null)
		{
			ModIOManager.TryGetNewMapsModId(out var newMapsModId);
			Mod[] data = modioPage.Data;
			foreach (Mod mod in data)
			{
				if (mod != null && !(mod.Id == newMapsModId))
				{
					state.mods.Add(mod);
				}
			}
		}
		if (base.gameObject.activeInHierarchy)
		{
			RefreshRow(rowIndex);
		}
	}

	private async Task<bool> IsMostPlayersRowEnabled()
	{
		float waitStart = Time.realtimeSinceStartup;
		while (true)
		{
			GorillaServer instance = GorillaServer.Instance;
			if ((object)instance == null || instance.FeatureFlagsReady)
			{
				break;
			}
			if (Time.realtimeSinceStartup - waitStart > featureFlagsTimeoutSeconds)
			{
				GTDev.LogWarning("[CustomMapsFeaturedScreen::IsMostPlayersRowEnabled] Timed out waiting on the feature flags; showing the mod.io rows.");
				return false;
			}
			await Task.Delay(100);
		}
		try
		{
			return GorillaServer.Instance != null && GorillaServer.Instance.CheckMostPlayersFrontPageRowEnabled();
		}
		catch (Exception ex)
		{
			GTDev.LogError("[CustomMapsFeaturedScreen::IsMostPlayersRowEnabled] Failed to check the feature flag, showing the mod.io rows: " + ex.Message);
			return false;
		}
	}

	private int GetRequestCount(GamePlacement placement)
	{
		return Mathf.Clamp((placement.Total > 0) ? placement.Total : maxModsPerRow, modsPerRow, maxModsPerRow);
	}

	private bool RerankIfStale(int rowIndex)
	{
		if (rowIndex < 0 || rowIndex >= rowStates.Length)
		{
			return false;
		}
		RowState rowState = rowStates[rowIndex];
		if (!rowState.playerCountRow || rowState.loading || rowState.ranking == null || !rowState.ranking.IsStale)
		{
			return false;
		}
		rowState.loading = true;
		RefreshRow(rowIndex);
		LoadPlayerCountRowMods(rowIndex, loadRequestId, GetRequestCount(rowState.placement), forceRefresh: false, reRank: true);
		return true;
	}

	private async Task LoadPlayerCountRowMods(int rowIndex, int requestId, int requestCount, bool forceRefresh, bool reRank = false)
	{
		RowState state = rowStates[rowIndex];
		(bool, PlayerCountSort.Ranking) obj = await PlayerCountSort.GetModsByPlayerCount(null, forceRefresh);
		bool item = obj.Item1;
		PlayerCountSort.Ranking item2 = obj.Item2;
		List<Mod> mods = item2.Mods;
		if (requestId != loadRequestId || rowIndex >= rowStates.Length || rowStates[rowIndex] != state)
		{
			return;
		}
		if (reRank && (!item || mods.Count == 0))
		{
			GTDev.LogWarning("[CustomMapsFeaturedScreen::LoadPlayerCountRowMods] Couldn't re-rank maps by player count, keeping the previous ranking.");
			state.loading = false;
			if (base.gameObject.activeInHierarchy)
			{
				RefreshRow(rowIndex);
			}
			return;
		}
		if ((!item || mods.Count == 0) && state.replacedPlacement != null)
		{
			GTDev.LogWarning("[CustomMapsFeaturedScreen::LoadPlayerCountRowMods] Couldn't rank maps by player count, falling back to the '" + state.replacedPlacement.Name + "' row.");
			state.placement = state.replacedPlacement;
			state.replacedPlacement = null;
			state.playerCountRow = false;
			if (base.gameObject.activeInHierarchy)
			{
				RefreshRow(rowIndex);
			}
			await LoadRowMods(rowIndex, requestId, forceRefresh);
			return;
		}
		state.mods.Clear();
		if (!reRank)
		{
			state.page = 0;
		}
		state.loading = false;
		state.error = !item;
		state.ranking = (item ? item2 : null);
		if (item)
		{
			state.mods.AddRange(mods.Take(requestCount));
		}
		if (base.gameObject.activeInHierarchy)
		{
			RefreshRow(rowIndex);
		}
	}

	private void RefreshScreenState()
	{
		errorText.gameObject.SetActive(value: false);
		if (loadingPlacements || ModIOManager.IsRefreshing())
		{
			loadingText.gameObject.SetActive(value: true);
			for (int i = 0; i < rows.Length; i++)
			{
				HideRow(i);
			}
			return;
		}
		loadingText.gameObject.SetActive(value: false);
		if (rowStates.Length == 0)
		{
			for (int j = 0; j < rows.Length; j++)
			{
				HideRow(j);
			}
			errorText.text = failedToRetrieveModsString;
			errorText.gameObject.SetActive(value: true);
			return;
		}
		for (int k = 0; k < rows.Length; k++)
		{
			if (k < rowStates.Length)
			{
				RefreshRow(k);
			}
			else
			{
				HideRow(k);
			}
		}
	}

	private void HideRow(int rowIndex)
	{
		FeaturedRowView featuredRowView = rows[rowIndex];
		if (featuredRowView != null)
		{
			if (!featuredRowView.titleText.IsNull())
			{
				featuredRowView.titleText.gameObject.SetActive(value: false);
			}
			if (!featuredRowView.pageText.IsNull())
			{
				featuredRowView.pageText.gameObject.SetActive(value: false);
			}
			if (!featuredRowView.leftPageButton.IsNull())
			{
				featuredRowView.leftPageButton.SetActive(value: false);
			}
			if (!featuredRowView.rightPageButton.IsNull())
			{
				featuredRowView.rightPageButton.SetActive(value: false);
			}
			if (!featuredRowView.gallery.IsNull())
			{
				featuredRowView.gallery.ResetGallery();
			}
		}
	}

	private void RefreshRow(int rowIndex)
	{
		if (rowIndex < 0 || rowIndex >= rows.Length || rowIndex >= rowStates.Length)
		{
			return;
		}
		FeaturedRowView featuredRowView = rows[rowIndex];
		RowState rowState = rowStates[rowIndex];
		if (featuredRowView == null || rowState == null)
		{
			return;
		}
		if (!featuredRowView.titleText.IsNull())
		{
			featuredRowView.titleText.text = rowState.placement.GetTitle().ToUpperInvariant();
			featuredRowView.titleText.gameObject.SetActive(value: true);
		}
		if (!featuredRowView.gallery.IsNull())
		{
			featuredRowView.gallery.ResetGallery();
		}
		if (rowState.loading)
		{
			SetRowPageText(featuredRowView, rowLoadingString, visible: true);
			SetRowArrows(featuredRowView, showLeft: false, showRight: false);
			return;
		}
		if (rowState.error)
		{
			SetRowPageText(featuredRowView, rowErrorString, visible: true);
			SetRowArrows(featuredRowView, showLeft: false, showRight: false);
			return;
		}
		if (rowState.mods.Count == 0)
		{
			SetRowPageText(featuredRowView, noModsAvailableString, visible: true);
			SetRowArrows(featuredRowView, showLeft: false, showRight: false);
			return;
		}
		int numPages = GetNumPages(rowState);
		rowState.page = Mathf.Clamp(rowState.page, 0, numPages - 1);
		SetRowPageText(featuredRowView, $"{rowState.page + 1}/{numPages}", numPages > 1);
		SetRowArrows(featuredRowView, rowState.page > 0, rowState.page < numPages - 1);
		if (!featuredRowView.gallery.IsNull())
		{
			int num = rowState.page * modsPerRow;
			int b = Mathf.Min(modsPerRow, rowState.mods.Count - num);
			List<Mod> range = rowState.mods.GetRange(num, Mathf.Max(0, b));
			IReadOnlyDictionary<string, ulong> knownPlayerCounts = ((!rowState.playerCountRow) ? null : rowState.ranking?.PlayerCounts);
			if (!featuredRowView.gallery.DisplayGallery(range, useMapName, out var error, knownPlayerCounts))
			{
				errorText.text = error;
				errorText.gameObject.SetActive(value: true);
			}
		}
	}

	private static void SetRowPageText(FeaturedRowView view, string text, bool visible)
	{
		if (!view.pageText.IsNull())
		{
			view.pageText.text = text;
			view.pageText.gameObject.SetActive(visible);
		}
	}

	private static void SetRowArrows(FeaturedRowView view, bool showLeft, bool showRight)
	{
		if (!view.leftPageButton.IsNull())
		{
			view.leftPageButton.SetActive(showLeft);
		}
		if (!view.rightPageButton.IsNull())
		{
			view.rightPageButton.SetActive(showRight);
		}
	}

	private int GetNumPages(RowState state)
	{
		if (state == null || modsPerRow <= 0)
		{
			return 0;
		}
		int num = state.mods.Count / modsPerRow;
		if (state.mods.Count % modsPerRow > 0)
		{
			num++;
		}
		return num;
	}
}

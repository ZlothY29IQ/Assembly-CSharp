using System;
using System.Text;
using TMPro;
using UnityEngine;

namespace GorillaTagScripts.Builder;

public class SharedBlocksScreenSearch : SharedBlocksScreen, IGorillaSliceableSimple
{
	[SerializeField]
	private TMP_Text loadedMap;

	[SerializeField]
	private TMP_Text inputText;

	[SerializeField]
	private TMP_Text statusText;

	[SerializeField]
	private TMP_Text recentList;

	[SerializeField]
	private TMP_Text myScanList;

	[SerializeField]
	private TMP_Text playerCountText;

	[SerializeField]
	private TMP_Text playersInLobbyWarning;

	private string currentMapCode;

	private string savedMapCode;

	private StringBuilder sb = new StringBuilder();

	private bool updating;

	public override void OnSelectPressed()
	{
		if (SharedBlocksManager.IsMapIDValid(currentMapCode))
		{
			savedMapCode = currentMapCode;
			terminal.SelectMapIDAndOpenInfo(savedMapCode);
		}
		else if (currentMapCode.Length < 8)
		{
			if (!LocalisationManager.TryGetKeyForCurrentLocale("SHARE_BLOCKS_TERMINAL_SEARCH_ERROR_INVALID_LENGTH", out var result, "INVALID MAP ID LENGTH"))
			{
				Debug.LogError("[LOCALIZATION::BUILDER_SCAN_KIOSK] Failed to get key for SHARE MY BLOCKS SEARCH TERMINAL localization [SHARE_BLOCKS_TERMINAL_SEARCH_ERROR_INVALID_LENGTH]");
			}
			terminal.SetStatusText(result);
		}
		else
		{
			if (!LocalisationManager.TryGetKeyForCurrentLocale("SHARE_BLOCKS_TERMINAL_SEARCH_ERROR_INVALID_ID", out var result2, "INVALID MAP ID"))
			{
				Debug.LogError("[LOCALIZATION::BUILDER_SCAN_KIOSK] Failed to get key for SHARE MY BLOCKS SEARCH TERMINAL localization [SHARE_BLOCKS_TERMINAL_SEARCH_ERROR_INVALID_ID]");
			}
			terminal.SetStatusText(result2);
		}
	}

	public override void OnDeletePressed()
	{
		if (currentMapCode.Length > 0)
		{
			currentMapCode = currentMapCode.Substring(0, currentMapCode.Length - 1);
			UpdateInput();
		}
	}

	public override void OnNumberPressed(int number)
	{
		if (currentMapCode.Length < 8)
		{
			currentMapCode += number;
			UpdateInput();
		}
	}

	public override void OnLetterPressed(string letter)
	{
		if (currentMapCode.Length < 8)
		{
			currentMapCode += letter;
			UpdateInput();
		}
	}

	public override void Show()
	{
		SharedBlocksManager.OnRecentMapIdsUpdated += DrawScreen;
		currentMapCode = string.Empty;
		DrawScreen();
		base.Show();
		RefreshPlayerCounter();
		BuilderTable table = terminal.GetTable();
		if (table != null)
		{
			table.OnMapLoaded.AddListener(OnMapLoaded);
			table.OnMapCleared.AddListener(OnMapCleared);
			OnMapLoaded(table.GetCurrentMapID());
		}
	}

	public override void Hide()
	{
		BuilderTable table = terminal.GetTable();
		if (table != null)
		{
			table.OnMapLoaded.RemoveListener(OnMapLoaded);
			table.OnMapCleared.RemoveListener(OnMapCleared);
		}
		statusText.text = "";
		statusText.gameObject.SetActive(value: false);
		SharedBlocksManager.OnRecentMapIdsUpdated -= DrawScreen;
		base.Hide();
	}

	private void OnMapLoaded(string mapID)
	{
		string defaultResult = "LOADED MAP : " + (SharedBlocksManager.IsMapIDValid(mapID) ? SharedBlocksTerminal.MapIDToDisplayedString(mapID) : "NONE");
		if (!LocalisationManager.TryGetKeyForCurrentLocale(SharedBlocksManager.IsMapIDValid(mapID) ? "SHARE_BLOCKS_TERMINAL_SEARCH_LOADED_LABEL" : "SHARE_BLOCKS_TERMINAL_SEARCH_LOADED_NONE", out var result, defaultResult))
		{
			Debug.LogError("[LOCALIZATION::BUILDER_SCAN_KIOSK] Failed to get key for SHARE MY BLOCKS SEARCH TERMINAL localization [SHARE_BLOCKS_TERMINAL_SEARCH_LOADED_LABEL]");
		}
		result = result.Replace("{mapDisplayName}", SharedBlocksTerminal.MapIDToDisplayedString(mapID));
		loadedMap.text = result;
	}

	private void OnMapCleared()
	{
		if (!LocalisationManager.TryGetKeyForCurrentLocale("SHARE_BLOCKS_TERMINAL_SEARCH_LOADED_NONE", out var result, "LOADED MAP : NONE"))
		{
			Debug.LogError("[LOCALIZATION::BUILDER_SCAN_KIOSK] Failed to get key for SHARE MY BLOCKS SEARCH TERMINAL localization [SHARE_BLOCKS_TERMINAL_SEARCH_LOADED_NONE]");
		}
		loadedMap.text = result;
	}

	private void UpdateInput()
	{
		if (!ApplicationQuittingState.IsQuitting)
		{
			string defaultResult = "MAP SEARCH : ";
			if (!LocalisationManager.TryGetKeyForCurrentLocale("SHARE_BLOCKS_TERMINAL_SEARCH_MAP_SEARCH", out var result, defaultResult))
			{
				Debug.LogError("[LOCALIZATION::BUILDER_SCAN_KIOSK] Failed to get key for SHARE MY BLOCKS SEARCH TERMINAL localization [SHARE_BLOCKS_TERMINAL_SEARCH_MAP_SEARCH]");
			}
			result += SharedBlocksTerminal.MapIDToDisplayedString(currentMapCode);
			inputText.text = result;
		}
	}

	public void SetMapCode(string mapCode)
	{
		if (mapCode == null)
		{
			currentMapCode = string.Empty;
		}
		else
		{
			currentMapCode = mapCode;
		}
		UpdateInput();
	}

	public void SetInputTextEnabled(bool enabled)
	{
		if (enabled)
		{
			inputText.color = Color.white;
		}
		else
		{
			inputText.color = Color.gray;
		}
	}

	private void DrawScreen()
	{
		if (ApplicationQuittingState.IsQuitting)
		{
			return;
		}
		UpdateInput();
		if (!LocalisationManager.TryGetKeyForCurrentLocale("SHARE_BLOCKS_TERMINAL_SEARCH_VOTES", out var result, "RECENT VOTES"))
		{
			Debug.LogError("[LOCALIZATION::BUILDER_SCAN_KIOSK] Failed to get key for SHARE MY BLOCKS SEARCH TERMINAL localization [SHARE_BLOCKS_TERMINAL_SEARCH_VOTES]");
		}
		sb.Clear();
		sb.Append(result + "\n");
		foreach (string recentUpVote in SharedBlocksManager.GetRecentUpVotes())
		{
			if (SharedBlocksManager.IsMapIDValid(recentUpVote))
			{
				sb.Append(SharedBlocksTerminal.MapIDToDisplayedString(recentUpVote));
				sb.Append("\n");
			}
		}
		recentList.text = sb.ToString();
		if (!LocalisationManager.TryGetKeyForCurrentLocale("SHARE_BLOCKS_TERMINAL_SEARCH_MAPS_LABEL", out result, "MY MAPS"))
		{
			Debug.LogError("[LOCALIZATION::BUILDER_SCAN_KIOSK] Failed to get key for SHARE MY BLOCKS SEARCH TERMINAL localization [SHARE_BLOCKS_TERMINAL_SEARCH_MAPS_LABEL]");
		}
		sb.Clear();
		sb.Append(result + "\n");
		foreach (string localMapID in SharedBlocksManager.GetLocalMapIDs())
		{
			if (SharedBlocksManager.IsMapIDValid(localMapID))
			{
				sb.Append(SharedBlocksTerminal.MapIDToDisplayedString(localMapID));
				sb.Append("\n");
			}
		}
		myScanList.text = sb.ToString();
	}

	private void RefreshPlayerCounter()
	{
		terminal.RefreshLobbyCount();
		playerCountText.text = terminal.GetLobbyText();
		playersInLobbyWarning.gameObject.SetActive(!terminal.AreAllPlayersInLobby());
	}

	public void SliceUpdate()
	{
		RefreshPlayerCounter();
	}

	public void OnEnable()
	{
		if (!updating)
		{
			GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
			updating = true;
		}
		RefreshPlayerCounter();
		RoomSystem.PlayersChangedEvent += new Action(PlayersChangedEvent);
	}

	private void PlayersChangedEvent()
	{
		RefreshPlayerCounter();
	}

	public void OnDisable()
	{
		if (updating)
		{
			GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
			updating = false;
		}
		RoomSystem.PlayersChangedEvent -= new Action(PlayersChangedEvent);
	}
}

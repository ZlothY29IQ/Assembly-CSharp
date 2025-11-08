using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

namespace GorillaTagScripts.Builder;

public class SharedBlocksVotingStation : MonoBehaviour
{
	public const int VOTING_STATUS_INDEX_SUCCESS = 0;

	public const int VOTING_STATUS_INDEX_NOT_LOGGED_IN = 1;

	public const int VOTING_STATUS_INDEX_EMPTY = 2;

	private const int MAP_DISPLAY_INDEX_NONE = 0;

	private const int MAP_DISPLAY_INDEX_NAMED_MAP = 1;

	[SerializeField]
	private TMP_Text screenText;

	[SerializeField]
	private TMP_Text statusText;

	[SerializeField]
	private GorillaPressableButton upVoteButton;

	[SerializeField]
	private GorillaPressableButton downVoteButton;

	[SerializeField]
	private GTZone tableZone = GTZone.monkeBlocksShared;

	[SerializeField]
	private Material buttonDefaultMaterial;

	[SerializeField]
	private Material buttonDisabledMaterial;

	[Header("Localization Setup")]
	[SerializeField]
	private LocalizedText _statusLocText;

	[SerializeField]
	private LocalizedText _screenLocText;

	private BuilderTable table;

	private string loadedMapID = string.Empty;

	private bool voteInProgress;

	private bool waitingToClearStatus;

	private float clearStatusTime;

	private float clearStatusDelay = 2f;

	private IntVariable _statusIndexVar;

	private IntVariable _mapDisplayIndexVar;

	private StringVariable _mapNameVar;

	private List<MeshRenderer> meshes = new List<MeshRenderer>(12);

	private void Start()
	{
		SetupLocalization();
		if (BuilderTable.TryGetBuilderTableForZone(tableZone, out var builderTable))
		{
			table = builderTable;
			table.OnMapLoaded.AddListener(OnLoadedMapChanged);
			table.OnMapCleared.AddListener(OnMapCleared);
			OnLoadedMapChanged(table.GetCurrentMapID());
		}
		else
		{
			GTDev.LogWarning("No Builder Table found for Voting Station");
		}
		GetComponentsInChildren(includeInactive: false, meshes);
		upVoteButton.onPressButton.AddListener(OnUpVotePressed);
		downVoteButton.onPressButton.AddListener(OnDownVotePressed);
		ZoneManagement instance = ZoneManagement.instance;
		instance.onZoneChanged = (Action)Delegate.Combine(instance.onZoneChanged, new Action(OnZoneChanged));
		OnZoneChanged();
	}

	private void OnDestroy()
	{
		upVoteButton.onPressButton.RemoveListener(OnUpVotePressed);
		downVoteButton.onPressButton.RemoveListener(OnDownVotePressed);
		if (table != null)
		{
			table.OnMapLoaded.RemoveListener(OnLoadedMapChanged);
			table.OnMapCleared.RemoveListener(OnMapCleared);
		}
		if (ZoneManagement.instance != null)
		{
			ZoneManagement instance = ZoneManagement.instance;
			instance.onZoneChanged = (Action)Delegate.Remove(instance.onZoneChanged, new Action(OnZoneChanged));
		}
	}

	private void SetupLocalization()
	{
		if (_statusLocText == null)
		{
			Debug.LogError("[LOCALIZATION::SHARED_BLOCKS_VOTING_STATION] Trying to set up Localization, but [_statusLocText] is NULL");
			return;
		}
		if (_screenLocText == null)
		{
			Debug.LogError("[LOCALIZATION::SHARED_BLOCKS_VOTING_STATION] Trying to set up Localization, but [_screenLocText] is NULL");
			return;
		}
		string text = "voting-status-index";
		string text2 = "map-name-index";
		string text3 = "map-name";
		_statusIndexVar = _statusLocText.StringReference[text] as IntVariable;
		_mapDisplayIndexVar = _screenLocText.StringReference[text2] as IntVariable;
		_mapNameVar = _screenLocText.StringReference[text3] as StringVariable;
		if (_statusIndexVar == null)
		{
			Debug.LogError("[LOCALIZATION::SHARED_BLOCKS_VOTING_STATION] Failed to find [IntVariable] with var-name [" + text + "]");
		}
		if (_mapDisplayIndexVar == null)
		{
			Debug.LogError("[LOCALIZATION::SHARED_BLOCKS_VOTING_STATION] Failed to find [IntVariable] with var-name [" + text2 + "]");
		}
		if (_mapNameVar == null)
		{
			Debug.LogError("[LOCALIZATION::SHARED_BLOCKS_VOTING_STATION] Failed to find [StringVariable] with var-name [" + text3 + "]");
		}
	}

	private void OnZoneChanged()
	{
		bool flag = ZoneManagement.instance.IsZoneActive(tableZone);
		foreach (MeshRenderer mesh in meshes)
		{
			mesh.enabled = flag;
		}
	}

	private void OnUpVotePressed()
	{
		if (!voteInProgress)
		{
			voteInProgress = true;
			_statusIndexVar.Value = 2;
			statusText.gameObject.SetActive(value: false);
			if (SharedBlocksManager.IsMapIDValid(loadedMapID) && upVoteButton.enabled)
			{
				SharedBlocksManager.instance.RequestVote(loadedMapID, up: true, OnVoteResponse);
				upVoteButton.buttonRenderer.material = upVoteButton.pressedMaterial;
				downVoteButton.buttonRenderer.material = buttonDefaultMaterial;
				upVoteButton.enabled = false;
				downVoteButton.enabled = true;
			}
		}
	}

	private void OnDownVotePressed()
	{
		if (!voteInProgress)
		{
			voteInProgress = true;
			_statusIndexVar.Value = 2;
			statusText.gameObject.SetActive(value: false);
			if (SharedBlocksManager.IsMapIDValid(loadedMapID) && downVoteButton.enabled)
			{
				SharedBlocksManager.instance.RequestVote(loadedMapID, up: false, OnVoteResponse);
				upVoteButton.buttonRenderer.material = buttonDefaultMaterial;
				downVoteButton.buttonRenderer.material = downVoteButton.pressedMaterial;
				upVoteButton.enabled = true;
				downVoteButton.enabled = false;
			}
		}
	}

	private void OnVoteResponse(bool success, string message)
	{
		voteInProgress = false;
		if (success)
		{
			_statusIndexVar.Value = 0;
			statusText.gameObject.SetActive(value: true);
		}
		else
		{
			if (int.TryParse(message, out var result))
			{
				_statusIndexVar.Value = result;
			}
			else
			{
				statusText.text = message;
				Debug.Log("[LOCALIZATION::SHARED_BLOCKS_VOTING_STATION] WARNING: Passing in a non-int value for the [message]. This will not be localized!");
			}
			statusText.gameObject.SetActive(value: true);
			if (!loadedMapID.IsNullOrEmpty())
			{
				upVoteButton.buttonRenderer.material = buttonDefaultMaterial;
				downVoteButton.buttonRenderer.material = buttonDefaultMaterial;
				upVoteButton.enabled = true;
				downVoteButton.enabled = true;
			}
		}
		clearStatusTime = Time.time + clearStatusDelay;
		waitingToClearStatus = true;
	}

	private void LateUpdate()
	{
		if (waitingToClearStatus && Time.time > clearStatusTime)
		{
			waitingToClearStatus = false;
			_statusIndexVar.Value = 2;
			statusText.gameObject.SetActive(value: false);
		}
	}

	private void OnLoadedMapChanged(string mapID)
	{
		loadedMapID = mapID;
		statusText.gameObject.SetActive(value: false);
		UpdateScreen();
	}

	private void OnMapCleared()
	{
		loadedMapID = null;
		statusText.gameObject.SetActive(value: false);
		UpdateScreen();
	}

	private void UpdateScreen()
	{
		if (!loadedMapID.IsNullOrEmpty() && SharedBlocksManager.IsMapIDValid(loadedMapID))
		{
			_mapDisplayIndexVar.Value = 1;
			_mapNameVar.Value = SharedBlocksTerminal.MapIDToDisplayedString(loadedMapID);
			upVoteButton.enabled = true;
			downVoteButton.enabled = true;
			upVoteButton.buttonRenderer.material = buttonDefaultMaterial;
			downVoteButton.buttonRenderer.material = buttonDefaultMaterial;
		}
		else
		{
			_mapDisplayIndexVar.Value = 0;
			upVoteButton.enabled = false;
			downVoteButton.enabled = false;
			upVoteButton.buttonRenderer.material = buttonDisabledMaterial;
			downVoteButton.buttonRenderer.material = buttonDisabledMaterial;
		}
	}
}

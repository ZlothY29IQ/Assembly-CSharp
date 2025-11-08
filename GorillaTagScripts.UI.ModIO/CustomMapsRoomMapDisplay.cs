using System;
using System.Threading.Tasks;
using GorillaTagScripts.VirtualStumpCustomMaps;
using Modio.Mods;
using TMPro;
using UnityEngine;

namespace GorillaTagScripts.UI.ModIO;

public class CustomMapsRoomMapDisplay : MonoBehaviour
{
	[SerializeField]
	private TMP_Text roomMapLabelText;

	[SerializeField]
	private TMP_Text roomMapNameText;

	[SerializeField]
	private TMP_Text roomMapStatusLabelText;

	[SerializeField]
	private TMP_Text roomMapStatusText;

	[SerializeField]
	private string noRoomMapString = "NONE";

	[SerializeField]
	private string notLoadedStatusString = "NOT LOADED";

	[SerializeField]
	private string loadingStatusString = "LOADING...";

	[SerializeField]
	private string readyToPlayStatusString = "READY!";

	[SerializeField]
	private string loadFailedStatusString = "LOAD FAILED";

	[SerializeField]
	private Color notLoadedStatusStringColor = Color.red;

	[SerializeField]
	private Color loadingStatusStringColor = Color.yellow;

	[SerializeField]
	private Color readyToPlayStatusStringColor = Color.green;

	[SerializeField]
	private Color loadFailedStatusStringColor = Color.red;

	public void Start()
	{
		roomMapNameText.text = noRoomMapString;
		roomMapStatusText.text = notLoadedStatusString;
		roomMapLabelText.gameObject.SetActive(value: true);
		roomMapNameText.gameObject.SetActive(value: true);
		roomMapStatusLabelText.gameObject.SetActive(value: false);
		roomMapStatusText.gameObject.SetActive(value: false);
		NetworkSystem.Instance.OnMultiplayerStarted += new Action(OnJoinedRoom);
		NetworkSystem.Instance.OnReturnedToSinglePlayer += new Action(OnDisconnectedFromRoom);
		CustomMapManager.OnRoomMapChanged.AddListener(OnRoomMapChanged);
		CustomMapManager.OnMapLoadStatusChanged.AddListener(OnMapLoadProgress);
		CustomMapManager.OnMapLoadComplete.AddListener(OnMapLoadComplete);
	}

	public void OnDestroy()
	{
		NetworkSystem.Instance.OnMultiplayerStarted -= new Action(OnJoinedRoom);
		NetworkSystem.Instance.OnReturnedToSinglePlayer -= new Action(OnDisconnectedFromRoom);
		CustomMapManager.OnRoomMapChanged.RemoveListener(OnRoomMapChanged);
	}

	private void OnJoinedRoom()
	{
		UpdateRoomMap();
	}

	private void OnDisconnectedFromRoom()
	{
		UpdateRoomMap();
	}

	private void OnRoomMapChanged(ModId roomMapModId)
	{
		UpdateRoomMap();
	}

	private async Task UpdateRoomMap()
	{
		ModId currentRoomMap = CustomMapManager.GetRoomMapId();
		if (currentRoomMap == ModId.Null)
		{
			roomMapNameText.text = noRoomMapString;
			roomMapStatusLabelText.gameObject.SetActive(value: false);
			roomMapStatusText.gameObject.SetActive(value: false);
			return;
		}
		var (error, mod) = await ModIOManager.GetMod(currentRoomMap);
		if ((bool)error)
		{
			roomMapNameText.text = $"FAILED TO GET MOD INFO.\n({error.Code})";
			return;
		}
		roomMapNameText.text = mod.Name;
		roomMapStatusLabelText.gameObject.SetActive(value: true);
		if (CustomMapLoader.IsMapLoaded(currentRoomMap))
		{
			roomMapStatusText.text = readyToPlayStatusString;
			roomMapStatusText.color = readyToPlayStatusStringColor;
		}
		else if (CustomMapManager.IsLoading(currentRoomMap._id))
		{
			roomMapStatusText.text = loadingStatusString;
			roomMapStatusText.color = loadingStatusStringColor;
		}
		else
		{
			roomMapStatusText.text = notLoadedStatusString;
			roomMapStatusText.color = notLoadedStatusStringColor;
		}
		roomMapStatusText.gameObject.SetActive(value: true);
	}

	private void OnMapLoadComplete(bool success)
	{
		if (success)
		{
			roomMapStatusText.text = readyToPlayStatusString;
			roomMapStatusText.color = readyToPlayStatusStringColor;
		}
		else
		{
			roomMapStatusText.text = loadFailedStatusString;
			roomMapStatusText.color = loadFailedStatusStringColor;
		}
	}

	private void OnMapLoadProgress(MapLoadStatus status, int progress, string message)
	{
		if ((uint)(status - 1) <= 1u)
		{
			roomMapStatusText.text = loadingStatusString;
			roomMapStatusText.color = loadingStatusStringColor;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GorillaGameModes;
using GorillaNetworking;
using TMPro;
using UnityEngine;

namespace GorillaTagScripts.VirtualStumpCustomMaps;

public class CustomMapModeSelector : GameModeSelectorButtonLayout
{
	[SerializeField]
	private TMP_Text roomHostText;

	[SerializeField]
	private GameObject roomHostDescriptionText;

	[SerializeField]
	private string notInRoomHostString = "-NOT IN ROOM-";

	[SerializeField]
	private string roomHostLabel = "ROOM HOST: ";

	private static List<GameModeType> gamemodes = new List<GameModeType> { GameModeType.Casual };

	private static GameModeType defaultGamemodeForLoadedMap = GameModeType.Casual;

	private static List<CustomMapModeSelector> instances = new List<CustomMapModeSelector>();

	private static string reusableString = "";

	private void Awake()
	{
		instances.AddIfNew(this);
	}

	public void OnEnable()
	{
		if (GorillaComputer.instance != null)
		{
			SetupButtons();
			GorillaComputer.instance.SetGameModeWithoutButton(defaultGamemodeForLoadedMap.ToString());
		}
		RoomSystem.JoinedRoomEvent += new Action(OnJoinedRoom);
		NetworkSystem.Instance.OnMasterClientSwitchedEvent += new Action<NetPlayer>(OnRoomHostSwitched);
		NetworkSystem.Instance.OnReturnedToSinglePlayer += new Action(OnDisconnected);
		roomHostDescriptionText.SetActive(value: false);
		roomHostText.gameObject.SetActive(value: false);
		if (NetworkSystem.Instance.InRoom && NetworkSystem.Instance.SessionIsPrivate)
		{
			OnRoomHostSwitched(NetworkSystem.Instance.MasterClient);
		}
	}

	public void OnDisable()
	{
		RoomSystem.JoinedRoomEvent -= new Action(OnJoinedRoom);
		NetworkSystem.Instance.OnMasterClientSwitchedEvent -= new Action<NetPlayer>(OnRoomHostSwitched);
		NetworkSystem.Instance.OnReturnedToSinglePlayer -= new Action(OnDisconnected);
	}

	private void OnJoinedRoom()
	{
		OnRoomHostSwitched(NetworkSystem.Instance.MasterClient);
	}

	private void OnRoomHostSwitched(NetPlayer newRoomHost)
	{
		if (!NetworkSystem.Instance.InRoom || !NetworkSystem.Instance.SessionIsPrivate)
		{
			return;
		}
		reusableString = notInRoomHostString;
		if (!newRoomHost.IsNull)
		{
			roomHostDescriptionText.SetActive(value: true);
			reusableString = newRoomHost.DefaultName;
			if (GorillaComputer.instance.NametagsEnabled && KIDManager.HasPermissionToUseFeature(EKIDFeatures.Custom_Nametags))
			{
				RigContainer playerRig;
				if (newRoomHost.IsLocal)
				{
					reusableString = newRoomHost.NickName;
				}
				else if (VRRigCache.Instance.TryGetVrrig(newRoomHost, out playerRig))
				{
					reusableString = playerRig.Rig.playerNameVisible;
				}
			}
		}
		roomHostText.text = roomHostLabel + reusableString;
		roomHostText.gameObject.SetActive(value: true);
	}

	private void OnDisconnected()
	{
		roomHostText.gameObject.SetActive(value: false);
		roomHostDescriptionText.SetActive(value: false);
	}

	public static void ResetButtons()
	{
		gamemodes = new List<GameModeType> { GameModeType.Casual };
		defaultGamemodeForLoadedMap = GameModeType.Casual;
		foreach (CustomMapModeSelector instance in instances)
		{
			instance.SetupButtons();
		}
		GorillaComputer.instance.SetGameModeWithoutButton(defaultGamemodeForLoadedMap.ToString());
	}

	public static void SetAvailableGameModes(int[] availableModes, int defaultMode)
	{
		gamemodes.Clear();
		gamemodes.Add(GameModeType.Casual);
		if (availableModes != null)
		{
			foreach (int item in availableModes)
			{
				gamemodes.Add((GameModeType)item);
			}
		}
		defaultGamemodeForLoadedMap = (GameModeType)defaultMode;
		foreach (CustomMapModeSelector instance in instances)
		{
			instance.SetupButtons();
		}
		GorillaComputer.instance.SetGameModeWithoutButton(defaultGamemodeForLoadedMap.ToString());
	}

	protected override async void SetupButtons()
	{
		if (superToggleButton != null)
		{
			superToggleButton.transform.parent.gameObject.SetActive(value: false);
		}
		int count = 0;
		while (GorillaComputer.instance == null)
		{
			await Task.Delay(100);
		}
		bool flag = GorillaTagger.Instance.offlineVRRig.zoneEntity.currentZone != zone;
		foreach (GameModeType gamemode in gamemodes)
		{
			if (count == currentButtons.Count)
			{
				currentButtons.Add(UnityEngine.Object.Instantiate(pf_button, base.transform));
			}
			ModeSelectButton modeSelectButton = currentButtons[count];
			modeSelectButton.transform.localPosition = new Vector3((float)count * -0.15f, 0f, 0f);
			modeSelectButton.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
			modeSelectButton.WarningScreen = warningScreen;
			modeSelectButton.SetInfo(gamemode.ToString(), GameMode.GameModeZoneMapping.GetModeName(gamemode), GameMode.GameModeZoneMapping.IsNew(gamemode), GameMode.GameModeZoneMapping.GetCountdown(gamemode));
			modeSelectButton.gameObject.SetActive(value: true);
			count++;
			flag |= GorillaComputer.instance.currentGameMode.Value.ToUpper() == gamemode.ToString().ToUpper();
		}
		for (int i = count; i < currentButtons.Count; i++)
		{
			currentButtons[i].gameObject.SetActive(value: false);
		}
	}

	public static void RefreshHostName()
	{
		foreach (CustomMapModeSelector instance in instances)
		{
			instance.OnRoomHostSwitched(NetworkSystem.Instance.MasterClient);
		}
	}
}

using System;
using GorillaNetworking;
using GorillaTagScripts.VirtualStumpCustomMaps;
using Modio.Mods;
using TMPro;
using UnityEngine;

public class CustomMapsTerminal : MonoBehaviour
{
	public enum ScreenType
	{
		Invalid = -1,
		TerminalControlPrompt,
		AvailableMods,
		InstalledMods,
		FavoriteMods,
		SubscribedMods,
		SearchMods,
		ModDetails
	}

	[SerializeField]
	private CustomMapsAccessScreen controlAccessScreen;

	[SerializeField]
	private CustomMapsAccessScreen detailsAccessScreen;

	[SerializeField]
	private CustomMapsListScreen modListScreen;

	[SerializeField]
	private CustomMapsDetailsScreen modDetailsScreen;

	[SerializeField]
	private CustomMapsDisplayScreen modDisplayScreen;

	[SerializeField]
	private CustomMapsSearchScreen modSearchScreen;

	[SerializeField]
	private VirtualStumpSerializer mapTerminalNetworkObject;

	[SerializeField]
	private CustomMapsTerminalControlButton terminalControlButton;

	[SerializeField]
	private TMP_Text terminalControllerLabelText;

	[SerializeField]
	private TMP_Text terminalControllerText;

	public const int NO_DRIVER_ID = -2;

	private static CustomMapsTerminal instance;

	private static bool hasInstance;

	private static long localModDetailsID = -1L;

	private static long cachedModDetailsID = -1L;

	private static int localDriverID = -1;

	private static int cachedLocalPlayerID = -1;

	private static ScreenType localCurrentScreen = ScreenType.Invalid;

	private static ScreenType cachedCurrentScreen = ScreenType.Invalid;

	private static ScreenType previousScreen = ScreenType.Invalid;

	public static int LocalPlayerID => NetworkSystem.Instance.LocalPlayer.ActorNumber;

	public static long LocalModDetailsID => localModDetailsID;

	public static int CurrentScreen => (int)localCurrentScreen;

	public static bool IsDriver => localDriverID == LocalPlayerID;

	private void Awake()
	{
		instance = this;
		hasInstance = true;
	}

	private void Start()
	{
		localDriverID = -2;
		localCurrentScreen = ScreenType.TerminalControlPrompt;
		previousScreen = ScreenType.TerminalControlPrompt;
		controlAccessScreen.Show();
		detailsAccessScreen.Show();
		modListScreen.Hide();
		modDetailsScreen.Hide();
		ModIOManager.OnModIOLoggedIn.AddListener(OnModIOLoggedIn);
		ModIOManager.OnModIOLoggedOut.AddListener(OnModIOLoggedOut);
		NetworkSystem.Instance.OnMultiplayerStarted += new Action(OnJoinedRoom);
		NetworkSystem.Instance.OnReturnedToSinglePlayer += new Action(OnReturnedToSinglePlayer);
	}

	private void OnDestroy()
	{
		ModIOManager.OnModIOLoggedIn.RemoveListener(OnModIOLoggedIn);
		ModIOManager.OnModIOLoggedOut.RemoveListener(OnModIOLoggedOut);
		NetworkSystem.Instance.OnMultiplayerStarted -= new Action(OnJoinedRoom);
		NetworkSystem.Instance.OnReturnedToSinglePlayer -= new Action(OnReturnedToSinglePlayer);
	}

	public static void ShowDetailsScreen(Mod mod)
	{
		previousScreen = localCurrentScreen;
		localCurrentScreen = ScreenType.ModDetails;
		localModDetailsID = mod.Id;
		instance.modListScreen.Hide();
		instance.controlAccessScreen.Hide();
		instance.detailsAccessScreen.Hide();
		instance.modDetailsScreen.Show();
		instance.modDetailsScreen.SetModProfile(mod);
		instance.modDisplayScreen.Show();
		instance.modDisplayScreen.SetModProfile(mod);
		instance.modSearchScreen.Hide();
		SendTerminalStatus();
	}

	public static void ReturnFromDetailsScreen()
	{
		ScreenType screenType = previousScreen;
		if (screenType == ScreenType.ModDetails || screenType == ScreenType.Invalid || screenType == ScreenType.TerminalControlPrompt)
		{
			localCurrentScreen = ScreenType.AvailableMods;
			previousScreen = ScreenType.AvailableMods;
		}
		else
		{
			localCurrentScreen = previousScreen;
		}
		switch (localCurrentScreen)
		{
		case ScreenType.TerminalControlPrompt:
			instance.modListScreen.Hide();
			instance.modDetailsScreen.Hide();
			instance.modDisplayScreen.Hide();
			instance.modSearchScreen.Hide();
			instance.controlAccessScreen.Show();
			instance.detailsAccessScreen.Show();
			break;
		case ScreenType.AvailableMods:
		case ScreenType.InstalledMods:
		case ScreenType.FavoriteMods:
		case ScreenType.SubscribedMods:
			instance.modListScreen.Show();
			instance.modSearchScreen.Hide();
			instance.modDetailsScreen.Hide();
			instance.modDisplayScreen.Hide();
			instance.controlAccessScreen.Hide();
			instance.detailsAccessScreen.Show();
			break;
		case ScreenType.SearchMods:
			instance.modListScreen.Hide();
			instance.modSearchScreen.ReturnFromDetailsScreen();
			instance.modDetailsScreen.Hide();
			instance.modDisplayScreen.Hide();
			instance.controlAccessScreen.Hide();
			instance.detailsAccessScreen.Show();
			break;
		}
		SendTerminalStatus();
	}

	public static void ShowSearchScreen()
	{
		previousScreen = localCurrentScreen;
		localCurrentScreen = ScreenType.SearchMods;
		instance.modListScreen.Hide();
		instance.controlAccessScreen.Hide();
		instance.detailsAccessScreen.SetDetailsScreenForDriver();
		instance.detailsAccessScreen.Show();
		instance.modDetailsScreen.Hide();
		instance.modDisplayScreen.Hide();
		instance.modSearchScreen.Show();
		SendTerminalStatus();
	}

	public static void ReturnFromSearchScreen()
	{
		ScreenType screenType = previousScreen;
		if (screenType == ScreenType.ModDetails || screenType == ScreenType.Invalid || screenType == ScreenType.TerminalControlPrompt || screenType == ScreenType.SearchMods)
		{
			localCurrentScreen = ScreenType.AvailableMods;
			previousScreen = ScreenType.AvailableMods;
		}
		else
		{
			localCurrentScreen = previousScreen;
		}
		switch (localCurrentScreen)
		{
		case ScreenType.TerminalControlPrompt:
			instance.modListScreen.Hide();
			instance.modSearchScreen.Hide();
			instance.modDetailsScreen.Hide();
			instance.modDisplayScreen.Hide();
			instance.controlAccessScreen.Show();
			instance.detailsAccessScreen.Show();
			break;
		case ScreenType.AvailableMods:
		case ScreenType.InstalledMods:
		case ScreenType.FavoriteMods:
		case ScreenType.SubscribedMods:
			instance.modListScreen.Show();
			instance.modSearchScreen.Hide();
			instance.modDetailsScreen.Hide();
			instance.modDisplayScreen.Hide();
			instance.controlAccessScreen.Hide();
			instance.detailsAccessScreen.Show();
			break;
		}
		SendTerminalStatus();
	}

	public static void SendTerminalStatus()
	{
		if (hasInstance)
		{
			instance.mapTerminalNetworkObject.SendTerminalStatus();
		}
	}

	public static void ResetTerminalControl()
	{
		localDriverID = -2;
		instance.terminalControlButton.UnlockTerminalControl();
		ShowTerminalControlScreen();
	}

	public static void HandleTerminalControlStatusChangeRequest(bool lockedStatus, int playerID)
	{
		if (lockedStatus && playerID == -2)
		{
			return;
		}
		if (localDriverID == -2)
		{
			if (!lockedStatus)
			{
				return;
			}
		}
		else if (localDriverID != playerID)
		{
			return;
		}
		SetTerminalControlStatus(lockedStatus, playerID, sendRPC: true);
	}

	public static void SetTerminalControlStatus(bool isLocked, int driverID = -2, bool sendRPC = false)
	{
		GTDev.Log($"[CustomMapsTerminal::SetTerminalControlStatus] isLocked: {isLocked} | driverID: {driverID} | playerId {LocalPlayerID} | sendRPC: {sendRPC}");
		if (isLocked)
		{
			localDriverID = driverID;
			instance.terminalControlButton.LockTerminalControl();
			if (IsDriver)
			{
				HideTerminalControlScreens();
			}
			else
			{
				ShowTerminalControlScreen();
			}
		}
		else
		{
			localDriverID = -2;
			instance.terminalControlButton.UnlockTerminalControl();
			ShowTerminalControlScreen();
		}
		if (sendRPC && NetworkSystem.Instance.IsMasterClient)
		{
			instance.mapTerminalNetworkObject.SetTerminalControlStatus(isLocked, localDriverID);
		}
	}

	public static void UpdateFromDriver(int currentScreen, long modDetailsID, int driverID)
	{
		if (!hasInstance)
		{
			return;
		}
		localDriverID = driverID;
		cachedModDetailsID = modDetailsID;
		localModDetailsID = modDetailsID;
		cachedCurrentScreen = (ScreenType)currentScreen;
		localCurrentScreen = (ScreenType)currentScreen;
		Debug.Log($"[CustomMapsTerminal::UpdateFromDriver] currentScreen {localCurrentScreen} modDetailsID {localModDetailsID}");
		if (localDriverID != -2)
		{
			RefreshDriverNickName();
		}
		switch (localCurrentScreen)
		{
		case ScreenType.TerminalControlPrompt:
		case ScreenType.AvailableMods:
		case ScreenType.InstalledMods:
		case ScreenType.FavoriteMods:
		case ScreenType.SubscribedMods:
		case ScreenType.SearchMods:
			ShowTerminalControlScreen();
			break;
		case ScreenType.ModDetails:
			ShowTerminalControlScreen();
			if (localModDetailsID > 0)
			{
				instance.detailsAccessScreen.Hide();
				instance.modDisplayScreen.Show();
				instance.modDisplayScreen.RetrieveModFromModIO(localModDetailsID);
			}
			break;
		}
	}

	private void UpdateControlScreenForDriver()
	{
		GTDev.Log($"[CustomMapsTerminal::UpdateScreenToMatchStatus] driverID: {localDriverID} " + $"| currentScreen: {localCurrentScreen} " + $"| previousScreen: {previousScreen} ");
		switch (localCurrentScreen)
		{
		case ScreenType.TerminalControlPrompt:
			break;
		case ScreenType.AvailableMods:
		case ScreenType.InstalledMods:
		case ScreenType.FavoriteMods:
		case ScreenType.SubscribedMods:
			controlAccessScreen.Hide();
			modSearchScreen.Hide();
			detailsAccessScreen.SetDetailsScreenForDriver();
			detailsAccessScreen.Show();
			modListScreen.Show();
			modDetailsScreen.Hide();
			modDisplayScreen.Hide();
			break;
		case ScreenType.ModDetails:
			controlAccessScreen.Hide();
			modSearchScreen.Hide();
			detailsAccessScreen.Hide();
			modListScreen.Hide();
			modDetailsScreen.Show();
			modDetailsScreen.RetrieveModFromModIO(localModDetailsID);
			modDisplayScreen.Show();
			modDisplayScreen.RetrieveModFromModIO(localModDetailsID);
			break;
		case ScreenType.SearchMods:
			controlAccessScreen.Hide();
			modSearchScreen.Show();
			detailsAccessScreen.SetDetailsScreenForDriver();
			detailsAccessScreen.Show();
			modListScreen.Hide();
			modDetailsScreen.Hide();
			modDisplayScreen.Hide();
			break;
		}
	}

	private void ValidateLocalStatus()
	{
		if (localDriverID != -2)
		{
			if (CustomMapLoader.IsMapLoaded())
			{
				localCurrentScreen = ScreenType.ModDetails;
				localModDetailsID = CustomMapLoader.LoadedMapModId;
				SendTerminalStatus();
			}
			else if (CustomMapManager.IsLoading())
			{
				localCurrentScreen = ScreenType.ModDetails;
				localModDetailsID = CustomMapManager.LoadingMapId;
				SendTerminalStatus();
			}
			else if (CustomMapManager.GetRoomMapId() != ModId.Null)
			{
				localCurrentScreen = ScreenType.ModDetails;
				localModDetailsID = CustomMapManager.GetRoomMapId()._id;
				SendTerminalStatus();
			}
		}
	}

	private void OnModIOLoggedIn()
	{
	}

	private void OnModIOLoggedOut()
	{
		if (localCurrentScreen == ScreenType.SubscribedMods)
		{
			if (modListScreen.isActiveAndEnabled)
			{
				modListScreen.SwapListDisplay(CustomMapsListScreen.ListScreenState.AvailableMods);
			}
			else
			{
				localCurrentScreen = ScreenType.AvailableMods;
			}
		}
		if (previousScreen == ScreenType.SubscribedMods)
		{
			previousScreen = ScreenType.AvailableMods;
		}
	}

	public void HandleTerminalControlButtonPressed()
	{
		if (NetworkSystem.Instance.InRoom)
		{
			if (localDriverID == -2 || IsDriver)
			{
				if (mapTerminalNetworkObject.HasAuthority)
				{
					HandleTerminalControlStatusChangeRequest(!terminalControlButton.IsLocked, LocalPlayerID);
				}
				else
				{
					mapTerminalNetworkObject.RequestTerminalControlStatusChange(!terminalControlButton.IsLocked);
				}
			}
		}
		else
		{
			SetTerminalControlStatus(!terminalControlButton.IsLocked, LocalPlayerID);
		}
	}

	private static void ShowTerminalControlScreen()
	{
		if (hasInstance)
		{
			if (localDriverID == -2)
			{
				instance.controlAccessScreen.Reset();
				instance.detailsAccessScreen.Reset();
			}
			else
			{
				instance.controlAccessScreen.SetDriverName();
				instance.detailsAccessScreen.SetDriverName();
			}
			instance.modListScreen.Hide();
			instance.modDetailsScreen.Hide();
			instance.modDisplayScreen.Hide();
			instance.controlAccessScreen.Show();
			instance.detailsAccessScreen.Show();
			instance.modSearchScreen.Hide();
			previousScreen = localCurrentScreen;
			localCurrentScreen = ScreenType.TerminalControlPrompt;
		}
	}

	private static void HideTerminalControlScreens()
	{
		if (!hasInstance || localCurrentScreen != ScreenType.TerminalControlPrompt)
		{
			return;
		}
		if (previousScreen > ScreenType.TerminalControlPrompt)
		{
			localCurrentScreen = previousScreen;
			if ((localCurrentScreen == ScreenType.SubscribedMods || localCurrentScreen == ScreenType.FavoriteMods) && !ModIOManager.IsLoggedIn())
			{
				localCurrentScreen = ScreenType.AvailableMods;
			}
		}
		else if (CustomMapLoader.IsMapLoaded() || CustomMapManager.IsLoading() || CustomMapManager.GetRoomMapId() != ModId.Null)
		{
			localCurrentScreen = ScreenType.ModDetails;
		}
		else
		{
			localCurrentScreen = ScreenType.AvailableMods;
		}
		instance.UpdateControlScreenForDriver();
	}

	public static void RequestDriverNickNameRefresh()
	{
		if (hasInstance && IsDriver)
		{
			RefreshDriverNickName();
			instance.mapTerminalNetworkObject.RefreshDriverNickName();
		}
	}

	public static void RefreshDriverNickName()
	{
		if (!hasInstance)
		{
			return;
		}
		bool flag = KIDManager.HasPermissionToUseFeature(EKIDFeatures.Custom_Nametags);
		instance.terminalControllerLabelText.gameObject.SetActive(value: true);
		if (NetworkSystem.Instance.InRoom)
		{
			NetPlayer netPlayerByID = NetworkSystem.Instance.GetNetPlayerByID(localDriverID);
			instance.terminalControllerText.text = netPlayerByID.DefaultName;
			if (GorillaComputer.instance.NametagsEnabled && flag)
			{
				RigContainer playerRig;
				if (netPlayerByID.IsLocal)
				{
					instance.terminalControllerText.text = netPlayerByID.NickName;
				}
				else if (VRRigCache.Instance.TryGetVrrig(netPlayerByID, out playerRig))
				{
					instance.terminalControllerText.text = playerRig.Rig.playerNameVisible;
				}
			}
		}
		else
		{
			instance.terminalControllerText.text = ((GorillaComputer.instance.NametagsEnabled && flag) ? NetworkSystem.Instance.LocalPlayer.NickName : NetworkSystem.Instance.LocalPlayer.DefaultName);
		}
		instance.terminalControllerText.gameObject.SetActive(value: true);
		instance.modListScreen.RefreshDriverNickname(instance.terminalControllerText.text);
	}

	private void OnReturnedToSinglePlayer()
	{
		if (localDriverID != cachedLocalPlayerID)
		{
			ResetTerminalControl();
		}
		else
		{
			localDriverID = LocalPlayerID;
		}
		cachedLocalPlayerID = -1;
	}

	private void OnJoinedRoom()
	{
		cachedLocalPlayerID = LocalPlayerID;
		ResetTerminalControl();
	}

	public static bool IsLocked()
	{
		if (localDriverID == -2)
		{
			return false;
		}
		return true;
	}

	public static int GetDriverID()
	{
		return localDriverID;
	}

	public static string GetDriverNickname()
	{
		if (!hasInstance)
		{
			return "";
		}
		return instance.terminalControllerText.text;
	}
}

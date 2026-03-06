using System;
using System.Collections.Generic;
using Fusion;
using GorillaExtensions;
using UnityEngine;

namespace GorillaGameModes;

public class GameMode : MonoBehaviour
{
	public delegate void OnStartGameModeAction(GameModeType newGameModeType);

	[SerializeField]
	private GameModeZoneMapping gameModeZoneMapping;

	[OnEnterPlay_SetNull]
	private static GameMode instance;

	[OnEnterPlay_Clear]
	private static Dictionary<int, GorillaGameManager> gameModeTable;

	[OnEnterPlay_Clear]
	public static Dictionary<string, int> gameModeKeyByName;

	[OnEnterPlay_Clear]
	private static Dictionary<int, FusionGameModeData> fusionTypeTable;

	[OnEnterPlay_Clear]
	public static List<GorillaGameManager> gameModes;

	[OnEnterPlay_Clear]
	public static readonly List<string> gameModeNames;

	[OnEnterPlay_Clear]
	private static readonly List<GorillaGameManager> activatedGameModes;

	[OnEnterPlay_SetNull]
	private static GorillaGameManager activeGameMode;

	[OnEnterPlay_SetNull]
	private static GameModeSerializer activeNetworkHandler;

	[OnEnterPlay_Clear]
	private static readonly HashSet<int> optOutPlayers;

	[OnEnterPlay_Clear]
	private static readonly List<NetPlayer> _participatingPlayers;

	private static readonly NetPlayer[] _oldPlayersBuffer;

	private static int _oldPlayersCount;

	private static readonly List<NetPlayer> _tempAddedPlayers;

	private static readonly List<NetPlayer> _tempRemovedPlayers;

	public static GorillaGameManager ActiveGameMode => activeGameMode;

	internal static GameModeSerializer ActiveNetworkHandler => activeNetworkHandler;

	public static GameModeZoneMapping GameModeZoneMapping => instance.gameModeZoneMapping;

	public static GameModeType CurrentGameModeType { get; private set; }

	public static int CurrentGameModeFlag => 1 << (int)CurrentGameModeType;

	public static List<NetPlayer> ParticipatingPlayers => _participatingPlayers;

	public static event OnStartGameModeAction OnStartGameMode;

	public static event Action<List<NetPlayer>, List<NetPlayer>> ParticipatingPlayersChanged;

	private void Awake()
	{
		if (instance.IsNull())
		{
			instance = this;
			GorillaGameManager[] componentsInChildren = base.gameObject.GetComponentsInChildren<GorillaGameManager>(includeInactive: true);
			foreach (GorillaGameManager gorillaGameManager in componentsInChildren)
			{
				int num = (int)gorillaGameManager.GameType();
				string text = gorillaGameManager.GameTypeName();
				if (gameModeTable.ContainsKey(num))
				{
					Debug.LogWarning("Duplicate gamemode type, skipping this instance", gorillaGameManager);
					continue;
				}
				gameModeTable.Add((int)gorillaGameManager.GameType(), gorillaGameManager);
				gameModeKeyByName.Add(text, num);
				gameModes.Add(gorillaGameManager);
				gameModeNames.Add(text);
			}
		}
		else
		{
			UnityEngine.Object.Destroy(this);
		}
	}

	private void OnDestroy()
	{
		if (instance == this)
		{
			instance = null;
		}
	}

	static GameMode()
	{
		gameModeTable = new Dictionary<int, GorillaGameManager>();
		gameModeKeyByName = new Dictionary<string, int>();
		fusionTypeTable = new Dictionary<int, FusionGameModeData>();
		gameModes = new List<GorillaGameManager>(10);
		gameModeNames = new List<string>(10);
		activatedGameModes = new List<GorillaGameManager>(13);
		activeGameMode = null;
		activeNetworkHandler = null;
		CurrentGameModeType = GameModeType.None;
		optOutPlayers = new HashSet<int>(20);
		_participatingPlayers = new List<NetPlayer>(20);
		_oldPlayersBuffer = new NetPlayer[20];
		_tempAddedPlayers = new List<NetPlayer>(20);
		_tempRemovedPlayers = new List<NetPlayer>(20);
		StaticLoad();
	}

	[OnEnterPlay_Run]
	private static void StaticLoad()
	{
		RoomSystem.LeftRoomEvent += new Action(ResetGameModes);
		RoomSystem.JoinedRoomEvent += new Action(RefreshPlayers);
		RoomSystem.PlayersChangedEvent += new Action(RefreshPlayers);
	}

	public static bool IsPlaying(GameModeType type)
	{
		return type == CurrentGameModeType;
	}

	internal static bool LoadGameModeFromProperty()
	{
		return LoadGameMode(FindGameModeFromRoomProperty());
	}

	internal static bool ChangeGameFromProperty()
	{
		return ChangeGameMode(FindGameModeFromRoomProperty());
	}

	internal static bool LoadGameModeFromProperty(string prop)
	{
		return LoadGameMode(FindGameModeInString(prop));
	}

	internal static bool ChangeGameFromProperty(string prop)
	{
		return ChangeGameMode(FindGameModeInString(prop));
	}

	public static int GetGameModeKeyFromRoomProp()
	{
		string text = FindGameModeFromRoomProperty();
		if (string.IsNullOrEmpty(text) || !gameModeKeyByName.TryGetValue(text, out var value))
		{
			GTDev.LogWarning("Unable to find game mode key for " + text);
			return -1;
		}
		return value;
	}

	private static string FindGameModeFromRoomProperty()
	{
		if (!NetworkSystem.Instance.InRoom || string.IsNullOrEmpty(NetworkSystem.Instance.GameModeString))
		{
			return null;
		}
		return FindGameModeInString(NetworkSystem.Instance.GameModeString);
	}

	public static bool IsValidGameMode(string gameMode)
	{
		if (!string.IsNullOrEmpty(gameMode))
		{
			return gameModeKeyByName.ContainsKey(gameMode);
		}
		return false;
	}

	private static string FindGameModeInString(string gmString)
	{
		for (int i = 0; i < gameModes.Count; i++)
		{
			string text = gameModes[i].GameTypeName();
			if (gmString.Length <= text.Length)
			{
				continue;
			}
			if (gmString.Contains('|'))
			{
				int num = text.Length + 1;
				if (gmString[gmString.Length - num] != '|')
				{
					continue;
				}
			}
			if (gmString.EndsWith(text))
			{
				return text;
			}
		}
		return null;
	}

	public static bool LoadGameMode(string gameMode)
	{
		if (gameMode == null)
		{
			Debug.LogError("GAME MODE NULL");
			return false;
		}
		if (!gameModeKeyByName.TryGetValue(gameMode, out var value))
		{
			Debug.LogWarning("Unable to find game mode key for " + gameMode);
			return false;
		}
		return LoadGameMode(value);
	}

	public static bool LoadGameMode(int key)
	{
		foreach (KeyValuePair<int, GorillaGameManager> item in gameModeTable)
		{
			_ = item;
		}
		if (!gameModeTable.ContainsKey(key))
		{
			Debug.LogWarning("Missing game mode for key " + key);
			return false;
		}
		VRRigCache.Instance.GetComponent<PhotonPrefabPool>().networkPrefabs.TryGetValue("GameMode", out var value);
		GameObject prefab = value.prefab;
		if (prefab == null)
		{
			GTDev.LogError("Unable to find game mode prefab to spawn");
			return false;
		}
		if (NetworkSystem.Instance.NetInstantiate(prefab, Vector3.zero, Quaternion.identity, isRoomObject: true, 0, new object[1] { key }, delegate(NetworkRunner runner, NetworkObject no)
		{
			no.GetComponent<GameModeSerializer>().Init(key);
		}).IsNull())
		{
			GTDev.LogWarning("Unable to create GameManager with key " + key);
			return false;
		}
		return true;
	}

	internal static bool ChangeGameMode(string gameMode)
	{
		if (gameMode == null)
		{
			return false;
		}
		if (!gameModeKeyByName.TryGetValue(gameMode, out var value))
		{
			Debug.LogWarning("Unable to find game mode key for " + gameMode);
			return false;
		}
		return ChangeGameMode(value);
	}

	internal static bool ChangeGameMode(int key)
	{
		if (!NetworkSystem.Instance.IsMasterClient || !gameModeTable.TryGetValue(key, out var value) || value == activeGameMode)
		{
			return false;
		}
		if (activeNetworkHandler.IsNotNull())
		{
			NetworkSystem.Instance.NetDestroy(activeNetworkHandler.gameObject);
		}
		StopGameModeSafe(activeGameMode);
		activeGameMode = null;
		activeNetworkHandler = null;
		CurrentGameModeType = GameModeType.None;
		return LoadGameMode(key);
	}

	internal static void SetupGameModeRemote(GameModeSerializer networkSerializer)
	{
		GorillaGameManager gameModeInstance = networkSerializer.GameModeInstance;
		bool flag = gameModeInstance != activeGameMode;
		if (activeGameMode.IsNotNull() && gameModeInstance.IsNotNull() && flag)
		{
			StopGameModeSafe(activeGameMode);
		}
		activeNetworkHandler = networkSerializer;
		activeGameMode = gameModeInstance;
		activeGameMode.NetworkLinkSetup(networkSerializer);
		CurrentGameModeType = activeGameMode.GameType();
		if (!activatedGameModes.Contains(activeGameMode))
		{
			activatedGameModes.Add(activeGameMode);
		}
		if (flag)
		{
			StartGameModeSafe(activeGameMode);
			if (GameMode.OnStartGameMode != null)
			{
				GameMode.OnStartGameMode(activeGameMode.GameType());
			}
		}
	}

	internal static void RemoveNetworkLink(GameModeSerializer networkSerializer)
	{
		if (activeGameMode.IsNotNull() && networkSerializer == activeNetworkHandler)
		{
			activeGameMode.NetworkLinkDestroyed(networkSerializer);
			activeNetworkHandler = null;
		}
	}

	public static GorillaGameManager GetGameModeInstance(GameModeType type)
	{
		return GetGameModeInstance((int)type);
	}

	public static GorillaGameManager GetGameModeInstance(int type)
	{
		if (gameModeTable.TryGetValue(type, out var value))
		{
			if (value == null)
			{
				Debug.LogError("Couldnt get mode from table");
				foreach (KeyValuePair<int, GorillaGameManager> item in gameModeTable)
				{
					_ = item;
				}
			}
			return value;
		}
		return null;
	}

	public static T GetGameModeInstance<T>(GameModeType type) where T : GorillaGameManager
	{
		return GetGameModeInstance<T>((int)type);
	}

	public static T GetGameModeInstance<T>(int type) where T : GorillaGameManager
	{
		if (GetGameModeInstance(type) is T result)
		{
			return result;
		}
		return null;
	}

	public static void ResetGameModes()
	{
		CurrentGameModeType = GameModeType.None;
		activeGameMode = null;
		activeNetworkHandler = null;
		optOutPlayers.Clear();
		ParticipatingPlayers.Clear();
		for (int i = 0; i < activatedGameModes.Count; i++)
		{
			GorillaGameManager gameMode = activatedGameModes[i];
			StopGameModeSafe(gameMode);
			ResetGameModeSafe(gameMode);
		}
		activatedGameModes.Clear();
	}

	private static void StartGameModeSafe(GorillaGameManager gameMode)
	{
		try
		{
			gameMode.StartPlaying();
		}
		catch (Exception)
		{
		}
	}

	private static void StopGameModeSafe(GorillaGameManager gameMode)
	{
		try
		{
			gameMode.StopPlaying();
		}
		catch (Exception)
		{
		}
	}

	private static void ResetGameModeSafe(GorillaGameManager gameMode)
	{
		try
		{
			gameMode.ResetGame();
		}
		catch (Exception)
		{
		}
	}

	public static void ReportTag(NetPlayer player)
	{
		if (NetworkSystem.Instance.InRoom && activeNetworkHandler.IsNotNull())
		{
			activeNetworkHandler.SendRPC("RPC_ReportTag", false, player.ActorNumber);
		}
	}

	public static void ReportHit()
	{
		if (GorillaGameManager.instance.GameType() == GameModeType.Custom)
		{
			CustomGameMode.TaggedByEnvironment();
		}
		if (NetworkSystem.Instance.InRoom && activeNetworkHandler.IsNotNull())
		{
			activeNetworkHandler.SendRPC("RPC_ReportHit", false);
		}
	}

	public static bool LocalIsTagged(NetPlayer player)
	{
		if (ActiveGameMode.IsNull())
		{
			return false;
		}
		return ActiveGameMode.LocalIsTagged(player);
	}

	public static void BroadcastRoundComplete()
	{
		if (NetworkSystem.Instance.IsMasterClient && NetworkSystem.Instance.InRoom && activeNetworkHandler.IsNotNull())
		{
			activeNetworkHandler.SendRPC("RPC_BroadcastRoundComplete", true);
		}
	}

	public static void BroadcastTag(NetPlayer taggedPlayer, NetPlayer taggingPlayer)
	{
		if (NetworkSystem.Instance.IsMasterClient && NetworkSystem.Instance.InRoom && activeNetworkHandler.IsNotNull())
		{
			activeNetworkHandler.SendRPC("RPC_BroadcastTag", true, taggedPlayer.ActorNumber, taggingPlayer.ActorNumber);
		}
	}

	public static void RefreshPlayers()
	{
		_oldPlayersCount = _participatingPlayers.Count;
		for (int i = 0; i < _oldPlayersCount; i++)
		{
			_oldPlayersBuffer[i] = _participatingPlayers[i];
		}
		_participatingPlayers.Clear();
		List<NetPlayer> playersInRoom = RoomSystem.PlayersInRoom;
		int num = Mathf.Min(playersInRoom.Count, 20);
		for (int j = 0; j < num; j++)
		{
			if (CanParticipate(playersInRoom[j]))
			{
				ParticipatingPlayers.Add(playersInRoom[j]);
			}
		}
		_tempRemovedPlayers.Clear();
		for (int k = 0; k < _oldPlayersCount; k++)
		{
			NetPlayer netPlayer = _oldPlayersBuffer[k];
			if (!ContainsNetPlayer(_participatingPlayers, netPlayer))
			{
				_tempRemovedPlayers.Add(netPlayer);
			}
		}
		_tempAddedPlayers.Clear();
		int count = _participatingPlayers.Count;
		for (int l = 0; l < count; l++)
		{
			NetPlayer netPlayer2 = _participatingPlayers[l];
			if (!ContainsNetPlayer(_oldPlayersBuffer, netPlayer2, _oldPlayersCount))
			{
				_tempAddedPlayers.Add(netPlayer2);
			}
		}
		if ((_tempAddedPlayers.Count > 0 || _tempRemovedPlayers.Count > 0) && GameMode.ParticipatingPlayersChanged != null)
		{
			GameMode.ParticipatingPlayersChanged(_tempAddedPlayers, _tempRemovedPlayers);
		}
	}

	private static bool ContainsNetPlayer(List<NetPlayer> list, NetPlayer candidate)
	{
		int count = list.Count;
		for (int i = 0; i < count; i++)
		{
			if (list[i] == candidate)
			{
				return true;
			}
		}
		return false;
	}

	private static bool ContainsNetPlayer(NetPlayer[] array, NetPlayer candidate, int length)
	{
		for (int i = 0; i < length; i++)
		{
			if (array[i] == candidate)
			{
				return true;
			}
		}
		return false;
	}

	public static void OptOut(VRRig rig)
	{
		OptOut(rig.creator.ActorNumber);
	}

	public static void OptOut(NetPlayer player)
	{
		OptOut(player.ActorNumber);
	}

	public static void OptOut(int playerActorNumber)
	{
		if (optOutPlayers.Add(playerActorNumber))
		{
			RefreshPlayers();
		}
	}

	public static void OptIn(VRRig rig)
	{
		OptIn(rig.creator.ActorNumber);
	}

	public static void OptIn(NetPlayer player)
	{
		OptIn(player.ActorNumber);
	}

	public static void OptIn(int playerActorNumber)
	{
		if (optOutPlayers.Remove(playerActorNumber))
		{
			RefreshPlayers();
		}
	}

	private static bool CanParticipate(NetPlayer player)
	{
		if (player.InRoom() && !optOutPlayers.Contains(player.ActorNumber) && NetworkSystem.Instance.GetPlayerTutorialCompletion(player.ActorNumber))
		{
			if (!(GorillaGameManager.instance != null))
			{
				return true;
			}
			return GorillaGameManager.instance.CanPlayerParticipate(player);
		}
		return false;
	}
}

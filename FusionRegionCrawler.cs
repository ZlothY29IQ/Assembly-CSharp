using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
using UnityEngine;

public class FusionRegionCrawler : MonoBehaviour, INetworkRunnerCallbacks, IPublicFacingInterface
{
	public delegate void PlayerCountUpdated(int playerCount);

	public PlayerCountUpdated OnPlayerCountUpdated;

	private NetworkRunner regionRunner;

	private List<SessionInfo> sessionInfoCache;

	private bool waitingForSessionListUpdate;

	private int globalPlayerCount;

	private float UpdateFrequency = 10f;

	private bool refreshPlayerCountAutomatically = true;

	private int tempSessionPlayerCount;

	public int PlayerCountGlobal => globalPlayerCount;

	public void Start()
	{
		regionRunner = base.gameObject.AddComponent<NetworkRunner>();
		regionRunner.AddCallbacks(this);
		StartCoroutine(OccasionalUpdate());
	}

	public IEnumerator OccasionalUpdate()
	{
		while (refreshPlayerCountAutomatically)
		{
			yield return UpdatePlayerCount();
			yield return new WaitForSeconds(UpdateFrequency);
		}
	}

	public IEnumerator UpdatePlayerCount()
	{
		int tempGlobalPlayerCount = 0;
		StartGameArgs startGameArgs = default(StartGameArgs);
		string[] regionNames = NetworkSystem.Instance.regionNames;
		foreach (string fixedRegion in regionNames)
		{
			startGameArgs.CustomPhotonAppSettings = new FusionAppSettings();
			startGameArgs.CustomPhotonAppSettings.FixedRegion = fixedRegion;
			waitingForSessionListUpdate = true;
			regionRunner.JoinSessionLobby(SessionLobby.ClientServer, startGameArgs.CustomPhotonAppSettings.FixedRegion, null, null, false);
			while (waitingForSessionListUpdate)
			{
				yield return new WaitForEndOfFrame();
			}
			foreach (SessionInfo item in sessionInfoCache)
			{
				tempGlobalPlayerCount += item.PlayerCount;
			}
			tempGlobalPlayerCount += tempSessionPlayerCount;
		}
		globalPlayerCount = tempGlobalPlayerCount;
		OnPlayerCountUpdated?.Invoke(globalPlayerCount);
	}

	public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
	{
		if (waitingForSessionListUpdate)
		{
			sessionInfoCache = sessionList;
			waitingForSessionListUpdate = false;
		}
	}

	void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
	{
	}

	void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
	{
	}

	void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input)
	{
	}

	void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
	{
	}

	void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
	{
	}

	void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner)
	{
	}

	void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
	{
	}

	void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
	{
	}

	void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
	{
	}

	void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
	{
	}

	void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
	{
	}

	void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
	{
	}

	void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner)
	{
	}

	void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner)
	{
	}

	public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
	{
	}

	public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
	{
	}

	public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
	{
	}

	public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
	{
	}

	public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
	{
	}
}

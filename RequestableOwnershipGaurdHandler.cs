using System;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Fusion;
using Fusion.Sockets;
using GorillaExtensions;
using Photon.Pun;
using Photon.Realtime;

internal class RequestableOwnershipGaurdHandler : IPunOwnershipCallbacks, IInRoomCallbacks, INetworkRunnerCallbacks, IPublicFacingInterface
{
	private static HashSet<NetworkView> gaurdedViews;

	private static readonly RequestableOwnershipGaurdHandler callbackInstance;

	private static Dictionary<NetworkView, RequestableOwnershipGuard> guardingLookup;

	static RequestableOwnershipGaurdHandler()
	{
		gaurdedViews = new HashSet<NetworkView>();
		callbackInstance = new RequestableOwnershipGaurdHandler();
		guardingLookup = new Dictionary<NetworkView, RequestableOwnershipGuard>();
		PhotonNetwork.AddCallbackTarget(callbackInstance);
	}

	internal static void RegisterView(NetworkView view, RequestableOwnershipGuard guard)
	{
		if (!(view == null) && !gaurdedViews.Contains(view))
		{
			gaurdedViews.Add(view);
			guardingLookup.Add(view, guard);
		}
	}

	internal static void RemoveView(NetworkView view)
	{
		if (!(view == null))
		{
			gaurdedViews.Remove(view);
			guardingLookup.Remove(view);
		}
	}

	internal static void RegisterViews(NetworkView[] views, RequestableOwnershipGuard guard)
	{
		for (int i = 0; i < views.Length; i++)
		{
			RegisterView(views[i], guard);
		}
	}

	public static void RemoveViews(NetworkView[] views, RequestableOwnershipGuard guard)
	{
		for (int i = 0; i < views.Length; i++)
		{
			RemoveView(views[i]);
		}
	}

	void IPunOwnershipCallbacks.OnOwnershipTransfered(PhotonView targetView, Player previousOwner)
	{
		NetworkView networkView = gaurdedViews.FirstOrDefault((NetworkView p) => p.GetView == targetView);
		if (!networkView.IsNull() && guardingLookup.TryGetValue(networkView, out var value) && !value.IsNull())
		{
			Player player = value.currentOwner?.GetPlayerRef();
			int num = player?.ActorNumber ?? 0;
			if (num == 0 || previousOwner != player)
			{
				GTDev.LogWarning("Ownership transferred but the previous owner didn't initiate the request, Switching back");
				targetView.OwnerActorNr = num;
				targetView.ControllerActorNr = num;
			}
		}
	}

	void IInRoomCallbacks.OnMasterClientSwitched(Player newMasterClient)
	{
		OnHostChangedShared();
	}

	public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
	{
		OnHostChangedShared();
	}

	private void OnHostChangedShared()
	{
		foreach (NetworkView gaurdedView in gaurdedViews)
		{
			if (!guardingLookup.TryGetValue(gaurdedView, out var value))
			{
				break;
			}
			if (gaurdedView.Owner != null && value.currentOwner != null && !object.Equals(gaurdedView.Owner, value.currentOwner))
			{
				gaurdedView.OwnerActorNr = value.currentOwner.ActorNumber;
				gaurdedView.ControllerActorNr = value.currentOwner.ActorNumber;
			}
		}
	}

	void IPunOwnershipCallbacks.OnOwnershipRequest(PhotonView targetView, Player requestingPlayer)
	{
	}

	void IPunOwnershipCallbacks.OnOwnershipTransferFailed(PhotonView targetView, Player senderOfFailedRequest)
	{
	}

	public void OnPlayerEnteredRoom(Player newPlayer)
	{
	}

	public void OnPlayerLeftRoom(Player otherPlayer)
	{
	}

	public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
	{
	}

	public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
	{
	}

	public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
	{
	}

	public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
	{
	}

	public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
	{
	}

	public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
	{
	}

	public void OnInput(NetworkRunner runner, NetworkInput input)
	{
	}

	public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
	{
	}

	public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
	{
	}

	public void OnConnectedToServer(NetworkRunner runner)
	{
	}

	public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
	{
	}

	public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
	{
	}

	public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
	{
	}

	public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
	{
	}

	public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
	{
	}

	public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
	{
	}

	public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
	{
	}

	public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
	{
	}

	public void OnSceneLoadDone(NetworkRunner runner)
	{
	}

	public void OnSceneLoadStart(NetworkRunner runner)
	{
	}
}

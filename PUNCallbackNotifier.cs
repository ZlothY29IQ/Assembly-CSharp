using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PUNCallbackNotifier : MonoBehaviourPunCallbacks, IOnEventCallback
{
	private NetworkSystemPUN parentSystem;

	private void Start()
	{
		parentSystem = GetComponent<NetworkSystemPUN>();
	}

	private void Update()
	{
	}

	public override void OnConnectedToMaster()
	{
		parentSystem.OnConnectedtoMaster();
	}

	public override void OnJoinedRoom()
	{
		parentSystem.OnJoinedRoom();
	}

	public override void OnJoinRoomFailed(short returnCode, string message)
	{
		parentSystem.OnJoinRoomFailed(returnCode, message);
	}

	public override void OnJoinRandomFailed(short returnCode, string message)
	{
		parentSystem.OnJoinRoomFailed(returnCode, message);
	}

	public override void OnCreateRoomFailed(short returnCode, string message)
	{
		parentSystem.OnCreateRoomFailed(returnCode, message);
	}

	public override void OnPlayerEnteredRoom(Player newPlayer)
	{
		parentSystem.OnPlayerEnteredRoom(newPlayer);
	}

	public override void OnPlayerLeftRoom(Player otherPlayer)
	{
		parentSystem.OnPlayerLeftRoom(otherPlayer);
	}

	public override void OnDisconnected(DisconnectCause cause)
	{
		Debug.Log("Disconnect callback, cause:" + cause);
		parentSystem.OnDisconnected(cause);
	}

	public void OnEvent(EventData photonEvent)
	{
		parentSystem.RaiseEvent(photonEvent.Code, photonEvent.CustomData, photonEvent.Sender);
	}

	public override void OnPreLeavingRoom()
	{
		parentSystem.PreLeavingRoom();
	}

	public override void OnMasterClientSwitched(Player newMasterClient)
	{
		parentSystem.OnMasterClientSwitched(newMasterClient);
	}

	public override void OnCustomAuthenticationResponse(Dictionary<string, object> data)
	{
		base.OnCustomAuthenticationResponse(data);
		NetworkSystem.Instance.CustomAuthenticationResponse(data);
	}
}

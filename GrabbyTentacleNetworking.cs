using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class GrabbyTentacleNetworking : MonoBehaviourPunCallbacks
{
	[SerializeField]
	private PhotonView tablePhotonView;

	private GrabbyTentacleController registeredController;

	public static GrabbyTentacleNetworking Instance { get; private set; }

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Debug.LogWarning("[GrabbyTentacleNetworking] duplicate instance on " + base.name);
			return;
		}
		Instance = this;
		if (tablePhotonView == null)
		{
			tablePhotonView = GetComponent<PhotonView>();
		}
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public void Register(GrabbyTentacleController controller)
	{
		registeredController = controller;
	}

	public void Unregister(GrabbyTentacleController controller)
	{
		if (registeredController == controller)
		{
			registeredController = null;
		}
	}

	public void SendGrab(int tentacleIndex, Player targetPlayer)
	{
		if (PhotonNetwork.IsMasterClient && !(tablePhotonView == null) && targetPlayer != null)
		{
			tablePhotonView.RPC("ApplyTargetRPC", RpcTarget.All, tentacleIndex, targetPlayer);
		}
	}

	[PunRPC]
	public void ApplyTargetRPC(int tentacleIndex, Player targetPlayer, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "ApplyTargetRPC");
		if (info.Sender != null && info.Sender.IsMasterClient && targetPlayer != null && !(registeredController == null) && VRRigCache.Instance.TryGetVrrig(targetPlayer, out var playerRig) && !(playerRig == null))
		{
			bool isLocalPlayer = targetPlayer.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber;
			registeredController.OnGrabReceived(tentacleIndex, playerRig.Rig, isLocalPlayer);
		}
	}
}

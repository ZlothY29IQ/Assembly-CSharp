using Fusion;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

[RequireComponent(typeof(PhotonView), typeof(NetworkObject))]
[NetworkBehaviourWeaved(0)]
public class NetworkView : NetworkBehaviour, IStateAuthorityChanged, IPublicFacingInterface, IPunOwnershipCallbacks
{
	[SerializeField]
	private PhotonView punView;

	[SerializeField]
	private PhotonView reliableView;

	[SerializeField]
	internal NetworkObject fusionView;

	[SerializeField]
	protected bool _sceneObject;

	private bool _spawned;

	private bool changingStatAuth;

	public bool IsMine
	{
		get
		{
			if (punView != null)
			{
				return punView.IsMine;
			}
			return false;
		}
	}

	public bool IsValid => punView != null;

	public bool HasView => punView != null;

	public bool IsRoomView => punView.IsRoomView;

	public PhotonView GetView => punView;

	public NetPlayer Owner => NetworkSystem.Instance.GetPlayer(punView.Owner);

	public int ViewID => punView.ViewID;

	internal OwnershipOption OwnershipTransfer
	{
		get
		{
			return punView.OwnershipTransfer;
		}
		set
		{
			punView.OwnershipTransfer = value;
			if (reliableView != null)
			{
				reliableView.OwnershipTransfer = value;
			}
		}
	}

	public int OwnerActorNr
	{
		get
		{
			return punView.OwnerActorNr;
		}
		set
		{
			punView.OwnerActorNr = value;
			if (reliableView != null)
			{
				reliableView.OwnerActorNr = value;
			}
		}
	}

	public int ControllerActorNr
	{
		get
		{
			return punView.ControllerActorNr;
		}
		set
		{
			punView.ControllerActorNr = value;
			if (reliableView != null)
			{
				reliableView.ControllerActorNr = value;
			}
		}
	}

	private void GetViews()
	{
		PhotonView[] components = GetComponents<PhotonView>();
		if (components.Length > 1)
		{
			if (components[0].Synchronization == ViewSynchronization.UnreliableOnChange)
			{
				punView = components[0];
				reliableView = components[1];
			}
			else if (components[0].Synchronization == ViewSynchronization.ReliableDeltaCompressed)
			{
				reliableView = components[0];
				punView = components[1];
			}
		}
		else
		{
			punView = components[0];
		}
		if (punView == null)
		{
			punView = GetComponent<PhotonView>();
		}
		if (fusionView == null)
		{
			fusionView = GetComponent<NetworkObject>();
		}
	}

	protected virtual void Awake()
	{
		GetViews();
	}

	protected virtual void Start()
	{
		if (_sceneObject)
		{
			NetworkSystem.Instance.RegisterSceneNetworkItem(base.gameObject);
		}
	}

	public void SendRPC(string method, NetPlayer targetPlayer, params object[] parameters)
	{
		Player playerRef = (targetPlayer as PunNetPlayer).PlayerRef;
		punView.RPC(method, playerRef, parameters);
	}

	public void SendRPC(string method, RpcTarget target, params object[] parameters)
	{
		punView.RPC(method, target, parameters);
	}

	public void SendRPC(string method, int target, params object[] parameters)
	{
		Room currentRoom = PhotonNetwork.CurrentRoom;
		if (currentRoom != null && currentRoom.Players.ContainsKey(target))
		{
			punView.RPC(method, currentRoom.Players[target], parameters);
		}
	}

	public override void Spawned()
	{
		base.Spawned();
		_spawned = true;
	}

	public void RequestOwnership()
	{
		GetView.RequestOwnership();
	}

	public void ReleaseOwnership()
	{
		changingStatAuth = true;
		base.Object.ReleaseStateAuthority();
	}

	public virtual void StateAuthorityChanged()
	{
		if (changingStatAuth)
		{
			changingStatAuth = false;
		}
	}

	public virtual void OnOwnershipRequest(PhotonView targetView, Player requestingPlayer)
	{
	}

	public virtual void OnOwnershipTransfered(PhotonView targetView, Player previousOwner)
	{
	}

	public virtual void OnOwnershipTransferFailed(PhotonView targetView, Player senderOfFailedRequest)
	{
	}

	[WeaverGenerated]
	public override void CopyBackingFieldsToState(bool P_0)
	{
	}

	[WeaverGenerated]
	public override void CopyStateToBackingFields()
	{
	}
}

using ExitGames.Client.Photon;
using Fusion;
using Photon.Pun;
using Photon.Realtime;

[NetworkBehaviourWeaved(0)]
public abstract class NetworkComponent : NetworkView, IPunObservable, IStateAuthorityChanged, IPublicFacingInterface, IOnPhotonViewOwnerChange, IPhotonViewCallback, IInRoomCallbacks, IPunInstantiateMagicCallback
{
	public bool IsLocallyOwned => base.IsMine;

	public bool ShouldWriteObjectData => NetworkSystem.Instance.ShouldWriteObjectData(base.gameObject);

	public bool ShouldUpdateobject => NetworkSystem.Instance.ShouldUpdateObject(base.gameObject);

	public int OwnerID => NetworkSystem.Instance.GetOwningPlayerID(base.gameObject);

	internal virtual void OnEnable()
	{
		NetworkBehaviourUtils.InternalOnEnable(this);
		AddToNetwork();
	}

	internal virtual void OnDisable()
	{
		NetworkBehaviourUtils.InternalOnDisable(this);
		PhotonNetwork.RemoveCallbackTarget(this);
	}

	protected override void Start()
	{
		base.Start();
		AddToNetwork();
	}

	private void AddToNetwork()
	{
		PhotonNetwork.AddCallbackTarget(this);
	}

	public override void Spawned()
	{
		if (NetworkSystem.Instance.InRoom)
		{
			OnSpawned();
		}
	}

	public override void FixedUpdateNetwork()
	{
		WriteDataFusion();
	}

	public override void Render()
	{
		if (!base.HasStateAuthority)
		{
			ReadDataFusion();
		}
	}

	public abstract void WriteDataFusion();

	public abstract void ReadDataFusion();

	public virtual void OnPhotonInstantiate(PhotonMessageInfo info)
	{
		OnSpawned();
	}

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			WriteDataPUN(stream, info);
		}
		else if (stream.IsReading)
		{
			ReadDataPUN(stream, info);
		}
	}

	protected abstract void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info);

	protected abstract void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info);

	public virtual void OnSpawned()
	{
	}

	protected virtual void OnOwnerSwitched(NetPlayer newOwningPlayer)
	{
	}

	void IInRoomCallbacks.OnMasterClientSwitched(Player newMasterClient)
	{
		OnOwnerSwitched(NetworkSystem.Instance.GetPlayer(newMasterClient));
	}

	public override void StateAuthorityChanged()
	{
		base.StateAuthorityChanged();
		if (!(base.Object == null) && !(base.Object.StateAuthority == default(PlayerRef)))
		{
			if (NetworkSystem.Instance.InRoom)
			{
				OnOwnerSwitched(NetworkSystem.Instance.GetPlayer(base.Object.StateAuthority));
			}
			else
			{
				OnOwnerSwitched(NetworkSystem.Instance.LocalPlayer);
			}
		}
	}

	public void OnMasterClientSwitch(NetPlayer newMaster)
	{
		StateAuthorityChanged();
	}

	void IInRoomCallbacks.OnPlayerEnteredRoom(Player newPlayer)
	{
	}

	void IInRoomCallbacks.OnPlayerLeftRoom(Player otherPlayer)
	{
	}

	void IInRoomCallbacks.OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
	{
	}

	void IInRoomCallbacks.OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
	{
	}

	public virtual void OnOwnerChange(Player newOwner, Player previousOwner)
	{
	}

	[WeaverGenerated]
	public override void CopyBackingFieldsToState(bool P_0)
	{
		base.CopyBackingFieldsToState(P_0);
	}

	[WeaverGenerated]
	public override void CopyStateToBackingFields()
	{
		base.CopyStateToBackingFields();
	}
}

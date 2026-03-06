using System;
using System.Collections;
using System.Collections.Generic;
using GorillaExtensions;
using JetBrains.Annotations;
using Photon.Pun;
using Photon.Realtime;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(NetworkView))]
public class RequestableOwnershipGuard : MonoBehaviourPunCallbacks, ISelfValidator
{
	[DevInspectorShow]
	[DevInspectorColor("#ff5")]
	public NetworkingState currentState;

	[FormerlySerializedAs("NetworkView")]
	[SerializeField]
	private NetworkView[] netViews;

	[DevInspectorHide]
	[SerializeField]
	private bool autoRegister = true;

	[DevInspectorShow]
	[CanBeNull]
	[SerializeField]
	[SerializeReference]
	public NetPlayer currentOwner;

	[CanBeNull]
	[SerializeField]
	[SerializeReference]
	private NetPlayer currentMasterClient;

	[CanBeNull]
	[SerializeField]
	[SerializeReference]
	private NetPlayer fallbackOwner;

	[CanBeNull]
	[SerializeField]
	[SerializeReference]
	public NetPlayer creator;

	public bool giveCreatorAbsoluteAuthority;

	public bool attemptMasterAssistedTakeoverOnDeny;

	private Action ownershipDenied;

	private Action ownershipRequestAccepted;

	[CanBeNull]
	[SerializeField]
	[SerializeReference]
	[DevInspectorShow]
	public NetPlayer actualOwner;

	public string ownershipRequestNonce;

	public List<IRequestableOwnershipGuardCallbacks> callbacksList = new List<IRequestableOwnershipGuardCallbacks>();

	private NetworkView netView
	{
		get
		{
			if (netViews == null)
			{
				return null;
			}
			if (netViews.Length == 0)
			{
				return null;
			}
			return netViews[0];
		}
	}

	[DevInspectorShow]
	public bool isTrulyMine => object.Equals(actualOwner, NetworkSystem.Instance.LocalPlayer);

	public bool isMine => object.Equals(currentOwner, NetworkSystem.Instance.LocalPlayer);

	private NetworkingState EdCurrentState => currentState;

	private void SetViewToRequest()
	{
		GetComponent<NetworkView>().OwnershipTransfer = OwnershipOption.Request;
	}

	private void BindNetworkViews()
	{
		netViews = GetComponents<NetworkView>();
	}

	public override void OnDisable()
	{
		base.OnDisable();
		RequestableOwnershipGaurdHandler.RemoveViews(netViews, this);
		NetworkSystem.Instance.OnPlayerJoined -= new Action<NetPlayer>(PlayerEnteredRoom);
		NetworkSystem.Instance.OnPlayerLeft -= new Action<NetPlayer>(PlayerLeftRoom);
		NetworkSystem.Instance.OnJoinedRoomEvent -= new Action(JoinedRoom);
		NetworkSystem.Instance.OnMasterClientSwitchedEvent -= new Action<NetPlayer>(MasterClientSwitch);
		currentMasterClient = null;
		currentOwner = null;
		actualOwner = null;
		creator = NetworkSystem.Instance.LocalPlayer;
		currentState = NetworkingState.IsOwner;
	}

	public override void OnEnable()
	{
		base.OnEnable();
		if (autoRegister)
		{
			BindNetworkViews();
		}
		if (netViews == null)
		{
			return;
		}
		RequestableOwnershipGaurdHandler.RegisterViews(netViews, this);
		NetworkSystem.Instance.OnPlayerJoined += new Action<NetPlayer>(PlayerEnteredRoom);
		NetworkSystem.Instance.OnPlayerLeft += new Action<NetPlayer>(PlayerLeftRoom);
		NetworkSystem.Instance.OnJoinedRoomEvent += new Action(JoinedRoom);
		NetworkSystem.Instance.OnMasterClientSwitchedEvent += new Action<NetPlayer>(MasterClientSwitch);
		NetworkSystem instance = NetworkSystem.Instance;
		if ((object)instance != null && instance.InRoom)
		{
			currentMasterClient = NetworkSystem.Instance.MasterClient;
			if (netView.GetView.CreatorActorNr != currentMasterClient?.ActorNumber)
			{
				SetOwnership(NetworkSystem.Instance.GetPlayer(netView.GetView.CreatorActorNr));
			}
			else if (PlayerHasAuthority(NetworkSystem.Instance.LocalPlayer))
			{
				SetOwnership(NetworkSystem.Instance.LocalPlayer);
				currentState = NetworkingState.IsOwner;
			}
			else
			{
				currentState = NetworkingState.IsBlindClient;
				SetOwnership(null);
				RequestTheCurrentOwnerFromAuthority();
			}
		}
		else
		{
			GorillaTagger.OnPlayerSpawned(delegate
			{
				SetOwnership(NetworkSystem.Instance.LocalPlayer);
			});
		}
	}

	private void PlayerEnteredRoom(NetPlayer player)
	{
		try
		{
			if (!player.IsLocal && NetworkSystem.Instance.InRoom && PlayerHasAuthority(NetworkSystem.Instance.LocalPlayer))
			{
				netView.SendRPC("SetOwnershipFromMasterClient", player, currentOwner.GetPlayerRef());
			}
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
	}

	public override void OnPreLeavingRoom()
	{
		if (!PhotonNetwork.InRoom)
		{
			return;
		}
		switch (currentState)
		{
		case NetworkingState.IsClient:
		case NetworkingState.ForcefullyTakingOver:
		case NetworkingState.RequestingOwnership:
			callbacksList.ForEachBackwards(delegate(IRequestableOwnershipGuardCallbacks callback)
			{
				callback.OnMyOwnerLeft();
			});
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case NetworkingState.IsOwner:
		case NetworkingState.IsBlindClient:
		case NetworkingState.RequestingOwnershipWaitingForSight:
		case NetworkingState.ForcefullyTakingOverWaitingForSight:
			break;
		}
		SetOwnership(NetworkSystem.Instance.LocalPlayer);
	}

	private void JoinedRoom()
	{
		currentMasterClient = NetworkSystem.Instance.MasterClient;
		if (PlayerHasAuthority(NetworkSystem.Instance.LocalPlayer))
		{
			SetOwnership(NetworkSystem.Instance.LocalPlayer);
			currentState = NetworkingState.IsOwner;
		}
		else
		{
			currentState = NetworkingState.IsBlindClient;
			SetOwnership(null);
		}
	}

	private void PlayerLeftRoom(NetPlayer otherPlayer)
	{
		switch (currentState)
		{
		case NetworkingState.IsClient:
			if (creator != null && object.Equals(creator, otherPlayer))
			{
				callbacksList.ForEachBackwards(delegate(IRequestableOwnershipGuardCallbacks callback)
				{
					callback.OnMyCreatorLeft();
				});
			}
			if (object.Equals(actualOwner, otherPlayer))
			{
				callbacksList.ForEachBackwards(delegate(IRequestableOwnershipGuardCallbacks callback)
				{
					callback.OnMyOwnerLeft();
				});
				if (fallbackOwner != null)
				{
					SetOwnership(fallbackOwner);
				}
				else
				{
					SetOwnership(currentMasterClient);
				}
			}
			break;
		case NetworkingState.IsBlindClient:
			if (PlayerHasAuthority(NetworkSystem.Instance.LocalPlayer))
			{
				SetOwnership(NetworkSystem.Instance.LocalPlayer);
			}
			else
			{
				RequestTheCurrentOwnerFromAuthority();
			}
			break;
		case NetworkingState.ForcefullyTakingOver:
		case NetworkingState.RequestingOwnership:
			if (creator != null && object.Equals(creator, otherPlayer))
			{
				callbacksList.ForEachBackwards(delegate(IRequestableOwnershipGuardCallbacks callback)
				{
					callback.OnMyCreatorLeft();
				});
			}
			if (currentState == NetworkingState.ForcefullyTakingOver && object.Equals(currentOwner, otherPlayer))
			{
				callbacksList.ForEachBackwards(delegate(IRequestableOwnershipGuardCallbacks callback)
				{
					callback.OnMyOwnerLeft();
				});
			}
			if (!object.Equals(actualOwner, otherPlayer))
			{
				break;
			}
			if (fallbackOwner != null)
			{
				SetOwnership(fallbackOwner);
				if (object.Equals(fallbackOwner, PhotonNetwork.LocalPlayer))
				{
					ownershipRequestAccepted?.Invoke();
				}
				else
				{
					ownershipDenied?.Invoke();
				}
			}
			else if (object.Equals(currentMasterClient, PhotonNetwork.LocalPlayer))
			{
				ownershipRequestAccepted?.Invoke();
			}
			else
			{
				ownershipDenied?.Invoke();
			}
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case NetworkingState.IsOwner:
		case NetworkingState.RequestingOwnershipWaitingForSight:
		case NetworkingState.ForcefullyTakingOverWaitingForSight:
			break;
		}
	}

	private void MasterClientSwitch(NetPlayer newMaster)
	{
		switch (currentState)
		{
		case NetworkingState.IsOwner:
		case NetworkingState.IsClient:
			if (actualOwner == null && currentMasterClient == null)
			{
				SetOwnership(newMaster);
			}
			break;
		case NetworkingState.IsBlindClient:
			if (object.Equals(newMaster, NetworkSystem.Instance.LocalPlayer))
			{
				SetOwnership(NetworkSystem.Instance.LocalPlayer);
			}
			else
			{
				RequestTheCurrentOwnerFromAuthority();
			}
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case NetworkingState.ForcefullyTakingOver:
		case NetworkingState.RequestingOwnership:
		case NetworkingState.RequestingOwnershipWaitingForSight:
		case NetworkingState.ForcefullyTakingOverWaitingForSight:
			break;
		}
		currentMasterClient = newMaster;
	}

	[PunRPC]
	public void RequestCurrentOwnerFromAuthorityRPC(PhotonMessageInfo info)
	{
		NetPlayer player = NetworkSystem.Instance.GetPlayer(info.Sender);
		MonkeAgent.IncrementRPCCall(info, "RequestCurrentOwnerFromAuthorityRPC");
		if (PlayerHasAuthority(NetworkSystem.Instance.LocalPlayer) && VRRigCache.Instance.TryGetVrrig(player, out var playerRig) && FXSystem.CheckCallSpam(playerRig.Rig.fxSettings, 22, info.SentServerTime))
		{
			netView.SendRPC("SetOwnershipFromMasterClient", player, actualOwner.GetPlayerRef());
		}
	}

	[PunRPC]
	public void TransferOwnershipFromToRPC([CanBeNull] Player nextplayer, string nonce, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "TransferOwnershipFromToRPC");
		if (nextplayer == null)
		{
			return;
		}
		NetPlayer player = NetworkSystem.Instance.GetPlayer(nextplayer);
		NetPlayer player2 = NetworkSystem.Instance.GetPlayer(info.Sender);
		if (!PlayerHasAuthority(NetworkSystem.Instance.LocalPlayer) && base.photonView.OwnerActorNr != info.Sender.ActorNumber && currentOwner?.ActorNumber != info.Sender.ActorNumber && actualOwner?.ActorNumber != info.Sender.ActorNumber)
		{
			return;
		}
		if (currentOwner == null)
		{
			if (VRRigCache.Instance.TryGetVrrig(player2, out var playerRig) && FXSystem.CheckCallSpam(playerRig.Rig.fxSettings, 22, info.SentServerTime))
			{
				RequestTheCurrentOwnerFromAuthority();
			}
		}
		else
		{
			if (currentOwner.ActorNumber != base.photonView.OwnerActorNr || actualOwner.ActorNumber == player.ActorNumber)
			{
				return;
			}
			switch (currentState)
			{
			case NetworkingState.IsClient:
				SetOwnership(player);
				break;
			case NetworkingState.ForcefullyTakingOver:
			case NetworkingState.RequestingOwnership:
				if (ownershipRequestNonce == nonce)
				{
					ownershipRequestNonce = "";
					SetOwnership(NetworkSystem.Instance.LocalPlayer);
				}
				else
				{
					actualOwner = player;
				}
				break;
			case NetworkingState.RequestingOwnershipWaitingForSight:
			case NetworkingState.ForcefullyTakingOverWaitingForSight:
				RequestTheCurrentOwnerFromAuthority();
				break;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}
	}

	[PunRPC]
	public void SetOwnershipFromMasterClient([CanBeNull] Player nextMaster, PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "SetOwnershipFromMasterClient");
		if (nextMaster != null)
		{
			NetPlayer player = NetworkSystem.Instance.GetPlayer(nextMaster);
			NetPlayer player2 = NetworkSystem.Instance.GetPlayer(info.Sender);
			SetOwnershipFromMasterClient(player, player2);
		}
	}

	public void SetOwnershipFromMasterClient([CanBeNull] NetPlayer nextMaster, NetPlayer sender)
	{
		if (nextMaster == null)
		{
			return;
		}
		if (!PlayerHasAuthority(sender))
		{
			MonkeAgent.instance.SendReport("Sent an SetOwnershipFromMasterClient when they weren't the master client", sender.UserId, sender.NickName);
			return;
		}
		NetworkingState networkingState;
		if (currentOwner == null)
		{
			networkingState = currentState;
			if (networkingState != NetworkingState.IsBlindClient)
			{
				_ = networkingState - 5;
				_ = 1;
			}
		}
		networkingState = currentState;
		if ((uint)(networkingState - 3) <= 3u && object.Equals(nextMaster, PhotonNetwork.LocalPlayer))
		{
			ownershipRequestAccepted?.Invoke();
			SetOwnership(nextMaster);
			return;
		}
		switch (currentState)
		{
		case NetworkingState.IsOwner:
		case NetworkingState.IsBlindClient:
		case NetworkingState.IsClient:
			SetOwnership(nextMaster);
			break;
		case NetworkingState.ForcefullyTakingOverWaitingForSight:
			actualOwner = nextMaster;
			currentState = NetworkingState.ForcefullyTakingOver;
			ownershipRequestNonce = Guid.NewGuid().ToString();
			netView.SendRPC("OwnershipRequested", actualOwner, ownershipRequestNonce);
			break;
		case NetworkingState.ForcefullyTakingOver:
			actualOwner = nextMaster;
			currentState = NetworkingState.ForcefullyTakingOver;
			break;
		case NetworkingState.RequestingOwnershipWaitingForSight:
			SetOwnership(NetworkSystem.Instance.LocalPlayer);
			currentState = NetworkingState.RequestingOwnership;
			ownershipRequestNonce = Guid.NewGuid().ToString();
			netView.SendRPC("OwnershipRequested", actualOwner, ownershipRequestNonce);
			break;
		case NetworkingState.RequestingOwnership:
			SetOwnership(NetworkSystem.Instance.LocalPlayer);
			currentState = NetworkingState.RequestingOwnership;
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	[PunRPC]
	public void OwnershipRequested(string nonce, PhotonMessageInfo info)
	{
		NetPlayer player = NetworkSystem.Instance.GetPlayer(info.Sender);
		MonkeAgent.IncrementRPCCall(info, "OwnershipRequested");
		if ((nonce != null && nonce.Length > 68) || info.Sender == PhotonNetwork.LocalPlayer || !VRRigCache.Instance.TryGetVrrig(player, out var playerRig) || !playerRig.Rig.fxSettings.callSettings[8].CallLimitSettings.CheckCallTime(Time.unscaledTime))
		{
			return;
		}
		bool flag = true;
		foreach (IRequestableOwnershipGuardCallbacks callbacks in callbacksList)
		{
			if (!callbacks.OnOwnershipRequest(player))
			{
				flag = false;
			}
		}
		if (!flag)
		{
			netView.SendRPC("OwnershipRequestDenied", player, nonce);
		}
		else
		{
			TransferOwnership(player, nonce);
		}
	}

	private void TransferOwnershipWithID(int id)
	{
		TransferOwnership(NetworkSystem.Instance.GetPlayer(id));
	}

	public void TransferOwnership(NetPlayer player, string Nonce = "")
	{
		if (NetworkSystem.Instance.InRoom)
		{
			if (base.photonView.IsMine)
			{
				SetOwnership(player);
				netView.SendRPC("TransferOwnershipFromToRPC", RpcTarget.Others, player.GetPlayerRef(), Nonce);
			}
			else if (PlayerHasAuthority(NetworkSystem.Instance.LocalPlayer))
			{
				SetOwnership(player);
				netView.SendRPC("SetOwnershipFromMasterClient", RpcTarget.Others, player.GetPlayerRef());
			}
			else
			{
				Debug.LogError("Tried to transfer ownership when im not the owner or a master client");
			}
		}
		else
		{
			SetOwnership(player);
		}
	}

	public void RequestTheCurrentOwnerFromAuthority()
	{
		netView.SendRPC("RequestCurrentOwnerFromAuthorityRPC", GetAuthoritativePlayer());
	}

	protected void SetCurrentOwner(NetPlayer player)
	{
		if (player == null)
		{
			currentOwner = null;
		}
		else
		{
			currentOwner = player;
		}
		NetworkView[] array = netViews;
		foreach (NetworkView networkView in array)
		{
			if (player == null)
			{
				networkView.OwnerActorNr = -1;
				networkView.ControllerActorNr = -1;
			}
			else
			{
				networkView.OwnerActorNr = player.ActorNumber;
				networkView.ControllerActorNr = player.ActorNumber;
			}
		}
	}

	protected internal void SetOwnership(NetPlayer player, bool isLocalOnly = false, bool dontPropigate = false)
	{
		if (!object.Equals(player, currentOwner) && !dontPropigate)
		{
			callbacksList.ForEachBackwards(delegate(IRequestableOwnershipGuardCallbacks actualOwner)
			{
				actualOwner.OnOwnershipTransferred(player, currentOwner);
			});
		}
		SetCurrentOwner(player);
		if (isLocalOnly)
		{
			return;
		}
		actualOwner = player;
		if (player != null)
		{
			if (player.ActorNumber == NetworkSystem.Instance.LocalPlayer.ActorNumber)
			{
				currentState = NetworkingState.IsOwner;
			}
			else
			{
				currentState = NetworkingState.IsClient;
			}
		}
	}

	public NetPlayer GetAuthoritativePlayer()
	{
		if (giveCreatorAbsoluteAuthority)
		{
			return creator;
		}
		return NetworkSystem.Instance.MasterClient;
	}

	[PunRPC]
	public void OwnershipRequestDenied(string nonce, PhotonMessageInfo info)
	{
		NetPlayer player = NetworkSystem.Instance.GetPlayer(info.Sender);
		MonkeAgent.IncrementRPCCall(info, "OwnershipRequestDenied");
		if (info.Sender.ActorNumber == actualOwner?.ActorNumber || PlayerHasAuthority(player))
		{
			ownershipDenied?.Invoke();
			ownershipDenied = null;
			switch (currentState)
			{
			case NetworkingState.ForcefullyTakingOver:
			case NetworkingState.RequestingOwnership:
				currentState = NetworkingState.IsClient;
				SetOwnership(actualOwner);
				break;
			case NetworkingState.RequestingOwnershipWaitingForSight:
			case NetworkingState.ForcefullyTakingOverWaitingForSight:
				netView.SendRPC("OwnershipRequested", actualOwner, ownershipRequestNonce);
				break;
			default:
				throw new ArgumentOutOfRangeException();
			case NetworkingState.IsOwner:
			case NetworkingState.IsBlindClient:
			case NetworkingState.IsClient:
				break;
			}
		}
	}

	public IEnumerator RequestTimeout()
	{
		Debug.Log($"Timeout request started...  {currentState} ");
		yield return new WaitForSecondsRealtime(2f);
		Debug.Log($"Timeout request ended! {currentState} ");
		switch (currentState)
		{
		case NetworkingState.ForcefullyTakingOver:
		case NetworkingState.RequestingOwnership:
			currentState = NetworkingState.IsClient;
			SetOwnership(actualOwner);
			break;
		case NetworkingState.RequestingOwnershipWaitingForSight:
		case NetworkingState.ForcefullyTakingOverWaitingForSight:
			netView.SendRPC("OwnershipRequested", actualOwner, ownershipRequestNonce);
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case NetworkingState.IsOwner:
		case NetworkingState.IsBlindClient:
		case NetworkingState.IsClient:
			break;
		}
	}

	public void RequestOwnership(Action onRequestSuccess, Action onRequestFailed)
	{
		switch (currentState)
		{
		case NetworkingState.ForcefullyTakingOver:
		case NetworkingState.RequestingOwnership:
		case NetworkingState.RequestingOwnershipWaitingForSight:
		case NetworkingState.ForcefullyTakingOverWaitingForSight:
			ownershipDenied = (Action)Delegate.Combine(ownershipDenied, onRequestFailed);
			StartCoroutine("RequestTimeout");
			break;
		case NetworkingState.IsClient:
			ownershipDenied = (Action)Delegate.Combine(ownershipDenied, onRequestFailed);
			ownershipRequestNonce = Guid.NewGuid().ToString();
			currentState = NetworkingState.RequestingOwnership;
			netView.SendRPC("OwnershipRequested", actualOwner, ownershipRequestNonce);
			StartCoroutine("RequestTimeout");
			break;
		case NetworkingState.IsBlindClient:
			ownershipDenied = (Action)Delegate.Combine(ownershipDenied, onRequestFailed);
			currentState = NetworkingState.RequestingOwnershipWaitingForSight;
			StartCoroutine("RequestTimeout");
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case NetworkingState.IsOwner:
			break;
		}
	}

	public void RequestOwnershipImmediately(Action onRequestFailed)
	{
		Debug.Log("WorldShareable RequestOwnershipImmediately");
		if (PlayerHasAuthority(NetworkSystem.Instance.LocalPlayer))
		{
			RequestOwnershipImmediatelyWithGuaranteedAuthority();
			return;
		}
		switch (currentState)
		{
		case NetworkingState.IsOwner:
			_ = NetworkSystem.Instance.InRoom;
			break;
		case NetworkingState.ForcefullyTakingOver:
		case NetworkingState.RequestingOwnership:
			ownershipDenied = (Action)Delegate.Combine(ownershipDenied, onRequestFailed);
			currentState = NetworkingState.ForcefullyTakingOver;
			StartCoroutine("RequestTimeout");
			break;
		case NetworkingState.IsClient:
			ownershipDenied = (Action)Delegate.Combine(ownershipDenied, onRequestFailed);
			ownershipRequestNonce = Guid.NewGuid().ToString();
			currentState = NetworkingState.ForcefullyTakingOver;
			SetOwnership(NetworkSystem.Instance.LocalPlayer, isLocalOnly: true);
			netView.SendRPC("OwnershipRequested", actualOwner, ownershipRequestNonce);
			StartCoroutine("RequestTimeout");
			break;
		case NetworkingState.IsBlindClient:
			ownershipDenied = (Action)Delegate.Combine(ownershipDenied, onRequestFailed);
			currentState = NetworkingState.ForcefullyTakingOverWaitingForSight;
			SetOwnership(NetworkSystem.Instance.LocalPlayer, isLocalOnly: true);
			RequestTheCurrentOwnerFromAuthority();
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	public void RequestOwnershipImmediatelyWithGuaranteedAuthority()
	{
		Debug.Log("WorldShareable RequestOwnershipImmediatelyWithGuaranteedAuthority");
		if (!PlayerHasAuthority(NetworkSystem.Instance.LocalPlayer))
		{
			Debug.LogError("Tried to request ownership immediately with guaranteed authority without acutely having authority ");
		}
		switch (currentState)
		{
		case NetworkingState.ForcefullyTakingOver:
		case NetworkingState.RequestingOwnership:
			currentState = NetworkingState.ForcefullyTakingOver;
			StartCoroutine("RequestTimeout");
			break;
		case NetworkingState.IsClient:
			currentState = NetworkingState.ForcefullyTakingOver;
			SetOwnership(NetworkSystem.Instance.LocalPlayer, isLocalOnly: true);
			netView.SendRPC("SetOwnershipFromMasterClient", RpcTarget.All, PhotonNetwork.LocalPlayer);
			StartCoroutine("RequestTimeout");
			break;
		case NetworkingState.IsBlindClient:
			currentState = NetworkingState.ForcefullyTakingOverWaitingForSight;
			SetOwnership(NetworkSystem.Instance.LocalPlayer, isLocalOnly: true);
			RequestTheCurrentOwnerFromAuthority();
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case NetworkingState.IsOwner:
			break;
		}
	}

	public void AddCallbackTarget(IRequestableOwnershipGuardCallbacks callbackObject)
	{
		if (!callbacksList.Contains(callbackObject))
		{
			callbacksList.Add(callbackObject);
			if (currentOwner != null)
			{
				callbackObject.OnOwnershipTransferred(currentOwner, null);
			}
		}
	}

	public void RemoveCallbackTarget(IRequestableOwnershipGuardCallbacks callbackObject)
	{
		if (callbacksList.Contains(callbackObject))
		{
			callbacksList.Remove(callbackObject);
			if (currentOwner != null)
			{
				callbackObject.OnOwnershipTransferred(null, currentOwner);
			}
		}
	}

	public void SetCreator(NetPlayer player)
	{
		creator = player;
	}

	public void Validate(SelfValidationResult result)
	{
	}

	public bool PlayerHasAuthority(NetPlayer player)
	{
		return object.Equals(GetAuthoritativePlayer(), player);
	}
}

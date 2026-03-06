using System;
using System.Collections.Generic;
using Fusion;
using GorillaExtensions;
using GorillaNetworking;
using GorillaTag;
using JetBrains.Annotations;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

[NetworkBehaviourWeaved(0)]
public class WorldShareableItem : NetworkComponent, IRequestableOwnershipGuardCallbacks
{
	public delegate void Delegate();

	public delegate void OnOwnerChangeDelegate(NetPlayer newOwner, NetPlayer prevOwner);

	public struct CachedData
	{
		public TransferrableObject.PositionState cachedTransferableObjectState;

		public TransferrableObject.ItemStates cachedTransferableObjectItemState;
	}

	private bool validShareable = true;

	public RequestableOwnershipGuard guard;

	private TransformViewTeleportSerializer teleportSerializer;

	[DevInspectorShow]
	[CanBeNull]
	private WorldTargetItem _target;

	public OnOwnerChangeDelegate onOwnerChangeCb;

	public Action rpcCallBack;

	private bool enableRemoteSync = true;

	public Dictionary<NetPlayer, CachedData> cachedDatas = new Dictionary<NetPlayer, CachedData>();

	[DevInspectorShow]
	public TransferrableObject.PositionState transferableObjectState { get; set; }

	public TransferrableObject.ItemStates transferableObjectItemState { get; set; }

	public TransferrableObject.PositionState transferableObjectStateNetworked { get; set; }

	public TransferrableObject.ItemStates transferableObjectItemStateNetworked { get; set; }

	[DevInspectorShow]
	public WorldTargetItem target
	{
		get
		{
			return _target;
		}
		set
		{
			_target = value;
		}
	}

	[DevInspectorShow]
	public bool EnableRemoteSync
	{
		get
		{
			return enableRemoteSync;
		}
		set
		{
			enableRemoteSync = value;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		guard = GetComponent<RequestableOwnershipGuard>();
		teleportSerializer = GetComponent<TransformViewTeleportSerializer>();
		NetworkSystem.Instance.RegisterSceneNetworkItem(base.gameObject);
	}

	internal override void OnEnable()
	{
		NetworkBehaviourUtils.InternalOnEnable(this);
		if (!GTAppState.isQuitting)
		{
			base.OnEnable();
			guard.AddCallbackTarget(this);
			WorldShareableItemManager.Register(this);
			NetworkSystem.Instance.RegisterSceneNetworkItem(base.gameObject);
		}
	}

	internal override void OnDisable()
	{
		NetworkBehaviourUtils.InternalOnDisable(this);
		base.OnDisable();
		if (target != null && target.transferrableObject.isSceneObject)
		{
			PhotonView[] components = GetComponents<PhotonView>();
			for (int i = 0; i < components.Length; i++)
			{
				components[i].ViewID = 0;
			}
			transferableObjectState = TransferrableObject.PositionState.None;
			transferableObjectItemState = TransferrableObject.ItemStates.State0;
			guard.RemoveCallbackTarget(this);
			rpcCallBack = null;
			onOwnerChangeCb = null;
			WorldShareableItemManager.Unregister(this);
		}
	}

	public void OnDestroy()
	{
		NetworkBehaviourUtils.InternalOnDestroy(this);
		WorldShareableItemManager.Unregister(this);
	}

	public void SetupSharableViewIDs(NetPlayer player, int slotID)
	{
		PhotonView[] components = GetComponents<PhotonView>();
		PhotonView photonView = components[0];
		PhotonView photonView2 = components[1];
		int num = player.ActorNumber * 1000 + 990 + slotID * 2;
		guard.giveCreatorAbsoluteAuthority = true;
		if (num != photonView.ViewID)
		{
			photonView.ViewID = player.ActorNumber * 1000 + 990 + slotID * 2;
			photonView2.ViewID = player.ActorNumber * 1000 + 990 + slotID * 2 + 1;
			guard.SetCreator(player);
		}
	}

	public void ResetViews()
	{
		if (!ApplicationQuittingState.IsQuitting)
		{
			PhotonView[] components = GetComponents<PhotonView>();
			PhotonView photonView = components[0];
			PhotonView obj = components[1];
			photonView.ViewID = 0;
			obj.ViewID = 0;
		}
	}

	public void SetupSharableObject(int itemIDx, NetPlayer owner, Transform targetXform)
	{
		if (target != null)
		{
			Debug.LogError("ERROR!!!  WorldShareableItem.SetupSharableObject: target is expected to be null before this call. In scene path = \"" + base.transform.GetPathQ() + "\"", this);
			return;
		}
		target = WorldTargetItem.GenerateTargetFromPlayerAndID(owner, itemIDx);
		if (target.targetObject != targetXform)
		{
			Debug.LogError($"The target object found a transform that does not match the target transform, this should never happen. owner: {owner} itemIDx: {itemIDx} targetXformPath: {targetXform.GetPath()}, target.targetObject: {target.targetObject.GetPath()}");
		}
		TransferrableObject component = target.targetObject.GetComponent<TransferrableObject>();
		validShareable = component.canDrop || component.shareable || component.allowWorldSharableInstance;
		if (!validShareable)
		{
			Debug.LogError($"tried to setup an invalid shareable {owner} {itemIDx} {targetXform.GetPath()}");
			base.gameObject.SetActive(value: false);
			Invalidate();
		}
		else
		{
			guard.AddCallbackTarget(component);
			guard.giveCreatorAbsoluteAuthority = true;
			component.SetWorldShareableItem(this);
		}
	}

	public override void OnPhotonInstantiate(PhotonMessageInfo info)
	{
		base.OnPhotonInstantiate(info);
	}

	public override void OnOwnerChange(Player newOwner, Player previousOwner)
	{
		if (onOwnerChangeCb != null)
		{
			NetPlayer player = NetworkSystem.Instance.GetPlayer(newOwner);
			NetPlayer player2 = NetworkSystem.Instance.GetPlayer(previousOwner);
			onOwnerChangeCb(player, player2);
		}
	}

	public void TriggeredUpdate()
	{
		if (IsTargetValid())
		{
			if (guard.isTrulyMine)
			{
				target.targetObject.GetPositionAndRotation(out var position, out var rotation);
				base.transform.SetPositionAndRotation(position, rotation);
			}
			else if (!base.IsMine && EnableRemoteSync)
			{
				base.transform.GetPositionAndRotation(out var position2, out var rotation2);
				target.targetObject.SetPositionAndRotation(position2, rotation2);
			}
		}
	}

	public void SyncToSceneObject(TransferrableObject transferrableObject)
	{
		target = WorldTargetItem.GenerateTargetFromWorldSharableItem(null, -2, transferrableObject.transform);
		base.transform.parent = null;
	}

	public void SetupSceneObjectOnNetwork(NetPlayer owner)
	{
		guard.SetOwnership(owner);
	}

	public bool IsTargetValid()
	{
		return target != null;
	}

	public void Invalidate()
	{
		target = null;
		transferableObjectState = TransferrableObject.PositionState.None;
		transferableObjectItemState = TransferrableObject.ItemStates.State0;
	}

	public void OnOwnershipTransferred(NetPlayer toPlayer, NetPlayer fromPlayer)
	{
		if (toPlayer != null && cachedDatas.TryGetValue(toPlayer, out var value))
		{
			transferableObjectState = value.cachedTransferableObjectState;
			transferableObjectItemState = value.cachedTransferableObjectItemState;
			cachedDatas.Remove(toPlayer);
		}
	}

	public override void WriteDataFusion()
	{
		transferableObjectItemStateNetworked = transferableObjectItemState;
		transferableObjectStateNetworked = transferableObjectState;
	}

	public override void ReadDataFusion()
	{
		transferableObjectItemState = transferableObjectItemStateNetworked;
		transferableObjectState = transferableObjectStateNetworked;
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		stream.SendNext(transferableObjectState);
		stream.SendNext(transferableObjectItemState);
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		NetPlayer player = NetworkSystem.Instance.GetPlayer(info.Sender);
		if (player != guard.actualOwner)
		{
			Debug.Log("Blocking info from non owner");
			cachedDatas.AddOrUpdate(player, new CachedData
			{
				cachedTransferableObjectState = (TransferrableObject.PositionState)stream.ReceiveNext(),
				cachedTransferableObjectItemState = (TransferrableObject.ItemStates)stream.ReceiveNext()
			});
		}
		else
		{
			transferableObjectState = (TransferrableObject.PositionState)stream.ReceiveNext();
			transferableObjectItemState = (TransferrableObject.ItemStates)stream.ReceiveNext();
		}
	}

	[PunRPC]
	internal void RPCWorldShareable(PhotonMessageInfo info)
	{
		NetworkSystem.Instance.GetPlayer(info.Sender);
		MonkeAgent.IncrementRPCCall(info, "RPCWorldShareable");
		if (rpcCallBack != null)
		{
			rpcCallBack();
		}
	}

	public bool OnMasterClientAssistedTakeoverRequest(NetPlayer fromPlayer, NetPlayer toPlayer)
	{
		return true;
	}

	public void OnMyCreatorLeft()
	{
	}

	public bool OnOwnershipRequest(NetPlayer fromPlayer)
	{
		return true;
	}

	public void OnMyOwnerLeft()
	{
	}

	public void SetWillTeleport()
	{
		teleportSerializer.SetWillTeleport();
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

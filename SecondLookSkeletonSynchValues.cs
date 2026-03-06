using System;
using Fusion;
using GorillaExtensions;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Scripting;

[NetworkBehaviourWeaved(11)]
public class SecondLookSkeletonSynchValues : NetworkComponent
{
	public SecondLookSkeleton.GhostState currentState;

	public Vector3 position;

	public Quaternion rotation;

	public SecondLookSkeleton mySkeleton;

	public int currentNode;

	public int nextNode;

	public int angerPoint;

	[WeaverGenerated]
	[SerializeField]
	[DefaultForProperty("NetData", 0, 11)]
	[DrawIf("IsEditorWritable", true, CompareOperator.Equal, DrawIfMode.ReadOnly)]
	private SkeletonNetData _NetData;

	[Networked]
	[NetworkedWeaved(0, 11)]
	public unsafe SkeletonNetData NetData
	{
		get
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing SecondLookSkeletonSynchValues.NetData. Networked properties can only be accessed when Spawned() has been called.");
			}
			return *(SkeletonNetData*)((byte*)((NetworkBehaviour)this).Ptr + 0);
		}
		set
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing SecondLookSkeletonSynchValues.NetData. Networked properties can only be accessed when Spawned() has been called.");
			}
			*(SkeletonNetData*)((byte*)((NetworkBehaviour)this).Ptr + 0) = value;
		}
	}

	protected override void OnOwnerSwitched(NetPlayer newOwningPlayer)
	{
		base.OnOwnerSwitched(newOwningPlayer);
		if (newOwningPlayer.IsLocal)
		{
			mySkeleton.SetNodes();
			if (mySkeleton.currentState != currentState)
			{
				mySkeleton.ChangeState(currentState);
			}
		}
	}

	public override void WriteDataFusion()
	{
		NetData = new SkeletonNetData((int)currentState, position, rotation, currentNode, nextNode, angerPoint);
	}

	public override void ReadDataFusion()
	{
		currentState = (SecondLookSkeleton.GhostState)NetData.CurrentState;
		position.SetValueSafe(NetData.Position);
		rotation.SetValueSafe(NetData.Rotation);
		currentNode = NetData.CurrentNode;
		nextNode = NetData.NextNode;
		angerPoint = NetData.AngerPoint;
		if (mySkeleton.tapped && currentState != mySkeleton.currentState)
		{
			mySkeleton.ChangeState(currentState);
		}
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (base.IsMine || info.Sender.IsMasterClient)
		{
			stream.SendNext(mySkeleton.currentState);
			stream.SendNext(mySkeleton.spookyGhost.transform.position);
			stream.SendNext(mySkeleton.spookyGhost.transform.rotation);
			stream.SendNext(currentNode);
			stream.SendNext(nextNode);
			stream.SendNext(angerPoint);
		}
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (base.IsMine || info.Sender.IsMasterClient)
		{
			currentState = (SecondLookSkeleton.GhostState)stream.ReceiveNext();
			position.SetValueSafe((Vector3)stream.ReceiveNext());
			rotation.SetValueSafe((Quaternion)stream.ReceiveNext());
			currentNode = (int)stream.ReceiveNext();
			nextNode = (int)stream.ReceiveNext();
			angerPoint = (int)stream.ReceiveNext();
			if (mySkeleton.tapped && currentState != mySkeleton.currentState)
			{
				mySkeleton.ChangeState(currentState);
			}
		}
	}

	[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
	public unsafe void RPC_RemoteActiveGhost(RpcInfo info = default(RpcInfo))
	{
		if (((NetworkBehaviour)this).InvokeRpc)
		{
			((NetworkBehaviour)this).InvokeRpc = false;
		}
		else
		{
			NetworkBehaviourUtils.ThrowIfBehaviourNotInitialized(this);
			if (base.Runner.Stage == SimulationStages.Resimulate)
			{
				return;
			}
			int localAuthorityMask = base.Object.GetLocalAuthorityMask();
			if ((localAuthorityMask & 7) == 0)
			{
				NetworkBehaviourUtils.NotifyLocalSimulationNotAllowedToSendRpc("System.Void SecondLookSkeletonSynchValues::RPC_RemoteActiveGhost(Fusion.RpcInfo)", base.Object, 7);
				return;
			}
			if ((localAuthorityMask & 1) != 1)
			{
				int num = 8;
				if (!SimulationMessage.CanAllocateUserPayload(num))
				{
					NetworkBehaviourUtils.NotifyRpcPayloadSizeExceeded("System.Void SecondLookSkeletonSynchValues::RPC_RemoteActiveGhost(Fusion.RpcInfo)", num);
					return;
				}
				if (base.Runner.HasAnyActiveConnections())
				{
					SimulationMessage* ptr = SimulationMessage.Allocate(base.Runner.Simulation, num);
					byte* ptr2 = (byte*)ptr + 28;
					*(RpcHeader*)ptr2 = RpcHeader.Create(base.Object.Id, ((NetworkBehaviour)this).ObjectIndex, 1);
					int num2 = 8;
					ptr->Offset = num2 * 8;
					base.Runner.SendRpc(ptr);
				}
				if ((localAuthorityMask & 1) == 0)
				{
					return;
				}
			}
			info = RpcInfo.FromLocal(base.Runner, RpcChannel.Reliable, RpcHostMode.SourceIsServer);
		}
		MonkeAgent.IncrementRPCCall(info, "RPC_RemoteActiveGhost");
		if (base.IsMine)
		{
			mySkeleton.RemoteActivateGhost();
		}
	}

	[Rpc(RpcSources.All, RpcTargets.All)]
	public unsafe void RPC_RemotePlayerSeen(RpcInfo info = default(RpcInfo))
	{
		if (((NetworkBehaviour)this).InvokeRpc)
		{
			((NetworkBehaviour)this).InvokeRpc = false;
		}
		else
		{
			NetworkBehaviourUtils.ThrowIfBehaviourNotInitialized(this);
			if (base.Runner.Stage == SimulationStages.Resimulate)
			{
				return;
			}
			int localAuthorityMask = base.Object.GetLocalAuthorityMask();
			if ((localAuthorityMask & 7) == 0)
			{
				NetworkBehaviourUtils.NotifyLocalSimulationNotAllowedToSendRpc("System.Void SecondLookSkeletonSynchValues::RPC_RemotePlayerSeen(Fusion.RpcInfo)", base.Object, 7);
				return;
			}
			int num = 8;
			if (!SimulationMessage.CanAllocateUserPayload(num))
			{
				NetworkBehaviourUtils.NotifyRpcPayloadSizeExceeded("System.Void SecondLookSkeletonSynchValues::RPC_RemotePlayerSeen(Fusion.RpcInfo)", num);
				return;
			}
			if (base.Runner.HasAnyActiveConnections())
			{
				SimulationMessage* ptr = SimulationMessage.Allocate(base.Runner.Simulation, num);
				byte* ptr2 = (byte*)ptr + 28;
				*(RpcHeader*)ptr2 = RpcHeader.Create(base.Object.Id, ((NetworkBehaviour)this).ObjectIndex, 2);
				int num2 = 8;
				ptr->Offset = num2 * 8;
				base.Runner.SendRpc(ptr);
			}
			if ((localAuthorityMask & 7) == 0)
			{
				return;
			}
			info = RpcInfo.FromLocal(base.Runner, RpcChannel.Reliable, RpcHostMode.SourceIsServer);
		}
		MonkeAgent.IncrementRPCCall(info, "RPC_RemotePlayerSeen");
		NetPlayer player = NetworkSystem.Instance.GetPlayer(info.Source);
		if (!mySkeleton.playersSeen.Contains(player))
		{
			mySkeleton.RemotePlayerSeen(player);
		}
	}

	[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
	public unsafe void RPC_RemotePlayerCaught(RpcInfo info = default(RpcInfo))
	{
		if (((NetworkBehaviour)this).InvokeRpc)
		{
			((NetworkBehaviour)this).InvokeRpc = false;
		}
		else
		{
			NetworkBehaviourUtils.ThrowIfBehaviourNotInitialized(this);
			if (base.Runner.Stage == SimulationStages.Resimulate)
			{
				return;
			}
			int localAuthorityMask = base.Object.GetLocalAuthorityMask();
			if ((localAuthorityMask & 7) == 0)
			{
				NetworkBehaviourUtils.NotifyLocalSimulationNotAllowedToSendRpc("System.Void SecondLookSkeletonSynchValues::RPC_RemotePlayerCaught(Fusion.RpcInfo)", base.Object, 7);
				return;
			}
			if ((localAuthorityMask & 1) != 1)
			{
				int num = 8;
				if (!SimulationMessage.CanAllocateUserPayload(num))
				{
					NetworkBehaviourUtils.NotifyRpcPayloadSizeExceeded("System.Void SecondLookSkeletonSynchValues::RPC_RemotePlayerCaught(Fusion.RpcInfo)", num);
					return;
				}
				if (base.Runner.HasAnyActiveConnections())
				{
					SimulationMessage* ptr = SimulationMessage.Allocate(base.Runner.Simulation, num);
					byte* ptr2 = (byte*)ptr + 28;
					*(RpcHeader*)ptr2 = RpcHeader.Create(base.Object.Id, ((NetworkBehaviour)this).ObjectIndex, 3);
					int num2 = 8;
					ptr->Offset = num2 * 8;
					base.Runner.SendRpc(ptr);
				}
				if ((localAuthorityMask & 1) == 0)
				{
					return;
				}
			}
			info = RpcInfo.FromLocal(base.Runner, RpcChannel.Reliable, RpcHostMode.SourceIsServer);
		}
		MonkeAgent.IncrementRPCCall(info, "RPC_RemotePlayerCaught");
		if (base.IsMine)
		{
			NetPlayer player = NetworkSystem.Instance.GetPlayer(info.Source);
			if (mySkeleton.currentState == SecondLookSkeleton.GhostState.Chasing)
			{
				mySkeleton.RemotePlayerCaught(player);
			}
		}
	}

	[PunRPC]
	public void RemoteActivateGhost(PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "RemoteActivateGhost");
		if (base.IsMine)
		{
			mySkeleton.RemoteActivateGhost();
		}
	}

	[PunRPC]
	public void RemotePlayerSeen(PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "RemotePlayerSeen");
		NetPlayer player = NetworkSystem.Instance.GetPlayer(info.Sender);
		if (!mySkeleton.playersSeen.Contains(player))
		{
			mySkeleton.RemotePlayerSeen(player);
		}
	}

	[PunRPC]
	public void RemotePlayerCaught(PhotonMessageInfo info)
	{
		MonkeAgent.IncrementRPCCall(info, "RemotePlayerCaught");
		if (base.IsMine)
		{
			NetPlayer player = NetworkSystem.Instance.GetPlayer(info.Sender);
			if (mySkeleton.currentState == SecondLookSkeleton.GhostState.Chasing)
			{
				mySkeleton.RemotePlayerCaught(player);
			}
		}
	}

	[WeaverGenerated]
	public override void CopyBackingFieldsToState(bool P_0)
	{
		base.CopyBackingFieldsToState(P_0);
		NetData = _NetData;
	}

	[WeaverGenerated]
	public override void CopyStateToBackingFields()
	{
		base.CopyStateToBackingFields();
		_NetData = NetData;
	}

	[NetworkRpcWeavedInvoker(1, 7, 1)]
	[Preserve]
	[WeaverGenerated]
	protected unsafe static void RPC_RemoteActiveGhost_0040Invoker(NetworkBehaviour behaviour, SimulationMessage* message)
	{
		byte* ptr = (byte*)message + 28;
		int num = 8;
		RpcInfo info = RpcInfo.FromMessage(behaviour.Runner, message, RpcHostMode.SourceIsServer);
		behaviour.InvokeRpc = true;
		((SecondLookSkeletonSynchValues)behaviour).RPC_RemoteActiveGhost(info);
	}

	[NetworkRpcWeavedInvoker(2, 7, 7)]
	[Preserve]
	[WeaverGenerated]
	protected unsafe static void RPC_RemotePlayerSeen_0040Invoker(NetworkBehaviour behaviour, SimulationMessage* message)
	{
		byte* ptr = (byte*)message + 28;
		int num = 8;
		RpcInfo info = RpcInfo.FromMessage(behaviour.Runner, message, RpcHostMode.SourceIsServer);
		behaviour.InvokeRpc = true;
		((SecondLookSkeletonSynchValues)behaviour).RPC_RemotePlayerSeen(info);
	}

	[NetworkRpcWeavedInvoker(3, 7, 1)]
	[Preserve]
	[WeaverGenerated]
	protected unsafe static void RPC_RemotePlayerCaught_0040Invoker(NetworkBehaviour behaviour, SimulationMessage* message)
	{
		byte* ptr = (byte*)message + 28;
		int num = 8;
		RpcInfo info = RpcInfo.FromMessage(behaviour.Runner, message, RpcHostMode.SourceIsServer);
		behaviour.InvokeRpc = true;
		((SecondLookSkeletonSynchValues)behaviour).RPC_RemotePlayerCaught(info);
	}
}

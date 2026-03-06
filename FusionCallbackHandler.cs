using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.Scripting;

public class FusionCallbackHandler : SimulationBehaviour, INetworkRunnerCallbacks, IPublicFacingInterface
{
	private NetworkSystemFusion parent;

	public void Setup(NetworkSystemFusion parentController)
	{
		parent = parentController;
		parent.runner.AddCallbacks(this);
	}

	private void OnDestroy()
	{
		NetworkBehaviourUtils.InternalOnDestroy(this);
		RemoveCallbacks();
	}

	private async void RemoveCallbacks()
	{
		await Task.Delay(500);
		parent.runner.RemoveCallbacks(this);
	}

	public void OnConnectedToServer(NetworkRunner runner)
	{
		parent.OnJoinedSession();
	}

	public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
	{
		parent.OnJoinFailed(reason);
	}

	public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
	{
	}

	public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
	{
		parent.CustomAuthenticationResponse(data);
		Debug.Log("Received custom auth response:");
		foreach (KeyValuePair<string, object> datum in data)
		{
			Debug.Log(datum.Key + ":" + (datum.Value as string));
		}
	}

	public void OnDisconnectedFromServer(NetworkRunner runner)
	{
		parent.OnDisconnectedFromSession();
	}

	public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
	{
		parent.MigrateHost(runner, hostMigrationToken);
	}

	public void OnInput(NetworkRunner runner, NetworkInput input)
	{
		NetworkedInput input2 = NetInput.GetInput();
		input.Set(input2);
	}

	public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
	{
	}

	public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
	{
		parent.OnFusionPlayerJoined(player);
	}

	public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
	{
		parent.OnFusionPlayerLeft(player);
	}

	public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data)
	{
	}

	public void OnSceneLoadDone(NetworkRunner runner)
	{
	}

	public void OnSceneLoadStart(NetworkRunner runner)
	{
	}

	public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
	{
	}

	public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
	{
		parent.OnRunnerShutDown();
	}

	public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
	{
	}

	[Rpc(Channel = RpcChannel.Reliable)]
	public unsafe static void RPC_OnEventRaisedReliable(NetworkRunner runner, byte eventCode, byte[] byteData, bool hasOps, byte[] netOptsData, RpcInfo info = default(RpcInfo))
	{
		if (NetworkBehaviourUtils.InvokeRpc)
		{
			NetworkBehaviourUtils.InvokeRpc = false;
		}
		else
		{
			if ((object)runner == null)
			{
				throw new ArgumentNullException("runner");
			}
			if (runner.Stage == SimulationStages.Resimulate)
			{
				return;
			}
			int num = 8;
			num += 4;
			num += (byteData.Length * 1 + 4 + 3) & -4;
			num += 4;
			num += (netOptsData.Length * 1 + 4 + 3) & -4;
			if (!SimulationMessage.CanAllocateUserPayload(num))
			{
				NetworkBehaviourUtils.NotifyRpcPayloadSizeExceeded("System.Void FusionCallbackHandler::RPC_OnEventRaisedReliable(Fusion.NetworkRunner,System.Byte,System.Byte[],System.Boolean,System.Byte[],Fusion.RpcInfo)", num);
				return;
			}
			if (runner.HasAnyActiveConnections())
			{
				SimulationMessage* ptr = SimulationMessage.Allocate(runner.Simulation, num);
				byte* ptr2 = (byte*)ptr + 28;
				*(RpcHeader*)ptr2 = RpcHeader.Create(NetworkBehaviourUtils.GetRpcStaticIndexOrThrow("System.Void FusionCallbackHandler::RPC_OnEventRaisedReliable(Fusion.NetworkRunner,System.Byte,System.Byte[],System.Boolean,System.Byte[],Fusion.RpcInfo)"));
				int num2 = 8;
				ptr2[num2] = eventCode;
				num2 += 4 & -4;
				*(int*)(ptr2 + num2) = byteData.Length;
				num2 += 4;
				num2 = ((Native.CopyFromArray(ptr2 + num2, byteData) + 3) & -4) + num2;
				ReadWriteUtilsForWeaver.WriteBoolean((int*)(ptr2 + num2), hasOps);
				num2 += 4;
				*(int*)(ptr2 + num2) = netOptsData.Length;
				num2 += 4;
				num2 = ((Native.CopyFromArray(ptr2 + num2, netOptsData) + 3) & -4) + num2;
				ptr->Offset = num2 * 8;
				ptr->SetStatic();
				runner.SendRpc(ptr);
			}
			info = RpcInfo.FromLocal(runner, RpcChannel.Reliable, RpcHostMode.SourceIsServer);
		}
		object data = byteData.ByteDeserialize();
		NetEventOptions opts = null;
		if (hasOps)
		{
			opts = (NetEventOptions)netOptsData.ByteDeserialize();
		}
		if (CanRecieveEvent(runner, opts, info))
		{
			NetworkSystem.Instance.RaiseEvent(eventCode, data, info.Source.PlayerId);
		}
	}

	[Rpc(Channel = RpcChannel.Unreliable)]
	public unsafe static void RPC_OnEventRaisedUnreliable(NetworkRunner runner, byte eventCode, byte[] byteData, bool hasOps, byte[] netOptsData, RpcInfo info = default(RpcInfo))
	{
		if (NetworkBehaviourUtils.InvokeRpc)
		{
			NetworkBehaviourUtils.InvokeRpc = false;
		}
		else
		{
			if ((object)runner == null)
			{
				throw new ArgumentNullException("runner");
			}
			if (runner.Stage == SimulationStages.Resimulate)
			{
				return;
			}
			int num = 8;
			num += 4;
			num += (byteData.Length * 1 + 4 + 3) & -4;
			num += 4;
			num += (netOptsData.Length * 1 + 4 + 3) & -4;
			if (!SimulationMessage.CanAllocateUserPayload(num))
			{
				NetworkBehaviourUtils.NotifyRpcPayloadSizeExceeded("System.Void FusionCallbackHandler::RPC_OnEventRaisedUnreliable(Fusion.NetworkRunner,System.Byte,System.Byte[],System.Boolean,System.Byte[],Fusion.RpcInfo)", num);
				return;
			}
			if (runner.HasAnyActiveConnections())
			{
				SimulationMessage* ptr = SimulationMessage.Allocate(runner.Simulation, num);
				byte* ptr2 = (byte*)ptr + 28;
				*(RpcHeader*)ptr2 = RpcHeader.Create(NetworkBehaviourUtils.GetRpcStaticIndexOrThrow("System.Void FusionCallbackHandler::RPC_OnEventRaisedUnreliable(Fusion.NetworkRunner,System.Byte,System.Byte[],System.Boolean,System.Byte[],Fusion.RpcInfo)"));
				int num2 = 8;
				ptr2[num2] = eventCode;
				num2 += 4 & -4;
				*(int*)(ptr2 + num2) = byteData.Length;
				num2 += 4;
				num2 = ((Native.CopyFromArray(ptr2 + num2, byteData) + 3) & -4) + num2;
				ReadWriteUtilsForWeaver.WriteBoolean((int*)(ptr2 + num2), hasOps);
				num2 += 4;
				*(int*)(ptr2 + num2) = netOptsData.Length;
				num2 += 4;
				num2 = ((Native.CopyFromArray(ptr2 + num2, netOptsData) + 3) & -4) + num2;
				ptr->Offset = num2 * 8;
				ptr->SetUnreliable();
				ptr->SetStatic();
				runner.SendRpc(ptr);
			}
			info = RpcInfo.FromLocal(runner, RpcChannel.Unreliable, RpcHostMode.SourceIsServer);
		}
		object data = byteData.ByteDeserialize();
		NetEventOptions opts = null;
		if (hasOps)
		{
			opts = (NetEventOptions)netOptsData.ByteDeserialize();
		}
		if (CanRecieveEvent(runner, opts, info))
		{
			NetworkSystem.Instance.RaiseEvent(eventCode, data, info.Source.PlayerId);
		}
	}

	private static bool CanRecieveEvent(NetworkRunner runner, NetEventOptions opts, RpcInfo info)
	{
		if (opts != null)
		{
			if (opts.Reciever != NetEventOptions.RecieverTarget.all)
			{
				if (opts.Reciever == NetEventOptions.RecieverTarget.master && !NetworkSystem.Instance.IsMasterClient)
				{
					return false;
				}
				if (info.Source == runner.LocalPlayer)
				{
					return false;
				}
			}
			if (opts.TargetActors != null && !Enumerable.Contains(opts.TargetActors, runner.LocalPlayer.PlayerId))
			{
				return false;
			}
		}
		return true;
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

	[NetworkRpcStaticWeavedInvoker("System.Void FusionCallbackHandler::RPC_OnEventRaisedReliable(Fusion.NetworkRunner,System.Byte,System.Byte[],System.Boolean,System.Byte[],Fusion.RpcInfo)")]
	[Preserve]
	[WeaverGenerated]
	protected unsafe static void RPC_OnEventRaisedReliable_0040Invoker(NetworkRunner runner, SimulationMessage* message)
	{
		byte* ptr = (byte*)message + 28;
		int num = 8;
		byte num2 = ptr[num];
		num += 4 & -4;
		byte eventCode = num2;
		byte[] array = new byte[*(int*)(ptr + num)];
		num += 4;
		num = ((Native.CopyToArray(array, ptr + num) + 3) & -4) + num;
		bool num3 = ReadWriteUtilsForWeaver.ReadBoolean((int*)(ptr + num));
		num += 4;
		bool hasOps = num3;
		byte[] array2 = new byte[*(int*)(ptr + num)];
		num += 4;
		num = ((Native.CopyToArray(array2, ptr + num) + 3) & -4) + num;
		RpcInfo info = RpcInfo.FromMessage(runner, message, RpcHostMode.SourceIsServer);
		NetworkBehaviourUtils.InvokeRpc = true;
		RPC_OnEventRaisedReliable(runner, eventCode, array, hasOps, array2, info);
	}

	[NetworkRpcStaticWeavedInvoker("System.Void FusionCallbackHandler::RPC_OnEventRaisedUnreliable(Fusion.NetworkRunner,System.Byte,System.Byte[],System.Boolean,System.Byte[],Fusion.RpcInfo)")]
	[Preserve]
	[WeaverGenerated]
	protected unsafe static void RPC_OnEventRaisedUnreliable_0040Invoker(NetworkRunner runner, SimulationMessage* message)
	{
		byte* ptr = (byte*)message + 28;
		int num = 8;
		byte num2 = ptr[num];
		num += 4 & -4;
		byte eventCode = num2;
		byte[] array = new byte[*(int*)(ptr + num)];
		num += 4;
		num = ((Native.CopyToArray(array, ptr + num) + 3) & -4) + num;
		bool num3 = ReadWriteUtilsForWeaver.ReadBoolean((int*)(ptr + num));
		num += 4;
		bool hasOps = num3;
		byte[] array2 = new byte[*(int*)(ptr + num)];
		num += 4;
		num = ((Native.CopyToArray(array2, ptr + num) + 3) & -4) + num;
		RpcInfo info = RpcInfo.FromMessage(runner, message, RpcHostMode.SourceIsServer);
		NetworkBehaviourUtils.InvokeRpc = true;
		RPC_OnEventRaisedUnreliable(runner, eventCode, array, hasOps, array2, info);
	}
}

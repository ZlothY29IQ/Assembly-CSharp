using System;
using Fusion;
using UnityEngine;
using UnityEngine.Scripting;

public class FusionInternalRPCs : SimulationBehaviour
{
	private static NetworkSystemFusion netSys;

	private void Awake()
	{
		netSys = NetworkSystem.Instance as NetworkSystemFusion;
	}

	[Rpc(RpcSources.All, RpcTargets.All)]
	public unsafe static void RPC_SendPlayerSyncProp(NetworkRunner runner, [RpcTarget] PlayerRef player, PlayerRef playerData, string propKey, string propValue)
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
			switch (runner.GetRpcTargetStatus(player))
			{
			case RpcTargetStatus.Self:
				break;
			case RpcTargetStatus.Unreachable:
				NetworkBehaviourUtils.NotifyRpcTargetUnreachable(player, "System.Void FusionInternalRPCs::RPC_SendPlayerSyncProp(Fusion.NetworkRunner,Fusion.PlayerRef,Fusion.PlayerRef,System.String,System.String)");
				return;
			default:
			{
				int num = 8;
				num += 4;
				num += (ReadWriteUtilsForWeaver.GetByteCountUtf8NoHash(propKey) + 3) & -4;
				num += (ReadWriteUtilsForWeaver.GetByteCountUtf8NoHash(propValue) + 3) & -4;
				if (!SimulationMessage.CanAllocateUserPayload(num))
				{
					NetworkBehaviourUtils.NotifyRpcPayloadSizeExceeded("System.Void FusionInternalRPCs::RPC_SendPlayerSyncProp(Fusion.NetworkRunner,Fusion.PlayerRef,Fusion.PlayerRef,System.String,System.String)", num);
					return;
				}
				SimulationMessage* ptr = SimulationMessage.Allocate(runner.Simulation, num);
				byte* ptr2 = (byte*)ptr + 28;
				*(RpcHeader*)ptr2 = RpcHeader.Create(NetworkBehaviourUtils.GetRpcStaticIndexOrThrow("System.Void FusionInternalRPCs::RPC_SendPlayerSyncProp(Fusion.NetworkRunner,Fusion.PlayerRef,Fusion.PlayerRef,System.String,System.String)"));
				int num2 = 8;
				*(PlayerRef*)(ptr2 + num2) = playerData;
				num2 += 4;
				num2 = ((ReadWriteUtilsForWeaver.WriteStringUtf8NoHash(ptr2 + num2, propKey) + 3) & -4) + num2;
				num2 = ((ReadWriteUtilsForWeaver.WriteStringUtf8NoHash(ptr2 + num2, propValue) + 3) & -4) + num2;
				ptr->Offset = num2 * 8;
				ptr->SetTarget(player);
				ptr->SetStatic();
				runner.SendRpc(ptr);
				return;
			}
			}
		}
		Debug.Log("RPC Setting player prop: " + propKey + " - " + propValue);
	}

	[NetworkRpcStaticWeavedInvoker("System.Void FusionInternalRPCs::RPC_SendPlayerSyncProp(Fusion.NetworkRunner,Fusion.PlayerRef,Fusion.PlayerRef,System.String,System.String)")]
	[Preserve]
	[WeaverGenerated]
	protected unsafe static void RPC_SendPlayerSyncProp_0040Invoker(NetworkRunner runner, SimulationMessage* message)
	{
		byte* ptr = (byte*)message + 28;
		int num = 8;
		PlayerRef target = message->Target;
		PlayerRef playerRef = *(PlayerRef*)(ptr + num);
		num += 4;
		PlayerRef playerData = playerRef;
		num = ((ReadWriteUtilsForWeaver.ReadStringUtf8NoHash(ptr + num, out var result) + 3) & -4) + num;
		num = ((ReadWriteUtilsForWeaver.ReadStringUtf8NoHash(ptr + num, out var result2) + 3) & -4) + num;
		NetworkBehaviourUtils.InvokeRpc = true;
		RPC_SendPlayerSyncProp(runner, target, playerData, result, result2);
	}
}

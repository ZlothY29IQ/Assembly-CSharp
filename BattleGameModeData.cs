using System;
using Fusion;
using UnityEngine;
using UnityEngine.Scripting;

[NetworkBehaviourWeaved(61)]
public class BattleGameModeData : FusionGameModeData
{
	[WeaverGenerated]
	[DefaultForProperty("PaintbrawlData", 0, 61)]
	[DrawIf("IsEditorWritable", true, CompareOperator.Equal, DrawIfMode.ReadOnly)]
	private PaintbrawlData _PaintbrawlData;

	private GorillaPaintbrawlManager battleTarget;

	private GameModeSerializer serializer;

	[Networked]
	[NetworkedWeaved(0, 61)]
	private unsafe PaintbrawlData PaintbrawlData
	{
		get
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing BattleGameModeData.PaintbrawlData. Networked properties can only be accessed when Spawned() has been called.");
			}
			return *(PaintbrawlData*)((byte*)((NetworkBehaviour)this).Ptr + 0);
		}
		set
		{
			if (((NetworkBehaviour)this).Ptr == null)
			{
				throw new InvalidOperationException("Error when accessing BattleGameModeData.PaintbrawlData. Networked properties can only be accessed when Spawned() has been called.");
			}
			*(PaintbrawlData*)((byte*)((NetworkBehaviour)this).Ptr + 0) = value;
		}
	}

	public override object Data
	{
		get
		{
			return PaintbrawlData;
		}
		set
		{
			PaintbrawlData = (PaintbrawlData)value;
		}
	}

	public override void Spawned()
	{
		serializer = GetComponent<GameModeSerializer>();
		battleTarget = (GorillaPaintbrawlManager)serializer.GameModeInstance;
	}

	[Rpc]
	public unsafe void RPC_ReportSlinshotHit(int taggedPlayerID, Vector3 hitLocation, int projectileCount, RpcInfo rpcInfo = default(RpcInfo))
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
				NetworkBehaviourUtils.NotifyLocalSimulationNotAllowedToSendRpc("System.Void BattleGameModeData::RPC_ReportSlinshotHit(System.Int32,UnityEngine.Vector3,System.Int32,Fusion.RpcInfo)", base.Object, 7);
				return;
			}
			int num = 8;
			num += 4;
			num += 12;
			num += 4;
			if (!SimulationMessage.CanAllocateUserPayload(num))
			{
				NetworkBehaviourUtils.NotifyRpcPayloadSizeExceeded("System.Void BattleGameModeData::RPC_ReportSlinshotHit(System.Int32,UnityEngine.Vector3,System.Int32,Fusion.RpcInfo)", num);
				return;
			}
			if (base.Runner.HasAnyActiveConnections())
			{
				SimulationMessage* ptr = SimulationMessage.Allocate(base.Runner.Simulation, num);
				byte* ptr2 = (byte*)ptr + 28;
				*(RpcHeader*)ptr2 = RpcHeader.Create(base.Object.Id, ((NetworkBehaviour)this).ObjectIndex, 1);
				int num2 = 8;
				*(int*)(ptr2 + num2) = taggedPlayerID;
				num2 += 4;
				*(Vector3*)(ptr2 + num2) = hitLocation;
				num2 += 12;
				*(int*)(ptr2 + num2) = projectileCount;
				num2 += 4;
				ptr->Offset = num2 * 8;
				base.Runner.SendRpc(ptr);
			}
			if ((localAuthorityMask & 7) == 0)
			{
				return;
			}
			rpcInfo = RpcInfo.FromLocal(base.Runner, RpcChannel.Reliable, RpcHostMode.SourceIsServer);
		}
		PhotonMessageInfoWrapped photonMessageInfoWrapped = new PhotonMessageInfoWrapped(rpcInfo);
		MonkeAgent.IncrementRPCCall(photonMessageInfoWrapped, "RPC_ReportSlinshotHit");
		if (NetworkSystem.Instance.IsMasterClient)
		{
			NetPlayer player = NetworkSystem.Instance.GetPlayer(taggedPlayerID);
			battleTarget.ReportSlingshotHit(player, hitLocation, projectileCount, photonMessageInfoWrapped);
		}
	}

	[WeaverGenerated]
	public override void CopyBackingFieldsToState(bool P_0)
	{
		base.CopyBackingFieldsToState(P_0);
		PaintbrawlData = _PaintbrawlData;
	}

	[WeaverGenerated]
	public override void CopyStateToBackingFields()
	{
		base.CopyStateToBackingFields();
		_PaintbrawlData = PaintbrawlData;
	}

	[NetworkRpcWeavedInvoker(1, 7, 7)]
	[Preserve]
	[WeaverGenerated]
	protected unsafe static void RPC_ReportSlinshotHit_0040Invoker(NetworkBehaviour behaviour, SimulationMessage* message)
	{
		byte* ptr = (byte*)message + 28;
		int num = 8;
		int num2 = *(int*)(ptr + num);
		num += 4;
		int taggedPlayerID = num2;
		Vector3 vector = *(Vector3*)(ptr + num);
		num += 12;
		Vector3 hitLocation = vector;
		int num3 = *(int*)(ptr + num);
		num += 4;
		int projectileCount = num3;
		RpcInfo rpcInfo = RpcInfo.FromMessage(behaviour.Runner, message, RpcHostMode.SourceIsServer);
		behaviour.InvokeRpc = true;
		((BattleGameModeData)behaviour).RPC_ReportSlinshotHit(taggedPlayerID, hitLocation, projectileCount, rpcInfo);
	}
}

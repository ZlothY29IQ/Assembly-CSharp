using System;
using System.Collections.Generic;
using Fusion;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Scripting;

namespace GorillaLocomotion.Gameplay;

public class RopeSwingManager : NetworkSceneObject
{
	private Dictionary<int, GorillaRopeSwing> ropes = new Dictionary<int, GorillaRopeSwing>();

	public static RopeSwingManager instance { get; private set; }

	private void Awake()
	{
		if (instance != null && instance != this)
		{
			GTDev.LogWarning("Instance of RopeSwingManager already exists. Destroying.");
			UnityEngine.Object.Destroy(this);
		}
		else if (instance == null)
		{
			instance = this;
		}
	}

	private void RegisterInstance(GorillaRopeSwing t)
	{
		ropes.Add(t.ropeId, t);
	}

	private void UnregisterInstance(GorillaRopeSwing t)
	{
		ropes.Remove(t.ropeId);
	}

	public static void Register(GorillaRopeSwing t)
	{
		instance.RegisterInstance(t);
	}

	public static void Unregister(GorillaRopeSwing t)
	{
		instance.UnregisterInstance(t);
	}

	public void SendSetVelocity_RPC(int ropeId, int boneIndex, Vector3 velocity, bool wholeRope)
	{
		if (NetworkSystem.Instance.InRoom)
		{
			photonView.RPC("SetVelocity", RpcTarget.All, ropeId, boneIndex, velocity, wholeRope);
		}
		else
		{
			SetVelocityShared(ropeId, boneIndex, velocity, wholeRope, default(PhotonMessageInfoWrapped));
		}
	}

	public bool TryGetRope(int ropeId, out GorillaRopeSwing result)
	{
		return ropes.TryGetValue(ropeId, out result);
	}

	[PunRPC]
	public void SetVelocity(int ropeId, int boneIndex, Vector3 velocity, bool wholeRope, PhotonMessageInfo info)
	{
		PhotonMessageInfoWrapped info2 = new PhotonMessageInfoWrapped(info);
		SetVelocityShared(ropeId, boneIndex, velocity, wholeRope, info2);
		Utils.Log("Receiving RPC for ropes");
	}

	[Rpc]
	public unsafe static void RPC_SetVelocity(NetworkRunner runner, int ropeId, int boneIndex, Vector3 velocity, bool wholeRope, RpcInfo info = default(RpcInfo))
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
			num += 4;
			num += 12;
			num += 4;
			if (!SimulationMessage.CanAllocateUserPayload(num))
			{
				NetworkBehaviourUtils.NotifyRpcPayloadSizeExceeded("System.Void GorillaLocomotion.Gameplay.RopeSwingManager::RPC_SetVelocity(Fusion.NetworkRunner,System.Int32,System.Int32,UnityEngine.Vector3,System.Boolean,Fusion.RpcInfo)", num);
				return;
			}
			if (runner.HasAnyActiveConnections())
			{
				SimulationMessage* ptr = SimulationMessage.Allocate(runner.Simulation, num);
				byte* ptr2 = (byte*)ptr + 28;
				*(RpcHeader*)ptr2 = RpcHeader.Create(NetworkBehaviourUtils.GetRpcStaticIndexOrThrow("System.Void GorillaLocomotion.Gameplay.RopeSwingManager::RPC_SetVelocity(Fusion.NetworkRunner,System.Int32,System.Int32,UnityEngine.Vector3,System.Boolean,Fusion.RpcInfo)"));
				int num2 = 8;
				*(int*)(ptr2 + num2) = ropeId;
				num2 += 4;
				*(int*)(ptr2 + num2) = boneIndex;
				num2 += 4;
				*(Vector3*)(ptr2 + num2) = velocity;
				num2 += 12;
				ReadWriteUtilsForWeaver.WriteBoolean((int*)(ptr2 + num2), wholeRope);
				num2 += 4;
				ptr->Offset = num2 * 8;
				ptr->SetStatic();
				runner.SendRpc(ptr);
			}
			info = RpcInfo.FromLocal(runner, RpcChannel.Reliable, RpcHostMode.SourceIsServer);
		}
		PhotonMessageInfoWrapped info2 = new PhotonMessageInfoWrapped(info);
		instance.SetVelocityShared(ropeId, boneIndex, velocity, wholeRope, info2);
	}

	private void SetVelocityShared(int ropeId, int boneIndex, Vector3 velocity, bool wholeRope, PhotonMessageInfoWrapped info)
	{
		if (info.Sender != null)
		{
			MonkeAgent.IncrementRPCCall(info, "SetVelocityShared");
		}
		if (TryGetRope(ropeId, out var result) && result != null)
		{
			result.SetVelocity(boneIndex, velocity, wholeRope, info);
		}
	}

	[NetworkRpcStaticWeavedInvoker("System.Void GorillaLocomotion.Gameplay.RopeSwingManager::RPC_SetVelocity(Fusion.NetworkRunner,System.Int32,System.Int32,UnityEngine.Vector3,System.Boolean,Fusion.RpcInfo)")]
	[Preserve]
	[WeaverGenerated]
	protected unsafe static void RPC_SetVelocity_0040Invoker(NetworkRunner runner, SimulationMessage* message)
	{
		byte* ptr = (byte*)message + 28;
		int num = 8;
		int num2 = *(int*)(ptr + num);
		num += 4;
		int ropeId = num2;
		int num3 = *(int*)(ptr + num);
		num += 4;
		int boneIndex = num3;
		Vector3 vector = *(Vector3*)(ptr + num);
		num += 12;
		Vector3 velocity = vector;
		bool num4 = ReadWriteUtilsForWeaver.ReadBoolean((int*)(ptr + num));
		num += 4;
		bool wholeRope = num4;
		RpcInfo info = RpcInfo.FromMessage(runner, message, RpcHostMode.SourceIsServer);
		NetworkBehaviourUtils.InvokeRpc = true;
		RPC_SetVelocity(runner, ropeId, boneIndex, velocity, wholeRope, info);
	}
}

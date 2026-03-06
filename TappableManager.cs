using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Fusion;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Scripting;

public class TappableManager : NetworkSceneObject
{
	private static TappableManager gManager;

	[SerializeField]
	private List<Tappable> tappables = new List<Tappable>();

	private HashSet<int> idSet = new HashSet<int>();

	private static HashSet<Tappable> gRegistry = new HashSet<Tappable>();

	private void Awake()
	{
		if (gManager != null && gManager != this)
		{
			GTDev.LogWarning("Instance of TappableManager already exists. Destroying.");
			UnityEngine.Object.Destroy(this);
			return;
		}
		if (gManager == null)
		{
			gManager = this;
		}
		if (gRegistry.Count == 0)
		{
			return;
		}
		Tappable[] array = gRegistry.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			if (!(array[i] == null))
			{
				RegisterInstance(array[i]);
			}
		}
		gRegistry.Clear();
	}

	private void RegisterInstance(Tappable t)
	{
		if (t == null)
		{
			GTDev.LogError("Tappable is null.");
			return;
		}
		t.manager = this;
		if (idSet.Add(t.tappableId))
		{
			tappables.Add(t);
		}
	}

	private void UnregisterInstance(Tappable t)
	{
		if (!(t == null) && idSet.Remove(t.tappableId))
		{
			tappables.Remove(t);
			t.manager = null;
		}
	}

	public static void Register(Tappable t)
	{
		if (gManager != null)
		{
			gManager.RegisterInstance(t);
		}
		else
		{
			gRegistry.Add(t);
		}
	}

	public static void Unregister(Tappable t)
	{
		if (gManager != null)
		{
			gManager.UnregisterInstance(t);
		}
		else
		{
			gRegistry.Remove(t);
		}
	}

	[Conditional("QATESTING")]
	public void DebugTestTap()
	{
		if (tappables.Count > 0)
		{
			int index = UnityEngine.Random.Range(0, tappables.Count);
			UnityEngine.Debug.Log("Send TestTap to tappable index: " + index + "/" + tappables.Count);
			tappables[index].OnTap(10f);
		}
		else
		{
			UnityEngine.Debug.Log("TappableManager: tappables array is empty.");
		}
	}

	[PunRPC]
	public void SendOnTapRPC(int key, float tapStrength, PhotonMessageInfo info)
	{
		SendOnTapShared(key, tapStrength, new PhotonMessageInfoWrapped(info));
	}

	[Rpc]
	public unsafe static void RPC_SendOnTap(NetworkRunner runner, int key, float tapStrength, RpcInfo info = default(RpcInfo))
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
			if (!SimulationMessage.CanAllocateUserPayload(num))
			{
				NetworkBehaviourUtils.NotifyRpcPayloadSizeExceeded("System.Void TappableManager::RPC_SendOnTap(Fusion.NetworkRunner,System.Int32,System.Single,Fusion.RpcInfo)", num);
				return;
			}
			if (runner.HasAnyActiveConnections())
			{
				SimulationMessage* ptr = SimulationMessage.Allocate(runner.Simulation, num);
				byte* ptr2 = (byte*)ptr + 28;
				*(RpcHeader*)ptr2 = RpcHeader.Create(NetworkBehaviourUtils.GetRpcStaticIndexOrThrow("System.Void TappableManager::RPC_SendOnTap(Fusion.NetworkRunner,System.Int32,System.Single,Fusion.RpcInfo)"));
				int num2 = 8;
				*(int*)(ptr2 + num2) = key;
				num2 += 4;
				*(float*)(ptr2 + num2) = tapStrength;
				num2 += 4;
				ptr->Offset = num2 * 8;
				ptr->SetStatic();
				runner.SendRpc(ptr);
			}
			info = RpcInfo.FromLocal(runner, RpcChannel.Reliable, RpcHostMode.SourceIsServer);
		}
		gManager.SendOnTapShared(key, tapStrength, new PhotonMessageInfoWrapped(info));
	}

	private void SendOnTapShared(int key, float tapStrength, PhotonMessageInfoWrapped info)
	{
		MonkeAgent.IncrementRPCCall(info, "SendOnTapShared");
		if (key == 0 || !float.IsFinite(tapStrength))
		{
			return;
		}
		tapStrength = Mathf.Clamp(tapStrength, 0f, 1f);
		for (int i = 0; i < tappables.Count; i++)
		{
			Tappable tappable = tappables[i];
			if (tappable.tappableId == key)
			{
				tappable.OnTapLocal(tapStrength, Time.time, info);
			}
		}
	}

	[PunRPC]
	public void SendOnGrabRPC(int key, PhotonMessageInfo info)
	{
		SendOnGrabShared(key, new PhotonMessageInfoWrapped(info));
	}

	[Rpc]
	public unsafe static void RPC_SendOnGrab(NetworkRunner runner, int key, RpcInfo info = default(RpcInfo))
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
			if (!SimulationMessage.CanAllocateUserPayload(num))
			{
				NetworkBehaviourUtils.NotifyRpcPayloadSizeExceeded("System.Void TappableManager::RPC_SendOnGrab(Fusion.NetworkRunner,System.Int32,Fusion.RpcInfo)", num);
				return;
			}
			if (runner.HasAnyActiveConnections())
			{
				SimulationMessage* ptr = SimulationMessage.Allocate(runner.Simulation, num);
				byte* ptr2 = (byte*)ptr + 28;
				*(RpcHeader*)ptr2 = RpcHeader.Create(NetworkBehaviourUtils.GetRpcStaticIndexOrThrow("System.Void TappableManager::RPC_SendOnGrab(Fusion.NetworkRunner,System.Int32,Fusion.RpcInfo)"));
				int num2 = 8;
				*(int*)(ptr2 + num2) = key;
				num2 += 4;
				ptr->Offset = num2 * 8;
				ptr->SetStatic();
				runner.SendRpc(ptr);
			}
			info = RpcInfo.FromLocal(runner, RpcChannel.Reliable, RpcHostMode.SourceIsServer);
		}
		gManager.SendOnGrabShared(key, new PhotonMessageInfoWrapped(info));
	}

	private void SendOnGrabShared(int key, PhotonMessageInfoWrapped info)
	{
		MonkeAgent.IncrementRPCCall(info, "SendOnGrabShared");
		if (key == 0)
		{
			return;
		}
		for (int i = 0; i < tappables.Count; i++)
		{
			Tappable tappable = tappables[i];
			if (tappable.tappableId == key)
			{
				tappable.OnGrabLocal(Time.time, info);
			}
		}
	}

	[PunRPC]
	public void SendOnReleaseRPC(int key, PhotonMessageInfo info)
	{
		SendOnReleaseShared(key, new PhotonMessageInfoWrapped(info));
	}

	[Rpc]
	public unsafe static void RPC_SendOnRelease(NetworkRunner runner, int key, RpcInfo info = default(RpcInfo))
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
			if (!SimulationMessage.CanAllocateUserPayload(num))
			{
				NetworkBehaviourUtils.NotifyRpcPayloadSizeExceeded("System.Void TappableManager::RPC_SendOnRelease(Fusion.NetworkRunner,System.Int32,Fusion.RpcInfo)", num);
				return;
			}
			if (runner.HasAnyActiveConnections())
			{
				SimulationMessage* ptr = SimulationMessage.Allocate(runner.Simulation, num);
				byte* ptr2 = (byte*)ptr + 28;
				*(RpcHeader*)ptr2 = RpcHeader.Create(NetworkBehaviourUtils.GetRpcStaticIndexOrThrow("System.Void TappableManager::RPC_SendOnRelease(Fusion.NetworkRunner,System.Int32,Fusion.RpcInfo)"));
				int num2 = 8;
				*(int*)(ptr2 + num2) = key;
				num2 += 4;
				ptr->Offset = num2 * 8;
				ptr->SetStatic();
				runner.SendRpc(ptr);
			}
			info = RpcInfo.FromLocal(runner, RpcChannel.Reliable, RpcHostMode.SourceIsServer);
		}
		gManager.SendOnReleaseShared(key, new PhotonMessageInfoWrapped(info));
	}

	public void SendOnReleaseShared(int key, PhotonMessageInfoWrapped info)
	{
		MonkeAgent.IncrementRPCCall(info, "SendOnReleaseShared");
		if (key == 0)
		{
			return;
		}
		for (int i = 0; i < tappables.Count; i++)
		{
			Tappable tappable = tappables[i];
			if (tappable.tappableId == key)
			{
				tappable.OnReleaseLocal(Time.time, info);
			}
		}
	}

	[NetworkRpcStaticWeavedInvoker("System.Void TappableManager::RPC_SendOnTap(Fusion.NetworkRunner,System.Int32,System.Single,Fusion.RpcInfo)")]
	[Preserve]
	[WeaverGenerated]
	protected unsafe static void RPC_SendOnTap_0040Invoker(NetworkRunner runner, SimulationMessage* message)
	{
		byte* ptr = (byte*)message + 28;
		int num = 8;
		int num2 = *(int*)(ptr + num);
		num += 4;
		int key = num2;
		float num3 = *(float*)(ptr + num);
		num += 4;
		float tapStrength = num3;
		RpcInfo info = RpcInfo.FromMessage(runner, message, RpcHostMode.SourceIsServer);
		NetworkBehaviourUtils.InvokeRpc = true;
		RPC_SendOnTap(runner, key, tapStrength, info);
	}

	[NetworkRpcStaticWeavedInvoker("System.Void TappableManager::RPC_SendOnGrab(Fusion.NetworkRunner,System.Int32,Fusion.RpcInfo)")]
	[Preserve]
	[WeaverGenerated]
	protected unsafe static void RPC_SendOnGrab_0040Invoker(NetworkRunner runner, SimulationMessage* message)
	{
		byte* ptr = (byte*)message + 28;
		int num = 8;
		int num2 = *(int*)(ptr + num);
		num += 4;
		int key = num2;
		RpcInfo info = RpcInfo.FromMessage(runner, message, RpcHostMode.SourceIsServer);
		NetworkBehaviourUtils.InvokeRpc = true;
		RPC_SendOnGrab(runner, key, info);
	}

	[NetworkRpcStaticWeavedInvoker("System.Void TappableManager::RPC_SendOnRelease(Fusion.NetworkRunner,System.Int32,Fusion.RpcInfo)")]
	[Preserve]
	[WeaverGenerated]
	protected unsafe static void RPC_SendOnRelease_0040Invoker(NetworkRunner runner, SimulationMessage* message)
	{
		byte* ptr = (byte*)message + 28;
		int num = 8;
		int num2 = *(int*)(ptr + num);
		num += 4;
		int key = num2;
		RpcInfo info = RpcInfo.FromMessage(runner, message, RpcHostMode.SourceIsServer);
		NetworkBehaviourUtils.InvokeRpc = true;
		RPC_SendOnRelease(runner, key, info);
	}
}

using System.Runtime.InteropServices;
using Fusion;
using Fusion.CodeGen;
using UnityEngine;
using UnityEngine.Scripting;

[NetworkBehaviourWeaved(0)]
public class FusionPlayerProperties : NetworkBehaviour
{
	[StructLayout(LayoutKind.Explicit, Size = 960)]
	[NetworkStructWeaved(240)]
	public struct PlayerInfo : INetworkStruct
	{
		[FieldOffset(0)]
		[FixedBufferProperty(typeof(NetworkString<_32>), typeof(UnityValueSurrogate_0040ReaderWriter_0040Fusion_NetworkString_00601_003CFusion__32_003E), 0, order = -2147483647)]
		[WeaverGenerated]
		[SerializeField]
		private FixedStorage_004033 _NickName;

		[FieldOffset(132)]
		[FixedBufferProperty(typeof(NetworkDictionary<NetworkString<_32>, NetworkString<_32>>), typeof(UnityDictionarySurrogate_0040ReaderWriter_0040Fusion_NetworkString_00601_003CFusion__32_003E_0040ReaderWriter_0040Fusion_NetworkString_00601_003CFusion__32_003E), 3, order = -2147483647)]
		[WeaverGenerated]
		[SerializeField]
		private FixedStorage_0040207 _properties;

		[Networked]
		[NetworkedWeaved(0, 33)]
		public unsafe NetworkString<_32> NickName
		{
			readonly get
			{
				return *(NetworkString<_32>*)Native.ReferenceToPointer(ref _NickName);
			}
			set
			{
				*(NetworkString<_32>*)Native.ReferenceToPointer(ref _NickName) = value;
			}
		}

		[Networked]
		[NetworkedWeavedDictionary(3, 33, 33, typeof(ReaderWriter_0040Fusion_NetworkString_00601_003CFusion__32_003E), typeof(ReaderWriter_0040Fusion_NetworkString_00601_003CFusion__32_003E))]
		[NetworkedWeaved(33, 207)]
		public unsafe NetworkDictionary<NetworkString<_32>, NetworkString<_32>> properties => new NetworkDictionary<NetworkString<_32>, NetworkString<_32>>((int*)Native.ReferenceToPointer(ref _properties), 3, ReaderWriter_0040Fusion_NetworkString_00601_003CFusion__32_003E.GetInstance(), ReaderWriter_0040Fusion_NetworkString_00601_003CFusion__32_003E.GetInstance());
	}

	public delegate void PlayerAttributeOnChanged();

	public PlayerAttributeOnChanged playerAttributeOnChanged;

	[Capacity(20)]
	private NetworkDictionary<PlayerRef, PlayerInfo> netPlayerAttributes => default(NetworkDictionary<PlayerRef, PlayerInfo>);

	public PlayerInfo PlayerProperties => netPlayerAttributes[base.Runner.LocalPlayer];

	private void OnAttributesChanged()
	{
		playerAttributeOnChanged?.Invoke();
	}

	[Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true, TickAligned = true)]
	public unsafe void RPC_UpdatePlayerAttributes(PlayerInfo newInfo, RpcInfo info = default(RpcInfo))
	{
		if (base.InvokeRpc)
		{
			base.InvokeRpc = false;
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
				NetworkBehaviourUtils.NotifyLocalSimulationNotAllowedToSendRpc("System.Void FusionPlayerProperties::RPC_UpdatePlayerAttributes(FusionPlayerProperties/PlayerInfo,Fusion.RpcInfo)", base.Object, 7);
				return;
			}
			int num = 8;
			num += 960;
			if (!SimulationMessage.CanAllocateUserPayload(num))
			{
				NetworkBehaviourUtils.NotifyRpcPayloadSizeExceeded("System.Void FusionPlayerProperties::RPC_UpdatePlayerAttributes(FusionPlayerProperties/PlayerInfo,Fusion.RpcInfo)", num);
				return;
			}
			if (base.Runner.HasAnyActiveConnections())
			{
				SimulationMessage* ptr = SimulationMessage.Allocate(base.Runner.Simulation, num);
				byte* ptr2 = (byte*)ptr + 28;
				*(RpcHeader*)ptr2 = RpcHeader.Create(base.Object.Id, base.ObjectIndex, 1);
				int num2 = 8;
				*(PlayerInfo*)(ptr2 + num2) = newInfo;
				num2 += 960;
				ptr->Offset = num2 * 8;
				base.Runner.SendRpc(ptr);
			}
			if ((localAuthorityMask & 7) == 0)
			{
				return;
			}
			info = RpcInfo.FromLocal(base.Runner, RpcChannel.Reliable, RpcHostMode.SourceIsServer);
		}
		Debug.Log("Update Player attributes triggered");
		PlayerRef source = info.Source;
		if (netPlayerAttributes.ContainsKey(source))
		{
			Debug.Log("Current nickname is " + netPlayerAttributes[source].NickName.ToString());
			Debug.Log("Sent nickname is " + newInfo.NickName.ToString());
			if (netPlayerAttributes[source].Equals(newInfo))
			{
				Debug.Log("Info is already correct for this user. Shouldnt have received an RPC in this case.");
				return;
			}
		}
		netPlayerAttributes.Set(source, newInfo);
	}

	public override void Spawned()
	{
		Debug.Log("Player props SPAWNED!");
		if (base.Runner.Mode == SimulationModes.Client)
		{
			Debug.Log("SET Player Properties manager!");
		}
	}

	public string GetDisplayName(PlayerRef player)
	{
		return netPlayerAttributes[player].NickName.Value;
	}

	public string GetLocalDisplayName()
	{
		return netPlayerAttributes[base.Runner.LocalPlayer].NickName.Value;
	}

	public bool GetProperty(PlayerRef player, string propertyName, out string propertyValue)
	{
		if (netPlayerAttributes[player].properties.TryGet(propertyName, out var value))
		{
			propertyValue = value.Value;
			return true;
		}
		propertyValue = null;
		return false;
	}

	public bool PlayerHasEntry(PlayerRef player)
	{
		return netPlayerAttributes.ContainsKey(player);
	}

	public void RemovePlayerEntry(PlayerRef player)
	{
		if (base.Object.HasStateAuthority)
		{
			string value = netPlayerAttributes[player].NickName.Value;
			netPlayerAttributes.Remove(player);
			Debug.Log("Removed " + value + "player properties as they just left.");
		}
	}

	[WeaverGenerated]
	public override void CopyBackingFieldsToState(bool P_0)
	{
	}

	[WeaverGenerated]
	public override void CopyStateToBackingFields()
	{
	}

	[NetworkRpcWeavedInvoker(1, 7, 7)]
	[Preserve]
	[WeaverGenerated]
	protected unsafe static void RPC_UpdatePlayerAttributes_0040Invoker(NetworkBehaviour behaviour, SimulationMessage* message)
	{
		byte* ptr = (byte*)message + 28;
		int num = 8;
		PlayerInfo playerInfo = *(PlayerInfo*)(ptr + num);
		num += 960;
		PlayerInfo newInfo = playerInfo;
		RpcInfo info = RpcInfo.FromMessage(behaviour.Runner, message, RpcHostMode.SourceIsServer);
		behaviour.InvokeRpc = true;
		((FusionPlayerProperties)behaviour).RPC_UpdatePlayerAttributes(newInfo, info);
	}
}

using System;
using System.Collections.Generic;
using Fusion;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Scripting;

namespace GorillaTag.Cosmetics;

public class EnvironmentProximityReactorManager : NetworkSceneObject
{
	private struct PendingProximityEvent
	{
		public int reactorId;

		public int blockIndex;

		public bool isBelow;

		public PhotonMessageInfoWrapped info;

		public float receivedTime;
	}

	private static EnvironmentProximityReactorManager instance;

	[SerializeField]
	private List<EnvironmentProximityReactor> reactors = new List<EnvironmentProximityReactor>();

	private readonly HashSet<int> idSet = new HashSet<int>();

	private readonly Dictionary<int, List<PendingProximityEvent>> pendingEvents = new Dictionary<int, List<PendingProximityEvent>>();

	private float distanceBuffer = 3f;

	private const float cosmeticSyncTimeout = 10f;

	private static readonly HashSet<EnvironmentProximityReactor> registry = new HashSet<EnvironmentProximityReactor>();

	public static EnvironmentProximityReactorManager Instance => instance;

	private void Awake()
	{
		if (instance != null && instance != this)
		{
			GTDev.LogWarning("[EnvironmentProximityReactorManager] Duplicate instance of the Environment Reactor Manager, destroying.");
			UnityEngine.Object.Destroy(this);
			return;
		}
		instance = this;
		foreach (EnvironmentProximityReactor item in registry)
		{
			if (item != null)
			{
				RegisterInstance(item);
			}
		}
		registry.Clear();
		RoomSystem.PlayerJoinedEvent += new Action<NetPlayer>(OnPlayerJoined);
		RoomSystem.PlayerLeftEvent += new Action<NetPlayer>(OnPlayerLeft);
		RoomSystem.LeftRoomEvent += new Action(OnLeftRoom);
		CosmeticsProximityReactorManager.OnCosmeticRegistered += OnCosmeticRegistered;
	}

	private void OnDestroy()
	{
		NetworkBehaviourUtils.InternalOnDestroy(this);
		if (instance == this)
		{
			instance = null;
		}
		if (NetworkSystem.Instance != null)
		{
			RoomSystem.PlayerJoinedEvent -= new Action<NetPlayer>(OnPlayerJoined);
			RoomSystem.PlayerLeftEvent -= new Action<NetPlayer>(OnPlayerLeft);
			RoomSystem.LeftRoomEvent -= new Action(OnLeftRoom);
		}
		CosmeticsProximityReactorManager.OnCosmeticRegistered -= OnCosmeticRegistered;
	}

	private void OnPlayerLeft(NetPlayer player)
	{
		pendingEvents.Remove(player.ActorNumber);
	}

	private void OnLeftRoom()
	{
		pendingEvents.Clear();
	}

	private void OnPlayerJoined(NetPlayer newPlayer)
	{
		if (newPlayer.IsLocal || !NetworkSystem.Instance.InRoom)
		{
			return;
		}
		for (int i = 0; i < reactors.Count; i++)
		{
			if (reactors[i] != null)
			{
				reactors[i].SyncStateTo(newPlayer, this);
			}
		}
	}

	public void BroadcastProximityStateTo(NetPlayer target, int reactorId, int blockIndex, bool isBelow)
	{
		if (NetworkSystem.Instance.InRoom)
		{
			photonView.RPC("ProximityStateRPC", ((PunNetPlayer)target).PlayerRef, reactorId, blockIndex, isBelow);
		}
	}

	private bool CheckPlayerRateLimit(NetPlayer sender)
	{
		if (!VRRigCache.Instance.TryGetVrrig(sender, out var playerRig))
		{
			return false;
		}
		return playerRig.Rig.fxSettings.callSettings[24].CallLimitSettings.CheckCallTime(Time.unscaledTime);
	}

	private bool SenderHasValidCosmetic(int reactorId, int blockIndex, PhotonMessageInfoWrapped info)
	{
		if (CosmeticsProximityReactorManager.Instance == null)
		{
			return false;
		}
		EnvironmentProximityReactor environmentProximityReactor = null;
		for (int i = 0; i < reactors.Count; i++)
		{
			if (reactors[i] != null && reactors[i].reactorId == reactorId)
			{
				environmentProximityReactor = reactors[i];
				break;
			}
		}
		if (environmentProximityReactor == null || blockIndex >= environmentProximityReactor.blocks.Count)
		{
			return false;
		}
		EnvironmentProximityReactor.InteractionBlock interactionBlock = environmentProximityReactor.blocks[blockIndex];
		IReadOnlyList<CosmeticsProximityReactor> cosmetics = CosmeticsProximityReactorManager.Instance.Cosmetics;
		for (int j = 0; j < cosmetics.Count; j++)
		{
			CosmeticsProximityReactor cosmeticsProximityReactor = cosmetics[j];
			if (!(cosmeticsProximityReactor == null))
			{
				VRRig ownerRig = cosmeticsProximityReactor.GetOwnerRig();
				if (!(ownerRig == null) && ownerRig.Creator == info.Sender && interactionBlock.CanTriggerFrom(cosmeticsProximityReactor))
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool SenderIsInRange(int reactorId, int blockIndex, PhotonMessageInfoWrapped info)
	{
		if (!VRRigCache.Instance.TryGetVrrig(info.Sender, out var playerRig))
		{
			return false;
		}
		EnvironmentProximityReactor environmentProximityReactor = null;
		for (int i = 0; i < reactors.Count; i++)
		{
			if (reactors[i] != null && reactors[i].reactorId == reactorId)
			{
				environmentProximityReactor = reactors[i];
				break;
			}
		}
		if (environmentProximityReactor == null || blockIndex >= environmentProximityReactor.blocks.Count)
		{
			return false;
		}
		float range = environmentProximityReactor.blocks[blockIndex].proximityThreshold + distanceBuffer;
		return playerRig.Rig.IsPositionInRange(environmentProximityReactor.transform.position, range);
	}

	private void OnCosmeticRegistered(CosmeticsProximityReactor cosmetic)
	{
		VRRig ownerRig = cosmetic.GetOwnerRig();
		if (ownerRig == null || ownerRig.Creator == null)
		{
			return;
		}
		int actorNumber = ownerRig.Creator.ActorNumber;
		if (!pendingEvents.TryGetValue(actorNumber, out var value))
		{
			return;
		}
		float unscaledTime = Time.unscaledTime;
		for (int num = value.Count - 1; num >= 0; num--)
		{
			PendingProximityEvent pendingProximityEvent = value[num];
			if (unscaledTime - pendingProximityEvent.receivedTime > 10f)
			{
				value.RemoveAt(num);
			}
			else if (SenderHasValidCosmetic(pendingProximityEvent.reactorId, pendingProximityEvent.blockIndex, pendingProximityEvent.info))
			{
				if (!SenderIsInRange(pendingProximityEvent.reactorId, pendingProximityEvent.blockIndex, pendingProximityEvent.info))
				{
					value.RemoveAt(num);
				}
				else
				{
					ApplyProximityEventToReactor(pendingProximityEvent.reactorId, pendingProximityEvent.blockIndex, pendingProximityEvent.isBelow);
					value.RemoveAt(num);
				}
			}
		}
		if (value.Count == 0)
		{
			pendingEvents.Remove(actorNumber);
		}
	}

	private void Update()
	{
		if (pendingEvents.Count == 0)
		{
			return;
		}
		float unscaledTime = Time.unscaledTime;
		foreach (KeyValuePair<int, List<PendingProximityEvent>> pendingEvent in pendingEvents)
		{
			List<PendingProximityEvent> value = pendingEvent.Value;
			for (int num = value.Count - 1; num >= 0; num--)
			{
				if (unscaledTime - value[num].receivedTime > 10f)
				{
					value.RemoveAt(num);
				}
			}
		}
		foreach (KeyValuePair<int, List<PendingProximityEvent>> pendingEvent2 in pendingEvents)
		{
			if (pendingEvent2.Value.Count == 0)
			{
				pendingEvents.Remove(pendingEvent2.Key);
				break;
			}
		}
	}

	private void TryCacheProximityEvent(int reactorId, int blockIndex, bool isBelow, PhotonMessageInfoWrapped info)
	{
		int actorNumber = info.Sender.ActorNumber;
		if (!pendingEvents.TryGetValue(actorNumber, out var value))
		{
			value = new List<PendingProximityEvent>();
			pendingEvents[actorNumber] = value;
		}
		else if (value.Exists((PendingProximityEvent e) => e.reactorId == reactorId))
		{
			return;
		}
		value.Add(new PendingProximityEvent
		{
			reactorId = reactorId,
			blockIndex = blockIndex,
			isBelow = isBelow,
			info = info,
			receivedTime = Time.unscaledTime
		});
	}

	private void ApplyProximityEventToReactor(int reactorId, int blockIndex, bool isBelow)
	{
		for (int i = 0; i < reactors.Count; i++)
		{
			EnvironmentProximityReactor environmentProximityReactor = reactors[i];
			if (!(environmentProximityReactor == null) && environmentProximityReactor.reactorId == reactorId)
			{
				environmentProximityReactor.ApplySharedProximity(blockIndex, isBelow);
				break;
			}
		}
	}

	private void RegisterInstance(EnvironmentProximityReactor reactor)
	{
		if (!(reactor == null) && idSet.Add(reactor.reactorId))
		{
			reactors.Add(reactor);
		}
	}

	private void UnregisterInstance(EnvironmentProximityReactor reactor)
	{
		if (!(reactor == null) && idSet.Remove(reactor.reactorId))
		{
			reactors.Remove(reactor);
		}
	}

	public static void Register(EnvironmentProximityReactor reactor)
	{
		if (instance != null)
		{
			instance.RegisterInstance(reactor);
		}
		else
		{
			registry.Add(reactor);
		}
	}

	public static void Unregister(EnvironmentProximityReactor reactor)
	{
		if (instance != null)
		{
			instance.UnregisterInstance(reactor);
		}
		else
		{
			registry.Remove(reactor);
		}
	}

	public void BroadcastProximityState(int reactorId, int blockIndex, bool isBelow)
	{
		if (NetworkSystem.Instance.InRoom)
		{
			photonView.RPC("ProximityStateRPC", RpcTarget.Others, reactorId, blockIndex, isBelow);
		}
	}

	[PunRPC]
	public void ProximityStateRPC(int reactorId, int blockIndex, bool isBelow, PhotonMessageInfo info)
	{
		ApplyProximityStateShared(reactorId, blockIndex, isBelow, new PhotonMessageInfoWrapped(info));
	}

	[Rpc]
	public unsafe static void RPC_ProximityState(NetworkRunner runner, int reactorId, int blockIndex, bool isBelow, RpcInfo info = default(RpcInfo))
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
			num += 4;
			if (!SimulationMessage.CanAllocateUserPayload(num))
			{
				NetworkBehaviourUtils.NotifyRpcPayloadSizeExceeded("System.Void GorillaTag.Cosmetics.EnvironmentProximityReactorManager::RPC_ProximityState(Fusion.NetworkRunner,System.Int32,System.Int32,System.Boolean,Fusion.RpcInfo)", num);
				return;
			}
			if (runner.HasAnyActiveConnections())
			{
				SimulationMessage* ptr = SimulationMessage.Allocate(runner.Simulation, num);
				byte* ptr2 = (byte*)ptr + 28;
				*(RpcHeader*)ptr2 = RpcHeader.Create(NetworkBehaviourUtils.GetRpcStaticIndexOrThrow("System.Void GorillaTag.Cosmetics.EnvironmentProximityReactorManager::RPC_ProximityState(Fusion.NetworkRunner,System.Int32,System.Int32,System.Boolean,Fusion.RpcInfo)"));
				int num2 = 8;
				*(int*)(ptr2 + num2) = reactorId;
				num2 += 4;
				*(int*)(ptr2 + num2) = blockIndex;
				num2 += 4;
				ReadWriteUtilsForWeaver.WriteBoolean((int*)(ptr2 + num2), isBelow);
				num2 += 4;
				ptr->Offset = num2 * 8;
				ptr->SetStatic();
				runner.SendRpc(ptr);
			}
			info = RpcInfo.FromLocal(runner, RpcChannel.Reliable, RpcHostMode.SourceIsServer);
		}
		if (!(instance == null))
		{
			instance.ApplyProximityStateShared(reactorId, blockIndex, isBelow, new PhotonMessageInfoWrapped(info));
		}
	}

	private void ApplyProximityStateShared(int reactorId, int blockIndex, bool isBelow, PhotonMessageInfoWrapped info)
	{
		MonkeAgent.IncrementRPCCall(info, "ApplyProximityStateShared");
		if (blockIndex < 0)
		{
			return;
		}
		if (!idSet.Contains(reactorId))
		{
			MonkeAgent.instance.SendReport("Sent invalid reactorId in ProximityStateRPC", info.Sender.UserId, info.Sender.NickName);
		}
		else if (CheckPlayerRateLimit(info.Sender))
		{
			if (!SenderHasValidCosmetic(reactorId, blockIndex, info))
			{
				TryCacheProximityEvent(reactorId, blockIndex, isBelow, info);
			}
			else if (!SenderIsInRange(reactorId, blockIndex, info))
			{
				MonkeAgent.instance.SendReport("Sent ProximityStateRPC from out of range", info.Sender.UserId, info.Sender.NickName);
			}
			else
			{
				ApplyProximityEventToReactor(reactorId, blockIndex, isBelow);
			}
		}
	}

	[NetworkRpcStaticWeavedInvoker("System.Void GorillaTag.Cosmetics.EnvironmentProximityReactorManager::RPC_ProximityState(Fusion.NetworkRunner,System.Int32,System.Int32,System.Boolean,Fusion.RpcInfo)")]
	[Preserve]
	[WeaverGenerated]
	protected unsafe static void RPC_ProximityState_0040Invoker(NetworkRunner runner, SimulationMessage* message)
	{
		byte* ptr = (byte*)message + 28;
		int num = 8;
		int num2 = *(int*)(ptr + num);
		num += 4;
		int reactorId = num2;
		int num3 = *(int*)(ptr + num);
		num += 4;
		int blockIndex = num3;
		bool num4 = ReadWriteUtilsForWeaver.ReadBoolean((int*)(ptr + num));
		num += 4;
		bool isBelow = num4;
		RpcInfo info = RpcInfo.FromMessage(runner, message, RpcHostMode.SourceIsServer);
		NetworkBehaviourUtils.InvokeRpc = true;
		RPC_ProximityState(runner, reactorId, blockIndex, isBelow, info);
	}
}

using System;
using System.Collections.Generic;
using Fusion;
using GorillaExtensions;
using K4os.Compression.LZ4;
using Photon.Pun;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

namespace Voxels;

[NetworkBehaviourWeaved(0)]
public class VoxelManager : NetworkComponent
{
	public class StateInitQueue
	{
		public NetPlayer player;

		public List<ChunkInitState> chunks = new List<ChunkInitState>();

		public List<VoxelOperationResult> operations = new List<VoxelOperationResult>();

		public ChunkInitState currentChunk;

		public bool IsEmpty
		{
			get
			{
				if (currentChunk == null && chunks.Count == 0)
				{
					return operations.Count == 0;
				}
				return false;
			}
		}

		public StateInitQueue()
		{
		}

		public StateInitQueue(NetPlayer player)
		{
			this.player = player;
		}

		public int GetChunkIndex(int hash)
		{
			for (int i = 0; i < chunks.Count; i++)
			{
				if (chunks[i].hash == hash)
				{
					return i;
				}
			}
			return -1;
		}

		public ChunkInitState GetChunkState(int hash)
		{
			foreach (ChunkInitState chunk in chunks)
			{
				if (chunk.hash == hash)
				{
					return chunk;
				}
			}
			return null;
		}
	}

	public struct VoxelOperationResult
	{
		public int worldId;

		public UnityEngine.BoundsInt bounds;

		public byte[] data;
	}

	public class ChunkInitState
	{
		public int worldId;

		public int3 chunkId;

		public int hash;

		public byte[] serializedChunkState;

		public int numSerializedBytes;

		public int totalSerializedBytes;
	}

	public enum RPC
	{
		WorldRequest,
		OperationRequest,
		MineRequest,
		StartChunk,
		StartEmptyChunk,
		ContinueChunk,
		SetDensity,
		MineCommand,
		Count
	}

	public const int OP_SCALE = 256;

	private const int MAX_DATA_SIZE = 1000;

	private static VoxelManager _instance;

	private static Dictionary<int, VoxelWorld> _worlds = new Dictionary<int, VoxelWorld>();

	private static StaticArrayBag<byte> _byteArrayBag = new StaticArrayBag<byte>();

	private static StaticArrayBag<int> _intArrayBag = new StaticArrayBag<int>();

	private NetPlayer _owner;

	private static Dictionary<int, StateInitQueue> _initQueues = new Dictionary<int, StateInitQueue>();

	private static StateInitQueue _localInitQueue = new StateInitQueue();

	private static byte[] _packetData = new byte[1000];

	private static bool _shouldProcessQueues;

	private static Queue<(float time, int bytes)> _sendHistory = new Queue<(float, int)>();

	private static int _sendRate;

	private const int MAX_DATA_RATE = 10000;

	private static Dictionary<int, CallLimiter[]> _spamChecks = new Dictionary<int, CallLimiter[]>();

	public static bool HasAuthority
	{
		get
		{
			if (InRoom)
			{
				return _instance.IsLocallyOwned;
			}
			return true;
		}
	}

	public static bool InRoom => NetworkSystem.Instance.InRoom;

	protected override void Start()
	{
		base.Start();
		_instance = this;
	}

	internal override void OnEnable()
	{
		NetworkBehaviourUtils.InternalOnEnable(this);
		base.OnEnable();
		RoomSystem.JoinedRoomEvent += new Action(OnNetworkJoinedRoom);
		RoomSystem.LeftRoomEvent += new Action(OnNetworkLeftRoom);
		NetworkSystem.Instance.OnPlayerLeft += new Action<NetPlayer>(OnPlayerLeft);
		Application.lowMemory += OnLowMemory;
	}

	internal override void OnDisable()
	{
		NetworkBehaviourUtils.InternalOnDisable(this);
		base.OnDisable();
		RoomSystem.JoinedRoomEvent -= new Action(OnNetworkJoinedRoom);
		RoomSystem.LeftRoomEvent -= new Action(OnNetworkLeftRoom);
		NetworkSystem.Instance.OnPlayerLeft -= new Action<NetPlayer>(OnPlayerLeft);
		Application.lowMemory -= OnLowMemory;
	}

	private void Update()
	{
		if (!_shouldProcessQueues)
		{
			return;
		}
		_shouldProcessQueues = false;
		if (!PhotonNetwork.IsMasterClient)
		{
			_initQueues.Clear();
		}
		else
		{
			if (_worlds.Count == 0)
			{
				return;
			}
			UpdateTransferLog();
			foreach (NetPlayer item in RoomSystem.PlayersInRoom)
			{
				if (!item.IsLocal && _initQueues.TryGetValue(item.ActorNumber, out var value) && !value.IsEmpty)
				{
					if (_sendRate < 10000)
					{
						SendNextChunk(value);
					}
					if (!value.IsEmpty)
					{
						_shouldProcessQueues = true;
					}
				}
			}
		}
	}

	private void OnLowMemory()
	{
		long num = 1048576L;
		long num2 = Profiler.GetMonoUsedSizeLong() / num;
		GC.Collect();
		GC.WaitForPendingFinalizers();
		long num3 = Profiler.GetMonoUsedSizeLong() / num;
		PersistLog.Log($"Performing Memory Cleanup.  Used Memory: {num2}M->{num3}M ({num3 - num2}M)");
	}

	private static void UpdateTransferLog()
	{
		float num = Time.realtimeSinceStartup - 1f;
		while (_sendHistory.Count > 0 && _sendHistory.Peek().time <= num)
		{
			int item = _sendHistory.Dequeue().bytes;
			_sendRate -= item;
		}
	}

	private static void EnqueueTransferLog(int bytes)
	{
		_sendHistory.Enqueue((Time.realtimeSinceStartup, bytes));
		_sendRate += bytes;
	}

	public static int[] GetIntArray(int length)
	{
		int[] staticArray = _intArrayBag.GetStaticArray(length);
		for (int i = 0; i < length; i++)
		{
			staticArray[i] = 0;
		}
		return staticArray;
	}

	public static void Register(VoxelWorld world)
	{
		_worlds[world.Id] = world;
		Debug.Log($"Registered voxel world {world.name}[{world.Id}]", world);
		if (!HasAuthority)
		{
			SendWorldStateRequest(world.Id);
		}
	}

	public static void Unregister(VoxelWorld world)
	{
		_worlds.Remove(world.Id);
		Debug.Log($"Unregistered voxel world {world.name}[{world.Id}]", world);
	}

	public static void ReplicateState(VoxelWorld world)
	{
		if (!InRoom || !HasAuthority)
		{
			return;
		}
		foreach (NetPlayer item in RoomSystem.PlayersInRoom)
		{
			if (!item.IsLocal)
			{
				SendWorldStateToPlayer(world, item);
			}
		}
	}

	private void OnNetworkJoinedRoom()
	{
		if (!HasAuthority)
		{
			RequestVoxelWorldStates();
		}
	}

	private void OnNetworkLeftRoom()
	{
	}

	private void OnPlayerLeft(NetPlayer player)
	{
		if (HasAuthority && InRoom)
		{
			ClearQueueForPlayer(player);
		}
	}

	protected override void OnOwnerSwitched(NetPlayer newOwningPlayer)
	{
		_owner = newOwningPlayer;
		if (!HasAuthority)
		{
			RequestVoxelWorldStates();
		}
	}

	public override void WriteDataFusion()
	{
	}

	public override void ReadDataFusion()
	{
	}

	protected override void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
	}

	protected override void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
	}

	private static void RequestVoxelWorldStates()
	{
		foreach (int key in _worlds.Keys)
		{
			SendWorldStateRequest(key);
		}
	}

	public static void RequestWorldState(VoxelWorld world)
	{
		SendWorldStateRequest(world.Id);
	}

	private static void OnWorldStateRequestReceived(int worldId, PhotonMessageInfoWrapped info)
	{
		if (_worlds.TryGetValue(worldId, out var value))
		{
			SendWorldStateToPlayer(value, info.Sender);
		}
	}

	private static void SendWorldStateToPlayer(VoxelWorld world, NetPlayer player)
	{
		if (_initQueues.TryGetValue(player.ActorNumber, out var value))
		{
			for (int i = 0; i < value.chunks.Count; i++)
			{
				if (value.chunks[i].worldId == world.Id)
				{
					value.chunks.RemoveAt(i--);
				}
			}
		}
		foreach (Chunk chunk in world.Chunks)
		{
			QueueChunkForPlayer(chunk, player);
		}
	}

	private static bool WorldIsQueuedForPlayer(VoxelWorld world, NetPlayer player)
	{
		if (!_initQueues.TryGetValue(player.ActorNumber, out var value))
		{
			return false;
		}
		foreach (ChunkInitState chunk in value.chunks)
		{
			if (chunk.worldId == world.Id)
			{
				return true;
			}
		}
		return false;
	}

	private static void ClearQueueForPlayer(NetPlayer player)
	{
		if (_initQueues.TryGetValue(player.ActorNumber, out var value))
		{
			value.chunks.Clear();
			value.operations.Clear();
		}
	}

	private static StateInitQueue GetOrCreateQueueForPlayer(NetPlayer player)
	{
		if (!_initQueues.TryGetValue(player.ActorNumber, out var value))
		{
			value = new StateInitQueue(player);
			_initQueues[player.ActorNumber] = value;
		}
		return value;
	}

	private static void QueueChunkForPlayer(Chunk chunk, NetPlayer player)
	{
		StateInitQueue orCreateQueueForPlayer = GetOrCreateQueueForPlayer(player);
		byte[] array = null;
		int totalSerializedBytes = 0;
		if (chunk.IsDataChanged)
		{
			array = ChunkIO.SerializeChunk(new ChunkDTO(chunk));
			totalSerializedBytes = array.Length;
		}
		ChunkInitState item = new ChunkInitState
		{
			numSerializedBytes = 0,
			totalSerializedBytes = totalSerializedBytes,
			worldId = chunk.World.Id,
			chunkId = chunk.Id,
			hash = (chunk.World.Id ^ chunk.Id.GetHashCode()),
			serializedChunkState = array
		};
		orCreateQueueForPlayer.chunks.Add(item);
		_shouldProcessQueues = true;
	}

	private static void QueueOperationForPlayer(VoxelWorld world, NetPlayer player, UnityEngine.BoundsInt bounds, byte[] data)
	{
		if (!_initQueues.TryGetValue(player.ActorNumber, out var value))
		{
			value = new StateInitQueue();
			_initQueues[player.ActorNumber] = value;
		}
		VoxelOperationResult item = new VoxelOperationResult
		{
			worldId = world.Id,
			bounds = bounds,
			data = data
		};
		value.operations.Add(item);
		_shouldProcessQueues = true;
	}

	private static void SendNextChunk(StateInitQueue queue)
	{
		NetPlayer player = queue.player;
		if (queue.currentChunk != null)
		{
			ChunkInitState currentChunk = queue.currentChunk;
			SendNextPacketForChunk(currentChunk, player);
			if (currentChunk.numSerializedBytes == currentChunk.totalSerializedBytes)
			{
				queue.currentChunk = null;
			}
		}
		else if (queue.chunks.Count > 0)
		{
			ChunkInitState chunkInitState = queue.chunks[0];
			SendStartChunk(player, chunkInitState.worldId, chunkInitState.chunkId, chunkInitState.hash, chunkInitState.totalSerializedBytes);
			if (chunkInitState.totalSerializedBytes > 0)
			{
				queue.currentChunk = chunkInitState;
			}
			queue.chunks.RemoveAt(0);
		}
		else if (queue.operations.Count > 0)
		{
			VoxelOperationResult voxelOperationResult = queue.operations[0];
			SendSetDensity(player, voxelOperationResult.worldId, voxelOperationResult.bounds, voxelOperationResult.data);
			queue.operations.RemoveAt(0);
		}
	}

	private static void SendNextPacketForChunk(ChunkInitState chunkState, NetPlayer player)
	{
		int numSerializedBytes = chunkState.numSerializedBytes;
		int num = Mathf.Min(chunkState.totalSerializedBytes - numSerializedBytes, 1000);
		if (num > 0)
		{
			Array.Copy(chunkState.serializedChunkState, numSerializedBytes, _packetData, 0, num);
			chunkState.numSerializedBytes += num;
			SendContinueChunk(player, chunkState.hash, num, _packetData);
			EnqueueTransferLog(num);
			_ = chunkState.numSerializedBytes;
			_ = chunkState.totalSerializedBytes;
		}
	}

	private static void OnStartChunkReceived(int worldId, int3 chunkId, int hash, int size)
	{
		int chunkIndex = _localInitQueue.GetChunkIndex(hash);
		if (chunkIndex >= 0)
		{
			_localInitQueue.chunks.RemoveAt(chunkIndex);
		}
		Chunk chunk;
		if (!_worlds.TryGetValue(worldId, out var value))
		{
			Debug.LogError($"Failed to find world {worldId}");
		}
		else if (!value.TryGetChunk(chunkId, out chunk))
		{
			Debug.LogError($"Tried to receive a non-loaded chunk {chunkId}");
		}
		else if (size > 0)
		{
			ChunkInitState item = new ChunkInitState
			{
				worldId = worldId,
				hash = hash,
				serializedChunkState = new byte[size],
				numSerializedBytes = 0,
				totalSerializedBytes = size
			};
			_localInitQueue.chunks.Add(item);
		}
		else
		{
			value.ResetChunk(chunkId);
		}
	}

	private static void OnChunkPacketReceived(int hash, int size, byte[] data)
	{
		if (size > data.Length)
		{
			Debug.LogError("Size value is is larger than data");
			return;
		}
		int chunkIndex = _localInitQueue.GetChunkIndex(hash);
		if (chunkIndex < 0)
		{
			Debug.LogError($"Couldn't fetch chunk state with hash {hash}");
			return;
		}
		ChunkInitState chunkInitState = _localInitQueue.chunks[chunkIndex];
		if (chunkInitState.numSerializedBytes + size > chunkInitState.totalSerializedBytes)
		{
			Debug.LogError($"Received data larger than {chunkInitState.totalSerializedBytes} bytes for chunk {hash}");
			return;
		}
		Array.Copy(data, 0, chunkInitState.serializedChunkState, chunkInitState.numSerializedBytes, size);
		chunkInitState.numSerializedBytes += size;
		if (chunkInitState.numSerializedBytes != chunkInitState.totalSerializedBytes)
		{
			return;
		}
		if (ChunkIO.TryDeserializeChunk(in chunkInitState.serializedChunkState, out var dto))
		{
			if (_worlds.TryGetValue(dto.WorldId, out var value))
			{
				value.UpdateChunkFrom(dto);
			}
			else
			{
				Debug.LogError($"Deserialized chunk for nonexistent world {dto.WorldId}");
			}
		}
		else
		{
			Debug.LogError($"Unable to deserialize chunk with hash {chunkInitState.hash}");
		}
		_localInitQueue.chunks.RemoveAt(chunkIndex);
	}

	private static void SendDensity(VoxelWorld world, UnityEngine.BoundsInt bounds)
	{
		GetDensityForBounds(world, bounds, out var voxels);
		byte[] data = LZ4Pickler.Pickle(voxels);
		foreach (NetPlayer item in RoomSystem.PlayersInRoom)
		{
			if (!item.IsLocal)
			{
				if (WorldIsQueuedForPlayer(world, item))
				{
					QueueOperationForPlayer(world, item, bounds, data);
				}
				else
				{
					SendSetDensity(item, world.Id, bounds, data);
				}
			}
		}
	}

	public static void PerformOperation(VoxelWorld world, Vector3 position, VoxelAction action)
	{
		position = world.GetLocalPosition(position);
		action.radius /= world.Scale;
		if (InRoom)
		{
			if (HasAuthority)
			{
				OperateAuthority(world, position, action);
				return;
			}
			world.PerformLocalOperation(position, action);
			SendOperationRequest(world.Id, position, action);
		}
		else
		{
			world.PerformLocalOperation(position, action);
		}
	}

	private static void OperateAuthority(VoxelWorld world, Vector3 localPosition, VoxelAction action)
	{
		world.PerformLocalOperation(localPosition, action);
		UnityEngine.BoundsInt bounds = world.GetBounds(localPosition, action.radius);
		SendDensity(world, bounds);
	}

	private static void OnOperationRequestReceived(int worldId, Vector3 localPosition, VoxelAction action, PhotonMessageInfoWrapped info)
	{
		if (!_worlds.TryGetValue(worldId, out var value))
		{
			Debug.LogError($"Couldn't find voxel world {worldId}");
			return;
		}
		UnityEngine.BoundsInt bounds = value.GetBounds(localPosition, action.radius);
		if (bounds.GetVoxelCount() > 1000)
		{
			GTDev.LogError($"Received voxel operation request was too large [{bounds} = {bounds.GetVoxelCount()} voxels]");
		}
		else
		{
			OperateAuthority(value, localPosition, action);
		}
	}

	public static void Mine(VoxelWorld world, UnityEngine.BoundsInt bounds, Vector3 hitPoint, Vector3 hitNormal, Vector3 origin, VoxelAction action)
	{
		VoxelOperation op = new VoxelOperation(origin, action);
		if (InRoom)
		{
			if (HasAuthority)
			{
				MineAuthority(world, bounds, hitPoint, hitNormal, op);
				return;
			}
			world.PerformLocalMiningOperation(bounds, hitPoint, hitNormal, op);
			SendMineOperationRequest(world.Id, bounds, hitPoint, hitNormal, op);
		}
		else
		{
			world.PerformLocalMiningOperation(bounds, hitPoint, hitNormal, op);
		}
	}

	private static void MineAuthority(VoxelWorld world, UnityEngine.BoundsInt bounds, Vector3 hitPoint, Vector3 hitNormal, VoxelOperation op, NetPlayer sender = null)
	{
		if (sender == null)
		{
			sender = NetworkSystem.Instance.LocalPlayer;
		}
		world.PerformLocalMiningOperation(bounds, hitPoint, hitNormal, op);
		foreach (NetPlayer item in RoomSystem.PlayersInRoom)
		{
			if (!item.IsLocal && item != sender)
			{
				SendMineCommand(item, world.Id, bounds, hitPoint, hitNormal, op);
			}
		}
	}

	private static void OnMineRequestReceived(int worldId, UnityEngine.BoundsInt bounds, Vector3 hitPoint, Vector3 hitNormal, VoxelOperation op, PhotonMessageInfoWrapped info)
	{
		if (!_worlds.TryGetValue(worldId, out var value))
		{
			Debug.LogError($"Couldn't find voxel world {worldId}");
		}
		else
		{
			MineAuthority(value, bounds, hitPoint, hitNormal, op, info.Sender);
		}
	}

	private static void GetVoxelsForBounds(VoxelWorld world, UnityEngine.BoundsInt bounds, out Voxel[] voxels)
	{
		int voxelCount = bounds.GetVoxelCount();
		voxels = new Voxel[voxelCount];
		int num = 0;
		for (int i = bounds.min.x; i <= bounds.max.x; i++)
		{
			for (int j = bounds.min.y; j <= bounds.max.y; j++)
			{
				for (int k = bounds.min.z; k <= bounds.max.z; k++)
				{
					voxels[num++] = world.GetVoxelData(new int3(i, j, k));
				}
			}
		}
	}

	private static void GetDensityForBounds(VoxelWorld world, UnityEngine.BoundsInt bounds, out byte[] voxels)
	{
		int voxelCount = bounds.GetVoxelCount();
		voxels = _byteArrayBag.GetStaticArray(voxelCount);
		int num = 0;
		for (int i = bounds.min.x; i <= bounds.max.x; i++)
		{
			for (int j = bounds.min.y; j <= bounds.max.y; j++)
			{
				for (int k = bounds.min.z; k <= bounds.max.z; k++)
				{
					voxels[num++] = world.GetVoxelDensity(new int3(i, j, k));
				}
			}
		}
	}

	private static void OnSetDensityReceived(int worldId, UnityEngine.BoundsInt bounds, byte[] data)
	{
		if (!_worlds.TryGetValue(worldId, out var value))
		{
			throw new InvalidOperationException($"Couldn't find voxel world {worldId}");
		}
		byte[] array = LZ4Pickler.Unpickle(data);
		if (bounds.GetVoxelCount() != array.Length)
		{
			Debug.LogError($"Voxel count mismatch: {bounds.GetVoxelCount()} vs {array.Length}");
		}
		else
		{
			value.SetVoxelDensity(bounds, array, immediate: false);
		}
	}

	private static void OnMineCommandReceived(VoxelWorld world, UnityEngine.BoundsInt bounds, Vector3 hitPoint, Vector3 hitNormal, VoxelOperation op)
	{
		world.PerformLocalMiningOperation(bounds, hitPoint, hitNormal, op);
	}

	internal static bool IsValidAuthorityRPC(PhotonMessageInfoWrapped info, RPC eventType)
	{
		if ((HasAuthority && InRoom) || info.Sender.IsLocal)
		{
			return !IsSpamming(info, eventType);
		}
		return false;
	}

	internal static bool IsValidClientRPC(PhotonMessageInfoWrapped info, RPC eventType)
	{
		if (info.Sender.IsMasterClient || info.Sender.IsLocal)
		{
			return !IsSpamming(info, eventType);
		}
		return false;
	}

	internal static bool IsSpamming(PhotonMessageInfoWrapped info, RPC eventType)
	{
		return !GetSpamChecksForUser(info.senderID)[(int)eventType].CheckCallTime(Time.unscaledTime);
	}

	internal static CallLimiter[] GetSpamChecksForUser(int userID)
	{
		if (!_spamChecks.TryGetValue(userID, out var value))
		{
			value = new CallLimiter[8]
			{
				new CallLimiter(10, 30f),
				new CallLimiter(10, 1f),
				new CallLimiter(10, 1f),
				new CallLimiter(10, 1f),
				new CallLimiter(100, 1f),
				new CallLimiter(20, 1f),
				new CallLimiter(50, 1f),
				new CallLimiter(50, 1f)
			};
			_spamChecks[userID] = value;
		}
		return value;
	}

	internal static void RegisterNetEventCallbacks()
	{
		RoomSystem.netEventCallbacks[100] = DeserializeWorldStateRequest;
		RoomSystem.netEventCallbacks[101] = DeserializeOperationRequest;
		RoomSystem.netEventCallbacks[102] = DeserializeMineOperationRequest;
		RoomSystem.netEventCallbacks[103] = DeserializeStartChunk;
		RoomSystem.netEventCallbacks[104] = DeserializeContinueChunk;
		RoomSystem.netEventCallbacks[105] = DeserializeSetDensity;
		RoomSystem.netEventCallbacks[106] = DeserializeMineCommand;
	}

	private static void SendWorldStateRequest(int worldId)
	{
		RoomSystem.SendEvent(100, new object[1] { worldId }, in NetworkSystemRaiseEvent.neoMaster, reliable: true);
	}

	internal static void DeserializeWorldStateRequest(object[] eventData, PhotonMessageInfoWrapped info)
	{
		MonkeAgent.IncrementRPCCall(info, "DeserializeWorldStateRequest");
		if (IsValidAuthorityRPC(info, RPC.WorldRequest) && eventData.TryDeserializeTo<int>(out var v))
		{
			OnWorldStateRequestReceived(v, info);
		}
	}

	private static void SendOperationRequest(int worldId, Vector3 localPosition, VoxelAction action)
	{
		object[] evData = new object[3] { worldId, localPosition, action };
		RoomSystem.SendEvent(101, evData, in NetworkSystemRaiseEvent.neoMaster, reliable: true);
	}

	private static void DeserializeOperationRequest(object[] eventData, PhotonMessageInfoWrapped info)
	{
		MonkeAgent.IncrementRPCCall(info, "DeserializeOperationRequest");
		if (IsValidAuthorityRPC(info, RPC.OperationRequest) && eventData.TryDeserializeTo<int, Vector3, VoxelAction>(out var v, out var v2, out var v3) && v2.IsValid(10000f) && v3.IsValid() && !(v3.radius > 5f))
		{
			OnOperationRequestReceived(v, v2, v3, info);
		}
	}

	private static void SendMineOperationRequest(int worldId, UnityEngine.BoundsInt bounds, Vector3 hitPoint, Vector3 hitNormal, VoxelOperation op)
	{
		object[] evData = new object[5] { worldId, bounds, hitPoint, hitNormal, op };
		RoomSystem.SendEvent(102, evData, in NetworkSystemRaiseEvent.neoMaster, reliable: true);
	}

	private static void DeserializeMineOperationRequest(object[] eventData, PhotonMessageInfoWrapped info)
	{
		MonkeAgent.IncrementRPCCall(info, "DeserializeMineOperationRequest");
		if (IsValidAuthorityRPC(info, RPC.MineRequest) && eventData.TryDeserializeTo<int, UnityEngine.BoundsInt, Vector3, Vector3, VoxelOperation>(out var v, out var v2, out var v3, out var v4, out var v5) && v3.IsValid(10000f) && Mathf.Approximately(v4.sqrMagnitude, 1f) && v5.IsValid())
		{
			OnMineRequestReceived(v, v2, v3, v4, v5, info);
		}
	}

	private static void SendStartChunk(NetPlayer player, int worldId, int3 chunkId, int hash, int totalSerializedBytes)
	{
		object[] evData = new object[4] { worldId, chunkId, hash, totalSerializedBytes };
		RoomSystem.SendEvent(103, evData, in player, reliable: true);
	}

	private static void DeserializeStartChunk(object[] eventData, PhotonMessageInfoWrapped info)
	{
		if (eventData.TryDeserializeTo<int, int3, int, int>(out var v, out var v2, out var v3, out var v4))
		{
			if (v4 > 0)
			{
				MonkeAgent.IncrementRPCCall(info, "DeserializeStartChunk");
			}
			if (IsValidClientRPC(info, (v4 > 0) ? RPC.StartChunk : RPC.StartEmptyChunk))
			{
				OnStartChunkReceived(v, v2, v3, v4);
			}
		}
	}

	private static void SendContinueChunk(NetPlayer player, int hash, int size, byte[] data)
	{
		if (data.Length > 1000)
		{
			Debug.LogError($"Attempted to send ContinueChunk() with too many bytes ({data.Length})");
			return;
		}
		object[] evData = new object[3] { hash, size, data };
		RoomSystem.SendEvent(104, evData, in player, reliable: true);
	}

	private static void DeserializeContinueChunk(object[] eventData, PhotonMessageInfoWrapped info)
	{
		MonkeAgent.IncrementRPCCall(info, "DeserializeContinueChunk");
		if (IsValidClientRPC(info, RPC.ContinueChunk) && eventData.TryDeserializeTo<int, int, byte[]>(out var v, out var v2, out var v3) && v2 >= 1 && v2 <= 1000 && v2 <= v3.Length)
		{
			OnChunkPacketReceived(v, v2, v3);
		}
	}

	private static void SendSetDensity(NetPlayer player, int worldId, UnityEngine.BoundsInt bounds, byte[] data)
	{
		if (data.Length > 1000)
		{
			Debug.LogError($"Attempted to send SetDensity() with too many bytes ({data.Length})");
			return;
		}
		object[] evData = new object[3] { worldId, bounds, data };
		RoomSystem.SendEvent(105, evData, in player, reliable: true);
		EnqueueTransferLog(data.Length);
	}

	private static void DeserializeSetDensity(object[] eventData, PhotonMessageInfoWrapped info)
	{
		MonkeAgent.IncrementRPCCall(info, "DeserializeSetDensity");
		if (IsValidClientRPC(info, RPC.SetDensity) && eventData.TryDeserializeTo<int, UnityEngine.BoundsInt, byte[]>(out var v, out var v2, out var v3) && v3.Length <= 1000)
		{
			OnSetDensityReceived(v, v2, v3);
		}
	}

	private static void SendMineCommand(NetPlayer player, int worldId, UnityEngine.BoundsInt bounds, Vector3 hitPoint, Vector3 hitNormal, VoxelOperation op)
	{
		object[] evData = new object[5] { worldId, bounds, hitPoint, hitNormal, op };
		RoomSystem.SendEvent(106, evData, in player, reliable: true);
	}

	private static void DeserializeMineCommand(object[] eventData, PhotonMessageInfoWrapped info)
	{
		MonkeAgent.IncrementRPCCall(info, "DeserializeMineCommand");
		if (IsValidClientRPC(info, RPC.MineCommand) && eventData.TryDeserializeTo<int, UnityEngine.BoundsInt, Vector3, Vector3, VoxelOperation>(out var v, out var v2, out var v3, out var v4, out var v5) && _worlds.TryGetValue(v, out var value) && Mathf.Approximately(v4.sqrMagnitude, 1f) && v3.IsValid(10000f))
		{
			OnMineCommandReceived(value, v2, v3, v4, v5);
		}
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

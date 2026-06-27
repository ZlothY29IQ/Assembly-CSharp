using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Voxels;

[DefaultExecutionOrder(5)]
public class VoxelWorld : MonoBehaviour
{
	public enum WorldType
	{
		Infinite,
		Bounded
	}

	private static readonly Dictionary<int, VoxelWorld> WorldLookup = new Dictionary<int, VoxelWorld>();

	[Header("World Settings")]
	public VoxelMaterialSet MaterialSet;

	public GenerationParameters generationParameters = new GenerationParameters
	{
		NoiseScale = 0.01f,
		GroundLevel = 0f,
		HeightScale = 0.01f,
		Octaves = 4,
		Persistence = 0.5f,
		IsoLevel = 0f,
		Seed = 12345,
		NormalThreshold = 60f,
		AreaWeightedNormals = true
	};

	[SerializeField]
	private WorldType worldType;

	[SerializeField]
	private UnityEngine.BoundsInt worldBounds;

	[SerializeField]
	private float worldScale = 1f;

	[SerializeField]
	private int chunkSize = 32;

	[SerializeField]
	private int viewDistance = 5;

	[SerializeField]
	private int maxJobs = 10;

	[SerializeField]
	private bool registerAsSceneWorld = true;

	[SerializeField]
	private bool persistChanges = true;

	[Header("References")]
	public ChunkComponent chunkPrefab;

	public Transform target;

	protected Dictionary<int3, Chunk> chunks = new Dictionary<int3, Chunk>();

	private Dictionary<int3, ChunkTaskSet> chunkJobs = new Dictionary<int3, ChunkTaskSet>();

	private List<int3> completedJobs = new List<int3>();

	protected NativeHashSet<int3> chunksToGenerate;

	protected NativeList<int3> sortedChunks;

	protected int chunkSortIndex;

	protected JobHandle sortJobHandle;

	protected int sortedChunkCount;

	protected int3 playerChunk = new int3(int.MaxValue, int.MaxValue, int.MaxValue);

	protected bool generationQueueChanged;

	private List<int3> chunksToRemove = new List<int3>();

	private UnityEngine.Pool.ObjectPool<Chunk> _chunkPool;

	private UnityEngine.Pool.ObjectPool<ChunkComponent> _chunkComponentPool;

	private UnityEngine.Pool.ObjectPool<Mesh> _meshPool;

	private bool _updateWorld = true;

	private static UnityEngine.BoundsInt _opBounds;

	private static Chunk _opChunk;

	private static bool _opAnyChanged;

	private static Func<int3, (byte density, byte material), (byte density, byte material)> _opSetDataFunction;

	public IEnumerable<Chunk> Chunks => chunks.Values;

	public bool Initialized { get; private set; }

	public bool IsInfinite => worldType == WorldType.Infinite;

	public UnityEngine.BoundsInt WorldBounds => worldBounds;

	public int Id { get; private set; }

	public bool UpdateWorld
	{
		get
		{
			return _updateWorld;
		}
		set
		{
			_updateWorld = value;
		}
	}

	public int3 ChunkSize { get; private set; }

	public int VoxelDimension { get; private set; }

	public int VoxelCount { get; private set; }

	public MeshGenerationMode MeshGenerationMode => generationParameters.MeshGenerationMode;

	public bool WorldGenerationComplete
	{
		get
		{
			if (chunks.Count > 0)
			{
				return chunksToGenerate.Count == 0;
			}
			return false;
		}
	}

	public float Scale => worldScale;

	public static bool ExistsFor(Scene scene)
	{
		return WorldLookup.ContainsKey(scene.GetHashCode());
	}

	public static bool ExistsFor(GameObject gameObject)
	{
		return ExistsFor(gameObject.scene);
	}

	public static bool ExistsFor(Component component)
	{
		return ExistsFor(component.gameObject.scene);
	}

	public static void SetFor(Scene scene, VoxelWorld voxelWorld)
	{
		if (!WorldLookup.TryAdd(scene.GetHashCode(), voxelWorld))
		{
			throw new InvalidOperationException($"Scene {scene} already has a VoxelWorld set.");
		}
	}

	public static void SetFor(GameObject gameObject, VoxelWorld voxelWorld)
	{
		SetFor(gameObject.scene, voxelWorld);
	}

	public static void SetFor(Component component, VoxelWorld voxelWorld)
	{
		SetFor(component.gameObject.scene, voxelWorld);
	}

	public static VoxelWorld GetFor(Scene scene)
	{
		if (!WorldLookup.TryGetValue(scene.GetHashCode(), out var value))
		{
			Debug.LogError($"No VoxelWorld found for scene {scene}");
		}
		return value;
	}

	public static VoxelWorld GetFor(GameObject gameObject)
	{
		return GetFor(gameObject.scene);
	}

	public static VoxelWorld GetFor(Component component)
	{
		return GetFor(component.gameObject.scene);
	}

	private void Awake()
	{
		if (registerAsSceneWorld && !ExistsFor(this))
		{
			SetFor(base.gameObject, this);
		}
		switch (generationParameters.MeshGenerationMode)
		{
		case MeshGenerationMode.MarchingCubes:
			Chunk.Pad = 1;
			break;
		case MeshGenerationMode.SurfaceNets:
			Chunk.Pad = 2;
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		if (!target)
		{
			target = base.transform;
		}
		Id = this.GenerateHashcodeFromPath();
	}

	private void Start()
	{
		Chunk.DefaultSize = chunkSize;
		ChunkSize = Chunk.DefaultSize;
		VoxelDimension = chunkSize + Chunk.Pad;
		VoxelCount = VoxelDimension * VoxelDimension * VoxelDimension;
		int num = viewDistance * 2 + 1;
		chunksToGenerate = new NativeHashSet<int3>(num * num * num, Allocator.Persistent);
		sortedChunks = new NativeList<int3>(Allocator.Persistent);
		ConfigurePools();
		Initialized = true;
	}

	private void OnEnable()
	{
		VoxelManager.Register(this);
	}

	private void OnDisable()
	{
		if (!ApplicationQuittingState.IsQuitting)
		{
			VoxelManager.Unregister(this);
		}
	}

	private void OnDestroy()
	{
		if (persistChanges)
		{
			SaveChunks();
		}
		foreach (Chunk value in chunks.Values)
		{
			value.Dispose();
		}
		if (sortedChunks.IsCreated)
		{
			sortedChunks.Dispose();
		}
		if (chunksToGenerate.IsCreated)
		{
			chunksToGenerate.Dispose();
		}
		sortedChunks = default(NativeList<int3>);
		chunksToGenerate = default(NativeHashSet<int3>);
	}

	private void Update()
	{
		if (!_updateWorld)
		{
			return;
		}
		if (generationQueueChanged)
		{
			sortJobHandle.Complete();
			sortedChunkCount = sortedChunks.Length;
			chunkSortIndex = 0;
			generationQueueChanged = false;
		}
		foreach (ChunkTaskSet value in chunkJobs.Values)
		{
			if (value.CompleteIfReady())
			{
				HandleJobCompletion(value);
			}
		}
		foreach (int3 completedJob in completedJobs)
		{
			chunkJobs.Remove(completedJob);
		}
		completedJobs.Clear();
		foreach (Chunk value2 in chunks.Values)
		{
			if (value2.IsDirty)
			{
				chunksToGenerate.Add(value2.Id);
				generationQueueChanged = true;
			}
		}
		while (chunkSortIndex < sortedChunks.Length && chunkJobs.Count < maxJobs)
		{
			int3 int5 = sortedChunks[chunkSortIndex++];
			chunksToGenerate.Remove(int5);
			ProcessChunk(int5);
		}
		UpdateVisibleChunks();
	}

	private void SaveChunks()
	{
		Debug.Log("Saving chunks...");
		foreach (Chunk value in chunks.Values)
		{
			if (value.IsDataChanged)
			{
				ChunkIO.SaveChunk(new ChunkDTO(value));
			}
		}
	}

	private void ConfigurePools()
	{
		_chunkPool = new UnityEngine.Pool.ObjectPool<Chunk>(() => new Chunk(int3.zero, ChunkSize), delegate
		{
		}, delegate(Chunk chunk)
		{
			if ((bool)chunk.Component)
			{
				_chunkComponentPool.Release(chunk.Component);
				chunk.SetComponent(null);
			}
			chunk.Clear();
		}, delegate(Chunk chunk)
		{
			chunk.Dispose();
		}, collectionCheck: true, 100, 100);
		_chunkComponentPool = new UnityEngine.Pool.ObjectPool<ChunkComponent>(() => UnityEngine.Object.Instantiate(chunkPrefab), delegate(ChunkComponent chunkComponent)
		{
			chunkComponent.gameObject.SetActive(value: false);
			chunkComponent.transform.SetParent(base.transform, worldPositionStays: false);
		}, delegate(ChunkComponent chunkComponent)
		{
			if ((bool)chunkComponent.meshFilter.sharedMesh)
			{
				Mesh sharedMesh = chunkComponent.meshFilter.sharedMesh;
				chunkComponent.meshFilter.sharedMesh = null;
				chunkComponent.meshCollider.sharedMesh = null;
				_meshPool.Release(sharedMesh);
			}
			chunkComponent.gameObject.SetActive(value: false);
		}, delegate(ChunkComponent chunkComponent)
		{
			if ((bool)chunkComponent)
			{
				UnityEngine.Object.Destroy(chunkComponent.gameObject);
			}
		}, collectionCheck: true, 100, 100);
		_meshPool = new UnityEngine.Pool.ObjectPool<Mesh>(() => new Mesh(), null, delegate(Mesh mesh)
		{
			mesh.Clear(keepVertexLayout: false);
		}, null, collectionCheck: true, 100, 100);
	}

	public bool TryGetChunk(int3 chunkId, out Chunk chunk)
	{
		return chunks.TryGetValue(chunkId, out chunk);
	}

	private Chunk GetPooledChunk(int3 chunkId)
	{
		Chunk chunk = _chunkPool.Get();
		chunk.World = this;
		chunk.Id = chunkId;
		return chunk;
	}

	private Chunk CreateOrLoadChunk(int3 chunkId)
	{
		Chunk pooledChunk = GetPooledChunk(chunkId);
		if (persistChanges && ChunkIO.TryLoadChunk(chunkId, out var dto))
		{
			pooledChunk.SetFrom(dto);
		}
		else
		{
			pooledChunk.Id = chunkId;
		}
		return pooledChunk;
	}

	public void SetChunkFrom(ChunkDTO dto)
	{
		if (!chunks.TryGetValue(dto.Id, out var value))
		{
			value = GetPooledChunk(dto.Id);
			chunks[dto.Id] = value;
		}
		value.SetFrom(dto);
	}

	public void UpdateChunkFrom(ChunkDTO dto)
	{
		if (!chunks.TryGetValue(dto.Id, out var value))
		{
			value = GetPooledChunk(dto.Id);
			chunks[dto.Id] = value;
			value.SetFrom(dto);
		}
		else
		{
			value.UpdateFrom(dto);
		}
	}

	private void Save(Chunk chunk)
	{
		if (chunk.IsDataChanged)
		{
			ChunkIO.SaveChunk(new ChunkDTO(chunk));
		}
	}

	private void Unload(Chunk chunk)
	{
		if (persistChanges)
		{
			Save(chunk);
		}
		_chunkPool.Release(chunk);
	}

	private void UpdateVisibleChunks(bool isFirstTime = false)
	{
		int3 chunkIdForWorldPosition = GetChunkIdForWorldPosition(target.position);
		if (chunkIdForWorldPosition.Equals(playerChunk) && !generationQueueChanged)
		{
			return;
		}
		playerChunk = chunkIdForWorldPosition;
		generationQueueChanged = true;
		switch (worldType)
		{
		case WorldType.Infinite:
		{
			for (int l = -viewDistance; l <= viewDistance; l++)
			{
				for (int m = -viewDistance; m <= viewDistance; m++)
				{
					for (int n = -viewDistance; n <= viewDistance; n++)
					{
						int3 int6 = playerChunk + new int3(l, m, n);
						if (!chunks.ContainsKey(int6) && !chunksToGenerate.Contains(int6))
						{
							chunksToGenerate.Add(int6);
						}
					}
				}
			}
			break;
		}
		case WorldType.Bounded:
		{
			(int3 min, int3 max) chunkBoundsForLocalBounds = GetChunkBoundsForLocalBounds(worldBounds);
			int3 item = chunkBoundsForLocalBounds.min;
			int3 item2 = chunkBoundsForLocalBounds.max;
			for (int i = item.x; i <= item2.x; i++)
			{
				for (int j = item.y; j <= item2.y; j++)
				{
					for (int k = item.z; k <= item2.z; k++)
					{
						int3 int5 = new int3(i, j, k);
						if (!chunks.ContainsKey(int5) && !chunksToGenerate.Contains(int5))
						{
							chunksToGenerate.Add(int5);
						}
					}
				}
			}
			break;
		}
		default:
			throw new ArgumentOutOfRangeException();
		}
		SortChunksJob jobData = new SortChunksJob
		{
			ChunkSet = chunksToGenerate,
			SortedChunks = sortedChunks
		};
		sortJobHandle = jobData.Schedule();
		if (worldType != WorldType.Infinite)
		{
			return;
		}
		int num = viewDistance + 2;
		chunksToRemove.Clear();
		foreach (int3 key in chunks.Keys)
		{
			if (Mathf.Abs(key.x - playerChunk.x) > num || Mathf.Abs(key.y - playerChunk.y) > num || Mathf.Abs(key.z - playerChunk.z) > num)
			{
				chunksToRemove.Add(key);
			}
		}
		foreach (int3 item3 in chunksToRemove)
		{
			if (chunks.TryGetValue(item3, out var value))
			{
				if (chunkJobs.TryGetValue(item3, out var value2))
				{
					value2.Complete();
					chunkJobs.Remove(item3);
				}
				Unload(value);
				chunks.Remove(item3);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int3 GetChunkIdForWorldPosition(Vector3 worldPosition)
	{
		return GetLocalPosition(worldPosition).LocalPositionToChunkId(ChunkSize);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int3 GetChunkIdForLocalPosition(Vector3 voxelWorldPosition)
	{
		return voxelWorldPosition.LocalPositionToChunkId(ChunkSize);
	}

	public void SetWorldType(WorldType newWorldType, bool force = false)
	{
		if (force || worldType != newWorldType)
		{
			worldType = newWorldType;
			RegenerateAllChunks();
		}
	}

	public void SetWorldBounds(UnityEngine.BoundsInt bounds)
	{
		worldBounds = bounds;
		SetWorldType(WorldType.Bounded, force: true);
	}

	public static void SaveWorld(Scene scene)
	{
		VoxelWorld voxelWorld = GetFor(scene);
		foreach (Chunk value in voxelWorld.chunks.Values)
		{
			voxelWorld.Save(value);
		}
	}

	public static void ResetWorld(Scene scene)
	{
		ChunkIO.DeleteWorld();
		VoxelWorld voxelWorld = GetFor(scene);
		if ((bool)voxelWorld)
		{
			voxelWorld.RegenerateAllChunks();
		}
	}

	private void RegenerateAllChunks()
	{
		if (!Initialized)
		{
			return;
		}
		foreach (ChunkTaskSet value in chunkJobs.Values)
		{
			value.Complete();
		}
		chunkJobs.Clear();
		completedJobs.Clear();
		chunksToGenerate.Clear();
		sortedChunks.Clear();
		foreach (Chunk value2 in chunks.Values)
		{
			value2.Clear();
		}
		generationQueueChanged = true;
	}

	public void ResetChunk(int3 chunkId)
	{
		if (chunks.TryGetValue(chunkId, out var value) && value.IsDataChanged)
		{
			if (chunkJobs.TryGetValue(chunkId, out var value2))
			{
				value2.Complete();
				chunkJobs.Remove(chunkId);
			}
			value.IsDataGenerated = false;
			value.IsDirty = true;
		}
	}

	private void ProcessChunk(int3 chunkId)
	{
		if (!chunks.TryGetValue(chunkId, out var value))
		{
			value = CreateOrLoadChunk(chunkId);
			chunks[chunkId] = value;
		}
		if (!value.IsDirty)
		{
			Debug.LogWarning($"{value} is not dirty, skipping processing");
			return;
		}
		value.IsDirty = false;
		ChunkTaskSet chunkTaskSet = new ChunkTaskSet(value, generationParameters);
		ChunkState state = value.State;
		if (state < ChunkState.VoxelDataGenerated)
		{
			chunkTaskSet.AddTask(ChunkTask.CreateVoxelDataJob);
		}
		if (state < ChunkState.MeshDataGenerated)
		{
			if (generationParameters.MeshGenerationMode == MeshGenerationMode.MarchingCubes)
			{
				chunkTaskSet.AddTask(ChunkTask.CreateMeshDataJob, CreateChunkMesh);
			}
			else if (generationParameters.MeshGenerationMode == MeshGenerationMode.SurfaceNets)
			{
				chunkTaskSet.AddTask(ChunkTask.CreateMeshDataJob);
				chunkTaskSet.AddTask(ChunkTask.CreateSurfaceNetsPostProcessingJob, CreateChunkMesh);
			}
			else
			{
				Debug.LogError($"Unknown mesh generation mode: {generationParameters.MeshGenerationMode}");
			}
		}
		else if (state < ChunkState.MeshCreated)
		{
			chunkTaskSet.AddTask(null, CreateChunkMesh);
		}
		if (state < ChunkState.CollisionBaked)
		{
			chunkTaskSet.AddTask(ChunkTask.CreateCollisionJob, AssignMesh);
		}
		else if (state < ChunkState.MeshAssigned)
		{
			chunkTaskSet.AddTask(null, AssignMesh);
		}
		if (!chunkTaskSet.IsEmpty)
		{
			chunkJobs.Add(value.Id, chunkTaskSet);
			chunkTaskSet.Start();
		}
		else
		{
			Debug.LogWarning($"{value} was dirty but nothing to do?");
		}
	}

	private void MeshChunkImmediately(Chunk chunk)
	{
		ChunkTask.CreateMeshDataJob(chunk, generationParameters).Complete();
		if (generationParameters.MeshGenerationMode == MeshGenerationMode.SurfaceNets)
		{
			ChunkTask.CreateSurfaceNetsPostProcessingJob(chunk, generationParameters).Complete();
		}
		chunk.Mesh = CreateMesh(chunk);
		if ((bool)chunk.Mesh)
		{
			ChunkTask.CreateCollisionJob(chunk).Complete();
		}
		AssignMesh(chunk);
		chunkJobs.Remove(chunk.Id);
	}

	private void CreateChunkMesh(Chunk chunk)
	{
		chunk.Mesh = CreateMesh(chunk);
	}

	private Mesh CreateMesh(Chunk chunk)
	{
		if (chunk.VertexCount == 0)
		{
			chunk.DisposeMeshData();
			chunk.IsMeshCreated = true;
			return null;
		}
		int vertexCount = chunk.VertexCount;
		Mesh mesh = _meshPool.Get();
		if (vertexCount > chunk.VertexData.Length)
		{
			Debug.LogError($"Vertex count {vertexCount} exceeds allocated vertex data length {chunk.VertexData.Length} for chunk {chunk.Id}. This is likely a bug in the meshing job.");
			return null;
		}
		mesh.SetVertexBufferParams(vertexCount, MeshVertexData.VertexBufferMemoryLayout);
		mesh.SetIndexBufferParams(vertexCount, IndexFormat.UInt16);
		mesh.SetVertexBufferData(chunk.VertexData, 0, 0, vertexCount, 0, MeshUpdateFlags.DontValidateIndices);
		mesh.SetIndexBufferData(chunk.TriangleData, 0, 0, vertexCount, MeshUpdateFlags.DontValidateIndices);
		mesh.subMeshCount = 1;
		mesh.SetSubMesh(0, new SubMeshDescriptor(0, vertexCount));
		mesh.RecalculateBounds();
		chunk.DisposeMeshData();
		chunk.IsMeshCreated = true;
		return mesh;
	}

	private void AssignMesh(Chunk chunk)
	{
		Mesh mesh = chunk.Mesh;
		if ((bool)mesh)
		{
			if (!chunk.Component)
			{
				ChunkComponent chunkComponent = _chunkComponentPool.Get();
				chunkComponent.transform.SetParent(base.transform, worldPositionStays: false);
				chunkComponent.transform.localScale = Vector3.one * worldScale;
				chunkComponent.transform.localPosition = (chunk.Id * ChunkSize).ToVector3() * worldScale;
				chunk.SetComponent(chunkComponent);
			}
			Mesh sharedMesh = chunk.MeshFilter.sharedMesh;
			chunk.MeshFilter.sharedMesh = mesh;
			chunk.MeshCollider.sharedMesh = mesh;
			if ((bool)sharedMesh)
			{
				_meshPool.Release(sharedMesh);
			}
			chunk.GameObject.SetActive(value: true);
		}
		else if ((bool)chunk.Component)
		{
			_chunkComponentPool.Release(chunk.Component);
			chunk.SetComponent(null);
		}
		chunk.IsMeshAssigned = true;
		chunk.IsDirty = false;
	}

	public void SetVoxelDensityCustom(UnityEngine.BoundsInt worldBounds, Func<int3, byte, byte> setDensityFunction, bool immediate = true)
	{
		ForEachChunkInBounds(worldBounds, SetVoxelDensityInChunk);
		void SetDensity(int3 voxelWorldPosition, int3 voxelLocalPosition, int voxelIndex, byte density)
		{
			byte b = setDensityFunction(voxelWorldPosition, density);
			if (b != density)
			{
				_opChunk.Density[voxelIndex] = b;
				_opAnyChanged = true;
			}
		}
		void SetVoxelDensityInChunk()
		{
			ChunkTaskSet value;
			bool flag = chunkJobs.TryGetValue(_opChunk.Id, out value);
			if (flag)
			{
				value.Complete();
			}
			_opAnyChanged = false;
			ForEachVoxelInChunkInBounds(_opBounds, _opChunk, SetDensity);
			if (_opAnyChanged)
			{
				_opChunk.IsDataChanged = true;
				if (immediate)
				{
					MeshChunkImmediately(_opChunk);
				}
				else
				{
					_opChunk.IsMeshGenerated = false;
					_opChunk.IsDirty = true;
				}
			}
			else if (flag)
			{
				HandleJobCompletion(value);
			}
		}
	}

	public void SetVoxelDataCustom(UnityEngine.BoundsInt worldBounds, Func<int3, (byte density, byte material), (byte density, byte material)> setDataFunction, bool immediate = true)
	{
		_opSetDataFunction = setDataFunction;
		ForEachChunkInBounds(worldBounds, SetVoxelDataInChunk);
		static void SetVoxelData(int3 voxelWorldPosition, int3 voxelLocalPosition, int voxelIndex, byte density, byte material)
		{
			var (b, b2) = _opSetDataFunction(voxelWorldPosition, (density, material));
			if (b != density || b2 != material)
			{
				_opChunk.Density[voxelIndex] = b;
				_opChunk.Material[voxelIndex] = b2;
				_opAnyChanged = true;
			}
		}
		void SetVoxelDataInChunk()
		{
			ChunkTaskSet value;
			bool flag = chunkJobs.TryGetValue(_opChunk.Id, out value);
			if (flag)
			{
				value.Complete();
			}
			_opAnyChanged = false;
			ForEachVoxelInChunkInBounds(_opBounds, _opChunk, SetVoxelData);
			if (_opAnyChanged)
			{
				_opChunk.IsDataChanged = true;
				if (immediate)
				{
					MeshChunkImmediately(_opChunk);
				}
				else
				{
					_opChunk.IsMeshGenerated = false;
					_opChunk.IsDirty = true;
				}
			}
			else if (flag)
			{
				HandleJobCompletion(value);
			}
		}
	}

	public void SetVoxelDataCustom(int3[] voxels, Func<int3, (byte density, byte material), (byte density, byte material)> setDataFunction, bool immediate = true)
	{
		UnityEngine.BoundsInt boundsFor = GetBoundsFor(voxels);
		ForEachChunkInBounds(boundsFor, SetVoxelDataInChunk);
		void SetVoxelData(int3 voxelWorldPosition, int3 voxelLocalPosition, int voxelIndex, byte density, byte material)
		{
			var (b, b2) = setDataFunction(voxelWorldPosition, (density, material));
			if (b != density || b2 != material)
			{
				_opChunk.Density[voxelIndex] = b;
				_opChunk.Material[voxelIndex] = b2;
				_opAnyChanged = true;
			}
		}
		void SetVoxelDataInChunk()
		{
			ChunkTaskSet value;
			bool flag = chunkJobs.TryGetValue(_opChunk.Id, out value);
			if (flag)
			{
				value.Complete();
			}
			_opAnyChanged = false;
			ForEachSpecifiedVoxelInChunk(voxels, _opChunk, SetVoxelData);
			if (_opAnyChanged)
			{
				_opChunk.IsDataChanged = true;
				if (immediate)
				{
					MeshChunkImmediately(_opChunk);
				}
				else
				{
					_opChunk.IsMeshGenerated = false;
					_opChunk.IsDirty = true;
				}
			}
			else if (flag)
			{
				HandleJobCompletion(value);
			}
		}
	}

	public void SetVoxels(UnityEngine.BoundsInt bounds, Voxel[] voxels, bool immediate = true)
	{
		bounds.GetVoxelCount();
		ForEachChunkInBounds(bounds, SetVoxelDataInChunk);
		void SetVoxelData(int3 voxelWorldPosition, int3 voxelLocalPosition, int voxelIndex, byte material, byte density)
		{
			byte material2 = voxels[P_5.index].Material;
			byte density2 = voxels[P_5.index].Density;
			byte b = material2;
			byte b2 = density2;
			if (b != density || b2 != material)
			{
				_opChunk.Density[voxelIndex] = b;
				_opChunk.Material[voxelIndex] = b2;
				_opAnyChanged = true;
			}
		}
		void SetVoxelDataInChunk()
		{
			ChunkTaskSet value;
			bool flag = chunkJobs.TryGetValue(_opChunk.Id, out value);
			if (flag)
			{
				value.Complete();
			}
			_opAnyChanged = false;
			int3 zero = int3.zero;
			int3 max = _opChunk.Dimensions - 1;
			int index = 0;
			for (int i = _opBounds.min.x; i <= _opBounds.max.x; i++)
			{
				for (int j = _opBounds.min.y; j <= _opBounds.max.y; j++)
				{
					for (int k = _opBounds.min.z; k <= _opBounds.max.z; k++)
					{
						int3 int5 = new int3(i, j, k);
						int3 localPosition = _opChunk.GetLocalPosition(int5);
						if (localPosition.IsInBounds(zero, max))
						{
							int num = localPosition.x + VoxelDimension * (localPosition.y + VoxelDimension * localPosition.z);
							SetVoxelData(int5, localPosition, num, _opChunk.Material[num], _opChunk.Density[num]);
						}
						index++;
					}
				}
			}
			if (_opAnyChanged)
			{
				_opChunk.IsDataChanged = true;
				if (immediate)
				{
					MeshChunkImmediately(_opChunk);
				}
				else
				{
					_opChunk.IsMeshGenerated = false;
					_opChunk.IsDirty = true;
				}
			}
			else if (flag)
			{
				HandleJobCompletion(value);
			}
		}
	}

	public void SetVoxelDensity(UnityEngine.BoundsInt bounds, byte[] data, bool immediate = true)
	{
		ForEachChunkInBounds(bounds, SetVoxelDensityInChunk);
		void SetVoxelDensity(int voxelIndex, byte density)
		{
			byte b = data[P_2.index];
			if (b != density)
			{
				_opChunk.Density[voxelIndex] = b;
				_opAnyChanged = true;
			}
		}
		void SetVoxelDensityInChunk()
		{
			ChunkTaskSet value;
			bool flag = chunkJobs.TryGetValue(_opChunk.Id, out value);
			if (flag)
			{
				value.Complete();
			}
			_opAnyChanged = false;
			int3 zero = int3.zero;
			int3 max = _opChunk.Dimensions - 1;
			int index = 0;
			for (int i = _opBounds.min.x; i <= _opBounds.max.x; i++)
			{
				for (int j = _opBounds.min.y; j <= _opBounds.max.y; j++)
				{
					for (int k = _opBounds.min.z; k <= _opBounds.max.z; k++)
					{
						int3 voxelPosition = new int3(i, j, k);
						int3 localPosition = _opChunk.GetLocalPosition(voxelPosition);
						if (localPosition.IsInBounds(zero, max))
						{
							int num = localPosition.x + VoxelDimension * (localPosition.y + VoxelDimension * localPosition.z);
							SetVoxelDensity(num, _opChunk.Density[num]);
						}
						index++;
					}
				}
			}
			if (_opAnyChanged)
			{
				_opChunk.IsDataChanged = true;
				if (immediate)
				{
					MeshChunkImmediately(_opChunk);
				}
				else
				{
					_opChunk.IsMeshGenerated = false;
					_opChunk.IsDirty = true;
				}
			}
			else if (flag)
			{
				HandleJobCompletion(value);
			}
		}
	}

	public byte GetVoxelMaterial(int3 voxelId)
	{
		int3 key = voxelId.LocalPositionToChunkId(ChunkSize);
		if (!chunks.TryGetValue(key, out var value))
		{
			return 0;
		}
		int3 int5 = voxelId - value.Id * value.Size;
		int index = int5.x + VoxelDimension * (int5.y + VoxelDimension * int5.z);
		return value.Material[index];
	}

	public byte GetVoxelDensity(int3 voxelId)
	{
		int3 key = voxelId.LocalPositionToChunkId(ChunkSize);
		if (!chunks.TryGetValue(key, out var value))
		{
			return 0;
		}
		int3 int5 = voxelId - value.Id * value.Size;
		int index = int5.x + VoxelDimension * (int5.y + VoxelDimension * int5.z);
		return value.Density[index];
	}

	public Voxel GetVoxelData(int3 voxelId)
	{
		int3 key = voxelId.LocalPositionToChunkId(ChunkSize);
		if (!chunks.TryGetValue(key, out var value))
		{
			return default(Voxel);
		}
		int3 int5 = voxelId - value.Id * value.Size;
		int index = int5.x + VoxelDimension * (int5.y + VoxelDimension * int5.z);
		return new Voxel(value.Material[index], value.Density[index]);
	}

	public void SetVoxelMaterial(int3 voxelId, byte material)
	{
		int3 key = voxelId.LocalPositionToChunkId(ChunkSize);
		if (chunks.TryGetValue(key, out var value))
		{
			int3 int5 = voxelId - value.Id * value.Size;
			int index = int5.x + VoxelDimension * (int5.y + VoxelDimension * int5.z);
			value.Material[index] = material;
		}
	}

	public void SetVoxelDensity(int3 voxelId, byte density)
	{
		int3 key = voxelId.LocalPositionToChunkId(ChunkSize);
		if (chunks.TryGetValue(key, out var value))
		{
			int3 int5 = voxelId - value.Id * value.Size;
			int index = int5.x + VoxelDimension * (int5.y + VoxelDimension * int5.z);
			value.Density[index] = density;
		}
	}

	public void SetVoxelData(int3 voxelId, Voxel data)
	{
		int3 key = voxelId.LocalPositionToChunkId(ChunkSize);
		if (chunks.TryGetValue(key, out var value))
		{
			int3 int5 = voxelId - value.Id * value.Size;
			int index = int5.x + VoxelDimension * (int5.y + VoxelDimension * int5.z);
			value.Material[index] = data.Material;
			value.Density[index] = data.Density;
		}
	}

	public static UnityEngine.BoundsInt GetBoundsFor(int3[] voxels)
	{
		int3 int5 = new int3(int.MaxValue, int.MaxValue, int.MaxValue);
		int3 int6 = new int3(int.MinValue, int.MinValue, int.MinValue);
		foreach (int3 x in voxels)
		{
			int5 = math.min(x, int5);
			int6 = math.max(x, int6);
		}
		return new UnityEngine.BoundsInt
		{
			min = int5.ToVectorInt(),
			max = int6.ToVectorInt()
		};
	}

	public (int3 min, int3 max) GetChunkBoundsForLocalBounds(UnityEngine.BoundsInt worldBounds, bool includeLLC = false)
	{
		return (min: (worldBounds.min - (includeLLC ? (Vector3Int.one * Chunk.Pad) : Vector3Int.zero)).LocalPositionToChunkId(ChunkSize), max: worldBounds.max.LocalPositionToChunkId(ChunkSize));
	}

	public bool BoundsChunksLoaded(UnityEngine.BoundsInt localWorldBounds, bool includeLLC = false)
	{
		if (!Initialized)
		{
			return false;
		}
		(int3 min, int3 max) chunkBoundsForLocalBounds = GetChunkBoundsForLocalBounds(localWorldBounds, includeLLC);
		int3 item = chunkBoundsForLocalBounds.min;
		int3 item2 = chunkBoundsForLocalBounds.max;
		for (int i = item.x; i <= item2.x; i++)
		{
			for (int j = item.y; j <= item2.y; j++)
			{
				for (int k = item.z; k <= item2.z; k++)
				{
					int3 key = new int3(i, j, k);
					if (!chunks.TryGetValue(key, out var _))
					{
						return false;
					}
				}
			}
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private Vector3Int ClampToWorldBounds(Vector3Int coord)
	{
		if (worldType == WorldType.Bounded)
		{
			coord.Clamp(worldBounds.min, worldBounds.max);
		}
		return coord;
	}

	private void ForEachChunkInBounds(UnityEngine.BoundsInt bounds, Action action)
	{
		_opBounds = bounds;
		int3 int5 = ClampToWorldBounds(bounds.min - Vector3Int.one * Chunk.Pad).LocalPositionToChunkId(ChunkSize);
		int3 int6 = ClampToWorldBounds(bounds.max).LocalPositionToChunkId(ChunkSize);
		for (int i = int5.x; i <= int6.x; i++)
		{
			for (int j = int5.y; j <= int6.y; j++)
			{
				for (int k = int5.z; k <= int6.z; k++)
				{
					int3 int7 = new int3(i, j, k);
					if (chunks.TryGetValue(int7, out var value))
					{
						_opChunk = value;
						action();
					}
					else
					{
						Debug.LogError($"Couldn't find chunk {int7} to perform operation");
					}
				}
			}
		}
	}

	public void GetChunksForBounds(UnityEngine.BoundsInt worldBounds, ref List<Chunk> list)
	{
		if (list == null)
		{
			list = new List<Chunk>();
		}
		list.Clear();
		int3 int5 = ClampToWorldBounds(worldBounds.min - Vector3Int.one * Chunk.Pad).LocalPositionToChunkId(ChunkSize);
		int3 int6 = ClampToWorldBounds(worldBounds.max).LocalPositionToChunkId(ChunkSize);
		for (int i = int5.x; i <= int6.x; i++)
		{
			for (int j = int5.y; j <= int6.y; j++)
			{
				for (int k = int5.z; k <= int6.z; k++)
				{
					int3 key = new int3(i, j, k);
					if (chunks.TryGetValue(key, out var value))
					{
						list.Add(value);
					}
				}
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Chunk GetChunkForLocalPosition(int3 worldPosition)
	{
		return chunks.GetValueOrDefault(worldPosition.LocalPositionToChunkId(ChunkSize));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Chunk GetChunkForLocalPosition(Vector3 worldPosition)
	{
		return chunks.GetValueOrDefault(worldPosition.LocalPositionToChunkId(ChunkSize));
	}

	private void ForEachVoxelInChunkInBounds(UnityEngine.BoundsInt worldBounds, Chunk chunk, Action<int3, int3, int, byte> action)
	{
		int3 int5 = new int3(worldBounds.min.x, worldBounds.min.y, worldBounds.min.z);
		int3 int6 = new int3(worldBounds.max.x, worldBounds.max.y, worldBounds.max.z);
		int3 int7 = chunk.Id * chunk.Size;
		int3 int8 = int7 + chunk.Dimensions - 1;
		int5.x = math.max(int5.x, int7.x);
		int5.y = math.max(int5.y, int7.y);
		int5.z = math.max(int5.z, int7.z);
		int6.x = math.min(int6.x, int8.x);
		int6.y = math.min(int6.y, int8.y);
		int6.z = math.min(int6.z, int8.z);
		if (int5.x > int6.x || int5.y > int6.y || int5.z > int6.z)
		{
			Debug.LogWarning($"No overlap between chunk {chunk.Id} and bounds {worldBounds}");
			return;
		}
		for (int i = int5.x; i <= int6.x; i++)
		{
			for (int j = int5.y; j <= int6.y; j++)
			{
				for (int k = int5.z; k <= int6.z; k++)
				{
					int3 int9 = new int3(i, j, k);
					int3 arg = int9 - chunk.Id * chunk.Size;
					int num = arg.x + VoxelDimension * (arg.y + VoxelDimension * arg.z);
					byte arg2 = chunk.Density[num];
					action(int9, arg, num, arg2);
				}
			}
		}
	}

	private void ForEachVoxelInChunkInBounds(UnityEngine.BoundsInt worldBounds, Chunk chunk, Action<int3, int3, int, byte, byte> action)
	{
		int3 int5 = new int3(worldBounds.min.x, worldBounds.min.y, worldBounds.min.z);
		int3 int6 = new int3(worldBounds.max.x, worldBounds.max.y, worldBounds.max.z);
		int3 int7 = chunk.Id * chunk.Size;
		int3 int8 = int7 + chunk.Dimensions - 1;
		int5.x = math.max(int5.x, int7.x);
		int5.y = math.max(int5.y, int7.y);
		int5.z = math.max(int5.z, int7.z);
		int6.x = math.min(int6.x, int8.x);
		int6.y = math.min(int6.y, int8.y);
		int6.z = math.min(int6.z, int8.z);
		if (int5.x > int6.x || int5.y > int6.y || int5.z > int6.z)
		{
			Debug.LogWarning($"No overlap between chunk {chunk.Id} and bounds {worldBounds}");
			return;
		}
		for (int i = int5.x; i <= int6.x; i++)
		{
			for (int j = int5.y; j <= int6.y; j++)
			{
				for (int k = int5.z; k <= int6.z; k++)
				{
					int3 int9 = new int3(i, j, k);
					int3 arg = int9 - chunk.Id * chunk.Size;
					int num = arg.x + VoxelDimension * (arg.y + VoxelDimension * arg.z);
					byte arg2 = chunk.Density[num];
					byte arg3 = chunk.Material[num];
					action(int9, arg, num, arg2, arg3);
				}
			}
		}
	}

	private void ForEachSpecifiedVoxelInChunk(int3[] voxels, Chunk chunk, Action<int3, int3, int, byte, byte> action)
	{
		int3 int5 = chunk.Id * chunk.Size;
		int3 max = int5 + chunk.Dimensions - 1;
		foreach (int3 int6 in voxels)
		{
			if (int6.IsInBounds(int5, max))
			{
				int3 arg = int6 - chunk.Id * chunk.Size;
				int num = arg.x + VoxelDimension * (arg.y + VoxelDimension * arg.z);
				byte arg2 = chunk.Density[num];
				byte arg3 = chunk.Material[num];
				action(int6, arg, num, arg2, arg3);
			}
		}
	}

	private void HandleJobCompletion(ChunkTaskSet chunkTask)
	{
		if (!chunkTask.HasChunks)
		{
			Debug.LogError("Chunk is null in HandleJobCompletion");
		}
		Chunk chunk = chunkTask.Chunk;
		if (chunk.State < ChunkState.MeshAssigned || chunk.IsDirty)
		{
			Debug.LogWarning($"{chunk} job completed with state {chunk.State} and dirty {chunk.IsDirty}");
		}
		completedJobs.Add(chunkTask.Chunk.Id);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public byte GetDensityAt(Vector3 voxelWorldPosition)
	{
		return GetDensityAt(voxelWorldPosition.ToInt3(), 0);
	}

	public byte GetDensityAt(int3 voxelWorldPosition, byte defaultDensity = 0)
	{
		Chunk chunkForLocalPosition = GetChunkForLocalPosition(voxelWorldPosition);
		if (chunkForLocalPosition != null && chunkForLocalPosition.IsDataGenerated)
		{
			int3 localPosition = chunkForLocalPosition.GetLocalPosition(voxelWorldPosition);
			return chunkForLocalPosition.Density[localPosition.x + VoxelDimension * (localPosition.y + VoxelDimension * localPosition.z)];
		}
		return defaultDensity;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetDensityAt(Vector3 voxelWorldPosition, byte density)
	{
		SetDensityAt(voxelWorldPosition.ToInt3(), density);
	}

	public void SetDensityAt(int3 voxelWorldPosition, byte density)
	{
		Chunk chunkForLocalPosition = GetChunkForLocalPosition(voxelWorldPosition);
		if (chunkForLocalPosition != null && chunkForLocalPosition.IsDataGenerated)
		{
			int3 localPosition = chunkForLocalPosition.GetLocalPosition(voxelWorldPosition);
			int index = localPosition.x + VoxelDimension * (localPosition.y + VoxelDimension * localPosition.z);
			if (chunkForLocalPosition.Density[index] != density)
			{
				chunkForLocalPosition.Density[index] = density;
				chunkForLocalPosition.IsDataChanged = true;
				chunkForLocalPosition.IsMeshCreated = false;
				chunkForLocalPosition.IsDirty = true;
			}
		}
		else
		{
			Debug.LogWarning($"No chunk found for world position {voxelWorldPosition}, cannot set density.");
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector3 GetLocalPosition(Vector3 worldPosition)
	{
		return Matrix4x4.TRS(base.transform.position, base.transform.rotation, Vector3.one * worldScale).inverse.MultiplyPoint(worldPosition);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector3 GetWorldPosition(Vector3 localPosition)
	{
		return Matrix4x4.TRS(base.transform.position, base.transform.rotation, Vector3.one * worldScale).MultiplyPoint(localPosition);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector3 GetWorldPosition(int3 localPosition)
	{
		return GetWorldPosition(localPosition.ToVector3());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int3 GetVoxelForWorldPosition(Vector3 worldPosition)
	{
		return GetLocalPosition(worldPosition).RoundToInt();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int3 GetVoxelForLocalPosition(Vector3 localPosition)
	{
		return localPosition.RoundToInt();
	}

	private void OnDrawGizmosSelected()
	{
		Matrix4x4 matrix = Matrix4x4.TRS(base.transform.position, base.transform.rotation, Vector3.one * worldScale);
		Vector3 vector = worldBounds.min;
		Vector3 vector2 = worldBounds.max;
		Vector3 vector3 = (vector + vector2) / 2f;
		Vector3 size = vector2 - vector;
		Gizmos.color = Color.green;
		Gizmos.DrawLine(base.transform.position, matrix.MultiplyPoint(vector3));
		Gizmos.matrix = matrix;
		Gizmos.DrawWireCube(vector3, size);
		Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
		Gizmos.DrawCube(vector3, size);
	}
}

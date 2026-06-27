using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Voxels;

public struct ChunkTask
{
	public Chunk Chunk;

	public JobHandle Handle;

	private Action _onJobComplete;

	public bool IsCreated => !Handle.Equals(default(JobHandle));

	public bool IsCompleted => Handle.IsCompleted;

	public bool CompleteIfReady()
	{
		if (Handle.IsCompleted)
		{
			Complete();
			return true;
		}
		return false;
	}

	public void Complete()
	{
		Handle.Complete();
		_onJobComplete?.Invoke();
		_onJobComplete = null;
	}

	public ChunkTask(Chunk chunk, JobHandle handle, Action onComplete = null)
	{
		Chunk = chunk;
		Handle = handle;
		_onJobComplete = onComplete;
	}

	public static ChunkTask CreateVoxelDataJob(Chunk chunk, GenerationParameters parameters)
	{
		if (!chunk.Density.IsCreated)
		{
			chunk.Density = new NativeArray<byte>(chunk.VoxelCount, Allocator.Persistent);
		}
		if (!chunk.Material.IsCreated)
		{
			chunk.Material = new NativeArray<byte>(chunk.VoxelCount, Allocator.Persistent);
		}
		JobHandle handle = IJobParallelForExtensions.Schedule(new GenerateVoxelDataJob
		{
			chunkPosition = chunk.Id,
			chunkSize = chunk.Size.x,
			dimension = chunk.Dimensions.x,
			noiseScale = parameters.NoiseScale,
			groundLevel = parameters.GroundLevel,
			heightCompensation = parameters.HeightCompensation,
			octaves = parameters.Octaves,
			persistence = parameters.Persistence,
			heightScale = parameters.HeightScale,
			seed = parameters.Seed,
			voxels = chunk.Density,
			materials = chunk.Material
		}, chunk.VoxelCount, 64);
		Action onComplete = delegate
		{
			chunk.IsDataGenerated = true;
			chunk.IsMeshGenerated = false;
			chunk.IsDirty = true;
		};
		return new ChunkTask(chunk, handle, onComplete);
	}

	public static ChunkTask CreateMeshDataJob(Chunk chunk, GenerationParameters parameters)
	{
		NativeCounter triangleCounter = new NativeCounter(Allocator.TempJob);
		Action onComplete = null;
		JobHandle handle = parameters.MeshGenerationMode switch
		{
			MeshGenerationMode.MarchingCubes => CreateMarchingCubesMeshJob(), 
			MeshGenerationMode.SurfaceNets => CreateSurfaceNetsMeshJob(), 
			_ => throw new ArgumentOutOfRangeException(), 
		};
		return new ChunkTask(chunk, handle, onComplete);
		JobHandle CreateMarchingCubesMeshJob()
		{
			int length = math.min(chunk.VoxelCount * 15, 65535);
			chunk.AllocateVertexData(length);
			chunk.AllocateTriangleData(length);
			onComplete = delegate
			{
				chunk.IsMeshGenerated = true;
				chunk.IsCollisionBaked = false;
				chunk.IsDirty = true;
				chunk.VertexCount = triangleCounter.Count * 3;
				triangleCounter.Dispose();
			};
			return new MarchingCubesMeshingJob
			{
				voxels = chunk.Density,
				materials = chunk.Material,
				vertexData = chunk.VertexData,
				triangleData = chunk.TriangleData,
				triangleCounter = triangleCounter,
				chunkSize = chunk.Size.x,
				isoLevel = parameters.IsoLevel.ToByte()
			}.Schedule();
		}
		JobHandle CreateSurfaceNetsMeshJob()
		{
			SurfaceNetsBuffer surfaceNetsBuffer = new SurfaceNetsBuffer(32768, 65536, chunk.VoxelCount);
			chunk.GenericMeshData = surfaceNetsBuffer;
			onComplete = delegate
			{
			};
			return new SurfaceNetsJob
			{
				sdf = chunk.Density,
				material = chunk.Material,
				shape = chunk.Dimensions,
				min = 0,
				max = chunk.Dimensions - 1,
				buffer = surfaceNetsBuffer,
				isoLevel = parameters.IsoLevel.ToByte()
			}.Schedule();
		}
	}

	public static ChunkTask CreateSurfaceNetsPostProcessingJob(Chunk chunk, GenerationParameters parameters)
	{
		if (!(chunk.GenericMeshData is SurfaceNetsBuffer surfaceNetsBuffer))
		{
			throw new InvalidOperationException($"{chunk} GenericMeshData is not a SurfaceNetsBuffer.");
		}
		if (surfaceNetsBuffer.Triangles.Length < 3)
		{
			chunk.IsMeshGenerated = true;
			chunk.IsCollisionBaked = false;
			chunk.IsDirty = true;
			chunk.VertexCount = 0;
			return default(ChunkTask);
		}
		NativeCounter triangleCounter = new NativeCounter(Allocator.TempJob);
		int length = math.min(chunk.VoxelCount * 15, 65535);
		if (!chunk.VertexData.IsCreated)
		{
			chunk.VertexData = new NativeArray<MeshVertexData>(length, Allocator.Persistent);
		}
		if (!chunk.TriangleData.IsCreated)
		{
			chunk.TriangleData = new NativeArray<ushort>(length, Allocator.Persistent);
		}
		MeshUtilities.VoxelMeshData voxelMeshData = MeshUtilities.SplitByAngle(surfaceNetsBuffer.Vertices.AsArray(), surfaceNetsBuffer.Materials.AsArray(), surfaceNetsBuffer.Triangles.AsArray(), parameters.NormalThreshold, parameters.AreaWeightedNormals);
		ref NativeList<float3> vertices = ref surfaceNetsBuffer.Vertices;
		ref NativeList<float3> vertices2 = ref voxelMeshData.Vertices;
		NativeList<float3> vertices3 = voxelMeshData.Vertices;
		NativeList<float3> vertices4 = surfaceNetsBuffer.Vertices;
		vertices = vertices3;
		vertices2 = vertices4;
		ref NativeList<byte> materials = ref surfaceNetsBuffer.Materials;
		ref NativeList<byte> materials2 = ref voxelMeshData.Materials;
		NativeList<byte> materials3 = voxelMeshData.Materials;
		NativeList<byte> materials4 = surfaceNetsBuffer.Materials;
		materials = materials3;
		materials2 = materials4;
		vertices = ref surfaceNetsBuffer.Normals;
		ref NativeList<float3> normals = ref voxelMeshData.Normals;
		vertices4 = voxelMeshData.Normals;
		vertices3 = surfaceNetsBuffer.Normals;
		vertices = vertices4;
		normals = vertices3;
		ref NativeList<int> triangles = ref surfaceNetsBuffer.Triangles;
		ref NativeList<int> triangles2 = ref voxelMeshData.Triangles;
		NativeList<int> triangles3 = voxelMeshData.Triangles;
		NativeList<int> triangles4 = surfaceNetsBuffer.Triangles;
		triangles = triangles3;
		triangles2 = triangles4;
		chunk.GenericMeshData = surfaceNetsBuffer;
		voxelMeshData.Dispose();
		JobHandle handle = new AssembleVertexDataJob
		{
			vertexData = chunk.VertexData,
			triangleData = chunk.TriangleData,
			triangleCounter = triangleCounter,
			srcVerts = surfaceNetsBuffer.Vertices.AsArray(),
			srcMats = surfaceNetsBuffer.Materials.AsArray(),
			srcNorm = surfaceNetsBuffer.Normals.AsArray(),
			srcTris = surfaceNetsBuffer.Triangles.AsArray()
		}.Schedule();
		Action onComplete = delegate
		{
			chunk.IsMeshGenerated = true;
			chunk.IsCollisionBaked = false;
			chunk.IsDirty = true;
			chunk.VertexCount = triangleCounter.Count * 3;
			triangleCounter.Dispose();
		};
		return new ChunkTask(chunk, handle, onComplete);
	}

	public static ChunkTask CreateCollisionJob(Chunk chunk, GenerationParameters parameters = default(GenerationParameters))
	{
		if (chunk.Mesh == null)
		{
			chunk.IsCollisionBaked = true;
			chunk.IsDirty = true;
			return default(ChunkTask);
		}
		JobHandle handle = new CollisionJob
		{
			MeshId = chunk.Mesh.GetEntityId()
		}.Schedule();
		Action onJobComplete = delegate
		{
			chunk.IsCollisionBaked = true;
			chunk.IsDirty = true;
		};
		return new ChunkTask
		{
			Handle = handle,
			_onJobComplete = onJobComplete,
			Chunk = chunk
		};
	}
}

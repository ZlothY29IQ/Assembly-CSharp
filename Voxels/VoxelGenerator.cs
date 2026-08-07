using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Voxels;

[Serializable]
public abstract class VoxelGenerator
{
	[Serializable]
	public struct MeshingParameters
	{
		public MeshGenerationMode MeshGenerationMode;

		public float NormalThreshold;

		public bool AreaWeightedNormals;
	}

	public int Seed = 12345;

	public MeshingParameters meshParameters = new MeshingParameters
	{
		MeshGenerationMode = MeshGenerationMode.MarchingCubes,
		NormalThreshold = 60f,
		AreaWeightedNormals = true
	};

	public bool PostProcessMesh => meshParameters.MeshGenerationMode == MeshGenerationMode.SurfaceNets;

	public virtual void DrawGizmos(VoxelWorld world)
	{
	}

	public virtual UnityEngine.BoundsInt GetWorldBounds()
	{
		return default(UnityEngine.BoundsInt);
	}

	public virtual void ShiftWorldBounds(Vector3Int worldShift)
	{
	}

	public virtual void InitWorld(VoxelWorld world)
	{
	}

	public abstract ChunkTask CreateVoxelDataJob(Chunk chunk);

	public ChunkTask CreateMeshDataJob(Chunk chunk)
	{
		NativeCounter triangleCounter = new NativeCounter(Allocator.TempJob);
		Action onComplete = null;
		JobHandle handle = meshParameters.MeshGenerationMode switch
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
				isoLevel = 0f.ToByte()
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
				isoLevel = 0f.ToByte()
			}.Schedule();
		}
	}

	public ChunkTask CreateMeshPostProcessJob(Chunk chunk)
	{
		if (!PostProcessMesh)
		{
			throw new InvalidOperationException($"{this} does not use Mesh Post-Processing.");
		}
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
		MeshUtilities.VoxelMeshData voxelMeshData = MeshUtilities.SplitByAngle(surfaceNetsBuffer.Vertices.AsArray(), surfaceNetsBuffer.Materials.AsArray(), surfaceNetsBuffer.Triangles.AsArray(), meshParameters.NormalThreshold, meshParameters.AreaWeightedNormals);
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
}

using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Voxels;

[Serializable]
public class Chunk : IDisposable
{
	public VoxelWorld World;

	public int3 Id;

	public int3 Size;

	public int3 Dimensions;

	public int VoxelCount;

	public NativeArray<byte> Density;

	public NativeArray<byte> Material;

	public NativeArray<MeshVertexData> VertexData;

	public NativeArray<ushort> TriangleData;

	public object GenericMeshData;

	public bool IsDataGenerated;

	public bool IsDataChanged;

	public bool IsMeshGenerated;

	public bool IsMeshCreated;

	public bool IsCollisionBaked;

	public bool IsMeshAssigned;

	public bool IsDirty = true;

	public int VertexCount;

	public Mesh Mesh;

	public ChunkComponent Component { get; private set; }

	public GameObject GameObject { get; private set; }

	public MeshFilter MeshFilter { get; private set; }

	public MeshRenderer MeshRenderer { get; private set; }

	public MeshCollider MeshCollider { get; private set; }

	public static int3 DefaultSize { get; set; } = 32;

	public static int Pad { get; set; } = 1;

	public ChunkState State
	{
		get
		{
			if (!IsDataGenerated)
			{
				return ChunkState.Created;
			}
			if (!IsMeshGenerated)
			{
				return ChunkState.VoxelDataGenerated;
			}
			if (!IsMeshCreated)
			{
				return ChunkState.MeshDataGenerated;
			}
			if (!IsCollisionBaked)
			{
				return ChunkState.MeshCreated;
			}
			if (!IsMeshAssigned)
			{
				return ChunkState.CollisionBaked;
			}
			return ChunkState.MeshAssigned;
		}
	}

	public Chunk(ChunkDTO dto)
	{
		Id = dto.Id;
		Size = dto.Size;
		Dimensions = dto.Dimensions;
		VoxelCount = Dimensions.x * Dimensions.y * Dimensions.z;
		Density = dto.Density;
		Material = dto.Material;
		VertexData = default(NativeArray<MeshVertexData>);
		TriangleData = default(NativeArray<ushort>);
		GenericMeshData = null;
	}

	public Chunk(int3 id, int3 size, int padding = -1)
	{
		if (padding < 0)
		{
			padding = Pad;
		}
		Id = id;
		Size = size;
		Dimensions = size + padding;
		VoxelCount = Dimensions.x * Dimensions.y * Dimensions.z;
		Density = default(NativeArray<byte>);
		Material = default(NativeArray<byte>);
		VertexData = default(NativeArray<MeshVertexData>);
		TriangleData = default(NativeArray<ushort>);
		GenericMeshData = null;
	}

	public void SetFrom(ChunkDTO dto)
	{
		Id = dto.Id;
		Size = dto.Size;
		Dimensions = dto.Dimensions;
		VoxelCount = Dimensions.x * Dimensions.y * Dimensions.z;
		Dispose();
		Density = dto.Density;
		Material = dto.Material;
		IsDataGenerated = true;
		IsDataChanged = true;
		IsMeshGenerated = false;
		IsMeshCreated = false;
		IsCollisionBaked = false;
		IsMeshAssigned = false;
		IsDirty = true;
		VertexCount = 0;
		Mesh = null;
	}

	public void UpdateFrom(ChunkDTO dto)
	{
		Id = dto.Id;
		Size = dto.Size;
		Dimensions = dto.Dimensions;
		VoxelCount = Dimensions.x * Dimensions.y * Dimensions.z;
		DisposeAllExceptComponent();
		Density = dto.Density;
		Material = dto.Material;
		IsDataGenerated = true;
		IsDataChanged = true;
		IsMeshGenerated = false;
		IsMeshCreated = false;
		IsCollisionBaked = false;
		IsMeshAssigned = false;
		IsDirty = true;
		VertexCount = 0;
		Mesh = null;
	}

	public void Clear()
	{
		DisposeMeshData();
		IsDataGenerated = false;
		IsDataChanged = false;
		IsMeshGenerated = false;
		IsMeshCreated = false;
		IsCollisionBaked = false;
		IsMeshAssigned = false;
		IsDirty = true;
		VertexCount = 0;
		Mesh = null;
	}

	public void SetComponent(ChunkComponent chunkComponent)
	{
		Component = chunkComponent;
		if ((bool)chunkComponent)
		{
			GameObject = chunkComponent.gameObject;
			MeshFilter = chunkComponent.meshFilter;
			MeshRenderer = chunkComponent.meshRenderer;
			MeshCollider = chunkComponent.meshCollider;
			Component.name = GetChunkName(Id);
			Component.World = World;
			Component.meshRenderer.sharedMaterial = World.MaterialSet.Material;
		}
		else
		{
			GameObject = null;
			MeshFilter = null;
			MeshRenderer = null;
			MeshCollider = null;
		}
	}

	public void Dispose()
	{
		if (Density.IsCreated)
		{
			Density.Dispose();
			Density = default(NativeArray<byte>);
		}
		if (Material.IsCreated)
		{
			Material.Dispose();
			Material = default(NativeArray<byte>);
		}
		DisposeMeshData();
		if ((bool)Component)
		{
			UnityEngine.Object.Destroy(Component.gameObject);
		}
	}

	public void DisposeAllExceptComponent()
	{
		if (Density.IsCreated)
		{
			Density.Dispose();
			Density = default(NativeArray<byte>);
		}
		if (Material.IsCreated)
		{
			Material.Dispose();
			Material = default(NativeArray<byte>);
		}
		DisposeMeshData();
	}

	public void AllocateVertexData(int length)
	{
		if (VertexData.IsCreated)
		{
			if (VertexData.Length == length)
			{
				return;
			}
			NativeArrayPool<MeshVertexData>.Return(VertexData);
			VertexData = default(NativeArray<MeshVertexData>);
		}
		VertexData = NativeArrayPool<MeshVertexData>.Get(length);
	}

	public void AllocateTriangleData(int length)
	{
		if (TriangleData.IsCreated)
		{
			if (TriangleData.Length == length)
			{
				return;
			}
			NativeArrayPool<ushort>.Return(TriangleData);
			TriangleData = default(NativeArray<ushort>);
		}
		TriangleData = NativeArrayPool<ushort>.Get(length);
	}

	public void DisposeMeshData()
	{
		if (VertexData.IsCreated)
		{
			NativeArrayPool<MeshVertexData>.Return(VertexData);
			VertexData = default(NativeArray<MeshVertexData>);
		}
		if (TriangleData.IsCreated)
		{
			NativeArrayPool<ushort>.Return(TriangleData);
			TriangleData = default(NativeArray<ushort>);
		}
		if (GenericMeshData is IDisposable disposable)
		{
			disposable.Dispose();
			GenericMeshData = null;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int3 GetLocalPosition(int3 voxelPosition)
	{
		return voxelPosition - Id * Size;
	}

	public override string ToString()
	{
		return string.Format("Chunk ({0}, {1}, {2}) [{3}{4}{5}{6}{7}{8}]", Id.x, Id.y, Id.z, IsDataGenerated ? "D" : "_", IsDataChanged ? "C" : "_", IsMeshGenerated ? "G" : "_", IsMeshCreated ? "M" : "_", IsCollisionBaked ? "B" : "_", IsMeshAssigned ? "A" : "_");
	}

	public static string GetChunkName(int3 id)
	{
		return $"Chunk_{id.x}_{id.y}_{id.z}";
	}
}

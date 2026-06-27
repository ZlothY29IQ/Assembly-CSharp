using System;
using Unity.Collections;
using Unity.Mathematics;

namespace Voxels;

public struct SurfaceNetsBuffer : IDisposable
{
	public NativeList<float3> Vertices;

	public NativeList<float3> Normals;

	public NativeList<byte> Materials;

	public NativeList<int> Triangles;

	public NativeList<int3> SurfacePoints;

	public NativeList<int> SurfaceStrides;

	public NativeArray<int> StrideToIndex;

	public const int NullVertex = int.MaxValue;

	public SurfaceNetsBuffer(int vertexCap, int indexCap, int strideCount, Allocator alloc = Allocator.TempJob)
	{
		Vertices = new NativeList<float3>(vertexCap, alloc);
		Normals = new NativeList<float3>(vertexCap, alloc);
		Materials = new NativeList<byte>(vertexCap, alloc);
		Triangles = new NativeList<int>(indexCap, alloc);
		SurfacePoints = new NativeList<int3>(vertexCap, alloc);
		SurfaceStrides = new NativeList<int>(vertexCap, alloc);
		StrideToIndex = new NativeArray<int>(strideCount, alloc, NativeArrayOptions.UninitializedMemory);
		Reset(strideCount);
	}

	public void Reset(int strideCount)
	{
		Vertices.Clear();
		Normals.Clear();
		Triangles.Clear();
		SurfacePoints.Clear();
		SurfaceStrides.Clear();
		if (StrideToIndex.Length < strideCount)
		{
			StrideToIndex.Dispose();
		}
		if (!StrideToIndex.IsCreated || StrideToIndex.Length != strideCount)
		{
			StrideToIndex = new NativeArray<int>(strideCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
		}
		for (int i = 0; i < strideCount; i++)
		{
			StrideToIndex[i] = int.MaxValue;
		}
	}

	public void Dispose()
	{
		Vertices.Dispose();
		Normals.Dispose();
		Materials.Dispose();
		Triangles.Dispose();
		SurfacePoints.Dispose();
		SurfaceStrides.Dispose();
		StrideToIndex.Dispose();
	}
}

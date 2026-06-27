using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace FastSurfaceNets;

public class SurfaceNetsBuffer
{
	public readonly List<float3> Positions = new List<float3>();

	public readonly List<float3> Normals = new List<float3>();

	public readonly List<int> Indices = new List<int>();

	internal readonly List<int3> SurfacePoints = new List<int3>();

	internal readonly List<int> SurfaceStrides = new List<int>();

	internal int[] StrideToIndex = Array.Empty<int>();

	public const int NullVertex = int.MaxValue;

	internal void Reset(int arraySize)
	{
		Positions.Clear();
		Normals.Clear();
		Indices.Clear();
		SurfacePoints.Clear();
		SurfaceStrides.Clear();
		if (StrideToIndex.Length < arraySize)
		{
			Array.Resize(ref StrideToIndex, arraySize);
		}
		for (int i = 0; i < arraySize; i++)
		{
			StrideToIndex[i] = int.MaxValue;
		}
	}
}

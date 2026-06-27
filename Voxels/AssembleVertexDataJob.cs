using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Voxels;

public struct AssembleVertexDataJob : IJob
{
	private struct Key : IEquatable<Key>
	{
		public int srcIdx;

		public int4 mats;

		public bool Equals(Key other)
		{
			if (srcIdx == other.srcIdx)
			{
				return mats.Equals(other.mats);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (int)math.hash(new int4(srcIdx, mats.xyz));
		}
	}

	[NativeDisableParallelForRestriction]
	[WriteOnly]
	public NativeArray<MeshVertexData> vertexData;

	[NativeDisableParallelForRestriction]
	[WriteOnly]
	public NativeArray<ushort> triangleData;

	[ReadOnly]
	public NativeArray<float3> srcVerts;

	[ReadOnly]
	public NativeArray<byte> srcMats;

	[ReadOnly]
	public NativeArray<float3> srcNorm;

	[ReadOnly]
	public NativeArray<int> srcTris;

	public NativeCounter triangleCounter;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int3 Sort3(int a, int b, int c)
	{
		if (a > b)
		{
			int num = a;
			a = b;
			b = num;
		}
		if (b > c)
		{
			int num2 = b;
			b = c;
			c = num2;
		}
		if (a > b)
		{
			int num3 = a;
			a = b;
			b = num3;
		}
		return new int3(a, b, c);
	}

	private static int4 MakeMatSet(byte m0, byte m1, byte m2)
	{
		return new int4(m0, m1, m2, 255);
	}

	public void Execute()
	{
		NativeParallelHashMap<Key, int> map = new NativeParallelHashMap<Key, int>(srcVerts.Length * 2, Allocator.Temp);
		NativeList<MeshVertexData> vertsOut = new NativeList<MeshVertexData>(srcVerts.Length * 2, Allocator.Temp);
		NativeList<ushort> nativeList = new NativeList<ushort>(srcTris.Length, Allocator.Temp);
		int num = srcTris.Length / 3;
		for (int i = 0; i < num; i++)
		{
			int num2 = srcTris[i * 3];
			int num3 = srcTris[i * 3 + 1];
			int num4 = srcTris[i * 3 + 2];
			int4 mats = MakeMatSet(srcMats[num2], srcMats[num3], srcMats[num4]);
			nativeList.Add(GetOrCreate(num2, mats, new float4(1f, 0f, 0f, 0f), ref map, ref vertsOut));
			nativeList.Add(GetOrCreate(num3, mats, new float4(0f, 1f, 0f, 0f), ref map, ref vertsOut));
			nativeList.Add(GetOrCreate(num4, mats, new float4(0f, 0f, 1f, 0f), ref map, ref vertsOut));
		}
		triangleCounter.Count = num;
		int length = vertsOut.Length;
		int length2 = nativeList.Length;
		NativeArray<MeshVertexData>.Copy(vertsOut.AsArray(), vertexData, length);
		NativeArray<ushort>.Copy(nativeList.AsArray(), triangleData, length2);
	}

	private ushort GetOrCreate(int srcIdx, int4 mats, float4 blend, ref NativeParallelHashMap<Key, int> map, ref NativeList<MeshVertexData> vertsOut)
	{
		Key key = new Key
		{
			srcIdx = srcIdx,
			mats = mats
		};
		if (!map.TryGetValue(key, out var item))
		{
			item = vertsOut.Length;
			map.Add(key, item);
			float3 float5 = srcNorm[srcIdx];
			float3 xyz = math.normalize(math.cross((math.abs(float5.y) < 0.999f) ? new float3(0f, 1f, 0f) : new float3(1f, 0f, 0f), float5));
			vertsOut.Add(new MeshVertexData(srcVerts[srcIdx], float5, new float4(xyz, 1f), mats, blend));
		}
		return (ushort)item;
	}
}

using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Voxels;

[BurstCompile]
public struct SortChunksJob : IJob
{
	private struct SortKey(ulong val) : IComparable<SortKey>
	{
		public ulong value = val;

		public int CompareTo(SortKey other)
		{
			return value.CompareTo(other.value);
		}
	}

	[ReadOnly]
	public NativeHashSet<int3> ChunkSet;

	[ReadOnly]
	public int3 TargetPos;

	public NativeList<int3> SortedChunks;

	public void Execute()
	{
		int count = ChunkSet.Count;
		SortedChunks.ResizeUninitialized(count);
		NativeArray<int3> nativeArray = new NativeArray<int3>(count, Allocator.Temp);
		int num = 0;
		foreach (int3 item in ChunkSet)
		{
			nativeArray[num++] = item;
		}
		NativeArray<SortKey> array = new NativeArray<SortKey>(count, Allocator.Temp);
		for (int i = 0; i < count; i++)
		{
			uint num2 = (uint)math.distancesq(nativeArray[i], TargetPos);
			array[i] = new SortKey(((ulong)num2 << 32) | (uint)i);
		}
		array.Sort();
		for (int j = 0; j < count; j++)
		{
			int index = (int)(array[j].value & 0xFFFFFFFFu);
			SortedChunks[j] = nativeArray[index];
		}
		array.Dispose();
		nativeArray.Dispose();
	}
}

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Voxels;

[BurstCompile]
public struct MarchingCubesMeshingJob : IJob
{
	[ReadOnly]
	public NativeArray<byte> voxels;

	[ReadOnly]
	public NativeArray<byte> materials;

	[ReadOnly]
	public int chunkSize;

	[ReadOnly]
	public byte isoLevel;

	public NativeCounter triangleCounter;

	[NativeDisableParallelForRestriction]
	[WriteOnly]
	public NativeArray<MeshVertexData> vertexData;

	[NativeDisableParallelForRestriction]
	[WriteOnly]
	public NativeArray<ushort> triangleData;

	private int dimension;

	public void Execute()
	{
		dimension = chunkSize + 1;
		for (int i = 0; i < chunkSize; i++)
		{
			for (int j = 0; j < chunkSize; j++)
			{
				for (int k = 0; k < chunkSize; k++)
				{
					ProcessCube(i, j, k);
				}
			}
		}
	}

	private void ProcessCube(int x, int y, int z)
	{
		int3 int5 = new int3(x, y, z);
		GetMaterialValue(int5);
		NativeArray<byte> nativeArray = new NativeArray<byte>(8, Allocator.Temp);
		for (int i = 0; i < 8; i++)
		{
			int3 pos = new int3(x, y, z) + (int3)MarchingCubesLookup.CornerOffsets[i];
			nativeArray[i] = GetVoxelValue(pos);
		}
		int num = 0;
		for (int j = 0; j < 8; j++)
		{
			if (nativeArray[j] < isoLevel)
			{
				num |= 1 << j;
			}
		}
		if (num == 0 || num == 255)
		{
			nativeArray.Dispose();
			return;
		}
		NativeArray<float3> nativeArray2 = new NativeArray<float3>(12, Allocator.Temp);
		NativeArray<float> nativeArray3 = new NativeArray<float>(12, Allocator.Temp);
		for (int k = 0; k < 12; k++)
		{
			if ((MarchingCubesLookup.EdgeTable[num] & (1 << k)) != 0)
			{
				int2 int6 = MarchingCubesLookup.EdgeVertices[k];
				float3 start = new float3(x, y, z) + MarchingCubesLookup.CornerOffsets[int6.x];
				float3 end = new float3(x, y, z) + MarchingCubesLookup.CornerOffsets[int6.y];
				float t = (float)(isoLevel - nativeArray[int6.x]) / (float)(nativeArray[int6.y] - nativeArray[int6.x]);
				nativeArray2[k] = math.lerp(start, end, t);
				byte materialValue = GetMaterialValue((int5 + MarchingCubesLookup.CornerOffsets[int6.x]).ToInt3());
				byte materialValue2 = GetMaterialValue((int5 + MarchingCubesLookup.CornerOffsets[int6.y]).ToInt3());
				int num2 = math.max((int)materialValue, (int)materialValue2);
				nativeArray3[k] = num2;
			}
		}
		for (int l = 0; l < 16 && MarchingCubesLookup.TriTable[num * 16 + l] != -1; l += 3)
		{
			float3 float5 = nativeArray2[MarchingCubesLookup.TriTable[num * 16 + l]];
			float3 float6 = nativeArray2[MarchingCubesLookup.TriTable[num * 16 + l + 1]];
			float3 float7 = nativeArray2[MarchingCubesLookup.TriTable[num * 16 + l + 2]];
			float x2 = nativeArray3[MarchingCubesLookup.TriTable[num * 16 + l]];
			float y2 = nativeArray3[MarchingCubesLookup.TriTable[num * 16 + l + 1]];
			float z2 = nativeArray3[MarchingCubesLookup.TriTable[num * 16 + l + 2]];
			float4 float8 = new float4(x2, y2, z2, 0f);
			if (!float5.Equals(float6) && !float5.Equals(float7) && !float6.Equals(float7))
			{
				float3 float9 = math.normalize(math.cross(float6 - float5, float7 - float5));
				float3 xyz = math.normalize(math.cross((math.abs(float9.y) < 0.999f) ? new float3(0f, 1f, 0f) : new float3(1f, 0f, 0f), float9));
				float4 tangent = new float4(xyz, 1f);
				int num3 = triangleCounter.Increment() * 3;
				vertexData[num3] = new MeshVertexData(float5, float9, tangent, float8, new float4(1f, 0f, 0f, 0f));
				triangleData[num3] = (ushort)num3;
				vertexData[num3 + 1] = new MeshVertexData(float6, float9, tangent, float8, new float4(0f, 1f, 0f, 0f));
				triangleData[num3 + 1] = (ushort)(num3 + 1);
				vertexData[num3 + 2] = new MeshVertexData(float7, float9, tangent, float8, new float4(0f, 0f, 1f, 0f));
				triangleData[num3 + 2] = (ushort)(num3 + 2);
			}
		}
		nativeArray.Dispose();
		nativeArray2.Dispose();
		nativeArray3.Dispose();
	}

	private byte GetMaterialValue(int3 pos)
	{
		return materials[pos.x + dimension * (pos.y + pos.z * dimension)];
	}

	private byte GetVoxelValue(int3 pos)
	{
		if (pos.x < 0 || pos.y < 0 || pos.z < 0 || pos.x > chunkSize || pos.y > chunkSize || pos.z > chunkSize)
		{
			return 0;
		}
		int index = pos.x + dimension * (pos.y + pos.z * dimension);
		return voxels[index];
	}
}

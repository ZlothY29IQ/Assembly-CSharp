using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Voxels;

[BurstCompile]
public struct GenerateVoxelDataJob : IJobParallelFor
{
	public int3 chunkPosition;

	public int chunkSize;

	public int dimension;

	public float noiseScale;

	public float groundLevel;

	public float heightScale;

	public float heightCompensation;

	public int octaves;

	public float persistence;

	public int seed;

	[WriteOnly]
	public NativeArray<byte> voxels;

	[WriteOnly]
	public NativeArray<byte> materials;

	public void Execute(int index)
	{
		int num = index % dimension;
		int num2 = index / dimension % dimension;
		int num3 = index / (dimension * dimension);
		float3 float5 = new float3(chunkPosition.x * chunkSize + num, chunkPosition.y * chunkSize + num2, chunkPosition.z * chunkSize + num3);
		float3 float6 = new float3((float)seed * 1.7f, (float)seed * 2.3f, (float)seed * 3.1f);
		float3 float7 = float5 + float6;
		float num4 = noise.snoise((new float3(float5.x, 0f, float5.z) + float6) * noiseScale) + (groundLevel - float5.y) / heightScale;
		num4 = math.clamp(num4 * heightCompensation, -1f, 1f);
		float num5 = noiseScale;
		float num6 = 1f;
		for (int i = 0; i < octaves; i++)
		{
			num5 *= 2f;
			num6 *= persistence;
			num4 += noise.snoise(float7 * num5) * num6;
		}
		if (noise.snoise(float7 * 0.05f) > 0.6f && num4 >= 0f)
		{
			materials[index] = 1;
		}
		voxels[index] = num4.ToByte();
	}
}

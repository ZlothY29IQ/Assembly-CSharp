using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Voxels;

namespace FastSurfaceNets;

internal struct FillChunkJob : IJobParallelFor
{
	[WriteOnly]
	public NativeArray<byte> sdf;

	[ReadOnly]
	public int3 shape;

	[ReadOnly]
	public int3 chunkPosition;

	[ReadOnly]
	public int3 shapeMin;

	[ReadOnly]
	public int3 shapeMax;

	[ReadOnly]
	public float noiseScale;

	[ReadOnly]
	public float heightScale;

	[ReadOnly]
	public int3 min;

	[ReadOnly]
	public int3 max;

	[ReadOnly]
	public int strideY;

	[ReadOnly]
	public int strideZ;

	public void Execute(int index)
	{
		int z = index / strideZ;
		int y = index % strideZ / strideY;
		int x = index % strideY;
		float3 float5 = (chunkPosition + new int3(x, y, z)).ToFloat3();
		float value = noise.snoise(float5 * noiseScale) - float5.y / heightScale;
		sdf[index] = value.ToByte();
	}
}

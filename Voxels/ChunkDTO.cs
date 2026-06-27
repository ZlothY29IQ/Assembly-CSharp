using Unity.Collections;
using Unity.Mathematics;

namespace Voxels;

public struct ChunkDTO
{
	public int WorldId;

	public int3 Id;

	public int3 Size;

	public int3 Dimensions;

	public NativeArray<byte> Density;

	public NativeArray<byte> Material;

	public bool IsValid
	{
		get
		{
			if (!Size.Equals(int3.zero) && Density.IsCreated)
			{
				return Material.IsCreated;
			}
			return false;
		}
	}

	public ChunkDTO(Chunk chunk)
	{
		WorldId = chunk.World.Id;
		Id = chunk.Id;
		Size = chunk.Size;
		Dimensions = chunk.Dimensions;
		Density = chunk.Density;
		Material = chunk.Material;
	}
}

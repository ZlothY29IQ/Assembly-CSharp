namespace Voxels;

public enum ChunkState
{
	UNINITIALIZED,
	Created,
	VoxelDataGenerated,
	MeshDataGenerated,
	MeshCreated,
	CollisionBaked,
	MeshAssigned
}

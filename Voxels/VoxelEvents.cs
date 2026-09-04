using UnityEngine;

namespace Voxels;

public static class VoxelEvents
{
	public delegate void ResourcesMinedAuthorityDelegate(NetPlayer player, VoxelWorld world, int[] amounts);

	public delegate void ResourcesMinedDelegate(VoxelWorld world, Vector3 hitPoint, Vector3 hitNormal, int[] amounts);

	public static event ResourcesMinedAuthorityDelegate OnResourcesMinedAuthority;

	public static event ResourcesMinedDelegate OnResourcesMined;

	public static void HandleResourceMinedAuthority(NetPlayer player, VoxelWorld world, int[] amounts)
	{
		VoxelEvents.OnResourcesMinedAuthority?.Invoke(player, world, amounts);
	}

	public static void HandleResourceMined(VoxelWorld world, Vector3 hitPoint, Vector3 hitNormal, int[] amounts)
	{
		world.MaterialSet.PlayDigFX(hitPoint, hitNormal, amounts);
		VoxelEvents.OnResourcesMined?.Invoke(world, hitPoint, hitNormal, amounts);
	}
}

using Unity.Burst;
using Unity.Jobs;
using UnityEngine;

namespace Voxels;

[BurstCompile]
public struct CollisionJob : IJob
{
	public const MeshColliderCookingOptions CookingOptions = MeshColliderCookingOptions.CookForFasterSimulation | MeshColliderCookingOptions.EnableMeshCleaning | MeshColliderCookingOptions.WeldColocatedVertices | MeshColliderCookingOptions.UseFastMidphase;

	public EntityId MeshId;

	public void Execute()
	{
		Physics.BakeMesh(MeshId, convex: false, MeshColliderCookingOptions.CookForFasterSimulation | MeshColliderCookingOptions.EnableMeshCleaning | MeshColliderCookingOptions.WeldColocatedVertices | MeshColliderCookingOptions.UseFastMidphase);
	}
}

using System;
using BoingKit;
using FastSurfaceNets;
using GorillaLocomotion.Gameplay;
using GorillaTag.Rendering;
using GorillaTagScripts;
using GorillaTagScripts.Builder;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using Voxels;

[Unity.Jobs.DOTSCompilerGenerated]
internal class __JobReflectionRegistrationOutput__1221673671587648887
{
	public static void CreateJobReflectionData()
	{
		try
		{
			IJobParallelForExtensions.EarlyJobInit<MeshExtensions.FaceNormalJob>();
			IJobExtensions.EarlyJobInit<MeshExtensions.SplitJob>();
			IJobParallelForExtensions.EarlyJobInit<MeshExtensions.TriNormalJob>();
			IJobParallelForExtensions.EarlyJobInit<MeshExtensions.BuildAdjJob>();
			IJobParallelForExtensions.EarlyJobInit<MeshExtensions.VertexNormalJob>();
			IJobParallelForExtensions.EarlyJobInit<HandEffectsTriggerRegistry.HandEffectsJob>();
			IJobParallelForTransformExtensions.EarlyJobInit<BuilderRenderer.SetupInstanceDataForMesh>();
			IJobParallelForTransformExtensions.EarlyJobInit<BuilderRenderer.SetupInstanceDataForMeshStatic>();
			IJobParallelForExtensions.EarlyJobInit<GorillaIKMgr.IKJob>();
			IJobParallelForTransformExtensions.EarlyJobInit<GorillaIKMgr.IKTransformJob>();
			IJobExtensions.EarlyJobInit<DayNightCycle.LerpBakedLightingJob>();
			IJobParallelForTransformExtensions.EarlyJobInit<VRRigJobManager.VRRigTransformJob>();
			IJobParallelForExtensions.EarlyJobInit<BuilderFindPotentialSnaps>();
			IJobParallelForTransformExtensions.EarlyJobInit<FindNearbyPiecesJob>();
			IJobParallelForTransformExtensions.EarlyJobInit<BuilderConveyorManager.EvaluateSplineJob>();
			IJobExtensions.EarlyJobInit<SolveRopeJob>();
			IJobExtensions.EarlyJobInit<VectorizedSolveRopeJob>();
			IJobExtensions.EarlyJobInit<EdMeshCombinerPrefab.CopyMeshJob>();
			IJobParallelForExtensions.EarlyJobInit<FillChunkJob>();
			IJobExtensions.EarlyJobInit<CollisionJob>();
			IJobParallelForExtensions.EarlyJobInit<GenerateVoxelDataJob>();
			IJobExtensions.EarlyJobInit<MarchingCubesMeshingJob>();
			IJobExtensions.EarlyJobInit<SortChunksJob>();
			IJobParallelForExtensions.EarlyJobInit<MeshUtilities.FaceNormalJob>();
			IJobExtensions.EarlyJobInit<MeshUtilities.SplitJob>();
			IJobExtensions.EarlyJobInit<MeshUtilities.SplitVoxelMeshJob>();
			IJobParallelForExtensions.EarlyJobInit<MeshUtilities.TriNormalJob>();
			IJobParallelForExtensions.EarlyJobInit<MeshUtilities.BuildAdjJob>();
			IJobParallelForExtensions.EarlyJobInit<MeshUtilities.VertexNormalJob>();
			IJobExtensions.EarlyJobInit<SurfaceNetsJob>();
			IJobExtensions.EarlyJobInit<AssembleVertexDataJob>();
			IJobParallelForExtensions.EarlyJobInit<BoingWorkAsynchronous.BehaviorJob>();
			IJobParallelForExtensions.EarlyJobInit<BoingWorkAsynchronous.ReactorJob>();
		}
		catch (Exception ex)
		{
			EarlyInitHelpers.JobReflectionDataCreationFailed(ex);
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
	public static void EarlyInit()
	{
		CreateJobReflectionData();
	}
}

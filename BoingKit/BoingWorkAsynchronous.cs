using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace BoingKit;

public static class BoingWorkAsynchronous
{
	private struct BehaviorJob : IJobParallelFor
	{
		public NativeArray<BoingWork.Params> Params;

		public NativeArray<BoingWork.Output> Output;

		public float DeltaTime;

		public float FixedDeltaTime;

		public void Execute(int index)
		{
			BoingWork.Params obj = Params[index];
			if (obj.Bits.IsBitSet(9))
			{
				obj.Execute(FixedDeltaTime);
			}
			else
			{
				obj.Execute(DeltaTime);
			}
			Output[index] = new BoingWork.Output(obj.InstanceID, ref obj.Instance.PositionSpring, ref obj.Instance.RotationSpring, ref obj.Instance.ScaleSpring);
		}
	}

	private struct ReactorJob : IJobParallelFor
	{
		[ReadOnly]
		public NativeArray<BoingEffector.Params> Effectors;

		public NativeArray<BoingWork.Params> Params;

		public NativeArray<BoingWork.Output> Output;

		public float DeltaTime;

		public float FixedDeltaTime;

		public void Execute(int index)
		{
			BoingWork.Params obj = Params[index];
			int i = 0;
			for (int length = Effectors.Length; i < length; i++)
			{
				BoingEffector.Params effector = Effectors[i];
				obj.AccumulateTarget(ref effector, DeltaTime);
			}
			obj.EndAccumulateTargets();
			if (obj.Bits.IsBitSet(9))
			{
				obj.Execute(FixedDeltaTime);
			}
			else
			{
				obj.Execute(BoingManager.DeltaTime);
			}
			Output[index] = new BoingWork.Output(obj.InstanceID, ref obj.Instance.PositionSpring, ref obj.Instance.RotationSpring, ref obj.Instance.ScaleSpring);
		}
	}

	private static bool s_behaviorJobNeedsGather;

	private static JobHandle s_hBehaviorJob;

	private static NativeArray<BoingWork.Params> s_aBehaviorParams;

	private static NativeArray<BoingWork.Output> s_aBehaviorOutput;

	private static bool s_reactorJobNeedsGather;

	private static JobHandle s_hReactorJob;

	private static NativeArray<BoingEffector.Params> s_aEffectors;

	private static NativeArray<BoingWork.Params> s_aReactorExecParams;

	private static NativeArray<BoingWork.Output> s_aReactorExecOutput;

	internal static void PostUnregisterBehaviorCleanUp()
	{
		if (s_behaviorJobNeedsGather)
		{
			s_hBehaviorJob.Complete();
			s_aBehaviorParams.Dispose();
			s_aBehaviorOutput.Dispose();
			s_behaviorJobNeedsGather = false;
		}
	}

	internal static void PostUnregisterEffectorReactorCleanUp()
	{
		if (s_reactorJobNeedsGather)
		{
			s_hReactorJob.Complete();
			s_aEffectors.Dispose();
			s_aReactorExecParams.Dispose();
			s_aReactorExecOutput.Dispose();
			s_reactorJobNeedsGather = false;
		}
	}

	internal static void ExecuteBehaviors(Dictionary<int, BoingBehavior> behaviorMap, BoingManager.UpdateMode updateMode)
	{
		int num = 0;
		s_aBehaviorParams = new NativeArray<BoingWork.Params>(behaviorMap.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
		s_aBehaviorOutput = new NativeArray<BoingWork.Output>(behaviorMap.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
		foreach (KeyValuePair<int, BoingBehavior> item in behaviorMap)
		{
			BoingBehavior value = item.Value;
			if (value.UpdateMode == updateMode)
			{
				value.PrepareExecute();
				s_aBehaviorParams[num++] = value.Params;
			}
		}
		if (num > 0)
		{
			s_hBehaviorJob = new BehaviorJob
			{
				Params = s_aBehaviorParams,
				Output = s_aBehaviorOutput,
				DeltaTime = BoingManager.DeltaTime,
				FixedDeltaTime = BoingManager.FixedDeltaTime
			}.Schedule(innerloopBatchCount: (int)Mathf.Ceil((float)num / (float)Environment.ProcessorCount), arrayLength: num);
			JobHandle.ScheduleBatchedJobs();
		}
		s_behaviorJobNeedsGather = true;
		if (!s_behaviorJobNeedsGather)
		{
			return;
		}
		if (num > 0)
		{
			s_hBehaviorJob.Complete();
			for (int i = 0; i < num; i++)
			{
				s_aBehaviorOutput[i].GatherOutput(behaviorMap, updateMode);
			}
		}
		s_aBehaviorParams.Dispose();
		s_aBehaviorOutput.Dispose();
		s_behaviorJobNeedsGather = false;
	}

	internal static void ExecuteReactors(Dictionary<int, BoingEffector> effectorMap, Dictionary<int, BoingReactor> reactorMap, Dictionary<int, BoingReactorField> fieldMap, Dictionary<int, BoingReactorFieldCPUSampler> cpuSamplerMap, BoingManager.UpdateMode updateMode)
	{
		int num = 0;
		s_aEffectors = new NativeArray<BoingEffector.Params>(effectorMap.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
		s_aReactorExecParams = new NativeArray<BoingWork.Params>(reactorMap.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
		s_aReactorExecOutput = new NativeArray<BoingWork.Output>(reactorMap.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
		foreach (KeyValuePair<int, BoingReactor> item in reactorMap)
		{
			BoingReactor value = item.Value;
			if (value.UpdateMode == updateMode)
			{
				value.PrepareExecute();
				s_aReactorExecParams[num++] = value.Params;
			}
		}
		if (num > 0)
		{
			int num2 = 0;
			BoingEffector.Params value2 = default(BoingEffector.Params);
			foreach (KeyValuePair<int, BoingEffector> item2 in effectorMap)
			{
				_ = item2.Value;
				value2.Fill(item2.Value);
				s_aEffectors[num2++] = value2;
			}
		}
		if (num > 0)
		{
			s_hReactorJob = IJobParallelForExtensions.Schedule(new ReactorJob
			{
				Effectors = s_aEffectors,
				Params = s_aReactorExecParams,
				Output = s_aReactorExecOutput,
				DeltaTime = BoingManager.DeltaTime,
				FixedDeltaTime = BoingManager.FixedDeltaTime
			}, num, 32);
			JobHandle.ScheduleBatchedJobs();
		}
		foreach (KeyValuePair<int, BoingReactorField> item3 in fieldMap)
		{
			BoingReactorField value3 = item3.Value;
			if (value3.HardwareMode == BoingReactorField.HardwareModeEnum.CPU)
			{
				value3.ExecuteCpu(BoingManager.DeltaTime);
			}
		}
		foreach (KeyValuePair<int, BoingReactorFieldCPUSampler> item4 in cpuSamplerMap)
		{
			_ = item4.Value;
		}
		s_reactorJobNeedsGather = true;
		if (!s_reactorJobNeedsGather)
		{
			return;
		}
		if (num > 0)
		{
			s_hReactorJob.Complete();
			for (int i = 0; i < num; i++)
			{
				s_aReactorExecOutput[i].GatherOutput(reactorMap, updateMode);
			}
		}
		s_aEffectors.Dispose();
		s_aReactorExecParams.Dispose();
		s_aReactorExecOutput.Dispose();
		s_reactorJobNeedsGather = false;
	}

	internal static void ExecuteBones(BoingEffector.Params[] aEffectorParams, Dictionary<int, BoingBones> bonesMap, BoingManager.UpdateMode updateMode)
	{
		float deltaTime = BoingManager.DeltaTime;
		foreach (KeyValuePair<int, BoingBones> item in bonesMap)
		{
			BoingBones value = item.Value;
			if (value.UpdateMode != updateMode)
			{
				continue;
			}
			value.PrepareExecute();
			if (aEffectorParams != null)
			{
				for (int i = 0; i < aEffectorParams.Length; i++)
				{
					value.AccumulateTarget(ref aEffectorParams[i], deltaTime);
				}
			}
			value.EndAccumulateTargets();
			switch (value.UpdateMode)
			{
			case BoingManager.UpdateMode.EarlyUpdate:
			case BoingManager.UpdateMode.LateUpdate:
				value.Params.Execute(value, BoingManager.DeltaTime);
				break;
			case BoingManager.UpdateMode.FixedUpdate:
				value.Params.Execute(value, BoingManager.FixedDeltaTime);
				break;
			}
		}
	}

	internal static void PullBonesResults(BoingEffector.Params[] aEffectorParams, Dictionary<int, BoingBones> bonesMap, BoingManager.UpdateMode updateMode)
	{
		foreach (KeyValuePair<int, BoingBones> item in bonesMap)
		{
			BoingBones value = item.Value;
			if (value.UpdateMode == updateMode)
			{
				value.Params.PullResults(value);
			}
		}
	}
}

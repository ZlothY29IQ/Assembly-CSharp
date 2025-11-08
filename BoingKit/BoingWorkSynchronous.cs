using System.Collections.Generic;
using UnityEngine;

namespace BoingKit;

internal static class BoingWorkSynchronous
{
	internal static void ExecuteBehaviors(Dictionary<int, BoingBehavior> behaviorMap, BoingManager.UpdateMode updateMode)
	{
		float deltaTime = Time.deltaTime;
		foreach (KeyValuePair<int, BoingBehavior> item in behaviorMap)
		{
			BoingBehavior value = item.Value;
			if (value.UpdateMode == updateMode)
			{
				value.PrepareExecute();
				switch (value.UpdateMode)
				{
				case BoingManager.UpdateMode.EarlyUpdate:
				case BoingManager.UpdateMode.LateUpdate:
					value.Execute(deltaTime);
					break;
				case BoingManager.UpdateMode.FixedUpdate:
					value.Execute(BoingManager.FixedDeltaTime);
					break;
				}
			}
		}
	}

	internal static void ExecuteReactors(BoingEffector.Params[] aEffectorParams, Dictionary<int, BoingReactor> reactorMap, Dictionary<int, BoingReactorField> fieldMap, Dictionary<int, BoingReactorFieldCPUSampler> cpuSamplerMap, BoingManager.UpdateMode updateMode)
	{
		float deltaTime = BoingManager.DeltaTime;
		foreach (KeyValuePair<int, BoingReactor> item in reactorMap)
		{
			BoingReactor value = item.Value;
			if (value.UpdateMode != updateMode)
			{
				continue;
			}
			value.PrepareExecute();
			if (aEffectorParams != null)
			{
				for (int i = 0; i < aEffectorParams.Length; i++)
				{
					value.Params.AccumulateTarget(ref aEffectorParams[i], deltaTime);
				}
			}
			value.Params.EndAccumulateTargets();
			switch (value.UpdateMode)
			{
			case BoingManager.UpdateMode.EarlyUpdate:
			case BoingManager.UpdateMode.LateUpdate:
				value.Execute(deltaTime);
				break;
			case BoingManager.UpdateMode.FixedUpdate:
				value.Execute(BoingManager.FixedDeltaTime);
				break;
			}
		}
		foreach (KeyValuePair<int, BoingReactorField> item2 in fieldMap)
		{
			BoingReactorField value2 = item2.Value;
			if (value2.HardwareMode == BoingReactorField.HardwareModeEnum.CPU)
			{
				value2.ExecuteCpu(deltaTime);
			}
		}
		foreach (KeyValuePair<int, BoingReactorFieldCPUSampler> item3 in cpuSamplerMap)
		{
			_ = item3.Value;
		}
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

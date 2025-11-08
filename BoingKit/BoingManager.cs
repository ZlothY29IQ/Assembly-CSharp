using System.Collections.Generic;
using UnityEngine;

namespace BoingKit;

public static class BoingManager
{
	public enum UpdateMode
	{
		FixedUpdate,
		EarlyUpdate,
		LateUpdate
	}

	public enum TranslationLockSpace
	{
		Global,
		Local
	}

	public delegate void BehaviorRegisterDelegate(BoingBehavior behavior);

	public delegate void BehaviorUnregisterDelegate(BoingBehavior behavior);

	public delegate void EffectorRegisterDelegate(BoingEffector effector);

	public delegate void EffectorUnregisterDelegate(BoingEffector effector);

	public delegate void ReactorRegisterDelegate(BoingReactor reactor);

	public delegate void ReactorUnregisterDelegate(BoingReactor reactor);

	public delegate void ReactorFieldRegisterDelegate(BoingReactorField field);

	public delegate void ReactorFieldUnregisterDelegate(BoingReactorField field);

	public delegate void ReactorFieldCPUSamplerRegisterDelegate(BoingReactorFieldCPUSampler sampler);

	public delegate void ReactorFieldCPUSamplerUnregisterDelegate(BoingReactorFieldCPUSampler sampler);

	public delegate void ReactorFieldGPUSamplerRegisterDelegate(BoingReactorFieldGPUSampler sampler);

	public delegate void ReactorFieldGPUSamplerUnregisterDelegate(BoingReactorFieldGPUSampler sampler);

	public delegate void BonesRegisterDelegate(BoingBones bones);

	public delegate void BonesUnregisterDelegate(BoingBones bones);

	public static BehaviorRegisterDelegate OnBehaviorRegister;

	public static BehaviorUnregisterDelegate OnBehaviorUnregister;

	public static EffectorRegisterDelegate OnEffectorRegister;

	public static EffectorUnregisterDelegate OnEffectorUnregister;

	public static ReactorRegisterDelegate OnReactorRegister;

	public static ReactorUnregisterDelegate OnReactorUnregister;

	public static ReactorFieldRegisterDelegate OnReactorFieldRegister;

	public static ReactorFieldUnregisterDelegate OnReactorFieldUnregister;

	public static ReactorFieldCPUSamplerRegisterDelegate OnReactorFieldCPUSamplerRegister;

	public static ReactorFieldCPUSamplerUnregisterDelegate OnReactorFieldCPUSamplerUnregister;

	public static ReactorFieldGPUSamplerRegisterDelegate OnReactorFieldGPUSamplerRegister;

	public static ReactorFieldGPUSamplerUnregisterDelegate OnFieldGPUSamplerUnregister;

	public static BonesRegisterDelegate OnBonesRegister;

	public static BonesUnregisterDelegate OnBonesUnregister;

	private static float s_deltaTime = 0f;

	private static Dictionary<int, BoingBehavior> s_behaviorMap = new Dictionary<int, BoingBehavior>();

	private static Dictionary<int, BoingEffector> s_effectorMap = new Dictionary<int, BoingEffector>();

	private static Dictionary<int, BoingReactor> s_reactorMap = new Dictionary<int, BoingReactor>();

	private static Dictionary<int, BoingReactorField> s_fieldMap = new Dictionary<int, BoingReactorField>();

	private static Dictionary<int, BoingReactorFieldCPUSampler> s_cpuSamplerMap = new Dictionary<int, BoingReactorFieldCPUSampler>();

	private static Dictionary<int, BoingReactorFieldGPUSampler> s_gpuSamplerMap = new Dictionary<int, BoingReactorFieldGPUSampler>();

	private static Dictionary<int, BoingBones> s_bonesMap = new Dictionary<int, BoingBones>();

	private static readonly int kEffectorParamsIncrement = 16;

	private static List<BoingEffector.Params> s_effectorParamsList = new List<BoingEffector.Params>(kEffectorParamsIncrement);

	private static BoingEffector.Params[] s_aEffectorParams;

	private static ComputeBuffer s_effectorParamsBuffer;

	private static Dictionary<int, int> s_effectorParamsIndexMap = new Dictionary<int, int>();

	internal static readonly bool UseAsynchronousJobs = true;

	internal static GameObject s_managerGo;

	public static IEnumerable<BoingBehavior> Behaviors => s_behaviorMap.Values;

	public static IEnumerable<BoingReactor> Reactors => s_reactorMap.Values;

	public static IEnumerable<BoingEffector> Effectors => s_effectorMap.Values;

	public static IEnumerable<BoingReactorField> ReactorFields => s_fieldMap.Values;

	public static IEnumerable<BoingReactorFieldCPUSampler> ReactorFieldCPUSamlers => s_cpuSamplerMap.Values;

	public static IEnumerable<BoingReactorFieldGPUSampler> ReactorFieldGPUSampler => s_gpuSamplerMap.Values;

	public static float DeltaTime => s_deltaTime;

	public static float FixedDeltaTime => Time.fixedDeltaTime;

	internal static int NumBehaviors => s_behaviorMap.Count;

	internal static int NumEffectors => s_effectorMap.Count;

	internal static int NumReactors => s_reactorMap.Count;

	internal static int NumFields => s_fieldMap.Count;

	internal static int NumCPUFieldSamplers => s_cpuSamplerMap.Count;

	internal static int NumGPUFieldSamplers => s_gpuSamplerMap.Count;

	internal static SphereCollider SharedSphereCollider
	{
		get
		{
			if (s_managerGo == null)
			{
				return null;
			}
			return s_managerGo.GetComponent<SphereCollider>();
		}
	}

	private static void ValidateManager()
	{
		if (!(s_managerGo != null))
		{
			s_managerGo = new GameObject("Boing Kit manager (don't delete)");
			s_managerGo.AddComponent<BoingManagerPreUpdatePump>();
			s_managerGo.AddComponent<BoingManagerPostUpdatePump>();
			Object.DontDestroyOnLoad(s_managerGo);
			s_managerGo.AddComponent<SphereCollider>().enabled = false;
		}
	}

	internal static void Register(BoingBehavior behavior)
	{
		PreRegisterBehavior();
		s_behaviorMap.Add(behavior.GetInstanceID(), behavior);
		if (OnBehaviorRegister != null)
		{
			OnBehaviorRegister(behavior);
		}
	}

	internal static void Unregister(BoingBehavior behavior)
	{
		if (OnBehaviorUnregister != null)
		{
			OnBehaviorUnregister(behavior);
		}
		s_behaviorMap.Remove(behavior.GetInstanceID());
		PostUnregisterBehavior();
	}

	internal static void Register(BoingEffector effector)
	{
		PreRegisterEffectorReactor();
		s_effectorMap.Add(effector.GetInstanceID(), effector);
		if (OnEffectorRegister != null)
		{
			OnEffectorRegister(effector);
		}
	}

	internal static void Unregister(BoingEffector effector)
	{
		if (OnEffectorUnregister != null)
		{
			OnEffectorUnregister(effector);
		}
		s_effectorMap.Remove(effector.GetInstanceID());
		PostUnregisterEffectorReactor();
	}

	internal static void Register(BoingReactor reactor)
	{
		PreRegisterEffectorReactor();
		s_reactorMap.Add(reactor.GetInstanceID(), reactor);
		if (OnReactorRegister != null)
		{
			OnReactorRegister(reactor);
		}
	}

	internal static void Unregister(BoingReactor reactor)
	{
		if (OnReactorUnregister != null)
		{
			OnReactorUnregister(reactor);
		}
		s_reactorMap.Remove(reactor.GetInstanceID());
		PostUnregisterEffectorReactor();
	}

	internal static void Register(BoingReactorField field)
	{
		PreRegisterEffectorReactor();
		s_fieldMap.Add(field.GetInstanceID(), field);
		if (OnReactorFieldRegister != null)
		{
			OnReactorFieldRegister(field);
		}
	}

	internal static void Unregister(BoingReactorField field)
	{
		if (OnReactorFieldUnregister != null)
		{
			OnReactorFieldUnregister(field);
		}
		s_fieldMap.Remove(field.GetInstanceID());
		PostUnregisterEffectorReactor();
	}

	internal static void Register(BoingReactorFieldCPUSampler sampler)
	{
		PreRegisterEffectorReactor();
		s_cpuSamplerMap.Add(sampler.GetInstanceID(), sampler);
		if (OnReactorFieldCPUSamplerRegister != null)
		{
			OnReactorFieldCPUSamplerUnregister(sampler);
		}
	}

	internal static void Unregister(BoingReactorFieldCPUSampler sampler)
	{
		if (OnReactorFieldCPUSamplerUnregister != null)
		{
			OnReactorFieldCPUSamplerUnregister(sampler);
		}
		s_cpuSamplerMap.Remove(sampler.GetInstanceID());
		PostUnregisterEffectorReactor();
	}

	internal static void Register(BoingReactorFieldGPUSampler sampler)
	{
		PreRegisterEffectorReactor();
		s_gpuSamplerMap.Add(sampler.GetInstanceID(), sampler);
		if (OnReactorFieldGPUSamplerRegister != null)
		{
			OnReactorFieldGPUSamplerRegister(sampler);
		}
	}

	internal static void Unregister(BoingReactorFieldGPUSampler sampler)
	{
		if (OnFieldGPUSamplerUnregister != null)
		{
			OnFieldGPUSamplerUnregister(sampler);
		}
		s_gpuSamplerMap.Remove(sampler.GetInstanceID());
		PostUnregisterEffectorReactor();
	}

	internal static void Register(BoingBones bones)
	{
		PreRegisterBones();
		s_bonesMap.Add(bones.GetInstanceID(), bones);
		if (OnBonesRegister != null)
		{
			OnBonesRegister(bones);
		}
	}

	internal static void Unregister(BoingBones bones)
	{
		if (OnBonesUnregister != null)
		{
			OnBonesUnregister(bones);
		}
		s_bonesMap.Remove(bones.GetInstanceID());
		PostUnregisterBones();
	}

	private static void PreRegisterBehavior()
	{
		ValidateManager();
	}

	private static void PostUnregisterBehavior()
	{
		if (s_behaviorMap.Count <= 0)
		{
			BoingWorkAsynchronous.PostUnregisterBehaviorCleanUp();
		}
	}

	private static void PreRegisterEffectorReactor()
	{
		ValidateManager();
		if (s_effectorParamsBuffer == null)
		{
			s_effectorParamsList = new List<BoingEffector.Params>(kEffectorParamsIncrement);
			s_effectorParamsBuffer = new ComputeBuffer(s_effectorParamsList.Capacity, BoingEffector.Params.Stride);
		}
		if (s_effectorMap.Count >= s_effectorParamsList.Capacity)
		{
			s_effectorParamsList.Capacity += kEffectorParamsIncrement;
			s_effectorParamsBuffer.Dispose();
			s_effectorParamsBuffer = new ComputeBuffer(s_effectorParamsList.Capacity, BoingEffector.Params.Stride);
		}
	}

	private static void PostUnregisterEffectorReactor()
	{
		if (s_effectorMap.Count <= 0 && s_reactorMap.Count <= 0 && s_fieldMap.Count <= 0 && s_cpuSamplerMap.Count <= 0 && s_gpuSamplerMap.Count <= 0)
		{
			s_effectorParamsList = null;
			s_effectorParamsBuffer.Dispose();
			s_effectorParamsBuffer = null;
			BoingWorkAsynchronous.PostUnregisterEffectorReactorCleanUp();
		}
	}

	private static void PreRegisterBones()
	{
		ValidateManager();
	}

	private static void PostUnregisterBones()
	{
	}

	internal static void Execute(UpdateMode updateMode)
	{
		if (updateMode == UpdateMode.EarlyUpdate)
		{
			s_deltaTime = Time.deltaTime;
		}
		RefreshEffectorParams();
		ExecuteBones(updateMode);
		ExecuteBehaviors(updateMode);
		ExecuteReactors(updateMode);
	}

	internal static void ExecuteBehaviors(UpdateMode updateMode)
	{
		if (s_behaviorMap.Count == 0)
		{
			return;
		}
		foreach (KeyValuePair<int, BoingBehavior> item in s_behaviorMap)
		{
			BoingBehavior value = item.Value;
			if (!value.InitRebooted)
			{
				value.Reboot();
				value.InitRebooted = true;
			}
		}
		if (UseAsynchronousJobs)
		{
			BoingWorkAsynchronous.ExecuteBehaviors(s_behaviorMap, updateMode);
		}
		else
		{
			BoingWorkSynchronous.ExecuteBehaviors(s_behaviorMap, updateMode);
		}
	}

	internal static void PullBehaviorResults(UpdateMode updateMode)
	{
		foreach (KeyValuePair<int, BoingBehavior> item in s_behaviorMap)
		{
			if (item.Value.UpdateMode == updateMode)
			{
				item.Value.PullResults();
			}
		}
	}

	internal static void RestoreBehaviors()
	{
		foreach (KeyValuePair<int, BoingBehavior> item in s_behaviorMap)
		{
			item.Value.Restore();
		}
	}

	internal static void RefreshEffectorParams()
	{
		if (s_effectorParamsList == null)
		{
			return;
		}
		s_effectorParamsIndexMap.Clear();
		s_effectorParamsList.Clear();
		foreach (KeyValuePair<int, BoingEffector> item in s_effectorMap)
		{
			BoingEffector value = item.Value;
			s_effectorParamsIndexMap.Add(value.GetInstanceID(), s_effectorParamsList.Count);
			s_effectorParamsList.Add(new BoingEffector.Params(value));
		}
		if (s_aEffectorParams == null || s_aEffectorParams.Length != s_effectorParamsList.Count)
		{
			s_aEffectorParams = s_effectorParamsList.ToArray();
		}
		else
		{
			s_effectorParamsList.CopyTo(s_aEffectorParams);
		}
	}

	internal static void ExecuteReactors(UpdateMode updateMode)
	{
		if (s_effectorMap.Count == 0 && s_reactorMap.Count == 0 && s_fieldMap.Count == 0 && s_cpuSamplerMap.Count == 0)
		{
			return;
		}
		foreach (KeyValuePair<int, BoingReactor> item in s_reactorMap)
		{
			BoingReactor value = item.Value;
			if (!value.InitRebooted)
			{
				value.Reboot();
				value.InitRebooted = true;
			}
		}
		if (UseAsynchronousJobs)
		{
			BoingWorkAsynchronous.ExecuteReactors(s_effectorMap, s_reactorMap, s_fieldMap, s_cpuSamplerMap, updateMode);
		}
		else
		{
			BoingWorkSynchronous.ExecuteReactors(s_aEffectorParams, s_reactorMap, s_fieldMap, s_cpuSamplerMap, updateMode);
		}
	}

	internal static void PullReactorResults(UpdateMode updateMode)
	{
		foreach (KeyValuePair<int, BoingReactor> item in s_reactorMap)
		{
			if (item.Value.UpdateMode == updateMode)
			{
				item.Value.PullResults();
			}
		}
		foreach (KeyValuePair<int, BoingReactorFieldCPUSampler> item2 in s_cpuSamplerMap)
		{
			if (item2.Value.UpdateMode == updateMode)
			{
				item2.Value.SampleFromField();
			}
		}
	}

	internal static void RestoreReactors()
	{
		foreach (KeyValuePair<int, BoingReactor> item in s_reactorMap)
		{
			item.Value.Restore();
		}
		foreach (KeyValuePair<int, BoingReactorFieldCPUSampler> item2 in s_cpuSamplerMap)
		{
			item2.Value.Restore();
		}
	}

	internal static void DispatchReactorFieldCompute()
	{
		if (s_effectorParamsBuffer == null)
		{
			return;
		}
		s_effectorParamsBuffer.SetData(s_aEffectorParams);
		float deltaTime = Time.deltaTime;
		foreach (KeyValuePair<int, BoingReactorField> item in s_fieldMap)
		{
			BoingReactorField value = item.Value;
			if (value.HardwareMode == BoingReactorField.HardwareModeEnum.GPU)
			{
				value.ExecuteGpu(deltaTime, s_effectorParamsBuffer, s_effectorParamsIndexMap);
			}
		}
	}

	internal static void ExecuteBones(UpdateMode updateMode)
	{
		if (s_bonesMap.Count == 0)
		{
			return;
		}
		foreach (KeyValuePair<int, BoingBones> item in s_bonesMap)
		{
			BoingBones value = item.Value;
			if (!value.InitRebooted)
			{
				value.Reboot();
				value.InitRebooted = true;
			}
		}
		if (UseAsynchronousJobs)
		{
			BoingWorkAsynchronous.ExecuteBones(s_aEffectorParams, s_bonesMap, updateMode);
		}
		else
		{
			BoingWorkSynchronous.ExecuteBones(s_aEffectorParams, s_bonesMap, updateMode);
		}
	}

	internal static void PullBonesResults(UpdateMode updateMode)
	{
		if (s_bonesMap.Count != 0)
		{
			if (UseAsynchronousJobs)
			{
				BoingWorkAsynchronous.PullBonesResults(s_aEffectorParams, s_bonesMap, updateMode);
			}
			else
			{
				BoingWorkSynchronous.PullBonesResults(s_aEffectorParams, s_bonesMap, updateMode);
			}
		}
	}

	internal static void RestoreBones()
	{
		foreach (KeyValuePair<int, BoingBones> item in s_bonesMap)
		{
			item.Value.Restore();
		}
	}
}

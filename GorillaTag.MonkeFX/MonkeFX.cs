using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GorillaExtensions;
using UnityEngine;

namespace GorillaTag.MonkeFX;

public class MonkeFX : ITickSystemPost
{
	private struct ElementsRange
	{
		public int min;

		public int max;
	}

	private static readonly string[] _boneNames = new string[3] { "body", "hand.L", "hand.R" };

	private static VRRig[] _rigs;

	private static Transform[] _bones;

	private static int _rigsHash;

	private static readonly GTLogErrorLimiter _errorLog_nullVRRigFromVRRigCache = new GTLogErrorLimiter("(This should never happen) Skipping null `VRRig` obtained from `VRRigCache`!");

	private static GTLogErrorLimiter _errorLog_nullMainSkin = new GTLogErrorLimiter("(This should never happen) Skipping null `mainSkin` on `VRRig`! Scene paths: \n");

	private static readonly GTLogErrorLimiter _errorLog_nullBone = new GTLogErrorLimiter("(This should never happen) Skipping null bone obtained from `VRRig.mainSkin.bones`! Index(es): ");

	private readonly HashSet<MonkeFXSettingsSO> _settingsSOs = new HashSet<MonkeFXSettingsSO>(8);

	private readonly Dictionary<int, int> _srcMeshInst_to_meshId = new Dictionary<int, int>(8);

	private readonly List<Mesh> _srcMeshId_to_sourceMesh = new List<Mesh>(8);

	private readonly List<ElementsRange> _srcMeshId_to_elemRange = new List<ElementsRange>(8);

	private readonly Dictionary<int, List<MonkeFXSettingsSO>> _meshId_to_settingsUsers = new Dictionary<int, List<MonkeFXSettingsSO>>();

	private const float _k16BitFactor = 65536f;

	public static MonkeFX instance { get; private set; }

	public static bool hasInstance { get; private set; }

	bool ITickSystemPost.PostTickRunning { get; set; }

	private static void InitBonesArray()
	{
		_rigs = VRRigCache.Instance.GetAllRigs();
		_bones = new Transform[_rigs.Length * _boneNames.Length];
		for (int i = 0; i < _rigs.Length; i++)
		{
			if (_rigs[i] == null)
			{
				_errorLog_nullVRRigFromVRRigCache.AddOccurrence(i.ToString());
				continue;
			}
			int num = i * _boneNames.Length;
			if (_rigs[i].mainSkin == null)
			{
				_errorLog_nullMainSkin.AddOccurrence(_rigs[i].transform.GetPath());
				Debug.LogError("(This should never happen) Skipping null `mainSkin` on `VRRig`! Scene path: \n- \"" + _rigs[i].transform.GetPath() + "\"");
				continue;
			}
			for (int j = 0; j < _rigs[i].mainSkin.bones.Length; j++)
			{
				Transform transform = _rigs[i].mainSkin.bones[j];
				if (transform == null)
				{
					_errorLog_nullBone.AddOccurrence(j.ToString());
					continue;
				}
				for (int k = 0; k < _boneNames.Length; k++)
				{
					if (_boneNames[k] == transform.name)
					{
						_bones[num + k] = transform;
					}
				}
			}
		}
		_errorLog_nullVRRigFromVRRigCache.LogOccurrences(VRRigCache.Instance, null, "InitBonesArray", "C:\\Users\\root\\GT\\Assets\\GorillaTag\\Shared\\Scripts\\MonkeFX\\MonkeFX-Bones.cs", 106);
		_errorLog_nullMainSkin.LogOccurrences(null, null, "InitBonesArray", "C:\\Users\\root\\GT\\Assets\\GorillaTag\\Shared\\Scripts\\MonkeFX\\MonkeFX-Bones.cs", 107);
		_errorLog_nullBone.LogOccurrences(null, null, "InitBonesArray", "C:\\Users\\root\\GT\\Assets\\GorillaTag\\Shared\\Scripts\\MonkeFX\\MonkeFX-Bones.cs", 108);
	}

	private static void UpdateBones()
	{
	}

	private static void UpdateBone()
	{
	}

	public static void Register(MonkeFXSettingsSO settingsSO)
	{
		EnsureInstance();
		if (settingsSO == null || !instance._settingsSOs.Add(settingsSO))
		{
			return;
		}
		int num = instance._srcMeshId_to_sourceMesh.Count;
		for (int i = 0; i < settingsSO.sourceMeshes.Length; i++)
		{
			Mesh obj = settingsSO.sourceMeshes[i].obj;
			if (!(obj == null) && instance._srcMeshInst_to_meshId.TryAdd(obj.GetInstanceID(), num))
			{
				instance._srcMeshId_to_sourceMesh.Add(obj);
				num++;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float GetScaleToFitInBounds(Mesh mesh)
	{
		Bounds bounds = mesh.bounds;
		float num = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
		if (!(num > 0f))
		{
			return 0f;
		}
		return 1f / num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Pack0To1Floats(float x, float y)
	{
		return Mathf.Clamp01(x) * 65536f + Mathf.Clamp01(y);
	}

	private static void EnsureInstance()
	{
		if (!hasInstance)
		{
			instance = new MonkeFX();
			hasInstance = true;
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void OnAfterFirstSceneLoaded()
	{
		EnsureInstance();
		TickSystem<object>.AddPostTickCallback(instance);
	}

	void ITickSystemPost.PostTick()
	{
		if (!ApplicationQuittingState.IsQuitting)
		{
			UpdateBones();
		}
	}

	private static void PauseTick()
	{
		if (!hasInstance)
		{
			instance = new MonkeFX();
		}
		TickSystem<object>.RemovePostTickCallback(instance);
	}

	private static void ResumeTick()
	{
		if (!hasInstance)
		{
			instance = new MonkeFX();
		}
		TickSystem<object>.AddPostTickCallback(instance);
	}
}

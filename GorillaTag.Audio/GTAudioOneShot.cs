using System;
using UnityEngine;

namespace GorillaTag.Audio;

internal static class GTAudioOneShot
{
	private struct DelayedPlayData
	{
		public AudioClip sound;

		public Transform xform;

		public Vector3 pos;

		public float volume;

		public float pitch;
	}

	private class DelayedPlayListener : IDelayedExecListener
	{
		public void OnDelayedAction(int contextId)
		{
			if ((uint)contextId < (uint)_delayedHighWater)
			{
				ref DelayedPlayData reference = ref _delayedData[contextId];
				if (reference.sound != null)
				{
					Vector3 position = ((reference.xform != null) ? reference.xform.TransformPoint(reference.pos) : reference.pos);
					Play(reference.sound, position, reference.volume, reference.pitch);
				}
				reference = default(DelayedPlayData);
				_delayedFreeNext[contextId] = _delayedFreeHead;
				_delayedFreeHead = contextId;
			}
		}
	}

	[OnEnterPlay_SetNull]
	internal static AudioSource audioSource;

	[OnEnterPlay_SetNull]
	internal static AnimationCurve defaultCurve;

	private const int k_initialDelayedCount = 32;

	[OnEnterPlay_Set(0)]
	private static int _delayedHighWater;

	[OnEnterPlay_Set(-1)]
	private static int _delayedFreeHead = -1;

	[OnEnterPlay_SetNew]
	private static DelayedPlayData[] _delayedData = new DelayedPlayData[32];

	[OnEnterPlay_SetNew]
	private static int[] _delayedFreeNext = new int[32];

	[OnEnterPlay_SetNew]
	private static readonly DelayedPlayListener _delayedListener = new DelayedPlayListener();

	[field: OnEnterPlay_Set(false)]
	internal static bool isInitialized { get; private set; }

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Initialize()
	{
		if (!isInitialized)
		{
			AudioSource audioSource = Resources.Load<AudioSource>("AudioSourceSingleton_Prefab");
			if (audioSource == null)
			{
				Debug.LogError("GTAudioOneShot: Failed to load AudioSourceSingleton_Prefab from resources!!!");
				return;
			}
			GTAudioOneShot.audioSource = UnityEngine.Object.Instantiate(audioSource);
			defaultCurve = GTAudioOneShot.audioSource.GetCustomCurve(AudioSourceCurveType.CustomRolloff);
			UnityEngine.Object.DontDestroyOnLoad(GTAudioOneShot.audioSource);
			isInitialized = true;
		}
	}

	internal static void Play(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
	{
		if (!ApplicationQuittingState.IsQuitting && isInitialized)
		{
			audioSource.pitch = pitch;
			audioSource.transform.position = position;
			audioSource.GTPlayOneShot(clip, volume);
		}
	}

	internal static void Play(AudioClip clip, Vector3 position, AnimationCurve curve, float volume = 1f, float pitch = 1f)
	{
		if (!ApplicationQuittingState.IsQuitting && isInitialized)
		{
			audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, curve);
			Play(clip, position, volume, pitch);
			audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, defaultCurve);
		}
	}

	internal static int PlayDelayed(AudioClip sound, Vector3 pos, float delay, float volume = 1f, float pitch = 1f)
	{
		return PlayDelayed(sound, null, pos, delay, volume, pitch);
	}

	internal static int PlayDelayed(AudioClip sound, Transform xform, Vector3 pos, float delay, float volume = 1f, float pitch = 1f)
	{
		if (ApplicationQuittingState.IsQuitting || !isInitialized)
		{
			return -1;
		}
		int num;
		if (_delayedFreeHead >= 0)
		{
			num = _delayedFreeHead;
			_delayedFreeHead = _delayedFreeNext[num];
		}
		else
		{
			if (_delayedHighWater >= _delayedData.Length)
			{
				int newSize = _delayedData.Length * 2;
				Array.Resize(ref _delayedData, newSize);
				Array.Resize(ref _delayedFreeNext, newSize);
			}
			num = _delayedHighWater++;
		}
		_delayedData[num] = new DelayedPlayData
		{
			sound = sound,
			xform = xform,
			pos = pos,
			volume = volume,
			pitch = pitch
		};
		GTDelayedExec.Add(_delayedListener, delay, num);
		return num;
	}

	internal static void CancelDelayed(int idx)
	{
		if ((uint)idx < (uint)_delayedHighWater)
		{
			_delayedData[idx].sound = null;
		}
	}

	internal static void UpdateDelayed(int idx, Transform xform)
	{
		if ((uint)idx < (uint)_delayedHighWater)
		{
			ref DelayedPlayData reference = ref _delayedData[idx];
			if (!(reference.sound == null))
			{
				reference.xform = xform;
			}
		}
	}

	internal static void UpdateDelayed(int idx, Vector3 pos)
	{
		if ((uint)idx < (uint)_delayedHighWater)
		{
			ref DelayedPlayData reference = ref _delayedData[idx];
			if (!(reference.sound == null))
			{
				reference.pos = pos;
			}
		}
	}

	internal static void UpdateDelayed(int idx, Transform xform, Vector3 pos)
	{
		if ((uint)idx < (uint)_delayedHighWater)
		{
			ref DelayedPlayData reference = ref _delayedData[idx];
			if (!(reference.sound == null))
			{
				reference.xform = xform;
				reference.pos = pos;
			}
		}
	}
}

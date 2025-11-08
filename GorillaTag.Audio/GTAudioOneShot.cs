using UnityEngine;

namespace GorillaTag.Audio;

internal static class GTAudioOneShot
{
	[OnEnterPlay_SetNull]
	internal static AudioSource audioSource;

	[OnEnterPlay_SetNull]
	internal static AnimationCurve defaultCurve;

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
			GTAudioOneShot.audioSource = Object.Instantiate(audioSource);
			defaultCurve = GTAudioOneShot.audioSource.GetCustomCurve(AudioSourceCurveType.CustomRolloff);
			Object.DontDestroyOnLoad(GTAudioOneShot.audioSource);
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
}

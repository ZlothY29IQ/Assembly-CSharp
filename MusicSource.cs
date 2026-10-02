using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicSource : MonoBehaviour
{
	[SerializeField]
	private float defaultVolume = 1f;

	[SerializeField]
	private bool setDefaultVolumeFromAudioSourceOnAwake = true;

	private AudioSource audioSource;

	private float? volumeOverride;

	private bool locked;

	public float volume
	{
		get
		{
			return audioSource.volume;
		}
		set
		{
			audioSource.volume = value;
		}
	}

	public bool mute
	{
		get
		{
			return audioSource.mute;
		}
		set
		{
			audioSource.mute = value;
		}
	}

	public AudioClip clip
	{
		get
		{
			return audioSource.clip;
		}
		set
		{
			audioSource.clip = value;
		}
	}

	public float time
	{
		get
		{
			return audioSource.time;
		}
		set
		{
			audioSource.time = value;
		}
	}

	public float DefaultVolume => defaultVolume;

	public bool VolumeOverridden => volumeOverride.HasValue;

	public bool isPlaying => audioSource.isPlaying;

	private void Awake()
	{
		if (audioSource == null)
		{
			audioSource = GetComponent<AudioSource>();
		}
		if (setDefaultVolumeFromAudioSourceOnAwake)
		{
			defaultVolume = audioSource.volume;
		}
		audioSource.volume = (defaultVolume = Mathf.Max(0.05f, defaultVolume));
	}

	private void OnEnable()
	{
		if (MusicManager.Instance != null)
		{
			MusicManager.Instance.RegisterMusicSource(this);
		}
	}

	private void OnDisable()
	{
		if (MusicManager.Instance != null)
		{
			MusicManager.Instance.UnregisterMusicSource(this);
		}
	}

	public void SetVolumeOverride(float volume)
	{
		volumeOverride = volume;
		audioSource.volume = volumeOverride.Value;
	}

	public void UnsetVolumeOverride()
	{
		volumeOverride = null;
		audioSource.volume = defaultVolume;
	}

	internal void Lock(bool v)
	{
		if (v)
		{
			audioSource.Stop();
		}
		locked = v;
	}

	internal void Stop()
	{
		audioSource.Stop();
	}

	internal void PlayOneShot(AudioClip clip)
	{
		if (!locked && !(audioSource.volume <= 0f))
		{
			audioSource.PlayOneShot(clip, 1f / audioSource.volume);
		}
	}

	internal void GTPlay()
	{
		if (!locked)
		{
			audioSource.GTPlay();
		}
	}
}

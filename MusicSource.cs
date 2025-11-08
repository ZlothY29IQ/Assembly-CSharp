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

	public AudioSource AudioSource => audioSource;

	public float DefaultVolume => defaultVolume;

	public bool VolumeOverridden => volumeOverride.HasValue;

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
}

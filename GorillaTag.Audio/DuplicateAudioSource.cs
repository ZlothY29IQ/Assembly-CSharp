using UnityEngine;

namespace GorillaTag.Audio;

public class DuplicateAudioSource : MonoBehaviour
{
	public AudioSource TargetAudioSource;

	[SerializeField]
	private AudioSource _audioSource;

	[SerializeField]
	private bool _isDuplicating;

	public void SetTargetAudioSource(AudioSource target)
	{
		TargetAudioSource = target;
		StartDuplicating();
	}

	[ContextMenu("Start Duplicating")]
	public void StartDuplicating()
	{
		_isDuplicating = true;
		_audioSource.loop = TargetAudioSource.loop;
		_audioSource.clip = TargetAudioSource.clip;
		if (TargetAudioSource.isPlaying)
		{
			_audioSource.Play();
		}
	}

	[ContextMenu("Stop Duplicating")]
	public void StopDuplicating()
	{
		_isDuplicating = false;
		_audioSource.Stop();
	}

	public void LateUpdate()
	{
		if (_isDuplicating)
		{
			if (TargetAudioSource.isPlaying && !_audioSource.isPlaying)
			{
				_audioSource.Play();
			}
			else if (!TargetAudioSource.isPlaying && _audioSource.isPlaying)
			{
				_audioSource.Stop();
			}
		}
	}
}

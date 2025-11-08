using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[DefaultExecutionOrder(0)]
public class KIDAudioManager : MonoBehaviour
{
	public enum KIDSoundType
	{
		ButtonClick,
		Hover,
		Success,
		Denied,
		InputBack,
		TurnOffPermission,
		PageTransition,
		ButtonHeld
	}

	private static KIDAudioManager _instance;

	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private AudioSource loopingAudioSource;

	[SerializeField]
	private AudioMixer mainMixer;

	[SerializeField]
	private AudioMixerSnapshot KIDSnapshot;

	[SerializeField]
	private AudioMixerSnapshot normalSnapshot;

	[SerializeField]
	private AudioMixerGroup kidUIGroup;

	[SerializeField]
	private AudioClip buttonClickSound;

	[SerializeField]
	private AudioClip deniedSound;

	[SerializeField]
	private AudioClip successSound;

	[SerializeField]
	private AudioClip buttonHoverSound;

	[SerializeField]
	private AudioClip buttonHeldSound;

	[SerializeField]
	private AudioClip pageTransitionSound;

	[SerializeField]
	private AudioClip inputBackSound;

	[SerializeField]
	private AudioClip turnOffPermissionSound;

	private const string GAME_VOLUME = "Game_Volume";

	private const string KID_VOLUME = "KID_UI_Volume";

	private const float MUTED_VALUE = -80f;

	private const float UNMUTED_VALUE = 0f;

	private bool isKIDUIActive;

	private float cachedGameVolume;

	private bool isHoldSoundPlaying;

	private Dictionary<KIDSoundType, AudioClip> soundClips;

	public static KIDAudioManager Instance
	{
		get
		{
			if (!_instance)
			{
				if (!ApplicationQuittingState.IsQuitting)
				{
					Debug.LogError("No KIDAudioManager instance found in scene!");
				}
				return null;
			}
			return _instance;
		}
	}

	private void Awake()
	{
		if (_instance == null)
		{
			_instance = this;
			base.transform.parent = null;
			Object.DontDestroyOnLoad(base.gameObject);
			ConfigureAudioSource();
			InitializeSoundClips();
			mainMixer.GetFloat("Game_Volume", out cachedGameVolume);
		}
		else if (_instance != this)
		{
			Object.Destroy(base.gameObject);
		}
	}

	private void ConfigureAudioSource()
	{
		if (audioSource != null)
		{
			audioSource.outputAudioMixerGroup = kidUIGroup;
			audioSource.playOnAwake = false;
			audioSource.spatialBlend = 0f;
			audioSource.volume = 1f;
			audioSource.enabled = true;
		}
		if (loopingAudioSource != null)
		{
			loopingAudioSource.outputAudioMixerGroup = kidUIGroup;
			loopingAudioSource.playOnAwake = false;
			loopingAudioSource.spatialBlend = 0f;
			loopingAudioSource.volume = 1f;
			loopingAudioSource.loop = true;
			loopingAudioSource.enabled = true;
		}
	}

	private void InitializeSoundClips()
	{
		soundClips = new Dictionary<KIDSoundType, AudioClip>
		{
			{
				KIDSoundType.ButtonClick,
				buttonClickSound
			},
			{
				KIDSoundType.Denied,
				deniedSound
			},
			{
				KIDSoundType.Success,
				successSound
			},
			{
				KIDSoundType.Hover,
				buttonHoverSound
			},
			{
				KIDSoundType.ButtonHeld,
				buttonHeldSound
			},
			{
				KIDSoundType.PageTransition,
				pageTransitionSound
			},
			{
				KIDSoundType.InputBack,
				inputBackSound
			},
			{
				KIDSoundType.TurnOffPermission,
				turnOffPermissionSound
			}
		};
	}

	public void SetKIDUIAudioActive(bool active)
	{
		if (IsInstanceValid() && isKIDUIActive != active)
		{
			isKIDUIActive = active;
			if (!active)
			{
				StopButtonHeldSound();
			}
			if (active)
			{
				KIDSnapshot.TransitionTo(0f);
			}
			else
			{
				normalSnapshot.TransitionTo(0f);
			}
		}
	}

	public void PlaySound(KIDSoundType soundType)
	{
		if (IsInstanceValid())
		{
			AudioClip value;
			if (soundType == KIDSoundType.ButtonHeld)
			{
				Debug.LogWarning("[KIDAudioManager] Button held sound is already playing, skipping delayed sound.");
			}
			else if (soundClips.TryGetValue(soundType, out value) && value != null)
			{
				audioSource.PlayOneShot(value);
			}
			else
			{
				Debug.LogWarning($"[KIDAudioManager] Sound clip for {soundType} is null or not found!");
			}
		}
	}

	public void StartButtonHeldSound()
	{
		if (IsInstanceValid() && !(buttonHeldSound == null) && !isHoldSoundPlaying)
		{
			loopingAudioSource.clip = buttonHeldSound;
			loopingAudioSource.Play();
			isHoldSoundPlaying = true;
		}
	}

	public void StopButtonHeldSound()
	{
		if (IsInstanceValid() && isHoldSoundPlaying)
		{
			if (loopingAudioSource.clip == buttonHeldSound)
			{
				loopingAudioSource.Stop();
			}
			isHoldSoundPlaying = false;
		}
	}

	private bool IsInstanceValid()
	{
		if (_instance == null || _instance != this || audioSource == null || loopingAudioSource == null)
		{
			return false;
		}
		return true;
	}

	public bool IsKIDUIActive()
	{
		return isKIDUIActive;
	}

	public void PlaySoundWithDelay(KIDSoundType soundType)
	{
		StartCoroutine(PlayDelayedSound(soundType, 0.05f));
	}

	private IEnumerator PlayDelayedSound(KIDSoundType soundType, float delay)
	{
		yield return new WaitForSeconds(delay);
		PlaySound(soundType);
	}
}

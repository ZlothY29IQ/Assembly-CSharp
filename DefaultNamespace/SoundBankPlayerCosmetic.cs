using UnityEngine;

namespace DefaultNamespace;

[RequireComponent(typeof(SoundBankPlayer))]
public class SoundBankPlayerCosmetic : MonoBehaviour, ITickSystemTick
{
	[SerializeField]
	private SoundBankPlayer soundBankPlayer;

	private bool playAudioLoop;

	public bool TickRunning { get; set; }

	private void Awake()
	{
		playAudioLoop = false;
	}

	private void OnEnable()
	{
		TickSystem<object>.AddTickCallback(this);
	}

	private void OnDisable()
	{
		TickSystem<object>.RemoveTickCallback(this);
	}

	public void Tick()
	{
		if (playAudioLoop && soundBankPlayer != null && soundBankPlayer.audioSource != null && soundBankPlayer.soundBank != null && !soundBankPlayer.audioSource.isPlaying)
		{
			soundBankPlayer.Play();
		}
	}

	public void PlayAudio()
	{
		if (soundBankPlayer != null && soundBankPlayer.audioSource != null && soundBankPlayer.soundBank != null)
		{
			soundBankPlayer.Play();
		}
	}

	public void PlayAudioLoop()
	{
		playAudioLoop = true;
	}

	public void PlayAudioNonInterrupting()
	{
		if (soundBankPlayer != null && soundBankPlayer.audioSource != null && soundBankPlayer.soundBank != null && !soundBankPlayer.audioSource.isPlaying)
		{
			soundBankPlayer.Play();
		}
	}

	public void PlayAudioWithTunableVolume(bool leftHand, float fingerValue)
	{
		if (soundBankPlayer != null && soundBankPlayer.audioSource != null && soundBankPlayer.soundBank != null)
		{
			float volume = Mathf.Clamp01(fingerValue);
			soundBankPlayer.audioSource.volume = volume;
			soundBankPlayer.Play();
		}
	}

	public void StopAudio()
	{
		if (soundBankPlayer != null && soundBankPlayer.audioSource != null && soundBankPlayer.soundBank != null)
		{
			soundBankPlayer.audioSource.Stop();
		}
		playAudioLoop = false;
	}
}

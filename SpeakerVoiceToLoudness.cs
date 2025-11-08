using System;
using Photon.Voice;
using Photon.Voice.Unity;
using UnityEngine;

[RequireComponent(typeof(Speaker))]
public class SpeakerVoiceToLoudness : MonoBehaviour
{
	[SerializeField]
	private PlaybackDelaySettings playbackDelaySettings = new PlaybackDelaySettings
	{
		MinDelaySoft = 200,
		MaxDelaySoft = 400,
		MaxDelayHard = 1000
	};

	public float loudness;

	private void Awake()
	{
		Speaker component = GetComponent<Speaker>();
		component.CustomAudioOutFactory = GetVolumeTracking(component);
	}

	private Func<IAudioOut<float>> GetVolumeTracking(Speaker speaker)
	{
		AudioOutDelayControl.PlayDelayConfig pdc = new AudioOutDelayControl.PlayDelayConfig
		{
			Low = playbackDelaySettings.MinDelaySoft,
			High = playbackDelaySettings.MaxDelaySoft,
			Max = playbackDelaySettings.MaxDelayHard
		};
		return () => new SpeakerVoiceLoudnessAudioOut(this, speaker.GetComponent<AudioSource>(), pdc, speaker.Logger, string.Empty, speaker.Logger.IsDebugEnabled);
	}
}

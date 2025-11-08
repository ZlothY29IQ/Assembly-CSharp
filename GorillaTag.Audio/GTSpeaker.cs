using System;
using System.Collections.Generic;
using Photon.Voice;
using Photon.Voice.Unity;
using UnityEngine;
using UnityEngine.Serialization;

namespace GorillaTag.Audio;

public class GTSpeaker : Speaker
{
	[FormerlySerializedAs("UseExternalAudioSources")]
	public bool BroadcastExternal;

	[SerializeField]
	private AudioSource[] _externalAudioSources;

	private List<IAudioOut<float>> _externalAudioOutputs;

	private int _frequency;

	private int _channels;

	private int _frameSamplesPerChannel;

	private bool _initializedExternalAudioSources;

	private bool _audioOutputStarted;

	public void Start()
	{
		LoudSpeakerNetwork componentInChildren = base.transform.root.GetComponentInChildren<LoudSpeakerNetwork>();
		if (componentInChildren != null)
		{
			AddExternalAudioSources(componentInChildren.SpeakerSources);
		}
	}

	public void AddExternalAudioSources(AudioSource[] audioSources)
	{
		if (!_initializedExternalAudioSources)
		{
			_externalAudioSources = audioSources;
			InitializeExternalAudioSources();
			if (_audioOutputStarted)
			{
				ExternalAudioOutputStart(_frequency, _channels, _frameSamplesPerChannel);
			}
		}
	}

	protected override void Initialize()
	{
		if (base.IsInitialized)
		{
			if (base.Logger.IsWarningEnabled)
			{
				base.Logger.LogWarning("Already initialized.");
			}
		}
		else
		{
			base.Initialize();
		}
	}

	private void InitializeExternalAudioSources()
	{
		_initializedExternalAudioSources = true;
		_externalAudioOutputs = new List<IAudioOut<float>>();
		AudioOutDelayControl.PlayDelayConfig pdc = new AudioOutDelayControl.PlayDelayConfig
		{
			Low = playbackDelaySettings.MinDelaySoft,
			High = playbackDelaySettings.MaxDelaySoft,
			Max = playbackDelaySettings.MaxDelayHard
		};
		AudioSource[] externalAudioSources = _externalAudioSources;
		foreach (AudioSource source in externalAudioSources)
		{
			_externalAudioOutputs.Add(GetAudioOutFactoryFromSource(source, pdc)());
		}
	}

	private Func<IAudioOut<float>> GetAudioOutFactoryFromSource(AudioSource source, AudioOutDelayControl.PlayDelayConfig pdc)
	{
		return () => new UnityAudioOut(source, pdc, base.Logger, string.Empty, base.Logger.IsDebugEnabled);
	}

	protected override void OnAudioFrame(FrameOut<float> frame)
	{
		base.OnAudioFrame(frame);
		if (!BroadcastExternal)
		{
			return;
		}
		foreach (IAudioOut<float> externalAudioOutput in _externalAudioOutputs)
		{
			externalAudioOutput.Push(frame.Buf);
			if (frame.EndOfStream)
			{
				externalAudioOutput.Flush();
			}
		}
	}

	protected override void AudioOutputStart(int frequency, int channels, int frameSamplesPerChannel)
	{
		_audioOutputStarted = true;
		_frequency = frequency;
		_channels = channels;
		_frameSamplesPerChannel = frameSamplesPerChannel;
		base.AudioOutputStart(frequency, channels, frameSamplesPerChannel);
		ExternalAudioOutputStart(frequency, channels, frameSamplesPerChannel);
	}

	private void ExternalAudioOutputStart(int frequency, int channels, int frameSamplesPerChannel)
	{
		if (_externalAudioOutputs == null)
		{
			return;
		}
		foreach (IAudioOut<float> externalAudioOutput in _externalAudioOutputs)
		{
			if (!externalAudioOutput.IsPlaying)
			{
				externalAudioOutput.Start(frequency, channels, frameSamplesPerChannel);
				externalAudioOutput.ToggleAudioSource(toggle: false);
			}
		}
	}

	protected override void AudioOutputStop()
	{
		_audioOutputStarted = false;
		if (_externalAudioOutputs != null)
		{
			foreach (IAudioOut<float> externalAudioOutput in _externalAudioOutputs)
			{
				externalAudioOutput.Stop();
			}
		}
		base.AudioOutputStop();
	}

	protected override void AudioOutputService()
	{
		base.AudioOutputService();
		if (_externalAudioOutputs == null)
		{
			return;
		}
		foreach (IAudioOut<float> externalAudioOutput in _externalAudioOutputs)
		{
			if (!externalAudioOutput.IsPlaying)
			{
				externalAudioOutput.Service();
			}
		}
	}

	public void ToggleAudioSource(bool toggle)
	{
		if (_externalAudioOutputs == null)
		{
			return;
		}
		foreach (IAudioOut<float> externalAudioOutput in _externalAudioOutputs)
		{
			externalAudioOutput.ToggleAudioSource(toggle);
		}
	}
}

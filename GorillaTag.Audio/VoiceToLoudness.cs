using System;
using Photon.Voice;
using Photon.Voice.Unity;
using UnityEngine;

namespace GorillaTag.Audio;

[RequireComponent(typeof(Recorder))]
public class VoiceToLoudness : MonoBehaviour
{
	[NonSerialized]
	public float loudness;

	private Recorder _recorder;

	protected void Awake()
	{
		_recorder = GetComponent<Recorder>();
	}

	protected void PhotonVoiceCreated(PhotonVoiceCreatedParams photonVoiceCreatedParams)
	{
		_ = photonVoiceCreatedParams.Voice.Info;
		if (photonVoiceCreatedParams.Voice is LocalVoiceAudioFloat localVoiceAudioFloat)
		{
			localVoiceAudioFloat.AddPostProcessor(new ProcessVoiceDataToLoudness(this));
		}
	}
}

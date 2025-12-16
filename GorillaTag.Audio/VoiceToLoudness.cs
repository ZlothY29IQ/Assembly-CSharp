using System;
using Photon.Voice;
using Photon.Voice.Unity;
using UnityEngine;

namespace GorillaTag.Audio;

[RequireComponent(typeof(Recorder))]
public class VoiceToLoudness : MonoBehaviour
{
	[NonSerialized]
	public float Loudness;

	private Recorder _recorder;

	private bool _photonVoiceCreated;

	private float _checkVoice;

	protected void Awake()
	{
		_recorder = GetComponent<Recorder>();
	}

	protected void PhotonVoiceCreated(PhotonVoiceCreatedParams photonVoiceCreatedParams)
	{
		CreateProcessVoiceData(photonVoiceCreatedParams.Voice);
	}

	private void CreateProcessVoiceData(LocalVoice voice)
	{
		if (voice is LocalVoiceAudioFloat localVoiceAudioFloat)
		{
			_photonVoiceCreated = true;
			localVoiceAudioFloat.AddPostProcessor(new ProcessVoiceDataToLoudness(this));
		}
	}

	private void Update()
	{
		if (!_photonVoiceCreated && _recorder != null && _recorder.Voice != null)
		{
			CreateProcessVoiceData(_recorder.Voice);
		}
	}
}

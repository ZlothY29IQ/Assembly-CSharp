using System;
using Photon.Voice;
using UnityEngine;

namespace GorillaTag.Audio;

internal class ProcessVoiceDataToLoudness : IProcessor<float>, IDisposable
{
	private VoiceToLoudness _voiceToLoudness;

	public ProcessVoiceDataToLoudness(VoiceToLoudness voiceToLoudness)
	{
		_voiceToLoudness = voiceToLoudness;
	}

	public float[] Process(float[] buf)
	{
		float num = 0f;
		for (int i = 0; i < buf.Length; i++)
		{
			num += Mathf.Abs(buf[i]);
		}
		_voiceToLoudness.loudness = num / (float)buf.Length;
		return buf;
	}

	public void Dispose()
	{
	}
}

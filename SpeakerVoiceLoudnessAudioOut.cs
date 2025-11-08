using Photon.Voice;
using Photon.Voice.Unity;
using UnityEngine;

public class SpeakerVoiceLoudnessAudioOut : UnityAudioOut
{
	private SpeakerVoiceToLoudness voiceToLoudness;

	public SpeakerVoiceLoudnessAudioOut(SpeakerVoiceToLoudness speaker, AudioSource audioSource, PlayDelayConfig playDelayConfig, Photon.Voice.ILogger logger, string logPrefix, bool debugInfo)
		: base(audioSource, playDelayConfig, logger, logPrefix, debugInfo)
	{
		voiceToLoudness = speaker;
	}

	public override void OutWrite(float[] data, int offsetSamples)
	{
		float num = 0f;
		for (int i = 0; i < data.Length; i++)
		{
			float num2 = data[i];
			if (!float.IsFinite(num2))
			{
				num2 = (data[i] = 0f);
			}
			else if (num2 > 1f)
			{
				num2 = (data[i] = 1f);
			}
			else if (num2 < -1f)
			{
				num2 = (data[i] = -1f);
			}
			num += Mathf.Abs(num2);
		}
		if (num > 0f)
		{
			voiceToLoudness.loudness = num / (float)data.Length;
		}
		base.OutWrite(data, offsetSamples);
	}
}

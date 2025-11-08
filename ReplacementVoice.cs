using GorillaTag.Cosmetics;
using UnityEngine;

public class ReplacementVoice : MonoBehaviour, IGorillaSliceableSimple
{
	public AudioSource replacementVoiceSource;

	public AudioClip[] replacementVoiceClips;

	public AudioClip[] replacementVoiceClipsLoud;

	public float loudReplacementVoiceThreshold = 0.1f;

	public VRRig myVRRig;

	public float normalVolume = 0.5f;

	public float loudVolume = 0.8f;

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
	}

	public void SliceUpdate()
	{
		CosmeticEffectsOnPlayers.CosmeticEffect value;
		if (!replacementVoiceSource.isPlaying && myVRRig.ShouldPlayReplacementVoice())
		{
			if (!Mathf.Approximately(myVRRig.voiceAudio.pitch, replacementVoiceSource.pitch))
			{
				replacementVoiceSource.pitch = myVRRig.voiceAudio.pitch;
			}
			if (myVRRig.SpeakingLoudness < loudReplacementVoiceThreshold)
			{
				replacementVoiceSource.clip = replacementVoiceClips[Random.Range(0, replacementVoiceClips.Length - 1)];
				replacementVoiceSource.volume = normalVolume;
			}
			else
			{
				replacementVoiceSource.clip = replacementVoiceClipsLoud[Random.Range(0, replacementVoiceClipsLoud.Length - 1)];
				replacementVoiceSource.volume = loudVolume;
			}
			replacementVoiceSource.GTPlay();
		}
		else if (!replacementVoiceSource.isPlaying && myVRRig.TryGetCosmeticVoiceOverride(CosmeticEffectsOnPlayers.EFFECTTYPE.VoiceOverride, out value) && !(myVRRig.SpeakingLoudness < myVRRig.replacementVoiceLoudnessThreshold))
		{
			if (!Mathf.Approximately(myVRRig.voiceAudio.pitch, replacementVoiceSource.pitch))
			{
				replacementVoiceSource.pitch = myVRRig.voiceAudio.pitch;
			}
			if (myVRRig.SpeakingLoudness < value.voiceOverrideLoudThreshold)
			{
				replacementVoiceSource.clip = value.voiceOverrideNormalClips[Random.Range(0, value.voiceOverrideNormalClips.Length - 1)];
				replacementVoiceSource.volume = value.voiceOverrideNormalVolume;
			}
			else
			{
				replacementVoiceSource.clip = value.voiceOverrideLoudClips[Random.Range(0, value.voiceOverrideLoudClips.Length - 1)];
				replacementVoiceSource.volume = value.voiceOverrideLoudVolume;
			}
			replacementVoiceSource.GTPlay();
		}
	}
}

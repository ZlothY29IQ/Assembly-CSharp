using GorillaExtensions;
using Photon.Pun;
using UnityEngine;

public class CrittersVoiceNoise : MonoBehaviour, IGorillaSliceableSimple
{
	[SerializeField]
	private GorillaSpeakerLoudness speaker;

	[SerializeField]
	private VRRig rig;

	[SerializeField]
	private float minTriggerThreshold = 0.01f;

	[SerializeField]
	private float maxTriggerThreshold = 0.3f;

	[SerializeField]
	private float noiseVolumeMin = 1f;

	[SerializeField]
	private float noisVolumeMax = 9f;

	private void Start()
	{
		speaker = GetComponent<GorillaSpeakerLoudness>();
	}

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
		float num = 0f;
		if (speaker.IsSpeaking)
		{
			num = speaker.Loudness;
		}
		if (num > minTriggerThreshold && CrittersManager.instance.IsNotNull())
		{
			CrittersLoudNoise crittersLoudNoise = (CrittersLoudNoise)CrittersManager.instance.rigSetupByRig[rig].rigActors[4].actorSet;
			if (crittersLoudNoise.IsNotNull() && !crittersLoudNoise.soundEnabled)
			{
				float volume = Mathf.Lerp(noiseVolumeMin, noisVolumeMax, Mathf.Clamp01((num - minTriggerThreshold) / maxTriggerThreshold));
				crittersLoudNoise.PlayVoiceSpeechLocal(PhotonNetwork.InRoom ? PhotonNetwork.Time : ((double)Time.time), 1f / 60f, volume);
			}
		}
	}
}

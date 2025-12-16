using System;
using GorillaNetworking;
using GorillaTag;
using GorillaTag.Audio;
using Oculus.VoiceSDK.Utilities;
using Photon.Voice.PUN;
using Photon.Voice.Unity;
using UnityEngine;

public class GorillaSpeakerLoudness : MonoBehaviour, IGorillaSliceableSimple, IDynamicFloat
{
	private bool isSpeaking;

	private float loudness;

	[SerializeField]
	private float normalizedMax = 0.175f;

	private bool isMicEnabled;

	private RigContainer rigContainer;

	private Speaker speaker;

	private SpeakerVoiceToLoudness speakerVoiceToLoudness;

	private Recorder recorder;

	private VoiceToLoudness voiceToLoudness;

	private float smoothedLoudness;

	private float lastLoudness;

	private float timeSinceLoudnessChange;

	private float loudnessUpdateCheckRate = 0.2f;

	private float loudnessBlendStrength = 2f;

	private bool permission;

	private bool micConnected;

	private float timeLastUpdated;

	private float deltaTime;

	private AudioClip offlineMic;

	private float[] voiceSampleBuffer = new float[128];

	public bool IsSpeaking => isSpeaking;

	public float Loudness => loudness;

	public float LoudnessNormalized => Mathf.Min(loudness / normalizedMax, 1f);

	public float floatValue => LoudnessNormalized;

	public bool IsMicEnabled => isMicEnabled;

	public float SmoothedLoudness => smoothedLoudness;

	private void Start()
	{
		rigContainer = GetComponent<RigContainer>();
		timeLastUpdated = Time.time;
		deltaTime = Time.deltaTime;
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
		deltaTime = Time.time - timeLastUpdated;
		timeLastUpdated = Time.time;
		UpdateMicEnabled();
		UpdateLoudness();
		UpdateSmoothedLoudness();
	}

	private void UpdateMicEnabled()
	{
		if (!(rigContainer == null))
		{
			VRRig rig = rigContainer.Rig;
			if (rig.isOfflineVRRig)
			{
				isMicEnabled = CheckMicConnection();
				rig.IsMicEnabled = isMicEnabled;
			}
			else
			{
				isMicEnabled = rig.IsMicEnabled;
			}
		}
	}

	private bool CheckMicConnection()
	{
		permission = permission || MicPermissionsManager.HasMicPermission();
		if (permission && !micConnected && Microphone.devices != null)
		{
			micConnected = Microphone.devices.Length != 0;
		}
		if (permission)
		{
			return micConnected;
		}
		return false;
	}

	private void UpdateLoudness()
	{
		if (rigContainer == null)
		{
			return;
		}
		PhotonVoiceView voice = rigContainer.Voice;
		if (voice != null && speaker == null)
		{
			speaker = voice.SpeakerInUse;
		}
		if (recorder == null)
		{
			recorder = voice?.RecorderInUse;
		}
		if (recorder != null && offlineMic != null)
		{
			Microphone.End(UnityMicrophone.devices[0]);
			UnityEngine.Object.Destroy(offlineMic);
			offlineMic = null;
			recorder.RestartRecording(force: true);
		}
		VRRig rig = rigContainer.Rig;
		if (rig.isOfflineVRRig && recorder == null && isMicEnabled && !Microphone.IsRecording(UnityMicrophone.devices[0]))
		{
			offlineMic = Microphone.Start(UnityMicrophone.devices[0], loop: true, 1, 16000);
		}
		if ((rig.remoteUseReplacementVoice || rig.localUseReplacementVoice || GorillaComputer.instance.voiceChatOn == "FALSE") && rig.SpeakingLoudness > 0f && !rigContainer.ForceMute && !rigContainer.Muted)
		{
			isSpeaking = true;
			loudness = rig.SpeakingLoudness;
		}
		else if (voice != null && voice.IsSpeaking)
		{
			isSpeaking = true;
			if (speaker != null)
			{
				if (speakerVoiceToLoudness == null)
				{
					speakerVoiceToLoudness = speaker.GetComponent<SpeakerVoiceToLoudness>();
				}
				if (speakerVoiceToLoudness != null)
				{
					loudness = speakerVoiceToLoudness.loudness;
				}
			}
			else
			{
				loudness = 0f;
			}
		}
		else if (voice != null && recorder != null && NetworkSystem.Instance.IsObjectLocallyOwned(voice.gameObject) && recorder.IsCurrentlyTransmitting)
		{
			if (voiceToLoudness == null)
			{
				voiceToLoudness = recorder.GetComponent<VoiceToLoudness>();
				if (voiceToLoudness == null)
				{
					recorder.AddComponent<VoiceToLoudness>();
				}
			}
			isSpeaking = true;
			if (voiceToLoudness != null)
			{
				loudness = voiceToLoudness.Loudness;
			}
			else
			{
				loudness = 0f;
			}
		}
		else if (offlineMic != null && recorder == null && isMicEnabled && Microphone.IsRecording(UnityMicrophone.devices[0]))
		{
			isSpeaking = true;
			int num = Mathf.Min(Mathf.CeilToInt(deltaTime * 16000f), 16000);
			if (num > voiceSampleBuffer.Length)
			{
				Array.Resize(ref voiceSampleBuffer, num);
			}
			if (offlineMic.samples >= num && offlineMic.GetData(voiceSampleBuffer, offlineMic.samples - num))
			{
				float num2 = 0f;
				for (int i = 0; i < voiceSampleBuffer.Length; i++)
				{
					num2 += Mathf.Abs(voiceSampleBuffer[i]);
				}
				loudness = num2 / (float)voiceSampleBuffer.Length;
			}
		}
		else
		{
			isSpeaking = false;
			loudness = 0f;
		}
	}

	private void UpdateSmoothedLoudness()
	{
		if (!isSpeaking)
		{
			smoothedLoudness = 0f;
		}
		else if (Mathf.Approximately(loudness, lastLoudness))
		{
			if (timeSinceLoudnessChange > loudnessUpdateCheckRate)
			{
				smoothedLoudness = 0.001f;
				return;
			}
			smoothedLoudness = Mathf.Lerp(smoothedLoudness, loudness, Mathf.Clamp01(loudnessBlendStrength * deltaTime));
			timeSinceLoudnessChange += deltaTime;
		}
		else
		{
			timeSinceLoudnessChange = 0f;
			smoothedLoudness = Mathf.Lerp(smoothedLoudness, loudness, Mathf.Clamp01(loudnessBlendStrength * deltaTime));
			lastLoudness = loudness;
		}
	}
}

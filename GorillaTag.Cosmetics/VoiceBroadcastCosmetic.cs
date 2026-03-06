using GorillaTag.Audio;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTag.Cosmetics;

[RequireComponent(typeof(LoudSpeakerActivator))]
public class VoiceBroadcastCosmetic : MonoBehaviour, IGorillaSliceableSimple
{
	public TalkingCosmeticType talkingCosmeticType;

	[Tooltip("How loud the Gorilla voice should be before detecting as talking.")]
	[SerializeField]
	public float minVolume = 0.1f;

	[Tooltip("How long the initial speaking section needs to last to trigger the talking animation.")]
	[SerializeField]
	public float minSpeakingTime = 0.15f;

	[SerializeField]
	private Animation simpleAnimation;

	[SerializeField]
	private string talkAnimationTriggerName;

	private int talkAnimationTrigger;

	private const string EVENTS = "Events";

	[SerializeField]
	private UnityEvent onStartListening;

	[SerializeField]
	private UnityEvent onStartSpeaking;

	[SerializeField]
	private UnityEvent onStopSpeaking;

	[SerializeField]
	private UnityEvent onStopListening;

	private float speakingTime;

	private bool isListening;

	private bool isSpeaking;

	private VoiceBroadcastCosmeticWearable wearable;

	private LoudSpeakerActivator loudSpeaker;

	private GorillaSpeakerLoudness gsl;

	private Animator animator;

	private float lastSliceUpdateTime;

	private void Awake()
	{
		loudSpeaker = GetComponent<LoudSpeakerActivator>();
		animator = GetComponent<Animator>();
		talkAnimationTrigger = Animator.StringToHash(talkAnimationTriggerName);
		gsl = GetComponentInParent<GorillaSpeakerLoudness>();
	}

	public void SetWearable(VoiceBroadcastCosmeticWearable wearable)
	{
		this.wearable = wearable;
	}

	private void StartBroadcast()
	{
		loudSpeaker.StartLocalBroadcast();
		onStartListening?.Invoke();
		onStartListening?.Invoke();
		wearable.OnCosmeticStartListening();
		lastSliceUpdateTime = Time.time;
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
	}

	private void StopBroadcast()
	{
		loudSpeaker.StopLocalBroadcast();
		onStopListening?.Invoke();
		wearable.OnCosmeticStopListening();
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
	}

	public void OnEnable()
	{
		isListening = false;
		speakingTime = 0f;
	}

	public void OnDisable()
	{
		isListening = false;
		speakingTime = 0f;
		StopBroadcast();
	}

	public void SetListenState(bool listening)
	{
		if (isListening != listening && base.enabled && base.gameObject.activeInHierarchy)
		{
			isListening = listening;
			speakingTime = 0f;
			if (listening)
			{
				StartBroadcast();
			}
			else
			{
				StopBroadcast();
			}
		}
	}

	public void SliceUpdate()
	{
		float num = Time.time - lastSliceUpdateTime;
		lastSliceUpdateTime = Time.time;
		if (gsl != null && gsl.IsSpeaking && gsl.LoudnessNormalized >= minVolume)
		{
			speakingTime += num;
			if (speakingTime >= minSpeakingTime)
			{
				if (animator != null)
				{
					animator.SetTrigger(talkAnimationTrigger);
				}
				if (simpleAnimation != null && !simpleAnimation.isPlaying)
				{
					simpleAnimation.Play();
				}
				if (!isSpeaking)
				{
					onStartSpeaking?.Invoke();
					isSpeaking = true;
				}
			}
		}
		else
		{
			speakingTime = 0f;
			if (isSpeaking)
			{
				onStopSpeaking?.Invoke();
				isSpeaking = false;
			}
		}
	}

	private void ResetToFirstFrame()
	{
		simpleAnimation.Rewind();
		simpleAnimation.Play();
		simpleAnimation.Sample();
		simpleAnimation.Stop();
	}
}

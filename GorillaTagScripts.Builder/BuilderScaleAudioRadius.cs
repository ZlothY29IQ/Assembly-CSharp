using UnityEngine;
using UnityEngine.Serialization;

namespace GorillaTagScripts.Builder;

public class BuilderScaleAudioRadius : MonoBehaviour
{
	[Tooltip("Scale particles on enable using lossy scale")]
	[SerializeField]
	private bool useLossyScaleOnEnable;

	[Tooltip("Play sound after scaling")]
	[SerializeField]
	private bool autoPlay;

	[SerializeField]
	private AudioSource audioSource;

	[FormerlySerializedAs("soundBankToPlay")]
	[SerializeField]
	private SoundBankPlayer autoPlaySoundBank;

	private float minDist;

	private float maxDist = 1f;

	private AnimationCurve customCurve;

	private AnimationCurve scaledCurve = new AnimationCurve();

	private float scale = 1f;

	private bool shouldRevert;

	private bool setScaleNextFrame;

	private int enableFrame;

	private void OnEnable()
	{
		if (useLossyScaleOnEnable)
		{
			setScaleNextFrame = true;
			enableFrame = Time.frameCount;
		}
	}

	private void OnDisable()
	{
		if (useLossyScaleOnEnable)
		{
			RevertScale();
		}
	}

	private void LateUpdate()
	{
		if (setScaleNextFrame && Time.frameCount > enableFrame)
		{
			if (useLossyScaleOnEnable)
			{
				SetScale(base.transform.lossyScale.x);
			}
			setScaleNextFrame = false;
		}
	}

	private void PlaySound()
	{
		if (autoPlaySoundBank != null)
		{
			autoPlaySoundBank.Play();
		}
		else if (audioSource.clip != null)
		{
			audioSource.Play();
		}
	}

	public void SetScale(float inScale)
	{
		if (Mathf.Approximately(inScale, scale))
		{
			if (autoPlay)
			{
				PlaySound();
			}
			return;
		}
		scale = inScale;
		RevertScale();
		if (Mathf.Approximately(scale, 1f))
		{
			if (autoPlay)
			{
				PlaySound();
			}
			return;
		}
		switch (audioSource.rolloffMode)
		{
		case AudioRolloffMode.Logarithmic:
		case AudioRolloffMode.Linear:
			minDist = audioSource.minDistance;
			maxDist = audioSource.maxDistance;
			audioSource.maxDistance *= scale;
			audioSource.minDistance *= scale;
			break;
		case AudioRolloffMode.Custom:
			maxDist = audioSource.maxDistance;
			audioSource.maxDistance *= scale;
			break;
		}
		if (autoPlay)
		{
			PlaySound();
		}
		shouldRevert = true;
	}

	public void RevertScale()
	{
		if (shouldRevert)
		{
			switch (audioSource.rolloffMode)
			{
			case AudioRolloffMode.Logarithmic:
			case AudioRolloffMode.Linear:
				audioSource.minDistance = minDist;
				audioSource.maxDistance = maxDist;
				break;
			case AudioRolloffMode.Custom:
				audioSource.maxDistance = maxDist;
				break;
			}
			scale = 1f;
			shouldRevert = false;
		}
	}
}

using UnityEngine;

public class PlayerSpeedBasedAudio : MonoBehaviour
{
	[SerializeField]
	private float minVolumeSpeed;

	[SerializeField]
	private float fullVolumeSpeed;

	[SerializeField]
	private float fadeTime;

	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private XSceneRef localPlayerVelocityEstimator;

	private GorillaVelocityEstimator velocityEstimator;

	private float baseVolume;

	private float fadeRate;

	private float currentFadeLevel;

	private void Start()
	{
		fadeRate = 1f / fadeTime;
		baseVolume = audioSource.volume;
		localPlayerVelocityEstimator.TryResolve(out velocityEstimator);
	}

	private void Update()
	{
		currentFadeLevel = Mathf.MoveTowards(currentFadeLevel, Mathf.InverseLerp(minVolumeSpeed, fullVolumeSpeed, velocityEstimator.linearVelocity.magnitude), fadeRate * Time.deltaTime);
		if (baseVolume == 0f || currentFadeLevel == 0f)
		{
			audioSource.volume = 0.0001f;
		}
		else
		{
			audioSource.volume = baseVolume * currentFadeLevel;
		}
	}
}

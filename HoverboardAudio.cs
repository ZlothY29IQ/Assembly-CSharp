using UnityEngine;

public class HoverboardAudio : MonoBehaviour
{
	[SerializeField]
	private AudioSource hum1;

	[SerializeField]
	private SoundBankPlayer turnSounds;

	private bool didInitHum1BaseVolume;

	private float hum1BaseVolume;

	[SerializeField]
	private float fadeSpeed;

	[SerializeField]
	private AudioAnimator windRushAnimator;

	[SerializeField]
	private AudioAnimator motorAnimator;

	[SerializeField]
	private AudioAnimator grindAnimator;

	[SerializeField]
	private float turnSoundCooldownDuration;

	[SerializeField]
	private float minAngleDeltaForTurnSound;

	private float turnSoundCooldownUntilTimestamp;

	private void Start()
	{
		Stop();
	}

	public void PlayTurnSound(float angle)
	{
		if (Time.time > turnSoundCooldownUntilTimestamp && angle > minAngleDeltaForTurnSound)
		{
			turnSoundCooldownUntilTimestamp = Time.time + turnSoundCooldownDuration;
			turnSounds.Play();
		}
	}

	public void UpdateAudioLoop(float speed, float airspeed, float strainLevel, float grindLevel)
	{
		motorAnimator.UpdateValue(speed);
		windRushAnimator.UpdateValue(airspeed);
		if (grindLevel > 0f)
		{
			grindAnimator.UpdatePitchAndVolume(speed, grindLevel + 0.5f);
		}
		else
		{
			grindAnimator.UpdatePitchAndVolume(0f, 0f);
		}
		strainLevel = Mathf.Clamp01(strainLevel * 10f);
		if (!didInitHum1BaseVolume)
		{
			hum1BaseVolume = hum1.volume;
			didInitHum1BaseVolume = true;
		}
		hum1.volume = Mathf.MoveTowards(hum1.volume, hum1BaseVolume * strainLevel, fadeSpeed * Time.deltaTime);
	}

	public void Stop()
	{
		if (!didInitHum1BaseVolume)
		{
			hum1BaseVolume = hum1.volume;
			didInitHum1BaseVolume = true;
		}
		hum1.volume = 0f;
		windRushAnimator.UpdateValue(0f, ignoreSmoothing: true);
		motorAnimator.UpdateValue(0f, ignoreSmoothing: true);
		grindAnimator.UpdateValue(0f, ignoreSmoothing: true);
	}
}

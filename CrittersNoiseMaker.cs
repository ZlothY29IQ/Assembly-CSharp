using System.Collections;
using UnityEngine;

public class CrittersNoiseMaker : CrittersToolThrowable
{
	[Header("Noise Maker")]
	public int soundSubIndex = 3;

	public bool playOnce = true;

	public float repeatNoiseDuration;

	public float repeatNoiseRate;

	public bool destroyAfterPlayingRepeatNoise = true;

	private Coroutine repeatPlayNoise;

	protected override void OnImpact(Vector3 hitPosition, Vector3 hitNormal)
	{
		if (CrittersManager.instance.LocalAuthority())
		{
			if (destroyOnImpact || playOnce)
			{
				PlaySingleNoise();
			}
			else
			{
				StartPlayingRepeatNoise();
			}
		}
	}

	protected override void OnImpactCritter(CrittersPawn impactedCritter)
	{
		OnImpact(impactedCritter.transform.position, impactedCritter.transform.up);
	}

	protected override void OnPickedUp()
	{
		StopPlayRepeatNoise();
	}

	private void PlaySingleNoise()
	{
		CrittersLoudNoise crittersLoudNoise = (CrittersLoudNoise)CrittersManager.instance.SpawnActor(CrittersActorType.LoudNoise, soundSubIndex);
		if (!(crittersLoudNoise == null))
		{
			crittersLoudNoise.MoveActor(base.transform.position, base.transform.rotation);
			crittersLoudNoise.SetImpulseVelocity(Vector3.zero, Vector3.zero);
			CrittersManager.instance.TriggerEvent(CrittersManager.CritterEvent.NoiseMakerTriggered, actorId, base.transform.position);
		}
	}

	private void StartPlayingRepeatNoise()
	{
		StopPlayRepeatNoise();
		repeatPlayNoise = StartCoroutine(PlayRepeatNoise());
	}

	private void StopPlayRepeatNoise()
	{
		if (repeatPlayNoise != null)
		{
			StopCoroutine(repeatPlayNoise);
			repeatPlayNoise = null;
		}
	}

	private IEnumerator PlayRepeatNoise()
	{
		int num = Mathf.FloorToInt(repeatNoiseDuration / repeatNoiseRate);
		for (int i = num; i > 0; i--)
		{
			PlaySingleNoise();
			yield return new WaitForSeconds(repeatNoiseRate);
		}
		if (destroyAfterPlayingRepeatNoise)
		{
			shouldDisable = true;
		}
	}
}

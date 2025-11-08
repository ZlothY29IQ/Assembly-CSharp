using UnityEngine;

public class GorillaTagCompetitiveRoundBuzzer : MonoBehaviour
{
	public AudioSource audioSource;

	public AudioClip roundCountdownClip;

	public AudioClip roundStartClip;

	public AudioClip roundEndingCountdownClip;

	public int roundEndCountdownDuration = 5;

	public AudioClip roundEndClip;

	public AudioClip needMorePlayerClip;

	private GorillaTagCompetitiveManager.GameState lastState;

	private float lastStateRemainingTime = -1f;

	private void OnEnable()
	{
		GorillaTagCompetitiveManager.onStateChanged += OnStateChanged;
		GorillaTagCompetitiveManager.onUpdateRemainingTime += OnUpdateRemainingTime;
	}

	private void OnDisable()
	{
		GorillaTagCompetitiveManager.onStateChanged -= OnStateChanged;
		GorillaTagCompetitiveManager.onUpdateRemainingTime -= OnUpdateRemainingTime;
	}

	private void OnStateChanged(GorillaTagCompetitiveManager.GameState newState)
	{
		switch (newState)
		{
		case GorillaTagCompetitiveManager.GameState.Playing:
			PlaySFX(roundStartClip);
			break;
		case GorillaTagCompetitiveManager.GameState.PostRound:
			PlaySFX(roundEndClip);
			break;
		case GorillaTagCompetitiveManager.GameState.WaitingForPlayers:
			PlaySFX(needMorePlayerClip);
			break;
		}
		lastState = newState;
	}

	private void OnUpdateRemainingTime(float remainingTime)
	{
		int num = Mathf.CeilToInt(remainingTime);
		int num2 = Mathf.CeilToInt(lastStateRemainingTime);
		if (num != num2)
		{
			switch (lastState)
			{
			case GorillaTagCompetitiveManager.GameState.StartingCountdown:
				if (num > 0)
				{
					PlaySFX(roundCountdownClip);
				}
				break;
			case GorillaTagCompetitiveManager.GameState.Playing:
				if (num > 0 && num <= roundEndCountdownDuration)
				{
					PlaySFX(roundEndingCountdownClip);
				}
				break;
			}
		}
		lastStateRemainingTime = remainingTime;
	}

	private void PlaySFX(AudioClip clip)
	{
		PlaySFX(clip, 1f);
	}

	private void PlaySFX(AudioClip clip, float volume)
	{
		audioSource.PlayOneShot(clip, volume);
	}
}

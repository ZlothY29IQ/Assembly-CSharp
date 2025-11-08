using TMPro;
using UnityEngine;

public class RaceVisual : MonoBehaviour
{
	[SerializeField]
	private TextMeshPro finishLineText;

	[SerializeField]
	private TextMeshPro countdownText;

	[SerializeField]
	private RacingScoreboard[] raceScoreboards;

	[SerializeField]
	private RacingScoreboard raceStartScoreboard;

	[SerializeField]
	private RaceConsoleVisual raceConsoleVisual;

	private float nextVisualRefreshTimestamp;

	private RaceCheckpointManager checkpoints;

	[SerializeField]
	private AudioClip raceEndSound;

	[SerializeField]
	private float countdownSoundGoTime;

	[SerializeField]
	private AudioSource countdownSoundPlayer;

	[SerializeField]
	private GameObject startingWall;

	private int lastDisplayedCountdown;

	private bool isRaceEndSoundEnabled;

	[field: SerializeField]
	public int raceId { get; private set; }

	public bool TickRunning { get; set; }

	private void Awake()
	{
		checkpoints = GetComponent<RaceCheckpointManager>();
		finishLineText.text = "";
		SetScoreboardText("", "");
		SetRaceStartScoreboardText("", "");
	}

	private void OnEnable()
	{
		RacingManager.instance.RegisterVisual(this);
	}

	public void Button_StartRace(int laps)
	{
		RacingManager.instance.Button_StartRace(raceId, laps);
	}

	public void ShowFinishLineText(string text)
	{
		finishLineText.text = text;
	}

	public void UpdateCountdown(int timeRemaining)
	{
		if (timeRemaining != lastDisplayedCountdown)
		{
			countdownText.text = timeRemaining.ToString();
			finishLineText.text = "";
			lastDisplayedCountdown = timeRemaining;
		}
	}

	public void SetScoreboardText(string mainText, string timesText)
	{
		RacingScoreboard[] array = raceScoreboards;
		foreach (RacingScoreboard obj in array)
		{
			obj.mainDisplay.text = mainText;
			obj.timesDisplay.text = timesText;
		}
	}

	public void SetRaceStartScoreboardText(string mainText, string timesText)
	{
		raceStartScoreboard.mainDisplay.text = mainText;
		raceStartScoreboard.timesDisplay.text = timesText;
	}

	public void ActivateStartingWall(bool enable)
	{
		startingWall.SetActive(enable);
	}

	public bool IsPlayerNearCheckpoint(VRRig player, int checkpoint)
	{
		return checkpoints.IsPlayerNearCheckpoint(player, checkpoint);
	}

	public void OnCountdownStart(int laps, float goAfterInterval)
	{
		raceConsoleVisual.ShowRaceInProgress(laps);
		countdownSoundPlayer.Play();
		countdownSoundPlayer.time = countdownSoundGoTime - goAfterInterval;
	}

	public void OnRaceStart()
	{
		finishLineText.text = "GO!";
		checkpoints.OnRaceStart();
		lastDisplayedCountdown = 0;
		startingWall.SetActive(value: false);
		isRaceEndSoundEnabled = false;
	}

	public void OnRaceEnded()
	{
		finishLineText.text = "";
		lastDisplayedCountdown = 0;
		checkpoints.OnRaceEnd();
	}

	public void OnRaceReset()
	{
		raceConsoleVisual.ShowCanStartRace();
	}

	public void EnableRaceEndSound()
	{
		isRaceEndSoundEnabled = true;
	}

	public void OnCheckpointPassed(int index, SoundBankPlayer checkpointSound)
	{
		if (index == 0 && isRaceEndSoundEnabled)
		{
			countdownSoundPlayer.PlayOneShot(raceEndSound);
		}
		else
		{
			checkpointSound.Play();
		}
		RacingManager.instance.OnCheckpointPassed(raceId, index);
	}
}

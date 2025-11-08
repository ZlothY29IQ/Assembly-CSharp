using GameObjectScheduling;
using GorillaNetworking;
using TMPro;
using UnityEngine;

public class ModeSelectButton : GorillaPressableButton
{
	[SerializeField]
	public string gameMode;

	[SerializeField]
	protected PartyGameModeWarning warningScreen;

	[SerializeField]
	private TMP_Text gameModeTitle;

	[SerializeField]
	private GameObject newModeSplash;

	[SerializeField]
	private CountdownText limitedCountdown;

	public PartyGameModeWarning WarningScreen
	{
		get
		{
			return warningScreen;
		}
		set
		{
			warningScreen = value;
		}
	}

	public override void Start()
	{
		base.Start();
		GorillaComputer.instance.currentGameMode.AddCallback(OnGameModeChanged, shouldCallbackNow: true);
	}

	private void OnDestroy()
	{
		if (!ApplicationQuittingState.IsQuitting)
		{
			GorillaComputer.instance.currentGameMode.RemoveCallback(OnGameModeChanged);
		}
	}

	public override void ButtonActivationWithHand(bool isLeftHand)
	{
		base.ButtonActivationWithHand(isLeftHand);
		if (warningScreen.ShouldShowWarning)
		{
			warningScreen.Show();
		}
		else
		{
			GorillaComputer.instance.OnModeSelectButtonPress(gameMode, isLeftHand);
		}
	}

	public void OnGameModeChanged(string newGameMode)
	{
		buttonRenderer.material = ((newGameMode.ToLower() == gameMode.ToLower()) ? pressedMaterial : unpressedMaterial);
	}

	public void SetInfo(string Mode, string ModeTitle, bool NewMode, CountdownTextDate CountdownTo)
	{
		gameModeTitle.text = ModeTitle;
		gameMode = Mode;
		newModeSplash.SetActive(NewMode);
		limitedCountdown.gameObject.SetActive(value: false);
		if (!(CountdownTo == null))
		{
			limitedCountdown.Countdown = CountdownTo;
			limitedCountdown.gameObject.SetActive(value: true);
		}
	}

	public void HideNewAndLimitedTimeInfo()
	{
		limitedCountdown.gameObject.SetActive(value: false);
		newModeSplash.SetActive(value: false);
	}
}

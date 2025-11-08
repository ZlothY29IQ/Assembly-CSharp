using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GorillaTagCompetitiveTimerDisplay : MonoBehaviour
{
	public TextMeshPro timerDisplay;

	public TextMeshPro timerDisplay2;

	public TextMeshPro resultsDisplay;

	public GameObject waitingForPlayersBackground;

	public GameObject startCountdownBackground;

	public Color timerColorStart = Color.white;

	public GameObject playingBackground;

	public Color timerColorPlaying = Color.white;

	public GameObject postRoundBackground;

	public Color timerColorPostRound = Color.white;

	public TextMeshPro[] postRoundTimerText;

	private GorillaTagCompetitiveManager.GameState currentState;

	private GameObject currentBackground;

	private int prevTime = -1;

	[SerializeField]
	private ParticleSystem tintableCelebration;

	[SerializeField]
	private ParticleSystem goldCelebration;

	[SerializeField]
	private ParticleSystem silverCelebration;

	[SerializeField]
	private ParticleSystem bronzeCelebration;

	private VRRig myRig;

	[SerializeField]
	private AudioSource celebrationAudio;

	private void Awake()
	{
		prevTime = -1;
		if ((bool)waitingForPlayersBackground)
		{
			waitingForPlayersBackground.SetActive(value: true);
			currentBackground = waitingForPlayersBackground;
		}
		if ((bool)startCountdownBackground)
		{
			startCountdownBackground.SetActive(value: false);
		}
		if ((bool)playingBackground)
		{
			playingBackground.SetActive(value: false);
		}
		if ((bool)postRoundBackground)
		{
			postRoundBackground.SetActive(value: false);
		}
		timerDisplay.gameObject.SetActive(value: false);
		if ((bool)timerDisplay2)
		{
			timerDisplay2.gameObject.SetActive(value: false);
		}
	}

	private void OnEnable()
	{
		GorillaTagCompetitiveManager.onStateChanged += HandleOnGameStateChanged;
		GorillaTagCompetitiveManager.onUpdateRemainingTime += HandleOnTimeChanged;
		GorillaTagCompetitiveManager gorillaTagCompetitiveManager = GorillaGameManager.instance as GorillaTagCompetitiveManager;
		if (gorillaTagCompetitiveManager != null)
		{
			HandleOnGameStateChanged(gorillaTagCompetitiveManager.GetCurrentGameState());
		}
		myRig = GetComponentInParent<VRRig>();
		DisplayStandardTimer(bShow: false);
	}

	private void OnDisable()
	{
		GorillaTagCompetitiveManager.onStateChanged -= HandleOnGameStateChanged;
		GorillaTagCompetitiveManager.onUpdateRemainingTime -= HandleOnTimeChanged;
	}

	private void HandleOnGameStateChanged(GorillaTagCompetitiveManager.GameState newState)
	{
		SetNewBackground(newState);
		switch (newState)
		{
		case GorillaTagCompetitiveManager.GameState.WaitingForPlayers:
			DisplayStandardTimer(bShow: false);
			resultsDisplay.gameObject.SetActive(value: false);
			break;
		case GorillaTagCompetitiveManager.GameState.StartingCountdown:
		case GorillaTagCompetitiveManager.GameState.Playing:
			DisplayStandardTimer(bShow: true);
			break;
		case GorillaTagCompetitiveManager.GameState.PostRound:
			DoPostRoundShow();
			break;
		}
	}

	private void DisplayStandardTimer(bool bShow)
	{
		if (bShow)
		{
			resultsDisplay.gameObject.SetActive(value: false);
		}
		timerDisplay.gameObject.SetActive(bShow);
		if (timerDisplay2 != null)
		{
			timerDisplay2.gameObject.SetActive(bShow);
		}
	}

	private void DoPostRoundShow()
	{
		GorillaTagCompetitiveManager gorillaTagCompetitiveManager = GorillaGameManager.instance as GorillaTagCompetitiveManager;
		if (gorillaTagCompetitiveManager == null)
		{
			return;
		}
		DisplayStandardTimer(bShow: false);
		resultsDisplay.gameObject.SetActive(value: true);
		List<VRRig> list = new List<VRRig>();
		List<RankedMultiplayerScore.PlayerScoreInRound> sortedScores = gorillaTagCompetitiveManager.GetScoring().GetSortedScores();
		float b = gorillaTagCompetitiveManager.GetScoring().ComputeGameScore(sortedScores[0].NumTags, sortedScores[0].PointsOnDefense);
		for (int i = 0; i < sortedScores.Count && i < 3; i++)
		{
			if (!VRRigCache.Instance.TryGetVrrig(sortedScores[i].PlayerId, out var playerRig))
			{
				continue;
			}
			float a = gorillaTagCompetitiveManager.GetScoring().ComputeGameScore(sortedScores[i].NumTags, sortedScores[i].PointsOnDefense);
			if (i == 0 || a.Approx(b, 0.01f))
			{
				list.Add(playerRig.Rig);
			}
			switch (i)
			{
			case 0:
				if (tintableCelebration != null)
				{
					Color playerColor = playerRig.Rig.playerColor;
					Color.RGBToHSV(playerColor, out var H, out var S, out var V);
					Color max = Color.HSVToRGB(H, S, (V < 0.5f) ? (V + 0.5f) : (V - 0.5f));
					ParticleSystem.MainModule main = tintableCelebration.main;
					main.startColor = new ParticleSystem.MinMaxGradient(playerColor, max);
					tintableCelebration.gameObject.SetActive(value: true);
				}
				if (goldCelebration != null && playerRig.Rig == myRig)
				{
					goldCelebration.gameObject.SetActive(value: true);
				}
				if (celebrationAudio != null)
				{
					celebrationAudio.Play();
				}
				break;
			case 1:
				if (silverCelebration != null && playerRig.Rig == myRig)
				{
					silverCelebration.gameObject.SetActive(value: true);
				}
				if (celebrationAudio != null)
				{
					celebrationAudio.Play();
				}
				break;
			case 2:
				if (bronzeCelebration != null && playerRig.Rig == myRig)
				{
					bronzeCelebration.gameObject.SetActive(value: true);
				}
				if (celebrationAudio != null)
				{
					celebrationAudio.Play();
				}
				break;
			}
		}
		for (int j = 0; j < postRoundTimerText.Length; j++)
		{
			postRoundTimerText[j].text = ((list.Count > 1) ? "SHARED WIN" : "WINNER");
		}
		string text = string.Empty;
		for (int k = 0; k < list.Count; k++)
		{
			text = text + list[k].playerText1.text.ToUpper() + "\n";
		}
		resultsDisplay.text = text.Trim();
		if (timerDisplay2 != null)
		{
			timerDisplay2.text = resultsDisplay.text;
		}
	}

	private void HandleOnTimeChanged(float time)
	{
		int a = Mathf.CeilToInt(time);
		a = Mathf.Max(a, 1);
		if (prevTime == a)
		{
			return;
		}
		prevTime = a;
		if (currentState == GorillaTagCompetitiveManager.GameState.Playing)
		{
			int num = prevTime / 60;
			int num2 = prevTime % 60;
			timerDisplay.text = $"{num}:{num2:D2}";
			if ((bool)timerDisplay2)
			{
				timerDisplay2.text = $"{num}:{num2:D2}";
			}
		}
		else if (currentState != GorillaTagCompetitiveManager.GameState.PostRound)
		{
			timerDisplay.text = prevTime.ToString("#00");
			if ((bool)timerDisplay2)
			{
				timerDisplay2.text = prevTime.ToString("#00");
			}
		}
	}

	private void SetNewBackground(GorillaTagCompetitiveManager.GameState newState)
	{
		if (currentBackground != null)
		{
			currentBackground.SetActive(value: false);
		}
		currentState = newState;
		GameObject gameObject = SelectBackground(newState);
		GetTextColor(newState);
		currentBackground = null;
		if (gameObject != null)
		{
			currentBackground = gameObject;
			currentBackground.SetActive(value: true);
		}
	}

	private GameObject SelectBackground(GorillaTagCompetitiveManager.GameState newState)
	{
		return newState switch
		{
			GorillaTagCompetitiveManager.GameState.StartingCountdown => startCountdownBackground, 
			GorillaTagCompetitiveManager.GameState.Playing => playingBackground, 
			GorillaTagCompetitiveManager.GameState.PostRound => postRoundBackground, 
			GorillaTagCompetitiveManager.GameState.WaitingForPlayers => waitingForPlayersBackground, 
			_ => null, 
		};
	}

	private Color GetTextColor(GorillaTagCompetitiveManager.GameState newState)
	{
		return newState switch
		{
			GorillaTagCompetitiveManager.GameState.StartingCountdown => timerColorStart, 
			GorillaTagCompetitiveManager.GameState.Playing => timerColorPlaying, 
			GorillaTagCompetitiveManager.GameState.PostRound => timerColorPostRound, 
			_ => Color.white, 
		};
	}
}

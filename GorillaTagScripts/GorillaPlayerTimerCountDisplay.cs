using System;
using TMPro;
using UnityEngine;

namespace GorillaTagScripts;

public class GorillaPlayerTimerCountDisplay : MonoBehaviour, ITickSystemTick
{
	[SerializeField]
	private TMP_Text displayText;

	private bool isInitialized;

	public bool TickRunning { get; set; }

	private void Start()
	{
		TryInit();
	}

	private void OnEnable()
	{
		TryInit();
	}

	private void TryInit()
	{
		if (!isInitialized && !(PlayerTimerManager.instance == null))
		{
			PlayerTimerManager.instance.OnTimerStopped.AddListener(OnTimerStopped);
			PlayerTimerManager.instance.OnLocalTimerStarted.AddListener(OnLocalTimerStarted);
			displayText.text = "TIME: --.--.-";
			if (PlayerTimerManager.instance.IsLocalTimerStarted() && !TickRunning)
			{
				TickSystem<object>.AddTickCallback(this);
			}
			isInitialized = true;
		}
	}

	private void OnDisable()
	{
		if (PlayerTimerManager.instance != null)
		{
			PlayerTimerManager.instance.OnTimerStopped.RemoveListener(OnTimerStopped);
			PlayerTimerManager.instance.OnLocalTimerStarted.RemoveListener(OnLocalTimerStarted);
		}
		isInitialized = false;
		if (TickRunning)
		{
			TickSystem<object>.RemoveTickCallback(this);
		}
	}

	private void OnLocalTimerStarted()
	{
		if (!TickRunning)
		{
			TickSystem<object>.AddTickCallback(this);
		}
	}

	private void OnTimerStopped(int actorNum, int timeDelta)
	{
		if (actorNum == NetworkSystem.Instance.LocalPlayer.ActorNumber)
		{
			double value = (double)(uint)timeDelta / 1000.0;
			displayText.text = "TIME: " + TimeSpan.FromSeconds(value).ToString("mm\\:ss\\:f");
			if (TickRunning)
			{
				TickSystem<object>.RemoveTickCallback(this);
			}
		}
	}

	private void UpdateLatestTime()
	{
		float timeForPlayer = PlayerTimerManager.instance.GetTimeForPlayer(NetworkSystem.Instance.LocalPlayer.ActorNumber);
		displayText.text = "TIME: " + TimeSpan.FromSeconds(timeForPlayer).ToString("mm\\:ss\\:f");
	}

	public void Tick()
	{
		UpdateLatestTime();
	}
}

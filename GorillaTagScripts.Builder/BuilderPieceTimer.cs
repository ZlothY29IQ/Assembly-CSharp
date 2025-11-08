using System;
using TMPro;
using UnityEngine;

namespace GorillaTagScripts.Builder;

public class BuilderPieceTimer : MonoBehaviour, IBuilderPieceComponent, ITickSystemTick
{
	[SerializeField]
	private BuilderPiece myPiece;

	[SerializeField]
	private bool isStart;

	[SerializeField]
	private bool isBoth;

	[SerializeField]
	private BuilderSmallHandTrigger buttonTrigger;

	[SerializeField]
	private SoundBankPlayer activateSoundBank;

	[SerializeField]
	private SoundBankPlayer stopSoundBank;

	[SerializeField]
	private float debounceTime = 0.5f;

	private float lastTriggeredTime;

	private double latestTime = 3.4028234663852886E+38;

	[SerializeField]
	private TMP_Text displayText;

	public bool TickRunning { get; set; }

	private void Awake()
	{
		buttonTrigger.TriggeredEvent.AddListener(OnButtonPressed);
	}

	private void OnDestroy()
	{
		if (buttonTrigger != null)
		{
			buttonTrigger.TriggeredEvent.RemoveListener(OnButtonPressed);
		}
	}

	private void OnButtonPressed()
	{
		if (myPiece.state == BuilderPiece.State.AttachedAndPlaced && Time.time > lastTriggeredTime + debounceTime)
		{
			lastTriggeredTime = Time.time;
			if (!isStart && stopSoundBank != null)
			{
				stopSoundBank.Play();
			}
			else if (activateSoundBank != null)
			{
				activateSoundBank.Play();
			}
			if (isBoth && isStart && displayText != null)
			{
				displayText.text = "TIME: 00:00:0";
			}
			PlayerTimerManager.instance.RequestTimerToggle(isStart);
		}
	}

	private void OnTimerStopped(int actorNum, int timeDelta)
	{
		if (isStart && !isBoth)
		{
			return;
		}
		double num = (uint)timeDelta;
		latestTime = num / 1000.0;
		if (latestTime > 3599.989990234375)
		{
			latestTime = 3599.989990234375;
		}
		displayText.text = "TIME: " + TimeSpan.FromSeconds(latestTime).ToString("mm\\:ss\\:ff");
		if (isBoth && actorNum == NetworkSystem.Instance.LocalPlayer.ActorNumber)
		{
			isStart = true;
			if (TickRunning)
			{
				TickSystem<object>.RemoveTickCallback(this);
			}
		}
	}

	private void OnLocalTimerStarted()
	{
		if (isBoth)
		{
			isStart = false;
		}
		if (myPiece.state == BuilderPiece.State.AttachedAndPlaced && !TickRunning)
		{
			TickSystem<object>.AddTickCallback(this);
		}
	}

	private void OnZoneChanged()
	{
		bool active = ZoneManagement.instance.IsZoneActive(myPiece.GetTable().tableZone);
		if (displayText != null)
		{
			displayText.gameObject.SetActive(active);
		}
	}

	public void OnPieceCreate(int pieceType, int pieceId)
	{
		latestTime = double.MaxValue;
		if (displayText != null)
		{
			ZoneManagement instance = ZoneManagement.instance;
			instance.onZoneChanged = (Action)Delegate.Combine(instance.onZoneChanged, new Action(OnZoneChanged));
			OnZoneChanged();
			displayText.text = "TIME: __:__:_";
		}
	}

	public void OnPieceDestroy()
	{
		if (displayText != null)
		{
			ZoneManagement instance = ZoneManagement.instance;
			instance.onZoneChanged = (Action)Delegate.Remove(instance.onZoneChanged, new Action(OnZoneChanged));
		}
	}

	public void OnPiecePlacementDeserialized()
	{
	}

	public void OnPieceActivate()
	{
		lastTriggeredTime = 0f;
		PlayerTimerManager.instance.OnTimerStopped.AddListener(OnTimerStopped);
		PlayerTimerManager.instance.OnLocalTimerStarted.AddListener(OnLocalTimerStarted);
		if (isBoth)
		{
			isStart = !PlayerTimerManager.instance.IsLocalTimerStarted();
			if (!isStart && displayText != null)
			{
				displayText.text = "TIME: __:__:_";
			}
		}
		if (PlayerTimerManager.instance.IsLocalTimerStarted() && !TickRunning)
		{
			TickSystem<object>.AddTickCallback(this);
		}
	}

	public void OnPieceDeactivate()
	{
		if (PlayerTimerManager.instance != null)
		{
			PlayerTimerManager.instance.OnTimerStopped.RemoveListener(OnTimerStopped);
			PlayerTimerManager.instance.OnLocalTimerStarted.RemoveListener(OnLocalTimerStarted);
		}
		if (TickRunning)
		{
			TickSystem<object>.RemoveTickCallback(this);
		}
		if (displayText != null)
		{
			displayText.text = "TIME: --:--:-";
		}
	}

	public void Tick()
	{
		if (displayText != null)
		{
			float timeForPlayer = PlayerTimerManager.instance.GetTimeForPlayer(NetworkSystem.Instance.LocalPlayer.ActorNumber);
			timeForPlayer = Mathf.Clamp(timeForPlayer, 0f, 3599.99f);
			displayText.text = "TIME: " + TimeSpan.FromSeconds(timeForPlayer).ToString("mm\\:ss\\:f");
		}
	}
}

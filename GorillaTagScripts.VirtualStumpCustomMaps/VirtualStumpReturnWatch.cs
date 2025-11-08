using System.Collections;
using GorillaExtensions;
using GorillaGameModes;
using GT_CustomMapSupportRuntime;
using TMPro;
using UnityEngine;

namespace GorillaTagScripts.VirtualStumpCustomMaps;

public class VirtualStumpReturnWatch : MonoBehaviour
{
	[SerializeField]
	private HeldButton returnButton;

	[SerializeField]
	private TMP_Text buttonText;

	[SerializeField]
	private TMP_Text countdownText;

	private static VirtualStumpReturnWatchProps currentCustomMapProps;

	private float startPressingButtonTime = -1f;

	private bool currentlyBeingPressed;

	private Coroutine updateCountdownCoroutine;

	private void Start()
	{
		if (returnButton != null)
		{
			returnButton.onStartPressingButton.AddListener(OnStartedPressingButton);
			returnButton.onStopPressingButton.AddListener(OnStoppedPressingButton);
			returnButton.onPressButton.AddListener(OnButtonPressed);
		}
	}

	private void OnDestroy()
	{
		if (returnButton != null)
		{
			returnButton.onStartPressingButton.RemoveListener(OnStartedPressingButton);
			returnButton.onStopPressingButton.RemoveListener(OnStoppedPressingButton);
			returnButton.onPressButton.RemoveListener(OnButtonPressed);
		}
	}

	public static void SetWatchProperties(VirtualStumpReturnWatchProps props)
	{
		currentCustomMapProps = props;
		currentCustomMapProps.holdDuration = Mathf.Clamp(currentCustomMapProps.holdDuration, 0.5f, 5f);
		currentCustomMapProps.holdDuration_Infection = Mathf.Clamp(currentCustomMapProps.holdDuration_Infection, 0.5f, 5f);
		currentCustomMapProps.holdDuration_Custom = Mathf.Clamp(currentCustomMapProps.holdDuration_Custom, 0.5f, 5f);
	}

	private float GetCurrentHoldDuration()
	{
		if (GorillaGameManager.instance.IsNull())
		{
			return currentCustomMapProps.holdDuration;
		}
		switch (GorillaGameManager.instance.GameType())
		{
		case GameModeType.Infection:
			if (currentCustomMapProps.infectionOverride)
			{
				return currentCustomMapProps.holdDuration_Infection;
			}
			return currentCustomMapProps.holdDuration;
		case GameModeType.Custom:
			if (currentCustomMapProps.customModeOverride)
			{
				return currentCustomMapProps.holdDuration_Custom;
			}
			return currentCustomMapProps.holdDuration;
		default:
			return currentCustomMapProps.holdDuration;
		}
	}

	private void OnStartedPressingButton()
	{
		startPressingButtonTime = Time.time;
		currentlyBeingPressed = true;
		returnButton.pressDuration = GetCurrentHoldDuration();
		ShowCountdownText();
		updateCountdownCoroutine = StartCoroutine(UpdateCountdownText());
	}

	private void OnStoppedPressingButton()
	{
		currentlyBeingPressed = false;
		HideCountdownText();
		if (updateCountdownCoroutine != null)
		{
			StopCoroutine(updateCountdownCoroutine);
			updateCountdownCoroutine = null;
		}
	}

	private void OnButtonPressed()
	{
		currentlyBeingPressed = false;
		if (!ZoneManagement.IsInZone(GTZone.customMaps) || CustomMapManager.IsLocalPlayerInVirtualStump())
		{
			return;
		}
		bool flag = currentCustomMapProps.shouldTagPlayer;
		bool flag2 = currentCustomMapProps.shouldKickPlayer;
		if (GorillaGameManager.instance.IsNotNull())
		{
			switch (GorillaGameManager.instance.GameType())
			{
			case GameModeType.Infection:
				if (currentCustomMapProps.infectionOverride)
				{
					flag = currentCustomMapProps.shouldTagPlayer_Infection;
					flag2 = currentCustomMapProps.shouldKickPlayer_Infection;
				}
				break;
			case GameModeType.Custom:
				if (currentCustomMapProps.customModeOverride)
				{
					flag = currentCustomMapProps.shouldTagPlayer_CustomMode;
					flag2 = currentCustomMapProps.shouldKickPlayer_CustomMode;
				}
				break;
			}
		}
		if (flag2 && NetworkSystem.Instance.InRoom && !NetworkSystem.Instance.SessionIsPrivate)
		{
			NetworkSystem.Instance.ReturnToSinglePlayer();
		}
		else if (flag)
		{
			GameMode.ReportHit();
		}
		CustomMapManager.ReturnToVirtualStump();
	}

	private void ShowCountdownText()
	{
		if (!countdownText.IsNull())
		{
			int num = 1 + Mathf.FloorToInt(GetCurrentHoldDuration());
			countdownText.text = num.ToString();
			countdownText.gameObject.SetActive(value: true);
			if (buttonText.IsNotNull())
			{
				buttonText.gameObject.SetActive(value: false);
			}
		}
	}

	private void HideCountdownText()
	{
		if (!countdownText.IsNull())
		{
			countdownText.text = "";
			countdownText.gameObject.SetActive(value: false);
			if (buttonText.IsNotNull())
			{
				buttonText.gameObject.SetActive(value: true);
			}
		}
	}

	private IEnumerator UpdateCountdownText()
	{
		while (currentlyBeingPressed && !countdownText.IsNull())
		{
			float f = GetCurrentHoldDuration() - (Time.time - startPressingButtonTime);
			int num = 1 + Mathf.FloorToInt(f);
			countdownText.text = num.ToString();
			yield return null;
		}
	}
}

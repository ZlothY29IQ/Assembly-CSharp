using TMPro;
using UnityEngine;

public class CustomMapsTerminalControlButton : CustomMapsScreenTouchPoint
{
	[SerializeField]
	private TMP_Text bttnText;

	[SerializeField]
	private string unlockedText = "TERMINAL AVAILABLE";

	[SerializeField]
	private string lockedText = "TERMINAL UNAVAILABLE";

	[SerializeField]
	private float unlockedFontSize = 30f;

	[SerializeField]
	private float lockedFontSize = 30f;

	[SerializeField]
	private Color unlockedTextColor = Color.black;

	[SerializeField]
	private Color lockedTextColor = Color.white;

	private bool isLocked;

	[SerializeField]
	private CustomMapsTerminal mapsTerminal;

	public bool IsLocked
	{
		get
		{
			return isLocked;
		}
		set
		{
			isLocked = value;
		}
	}

	protected override void OnButtonPressedEvent()
	{
		GTDev.Log("terminal control pressed");
		if (!(mapsTerminal == null))
		{
			mapsTerminal.HandleTerminalControlButtonPressed();
		}
	}

	public void LockTerminalControl()
	{
		if (!IsLocked)
		{
			IsLocked = true;
			PressButtonColourUpdate();
		}
	}

	public void UnlockTerminalControl()
	{
		if (IsLocked)
		{
			IsLocked = false;
			PressButtonColourUpdate();
		}
	}

	public override void PressButtonColourUpdate()
	{
		bttnText.fontSize = (isLocked ? lockedFontSize : unlockedFontSize);
		bttnText.text = (isLocked ? lockedText : unlockedText);
		bttnText.color = (isLocked ? lockedTextColor : unlockedTextColor);
		touchPointRenderer.color = (isLocked ? buttonColorSettings.PressedColor : buttonColorSettings.UnpressedColor);
	}
}

using GorillaExtensions;
using TMPro;
using UnityEngine;

public class CustomMapsScreenButton : CustomMapsScreenTouchPoint
{
	[SerializeField]
	private TMP_Text bttnText;

	[SerializeField]
	private bool isToggle;

	private bool isActive;

	protected override void OnDisable()
	{
		base.OnDisable();
		if (isToggle)
		{
			SetButtonActive(isActive);
		}
		else
		{
			isActive = false;
		}
	}

	public void SetButtonText(string text)
	{
		if (!bttnText.IsNull())
		{
			bttnText.text = text;
		}
	}

	public void SetButtonActive(bool active)
	{
		isActive = active;
		touchPointRenderer.color = (isActive ? buttonColorSettings.PressedColor : buttonColorSettings.UnpressedColor);
	}

	public override void PressButtonColourUpdate()
	{
		if (!isToggle)
		{
			base.PressButtonColourUpdate();
		}
	}

	protected override void OnButtonPressedEvent()
	{
		isActive = !isActive;
	}
}

using GorillaTagScripts.VirtualStumpCustomMaps.UI;

public class CustomMapsKeyToggleButton : CustomMapsKeyButton
{
	private bool isPressed;

	public override void PressButtonColourUpdate()
	{
	}

	public void SetButtonStatus(bool newIsPressed)
	{
		if (isPressed != newIsPressed)
		{
			isPressed = newIsPressed;
			propBlock.SetColor("_BaseColor", isPressed ? ButtonColorSettings.PressedColor : ButtonColorSettings.UnpressedColor);
			propBlock.SetColor("_Color", isPressed ? ButtonColorSettings.PressedColor : ButtonColorSettings.UnpressedColor);
			ButtonRenderer.SetPropertyBlock(propBlock);
		}
	}
}

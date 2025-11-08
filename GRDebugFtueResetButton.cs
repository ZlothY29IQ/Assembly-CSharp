using UnityEngine;

public class GRDebugFtueResetButton : GorillaPressableReleaseButton
{
	public bool availableOnLive;

	private void Awake()
	{
		if (!availableOnLive)
		{
			base.gameObject.SetActive(value: false);
		}
	}

	public void OnPressedButton()
	{
		PlayerPrefs.SetString("spawnInWrongStump", "flagged");
		PlayerPrefs.Save();
	}

	public override void ButtonActivation()
	{
		base.ButtonActivation();
		isOn = true;
		UpdateColor();
	}

	public override void ButtonDeactivation()
	{
		base.ButtonDeactivation();
		isOn = false;
		UpdateColor();
	}
}

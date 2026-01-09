using UnityEngine;

public class GorillaHatButtonParent : MonoBehaviour
{
	public GorillaHatButton[] hatButtons;

	public GameObject[] adminObjects;

	public string hat;

	public string face;

	public string badge;

	public string leftHandHold;

	public string rightHandHold;

	public bool initialized;

	public GorillaLevelScreen screen;

	public void Start()
	{
		hat = PlayerPrefs.GetString("hatCosmetic", "none");
		face = PlayerPrefs.GetString("faceCosmetic", "none");
		badge = PlayerPrefs.GetString("badgeCosmetic", "none");
		leftHandHold = PlayerPrefs.GetString("leftHandHoldCosmetic", "none");
		rightHandHold = PlayerPrefs.GetString("rightHandHoldCosmetic", "none");
	}

	public void LateUpdate()
	{
		if (initialized || !GorillaTagger.Instance.offlineVRRig.InitializedCosmetics)
		{
			return;
		}
		initialized = true;
		if (GorillaTagger.Instance.offlineVRRig.HasCosmetic("AdministratorBadge"))
		{
			GameObject[] array = adminObjects;
			foreach (GameObject obj in array)
			{
				Debug.Log("doing this?");
				obj.SetActive(value: true);
			}
		}
		if (GorillaTagger.Instance.offlineVRRig.HasCosmetic("earlyaccess"))
		{
			UpdateButtonState();
			screen.UpdateText("WELCOME TO THE HAT ROOM!\nTHANK YOU FOR PURCHASING THE EARLY ACCESS SUPPORTER PACK! PLEASE ENJOY THESE VARIOUS HATS AND NOT-HATS!", setToGoodMaterial: true);
		}
	}

	public void PressButton(bool isOn, GorillaHatButton.HatButtonType buttonType, string buttonValue)
	{
		if (!initialized || !GorillaTagger.Instance.offlineVRRig.HasCosmetic("earlyaccess"))
		{
			return;
		}
		switch (buttonType)
		{
		case GorillaHatButton.HatButtonType.Face:
			if (face != buttonValue)
			{
				face = buttonValue;
				PlayerPrefs.SetString("faceCosmetic", buttonValue);
			}
			else
			{
				face = "none";
				PlayerPrefs.SetString("faceCosmetic", "none");
			}
			break;
		case GorillaHatButton.HatButtonType.Badge:
			if (badge != buttonValue)
			{
				badge = buttonValue;
				PlayerPrefs.SetString("badgeCosmetic", buttonValue);
			}
			else
			{
				badge = "none";
				PlayerPrefs.SetString("badgeCosmetic", "none");
			}
			break;
		case GorillaHatButton.HatButtonType.Hat:
			if (hat != buttonValue)
			{
				hat = buttonValue;
				PlayerPrefs.SetString("hatCosmetic", buttonValue);
			}
			else
			{
				hat = "none";
				PlayerPrefs.SetString("hatCosmetic", "none");
			}
			break;
		}
		PlayerPrefs.Save();
		UpdateButtonState();
	}

	private void UpdateButtonState()
	{
		GorillaHatButton[] array = hatButtons;
		foreach (GorillaHatButton gorillaHatButton in array)
		{
			switch (gorillaHatButton.buttonType)
			{
			case GorillaHatButton.HatButtonType.Hat:
				gorillaHatButton.isOn = gorillaHatButton.cosmeticName == hat;
				break;
			case GorillaHatButton.HatButtonType.Face:
				gorillaHatButton.isOn = gorillaHatButton.cosmeticName == face;
				break;
			case GorillaHatButton.HatButtonType.Badge:
				gorillaHatButton.isOn = gorillaHatButton.cosmeticName == badge;
				break;
			}
			gorillaHatButton.UpdateColor();
		}
	}
}

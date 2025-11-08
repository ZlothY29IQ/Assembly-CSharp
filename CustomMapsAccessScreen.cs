using GorillaNetworking;
using TMPro;
using UnityEngine;

public class CustomMapsAccessScreen : CustomMapsTerminalScreen
{
	[SerializeField]
	private TMP_Text errorText;

	[SerializeField]
	private TMP_Text terminalControlPromptText;

	[SerializeField]
	private bool isControlScreen = true;

	[SerializeField]
	private string defaultText = "PRESS THE 'TERMINAL AVAILABLE' BUTTON TO PROCEED.";

	private string detailsScreenText = "\nMAP DETAILS WILL APPEAR HERE WHEN A MAP IS SELECTED.";

	private string displayedText = string.Empty;

	private bool useNametags;

	private void LateUpdate()
	{
		if (CustomMapsTerminal.GetDriverID() != -2 && !CustomMapsTerminal.IsDriver && !(GorillaComputer.instance == null) && useNametags != GorillaComputer.instance.NametagsEnabled)
		{
			useNametags = GorillaComputer.instance.NametagsEnabled;
			SetDriverName();
		}
	}

	public override void Initialize()
	{
	}

	public override void Show()
	{
		base.Show();
		if (displayedText == string.Empty)
		{
			displayedText = defaultText;
		}
		errorText.gameObject.SetActive(value: false);
		terminalControlPromptText.gameObject.SetActive(value: true);
		terminalControlPromptText.text = displayedText;
	}

	public override void Hide()
	{
		errorText.gameObject.SetActive(value: false);
		terminalControlPromptText.gameObject.SetActive(value: false);
		base.Hide();
	}

	public void Reset()
	{
		errorText.gameObject.SetActive(value: false);
		terminalControlPromptText.gameObject.SetActive(value: true);
		displayedText = defaultText;
	}

	public void SetDetailsScreenForDriver()
	{
		displayedText = detailsScreenText;
	}

	public void SetDriverName()
	{
		bool flag = KIDManager.HasPermissionToUseFeature(EKIDFeatures.Custom_Nametags);
		string text;
		if (NetworkSystem.Instance.InRoom)
		{
			NetPlayer netPlayerByID = NetworkSystem.Instance.GetNetPlayerByID(CustomMapsTerminal.GetDriverID());
			text = netPlayerByID.DefaultName;
			if (useNametags && flag)
			{
				RigContainer playerRig;
				if (netPlayerByID.IsLocal)
				{
					text = netPlayerByID.NickName;
				}
				else if (VRRigCache.Instance.TryGetVrrig(netPlayerByID, out playerRig))
				{
					text = playerRig.Rig.playerNameVisible;
				}
			}
		}
		else
		{
			text = ((useNametags && flag) ? NetworkSystem.Instance.LocalPlayer.NickName : NetworkSystem.Instance.LocalPlayer.DefaultName);
		}
		displayedText = "TERMINAL CONTROLLED BY: " + text;
		if (!isControlScreen)
		{
			displayedText += detailsScreenText;
		}
		terminalControlPromptText.text = displayedText;
	}

	public void DisplayError(string errorMessage)
	{
		terminalControlPromptText.gameObject.SetActive(value: false);
		errorText.text = errorMessage;
		errorText.gameObject.SetActive(value: true);
	}
}

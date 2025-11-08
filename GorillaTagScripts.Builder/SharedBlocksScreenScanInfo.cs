using TMPro;
using UnityEngine;

namespace GorillaTagScripts.Builder;

public class SharedBlocksScreenScanInfo : SharedBlocksScreen
{
	[SerializeField]
	private TMP_Text mapIDText;

	public override void OnUpPressed()
	{
	}

	public override void OnDownPressed()
	{
	}

	public override void OnSelectPressed()
	{
		terminal.OnLoadMapPressed();
	}

	public override void Show()
	{
		base.Show();
		DrawScreen();
	}

	private void DrawScreen()
	{
		if (terminal.SelectedMap == null)
		{
			mapIDText.text = "MAP ID: NONE";
		}
		else
		{
			mapIDText.text = "MAP ID: " + SharedBlocksTerminal.MapIDToDisplayedString(terminal.SelectedMap.MapID);
		}
	}
}

using UnityEngine;

public class RaceConsoleVisual : MonoBehaviour
{
	[SerializeField]
	private MeshRenderer button1;

	[SerializeField]
	private MeshRenderer button3;

	[SerializeField]
	private MeshRenderer button5;

	[SerializeField]
	private Vector3 buttonPressedOffset;

	[SerializeField]
	private Material pressableButton;

	[SerializeField]
	private Material selectedButton;

	[SerializeField]
	private Material inactiveButton;

	public void ShowRaceInProgress(int laps)
	{
		button1.sharedMaterial = inactiveButton;
		button3.sharedMaterial = inactiveButton;
		button5.sharedMaterial = inactiveButton;
		button1.transform.localPosition = Vector3.zero;
		button3.transform.localPosition = Vector3.zero;
		button5.transform.localPosition = Vector3.zero;
		switch (laps)
		{
		default:
			button1.sharedMaterial = selectedButton;
			button1.transform.localPosition = buttonPressedOffset;
			break;
		case 3:
			button3.sharedMaterial = selectedButton;
			button3.transform.localPosition = buttonPressedOffset;
			break;
		case 5:
			button5.sharedMaterial = selectedButton;
			button5.transform.localPosition = buttonPressedOffset;
			break;
		}
	}

	public void ShowCanStartRace()
	{
		button1.transform.localPosition = Vector3.zero;
		button3.transform.localPosition = Vector3.zero;
		button5.transform.localPosition = Vector3.zero;
		button1.sharedMaterial = pressableButton;
		button3.sharedMaterial = pressableButton;
		button5.sharedMaterial = pressableButton;
	}
}

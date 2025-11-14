using UnityEngine;

public class SIAutoPressButtonOnAwake : MonoBehaviour
{
	private SICombinedTerminal terminalParent;

	private SITouchscreenButton button;

	private float awakeTime;

	private bool buttonPressed;

	public float delay = 2f;

	private void Awake()
	{
		button = GetComponent<SITouchscreenButton>();
		terminalParent = button.GetComponentInParent<SICombinedTerminal>();
	}

	private void OnEnable()
	{
		if (!(button == null))
		{
			awakeTime = Time.time;
			buttonPressed = false;
		}
	}

	private void Update()
	{
		if (!buttonPressed && !(Time.time < awakeTime + delay))
		{
			if (terminalParent.activePlayer.ActorNr == SIPlayer.LocalPlayer.ActorNr)
			{
				button.PressButton();
			}
			buttonPressed = true;
		}
	}
}

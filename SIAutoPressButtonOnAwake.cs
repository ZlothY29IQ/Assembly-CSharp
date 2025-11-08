using UnityEngine;

public class SIAutoPressButtonOnAwake : MonoBehaviour
{
	private SITouchscreenButton button;

	private float awakeTime;

	private bool buttonPressed;

	public float delay = 2f;

	private void Awake()
	{
		button = GetComponent<SITouchscreenButton>();
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
			button.PressButton();
			buttonPressed = true;
		}
	}
}

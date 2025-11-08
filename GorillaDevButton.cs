using UnityEngine;

public class GorillaDevButton : GorillaPressableButton
{
	public DevButtonType Type;

	public LogType levelType;

	public DevConsoleInstance targetConsole;

	public int lineNumber;

	public bool repeatIfHeld;

	public float holdForSeconds;

	private Coroutine pressCoroutine;

	public bool on
	{
		get
		{
			return isOn;
		}
		set
		{
			if (isOn != value)
			{
				isOn = value;
				UpdateColor();
			}
		}
	}

	public void OnEnable()
	{
		UpdateColor();
	}
}

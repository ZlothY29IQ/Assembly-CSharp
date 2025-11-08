using GorillaTagScripts.VirtualStumpCustomMaps.UI;
using UnityEngine;

public abstract class CustomMapsTerminalScreen : MonoBehaviour
{
	public CustomMapsKeyboard terminalKeyboard;

	[SerializeField]
	protected float activationTime = 0.25f;

	protected float showTime;

	public abstract void Initialize();

	public virtual void Show()
	{
		if (!base.gameObject.activeSelf)
		{
			base.gameObject.SetActive(value: true);
			terminalKeyboard?.OnKeyPressed.AddListener(PressButton);
		}
		showTime = Time.time;
	}

	public virtual void Hide()
	{
		if (base.gameObject.activeSelf)
		{
			base.gameObject.SetActive(value: false);
			terminalKeyboard?.OnKeyPressed.RemoveListener(PressButton);
		}
		showTime = 0f;
	}

	public virtual void PressButton(CustomMapKeyboardBinding pressedButton)
	{
	}
}

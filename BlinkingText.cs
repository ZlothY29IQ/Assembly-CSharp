using UnityEngine;
using UnityEngine.UI;

public class BlinkingText : MonoBehaviour
{
	public float cycleTime;

	public float dutyCycle;

	private bool isOn;

	private float lastTime;

	private Text textComponent;

	private void Awake()
	{
		textComponent = GetComponent<Text>();
	}

	private void Update()
	{
		if (isOn && Time.time > lastTime + cycleTime * dutyCycle)
		{
			isOn = false;
			textComponent.enabled = false;
		}
		else if (!isOn && Time.time > lastTime + cycleTime)
		{
			lastTime = Time.time;
			isOn = true;
			textComponent.enabled = true;
		}
	}
}

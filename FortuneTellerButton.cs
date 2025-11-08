using System.Collections;
using UnityEngine;

public class FortuneTellerButton : GorillaPressableButton
{
	[SerializeField]
	private float durationPressed = 0.25f;

	[SerializeField]
	private Vector3 pressedOffset = new Vector3(0f, 0f, 0.1f);

	private float pressTime;

	private Vector3 startingPos;

	public void Awake()
	{
		startingPos = base.transform.localPosition;
	}

	public override void ButtonActivation()
	{
		PressButtonUpdate();
	}

	public void PressButtonUpdate()
	{
		if (pressTime == 0f)
		{
			base.transform.localPosition = startingPos + pressedOffset;
			buttonRenderer.material = pressedMaterial;
			pressTime = Time.time;
			StartCoroutine(ButtonColorUpdate_Local());
		}
		IEnumerator ButtonColorUpdate_Local()
		{
			yield return new WaitForSeconds(durationPressed);
			if (pressTime != 0f && Time.time > durationPressed + pressTime)
			{
				base.transform.localPosition = startingPos;
				buttonRenderer.material = unpressedMaterial;
				pressTime = 0f;
			}
		}
	}
}

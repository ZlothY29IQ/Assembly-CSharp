using System.Collections;
using UnityEngine;

namespace GorillaTag;

public class CustomMapTestingScript : GorillaPressableButton
{
	public override void ButtonActivation()
	{
		base.ButtonActivation();
		StartCoroutine(ButtonPressed_Local());
	}

	private IEnumerator ButtonPressed_Local()
	{
		isOn = true;
		UpdateColor();
		yield return new WaitForSeconds(debounceTime);
		isOn = false;
		UpdateColor();
	}
}

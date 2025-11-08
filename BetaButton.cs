using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BetaButton : GorillaPressableButton
{
	public GameObject betaParent;

	public int count;

	public float buttonFadeTime = 0.25f;

	public Text messageText;

	public override void ButtonActivation()
	{
		base.ButtonActivation();
		count++;
		StartCoroutine(ButtonColorUpdate());
		if (count >= 10)
		{
			betaParent.SetActive(value: false);
			PlayerPrefs.SetString("CheckedBox2", "true");
			PlayerPrefs.Save();
		}
	}

	private IEnumerator ButtonColorUpdate()
	{
		buttonRenderer.material = pressedMaterial;
		yield return new WaitForSeconds(buttonFadeTime);
		buttonRenderer.material = unpressedMaterial;
	}
}

using UnityEngine;

public class KIDUI_AgeAppealScreen : MonoBehaviour
{
	public void OnDisable()
	{
		KIDAudioManager.Instance?.PlaySoundWithDelay(KIDAudioManager.KIDSoundType.PageTransition);
	}

	public void ShowRestrictedAccessScreen()
	{
		base.gameObject.SetActive(value: true);
	}

	public void OnChangeAgePressed()
	{
		base.gameObject.SetActive(value: false);
	}
}

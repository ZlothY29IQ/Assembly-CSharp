using System.Threading;
using UnityEngine;

public class KIDUI_AgeAppealScreen : MonoBehaviour
{
	[SerializeField]
	private KIDUIButton _changeAgeButton;

	[SerializeField]
	private int _minimumDelay = 1000;

	private string _submittedEmailAddress;

	private CancellationTokenSource _cancellationTokenSource;

	private void Awake()
	{
	}

	private void OnEnable()
	{
	}

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

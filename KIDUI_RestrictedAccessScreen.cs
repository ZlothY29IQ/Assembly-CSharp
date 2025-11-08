using UnityEngine;

public class KIDUI_RestrictedAccessScreen : MonoBehaviour
{
	[SerializeField]
	private KIDAgeAppeal _ageAppealScreen;

	[SerializeField]
	private GameObject _pendingStatusIndicator;

	[SerializeField]
	private GameObject _prohibitedStatusIndicator;

	public void ShowRestrictedAccessScreen(SessionStatus? sessionStatus)
	{
		base.gameObject.SetActive(value: true);
		_pendingStatusIndicator.SetActive(value: false);
		_prohibitedStatusIndicator.SetActive(value: false);
		if (sessionStatus.HasValue && sessionStatus.HasValue)
		{
			switch (sessionStatus.GetValueOrDefault())
			{
			case SessionStatus.PROHIBITED:
				_prohibitedStatusIndicator.SetActive(value: true);
				break;
			case SessionStatus.PENDING_AGE_APPEAL:
				_pendingStatusIndicator.SetActive(value: true);
				break;
			case SessionStatus.PASS:
			case SessionStatus.CHALLENGE:
			case SessionStatus.CHALLENGE_SESSION_UPGRADE:
				break;
			}
		}
	}

	public void OnChangeAgePressed()
	{
		PrivateUIRoom.RemoveUI(base.transform);
		base.gameObject.SetActive(value: false);
		_ageAppealScreen.ShowAgeAppealScreen();
	}

	public void OnDisable()
	{
		KIDAudioManager.Instance?.PlaySoundWithDelay(KIDAudioManager.KIDSoundType.PageTransition);
	}
}

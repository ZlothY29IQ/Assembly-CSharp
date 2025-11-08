using GorillaGameModes;
using UnityEngine;

public class GorillaGuardianEjectWatch : MonoBehaviour
{
	[SerializeField]
	private HeldButton ejectButton;

	private void Start()
	{
		if (ejectButton != null)
		{
			ejectButton.onPressButton.AddListener(OnEjectButtonPressed);
		}
	}

	private void OnDestroy()
	{
		if (ejectButton != null)
		{
			ejectButton.onPressButton.RemoveListener(OnEjectButtonPressed);
		}
	}

	private void OnEjectButtonPressed()
	{
		if (GameMode.ActiveGameMode is GorillaGuardianManager gorillaGuardianManager)
		{
			gorillaGuardianManager.RequestEjectGuardian(NetworkSystem.Instance.LocalPlayer);
		}
	}
}

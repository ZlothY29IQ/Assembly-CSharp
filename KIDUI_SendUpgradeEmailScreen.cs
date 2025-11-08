using System.Collections.Generic;
using System.Threading.Tasks;
using KID.Model;
using UnityEngine;

public class KIDUI_SendUpgradeEmailScreen : MonoBehaviour
{
	[SerializeField]
	private KIDUI_AnimatedEllipsis _animatedEllipsis;

	[SerializeField]
	private KIDUI_MessageScreen _successScreen;

	[SerializeField]
	private KIDUI_MessageScreen _errorScreen;

	[SerializeField]
	private KIDUI_MainScreen _mainScreen;

	public async Task SendUpgradeEmail(List<string> requestedPermissions)
	{
		if (requestedPermissions.Count == 0)
		{
			Debug.Log("[KID] Tried requesting 0 permissions. Skipping upgrade email flow.");
			_mainScreen.ShowMainScreen(EMainScreenStatus.Pending);
			return;
		}
		base.gameObject.SetActive(value: true);
		_animatedEllipsis.StartAnimation();
		UpgradeSessionData upgradeSessionData = await KIDManager.TryUpgradeSession(requestedPermissions);
		if (upgradeSessionData == null)
		{
			OnFailure("We couldn't get to your information. Please contact Customer Support");
			Debug.LogError("[KID] UpgradeSessionData response was null. Maybe banned.");
		}
		else if (upgradeSessionData.status == SessionStatus.PASS)
		{
			OnSuccess();
		}
		else if (upgradeSessionData.status == SessionStatus.CHALLENGE_SESSION_UPGRADE)
		{
			if (KIDManager.CurrentSession.ManagedBy == Session.ManagedByEnum.PLAYER)
			{
				base.gameObject.SetActive(value: false);
				return;
			}
			var (flag, errorMessage) = await KIDManager.TrySendUpgradeSessionChallengeEmail();
			if (flag)
			{
				OnSuccess();
			}
			else
			{
				OnFailure(errorMessage);
			}
		}
		else
		{
			Debug.LogError("[KID] Unexpected session status when upgrading session: " + upgradeSessionData.status);
			OnFailure(null);
		}
	}

	public void OnCancel()
	{
		base.gameObject.SetActive(value: false);
		_mainScreen.ShowMainScreen(EMainScreenStatus.None);
	}

	private void OnSuccess()
	{
		base.gameObject.SetActive(value: false);
		_successScreen.Show(null);
	}

	private void OnFailure(string errorMessage)
	{
		base.gameObject.SetActive(value: false);
		_errorScreen.Show(errorMessage);
	}
}

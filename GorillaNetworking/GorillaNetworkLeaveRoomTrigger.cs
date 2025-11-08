using System.Threading.Tasks;
using GorillaTagScripts;
using UnityEngine;

namespace GorillaNetworking;

public class GorillaNetworkLeaveRoomTrigger : GorillaTriggerBox
{
	[SerializeField]
	private bool excludePrivateRooms;

	public override void OnBoxTriggered()
	{
		base.OnBoxTriggered();
		if (NetworkSystem.Instance.InRoom && (!excludePrivateRooms || !NetworkSystem.Instance.SessionIsPrivate))
		{
			if (FriendshipGroupDetection.Instance.IsInParty)
			{
				FriendshipGroupDetection.Instance.LeaveParty();
				DisconnectAfterDelay(1f);
			}
			else
			{
				NetworkSystem.Instance.ReturnToSinglePlayer();
			}
		}
	}

	private async void DisconnectAfterDelay(float seconds)
	{
		await Task.Delay((int)(1000f * seconds));
		await NetworkSystem.Instance.ReturnToSinglePlayer();
	}
}

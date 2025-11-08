using Photon.Pun;
using UnityEngine;

namespace GorillaTagScripts.ObstacleCourse;

public class TappableBell : Tappable
{
	public delegate void ObstacleCourseTriggerEvent(VRRig vrrig);

	private VRRig winnerRig;

	public CallLimiter rpcCooldown;

	public event ObstacleCourseTriggerEvent OnTapped;

	public override void OnTapLocal(float tapStrength, float tapTime, PhotonMessageInfoWrapped info)
	{
		if (PhotonNetwork.LocalPlayer.IsMasterClient && rpcCooldown.CheckCallTime(Time.time))
		{
			winnerRig = GorillaGameManager.StaticFindRigForPlayer(info.Sender);
			if ((object)winnerRig != null)
			{
				this.OnTapped?.Invoke(winnerRig);
			}
		}
	}
}

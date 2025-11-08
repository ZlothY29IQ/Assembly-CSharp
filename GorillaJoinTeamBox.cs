using Photon.Pun;
using UnityEngine;

public class GorillaJoinTeamBox : GorillaTriggerBox
{
	public bool joinRedTeam;

	public override void OnBoxTriggered()
	{
		base.OnBoxTriggered();
		if (GameObject.FindGameObjectWithTag("GorillaGameManager").GetComponent<GorillaGameManager>() != null)
		{
			_ = PhotonNetwork.InRoom;
		}
	}
}

using UnityEngine;

public class MonkeBallTeamZoneSelector : MonoBehaviour
{
	public int teamId;

	private void OnTriggerEnter(Collider other)
	{
		GameBallPlayer gamePlayer = GameBallPlayer.GetGamePlayer(other, bodyOnly: true);
		if (gamePlayer != null && gamePlayer.IsLocalPlayer() && gamePlayer.teamId != teamId)
		{
			MonkeBallGame.Instance.RequestSetTeam(teamId);
		}
	}
}

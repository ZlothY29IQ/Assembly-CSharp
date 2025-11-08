using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class MonkeBallGoalZone : MonoBehaviourTick
{
	public int teamId;

	public List<MonkeBallPlayer> playersInGoalZone;

	public override void Tick()
	{
		if (!PhotonNetwork.IsMasterClient || MonkeBallGame.Instance.GetGameState() != MonkeBallGame.GameState.Playing)
		{
			return;
		}
		for (int i = 0; i < playersInGoalZone.Count; i++)
		{
			MonkeBallPlayer monkeBallPlayer = playersInGoalZone[i];
			if (monkeBallPlayer.gamePlayer.teamId == teamId)
			{
				continue;
			}
			GameBallId gameBallId = monkeBallPlayer.gamePlayer.GetGameBallId();
			if (gameBallId.IsValid())
			{
				MonkeBallGame.Instance.RequestScore(monkeBallPlayer.gamePlayer.teamId);
				GameBallId gameBallId2 = monkeBallPlayer.gamePlayer.GetGameBallId();
				int otherTeam = MonkeBallGame.Instance.GetOtherTeam(monkeBallPlayer.gamePlayer.teamId);
				if (MonkeBallGame.Instance.resetBallPositionOnScore)
				{
					MonkeBallGame.Instance.RequestResetBall(gameBallId2, otherTeam);
				}
				MonkeBallGame.Instance.RequestRestrictBallToTeamOnScore(gameBallId2, otherTeam);
				monkeBallPlayer.gamePlayer.ClearGrabbedIfHeld(gameBallId);
			}
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		GameBallPlayer gamePlayer = GameBallPlayer.GetGamePlayer(other, bodyOnly: true);
		if (gamePlayer != null && gamePlayer.teamId != teamId)
		{
			MonkeBallPlayer component = gamePlayer.GetComponent<MonkeBallPlayer>();
			if (component != null)
			{
				component.currGoalZone = this;
				playersInGoalZone.Add(component);
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		GameBallPlayer gamePlayer = GameBallPlayer.GetGamePlayer(other, bodyOnly: true);
		if (gamePlayer != null && gamePlayer.teamId != teamId)
		{
			MonkeBallPlayer component = gamePlayer.GetComponent<MonkeBallPlayer>();
			if (component != null)
			{
				component.currGoalZone = null;
				playersInGoalZone.Remove(component);
			}
		}
	}

	public void CleanupPlayer(MonkeBallPlayer player)
	{
		playersInGoalZone.Remove(player);
	}
}

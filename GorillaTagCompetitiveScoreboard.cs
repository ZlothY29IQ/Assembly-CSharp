using System.Collections.Generic;
using UnityEngine;

public class GorillaTagCompetitiveScoreboard : MonoBehaviour
{
	public enum PredictedResult
	{
		Great,
		Good,
		Even,
		Bad,
		Poor
	}

	public GorillaTagCompetitiveScoreboardLine[] lines;

	public GameObject waitingForPlayers;

	public float smallEloDelta = 10f;

	public float largeEloDelta = 25f;

	private void Awake()
	{
		GorillaTagCompetitiveManager.RegisterScoreboard(this);
		for (int i = 0; i < lines.Length; i++)
		{
			lines[i].gameObject.SetActive(value: false);
		}
	}

	private void OnDestroy()
	{
		GorillaTagCompetitiveManager.DeregisterScoreboard(this);
	}

	public void UpdateScores(GorillaTagCompetitiveManager.GameState gameState, float activeRoundTime, List<RankedMultiplayerScore.PlayerScoreInRound> scores, Dictionary<int, int> PlayerRankedTiers, Dictionary<int, float> PlayerPredictedEloDeltas, List<NetPlayer> infectedPlayers, RankedProgressionManager progressionManager)
	{
		waitingForPlayers.SetActive(gameState == GorillaTagCompetitiveManager.GameState.WaitingForPlayers);
		for (int i = 0; i < lines.Length; i++)
		{
			if (gameState != GorillaTagCompetitiveManager.GameState.WaitingForPlayers && scores != null && scores.Count > i)
			{
				RankedMultiplayerScore.PlayerScoreInRound playerScoreInRound = scores[i];
				NetPlayer netPlayerByID = NetworkSystem.Instance.GetNetPlayerByID(playerScoreInRound.PlayerId);
				if (netPlayerByID == null)
				{
					continue;
				}
				lines[i].gameObject.SetActive(value: true);
				if (PlayerRankedTiers == null || !PlayerRankedTiers.ContainsKey(playerScoreInRound.PlayerId))
				{
					lines[i].SetPlayer(netPlayerByID.SanitizedNickName, null);
				}
				else
				{
					lines[i].SetPlayer(netPlayerByID.SanitizedNickName, progressionManager.GetProgressionRankIcon(PlayerRankedTiers[playerScoreInRound.PlayerId]));
				}
				if (playerScoreInRound.TaggedTime.Approx(0f))
				{
					lines[i].SetScore(Mathf.Max(activeRoundTime - playerScoreInRound.JoinTime, 0f), playerScoreInRound.NumTags);
				}
				else
				{
					lines[i].SetScore(Mathf.Max(playerScoreInRound.TaggedTime - playerScoreInRound.JoinTime, 0f), playerScoreInRound.NumTags);
				}
				if (PlayerPredictedEloDeltas.ContainsKey(playerScoreInRound.PlayerId))
				{
					float num = PlayerPredictedEloDeltas[playerScoreInRound.PlayerId];
					PredictedResult predictedResult = PredictedResult.Even;
					if (num > largeEloDelta)
					{
						predictedResult = PredictedResult.Great;
					}
					else if (num > smallEloDelta)
					{
						predictedResult = PredictedResult.Good;
					}
					else if (num < 0f - largeEloDelta)
					{
						predictedResult = PredictedResult.Poor;
					}
					else if (num < 0f - smallEloDelta)
					{
						predictedResult = PredictedResult.Bad;
					}
					lines[i].SetPredictedResult(predictedResult);
				}
				lines[i].SetInfected(gameState == GorillaTagCompetitiveManager.GameState.Playing && infectedPlayers.Contains(netPlayerByID));
			}
			else
			{
				lines[i].gameObject.SetActive(value: false);
			}
		}
	}

	public void DisplayPredictedResults(bool bShow)
	{
		for (int i = 0; i < lines.Length; i++)
		{
			lines[i].DisplayPredictedResults(bShow);
		}
	}
}

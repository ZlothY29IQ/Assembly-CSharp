using System.Collections.Generic;
using UnityEngine;

namespace GorillaTag.Sports;

public class SportGoalTrigger : MonoBehaviour
{
	[SerializeField]
	private SportScoreboard scoreboard;

	[SerializeField]
	private int teamScoringOnThisGoal = 1;

	[SerializeField]
	private float ballTriggerExitDistanceFallback = 3f;

	private HashSet<SportBall> ballsPendingTriggerExit = new HashSet<SportBall>();

	public void BallExitedGoalTrigger(SportBall ball)
	{
		if (ballsPendingTriggerExit.Contains(ball))
		{
			ballsPendingTriggerExit.Remove(ball);
		}
	}

	private void PruneBallsPendingTriggerExitByDistance()
	{
		foreach (SportBall item in ballsPendingTriggerExit)
		{
			if ((item.transform.position - base.transform.position).sqrMagnitude > ballTriggerExitDistanceFallback * ballTriggerExitDistanceFallback)
			{
				ballsPendingTriggerExit.Remove(item);
			}
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		SportBall componentInParent = other.GetComponentInParent<SportBall>();
		if (componentInParent != null && scoreboard != null)
		{
			PruneBallsPendingTriggerExitByDistance();
			if (!ballsPendingTriggerExit.Contains(componentInParent))
			{
				scoreboard.TeamScored(teamScoringOnThisGoal);
				ballsPendingTriggerExit.Add(componentInParent);
			}
		}
	}
}

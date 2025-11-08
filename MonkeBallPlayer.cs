using UnityEngine;

public class MonkeBallPlayer : MonoBehaviour
{
	public GameBallPlayer gamePlayer;

	public MonkeBallGoalZone currGoalZone;

	private void Awake()
	{
		if (gamePlayer == null)
		{
			gamePlayer = GetComponent<GameBallPlayer>();
		}
	}
}

using Photon.Pun;
using UnityEngine;

public class MonkeBallBallResetTrigger : MonoBehaviour
{
	public Renderer trigger;

	public Material[] teamMaterials;

	public Material neutralMaterial;

	private GameBall _lastBall;

	private void OnTriggerEnter(Collider other)
	{
		GameBall component = other.transform.GetComponent<GameBall>();
		if (!(component != null))
		{
			return;
		}
		GameBallPlayer gameBallPlayer = ((component.heldByActorNumber < 0) ? null : GameBallPlayer.GetGamePlayer(component.heldByActorNumber));
		if (gameBallPlayer == null)
		{
			gameBallPlayer = ((component.lastHeldByActorNumber < 0) ? null : GameBallPlayer.GetGamePlayer(component.lastHeldByActorNumber));
			if (gameBallPlayer == null)
			{
				return;
			}
		}
		_lastBall = component;
		int num = gameBallPlayer.teamId;
		if (num == -1)
		{
			num = component.lastHeldByTeamId;
		}
		if (num >= 0 && num < teamMaterials.Length)
		{
			trigger.sharedMaterial = teamMaterials[num];
		}
		if (PhotonNetwork.IsMasterClient)
		{
			MonkeBallGame.Instance.ToggleResetButton(toggle: true, num);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		GameBall component = other.transform.GetComponent<GameBall>();
		if (component != null)
		{
			if (component == _lastBall)
			{
				trigger.sharedMaterial = neutralMaterial;
				_lastBall = null;
			}
			if (PhotonNetwork.IsMasterClient)
			{
				MonkeBallGame.Instance.ToggleResetButton(toggle: false, -1);
			}
		}
	}
}

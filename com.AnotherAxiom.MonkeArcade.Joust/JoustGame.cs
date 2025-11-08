using UnityEngine;

namespace com.AnotherAxiom.MonkeArcade.Joust;

public class JoustGame : ArcadeGame
{
	[SerializeField]
	private JoustPlayer[] joustPlayers;

	public override byte[] GetNetworkState()
	{
		return new byte[0];
	}

	public override void SetNetworkState(byte[] obj)
	{
	}

	protected override void ButtonDown(int player, ArcadeButtons button)
	{
		switch (button)
		{
		case ArcadeButtons.TRIGGER:
			joustPlayers[player].Flap();
			break;
		case ArcadeButtons.GRAB:
			joustPlayers[player].gameObject.SetActive(value: true);
			break;
		}
	}

	protected override void ButtonUp(int player, ArcadeButtons button)
	{
		if (button == ArcadeButtons.GRAB)
		{
			joustPlayers[player].gameObject.SetActive(value: false);
		}
	}

	private void Start()
	{
		for (int i = 0; i < joustPlayers.Length; i++)
		{
			joustPlayers[i].gameObject.SetActive(value: false);
		}
	}

	private void Update()
	{
		for (int i = 0; i < joustPlayers.Length; i++)
		{
			if (joustPlayers[i].gameObject.activeInHierarchy)
			{
				int num = (getButtonState(i, ArcadeButtons.LEFT) ? (-1) : 0) + (getButtonState(i, ArcadeButtons.RIGHT) ? 1 : 0);
				joustPlayers[i].HorizontalSpeed = Mathf.Clamp(joustPlayers[i].HorizontalSpeed + (float)num * Time.deltaTime, -1f, 1f);
			}
		}
	}

	public override void OnTimeout()
	{
	}
}

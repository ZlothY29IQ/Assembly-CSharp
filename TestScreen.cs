using UnityEngine;

public class TestScreen : ArcadeGame
{
	[SerializeField]
	private SpriteRenderer[] lights;

	[SerializeField]
	private Transform dot;

	public override byte[] GetNetworkState()
	{
		return null;
	}

	public override void SetNetworkState(byte[] b)
	{
	}

	private int buttonToLightIndex(int player, ArcadeButtons button)
	{
		int num = 0;
		switch (button)
		{
		case ArcadeButtons.GRAB:
			num = 0;
			break;
		case ArcadeButtons.UP:
			num = 1;
			break;
		case ArcadeButtons.DOWN:
			num = 2;
			break;
		case ArcadeButtons.LEFT:
			num = 3;
			break;
		case ArcadeButtons.RIGHT:
			num = 4;
			break;
		case ArcadeButtons.B0:
			num = 5;
			break;
		case ArcadeButtons.B1:
			num = 6;
			break;
		case ArcadeButtons.TRIGGER:
			num = 7;
			break;
		}
		return (player * 8 + num) % lights.Length;
	}

	protected override void ButtonUp(int player, ArcadeButtons button)
	{
		lights[buttonToLightIndex(player, button)].color = Color.red;
	}

	protected override void ButtonDown(int player, ArcadeButtons button)
	{
		lights[buttonToLightIndex(player, button)].color = Color.green;
	}

	public override void OnTimeout()
	{
	}
}

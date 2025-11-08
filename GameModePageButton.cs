using UnityEngine;

public class GameModePageButton : GorillaPressableButton
{
	[SerializeField]
	private GameModePages selector;

	[SerializeField]
	private bool left;

	public override void ButtonActivation()
	{
		base.ButtonActivation();
		selector.ChangePage(left);
	}
}

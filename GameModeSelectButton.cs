using UnityEngine;

public class GameModeSelectButton : GorillaPressableButton
{
	[SerializeField]
	internal GameModePages selector;

	[SerializeField]
	internal int buttonIndex;

	public override void ButtonActivation()
	{
		base.ButtonActivation();
		selector.SelectEntryOnPage(buttonIndex);
	}
}

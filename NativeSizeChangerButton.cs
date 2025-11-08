using UnityEngine;

public class NativeSizeChangerButton : GorillaPressableButton
{
	[SerializeField]
	private NativeSizeChanger nativeSizeChanger;

	[SerializeField]
	private NativeSizeChangerSettings settings;

	public override void ButtonActivation()
	{
		nativeSizeChanger.Activate(settings);
	}
}

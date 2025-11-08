using UnityEngine;

public class SoundPostMuteButton : GorillaPressableButton
{
	public SynchedMusicController[] musicControllers;

	[Tooltip("If true, then this button will passthrough clicks to a connected SoundPostMuteButton.")]
	public bool IsDummyButton;

	[SerializeField]
	[Tooltip("The targetted SoundPostMuteButton if this is a dummy button.")]
	private SoundPostMuteButton _targetMuteButton;

	public override void ButtonActivation()
	{
		base.ButtonActivation();
		if (!IsDummyButton)
		{
			SynchedMusicController[] array = musicControllers;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].MuteAudio(this);
			}
		}
		else if (_targetMuteButton != null)
		{
			_targetMuteButton.ButtonActivation();
		}
	}
}

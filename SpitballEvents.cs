using UnityEngine;

public class SpitballEvents : SubEmitterListener
{
	[SerializeField]
	private AudioSource _audioSource;

	[SerializeField]
	private AudioClip _sfxHit;

	protected override void OnSubEmit()
	{
		base.OnSubEmit();
		if ((bool)_audioSource && (bool)_sfxHit)
		{
			_audioSource.GTPlayOneShot(_sfxHit);
		}
	}
}

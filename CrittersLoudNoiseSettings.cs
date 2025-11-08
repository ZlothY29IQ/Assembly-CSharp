public class CrittersLoudNoiseSettings : CrittersActorSettings
{
	public float _soundVolume;

	public float _soundDuration;

	public bool _soundEnabled;

	public bool _disableWhenSoundDisabled;

	public float _volumeFearAttractionMultiplier = 1f;

	public override void UpdateActorSettings()
	{
		base.UpdateActorSettings();
		CrittersLoudNoise obj = (CrittersLoudNoise)parentActor;
		obj.soundVolume = _soundVolume;
		obj.soundDuration = _soundDuration;
		obj.soundEnabled = _soundEnabled;
		obj.disableWhenSoundDisabled = _disableWhenSoundDisabled;
		obj.volumeFearAttractionMultiplier = _volumeFearAttractionMultiplier;
	}
}

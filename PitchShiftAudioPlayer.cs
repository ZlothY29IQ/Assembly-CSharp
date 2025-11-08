using UnityEngine;

public class PitchShiftAudioPlayer : MonoBehaviour
{
	public bool apply = true;

	[SerializeField]
	private AudioSource _source;

	[SerializeField]
	private AudioMixVarPool _pitchMixVars;

	[SerializeReference]
	private AudioMixVar _pitchMix;

	[SerializeField]
	private RangedFloat _pitch;

	private void Awake()
	{
		if (_source == null)
		{
			_source = GetComponent<AudioSource>();
		}
		if (_pitch == null)
		{
			_pitch = GetComponent<RangedFloat>();
		}
	}

	private void OnEnable()
	{
		_pitchMixVars.Rent(out _pitchMix);
		_source.outputAudioMixerGroup = _pitchMix.group;
	}

	private void OnDisable()
	{
		_source.Stop();
		_source.outputAudioMixerGroup = null;
		_pitchMix?.ReturnToPool();
	}

	private void Update()
	{
		if (apply)
		{
			ApplyPitch();
		}
	}

	private void ApplyPitch()
	{
		_pitchMix.value = _pitch.curved;
	}
}

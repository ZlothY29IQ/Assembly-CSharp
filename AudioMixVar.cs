using System;
using UnityEngine;
using UnityEngine.Audio;

[Serializable]
public class AudioMixVar
{
	public AudioMixerGroup group;

	public AudioMixer mixer;

	public string name;

	[NonSerialized]
	public bool taken;

	[SerializeField]
	private AudioMixVarPool _pool;

	public float value
	{
		get
		{
			if (!group)
			{
				return 0f;
			}
			if (!mixer)
			{
				return 0f;
			}
			if (!mixer.GetFloat(name, out var result))
			{
				return 0f;
			}
			return result;
		}
		set
		{
			if ((bool)mixer)
			{
				mixer.SetFloat(name, value);
			}
		}
	}

	public void ReturnToPool()
	{
		if (_pool != null)
		{
			_pool.Return(this);
		}
	}
}

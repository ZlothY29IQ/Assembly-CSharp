using System;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleEffect : MonoBehaviour
{
	public ParticleSystem system;

	[SerializeField]
	private long _effectID;

	public ParticleEffectsPool pool;

	[NonSerialized]
	public int poolIndex = -1;

	public long effectID => _effectID;

	public bool isPlaying
	{
		get
		{
			if ((bool)system)
			{
				return system.isPlaying;
			}
			return false;
		}
	}

	public virtual void Play()
	{
		base.gameObject.SetActive(value: true);
		system.Play(withChildren: true);
	}

	public virtual void Stop()
	{
		system.Stop(withChildren: true);
		base.gameObject.SetActive(value: false);
	}

	private void OnParticleSystemStopped()
	{
		base.gameObject.SetActive(value: false);
		if ((bool)pool)
		{
			pool.Return(this);
		}
	}
}

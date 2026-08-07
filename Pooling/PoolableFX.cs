using UnityEngine;
using UnityEngine.Pool;

namespace Pooling;

public class PoolableFX : MonoBehaviour, IPoolable<PoolableFX>
{
	[SerializeField]
	private ParticleSystem particles;

	public IObjectPool<PoolableFX> Pool { get; set; }

	private void Reset()
	{
		particles = GetComponent<ParticleSystem>();
		if ((bool)particles)
		{
			ParticleSystem.MainModule main = particles.main;
			main.playOnAwake = false;
			main.stopAction = ParticleSystemStopAction.Callback;
		}
	}

	public void Stop()
	{
		particles.Stop();
	}

	public void OnCreate()
	{
	}

	public void OnPreGet()
	{
	}

	public void OnPostGet()
	{
		if ((bool)particles)
		{
			particles.Play();
		}
	}

	public void OnRelease()
	{
	}

	private void OnParticleSystemStopped()
	{
		this.Release();
	}
}

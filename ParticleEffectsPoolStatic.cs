using UnityEngine;

public class ParticleEffectsPoolStatic<T> : ParticleEffectsPool where T : ParticleEffectsPool
{
	protected static T gInstance;

	public static T Instance => gInstance;

	protected override void OnPoolAwake()
	{
		if ((bool)gInstance && gInstance != this)
		{
			Object.Destroy(this);
		}
		else
		{
			gInstance = this as T;
		}
	}
}

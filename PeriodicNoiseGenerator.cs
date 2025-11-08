using UnityEngine;

public class PeriodicNoiseGenerator : MonoBehaviour
{
	public float sleepDuration;

	public float randomDuration;

	public float lastTime;

	private CrittersLoudNoise noiseActor;

	public Material transparent;

	public Material solid;

	private MeshRenderer mR;

	private void Awake()
	{
		noiseActor = GetComponentInParent<CrittersLoudNoise>();
		lastTime = Time.time;
		mR = GetComponentInChildren<MeshRenderer>();
	}

	private void Update()
	{
		if (CrittersManager.instance.LocalAuthority())
		{
			if (Time.time > lastTime + sleepDuration)
			{
				lastTime = Time.time + randomDuration * Random.value;
				noiseActor.SetTimeEnabled();
				noiseActor.soundEnabled = true;
				mR.sharedMaterial = solid;
			}
			if (!noiseActor.soundEnabled && mR.sharedMaterial != transparent)
			{
				mR.sharedMaterial = transparent;
			}
		}
	}
}

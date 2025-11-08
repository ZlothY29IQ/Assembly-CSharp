using UnityEngine;

public class GRFadeAndDestroyLight : MonoBehaviour
{
	public float TimeToFade = 10f;

	private float fadeRate;

	public GameLight gameLight;

	public float timeSlice = 0.1f;

	public float timeSinceLastUpdate;

	private void Start()
	{
		if (gameLight != null)
		{
			fadeRate = gameLight.light.intensity / TimeToFade;
		}
		timeSinceLastUpdate = Time.time;
	}

	public void OnEnable()
	{
	}

	public void OnDisable()
	{
	}

	public void Update()
	{
		if (Time.time < timeSinceLastUpdate || Time.time > timeSinceLastUpdate + timeSlice)
		{
			timeSinceLastUpdate = Time.time;
			float intensity = gameLight.light.intensity;
			intensity -= timeSlice * fadeRate;
			if (intensity <= 0f)
			{
				base.gameObject.Destroy();
			}
			else
			{
				gameLight.light.intensity = intensity;
			}
		}
	}
}

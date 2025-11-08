using UnityEngine;

public class AmbientSoundRandomizer : MonoBehaviour
{
	[SerializeField]
	private AudioSource[] audioSources;

	[SerializeField]
	private AudioClip[] audioClips;

	[SerializeField]
	private float baseTime = 15f;

	[SerializeField]
	private float randomModifier = 5f;

	private float timer;

	private float timerTarget;

	private void Button_Cache()
	{
		audioSources = GetComponentsInChildren<AudioSource>();
	}

	private void Awake()
	{
		SetTarget();
	}

	private void Update()
	{
		if (timer >= timerTarget)
		{
			int num = Random.Range(0, audioSources.Length);
			int num2 = Random.Range(0, audioClips.Length);
			audioSources[num].clip = audioClips[num2];
			audioSources[num].GTPlay();
			SetTarget();
		}
		else
		{
			timer += Time.deltaTime;
		}
	}

	private void SetTarget()
	{
		timerTarget = baseTime + Random.Range(0f, randomModifier);
		timer = 0f;
	}
}

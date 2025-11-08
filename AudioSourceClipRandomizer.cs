using GorillaNetworking;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioSourceClipRandomizer : MonoBehaviour
{
	[SerializeField]
	private AudioClip[] clips;

	private AudioSource source;

	private bool playOnAwake;

	private void Awake()
	{
		source = GetComponent<AudioSource>();
		playOnAwake = source.playOnAwake;
		source.playOnAwake = false;
	}

	public void Play()
	{
		int num = Random.Range(0, 60);
		if (GorillaComputer.instance != null)
		{
			num = GorillaComputer.instance.GetServerTime().Second;
		}
		source.clip = clips[num % clips.Length];
		source.GTPlay();
	}

	private void OnEnable()
	{
		if (playOnAwake)
		{
			Play();
		}
	}
}

using UnityEngine;
using UnityEngine.Audio;

public class ReverbTrigger : MonoBehaviour
{
	[SerializeField]
	private AudioMixer mixer;

	[SerializeField]
	private AudioMixerSnapshot targetSnapshot;

	[SerializeField]
	private AudioMixerSnapshot normalSnapshot;

	[SerializeField]
	private Collider reverbTrigger;

	[SerializeField]
	private float transitionTime = 1f;

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.layer == 8)
		{
			targetSnapshot.TransitionTo(transitionTime);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.gameObject.layer == 8)
		{
			normalSnapshot.TransitionTo(transitionTime);
		}
	}
}

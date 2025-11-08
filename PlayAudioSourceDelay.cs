using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayAudioSourceDelay : MonoBehaviour
{
	[SerializeField]
	private float _delay;

	public IEnumerator Start()
	{
		yield return new WaitForSecondsRealtime(_delay);
		GetComponent<AudioSource>().GTPlay();
	}
}

using System.Collections;
using System.Collections.Generic;
using GorillaLocomotion;
using UnityEngine;
using UnityEngine.Events;

public class CosmeticPlaySoundOnColision : MonoBehaviour
{
	[GorillaSoundLookup]
	[SerializeField]
	private int defaultSound = 1;

	[SerializeField]
	private SoundIdRemapping[] soundIdRemappings;

	[SerializeField]
	private UnityEvent OnStartPlayback;

	[SerializeField]
	private UnityEvent OnStopPlayback;

	[SerializeField]
	private float minSpeed = 0.1f;

	private TransferrableObject transferrableObject;

	private Dictionary<int, int> soundLookup;

	private AudioSource audioSource;

	private Coroutine crWaitForStopPlayback;

	private float speed;

	private Vector3 previousFramePosition;

	[SerializeField]
	private bool invokeEventsOnAllClients;

	[SerializeField]
	private bool invokeEventOnOverideSound = true;

	[SerializeField]
	private bool invokeEventOnDefaultSound;

	private void Awake()
	{
		transferrableObject = GetComponentInParent<TransferrableObject>();
		soundLookup = new Dictionary<int, int>();
		audioSource = GetComponent<AudioSource>();
		for (int i = 0; i < soundIdRemappings.Length; i++)
		{
			soundLookup.Add(soundIdRemappings[i].SoundIn, soundIdRemappings[i].SoundOut);
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (speed >= minSpeed && other.TryGetComponent<GorillaSurfaceOverride>(out var component))
		{
			if (soundLookup.TryGetValue(component.overrideIndex, out var value))
			{
				playSound(value, invokeEventOnOverideSound);
			}
			else
			{
				playSound(defaultSound, invokeEventOnDefaultSound);
			}
		}
	}

	private void playSound(int soundIndex, bool invokeEvent)
	{
		if (soundIndex <= -1 || soundIndex >= GTPlayer.Instance.materialData.Count)
		{
			return;
		}
		if (audioSource.isPlaying)
		{
			audioSource.GTStop();
			if (invokeEventsOnAllClients || transferrableObject.IsMyItem())
			{
				OnStopPlayback.Invoke();
			}
			if (crWaitForStopPlayback != null)
			{
				StopCoroutine(crWaitForStopPlayback);
				crWaitForStopPlayback = null;
			}
		}
		audioSource.clip = GTPlayer.Instance.materialData[soundIndex].audio;
		audioSource.GTPlay();
		if (invokeEvent && (invokeEventsOnAllClients || transferrableObject.IsMyItem()))
		{
			OnStartPlayback.Invoke();
			crWaitForStopPlayback = StartCoroutine(waitForStopPlayback());
		}
	}

	private IEnumerator waitForStopPlayback()
	{
		while (audioSource.isPlaying)
		{
			yield return null;
		}
		if (invokeEventsOnAllClients || transferrableObject.IsMyItem())
		{
			OnStopPlayback.Invoke();
		}
		crWaitForStopPlayback = null;
	}

	private void FixedUpdate()
	{
		speed = Vector3.Distance(base.transform.position, previousFramePosition) * Time.fixedDeltaTime * 100f;
		previousFramePosition = base.transform.position;
	}
}

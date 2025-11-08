using System.Collections.Generic;
using GorillaExtensions;
using UnityEngine;
using UnityEngine.Serialization;

public class VirtualStumpBarrierSFX : MonoBehaviour
{
	[SerializeField]
	private AudioSource barrierAudioSource;

	[FormerlySerializedAs("teleportingPlayerSoundClips")]
	[SerializeField]
	private List<AudioClip> PassThroughBarrierSoundClips = new List<AudioClip>();

	private Dictionary<GameObject, bool> trackedGameObjects = new Dictionary<GameObject, bool>();

	public void OnTriggerEnter(Collider other)
	{
		VRRig component;
		if (other.gameObject == GorillaTagger.Instance.headCollider.gameObject)
		{
			PlaySFX();
		}
		else if (other.gameObject.TryGetComponent<VRRig>(out component) && !component.isLocal)
		{
			bool value = other.gameObject.transform.position.z < base.gameObject.transform.position.z;
			trackedGameObjects.Add(other.gameObject, value);
			OnTriggerStay(other);
		}
	}

	public void OnTriggerStay(Collider other)
	{
		if (trackedGameObjects.TryGetValue(other.gameObject, out var value))
		{
			bool flag = other.gameObject.transform.position.z < base.gameObject.transform.position.z;
			if (value != flag)
			{
				PlaySFX();
				trackedGameObjects.Remove(other.gameObject);
			}
		}
	}

	public void OnTriggerExit(Collider other)
	{
		if (trackedGameObjects.TryGetValue(other.gameObject, out var value))
		{
			bool flag = other.gameObject.transform.position.z < base.gameObject.transform.position.z;
			if (value != flag)
			{
				PlaySFX();
			}
			trackedGameObjects.Remove(other.gameObject);
		}
	}

	public void PlaySFX()
	{
		if (!barrierAudioSource.IsNull() && !PassThroughBarrierSoundClips.IsNullOrEmpty())
		{
			barrierAudioSource.clip = PassThroughBarrierSoundClips[Random.Range(0, PassThroughBarrierSoundClips.Count)];
			barrierAudioSource.Play();
		}
	}
}

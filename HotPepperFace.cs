using UnityEngine;

public class HotPepperFace : MonoBehaviour
{
	[SerializeField]
	private GameObject _faceMesh;

	[SerializeField]
	private ParticleSystem _fireFX;

	[SerializeField]
	private AudioSource _flameSpeaker;

	[SerializeField]
	private AudioSource _breathSpeaker;

	[SerializeField]
	private float _effectLength = 1.5f;

	[SerializeField]
	private GameObject _thermalSourceVolume;

	public void PlayFX(float delay)
	{
		if (delay < 0f)
		{
			PlayFX();
		}
		else
		{
			Invoke("PlayFX", delay);
		}
	}

	public void PlayFX()
	{
		_faceMesh.SetActive(value: true);
		_thermalSourceVolume.SetActive(value: true);
		_fireFX.Play();
		_flameSpeaker.GTPlay();
		_breathSpeaker.GTPlay();
		Invoke("StopFX", _effectLength);
	}

	public void StopFX()
	{
		_faceMesh.SetActive(value: false);
		_thermalSourceVolume.SetActive(value: false);
		_fireFX.Stop();
		_flameSpeaker.GTStop();
		_breathSpeaker.GTStop();
	}
}

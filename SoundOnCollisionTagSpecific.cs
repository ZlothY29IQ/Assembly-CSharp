using UnityEngine;

public class SoundOnCollisionTagSpecific : MonoBehaviour
{
	public string tagName;

	public float noiseCooldown = 1f;

	private float nextSound;

	public AudioSource audioSource;

	public AudioClip[] collisionSounds;

	private void OnTriggerEnter(Collider collider)
	{
		if (Time.time > nextSound && collider.gameObject.CompareTag(tagName))
		{
			nextSound = Time.time + noiseCooldown;
			audioSource.GTPlayOneShot(collisionSounds[Random.Range(0, collisionSounds.Length)], 0.5f);
		}
	}
}

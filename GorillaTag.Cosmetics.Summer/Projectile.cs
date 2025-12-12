using System.Collections.Generic;
using GorillaTag.Reactions;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTag.Cosmetics.Summer;

public class Projectile : MonoBehaviour, IProjectile
{
	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private GameObject impactEffect;

	[SerializeField]
	private AudioClip launchAudio;

	[SerializeField]
	private LayerMask collisionLayerMasks;

	[SerializeField]
	private List<string> collisionTags = new List<string>();

	[SerializeField]
	private bool destroyOnCollisionEnter;

	[SerializeField]
	private float destroyDelay = 1f;

	[Tooltip("Distance from the surface that the particle should spawn.")]
	[SerializeField]
	private float impactEffectOffset = 0.1f;

	[SerializeField]
	private SpawnWorldEffects spawnWorldEffects;

	private ConstantForce forceComponent;

	public UnityEvent<float> onLaunchShared;

	public UnityEvent onImpactShared;

	private bool impactEffectSpawned;

	private Rigidbody rigidbody;

	protected void Awake()
	{
		rigidbody = GetComponent<Rigidbody>();
		impactEffectSpawned = false;
		forceComponent = GetComponent<ConstantForce>();
	}

	protected void OnEnable()
	{
	}

	public void Launch(Vector3 startPosition, Quaternion startRotation, Vector3 velocity, float chargeFrac, VRRig ownerRig, int progressStep)
	{
		Transform obj = base.transform;
		obj.SetPositionAndRotation(startPosition, startRotation);
		obj.localScale = Vector3.one * ownerRig.scaleFactor;
		if (rigidbody != null)
		{
			rigidbody.isKinematic = false;
			rigidbody.position = startPosition;
			rigidbody.rotation = startRotation;
			rigidbody.linearVelocity = velocity;
		}
		if ((bool)audioSource && (bool)launchAudio)
		{
			audioSource.GTPlayOneShot(launchAudio);
		}
		onLaunchShared?.Invoke(chargeFrac);
	}

	private bool IsTagValid(GameObject obj)
	{
		return collisionTags.Contains(obj.tag);
	}

	private void HandleImpact(GameObject hitObject, Vector3 hitPosition, Vector3 hitNormal)
	{
		if (impactEffectSpawned || (collisionTags.Count > 0 && !IsTagValid(hitObject)) || ((1 << hitObject.layer) & (int)collisionLayerMasks) == 0)
		{
			return;
		}
		SpawnImpactEffect(impactEffect, hitPosition, hitNormal);
		if (impactEffect != null)
		{
			SoundBankPlayer component = impactEffect.GetComponent<SoundBankPlayer>();
			if (component != null && !component.playOnEnable)
			{
				component.Play();
			}
		}
		impactEffectSpawned = true;
		if (destroyOnCollisionEnter)
		{
			if (destroyDelay > 0f)
			{
				Invoke("DestroyProjectile", destroyDelay);
			}
			else
			{
				DestroyProjectile();
			}
		}
	}

	private void GetColliderHitInfo(Collider other, out Vector3 position, out Vector3 normal)
	{
		Vector3 vector = Time.fixedDeltaTime * 2f * rigidbody.linearVelocity;
		Vector3 origin = base.transform.position - vector;
		float magnitude = vector.magnitude;
		other.Raycast(new Ray(origin, vector / magnitude), out var hitInfo, 2f * magnitude);
		position = hitInfo.point;
		normal = hitInfo.normal;
	}

	private void OnCollisionEnter(Collision other)
	{
		ContactPoint contact = other.GetContact(0);
		HandleImpact(other.gameObject, contact.point, contact.normal);
	}

	private void OnCollisionStay(Collision other)
	{
		ContactPoint contact = other.GetContact(0);
		HandleImpact(other.gameObject, contact.point, contact.normal);
	}

	private void OnTriggerEnter(Collider other)
	{
		GetColliderHitInfo(other, out var position, out var normal);
		HandleImpact(other.gameObject, position, normal);
	}

	private void OnTriggerStay(Collider other)
	{
		Transform transform = base.transform;
		HandleImpact(other.gameObject, transform.position, -transform.forward);
	}

	private void SpawnImpactEffect(GameObject prefab, Vector3 position, Vector3 normal)
	{
		if (prefab != null)
		{
			Vector3 position2 = position + normal * impactEffectOffset;
			GameObject obj = ObjectPools.instance.Instantiate(prefab, position2);
			obj.transform.up = normal;
			obj.transform.position = position2;
		}
		onImpactShared.Invoke();
		if (spawnWorldEffects != null)
		{
			spawnWorldEffects.RequestSpawn(position, normal);
		}
	}

	private void DestroyProjectile()
	{
		impactEffectSpawned = false;
		if ((bool)forceComponent)
		{
			forceComponent.enabled = false;
		}
		if (ObjectPools.instance.DoesPoolExist(base.gameObject))
		{
			ObjectPools.instance.Destroy(base.gameObject);
		}
		else
		{
			Object.Destroy(base.gameObject);
		}
	}
}

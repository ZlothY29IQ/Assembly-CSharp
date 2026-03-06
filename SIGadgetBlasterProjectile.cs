using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SIGadgetProjectileType))]
[RequireComponent(typeof(Rigidbody))]
public class SIGadgetBlasterProjectile : MonoBehaviourTick
{
	[OnEnterPlay_SetNull]
	public static Dictionary<int, List<GameObject>> blasterProjectileExplosionPools;

	[OnEnterPlay_SetNull]
	public static Dictionary<GameObject, int> explosionTypeKey;

	[NonSerialized]
	public int poolId;

	public SIGadgetProjectileType projectileType;

	public Rigidbody rb;

	public GameObject hitEffect;

	public GameObject hitEffectPlayer;

	public float maxLifetime = 10f;

	[NonSerialized]
	public float timeSpawned;

	public float hapticHitStrength = 0.75f;

	public float hapticHitDuration = 0.1f;

	[NonSerialized]
	public SIGadgetBlaster parentBlaster;

	[NonSerialized]
	public int projectileId;

	[NonSerialized]
	public SIPlayer firedByPlayer;

	public float startingVelocity;

	public const float EXCLUSION_ZONE_MINIMUM_LIFETIME = 0.02f;

	public GameObject exclusionZoneDespawnEffect;

	private AudioSource audioSource;

	public override void Tick()
	{
		if (Time.time > timeSpawned + maxLifetime)
		{
			parentBlaster.DespawnProjectile(this);
		}
	}

	public void InitializeProjectile()
	{
		rb.angularVelocity = Vector3.zero;
		rb.linearVelocity = base.transform.forward * startingVelocity;
		timeSpawned = Time.realtimeSinceStartup;
		if (audioSource == null)
		{
			audioSource = GetComponentInChildren<AudioSource>();
		}
		audioSource.time = 0f;
		projectileType = GetComponent<SIGadgetProjectileType>();
		SIGadgetProjectileModifier[] components = GetComponents<SIGadgetProjectileModifier>();
		for (int i = 0; i < components.Length; i++)
		{
			components[i].ModifyProjectile(this);
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.GetComponent<SIExclusionZone>() != null && Time.realtimeSinceStartup > timeSpawned + 0.02f)
		{
			if (exclusionZoneDespawnEffect != null)
			{
				SpawnExplosion(exclusionZoneDespawnEffect, base.transform.position, base.transform.rotation);
			}
			DespawnProjectile();
		}
		else
		{
			SIPlayer componentInParent = other.GetComponentInParent<SIPlayer>();
			if (!(componentInParent == null) && !(componentInParent == firedByPlayer) && !(firedByPlayer != SIPlayer.LocalPlayer) && !(componentInParent == SIPlayer.LocalPlayer))
			{
				projectileType.LocalProjectileHit(componentInParent);
			}
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		projectileType.LocalProjectileHit();
		if (collision.collider.gameObject.TryGetComponent<HitTargetNetworkState>(out var component))
		{
			component.TargetHit((Time.time - timeSpawned) * startingVelocity * -base.transform.forward + base.transform.position, base.transform.position);
		}
	}

	public void DespawnProjectile()
	{
		parentBlaster.DespawnProjectile(this);
	}

	public void KnockbackWithHaptics(Vector3 directionAndMagnitude, bool adjustForDirection = true)
	{
		KnockbackWithHaptics(directionAndMagnitude, hapticHitStrength, hapticHitDuration, adjustForDirection);
	}

	public void KnockbackWithHaptics(Vector3 directionAndMagnitude, float hapticStrength, float hapticDuration, bool adjustForDirection = true)
	{
		SIPlayer.LocalPlayer.PlayerKnockback(directionAndMagnitude);
		SIPlayer.LocalPlayer.NotifyBlasterHit();
		if (adjustForDirection)
		{
			Vector3 vector = GorillaTagger.Instance.leftHandTransform.position - GorillaTagger.Instance.bodyCollider.transform.position;
			Vector3 vector2 = GorillaTagger.Instance.rightHandTransform.position - GorillaTagger.Instance.bodyCollider.transform.position;
			float num = 0.5f;
			float num2 = 45f;
			float num3 = Vector3.Angle(vector, directionAndMagnitude);
			float num4 = Vector3.Angle(vector2, directionAndMagnitude);
			float hapticStrength2 = (1f - Mathf.Max(num3 - num2, 0f) / (180f - num2)) * num + (1f - num);
			float hapticStrength3 = (1f - Mathf.Max(num4 - num2, 0f) / (180f - num2)) * num + (1f - num);
			SIPlayer.LocalPlayer.PlayerHandHaptic(isLeft: true, hapticStrength2, hapticDuration);
			SIPlayer.LocalPlayer.PlayerHandHaptic(isLeft: false, hapticStrength3, hapticDuration);
		}
		else
		{
			SIPlayer.LocalPlayer.PlayerHandHaptic(isLeft: true, hapticStrength, hapticDuration);
			SIPlayer.LocalPlayer.PlayerHandHaptic(isLeft: false, hapticStrength, hapticDuration);
		}
	}

	public static GameObject SpawnExplosion(GameObject explosionPrefab, Vector3 position, Quaternion rotation)
	{
		if (blasterProjectileExplosionPools == null)
		{
			blasterProjectileExplosionPools = new Dictionary<int, List<GameObject>>();
		}
		if (explosionTypeKey == null)
		{
			explosionTypeKey = new Dictionary<GameObject, int>();
		}
		int instanceID = explosionPrefab.GetInstanceID();
		if (!blasterProjectileExplosionPools.ContainsKey(instanceID))
		{
			blasterProjectileExplosionPools.Add(instanceID, new List<GameObject>());
		}
		List<GameObject> list = blasterProjectileExplosionPools[instanceID];
		GameObject gameObject;
		if (list.Count <= 0)
		{
			gameObject = UnityEngine.Object.Instantiate(explosionPrefab, position, rotation);
			explosionTypeKey.Add(gameObject, instanceID);
		}
		else
		{
			gameObject = list[list.Count - 1];
			list.RemoveAt(list.Count - 1);
			gameObject.SetActive(value: true);
		}
		gameObject.transform.position = position;
		gameObject.transform.rotation = rotation;
		return gameObject;
	}

	public static void DespawnExplosion(GameObject explosion)
	{
		blasterProjectileExplosionPools[explosionTypeKey[explosion]].Add(explosion);
		explosion.SetActive(value: false);
	}
}

using System;
using GorillaLocomotion;
using GorillaLocomotion.Swimming;
using UnityEngine;

namespace GorillaTagScripts.Builder;

public class BuilderProjectile : MonoBehaviour
{
	public delegate void ProjectileImpactEvent(BuilderProjectile projectile, Vector3 impactPos, NetPlayer hitPlayer);

	public BuilderProjectileLauncher projectileSource;

	[Tooltip("Rotates to point along the Y axis after spawn.")]
	public GameObject surfaceImpactEffectPrefab;

	[Tooltip("Distance from the surface that the particle should spawn.")]
	private float impactEffectOffset;

	public float lifeTime = 20f;

	public bool faceDirectionOfTravel = true;

	private bool particleLaunched;

	private float timeCreated;

	private Rigidbody projectileRigidbody;

	public int projectileId;

	private float initialScale;

	private Vector3 previousPosition;

	[HideInInspector]
	public SlingshotProjectile.AOEKnockbackConfig? aoeKnockbackConfig;

	[HideInInspector]
	public float? impactSoundVolumeOverride;

	[HideInInspector]
	public float? impactSoundPitchOverride;

	[HideInInspector]
	public float impactEffectScaleMultiplier = 1f;

	[HideInInspector]
	public float gravityMultiplier = 1f;

	private ConstantForce forceComponent;

	public Vector3 launchPosition { get; private set; }

	public event ProjectileImpactEvent OnImpact;

	public void Launch(Vector3 position, Vector3 velocity, BuilderProjectileLauncher sourceObject, int projectileCount, float scale, int timeStamp)
	{
		particleLaunched = true;
		timeCreated = Time.time;
		projectileSource = sourceObject;
		float num = (float)(uint)(NetworkSystem.Instance.ServerTimestamp - timeStamp) / 1000f;
		if (num >= lifeTime)
		{
			Deactivate();
			return;
		}
		timeCreated -= num;
		Vector3 vector = Vector3.ProjectOnPlane(velocity, Vector3.up);
		float f = MathF.PI / 180f * Vector3.Angle(vector, velocity);
		float num2 = projectileRigidbody.mass * gravityMultiplier * ((scale < 1f) ? scale : 1f) * 9.8f;
		Vector3 vector2 = num * Mathf.Cos(f) * vector;
		float num3 = velocity.z * num * Mathf.Sin(f) - 0.5f * num2 * num * num;
		launchPosition = position + vector2 + num3 * Vector3.down;
		Transform obj = base.transform;
		obj.position = position;
		obj.localScale = Vector3.one * scale;
		GetComponent<Collider>().contactOffset = 0.01f * scale;
		RigidbodyWaterInteraction component = GetComponent<RigidbodyWaterInteraction>();
		if (component != null)
		{
			component.objectRadiusForWaterCollision = 0.02f * scale;
		}
		projectileRigidbody.useGravity = false;
		Vector3 vector3 = projectileRigidbody.mass * gravityMultiplier * ((scale < 1f) ? scale : 1f) * Physics.gravity;
		forceComponent.force = vector3;
		projectileRigidbody.linearVelocity = velocity + num * vector3;
		projectileId = projectileCount;
		projectileRigidbody.position = position;
		projectileSource.RegisterProjectile(this);
	}

	protected void Awake()
	{
		projectileRigidbody = GetComponent<Rigidbody>();
		forceComponent = GetComponent<ConstantForce>();
		initialScale = base.transform.localScale.x;
	}

	public void Deactivate()
	{
		base.transform.localScale = Vector3.one * initialScale;
		projectileRigidbody.useGravity = true;
		forceComponent.force = Vector3.zero;
		this.OnImpact = null;
		aoeKnockbackConfig = null;
		impactSoundVolumeOverride = null;
		impactSoundPitchOverride = null;
		impactEffectScaleMultiplier = 1f;
		gravityMultiplier = 1f;
		ObjectPools.instance.Destroy(base.gameObject);
	}

	private void SpawnImpactEffect(GameObject prefab, Vector3 position, Vector3 normal)
	{
		Vector3 position2 = position + normal * impactEffectOffset;
		GameObject obj = ObjectPools.instance.Instantiate(prefab, position2);
		Vector3 localScale = base.transform.localScale;
		obj.transform.localScale = localScale * impactEffectScaleMultiplier;
		obj.transform.up = normal;
		SurfaceImpactFX component = obj.GetComponent<SurfaceImpactFX>();
		if (component != null)
		{
			component.SetScale(localScale.x * impactEffectScaleMultiplier);
		}
		SoundBankPlayer component2 = obj.GetComponent<SoundBankPlayer>();
		if (component2 != null && !component2.playOnEnable)
		{
			component2.Play(impactSoundVolumeOverride, impactSoundPitchOverride);
		}
	}

	public void ApplyHitKnockback(Vector3 hitNormal)
	{
		if (aoeKnockbackConfig.HasValue && aoeKnockbackConfig.Value.applyAOEKnockback)
		{
			Vector3 vector = Vector3.ProjectOnPlane(hitNormal, Vector3.up);
			vector.Normalize();
			Vector3 direction = 0.75f * vector + 0.25f * Vector3.up;
			direction.Normalize();
			GTPlayer instance = GTPlayer.Instance;
			instance.ApplyKnockback(direction, aoeKnockbackConfig.Value.knockbackVelocity, instance.scale < 0.9f);
		}
	}

	private void OnEnable()
	{
		timeCreated = 0f;
		particleLaunched = false;
	}

	protected void OnDisable()
	{
		particleLaunched = false;
		if (projectileSource != null)
		{
			projectileSource.UnRegisterProjectile(this);
		}
		projectileSource = null;
	}

	public void UpdateProjectile()
	{
		if (particleLaunched)
		{
			if (Time.time > timeCreated + lifeTime)
			{
				Deactivate();
			}
			if (faceDirectionOfTravel)
			{
				Transform transform = base.transform;
				Vector3 position = transform.position;
				Vector3 forward = position - previousPosition;
				transform.rotation = ((forward.sqrMagnitude > 0f) ? Quaternion.LookRotation(forward) : transform.rotation);
				previousPosition = position;
			}
		}
	}

	private void OnCollisionEnter(Collision other)
	{
		if (!particleLaunched)
		{
			return;
		}
		BuilderPieceCollider component = other.transform.GetComponent<BuilderPieceCollider>();
		if (!(component != null) || !component.piece.gameObject.Equals(projectileSource.gameObject))
		{
			ContactPoint contact = other.GetContact(0);
			if (other.collider.gameObject.IsOnLayer(UnityLayer.GorillaBodyCollider))
			{
				ApplyHitKnockback(-1f * contact.normal);
			}
			SpawnImpactEffect(surfaceImpactEffectPrefab, contact.point, contact.normal);
			this.OnImpact?.Invoke(this, contact.point, null);
			Deactivate();
		}
	}

	protected void OnCollisionStay(Collision other)
	{
		if (!particleLaunched)
		{
			return;
		}
		BuilderPieceCollider component = other.transform.GetComponent<BuilderPieceCollider>();
		if (!(component != null) || !component.piece.gameObject.Equals(projectileSource.gameObject))
		{
			ContactPoint contact = other.GetContact(0);
			if (other.collider.gameObject.IsOnLayer(UnityLayer.GorillaBodyCollider))
			{
				ApplyHitKnockback(-1f * contact.normal);
			}
			SpawnImpactEffect(surfaceImpactEffectPrefab, contact.point, contact.normal);
			this.OnImpact?.Invoke(this, contact.point, null);
			Deactivate();
		}
	}

	protected void OnTriggerEnter(Collider other)
	{
		if (particleLaunched && NetworkSystem.Instance.InRoom && !(GorillaGameManager.instance == null) && other.gameObject.IsOnLayer(UnityLayer.GorillaTagCollider))
		{
			NetPlayer netPlayer = other.GetComponentInParent<VRRig>()?.creator;
			if (netPlayer != null && !netPlayer.IsLocal)
			{
				SpawnImpactEffect(surfaceImpactEffectPrefab, base.transform.position, Vector3.up);
				Deactivate();
			}
		}
	}
}

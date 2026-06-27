using GorillaTag.GuidedRefs;
using Unity.Cinemachine;
using UnityEngine;

public class SlingshotProjectileHitNotifier : BaseGuidedRefTargetMono
{
	public delegate void ProjectileHitEvent(SlingshotProjectile projectile, Collision collision);

	public delegate void PaperPlaneProjectileHitEvent(PaperPlaneProjectile projectile, Collider collider);

	public delegate void ProjectileTriggerEvent(SlingshotProjectile projectile, Collider collider);

	[TagField]
	[SerializeField]
	private string projectileType;

	public event ProjectileHitEvent OnProjectileHit;

	public event PaperPlaneProjectileHitEvent OnPaperPlaneHit;

	public event ProjectileHitEvent OnProjectileCollisionStay;

	public event ProjectileTriggerEvent OnProjectileTriggerEnter;

	public event ProjectileTriggerEvent OnProjectileTriggerExit;

	public void InvokeHit(SlingshotProjectile projectile, Collision collision)
	{
		if (!(projectileType != "") || !(projectile.tag != projectileType))
		{
			this.OnProjectileHit?.Invoke(projectile, collision);
		}
	}

	public void InvokeHit(PaperPlaneProjectile projectile, Collider collider)
	{
		this.OnPaperPlaneHit?.Invoke(projectile, collider);
	}

	public void InvokeCollisionStay(SlingshotProjectile projectile, Collision collision)
	{
		this.OnProjectileCollisionStay?.Invoke(projectile, collision);
	}

	public void InvokeTriggerEnter(SlingshotProjectile projectile, Collider collider)
	{
		this.OnProjectileTriggerEnter?.Invoke(projectile, collider);
	}

	public void InvokeTriggerExit(SlingshotProjectile projectile, Collider collider)
	{
		this.OnProjectileTriggerExit?.Invoke(projectile, collider);
	}

	private new void OnDestroy()
	{
		this.OnProjectileHit = null;
		this.OnProjectileCollisionStay = null;
		this.OnProjectileTriggerEnter = null;
		this.OnProjectileTriggerExit = null;
	}
}

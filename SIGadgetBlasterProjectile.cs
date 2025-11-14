using UnityEngine;

public class SIGadgetBlasterProjectile : MonoBehaviourTick
{
	public enum BlasterProjectileSize
	{
		Small,
		Medium,
		Large
	}

	public Rigidbody rb;

	public GameObject hitEffect;

	public GameObject hitEffectPlayer;

	public BlasterProjectileSize projectileSize;

	public float maxLifetime = 10f;

	public float timeSpawned;

	public SIGadgetBlaster parentBlaster;

	public int projectileId;

	public SIPlayer firedByPlayer;

	public float startingVelocity;

	public override void Tick()
	{
		if (Time.time > timeSpawned + maxLifetime)
		{
			Object.Destroy(base.gameObject);
		}
	}

	public new void OnEnable()
	{
		base.OnEnable();
		rb.linearVelocity = base.transform.forward * startingVelocity;
		timeSpawned = Time.time;
	}

	private void OnTriggerEnter(Collider other)
	{
		SIPlayer componentInParent = other.GetComponentInParent<SIPlayer>();
		if (!(componentInParent == null) && !(componentInParent == firedByPlayer) && !(firedByPlayer != SIPlayer.LocalPlayer) && !(componentInParent == SIPlayer.LocalPlayer))
		{
			parentBlaster.ProjectileHit(componentInParent, this);
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		parentBlaster.ProjectileHit(null, this);
	}

	private void OnDestroy()
	{
	}
}

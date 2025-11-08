using UnityEngine;

public class MonkeyeProjectileTarget : MonoBehaviour
{
	private MonkeyeAI monkeyeAI;

	private SlingshotProjectileHitNotifier notifier;

	private void Awake()
	{
		monkeyeAI = GetComponent<MonkeyeAI>();
		notifier = GetComponentInChildren<SlingshotProjectileHitNotifier>();
	}

	private void OnEnable()
	{
		if (notifier != null)
		{
			notifier.OnProjectileHit += Notifier_OnProjectileHit;
			notifier.OnPaperPlaneHit += Notifier_OnPaperPlaneHit;
		}
	}

	private void OnDisable()
	{
		if (notifier != null)
		{
			notifier.OnProjectileHit -= Notifier_OnProjectileHit;
			notifier.OnPaperPlaneHit -= Notifier_OnPaperPlaneHit;
		}
	}

	private void Notifier_OnProjectileHit(SlingshotProjectile projectile, Collision collision)
	{
		monkeyeAI.SetSleep();
	}

	private void Notifier_OnPaperPlaneHit(PaperPlaneProjectile projectile, Collider collider)
	{
		monkeyeAI.SetSleep();
	}
}

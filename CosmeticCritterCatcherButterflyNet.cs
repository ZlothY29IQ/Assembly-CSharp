using UnityEngine;

public class CosmeticCritterCatcherButterflyNet : CosmeticCritterCatcher
{
	[Tooltip("Use this for calculating the catch position and velocity.")]
	[SerializeField]
	private GorillaVelocityEstimator velocityEstimator;

	[Tooltip("Catch the Butterfly if it is within this radius.")]
	[SerializeField]
	private float maxCatchRadius;

	[Tooltip("Only catch the Butterfly if the net is moving faster than this speed.")]
	[SerializeField]
	private float minCatchSpeed;

	[Tooltip("Spawn a particle inside the net representing the caught Butterfly.")]
	[SerializeField]
	private ParticleSystem caughtButterflyParticleSystem;

	[Tooltip("Play this particle effect when catching a Butterfly.")]
	[SerializeField]
	private ParticleSystem catchFX;

	[Tooltip("Play this sound when catching a Butterfly.")]
	[SerializeField]
	private AudioSource catchSFX;

	public override CosmeticCritterAction GetLocalCatchAction(CosmeticCritter critter)
	{
		if (!(critter is CosmeticCritterButterfly) || !((critter.transform.position - velocityEstimator.transform.position).sqrMagnitude <= maxCatchRadius * maxCatchRadius) || !(velocityEstimator.linearVelocity.sqrMagnitude >= minCatchSpeed * minCatchSpeed))
		{
			return CosmeticCritterAction.None;
		}
		return CosmeticCritterAction.RPC | CosmeticCritterAction.Despawn;
	}

	public override bool ValidateRemoteCatchAction(CosmeticCritter critter, CosmeticCritterAction catchAction, double serverTime)
	{
		if (base.ValidateRemoteCatchAction(critter, catchAction, serverTime) && critter is CosmeticCritterButterfly && (critter.transform.position - velocityEstimator.transform.position).sqrMagnitude <= maxCatchRadius * maxCatchRadius + 1f && velocityEstimator.linearVelocity.sqrMagnitude >= minCatchSpeed * minCatchSpeed - 1f)
		{
			return catchAction == (CosmeticCritterAction.RPC | CosmeticCritterAction.Despawn);
		}
		return false;
	}

	public override void OnCatch(CosmeticCritter critter, CosmeticCritterAction catchAction, double serverTime)
	{
		caughtButterflyParticleSystem.Emit((critter as CosmeticCritterButterfly).GetEmitParams, 1);
		catchFX.Play();
		catchSFX.Play();
	}
}

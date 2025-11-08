using UnityEngine;

public class SIResourceMonkeIdol : SIResource
{
	[SerializeField]
	private GameObject depositEnabledParticle;

	protected override void OnEnable()
	{
		base.OnEnable();
		depositEnabledParticle.SetActive(SIPlayer.LocalPlayer.CanLimitedResourceBeDeposited(limitedDepositType));
	}

	public override void HandleDepositAuth(SIPlayer depositingPlayer)
	{
		SIPlayer.LocalPlayer.TriggerIdolDepositedCelebration(base.transform.position);
	}
}

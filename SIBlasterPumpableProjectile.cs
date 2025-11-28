using UnityEngine;

public class SIBlasterPumpableProjectile : MonoBehaviour, SIGadgetProjectileModifier
{
	public float maxPump;

	public float pumpChargedAmount;

	public float velocityPerPumpCharge;

	public float strengthPerPumpCharge;

	public void ModifyProjectile(SIGadgetBlasterProjectile projectile)
	{
		SIGadgetPumpBlaster component = projectile.parentBlaster.GetComponent<SIGadgetPumpBlaster>();
		if (component == null)
		{
			return;
		}
		pumpChargedAmount = Mathf.Min(maxPump, component.currentPumpChargeAmount);
		projectile.startingVelocity += pumpChargedAmount;
		if (strengthPerPumpCharge > 0f)
		{
			SIBlasterDirectHitProjectile component2 = projectile.GetComponent<SIBlasterDirectHitProjectile>();
			if (component2 != null)
			{
				component2.knockbackSpeed += strengthPerPumpCharge * pumpChargedAmount;
			}
			SIBlasterSplashProjectile component3 = projectile.GetComponent<SIBlasterSplashProjectile>();
			if (component3 != null)
			{
				component3.knockbackSpeed += strengthPerPumpCharge * pumpChargedAmount;
			}
			SIBlasterSprayProjectile component4 = projectile.GetComponent<SIBlasterSprayProjectile>();
			if (component4 != null)
			{
				component4.knockbackSpeed += strengthPerPumpCharge * pumpChargedAmount;
			}
		}
	}
}

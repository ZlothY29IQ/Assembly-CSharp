using UnityEngine;

public class SIBlasterRandomAngularVelocity : MonoBehaviour, SIGadgetProjectileModifier
{
	public float maxVel;

	public void ModifyProjectile(SIGadgetBlasterProjectile projectile)
	{
		projectile.rb.angularVelocity = new Vector3(Random.Range(0f - maxVel, maxVel), Random.Range(0f - maxVel, maxVel), Random.Range(0f - maxVel, maxVel));
	}
}

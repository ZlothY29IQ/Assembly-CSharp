using System.Collections.Generic;
using UnityEngine;

public class CrittersStunBomb : CrittersToolThrowable
{
	[Header("Stun Bomb")]
	public float radius = 1f;

	public float stunDuration = 5f;

	protected override void OnImpact(Vector3 hitPosition, Vector3 hitNormal)
	{
		if (!CrittersManager.instance.LocalAuthority())
		{
			return;
		}
		Vector3 position = base.transform.position;
		List<CrittersPawn> crittersPawns = CrittersManager.instance.crittersPawns;
		for (int i = 0; i < crittersPawns.Count; i++)
		{
			CrittersPawn crittersPawn = crittersPawns[i];
			if (crittersPawn.isActiveAndEnabled && Vector3.Distance(crittersPawn.transform.position, position) < radius)
			{
				crittersPawn.Stunned(stunDuration);
			}
		}
		CrittersManager.instance.TriggerEvent(CrittersManager.CritterEvent.StunExplosion, actorId, position, Quaternion.LookRotation(hitNormal));
	}

	protected override void OnImpactCritter(CrittersPawn impactedCritter)
	{
		if (CrittersManager.instance.LocalAuthority())
		{
			impactedCritter.Stunned(stunDuration);
		}
	}
}

using System;
using UnityEngine;

public class SIBlasterDirectHitProjectile : MonoBehaviour, SIGadgetProjectileType
{
	private SIGadgetBlasterProjectile projectile;

	public float knockbackSpeed;

	public float upwardsAngle = 30f;

	private void OnEnable()
	{
		projectile = GetComponent<SIGadgetBlasterProjectile>();
	}

	public void LocalProjectileHit(SIPlayer player = null)
	{
		if (player != null && projectile.hitEffectPlayer != null)
		{
			SIGadgetBlasterProjectile.SpawnExplosion(projectile.hitEffectPlayer, projectile.transform.position, projectile.transform.rotation);
		}
		if (player == null && projectile.hitEffect != null)
		{
			SIGadgetBlasterProjectile.SpawnExplosion(projectile.hitEffect, projectile.transform.position, projectile.transform.rotation);
		}
		if (player != null)
		{
			TriggerBlastDirectHitPlayer(player);
		}
		projectile.DespawnProjectile();
	}

	public void TriggerBlastDirectHitPlayer(SIPlayer playerHit)
	{
		if (!(playerHit == SIPlayer.LocalPlayer))
		{
			projectile.parentBlaster.SendClientToClientRPC(1, new object[3]
			{
				projectile.projectileId,
				base.transform.position,
				playerHit.ActorNr
			});
		}
	}

	public void NetworkedProjectileHit(object[] data)
	{
		if (data == null || data.Length != 3 || !GameEntityManager.ValidateDataType<int>(data[0], out var _) || !GameEntityManager.ValidateDataType<Vector3>(data[1], out var dataAsType2) || !dataAsType2.IsFinite() || !GameEntityManager.ValidateDataType<int>(data[2], out var dataAsType3) || (base.transform.position - dataAsType2).magnitude > projectile.parentBlaster.maxLagDistance)
		{
			return;
		}
		projectile.DespawnProjectile();
		SIPlayer sIPlayer = SIPlayer.Get(dataAsType3);
		if (!(sIPlayer == null))
		{
			if (sIPlayer != SIPlayer.LocalPlayer)
			{
				SIGadgetBlasterProjectile.SpawnExplosion(projectile.hitEffect, dataAsType2, projectile.transform.rotation);
				return;
			}
			SIGadgetBlasterProjectile.SpawnExplosion(projectile.hitEffectPlayer, dataAsType2, projectile.transform.rotation);
			float num = Vector3.Angle(base.transform.forward, Vector3.up);
			Vector3 vector = Vector3.RotateTowards(base.transform.forward.normalized, Vector3.up, Mathf.Clamp(num - upwardsAngle, 0f, upwardsAngle) * (MathF.PI / 180f), 0f);
			projectile.KnockbackWithHaptics(vector.normalized * knockbackSpeed);
		}
	}
}

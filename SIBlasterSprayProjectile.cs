using System;
using GorillaLocomotion;
using UnityEngine;

public class SIBlasterSprayProjectile : MonoBehaviour
{
	private SIGadgetBlasterProjectile projectile;

	public float knockbackSpeed;

	public float verticalOffset = -0.133f;

	public float upwardsAngle = 30f;

	private void OnEnable()
	{
		projectile = GetComponent<SIGadgetBlasterProjectile>();
	}

	public void LocalProjectileHit(SIPlayer player = null)
	{
		if (player != null && projectile.hitEffectPlayer != null)
		{
			UnityEngine.Object.Instantiate(projectile.hitEffectPlayer, projectile.transform.position, projectile.transform.rotation);
		}
		if (player == null && projectile.hitEffect != null)
		{
			UnityEngine.Object.Instantiate(projectile.hitEffect, projectile.transform.position, projectile.transform.rotation);
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
			float num = Vector3.Angle(base.transform.forward, Vector3.up);
			Vector3 vector = Vector3.RotateTowards(base.transform.forward.normalized, Vector3.up, Mathf.Clamp(num - upwardsAngle, 0f, upwardsAngle) * (MathF.PI / 180f), 0f);
			projectile.parentBlaster.SendClientToClientRPC(1, new object[4]
			{
				projectile.projectileId,
				base.transform.position,
				vector,
				playerHit.ActorNr
			});
		}
	}

	public void NetworkedProjectileHit(object[] data)
	{
		if (data == null || data.Length != 4 || !GameEntityManager.ValidateDataType<int>(data[0], out var _) || !GameEntityManager.ValidateDataType<Vector3>(data[1], out var dataAsType2) || !GameEntityManager.ValidateDataType<Vector3>(data[2], out var dataAsType3) || !GameEntityManager.ValidateDataType<int>(data[3], out var dataAsType4) || (base.transform.position - dataAsType2).magnitude > projectile.parentBlaster.maxLagDistance)
		{
			return;
		}
		projectile.DespawnProjectile();
		SIPlayer sIPlayer = SIPlayer.Get(dataAsType4);
		if (!(sIPlayer == null))
		{
			if (sIPlayer != SIPlayer.LocalPlayer)
			{
				UnityEngine.Object.Instantiate(projectile.hitEffect, dataAsType2, projectile.transform.rotation);
				return;
			}
			UnityEngine.Object.Instantiate(projectile.hitEffectPlayer, dataAsType2, projectile.transform.rotation);
			GTPlayer.Instance.ApplyKnockback(dataAsType3.normalized, knockbackSpeed, forceOffTheGround: true);
		}
	}
}

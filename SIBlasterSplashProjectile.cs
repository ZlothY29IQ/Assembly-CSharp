using System;
using System.Collections.Generic;
using UnityEngine;

public class SIBlasterSplashProjectile : MonoBehaviour, SIGadgetProjectileType
{
	public float knockbackSpeed;

	public float fullSplashRadius;

	public float splashHitDistance;

	public float upwardsAngle = 30f;

	private SIGadgetBlasterProjectile projectile;

	private List<VRRig> rigList = new List<VRRig>();

	private RaycastHit[] hits = new RaycastHit[20];

	private void OnEnable()
	{
		projectile = GetComponent<SIGadgetBlasterProjectile>();
	}

	public void LocalProjectileHit(SIPlayer player = null)
	{
		if (!projectile.firedByPlayer == (bool)SIPlayer.LocalPlayer)
		{
			SIGadgetBlasterProjectile.SpawnExplosion(projectile.hitEffect, projectile.transform.position, projectile.transform.rotation);
			projectile.DespawnProjectile();
			return;
		}
		rigList.Clear();
		VRRigCache.Instance.GetActiveRigs(rigList);
		Vector3 position = projectile.transform.position;
		for (int num = rigList.Count - 1; num >= 0; num--)
		{
			if ((rigList[num].transform.position - position).magnitude < splashHitDistance)
			{
				Vector3 position2 = rigList[num].head.rigTarget.position;
				Vector3 position3 = rigList[num].bodyTransform.position;
				if (Physics.RaycastNonAlloc(position, position2 - position, hits, splashHitDistance, projectile.parentBlaster.environmentLayerMask, QueryTriggerInteraction.Ignore) != 0 && Physics.RaycastNonAlloc(position, position3 - position, hits, splashHitDistance, projectile.parentBlaster.environmentLayerMask, QueryTriggerInteraction.Ignore) != 0)
				{
					rigList.RemoveAt(num);
				}
			}
			else
			{
				rigList.RemoveAt(num);
			}
		}
		if (rigList.Count <= 0)
		{
			SIGadgetBlasterProjectile.SpawnExplosion(projectile.hitEffect, projectile.transform.position, projectile.transform.rotation);
			projectile.DespawnProjectile();
		}
		else
		{
			SIGadgetBlasterProjectile.SpawnExplosion(projectile.hitEffectPlayer, projectile.transform.position, projectile.transform.rotation);
			TriggerSplashHitPlayers(rigList);
			projectile.DespawnProjectile();
		}
	}

	public void TriggerSplashHitPlayers(List<VRRig> hitPlayers)
	{
		int[] array = new int[hitPlayers.Count];
		Vector3[] array2 = new Vector3[hitPlayers.Count];
		for (int i = 0; i < hitPlayers.Count; i++)
		{
			array[i] = ((hitPlayers[i] != null && hitPlayers[i].OwningNetPlayer != null) ? hitPlayers[i].OwningNetPlayer.ActorNumber : (-1));
			Vector3 vector = hitPlayers[i].transform.position - projectile.transform.position;
			float num = Mathf.Max(0f, 1f - Mathf.Max(0f, vector.magnitude - fullSplashRadius) / (splashHitDistance - fullSplashRadius));
			array2[i] = vector.normalized * knockbackSpeed * num;
			if (hitPlayers[i] != null && hitPlayers[i].isLocal && num > 0f)
			{
				SplashHitLocalPlayer(array2[i]);
			}
		}
		projectile.parentBlaster.SendClientToClientRPC(1, new object[4]
		{
			projectile.projectileId,
			base.transform.position,
			array,
			array2
		});
	}

	public void NetworkedProjectileHit(object[] data)
	{
		if (data == null || data.Length != 4 || !GameEntityManager.ValidateDataType<int>(data[0], out var _) || !GameEntityManager.ValidateDataType<Vector3>(data[1], out var dataAsType2) || !dataAsType2.IsFinite() || !GameEntityManager.ValidateDataType<int[]>(data[2], out var dataAsType3) || !GameEntityManager.ValidateDataType<Vector3[]>(data[3], out var dataAsType4))
		{
			return;
		}
		for (int i = 0; i < dataAsType4.Length; i++)
		{
			if (!dataAsType4[i].IsFinite())
			{
				return;
			}
		}
		if (dataAsType3.Length > VRRigCache.Instance.GetAllRigs().Length || dataAsType3.Length != dataAsType4.Length || (base.transform.position - dataAsType2).magnitude > projectile.parentBlaster.maxLagDistance)
		{
			return;
		}
		projectile.DespawnProjectile();
		bool flag = false;
		for (int j = 0; j < dataAsType3.Length; j++)
		{
			SIPlayer sIPlayer = SIPlayer.Get(dataAsType3[j]);
			if (sIPlayer != null && sIPlayer == SIPlayer.LocalPlayer)
			{
				flag = true;
				SplashHitLocalPlayer(dataAsType4[j]);
			}
		}
		if (flag)
		{
			SIGadgetBlasterProjectile.SpawnExplosion(projectile.hitEffectPlayer, dataAsType2, base.transform.rotation);
		}
		else
		{
			SIGadgetBlasterProjectile.SpawnExplosion(projectile.hitEffect, dataAsType2, base.transform.rotation);
		}
	}

	public void SplashHitLocalPlayer(Vector3 directionAndMagnitude)
	{
		if (!(directionAndMagnitude.magnitude > knockbackSpeed * 1.05f))
		{
			SIPlayer.LocalPlayer.NotifyBlasterSplashHit();
			float num = Vector3.Angle(directionAndMagnitude.normalized, Vector3.up);
			Vector3 vector = Vector3.RotateTowards(directionAndMagnitude.normalized, Vector3.up, Mathf.Clamp(num - upwardsAngle, 0f, upwardsAngle) * (MathF.PI / 180f), 0f);
			projectile.KnockbackWithHaptics(vector * directionAndMagnitude.magnitude, directionAndMagnitude.magnitude / knockbackSpeed * projectile.hapticHitStrength, projectile.hapticHitDuration);
		}
	}
}

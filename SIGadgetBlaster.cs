using System;
using System.Collections;
using System.Collections.Generic;
using GorillaLocomotion;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(GameGrabbable))]
[RequireComponent(typeof(GameSnappable))]
[RequireComponent(typeof(GameButtonActivatable))]
public class SIGadgetBlaster : SIGadget
{
	private enum BlasterState
	{
		Idle,
		Charging,
		Cooldown,
		Count
	}

	private enum RPCCalls
	{
		FireProjectile,
		ProjectileHitPlayer
	}

	private BlasterState currentState;

	public GameObject smallProjectile;

	public GameObject mediumChargeProjectile;

	public GameObject largeChargeProjectile;

	[SerializeField]
	private GameButtonActivatable buttonActivatable;

	[SerializeField]
	private float inputActivateThreshold = 0.35f;

	[SerializeField]
	private float inputDeactivateThreshold = 0.25f;

	[SerializeField]
	private float chargeRatePerSecond = 20f;

	[SerializeField]
	private float mediumChargeLevel = 20f;

	[SerializeField]
	private float largeChargeLevel = 60f;

	[SerializeField]
	private int maxProjectileCount = 10;

	[SerializeField]
	private float fireCooldown = 0.2f;

	public float upwardsAngle = 30f;

	public float maxLagDistance = 5f;

	public float verticalOffset = -0.133f;

	public float largeProjectileKnockbackSpeed = 8f;

	public float mediumProjectileKnockbackSpeed = 5f;

	public float smallProjectileKnockbackSpeed = 2f;

	private bool wasActivated;

	public float maxChargeDiff = 5f;

	public const float PROJECTILE_MAX_LATENCY = 1f;

	private float currentCharge;

	private float lastFired;

	private int projectileCount;

	private int projectileId;

	private WaitForSeconds projectileDestroyDelay = new WaitForSeconds(1f);

	private List<SIGadgetBlasterProjectile> activeProjectiles = new List<SIGadgetBlasterProjectile>();

	private List<SIGadgetBlasterProjectile> projectilesToDespawn = new List<SIGadgetBlasterProjectile>();

	public Transform firingPosition;

	public AudioSource firingSource;

	public AudioClip firingSmallClip;

	public AudioClip firingMediumClip;

	public AudioClip firingLargeClip;

	public float firingSmallVolume;

	public float firingMediumVolume;

	public float firingLargeVolume;

	public AudioSource blasterSource;

	public AudioClip idleClip;

	public AudioClip chargingClip;

	public float idleVolume;

	public float chargingSmallVolume;

	public float chargingMediumVolume;

	public float chargingLargeVolume;

	public ParticleSystem smallFireFX;

	public ParticleSystem mediumFireFX;

	public ParticleSystem largeFireFX;

	public GameObject smallChargingFX;

	public GameObject mediumChargingFX;

	public GameObject largeChargingFX;

	protected override void OnEnable()
	{
		base.OnEnable();
		currentCharge = 0f;
		lastFired = 0f;
		GameEntity obj = gameEntity;
		obj.OnGrabbed = (Action)Delegate.Combine(obj.OnGrabbed, new Action(StartGrabbing));
		GameEntity obj2 = gameEntity;
		obj2.OnSnapped = (Action)Delegate.Combine(obj2.OnSnapped, new Action(StartGrabbing));
		GameEntity obj3 = gameEntity;
		obj3.OnReleased = (Action)Delegate.Combine(obj3.OnReleased, new Action(StopGrabbing));
		GameEntity obj4 = gameEntity;
		obj4.OnUnsnapped = (Action)Delegate.Combine(obj4.OnUnsnapped, new Action(StopGrabbing));
	}

	protected override void OnUpdateAuthority(float dt)
	{
		base.OnUpdateAuthority(dt);
		switch (currentState)
		{
		case BlasterState.Idle:
			if (CheckInput())
			{
				FireProjectile(0f, NextFireId(), firingPosition.position, firingPosition.rotation);
				SetStateAuthority(BlasterState.Charging);
			}
			break;
		case BlasterState.Charging:
			currentCharge += chargeRatePerSecond * Time.deltaTime;
			UpdateChargingVisuals();
			if (!CheckInput())
			{
				if (currentCharge >= mediumChargeLevel)
				{
					FireProjectile(currentCharge, NextFireId(), firingPosition.position, firingPosition.rotation);
					SetStateAuthority(BlasterState.Cooldown);
				}
				else
				{
					SetStateAuthority(BlasterState.Idle);
				}
			}
			break;
		case BlasterState.Cooldown:
			if (!(Time.time < lastFired + fireCooldown))
			{
				if (CheckInput())
				{
					SetStateAuthority(BlasterState.Charging);
				}
				else
				{
					SetStateAuthority(BlasterState.Idle);
				}
			}
			break;
		}
	}

	protected override void OnUpdateRemote(float dt)
	{
		base.OnUpdateRemote(dt);
		BlasterState blasterState = (BlasterState)gameEntity.GetState();
		if (blasterState != currentState)
		{
			SetStateShared(blasterState);
		}
		switch (currentState)
		{
		case BlasterState.Charging:
			currentCharge += chargeRatePerSecond * Time.deltaTime;
			UpdateChargingVisuals();
			break;
		case BlasterState.Idle:
		case BlasterState.Cooldown:
			break;
		}
	}

	private void SetStateAuthority(BlasterState newState)
	{
		SetStateShared(newState);
		gameEntity.RequestState(gameEntity.id, (long)newState);
	}

	private void SetStateShared(BlasterState newState)
	{
		if (newState == currentState || !CanChangeState((long)newState))
		{
			return;
		}
		_ = currentState;
		currentState = newState;
		switch (currentState)
		{
		case BlasterState.Idle:
			blasterSource.clip = idleClip;
			blasterSource.volume = idleVolume;
			currentCharge = 0f;
			break;
		case BlasterState.Charging:
			currentCharge = 0f;
			blasterSource.clip = chargingClip;
			blasterSource.volume = chargingSmallVolume;
			blasterSource.loop = true;
			blasterSource.Play();
			break;
		case BlasterState.Cooldown:
			blasterSource.Stop();
			if (Time.time > lastFired + fireCooldown)
			{
				lastFired = Time.time;
			}
			break;
		}
		UpdateChargingVisuals();
	}

	private void UpdateChargingVisuals()
	{
		bool flag = currentState == BlasterState.Charging;
		bool flag2 = currentCharge >= largeChargeLevel && flag;
		bool flag3 = currentCharge >= mediumChargeLevel && !flag2 && flag;
		bool flag4 = !flag2 && !flag3 && flag;
		if (largeChargingFX.activeSelf != flag2)
		{
			if (flag2)
			{
				blasterSource.clip = chargingClip;
				blasterSource.volume = chargingLargeVolume;
			}
			largeChargingFX.SetActive(flag2);
		}
		if (mediumChargingFX.activeSelf != flag3)
		{
			if (flag3)
			{
				blasterSource.clip = chargingClip;
				blasterSource.volume = chargingMediumVolume;
			}
			mediumChargingFX.SetActive(flag3);
		}
		if (smallChargingFX.activeSelf != flag4)
		{
			if (flag4)
			{
				blasterSource.volume = chargingSmallVolume;
				blasterSource.clip = chargingClip;
			}
			smallChargingFX.SetActive(flag4);
		}
		if (!flag)
		{
			blasterSource.Stop();
		}
	}

	public override void ApplyUpgradeNodes(SIUpgradeSet withUpgrades)
	{
	}

	private static bool CanChangeState(long newStateIndex)
	{
		if (newStateIndex >= 0)
		{
			return newStateIndex < 3;
		}
		return false;
	}

	private bool CheckInput()
	{
		float sensitivity = (wasActivated ? inputActivateThreshold : inputDeactivateThreshold);
		return buttonActivatable.CheckInput(checkHeld: true, checkSnapped: true, sensitivity);
	}

	private int NextFireId()
	{
		return projectileId++;
	}

	public void FireProjectile(float firedAtChargeLevel, int fireId, Vector3 position, Quaternion rotation)
	{
		if (IsEquippedLocal() || activatedLocally)
		{
			if (Time.time < lastFired + fireCooldown)
			{
				return;
			}
			SendClientToClientRPC(0, new object[4] { firedAtChargeLevel, fireId, position, rotation });
		}
		if (projectileCount <= maxProjectileCount)
		{
			if (Mathf.Abs(currentCharge - firedAtChargeLevel) <= maxChargeDiff)
			{
				currentCharge = firedAtChargeLevel;
			}
			GameObject gameObject = null;
			if (currentCharge > largeChargeLevel)
			{
				firingSource.clip = firingLargeClip;
				firingSource.volume = firingLargeVolume;
				largeFireFX.Play();
				gameObject = largeChargeProjectile;
			}
			else if (currentCharge > mediumChargeLevel)
			{
				firingSource.clip = firingMediumClip;
				firingSource.volume = firingMediumVolume;
				mediumFireFX.Play();
				gameObject = mediumChargeProjectile;
			}
			else
			{
				firingSource.clip = firingSmallClip;
				firingSource.volume = firingSmallVolume;
				smallFireFX.Play();
				gameObject = smallProjectile;
			}
			firingSource.time = 0f;
			firingSource.Play();
			firingSource.loop = false;
			currentCharge = 0f;
			projectileCount++;
			SIGadgetBlasterProjectile component = UnityEngine.Object.Instantiate(gameObject, position, rotation).GetComponent<SIGadgetBlasterProjectile>();
			component.parentBlaster = this;
			component.projectileId = fireId;
			component.firedByPlayer = (gameEntity.IsHeld() ? SIPlayer.Get(gameEntity.heldByActorNumber) : SIPlayer.Get(gameEntity.snappedByActorNumber));
			activeProjectiles.Add(component);
			lastFired = Time.time;
		}
	}

	public override void ProcessClientToClientRPC(PhotonMessageInfo info, int rpcID, object[] data)
	{
		switch ((RPCCalls)rpcID)
		{
		case RPCCalls.FireProjectile:
		{
			if (data != null && data.Length == 4 && GameEntityManager.ValidateDataType<float>(data[0], out var dataAsType5) && GameEntityManager.ValidateDataType<int>(data[1], out var dataAsType6) && GameEntityManager.ValidateDataType<Vector3>(data[2], out var dataAsType7) && GameEntityManager.ValidateDataType<Quaternion>(data[3], out var dataAsType8) && gameEntity.IsAttachedToPlayer(NetPlayer.Get(info.Sender)))
			{
				FireProjectile(dataAsType5, dataAsType6, dataAsType7, dataAsType8);
			}
			break;
		}
		case RPCCalls.ProjectileHitPlayer:
		{
			if (data == null || data.Length != 4 || !GameEntityManager.ValidateDataType<int>(data[0], out var dataAsType) || !GameEntityManager.ValidateDataType<Vector3>(data[1], out var dataAsType2) || !GameEntityManager.ValidateDataType<Vector3>(data[2], out var dataAsType3) || !GameEntityManager.ValidateDataType<int>(data[3], out var dataAsType4))
			{
				break;
			}
			SIGadgetBlasterProjectile sIGadgetBlasterProjectile = null;
			for (int i = 0; i < activeProjectiles.Count; i++)
			{
				if (activeProjectiles[i].projectileId == dataAsType)
				{
					sIGadgetBlasterProjectile = activeProjectiles[i];
					break;
				}
			}
			if (!(sIGadgetBlasterProjectile == null) && !(sIGadgetBlasterProjectile.firedByPlayer != SIPlayer.Get(info.Sender.ActorNumber)) && !((sIGadgetBlasterProjectile.transform.position - dataAsType2).magnitude > maxLagDistance))
			{
				SIGadgetBlasterProjectile.BlasterProjectileSize projectileSize = sIGadgetBlasterProjectile.projectileSize;
				DespawnProjectile(sIGadgetBlasterProjectile);
				SIPlayer sIPlayer = SIPlayer.Get(dataAsType4);
				if (sIPlayer != null && sIGadgetBlasterProjectile.hitEffectPlayer != null)
				{
					UnityEngine.Object.Instantiate(sIGadgetBlasterProjectile.hitEffect, dataAsType2, sIGadgetBlasterProjectile.transform.rotation);
				}
				if (!(sIPlayer != SIPlayer.LocalPlayer))
				{
					TriggerBlastHitPlayerKnockback(projectileSize, dataAsType3);
				}
			}
			break;
		}
		}
	}

	public void TriggerBlastHitPlayer(SIPlayer playerHit, int projectileId, Vector3 position, Vector3 forwardDirection)
	{
		if (!(playerHit == SIPlayer.LocalPlayer))
		{
			float num = Vector3.Angle(forwardDirection, Vector3.up);
			Vector3 vector = Vector3.RotateTowards(forwardDirection.normalized, Vector3.up, Mathf.Clamp(num - upwardsAngle, 0f, upwardsAngle) * (MathF.PI / 180f), 0f);
			SendClientToClientRPC(1, new object[4] { projectileId, position, vector, playerHit.ActorNr });
		}
	}

	public void TriggerBlastHitPlayerKnockback(SIGadgetBlasterProjectile.BlasterProjectileSize projectileSize, Vector3 direction)
	{
		float speed = 0f;
		switch (projectileSize)
		{
		case SIGadgetBlasterProjectile.BlasterProjectileSize.Large:
			speed = largeProjectileKnockbackSpeed;
			break;
		case SIGadgetBlasterProjectile.BlasterProjectileSize.Medium:
			speed = mediumProjectileKnockbackSpeed;
			break;
		case SIGadgetBlasterProjectile.BlasterProjectileSize.Small:
			speed = smallProjectileKnockbackSpeed;
			break;
		}
		GTPlayer.Instance.ApplyKnockback(direction.normalized, speed, forceOffTheGround: true);
	}

	public void StartGrabbing()
	{
		if (IsEquippedLocal() || activatedLocally)
		{
			SetStateAuthority(BlasterState.Idle);
		}
	}

	public void StopGrabbing()
	{
		SetStateShared(BlasterState.Idle);
	}

	public void ProjectileHit(SIPlayer hitPlayer, SIGadgetBlasterProjectile projectile)
	{
		if (hitPlayer != null && projectile.hitEffectPlayer != null)
		{
			UnityEngine.Object.Instantiate(projectile.hitEffectPlayer, projectile.transform.position, projectile.transform.rotation);
		}
		if (hitPlayer == null && projectile.hitEffect != null)
		{
			UnityEngine.Object.Instantiate(projectile.hitEffect, projectile.transform.position, projectile.transform.rotation);
		}
		if (hitPlayer != null)
		{
			TriggerBlastHitPlayer(hitPlayer, projectile.projectileId, projectile.transform.position, projectile.transform.forward);
		}
		DespawnProjectile(projectile);
	}

	private void DespawnProjectile(SIGadgetBlasterProjectile projectile)
	{
		projectile.gameObject.SetActive(value: false);
		if (!projectilesToDespawn.Contains(projectile))
		{
			StartCoroutine(DelayedDestroyProjectile(projectile));
		}
	}

	public IEnumerator DelayedDestroyProjectile(SIGadgetBlasterProjectile projectile)
	{
		projectilesToDespawn.Add(projectile);
		yield return projectileDestroyDelay;
		projectileCount--;
		if (activeProjectiles.Contains(projectile))
		{
			activeProjectiles.Remove(projectile);
		}
		if (projectile == null || projectile.gameObject == null)
		{
			yield return null;
		}
		projectilesToDespawn.Remove(projectile);
		UnityEngine.Object.Destroy(projectile.gameObject);
	}
}

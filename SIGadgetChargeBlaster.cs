using System;
using UnityEngine;

public class SIGadgetChargeBlaster : MonoBehaviour, SIGadgetBlasterType
{
	[Serializable]
	public struct BlasterChargeLevel
	{
		public float chargeThreshold;

		public float chargingVolume;

		public float firingVolume;

		public float chargingHapticStrength;

		public float firingHapticStrength;

		public float firingHapticDuration;

		public AudioClip firingClip;

		public ParticleSystem fireFX;

		public GameObject chargingFX;

		public SIGadgetBlasterProjectile projectilePrefab;
	}

	[SerializeField]
	private float fireCooldown = 0.2f;

	[SerializeField]
	private float chargeRatePerSecond = 20f;

	public float fireRateGracePercentage = 0.25f;

	public float maxChargeDiff = 5f;

	private float currentCharge;

	public AudioClip chargingClip;

	public BlasterChargeLevel[] chargeLevels;

	private SIGadgetBlaster blaster;

	private bool CheckInput()
	{
		return blaster.CheckInput();
	}

	private void OnEnable()
	{
		blaster = GetComponent<SIGadgetBlaster>();
		currentCharge = 0f;
	}

	public void OnUpdateAuthority(float dt)
	{
		switch (blaster.currentState)
		{
		case SIGadgetBlasterState.Idle:
			if (CheckInput())
			{
				FireProjectile(0f, blaster.NextFireId(), blaster.firingPosition.position, blaster.firingPosition.rotation);
				blaster.SetStateAuthority(SIGadgetBlasterState.Charging);
			}
			break;
		case SIGadgetBlasterState.Charging:
			currentCharge += chargeRatePerSecond * Time.deltaTime;
			UpdateChargingVisuals();
			if (CheckInput())
			{
				blaster.FireProjectileHaptics(chargeLevels[CurrentBlasterChargeLevel()].chargingHapticStrength, Time.fixedDeltaTime);
			}
			else if (CurrentBlasterChargeLevel() > 0)
			{
				FireProjectile(currentCharge, blaster.NextFireId(), blaster.firingPosition.position, blaster.firingPosition.rotation);
				blaster.SetStateAuthority(SIGadgetBlasterState.Cooldown);
			}
			else
			{
				blaster.SetStateAuthority(SIGadgetBlasterState.Idle);
			}
			break;
		case SIGadgetBlasterState.Cooldown:
			if (!(Time.time < blaster.lastFired + fireCooldown))
			{
				if (CheckInput())
				{
					blaster.SetStateAuthority(SIGadgetBlasterState.Charging);
				}
				else
				{
					blaster.SetStateAuthority(SIGadgetBlasterState.Idle);
				}
			}
			break;
		}
	}

	public void OnUpdateRemote(float dt)
	{
		switch (blaster.currentState)
		{
		case SIGadgetBlasterState.Charging:
			currentCharge += chargeRatePerSecond * Time.deltaTime;
			UpdateChargingVisuals();
			break;
		case SIGadgetBlasterState.Idle:
		case SIGadgetBlasterState.Cooldown:
			break;
		}
	}

	public void SetStateShared()
	{
		switch (blaster.currentState)
		{
		case SIGadgetBlasterState.Idle:
			currentCharge = 0f;
			break;
		case SIGadgetBlasterState.Charging:
			currentCharge = 0f;
			blaster.blasterSource.clip = chargingClip;
			blaster.blasterSource.volume = chargeLevels[0].chargingVolume;
			blaster.blasterSource.loop = true;
			blaster.blasterSource.Play();
			break;
		case SIGadgetBlasterState.Cooldown:
			blaster.blasterSource.Stop();
			if (Time.time > blaster.lastFired + fireCooldown)
			{
				blaster.lastFired = Time.time;
			}
			break;
		}
		UpdateChargingVisuals();
	}

	public void FireProjectile(float firedAtChargeLevel, int fireId, Vector3 position, Quaternion rotation)
	{
		if (blaster.projectileCount > blaster.maxProjectileCount)
		{
			return;
		}
		if (blaster.LocalEquippedOrActivated)
		{
			if (Time.time < blaster.lastFired + fireCooldown)
			{
				return;
			}
			blaster.SendClientToClientRPC(0, new object[4] { firedAtChargeLevel, fireId, position, rotation });
		}
		if (Mathf.Abs(currentCharge - firedAtChargeLevel) <= maxChargeDiff)
		{
			currentCharge = firedAtChargeLevel;
		}
		SIGadgetBlasterProjectile sIGadgetBlasterProjectile = null;
		int num = CurrentBlasterChargeLevel();
		blaster.firingSource.clip = chargeLevels[num].firingClip;
		blaster.firingSource.volume = chargeLevels[num].firingVolume;
		chargeLevels[num].fireFX.Play();
		sIGadgetBlasterProjectile = chargeLevels[num].projectilePrefab;
		blaster.firingSource.time = 0f;
		blaster.firingSource.Play();
		blaster.firingSource.loop = false;
		if (blaster.LocalEquippedOrActivated)
		{
			blaster.FireProjectileHaptics(chargeLevels[num].firingHapticStrength, chargeLevels[num].firingHapticDuration);
		}
		currentCharge = 0f;
		blaster.InstantiateProjectile(sIGadgetBlasterProjectile, position, rotation, fireId);
	}

	private void UpdateChargingVisuals()
	{
		bool flag = blaster.currentState == SIGadgetBlasterState.Charging;
		int num = CurrentBlasterChargeLevel();
		for (int i = 0; i < chargeLevels.Length; i++)
		{
			bool flag2 = flag && i == num;
			if (chargeLevels[i].chargingFX.activeSelf != flag2)
			{
				chargeLevels[i].chargingFX.SetActive(flag2);
			}
		}
		if (blaster.blasterSource.clip != chargingClip)
		{
			blaster.blasterSource.clip = chargingClip;
		}
		blaster.blasterSource.volume = chargeLevels[num].chargingVolume;
		if (!flag && blaster.blasterSource.isPlaying)
		{
			blaster.blasterSource.Stop();
		}
	}

	public void NetworkFireProjectile(object[] data)
	{
		if (data != null && data.Length == 4 && GameEntityManager.ValidateDataType<float>(data[0], out var dataAsType) && !float.IsNaN(dataAsType) && !float.IsInfinity(dataAsType) && GameEntityManager.ValidateDataType<int>(data[1], out var dataAsType2) && GameEntityManager.ValidateDataType<Vector3>(data[2], out var dataAsType3) && dataAsType3.IsFinite() && GameEntityManager.ValidateDataType<Quaternion>(data[3], out var dataAsType4) && !((dataAsType3 - blaster.firingPosition.position).magnitude > blaster.maxLagDistance) && !(blaster.CurrentFireRate() > 1f / fireCooldown * (1f + fireRateGracePercentage)))
		{
			FireProjectile(dataAsType, dataAsType2, dataAsType3, dataAsType4);
		}
	}

	public void ApplyUpgradeNodes(SIUpgradeSet withUpgrades)
	{
	}

	public int CurrentBlasterChargeLevel()
	{
		int result = -1;
		for (int i = 0; i < chargeLevels.Length; i++)
		{
			if (currentCharge >= chargeLevels[i].chargeThreshold)
			{
				result = i;
				continue;
			}
			return result;
		}
		return result;
	}
}

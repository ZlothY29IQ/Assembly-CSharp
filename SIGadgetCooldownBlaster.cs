using UnityEngine;

public class SIGadgetCooldownBlaster : MonoBehaviour, SIGadgetBlasterType
{
	public SIGadgetBlasterProjectile projectilePrefab;

	public float fireCooldown = 0.5f;

	public float fireRateGracePercentage = 0.25f;

	public float availableToFireHapticStrength = 0.1f;

	public float availableToFireHapticDuration = 0.01f;

	public float firingHapticStrength = 0.25f;

	public float firingHapticDuration = 0.01f;

	public AudioClip firingClip;

	public AudioClip cooldownClip;

	public float firingVolume;

	public float cooldownVolume;

	public ParticleSystem fireFX;

	public MeshRenderer cooldownIndicator;

	public Material readyToFireMaterial;

	public Material onCooldownMaterial;

	private bool triggerHeldDown;

	private SIGadgetBlaster blaster;

	private bool CheckInput()
	{
		return blaster.CheckInput();
	}

	private void OnEnable()
	{
		blaster = GetComponent<SIGadgetBlaster>();
		blaster.firingSource.clip = firingClip;
		blaster.firingSource.volume = firingVolume;
		blaster.firingSource.loop = false;
		blaster.blasterSource.clip = cooldownClip;
		blaster.blasterSource.volume = cooldownVolume;
		blaster.blasterSource.loop = false;
	}

	public void OnUpdateAuthority(float dt)
	{
		switch (blaster.currentState)
		{
		case SIGadgetBlasterState.Idle:
			if (!CheckInput())
			{
				triggerHeldDown = false;
			}
			else if (!triggerHeldDown)
			{
				triggerHeldDown = true;
				FireProjectile(blaster.NextFireId(), blaster.firingPosition.position, blaster.firingPosition.rotation);
				blaster.SetStateAuthority(SIGadgetBlasterState.Cooldown);
			}
			break;
		case SIGadgetBlasterState.Cooldown:
			if (!(Time.time < blaster.lastFired + fireCooldown))
			{
				blaster.FireProjectileHaptics(availableToFireHapticStrength, 0.02f);
				blaster.SetStateAuthority(SIGadgetBlasterState.Idle);
			}
			break;
		}
	}

	public void OnUpdateRemote(float dt)
	{
		if (blaster.currentState != SIGadgetBlasterState.Idle)
		{
			_ = 2;
		}
	}

	public void SetStateShared()
	{
		switch (blaster.currentState)
		{
		case SIGadgetBlasterState.Idle:
			cooldownIndicator.sharedMaterial = readyToFireMaterial;
			break;
		case SIGadgetBlasterState.Cooldown:
			blaster.lastFired = Time.time;
			cooldownIndicator.sharedMaterial = onCooldownMaterial;
			break;
		}
	}

	public void FireProjectile(int fireId, Vector3 position, Quaternion rotation)
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
			blaster.FireProjectileHaptics(firingHapticStrength, firingHapticDuration);
			blaster.SendClientToClientRPC(0, new object[3] { fireId, position, rotation });
		}
		blaster.firingSource.time = 0f;
		blaster.firingSource.Play();
		blaster.blasterSource.time = 0f;
		blaster.blasterSource.Play();
		blaster.InstantiateProjectile(projectilePrefab, position, rotation, fireId);
	}

	public void NetworkFireProjectile(object[] data)
	{
		if (data != null && data.Length == 3 && GameEntityManager.ValidateDataType<int>(data[0], out var dataAsType) && GameEntityManager.ValidateDataType<Vector3>(data[1], out var dataAsType2) && dataAsType2.IsFinite() && GameEntityManager.ValidateDataType<Quaternion>(data[2], out var dataAsType3) && !((dataAsType2 - blaster.firingPosition.position).magnitude > blaster.maxLagDistance) && !(blaster.CurrentFireRate() > 1f / fireCooldown * (1f + fireRateGracePercentage)))
		{
			FireProjectile(dataAsType, dataAsType2, dataAsType3);
		}
	}

	public void ApplyUpgradeNodes(SIUpgradeSet withUpgrades)
	{
	}
}

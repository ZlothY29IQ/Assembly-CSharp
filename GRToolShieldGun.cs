using System.Collections.Generic;
using GorillaExtensions;
using GorillaLocomotion;
using Photon.Pun;
using UnityEngine;
using UnityEngine.XR;

public class GRToolShieldGun : MonoBehaviour
{
	private enum State
	{
		Idle,
		Charging,
		Firing,
		Cooldown,
		Count
	}

	public GameEntity gameEntity;

	public GRTool tool;

	public GRAttributes attributes;

	public GameObject projectilePrefab;

	public GameObject projectileTrailPrefab;

	public Transform firingTransform;

	public List<Collider> colliders;

	public float projectileSpeed = 25f;

	public Color projectileColor = new Color(0.25f, 0.25f, 1f);

	public bool allowAoeHits;

	public float aeoHitRadius = 0.5f;

	public float chargeDuration = 0.75f;

	public float flashDuration = 0.1f;

	public float cooldownDuration;

	public AudioSource audioSource;

	public AudioClip chargeSound;

	public float chargeSoundVolume = 0.5f;

	public AudioClip firingSound;

	public float firingSoundVolume = 0.5f;

	public AudioClip upgrade1FiringSound;

	public AudioClip upgrade2FiringSound;

	public AudioClip upgrade3FiringSound;

	[Header("Haptic")]
	public AbilityHaptic onHaptic;

	private State state;

	private float stateTimeRemaining;

	private bool activatedLocally;

	private bool waitingForButtonRelease;

	private float timeLastFired;

	private float cooldownMinimum = 0.35f;

	private SlingshotProjectile firedProjectile;

	private static List<VRRig> vrRigs = new List<VRRig>(10);

	private void Awake()
	{
		if (tool != null)
		{
			tool.onToolUpgraded += OnToolUpgraded;
			OnToolUpgraded(tool);
		}
	}

	private void OnToolUpgraded(GRTool tool)
	{
		if (tool.HasUpgradeInstalled(GRToolProgressionManager.ToolParts.ShieldGunStrength1))
		{
			firingSound = upgrade1FiringSound;
		}
		else if (tool.HasUpgradeInstalled(GRToolProgressionManager.ToolParts.ShieldGunStrength2))
		{
			firingSound = upgrade2FiringSound;
		}
		else if (tool.HasUpgradeInstalled(GRToolProgressionManager.ToolParts.ShieldGunStrength3))
		{
			firingSound = upgrade3FiringSound;
		}
	}

	private bool IsHeldLocal()
	{
		return gameEntity.heldByActorNumber == PhotonNetwork.LocalPlayer.ActorNumber;
	}

	public void Update()
	{
		float deltaTime = Time.deltaTime;
		if (IsHeldLocal() || activatedLocally)
		{
			OnUpdateAuthority(deltaTime);
		}
		else
		{
			OnUpdateRemote(deltaTime);
		}
	}

	private void OnUpdateAuthority(float dt)
	{
		switch (state)
		{
		case State.Idle:
			if (tool.HasEnoughEnergy() && IsButtonHeld())
			{
				SetStateAuthority(State.Charging);
				activatedLocally = true;
			}
			break;
		case State.Charging:
		{
			bool flag = IsButtonHeld();
			stateTimeRemaining -= dt;
			if (stateTimeRemaining <= 0f)
			{
				SetStateAuthority(State.Firing);
			}
			else if (!flag)
			{
				SetStateAuthority(State.Idle);
				activatedLocally = false;
			}
			break;
		}
		case State.Firing:
			stateTimeRemaining -= dt;
			if (stateTimeRemaining <= 0f)
			{
				SetStateAuthority(State.Cooldown);
			}
			break;
		case State.Cooldown:
			stateTimeRemaining -= dt;
			if (stateTimeRemaining <= 0f && !IsButtonHeld())
			{
				SetStateAuthority(State.Idle);
				activatedLocally = false;
			}
			break;
		}
	}

	private void OnUpdateRemote(float dt)
	{
		State state = (State)gameEntity.GetState();
		if (state != this.state)
		{
			SetStateAuthority(state);
		}
	}

	private void SetStateAuthority(State newState)
	{
		SetState(newState);
		gameEntity.RequestState(gameEntity.id, (long)newState);
	}

	private void SetState(State newState)
	{
		if (newState != state && CanChangeState((long)newState))
		{
			state = newState;
			switch (state)
			{
			case State.Charging:
				StartCharge();
				stateTimeRemaining = chargeDuration;
				break;
			case State.Firing:
				StartFiring();
				stateTimeRemaining = flashDuration;
				break;
			case State.Cooldown:
				stateTimeRemaining = cooldownDuration;
				break;
			case State.Idle:
				stateTimeRemaining = -1f;
				break;
			}
		}
	}

	private void StartCharge()
	{
		if (chargeSound != null)
		{
			audioSource.PlayOneShot(chargeSound, chargeSoundVolume);
		}
		if (IsHeldLocal())
		{
			PlayVibration(GorillaTagger.Instance.tapHapticStrength, chargeDuration);
		}
	}

	private void StartFiring()
	{
		if (firingSound != null)
		{
			audioSource.PlayOneShot(firingSound, firingSoundVolume);
		}
		timeLastFired = Time.time;
		tool.UseEnergy();
		Vector3 position = firingTransform.position;
		Vector3 velocity = firingTransform.forward * projectileSpeed;
		float scale = GTPlayer.Instance.scale;
		int hash = PoolUtils.GameObjHashCode(projectilePrefab);
		firedProjectile = ObjectPools.instance.Instantiate(hash).GetComponent<SlingshotProjectile>();
		firedProjectile.transform.localScale = Vector3.one * scale;
		if (projectileTrailPrefab != null)
		{
			int trailHash = PoolUtils.GameObjHashCode(projectileTrailPrefab);
			AttachTrail(trailHash, firedProjectile.gameObject, position, blueTeam: false, orangeTeam: false);
		}
		Collider component = firedProjectile.gameObject.GetComponent<Collider>();
		if (component != null)
		{
			for (int i = 0; i < colliders.Count; i++)
			{
				Physics.IgnoreCollision(colliders[i], component);
			}
		}
		if (IsHeldLocal())
		{
			firedProjectile.OnImpact += OnProjectileImpact;
		}
		onHaptic.PlayIfHeldLocal(gameEntity);
		firedProjectile.Launch(position, velocity, NetworkSystem.Instance.LocalPlayer, blueTeam: false, orangeTeam: false, 1, scale, shouldOverrideColor: true, projectileColor);
	}

	private void AttachTrail(int trailHash, GameObject newProjectile, Vector3 location, bool blueTeam, bool orangeTeam)
	{
		GameObject gameObject = ObjectPools.instance.Instantiate(trailHash);
		SlingshotProjectileTrail component = gameObject.GetComponent<SlingshotProjectileTrail>();
		if (component.IsNull())
		{
			ObjectPools.instance.Destroy(gameObject);
		}
		newProjectile.transform.position = location;
		component.AttachTrail(newProjectile, blueTeam, orangeTeam);
	}

	private void OnProjectileImpact(SlingshotProjectile projectile, Vector3 impactPos, NetPlayer hitPlayer)
	{
		projectile.OnImpact -= OnProjectileImpact;
		GRPlayer gRPlayer = null;
		if (hitPlayer != null && VRRigCache.Instance.TryGetVrrig(hitPlayer, out var playerRig) && playerRig.Rig != null)
		{
			gRPlayer = playerRig.Rig.GetComponent<GRPlayer>();
		}
		else if (allowAoeHits)
		{
			vrRigs.Clear();
			vrRigs.Add(VRRig.LocalRig);
			VRRigCache.Instance.GetAllUsedRigs(vrRigs);
			VRRig vRRig = null;
			float num = float.MaxValue;
			for (int i = 0; i < vrRigs.Count; i++)
			{
				float sqrMagnitude = (vrRigs[i].bodyTransform.position - impactPos).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					vRRig = vrRigs[i];
				}
			}
			if (vRRig != null)
			{
				gRPlayer = vRRig.GetComponent<GRPlayer>();
			}
		}
		if (gRPlayer != null)
		{
			int num2 = 0;
			if (tool.HasUpgradeInstalled(GRToolProgressionManager.ToolParts.ShieldGunStrength1))
			{
				num2 |= 1;
			}
			if (tool.HasUpgradeInstalled(GRToolProgressionManager.ToolParts.ShieldGunStrength2))
			{
				num2 |= 2;
			}
			if (tool.HasUpgradeInstalled(GRToolProgressionManager.ToolParts.ShieldGunStrength3))
			{
				num2 |= 4;
			}
			gameEntity.manager.ghostReactorManager.RequestGrantPlayerShield(gRPlayer, attributes.CalculateFinalValueForAttribute(GRAttributeType.ShieldSize), num2);
		}
	}

	private bool IsButtonHeld()
	{
		if (!IsHeldLocal())
		{
			return false;
		}
		if (!GamePlayer.TryGetGamePlayer(gameEntity.heldByActorNumber, out var out_gamePlayer))
		{
			return false;
		}
		int num = out_gamePlayer.FindHandIndex(gameEntity.id);
		if (num == -1)
		{
			return false;
		}
		return ControllerInputPoller.TriggerFloat(GamePlayer.IsLeftHand(num) ? XRNode.LeftHand : XRNode.RightHand) > 0.25f;
	}

	private void PlayVibration(float strength, float duration)
	{
		if (IsHeldLocal() && GamePlayer.TryGetGamePlayer(gameEntity.heldByActorNumber, out var out_gamePlayer))
		{
			int num = out_gamePlayer.FindHandIndex(gameEntity.id);
			if (num != -1)
			{
				GorillaTagger.Instance.StartVibration(GamePlayer.IsLeftHand(num), strength, duration);
			}
		}
	}

	public bool CanChangeState(long newStateIndex)
	{
		if (newStateIndex < 0 || newStateIndex >= 4)
		{
			return false;
		}
		if ((int)newStateIndex == 2)
		{
			return Time.time > timeLastFired + cooldownMinimum;
		}
		return true;
	}
}

using System;
using GorillaExtensions;
using GorillaGameModes;
using GorillaLocomotion;
using GorillaLocomotion.Swimming;
using GorillaTag.Gravity;
using GorillaTag.Reactions;
using UnityEngine;
using UnityEngine.Events;

public class SlingshotProjectile : MonoBehaviour
{
	[Serializable]
	public struct AOEKnockbackConfig
	{
		public bool applyAOEKnockback;

		[Tooltip("Full knockback velocity is imparted within the inner radius")]
		public float aeoInnerRadius;

		[Tooltip("Partial knockback velocity is imparted between the inner and outer radius")]
		public float aeoOuterRadius;

		public float knockbackVelocity;

		[Tooltip("The required impact velocity to achieve full knockback velocity")]
		public float impactVelocityThreshold;

		[SerializeField]
		public PlayerEffect playerProximityEffect;
	}

	public delegate void ProjectileImpactEvent(SlingshotProjectile projectile, Vector3 impactPos, NetPlayer hitPlayer);

	public NetPlayer projectileOwner;

	[Tooltip("Rotates to point along the Y axis after spawn.")]
	public GameObject surfaceImpactEffectPrefab;

	[Tooltip("if left empty, the default player impact that is set in Room System Setting will be played")]
	public GameObject playerImpactEffectPrefab;

	[Tooltip("Distance from the surface that the particle should spawn.")]
	[SerializeField]
	private float impactEffectOffset;

	[SerializeField]
	private SoundBankPlayer launchSoundBankPlayer;

	[SerializeField]
	private bool dontDestroyOnHit;

	[SerializeField]
	private LayerMask floorLayerMask;

	[SerializeField]
	private float placementOffset = 0.01f;

	[SerializeField]
	private bool keepRotationUpright = true;

	public float lifeTime = 20f;

	public float gravityMultiplier = 1f;

	public bool useForwardForce;

	public float forwardForceMultiplier = 0.1f;

	public Color defaultColor = Color.white;

	public Color orangeColor = new Color(1f, 0.5f, 0f, 1f);

	public Color blueColor = new Color(0f, 0.72f, 1f, 1f);

	[Tooltip("Renderers with team specific meshes, materials, effects, etc.")]
	public Renderer defaultBall;

	[Tooltip("Renderers with team specific meshes, materials, effects, etc.")]
	public Renderer orangeBall;

	[Tooltip("Renderers with team specific meshes, materials, effects, etc.")]
	public Renderer blueBall;

	public bool colorizeBalls;

	public bool faceDirectionOfTravel = true;

	private bool particleLaunched;

	private float timeCreated;

	private Rigidbody projectileRigidbody;

	private Color teamColor = Color.white;

	private Renderer teamRenderer;

	public int myProjectileCount;

	private float initialScale;

	private Vector3 previousPosition;

	[HideInInspector]
	public AOEKnockbackConfig? aoeKnockbackConfig;

	[HideInInspector]
	public float? impactSoundVolumeOverride;

	[HideInInspector]
	public float? impactSoundPitchOverride;

	[HideInInspector]
	public float impactEffectScaleMultiplier = 1f;

	private ConstantForce forceComponent;

	public bool m_sendNetworkedImpact = true;

	public UnityEvent<NetPlayer> OnLaunch;

	public UnityEvent<Vector3> OnImapctEvent;

	private MaterialPropertyBlock matPropBlock;

	private SpawnWorldEffects spawnWorldEffects;

	public UnityEvent<VRRig> OnHitPlayer;

	private float remainingLifeTime;

	private bool isSettled;

	private float distanceTraveled;

	private MonkeGravityController gravityController;

	public Vector3 launchPosition { get; private set; }

	public event ProjectileImpactEvent OnImpact;

	public void Launch(Vector3 position, Vector3 velocity, NetPlayer player, bool blueTeam, bool orangeTeam, int projectileCount, float scale, bool shouldOverrideColor = false, Color overrideColor = default(Color))
	{
		if (launchSoundBankPlayer != null)
		{
			launchSoundBankPlayer.Play();
		}
		particleLaunched = true;
		timeCreated = Time.time;
		launchPosition = position;
		Transform obj = base.transform;
		obj.position = position;
		obj.localScale = Vector3.one * scale;
		GetComponent<Collider>().contactOffset = 0.01f * scale;
		RigidbodyWaterInteraction component = GetComponent<RigidbodyWaterInteraction>();
		if (component != null)
		{
			component.objectRadiusForWaterCollision = 0.02f * scale;
		}
		gravityController.GravityMultiplier = gravityMultiplier * ((scale < 1f) ? scale : 1f);
		projectileRigidbody.isKinematic = false;
		projectileRigidbody.useGravity = false;
		projectileRigidbody.linearVelocity = velocity;
		projectileOwner = player;
		myProjectileCount = projectileCount;
		projectileRigidbody.position = position;
		ApplyTeamModelAndColor(blueTeam, orangeTeam, shouldOverrideColor, overrideColor);
		remainingLifeTime = lifeTime;
		if (useForwardForce && (bool)forceComponent)
		{
			forceComponent.enabled = true;
			forceComponent.force = projectileRigidbody.linearVelocity.normalized * forwardForceMultiplier;
		}
		isSettled = false;
		if (VRRigCache.Instance.TryGetVrrig(player, out var playerRig))
		{
			gravityController.SetPersonalGravityDirection(playerRig.Rig.transform.up);
		}
		OnLaunch?.Invoke(projectileOwner);
	}

	protected void Awake()
	{
		if (playerImpactEffectPrefab == null)
		{
			playerImpactEffectPrefab = surfaceImpactEffectPrefab;
		}
		projectileRigidbody = GetComponent<Rigidbody>();
		forceComponent = GetComponent<ConstantForce>();
		initialScale = base.transform.localScale.x;
		matPropBlock = new MaterialPropertyBlock();
		spawnWorldEffects = GetComponent<SpawnWorldEffects>();
		remainingLifeTime = lifeTime;
		gravityController = GetComponent<MonkeGravityController>();
		if (gravityController == null)
		{
			gravityController = base.gameObject.AddComponent<MonkeGravityController>();
		}
	}

	public void Deactivate()
	{
		base.transform.localScale = Vector3.one * initialScale;
		projectileRigidbody.useGravity = true;
		if ((bool)forceComponent)
		{
			forceComponent.force = Vector3.zero;
		}
		this.OnImpact = null;
		aoeKnockbackConfig = null;
		impactSoundVolumeOverride = null;
		impactSoundPitchOverride = null;
		impactEffectScaleMultiplier = 1f;
		projectileRigidbody.isKinematic = false;
		ObjectPools.instance.Destroy(base.gameObject);
	}

	private void SpawnImpactEffect(GameObject prefab, Vector3 position, Vector3 normal)
	{
		if (!(prefab == null))
		{
			Vector3 position2 = position + normal * impactEffectOffset;
			GameObject obj = ObjectPools.instance.Instantiate(prefab, position2);
			Vector3 localScale = base.transform.localScale;
			obj.transform.localScale = localScale * impactEffectScaleMultiplier;
			obj.transform.up = normal;
			GorillaColorizableBase component = obj.GetComponent<GorillaColorizableBase>();
			if (component != null)
			{
				component.SetColor(teamColor);
			}
			SurfaceImpactFX component2 = obj.GetComponent<SurfaceImpactFX>();
			if (component2 != null)
			{
				component2.SetScale(localScale.x * impactEffectScaleMultiplier);
			}
			SoundBankPlayer component3 = obj.GetComponent<SoundBankPlayer>();
			if (component3 != null && !component3.playOnEnable)
			{
				component3.Play(impactSoundVolumeOverride, impactSoundPitchOverride);
			}
			if (spawnWorldEffects != null)
			{
				spawnWorldEffects.RequestSpawn(position, normal);
			}
			OnImapctEvent?.Invoke(position);
		}
	}

	public void CheckForAOEKnockback(Vector3 impactPosition, float impactSpeed)
	{
		if (!aoeKnockbackConfig.HasValue || !aoeKnockbackConfig.Value.applyAOEKnockback)
		{
			return;
		}
		Vector3 vector = GTPlayer.Instance.HeadCenterPosition - impactPosition;
		if (vector.sqrMagnitude < aoeKnockbackConfig.Value.aeoOuterRadius * aoeKnockbackConfig.Value.aeoOuterRadius)
		{
			float magnitude = vector.magnitude;
			Vector3 direction = ((magnitude > 0.001f) ? (vector / magnitude) : Vector3.up);
			float num = Mathf.InverseLerp(aoeKnockbackConfig.Value.aeoOuterRadius, aoeKnockbackConfig.Value.aeoInnerRadius, magnitude);
			float num2 = Mathf.InverseLerp(0f, aoeKnockbackConfig.Value.impactVelocityThreshold, impactSpeed);
			GTPlayer.Instance.ApplyKnockback(direction, aoeKnockbackConfig.Value.knockbackVelocity * num * num2);
			impactEffectScaleMultiplier = Mathf.Lerp(1f, impactEffectScaleMultiplier, num2);
			if (impactSoundVolumeOverride.HasValue)
			{
				impactSoundVolumeOverride = Mathf.Lerp(impactSoundVolumeOverride.Value * 0.5f, impactSoundVolumeOverride.Value, num2);
			}
			float num3 = Mathf.Lerp(aoeKnockbackConfig.Value.aeoInnerRadius, aoeKnockbackConfig.Value.aeoOuterRadius, 0.25f);
			if (aoeKnockbackConfig.Value.playerProximityEffect != PlayerEffect.NONE && vector.sqrMagnitude < num3 * num3)
			{
				RoomSystem.SendPlayerEffect(PlayerEffect.SNOWBALL_IMPACT, NetworkSystem.Instance.LocalPlayer);
			}
		}
	}

	public void ApplyTeamModelAndColor(bool blueTeam, bool orangeTeam, bool shouldOverrideColor = false, Color overrideColor = default(Color))
	{
		if (shouldOverrideColor)
		{
			teamColor = overrideColor;
		}
		else
		{
			teamColor = (blueTeam ? blueColor : (orangeTeam ? orangeColor : defaultColor));
		}
		blueBall.enabled = blueTeam;
		orangeBall.enabled = orangeTeam;
		defaultBall.enabled = !blueTeam && !orangeTeam;
		teamRenderer = (blueTeam ? blueBall : (orangeTeam ? orangeBall : defaultBall));
		ApplyColor(teamRenderer, (colorizeBalls || shouldOverrideColor) ? teamColor : Color.white);
	}

	protected void OnEnable()
	{
		timeCreated = 0f;
		particleLaunched = false;
		SlingshotProjectileManager.RegisterSP(this);
	}

	protected void OnDisable()
	{
		particleLaunched = false;
		SlingshotProjectileManager.UnregisterSP(this);
	}

	public void InvokeUpdate()
	{
		if (particleLaunched || dontDestroyOnHit)
		{
			if (Time.time > timeCreated + GetRemainingLifeTime())
			{
				DestroyAfterRelease();
			}
			if (faceDirectionOfTravel)
			{
				Transform transform = base.transform;
				Vector3 position = transform.position;
				Vector3 forward = position - previousPosition;
				transform.rotation = ((forward.sqrMagnitude > 0f) ? Quaternion.LookRotation(forward) : transform.rotation);
				previousPosition = position;
			}
		}
		if (dontDestroyOnHit)
		{
			SettleProjectile();
		}
	}

	public void DestroyAfterRelease()
	{
		SpawnImpactEffect(surfaceImpactEffectPrefab, base.transform.position, Vector3.up);
		Deactivate();
	}

	public float GetRemainingLifeTime()
	{
		return remainingLifeTime;
	}

	public void UpdateRemainingLifeTime(float newLifeTime)
	{
		remainingLifeTime = newLifeTime;
	}

	public float GetDistanceTraveled()
	{
		return (base.transform.position - launchPosition).magnitude;
	}

	private void SettleProjectile()
	{
		if (!isSettled)
		{
			int value = floorLayerMask.value;
			if (Physics.Raycast(base.transform.position, Vector3.down, out var hitInfo, 0.1f, value, QueryTriggerInteraction.Ignore) && Vector3.Angle(hitInfo.normal, Vector3.up) < 40f)
			{
				if ((bool)forceComponent)
				{
					forceComponent.force = Vector3.zero;
				}
				projectileRigidbody.angularVelocity = Vector3.zero;
				projectileRigidbody.linearVelocity = Vector3.zero;
				projectileRigidbody.isKinematic = true;
				base.transform.position = hitInfo.point + Vector3.up * placementOffset;
				isSettled = true;
			}
		}
		else if (keepRotationUpright)
		{
			Quaternion rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(base.transform.up, Vector3.up).normalized, Vector3.up);
			base.transform.rotation = rotation;
		}
	}

	protected void OnCollisionEnter(Collision collision)
	{
		if (particleLaunched && !dontDestroyOnHit)
		{
			if (collision.collider.gameObject.TryGetComponent<SlingshotProjectileHitNotifier>(out var component))
			{
				component.InvokeHit(this, collision);
			}
			ContactPoint contact = collision.GetContact(0);
			CheckForAOEKnockback(contact.point, collision.relativeVelocity.magnitude);
			SpawnImpactEffect(surfaceImpactEffectPrefab, contact.point, contact.normal);
			this.OnImpact?.Invoke(this, contact.point, null);
			Deactivate();
		}
	}

	protected void OnCollisionStay(Collision collision)
	{
		if (particleLaunched && !dontDestroyOnHit)
		{
			if (collision.gameObject.TryGetComponent<SlingshotProjectileHitNotifier>(out var component))
			{
				component.InvokeCollisionStay(this, collision);
			}
			ContactPoint contact = collision.GetContact(0);
			CheckForAOEKnockback(contact.point, collision.relativeVelocity.magnitude);
			SpawnImpactEffect(surfaceImpactEffectPrefab, contact.point, contact.normal);
			this.OnImpact?.Invoke(this, contact.point, null);
			Deactivate();
		}
	}

	protected void OnTriggerExit(Collider other)
	{
		if (particleLaunched && other.gameObject.TryGetComponent<SlingshotProjectileHitNotifier>(out var component))
		{
			component.InvokeTriggerExit(this, other);
		}
	}

	protected void OnTriggerEnter(Collider other)
	{
		if (!particleLaunched)
		{
			return;
		}
		if (other.gameObject.TryGetComponent<SlingshotProjectileHitNotifier>(out var component))
		{
			component.InvokeTriggerEnter(this, other);
		}
		if (projectileOwner == NetworkSystem.Instance.LocalPlayer)
		{
			if (!NetworkSystem.Instance.InRoom || GorillaGameManager.instance == null)
			{
				return;
			}
			GorillaPaintbrawlManager component2 = GorillaGameManager.instance.gameObject.GetComponent<GorillaPaintbrawlManager>();
			if (!other.gameObject.IsOnLayer(UnityLayer.GorillaTagCollider) && !other.gameObject.IsOnLayer(UnityLayer.GorillaSlingshotCollider))
			{
				return;
			}
			NetPlayer netPlayer = other.GetComponentInParent<VRRig>()?.creator;
			if (netPlayer == null)
			{
				return;
			}
			this.OnImpact?.Invoke(this, base.transform.position, netPlayer);
			if (NetworkSystem.Instance.LocalPlayer == netPlayer || ((bool)component2 && !component2.LocalCanHit(NetworkSystem.Instance.LocalPlayer, netPlayer)))
			{
				return;
			}
			if ((bool)component2 && (bool)GameMode.ActiveNetworkHandler)
			{
				GameMode.ActiveNetworkHandler.SendRPC("RPC_ReportSlingshotHit", false, (netPlayer as PunNetPlayer).PlayerRef, base.transform.position, myProjectileCount);
				PlayerGameEvents.GameModeObjectiveTriggered();
			}
			if (m_sendNetworkedImpact)
			{
				RoomSystem.SendImpactEffect(base.transform.position, teamColor.r, teamColor.g, teamColor.b, teamColor.a, myProjectileCount);
			}
			Deactivate();
		}
		Rigidbody attachedRigidbody = other.attachedRigidbody;
		if (attachedRigidbody.IsNotNull() && attachedRigidbody.gameObject.TryGetComponent<VRRig>(out var component3))
		{
			OnHitPlayer?.Invoke(component3);
		}
	}

	private void ApplyColor(Renderer rend, Color color)
	{
		if ((bool)rend)
		{
			matPropBlock.SetColor(ShaderProps._BaseColor, color);
			matPropBlock.SetColor(ShaderProps._Color, color);
			rend.SetPropertyBlock(matPropBlock);
		}
	}
}

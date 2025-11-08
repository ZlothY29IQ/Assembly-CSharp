using UnityEngine;

public class GRRangedEnemyProjectile : MonoBehaviour, IGameEntityComponent, IGameHittable, IGameHitter
{
	private int owningEntityNetID;

	private GameEntity entity;

	public GameEntity owningEntity;

	private IGameProjectileLauncher projectileLauncher;

	public Rigidbody projectileRigidbody;

	private ParticleSystem particleSystem;

	private AudioSource audioSource;

	private MeshRenderer meshRenderer;

	private GameHittable hittable;

	public float projectileSpeed = 5f;

	public float projectileHitRadius = 1f;

	public float postImpactLifetime = 2f;

	private bool projectileHasImpacted;

	private double projectileImpactTime;

	private float lastHitPlayerTime;

	private float minTimeBetweenHits = 0.5f;

	public bool applyFreezeEffect;

	public bool canHitPlayer = true;

	public AbilitySound hitSFX;

	private void Awake()
	{
		particleSystem = GetComponentInChildren<ParticleSystem>();
		audioSource = GetComponentInChildren<AudioSource>();
		meshRenderer = GetComponentInChildren<MeshRenderer>();
		hittable = GetComponentInChildren<GameHittable>();
		projectileRigidbody = GetComponent<Rigidbody>();
		entity = GetComponent<GameEntity>();
	}

	private void Start()
	{
		if (projectileRigidbody != null)
		{
			projectileRigidbody.linearVelocity = base.transform.forward * projectileSpeed;
		}
		projectileHasImpacted = false;
		if (!(owningEntity != null))
		{
			return;
		}
		Collider componentInChildren = GetComponentInChildren<Collider>();
		if (componentInChildren != null)
		{
			Collider[] componentsInChildren = owningEntity.gameObject.GetComponentsInChildren<Collider>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				Physics.IgnoreCollision(componentInChildren, componentsInChildren[i]);
			}
		}
	}

	private void Update()
	{
		if (entity.IsAuthority() && projectileHasImpacted && Time.timeAsDouble > projectileImpactTime + (double)postImpactLifetime)
		{
			entity.manager.RequestDestroyItem(entity.id);
		}
	}

	public void OnEntityInit()
	{
		owningEntityNetID = (int)entity.createData;
		if (owningEntityNetID != 0)
		{
			owningEntity = FindOwningEntity();
			projectileLauncher = owningEntity.GetComponent<IGameProjectileLauncher>();
			if (projectileLauncher != null)
			{
				projectileLauncher.OnProjectileInit(this);
			}
		}
	}

	public void OnEntityDestroy()
	{
	}

	public void OnEntityStateChange(long prevState, long nextState)
	{
	}

	private GameEntity FindOwningEntity()
	{
		if (owningEntityNetID != 0)
		{
			GameEntityManager gameEntityManager = GhostReactorManager.Get(entity).gameEntityManager;
			GameEntityId entityIdFromNetId = gameEntityManager.GetEntityIdFromNetId(owningEntityNetID);
			return gameEntityManager.GetGameEntity(entityIdFromNetId);
		}
		return null;
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (projectileHasImpacted)
		{
			return;
		}
		if (canHitPlayer)
		{
			Vector3 position = base.transform.position;
			if ((VRRig.LocalRig.GetMouthPosition() - position).sqrMagnitude < projectileHitRadius * projectileHitRadius && Time.time > lastHitPlayerTime + minTimeBetweenHits)
			{
				lastHitPlayerTime = Time.time;
				GhostReactorManager.Get(entity).RequestEnemyHitPlayer(GhostReactor.EnemyType.Ranged, entity.id, VRRig.LocalRig.GetComponent<GRPlayer>(), position);
			}
			if (projectileLauncher != null)
			{
				projectileLauncher.OnProjectileHit(this, collision);
			}
		}
		projectileHasImpacted = true;
		projectileImpactTime = Time.timeAsDouble;
	}

	private void OnTriggerEnter(Collider collider)
	{
		if (!projectileHasImpacted)
		{
			GRShieldCollider component = collider.GetComponent<GRShieldCollider>();
			if (component != null)
			{
				component.BlockHittable(projectileRigidbody.transform.position, projectileRigidbody.linearVelocity.normalized, hittable);
			}
		}
	}

	public bool IsHitValid(GameHitData hit)
	{
		return true;
	}

	public void OnHit(GameHitData hit)
	{
		GameHitType hitTypeId = (GameHitType)hit.hitTypeId;
		GRTool gameComponent = entity.manager.GetGameComponent<GRTool>(hit.hitByEntityId);
		if (gameComponent != null)
		{
			switch (hitTypeId)
			{
			case GameHitType.Club:
				OnHitByClub(gameComponent, hit);
				break;
			case GameHitType.Flash:
				OnHitByFlash(gameComponent, hit);
				break;
			case GameHitType.Shield:
				OnHitByShield(gameComponent, hit);
				break;
			}
		}
	}

	public void OnHitByClub(GRTool tool, GameHitData hit)
	{
		projectileHasImpacted = true;
		projectileImpactTime = Time.timeAsDouble;
		if (projectileRigidbody != null)
		{
			PlayImpactFX();
			projectileRigidbody.linearVelocity = hit.hitImpulse * (projectileRigidbody.linearVelocity.magnitude * 0.7f);
		}
	}

	public void OnHitByFlash(GRTool grTool, GameHitData hit)
	{
	}

	public void OnHitByShield(GRTool tool, GameHitData hit)
	{
		projectileHasImpacted = true;
		projectileImpactTime = Time.timeAsDouble;
		if (projectileRigidbody != null)
		{
			PlayImpactFX();
			projectileRigidbody.linearVelocity = hit.hitImpulse;
		}
	}

	private void PlayImpactFX()
	{
		if (particleSystem != null)
		{
			particleSystem.Play();
		}
		if (meshRenderer != null)
		{
			meshRenderer.enabled = false;
		}
	}

	public void OnSuccessfulHit(GameHitData hit)
	{
		PlayImpactFX();
	}

	public void OnSuccessfulHitPlayer(GRPlayer player, Vector3 hitPosition)
	{
		PlayImpactFX();
		hitSFX.Play(null);
		if (applyFreezeEffect)
		{
			player.SetAsFrozen(4f);
		}
	}
}

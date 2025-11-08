using System;
using GorillaExtensions;
using UnityEngine;

public class GRCollectibleDispenser : MonoBehaviour, IGameEntityComponent
{
	public GameEntity gameEntity;

	public GameEntity collectiblePrefab;

	public Transform spawnLocation;

	public LayerMask collectibleLayerMask;

	public float collectibleRespawnTimeMinutes = 1.5f;

	public int maxDispenseCount = 3;

	public AudioSource audioSource;

	public Transform stillDispensingModel;

	public Transform fullyConsumedModel;

	public ParticleSystem collectibleTakenEffect;

	public AudioClip collectibleTakenClip;

	public float collectibleTakenVolume;

	public ParticleSystem dispenserExhaustedEffect;

	public AudioClip dispenserExhaustedClip;

	public float dispenserExhaustedVolume;

	private GRCollectible currentCollectible;

	private Coroutine getSpawnedCollectibleCoroutine;

	private static Collider[] overlapColliders = new Collider[10];

	private uint collectiblesDispensed;

	private uint collectiblesCollected;

	private double collectibleDispenseRequestTime = -10000.0;

	private double collectibleDispenseTime = -10000.0;

	private double collectibleCollectedTime = -10000.0;

	public bool CollectibleAlreadySpawned => currentCollectible != null;

	public bool ReadyToDispenseNewCollectible
	{
		get
		{
			double num = (double)collectibleRespawnTimeMinutes * 60.0;
			bool flag = collectiblesDispensed < maxDispenseCount;
			if (!CollectibleAlreadySpawned && flag && Time.timeAsDouble - collectibleDispenseRequestTime > num && Time.timeAsDouble - collectibleDispenseTime > num)
			{
				return Time.timeAsDouble - collectibleCollectedTime > num;
			}
			return false;
		}
	}

	public void OnEntityInit()
	{
		GhostReactor reactor = GhostReactorManager.Get(gameEntity).reactor;
		if (reactor != null)
		{
			reactor.collectibleDispensers.Add(this);
		}
	}

	public void OnEntityDestroy()
	{
		GhostReactorManager ghostReactorManager = GhostReactorManager.Get(gameEntity);
		if (ghostReactorManager != null && ghostReactorManager.reactor != null)
		{
			ghostReactorManager.reactor.collectibleDispensers.Remove(this);
		}
	}

	public void OnEntityStateChange(long prevState, long nextState)
	{
		uint num = collectiblesDispensed;
		uint num2 = collectiblesCollected;
		collectiblesDispensed = (uint)(nextState >> 32);
		collectiblesCollected = (uint)(nextState & 0xFFFFFFFFu);
		if (num != collectiblesDispensed)
		{
			collectibleDispenseTime = Time.timeAsDouble;
		}
		if (num2 != collectiblesCollected)
		{
			collectibleCollectedTime = Time.timeAsDouble;
		}
		if (collectiblesCollected >= maxDispenseCount)
		{
			stillDispensingModel.gameObject.SetActive(value: false);
			fullyConsumedModel.gameObject.SetActive(value: true);
		}
	}

	public void RequestDispenseCollectible()
	{
		if (ReadyToDispenseNewCollectible && gameEntity.IsAuthority())
		{
			gameEntity.manager.RequestCreateItem(collectiblePrefab.name.GetStaticHash(), spawnLocation.position, spawnLocation.rotation, gameEntity.manager.GetNetIdFromEntityId(gameEntity.id));
			collectiblesDispensed++;
			collectibleDispenseTime = Time.timeAsDouble;
			long num = collectiblesDispensed;
			long num2 = collectiblesCollected;
			long newState = (num << 32) | num2;
			gameEntity.RequestState(gameEntity.id, newState);
		}
	}

	public void OnCollectibleConsumed()
	{
		if (currentCollectible != null && currentCollectible.IsNotNull())
		{
			GRCollectible gRCollectible = currentCollectible;
			gRCollectible.OnCollected = (Action)Delegate.Remove(gRCollectible.OnCollected, new Action(OnCollectibleConsumed));
			GameEntity entity = currentCollectible.entity;
			entity.OnGrabbed = (Action)Delegate.Remove(entity.OnGrabbed, new Action(OnCollectibleConsumed));
			currentCollectible = null;
		}
		collectiblesCollected++;
		collectibleCollectedTime = Time.timeAsDouble;
		if (gameEntity.IsAuthority())
		{
			long num = collectiblesDispensed;
			long num2 = collectiblesCollected;
			long newState = (num << 32) | num2;
			gameEntity.RequestState(gameEntity.id, newState);
		}
		if (collectiblesCollected >= maxDispenseCount)
		{
			dispenserExhaustedEffect.Play();
			audioSource.PlayOneShot(dispenserExhaustedClip, dispenserExhaustedVolume);
			stillDispensingModel.gameObject.SetActive(value: false);
			fullyConsumedModel.gameObject.SetActive(value: true);
		}
		else
		{
			collectibleTakenEffect.Play();
			audioSource.PlayOneShot(collectibleTakenClip, collectibleTakenVolume);
		}
	}

	public void GetSpawnedCollectible(GRCollectible collectible)
	{
		currentCollectible = collectible;
		collectible.OnCollected = (Action)Delegate.Combine(collectible.OnCollected, new Action(OnCollectibleConsumed));
		GameEntity entity = collectible.entity;
		entity.OnGrabbed = (Action)Delegate.Combine(entity.OnGrabbed, new Action(OnCollectibleConsumed));
	}
}

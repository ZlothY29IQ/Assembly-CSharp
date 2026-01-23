using System;
using System.Collections.Generic;
using GorillaTagScripts.GhostReactor;
using UnityEngine;

[Serializable]
public class GRAbilityDie : GRAbilityBase
{
	public float delayDeath;

	public float delayRespawn = -1f;

	public List<Renderer> hideWhenDead;

	public List<Collider> disableCollidersWhenDead;

	public bool disableAllCollidersWhenDead;

	public bool disableAllRenderersWhenDead;

	public GameObject fxDeath;

	public AbilitySound soundDeath;

	public AbilitySound soundOnHide;

	public float destroyDelay = 3f;

	public bool doKnockback = true;

	public GRBreakableItemSpawnConfig lootTable;

	public bool spawnOnGround;

	public LayerMask groundLayerMask;

	public Transform lootSpawnMarker;

	public List<AnimationData> animData;

	private int instigatingActorNumber;

	private bool isDead;

	private float totalDeathDelay;

	public GRAbilityInterpolatedMovement staggerMovement;

	public GameAbilityEvents events;

	private bool reported;

	public override void Setup(GameAgent agent, Animation anim, AudioSource audioSource, Transform root, Transform head, GRSenseLineOfSight lineOfSight)
	{
		base.Setup(agent, anim, audioSource, root, head, lineOfSight);
		if (disableAllCollidersWhenDead)
		{
			agent.GetComponentsInChildren(disableCollidersWhenDead);
		}
		if (disableAllRenderersWhenDead)
		{
			agent.GetComponentsInChildren(hideWhenDead);
		}
		Disable(disableCollidersWhenDead, disable: false);
		staggerMovement.Setup(root);
	}

	protected override void OnStart()
	{
		totalDeathDelay = delayDeath;
		if (animData.Count > 0)
		{
			int index = UnityEngine.Random.Range(0, animData.Count);
			totalDeathDelay += animData[index].duration;
			staggerMovement.InitFromVelocityAndDuration(staggerMovement.velocity, totalDeathDelay);
			PlayAnim(animData[index].animName, 0.1f, animData[index].speed);
		}
		agent.SetIsPathing(isPathing: false, ignoreRigiBody: true);
		agent.SetDisableNetworkSync(disable: true);
		isDead = false;
		if (doKnockback)
		{
			staggerMovement.Start();
		}
		soundDeath.soundSelectMode = AbilitySound.SoundSelectMode.Random;
		soundOnHide.soundSelectMode = AbilitySound.SoundSelectMode.Random;
		soundDeath.Play(null);
		Disable(disableCollidersWhenDead, disable: true);
		if (fxDeath != null)
		{
			fxDeath.SetActive(value: false);
		}
		events.Reset();
		events.OnAbilityStart(GetAbilityTime(Time.timeAsDouble), audioSource);
	}

	protected override void OnStop()
	{
		staggerMovement.Stop();
		agent.SetIsPathing(isPathing: true, ignoreRigiBody: true);
		agent.SetDisableNetworkSync(disable: false);
		Hide(hideWhenDead, hide: false);
		Disable(disableCollidersWhenDead, disable: false);
		events.OnAbilityStop(GetAbilityTime(Time.timeAsDouble), audioSource);
	}

	public void SetStaggerVelocity(Vector3 vel)
	{
		float magnitude = vel.magnitude;
		if (magnitude > 0f)
		{
			Vector3 vector = vel / magnitude;
			vector.y = 0f;
			vel = vector * magnitude;
		}
		staggerMovement.InitFromVelocityAndDuration(vel, totalDeathDelay);
	}

	public void SetInstigatingPlayerIndex(int actorNumber)
	{
		Debug.Log($"SetInstigatingPlayerIndex {actorNumber}");
		instigatingActorNumber = actorNumber;
	}

	private void Die()
	{
		soundOnHide.Play(null);
		if (fxDeath != null)
		{
			fxDeath.SetActive(value: false);
			fxDeath.SetActive(value: true);
		}
		Hide(hideWhenDead, hide: true);
		Disable(disableCollidersWhenDead, disable: true);
		GameEntity gameEntity = agent.entity;
		if (lootTable != null && gameEntity.IsAuthority() && lootTable.TryForRandomItem(gameEntity, out var gameEntity2))
		{
			Transform transform = lootSpawnMarker;
			if (transform == null)
			{
				transform = agent.transform;
			}
			Vector3 vector = transform.position;
			if (transform == null)
			{
				vector.y += 0.33f;
			}
			if (spawnOnGround && Physics.Raycast(new Ray(vector + Vector3.up * 0.5f, -Vector3.up), out var hitInfo, 5f, groundLayerMask.value, QueryTriggerInteraction.Ignore))
			{
				vector = hitInfo.point;
			}
			gameEntity.manager.RequestCreateItem(gameEntity2.gameObject.name.GetStaticHash(), vector, transform.rotation, 0L);
		}
		GREnemy component = gameEntity.GetComponent<GREnemy>();
		if (component != null && component.damageFlash != null)
		{
			component.damageFlash.Play();
		}
	}

	public void DestroySelf()
	{
		Debug.Log("DESTROY SELF");
		ReportDeathStat();
		if (agent.entity.IsAuthority())
		{
			agent.entity.manager.RequestDestroyItem(agent.entity.id);
		}
	}

	public void ReportDeathStat()
	{
		if (!reported)
		{
			reported = true;
			GameEntity gameEntity = agent.entity;
			GRPlayer gRPlayer = GRPlayer.Get(instigatingActorNumber);
			if (gRPlayer != null)
			{
				gRPlayer.IncrementSynchronizedSessionStat(GRPlayer.SynchronizedSessionStat.Kills, 1f);
			}
			GhostReactor.instance.shiftManager.shiftStats.IncrementEnemyKills(gameEntity.GetEnemyType());
		}
	}

	public override bool IsDone()
	{
		return false;
	}

	protected override void OnUpdateShared(float dt)
	{
		if (startTime >= 0.0)
		{
			if (doKnockback)
			{
				staggerMovement.Update(dt);
			}
			double num = Time.timeAsDouble - startTime;
			if (!isDead && num > (double)totalDeathDelay)
			{
				isDead = true;
				Die();
			}
			else if (isDead && num > (double)(totalDeathDelay + destroyDelay))
			{
				GhostReactorManager.Get(entity).OnAbilityDie(entity, delayRespawn);
				DestroySelf();
				startTime = -1.0;
			}
			events.TryPlay((float)num, audioSource);
		}
	}

	public static void Hide(List<Renderer> renderers, bool hide)
	{
		if (renderers == null)
		{
			return;
		}
		for (int i = 0; i < renderers.Count; i++)
		{
			if (renderers[i] != null)
			{
				renderers[i].enabled = !hide;
			}
		}
	}

	public static void Disable(List<Collider> colliders, bool disable)
	{
		if (colliders == null)
		{
			return;
		}
		for (int i = 0; i < colliders.Count; i++)
		{
			if (colliders[i] != null)
			{
				colliders[i].enabled = !disable;
			}
		}
	}
}

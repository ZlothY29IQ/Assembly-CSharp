using System;
using System.Collections.Generic;
using GorillaTagScripts.GhostReactor;
using UnityEngine;

[Serializable]
public class GRAbilityDie : GRAbilityBase
{
	public float delayDeath;

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

	public Transform lootSpawnMarker;

	public List<AnimationData> animData;

	private int instigatingActorNumber;

	private bool isDead;

	public GRAbilityInterpolatedMovement staggerMovement;

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

	public override void Start()
	{
		base.Start();
		if (animData.Count > 0)
		{
			int index = UnityEngine.Random.Range(0, animData.Count);
			delayDeath = animData[index].duration;
			staggerMovement.InitFromVelocityAndDuration(staggerMovement.velocity, delayDeath);
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
	}

	public override void Stop()
	{
		staggerMovement.Stop();
		agent.SetIsPathing(isPathing: true, ignoreRigiBody: true);
		agent.SetDisableNetworkSync(disable: false);
		Hide(hideWhenDead, hide: false);
		Disable(disableCollidersWhenDead, disable: false);
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
		staggerMovement.InitFromVelocityAndDuration(vel, delayDeath);
	}

	public void SetInstigatingPlayerIndex(int actorNumber)
	{
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
			Vector3 position = transform.position;
			if (transform == null)
			{
				position.y += 0.33f;
			}
			gameEntity.manager.RequestCreateItem(gameEntity2.gameObject.name.GetStaticHash(), position, transform.rotation, 0L);
		}
	}

	public void DestroySelf()
	{
		GameEntity gameEntity = agent.entity;
		GRPlayer gRPlayer = GRPlayer.Get(instigatingActorNumber);
		if (gRPlayer != null)
		{
			gRPlayer.IncrementSynchronizedSessionStat(GRPlayer.SynchronizedSessionStat.Kills, 1f);
		}
		GREnemyType? enemyType = gameEntity.GetEnemyType();
		if (enemyType.HasValue)
		{
			GREnemyType valueOrDefault = enemyType.GetValueOrDefault();
			GhostReactor.instance.shiftManager.shiftStats.IncrementEnemyKills(valueOrDefault);
		}
		if (gameEntity.IsAuthority())
		{
			gameEntity.manager.RequestDestroyItem(gameEntity.id);
		}
	}

	public override bool IsDone()
	{
		return false;
	}

	protected override void UpdateShared(float dt)
	{
		if (startTime >= 0.0)
		{
			if (doKnockback)
			{
				staggerMovement.Update(dt);
			}
			double num = Time.timeAsDouble - startTime;
			if (!isDead && num > (double)delayDeath)
			{
				isDead = true;
				Die();
			}
			else if (isDead && num > (double)(delayDeath + destroyDelay))
			{
				GhostReactorManager.Get(entity).OnAbilityDie(entity);
				DestroySelf();
				startTime = -1.0;
			}
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

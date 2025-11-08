using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[Serializable]
public class GRAbilitySummon : GRAbilityBase
{
	private enum State
	{
		Charge,
		Spawn,
		Done
	}

	private int lastAnimIndex = -1;

	public GameEntity entityPrefabToSpawn;

	public List<AnimationData> animData;

	private float animSpeed = 1f;

	public float chargeTime = 3f;

	public float duration = 3f;

	public float desiredSpawnDistance = 3f;

	public float minSpawnDistance = 1f;

	public float spawnHeight = 1f;

	public float summonConeAngle = 120f;

	private bool spawned;

	public AudioClip summonSpawnAudioClip;

	public GameObject fxStartSummon;

	public GameObject fxOnSpawn;

	public AbilitySound summonSound;

	private int spawnedCount;

	public Transform lookAtTarget;

	private State state;

	public override void Setup(GameAgent agent, Animation anim, AudioSource audioSource, Transform root, Transform head, GRSenseLineOfSight lineOfSight)
	{
		base.Setup(agent, anim, audioSource, root, head, lineOfSight);
	}

	public override void Start()
	{
		base.Start();
		lastAnimIndex = AbilityHelperFunctions.RandomRangeUnique(0, animData.Count, lastAnimIndex);
		duration = animData[lastAnimIndex].duration;
		chargeTime = animData[lastAnimIndex].eventTime;
		PlayAnim(animData[lastAnimIndex].animName, 0.1f, animSpeed);
		state = State.Charge;
		summonSound.Play(audioSource);
		spawnedCount = 0;
		agent.SetIsPathing(isPathing: false, ignoreRigiBody: true);
		agent.navAgent.speed = 1f;
		if (fxStartSummon != null)
		{
			fxStartSummon.SetActive(value: false);
			fxStartSummon.SetActive(value: true);
		}
	}

	public override void Stop()
	{
		lookAtTarget = null;
		agent.SetIsPathing(isPathing: true, ignoreRigiBody: true);
	}

	public void SetLookAtTarget(Transform transform)
	{
		lookAtTarget = transform;
	}

	public override void Think(float dt)
	{
		UpdateState(dt);
	}

	protected override void UpdateShared(float dt)
	{
		if (lookAtTarget != null)
		{
			GameAgent.UpdateFacingTarget(root, agent.navAgent, lookAtTarget, 360f);
		}
	}

	private void UpdateState(float dt)
	{
		double num = Time.timeAsDouble - startTime;
		switch (state)
		{
		case State.Charge:
			if (num > (double)chargeTime)
			{
				SetState(State.Spawn);
			}
			break;
		case State.Spawn:
			if (!spawned)
			{
				spawned = DoSpawn();
			}
			if (spawned && num > (double)duration)
			{
				SetState(State.Done);
				spawned = false;
			}
			break;
		case State.Done:
			break;
		}
	}

	private void SetState(State newState)
	{
		_ = state;
		state = newState;
		switch (newState)
		{
		}
	}

	private Vector3? GetSpawnLocation()
	{
		Vector3 position = root.position;
		float num = UnityEngine.Random.Range(0f, summonConeAngle);
		int num2 = 0;
		while (num2 < 5)
		{
			Vector3 vector = Quaternion.Euler(0f, num, 0f) * root.forward;
			Vector3 vector2 = position + vector * desiredSpawnDistance;
			if (NavMesh.Raycast(position, vector2, out var hit, walkableArea))
			{
				if (hit.distance < minSpawnDistance)
				{
					num += 15f;
					num2++;
					continue;
				}
				vector2 = hit.position + Vector3.up * spawnHeight;
			}
			return vector2;
		}
		return null;
	}

	private bool DoSpawn()
	{
		Vector3? spawnLocation = GetSpawnLocation();
		if (spawnLocation.HasValue)
		{
			if (entity.IsAuthority())
			{
				Quaternion identity = Quaternion.identity;
				GameEntityManager gameEntityManager = GhostReactorManager.Get(entity).gameEntityManager;
				Debug.Log($"summon ability spawning for summoner id: {entity.GetNetId()} name: {entity.ToString()}", entity);
				gameEntityManager.RequestCreateItem(entityPrefabToSpawn.name.GetStaticHash(), spawnLocation.Value, identity, entity.GetNetId());
				spawnedCount++;
			}
			if (audioSource != null)
			{
				audioSource.PlayOneShot(summonSpawnAudioClip);
			}
			if (fxOnSpawn != null)
			{
				fxOnSpawn.SetActive(value: false);
				fxOnSpawn.SetActive(value: true);
			}
			return true;
		}
		return false;
	}

	public override bool IsDone()
	{
		return state == State.Done;
	}
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[Serializable]
public class GRAbilitySummon : GRAbilityBase
{
	[Serializable]
	public class SummonMarker
	{
		public Transform transform;
	}

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

	public float coolDown;

	public float range;

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

	public List<SummonMarker> summonMarkers;

	private State state;

	public override void Setup(GameAgent agent, Animation anim, AudioSource audioSource, Transform root, Transform head, GRSenseLineOfSight lineOfSight)
	{
		base.Setup(agent, anim, audioSource, root, head, lineOfSight);
	}

	protected override void OnStart()
	{
		lastAnimIndex = AbilityHelperFunctions.RandomRangeUnique(0, animData.Count, lastAnimIndex);
		duration = animData[lastAnimIndex].duration;
		chargeTime = animData[lastAnimIndex].eventTime;
		PlayAnim(animData[lastAnimIndex].animName, 0.1f, animSpeed);
		state = State.Charge;
		summonSound.Play(audioSource);
		spawnedCount = 0;
		agent.SetStopped(stopMovement: true);
		agent.SetSpeed(1f);
		if (fxStartSummon != null)
		{
			fxStartSummon.SetActive(value: false);
			fxStartSummon.SetActive(value: true);
		}
	}

	protected override void OnStop()
	{
		lookAtTarget = null;
		agent.SetStopped(stopMovement: false);
	}

	public void SetLookAtTarget(Transform transform)
	{
		lookAtTarget = transform;
	}

	protected override void OnThink(float dt)
	{
		UpdateState(dt);
	}

	protected override void OnUpdateShared(float dt)
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
		if (summonMarkers != null && summonMarkers.Count > 0)
		{
			int index = UnityEngine.Random.Range(0, summonMarkers.Count);
			if (summonMarkers[index] != null)
			{
				return summonMarkers[index].transform.position;
			}
		}
		Vector3 position = root.position;
		float num = UnityEngine.Random.Range((0f - summonConeAngle) / 2f, summonConeAngle / 2f);
		for (int i = 0; i < 5; i++)
		{
			Vector3 vector = Quaternion.Euler(0f, num, 0f) * root.forward;
			Vector3 vector2 = position + vector * desiredSpawnDistance;
			if (NavMesh.Raycast(position, vector2, out var hit, walkableArea))
			{
				if (hit.distance < minSpawnDistance)
				{
					num += 15f;
					if (num > summonConeAngle / 2f)
					{
						summonConeAngle = (0f - summonConeAngle) / 2f;
					}
					continue;
				}
				vector2 = hit.position + Vector3.up * spawnHeight;
			}
			if (!Physics.Raycast(vector2, Vector3.down, out var hitInfo) || (object)hitInfo.collider.gameObject.GetComponent<GRHazardousMaterial>() == null)
			{
				return vector2;
			}
		}
		return null;
	}

	public bool ForceSpawn()
	{
		return DoSpawn();
	}

	private bool DoSpawn()
	{
		Vector3? spawnLocation = GetSpawnLocation();
		if (spawnLocation.HasValue)
		{
			if (entity.IsAuthority())
			{
				Quaternion identity = Quaternion.identity;
				GhostReactorManager.Get(entity).gameEntityManager.RequestCreateItem(entityPrefabToSpawn.name.GetStaticHash(), spawnLocation.Value, identity, 0L, entity.id);
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

	public override bool IsCoolDownOver()
	{
		return IsCoolDownOver(coolDown);
	}

	public override float GetRange()
	{
		return range;
	}
}

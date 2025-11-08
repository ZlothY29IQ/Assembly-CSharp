using System;
using CjLib;
using Photon.Pun;
using Unity.Mathematics;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.AI;

[Serializable]
public class GRAbilityPatrol : GRAbilityBase
{
	private NavMeshAgent navMeshAgent;

	public GRAbilityMoveToTarget moveAbility;

	private GRPatrolPath patrolPath;

	public double lastStateChange;

	public float ambientSoundVolume = 0.5f;

	public double ambientSoundDelayMin = 5.0;

	public double ambientSoundDelayMax = 10.0;

	public AudioClip[] ambientPatrolSounds;

	private double lastPartrolAmbientSoundTime;

	private double nextPatrolGroanTime;

	private Unity.Mathematics.Random patrolGroanSoundDelayRandom;

	private Unity.Mathematics.Random patrolGroanSoundRandom;

	[ReadOnly]
	public int nextPatrolNode;

	public bool HasValidPatrolPath()
	{
		if (patrolPath != null)
		{
			return patrolPath.patrolNodes.Count > 1;
		}
		return false;
	}

	public override void Setup(GameAgent agent, Animation anim, AudioSource audioSource, Transform root, Transform head, GRSenseLineOfSight lineOfSight)
	{
		base.Setup(agent, anim, audioSource, root, head, lineOfSight);
		moveAbility.Setup(agent, anim, audioSource, root, head, lineOfSight);
		if ((bool)attributes && moveAbility.moveSpeed == 0f)
		{
			moveAbility.moveSpeed = attributes.CalculateFinalFloatValueForAttribute(GRAttributeType.PatrolSpeed);
		}
		navMeshAgent = agent.GetComponent<NavMeshAgent>();
		InitializeRandoms();
		nextPatrolNode = 0;
	}

	private void InitializeRandoms()
	{
		patrolGroanSoundDelayRandom = new Unity.Mathematics.Random((uint)entity.GetNetId());
		patrolGroanSoundRandom = new Unity.Mathematics.Random((uint)entity.GetNetId());
	}

	public override void Start()
	{
		base.Start();
		moveAbility.Start();
		agent.SetIsPathing(isPathing: true, ignoreRigiBody: true);
		if (patrolPath != null)
		{
			moveAbility.SetTarget(patrolPath.patrolNodes[nextPatrolNode]);
		}
		else
		{
			Debug.LogError("Starting patrol ability with no patrol path");
		}
		CalculateNextPatrolGroan();
	}

	public override void Stop()
	{
		moveAbility.Stop();
	}

	public override bool IsDone()
	{
		return false;
	}

	public void SetPatrolPath(GRPatrolPath patrolPath)
	{
		this.patrolPath = patrolPath;
	}

	public GRPatrolPath GetPatrolPath()
	{
		return patrolPath;
	}

	public void SetNextPatrolNode(int nextPatrolNode)
	{
		this.nextPatrolNode = nextPatrolNode;
	}

	public void CalculateNextPatrolGroan()
	{
		nextPatrolGroanTime = patrolGroanSoundDelayRandom.NextDouble(ambientSoundDelayMin, ambientSoundDelayMax) + PhotonNetwork.Time;
	}

	private void PlayPatrolGroan()
	{
		audioSource.clip = ambientPatrolSounds[patrolGroanSoundRandom.NextInt(ambientPatrolSounds.Length - 1)];
		audioSource.volume = ambientSoundVolume;
		audioSource.Play();
		CalculateNextPatrolGroan();
	}

	public override void Update(float dt)
	{
		moveAbility.Update(dt);
		if (GhostReactorManager.entityDebugEnabled)
		{
			DebugUtil.DrawLine(root.position, moveAbility.GetTargetPos(), Color.green);
		}
		if (moveAbility.IsDone())
		{
			nextPatrolNode = (nextPatrolNode + 1) % patrolPath.patrolNodes.Count;
			moveAbility.SetTarget(patrolPath.patrolNodes[nextPatrolNode]);
		}
		if (PhotonNetwork.Time >= nextPatrolGroanTime)
		{
			PlayPatrolGroan();
		}
	}

	public override void UpdateRemote(float dt)
	{
		moveAbility.SetTarget(null);
		moveAbility.SetTargetPos(agent.navAgent.destination);
		moveAbility.Update(dt);
		if (GhostReactorManager.entityDebugEnabled)
		{
			DebugUtil.DrawLine(root.position, moveAbility.GetTargetPos(), Color.green);
		}
		if (PhotonNetwork.Time >= nextPatrolGroanTime)
		{
			PlayPatrolGroan();
		}
	}
}

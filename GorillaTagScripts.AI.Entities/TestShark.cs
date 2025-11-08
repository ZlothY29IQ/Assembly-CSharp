using System;
using GorillaTagScripts.AI.States;
using Photon.Pun;
using UnityEngine;

namespace GorillaTagScripts.AI.Entities;

public class TestShark : AIEntity
{
	public float nextTimeToChasePlayer = 30f;

	private float chasingTimer;

	private bool shouldChase;

	private StateMachine _stateMachine;

	private CircularPatrol_State circularPatrol;

	private Patrol_State patrol;

	private Chase_State chase;

	private new void Awake()
	{
		base.Awake();
		chasingTimer = 0f;
		_stateMachine = new StateMachine();
		circularPatrol = new CircularPatrol_State(this);
		patrol = new Patrol_State(this);
		chase = new Chase_State(this);
		_stateMachine.AddTransition(patrol, chase, ShouldChase());
		_stateMachine.AddTransition(chase, patrol, ShouldPatrol());
		_stateMachine.SetState(patrol);
		Func<bool> ShouldChase()
		{
			return () => shouldChase && PhotonNetwork.InRoom;
		}
		Func<bool> ShouldPatrol()
		{
			return () => chase.chaseOver;
		}
	}

	private void Update()
	{
		_stateMachine.Tick();
		shouldChase = false;
		chasingTimer += Time.deltaTime;
		if (chasingTimer >= nextTimeToChasePlayer)
		{
			ChooseClosestTarget();
			if (followTarget != null)
			{
				chase.FollowTarget = followTarget;
				shouldChase = true;
			}
			chasingTimer = 0f;
		}
	}
}

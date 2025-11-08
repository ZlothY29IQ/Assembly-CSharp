using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace GorillaTagScripts.AI;

public class AIEntity : MonoBehaviour
{
	public GameObject waypointsContainer;

	public Transform circleCenter;

	public float circleRadius;

	public float angularSpeed;

	public float patrolSpeed;

	public float fleeSpeed;

	public NavMeshAgent navMeshAgent;

	public Animator animator;

	public float fleeRang;

	public float fleeSpeedMult;

	public float minChaseRange;

	public float attackDistance;

	public float navMeshSampleRange = 5f;

	internal readonly List<Transform> waypoints = new List<Transform>();

	internal float defaultSpeed;

	public Transform followTarget;

	public NetPlayer targetPlayer;

	public bool targetIsOnNavMesh;

	protected void Awake()
	{
		navMeshAgent = base.gameObject.GetComponent<NavMeshAgent>();
		animator = base.gameObject.GetComponent<Animator>();
		if (waypointsContainer != null)
		{
			Transform[] componentsInChildren = waypointsContainer.GetComponentsInChildren<Transform>();
			foreach (Transform item in componentsInChildren)
			{
				waypoints.Add(item);
			}
		}
	}

	protected void ChooseRandomTarget()
	{
		int num = -1;
		int randomTarget = Random.Range(0, GorillaParent.instance.vrrigs.Count);
		num = GorillaParent.instance.vrrigs.FindIndex((VRRig x) => x.creator != null && x.creator == GorillaParent.instance.vrrigs[randomTarget].creator);
		if (num == -1)
		{
			num = Random.Range(0, GorillaParent.instance.vrrigs.Count);
		}
		if (num < GorillaParent.instance.vrrigs.Count)
		{
			targetPlayer = GorillaParent.instance.vrrigs[num].creator;
			followTarget = GorillaParent.instance.vrrigs[num].head.rigTarget;
			targetIsOnNavMesh = NavMesh.SamplePosition(followTarget.position, out var _, navMeshSampleRange, 1);
		}
		else
		{
			targetPlayer = null;
			followTarget = null;
		}
	}

	protected void ChooseClosestTarget()
	{
		VRRig vRRig = null;
		float num = float.MaxValue;
		foreach (VRRig vrrig in GorillaParent.instance.vrrigs)
		{
			if (vrrig.head != null && !(vrrig.head.rigTarget == null))
			{
				float sqrMagnitude = (base.transform.position - vrrig.head.rigTarget.transform.position).sqrMagnitude;
				if (sqrMagnitude < minChaseRange * minChaseRange && sqrMagnitude < num)
				{
					num = sqrMagnitude;
					vRRig = vrrig;
				}
			}
		}
		if (vRRig != null)
		{
			targetPlayer = vRRig.creator;
			followTarget = vRRig.head.rigTarget;
			targetIsOnNavMesh = NavMesh.SamplePosition(followTarget.position, out var _, navMeshSampleRange, 1);
		}
		else
		{
			targetPlayer = null;
			followTarget = null;
		}
	}
}

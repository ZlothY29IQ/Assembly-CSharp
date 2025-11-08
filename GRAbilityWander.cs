using System;
using UnityEngine;
using UnityEngine.AI;

[Serializable]
public class GRAbilityWander : GRAbilityBase
{
	public GRAbilityMoveToTarget moveAbility;

	private static Quaternion[] rotations = new Quaternion[8]
	{
		Quaternion.Euler(0f, 0f, 0f),
		Quaternion.Euler(0f, 45f, 0f),
		Quaternion.Euler(0f, -45f, 0f),
		Quaternion.Euler(0f, 90f, 0f),
		Quaternion.Euler(0f, -90f, 0f),
		Quaternion.Euler(0f, 135f, 0f),
		Quaternion.Euler(0f, -135f, 0f),
		Quaternion.Euler(0f, 180f, 0f)
	};

	private static float[] rotationWeight = new float[8] { 1f, 0.75f, 0.75f, 0.5f, 0.5f, 0.2f, 0.2f, 0.2f };

	public override void Setup(GameAgent agent, Animation anim, AudioSource audioSource, Transform root, Transform head, GRSenseLineOfSight lineOfSight)
	{
		base.Setup(agent, anim, audioSource, root, head, lineOfSight);
		moveAbility.Setup(agent, anim, audioSource, root, head, lineOfSight);
	}

	public override void Start()
	{
		base.Start();
		moveAbility.Start();
		Vector3 targetPos = PickRandomDestination();
		moveAbility.SetTargetPos(targetPos);
	}

	public override void Stop()
	{
		moveAbility.Stop();
	}

	public override bool IsDone()
	{
		return false;
	}

	public override void Think(float dt)
	{
		if (moveAbility.IsDone())
		{
			Vector3 targetPos = PickRandomDestination();
			moveAbility.SetTargetPos(targetPos);
		}
	}

	private Vector3 PickRandomDestination()
	{
		Vector3 position = agent.transform.position;
		if (NavMesh.SamplePosition(position, out var hit, 1f, walkableArea))
		{
			Vector3 position2 = hit.position;
			Vector3 forward = agent.transform.forward;
			float num = 0f;
			Vector3 sourcePosition = position2;
			for (int i = 0; i < rotations.Length; i++)
			{
				Vector3 vector = rotations[i] * forward;
				float num2 = 8f;
				if (NavMesh.Raycast(position2, position2 + vector * num2, out hit, walkableArea))
				{
					num2 = hit.distance * 0.95f;
				}
				float num3 = num2 * rotationWeight[i];
				if (num3 > num)
				{
					num = num3;
					sourcePosition = position2 + vector * num2;
				}
			}
			if (NavMesh.SamplePosition(sourcePosition, out hit, 1f, walkableArea))
			{
				position = hit.position;
			}
		}
		return position;
	}

	protected override void UpdateShared(float dt)
	{
		moveAbility.Update(dt);
	}
}

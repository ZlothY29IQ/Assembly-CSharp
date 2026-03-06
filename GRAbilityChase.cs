using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class GRAbilityChase : GRAbilityBase
{
	public float chaseSpeed;

	public string animName;

	public float animSpeed;

	public float maxTurnSpeed;

	public float loseVisibilityDelay;

	public float giveUpDelay;

	public AbilitySound movementSound;

	private NetPlayer targetPlayer;

	private double lastSeenTargetTime;

	private Vector3 lastSeenTargetPosition;

	private static List<Vector3> targetOffsets;

	public override void Setup(GameAgent agent, Animation anim, AudioSource audioSource, Transform root, Transform head, GRSenseLineOfSight lineOfSight)
	{
		base.Setup(agent, anim, audioSource, root, head, lineOfSight);
		targetPlayer = null;
		lastSeenTargetTime = 0.0;
		lastSeenTargetPosition = Vector3.zero;
		if (targetOffsets == null)
		{
			int num = 8;
			targetOffsets = new List<Vector3>(num);
			float x = 1f;
			for (int i = 0; i < num; i++)
			{
				Vector3 vector = new Vector3(x, 0f, 0f);
				vector = Quaternion.Euler(0f, (float)i / (float)num * 360f, 0f) * vector;
				targetOffsets.Add(vector);
			}
			System.Random random = new System.Random();
			List<Vector3> collection = targetOffsets.OrderBy((Vector3 vector2) => random.Next()).ToList();
			targetOffsets.Clear();
			targetOffsets.AddRange(collection);
		}
		if ((bool)attributes && chaseSpeed == 0f)
		{
			chaseSpeed = attributes.CalculateFinalFloatValueForAttribute(GRAttributeType.ChaseSpeed);
		}
	}

	protected override void OnStart()
	{
		PlayAnim(animName, 0.1f, animSpeed);
		agent.SetSpeed(chaseSpeed);
		lastSeenTargetTime = Time.timeAsDouble;
		movementSound.Play(null);
		agent.ClearLastRequestedDestination();
	}

	protected override void OnStop()
	{
	}

	public override bool IsDone()
	{
		if (targetPlayer != null)
		{
			return Time.timeAsDouble - lastSeenTargetTime >= (double)giveUpDelay;
		}
		return true;
	}

	protected override void OnThink(float dt)
	{
		GRPlayer gRPlayer = GRPlayer.Get(targetPlayer);
		if (gRPlayer != null && gRPlayer.State == GRPlayer.GRPlayerState.Alive)
		{
			Vector3 position = gRPlayer.transform.position;
			position += GetMoveTargetOffset(position, entity);
			if (lineOfSight.HasLineOfSight(head.position, position))
			{
				lastSeenTargetTime = Time.timeAsDouble;
			}
			if (!((float)(Time.timeAsDouble - lastSeenTargetTime) > loseVisibilityDelay))
			{
				lastSeenTargetPosition = position;
			}
		}
		agent.RequestDestination(lastSeenTargetPosition);
	}

	protected override void OnUpdateShared(float dt)
	{
		GameAgent.UpdateFacing(root, agent.navAgent, targetPlayer, maxTurnSpeed);
	}

	public void SetTargetPlayer(NetPlayer targetPlayer)
	{
		this.targetPlayer = targetPlayer;
	}

	public static Vector3 GetMoveTargetOffset(Vector3 targetPos, GameEntity attackingEntity)
	{
		int index = attackingEntity.id.index % targetOffsets.Count;
		return targetOffsets[index];
	}
}

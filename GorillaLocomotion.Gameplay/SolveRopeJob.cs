using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace GorillaLocomotion.Gameplay;

[BurstCompile]
public struct SolveRopeJob : IJob
{
	[ReadOnly]
	public float fixedDeltaTime;

	[WriteOnly]
	public NativeArray<BurstRopeNode> nodes;

	[ReadOnly]
	public Vector3 gravity;

	[ReadOnly]
	public Vector3 rootPos;

	[ReadOnly]
	public float nodeDistance;

	public void Execute()
	{
		Simulate();
		for (int i = 0; i < 20; i++)
		{
			ApplyConstraint();
		}
	}

	private void Simulate()
	{
		for (int i = 0; i < nodes.Length; i++)
		{
			BurstRopeNode value = nodes[i];
			Vector3 vector = value.curPos - value.lastPos;
			value.lastPos = value.curPos;
			Vector3 curPos = value.curPos + vector;
			curPos += gravity * fixedDeltaTime;
			value.curPos = curPos;
			nodes[i] = value;
		}
	}

	private void ApplyConstraint()
	{
		BurstRopeNode value = nodes[0];
		value.curPos = rootPos;
		nodes[0] = value;
		for (int i = 0; i < nodes.Length - 1; i++)
		{
			BurstRopeNode value2 = nodes[i];
			BurstRopeNode value3 = nodes[i + 1];
			float magnitude = (value2.curPos - value3.curPos).magnitude;
			float num = Mathf.Abs(magnitude - nodeDistance);
			Vector3 vector = Vector3.zero;
			if (magnitude > nodeDistance)
			{
				vector = (value2.curPos - value3.curPos).normalized;
			}
			else if (magnitude < nodeDistance)
			{
				vector = (value3.curPos - value2.curPos).normalized;
			}
			Vector3 vector2 = vector * num;
			value2.curPos -= vector2 * 0.5f;
			value3.curPos += vector2 * 0.5f;
			nodes[i] = value2;
			nodes[i + 1] = value3;
		}
	}
}

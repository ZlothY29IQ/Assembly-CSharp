using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace GorillaLocomotion.Gameplay;

public class VectorizedCustomRopeSimulation : MonoBehaviour
{
	public static VectorizedCustomRopeSimulation instance;

	public const int MAX_NODE_COUNT = 32;

	public const float MAX_ROPE_SPEED = 15f;

	private List<Transform> nodes = new List<Transform>();

	[SerializeField]
	private float nodeDistance = 1f;

	[SerializeField]
	private int applyConstraintIterations = 20;

	[SerializeField]
	private int finalPassIterations = 1;

	[SerializeField]
	private float gravity = -0.15f;

	private VectorizedBurstRopeData burstData;

	private float lastDelta = 0.02f;

	private List<GorillaRopeSwing> ropes = new List<GorillaRopeSwing>();

	private static List<GorillaRopeSwing> registerQueue = new List<GorillaRopeSwing>();

	private static List<GorillaRopeSwing> deregisterQueue = new List<GorillaRopeSwing>();

	private void Awake()
	{
		instance = this;
	}

	public static void Register(GorillaRopeSwing rope)
	{
		registerQueue.Add(rope);
	}

	public static void Unregister(GorillaRopeSwing rope)
	{
		deregisterQueue.Add(rope);
	}

	private void RegenerateData()
	{
		Dispose();
		foreach (GorillaRopeSwing item in registerQueue)
		{
			ropes.Add(item);
		}
		foreach (GorillaRopeSwing item2 in deregisterQueue)
		{
			ropes.Remove(item2);
		}
		registerQueue.Clear();
		deregisterQueue.Clear();
		int i;
		for (i = ropes.Count; i % 4 != 0; i++)
		{
		}
		int num = i * 32 / 4;
		burstData = new VectorizedBurstRopeData
		{
			posX = new NativeArray<float4>(num, Allocator.Persistent),
			posY = new NativeArray<float4>(num, Allocator.Persistent),
			posZ = new NativeArray<float4>(num, Allocator.Persistent),
			validNodes = new NativeArray<int4>(num, Allocator.Persistent),
			lastPosX = new NativeArray<float4>(num, Allocator.Persistent),
			lastPosY = new NativeArray<float4>(num, Allocator.Persistent),
			lastPosZ = new NativeArray<float4>(num, Allocator.Persistent),
			ropeRoots = new NativeArray<float3>(i, Allocator.Persistent),
			nodeMass = new NativeArray<float4>(num, Allocator.Persistent)
		};
		for (int j = 0; j < ropes.Count; j += 4)
		{
			for (int k = 0; k < 4 && ropes.Count > j + k; k++)
			{
				ropes[j + k].ropeDataStartIndex = 32 * j / 4;
				ropes[j + k].ropeDataIndexOffset = k;
			}
		}
		int num2 = 0;
		for (int l = 0; l < num; l++)
		{
			float4 value = burstData.posX[l];
			float4 value2 = burstData.posY[l];
			float4 value3 = burstData.posZ[l];
			int4 value4 = burstData.validNodes[l];
			for (int m = 0; m < 4; m++)
			{
				int num3 = num2 * 4 + m;
				int num4 = l - num2 * 32;
				if (ropes.Count > num3 && ropes[num3].nodes.Length > num4)
				{
					Vector3 localPosition = ropes[num3].nodes[num4].localPosition;
					value[m] = localPosition.x;
					value2[m] = localPosition.y;
					value3[m] = localPosition.z;
					value4[m] = 1;
				}
				else
				{
					value[m] = 0f;
					value2[m] = 0f;
					value3[m] = 0f;
					value4[m] = 0;
				}
			}
			if (l > 0 && (l + 1) % 32 == 0)
			{
				num2++;
			}
			burstData.posX[l] = value;
			burstData.posY[l] = value2;
			burstData.posZ[l] = value3;
			burstData.lastPosX[l] = value;
			burstData.lastPosY[l] = value2;
			burstData.lastPosZ[l] = value3;
			burstData.validNodes[l] = value4;
			burstData.nodeMass[l] = math.float4(1f, 1f, 1f, 1f);
		}
		for (int n = 0; n < ropes.Count; n++)
		{
			burstData.ropeRoots[n] = float3.zero;
		}
	}

	private void Dispose()
	{
		if (burstData.posX.IsCreated)
		{
			burstData.posX.Dispose();
			burstData.posY.Dispose();
			burstData.posZ.Dispose();
			burstData.validNodes.Dispose();
			burstData.lastPosX.Dispose();
			burstData.lastPosY.Dispose();
			burstData.lastPosZ.Dispose();
			burstData.ropeRoots.Dispose();
			burstData.nodeMass.Dispose();
		}
	}

	private void OnDestroy()
	{
		Dispose();
	}

	public void SetRopePos(GorillaRopeSwing ropeTarget, Vector3[] positions, bool setCurPos, bool setLastPos, int onlySetIndex = -1)
	{
		if (!ropes.Contains(ropeTarget))
		{
			return;
		}
		int ropeDataIndexOffset = ropeTarget.ropeDataIndexOffset;
		for (int i = 0; i < positions.Length; i++)
		{
			if (onlySetIndex < 0 || i == onlySetIndex)
			{
				int index = ropeTarget.ropeDataStartIndex + i;
				if (setCurPos)
				{
					float4 value = burstData.posX[index];
					float4 value2 = burstData.posY[index];
					float4 value3 = burstData.posZ[index];
					value[ropeDataIndexOffset] = positions[i].x;
					value2[ropeDataIndexOffset] = positions[i].y;
					value3[ropeDataIndexOffset] = positions[i].z;
					burstData.posX[index] = value;
					burstData.posY[index] = value2;
					burstData.posZ[index] = value3;
				}
				if (setLastPos)
				{
					float4 value4 = burstData.lastPosX[index];
					float4 value5 = burstData.lastPosY[index];
					float4 value6 = burstData.lastPosZ[index];
					value4[ropeDataIndexOffset] = positions[i].x;
					value5[ropeDataIndexOffset] = positions[i].y;
					value6[ropeDataIndexOffset] = positions[i].z;
					burstData.lastPosX[index] = value4;
					burstData.lastPosY[index] = value5;
					burstData.lastPosZ[index] = value6;
				}
			}
		}
	}

	public void SetVelocity(GorillaRopeSwing ropeTarget, Vector3 velocity, bool wholeRope, int boneIndex = 1)
	{
		List<Vector3> list = new List<Vector3>();
		float maxLength = math.min(velocity.magnitude, 15f);
		int ropeDataStartIndex = ropeTarget.ropeDataStartIndex;
		int ropeDataIndexOffset = ropeTarget.ropeDataIndexOffset;
		if (ropeTarget.SupportsMovingAtRuntime)
		{
			velocity = Quaternion.Inverse(ropeTarget.transform.rotation) * velocity;
		}
		for (int i = 0; i < ropeTarget.nodes.Length; i++)
		{
			Vector3 item = new Vector3(burstData.lastPosX[ropeDataStartIndex + i][ropeDataIndexOffset], burstData.lastPosY[ropeDataStartIndex + i][ropeDataIndexOffset], burstData.lastPosZ[ropeDataStartIndex + i][ropeDataIndexOffset]);
			if ((wholeRope || boneIndex == i) && boneIndex > 0)
			{
				Vector3 vector = velocity / boneIndex * i;
				vector = Vector3.ClampMagnitude(vector, maxLength);
				list.Add(item += vector * lastDelta);
			}
			else
			{
				list.Add(item);
			}
		}
		int onlySetIndex = -1;
		if (!wholeRope && boneIndex > 0)
		{
			onlySetIndex = boneIndex;
		}
		SetRopePos(ropeTarget, list.ToArray(), setCurPos: true, setLastPos: false, onlySetIndex);
	}

	public Vector3 GetNodeVelocity(GorillaRopeSwing ropeTarget, int nodeIndex)
	{
		int index = ropeTarget.ropeDataStartIndex + nodeIndex;
		int ropeDataIndexOffset = ropeTarget.ropeDataIndexOffset;
		Vector3 vector = new Vector3(burstData.posX[index][ropeDataIndexOffset], burstData.posY[index][ropeDataIndexOffset], burstData.posZ[index][ropeDataIndexOffset]);
		Vector3 vector2 = new Vector3(burstData.lastPosX[index][ropeDataIndexOffset], burstData.lastPosY[index][ropeDataIndexOffset], burstData.lastPosZ[index][ropeDataIndexOffset]);
		Vector3 vector3 = (vector - vector2) / lastDelta;
		if (ropeTarget.SupportsMovingAtRuntime)
		{
			vector3 = ropeTarget.transform.rotation * vector3;
		}
		return vector3;
	}

	public void SetMassForPlayers(GorillaRopeSwing ropeTarget, bool hasPlayers, int furthestBoneIndex = 0)
	{
		if (!ropes.Contains(ropeTarget))
		{
			return;
		}
		int ropeDataIndexOffset = ropeTarget.ropeDataIndexOffset;
		for (int i = 0; i < 32; i++)
		{
			int index = ropeTarget.ropeDataStartIndex + i;
			float4 value = burstData.nodeMass[index];
			if (hasPlayers && i == furthestBoneIndex + 1)
			{
				value[ropeDataIndexOffset] = 0.1f;
			}
			else
			{
				value[ropeDataIndexOffset] = 1f;
			}
			burstData.nodeMass[index] = value;
		}
	}

	private void Update()
	{
		if (registerQueue.Count > 0 || deregisterQueue.Count > 0)
		{
			RegenerateData();
		}
		if (ropes.Count <= 0)
		{
			return;
		}
		float deltaTime = math.min(Time.deltaTime, 0.05f);
		VectorizedSolveRopeJob jobData = new VectorizedSolveRopeJob
		{
			applyConstraintIterations = applyConstraintIterations,
			finalPassIterations = finalPassIterations,
			lastDeltaTime = lastDelta,
			deltaTime = deltaTime,
			gravity = gravity,
			data = burstData,
			nodeDistance = nodeDistance,
			ropeCount = ropes.Count
		};
		jobData.Schedule().Complete();
		for (int i = 0; i < ropes.Count; i++)
		{
			GorillaRopeSwing gorillaRopeSwing = ropes[i];
			if (gorillaRopeSwing.isIdle && gorillaRopeSwing.isFullyIdle)
			{
				continue;
			}
			int ropeDataIndexOffset = gorillaRopeSwing.ropeDataIndexOffset;
			for (int j = 0; j < gorillaRopeSwing.nodes.Length; j++)
			{
				int index = gorillaRopeSwing.ropeDataStartIndex + j;
				gorillaRopeSwing.nodes[j].localPosition = new Vector3(jobData.data.posX[index][ropeDataIndexOffset], jobData.data.posY[index][ropeDataIndexOffset], jobData.data.posZ[index][ropeDataIndexOffset]);
			}
			if (gorillaRopeSwing.SupportsMovingAtRuntime)
			{
				for (int k = 0; k < gorillaRopeSwing.nodes.Length - 1; k++)
				{
					Transform transform = gorillaRopeSwing.nodes[k];
					_ = gorillaRopeSwing.ropeDataStartIndex;
					transform.up = gorillaRopeSwing.transform.rotation * -(gorillaRopeSwing.nodes[k + 1].localPosition - transform.localPosition);
				}
			}
			else
			{
				for (int l = 0; l < gorillaRopeSwing.nodes.Length - 1; l++)
				{
					Transform transform2 = gorillaRopeSwing.nodes[l];
					_ = gorillaRopeSwing.ropeDataStartIndex;
					transform2.up = -(gorillaRopeSwing.nodes[l + 1].localPosition - transform2.localPosition);
				}
			}
		}
		lastDelta = deltaTime;
	}
}

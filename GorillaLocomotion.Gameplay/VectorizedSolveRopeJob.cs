using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace GorillaLocomotion.Gameplay;

[BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
public struct VectorizedSolveRopeJob : IJob
{
	[ReadOnly]
	public int applyConstraintIterations;

	[ReadOnly]
	public int finalPassIterations;

	[ReadOnly]
	public float deltaTime;

	[ReadOnly]
	public float lastDeltaTime;

	[ReadOnly]
	public int ropeCount;

	public VectorizedBurstRopeData data;

	[ReadOnly]
	public float gravity;

	[ReadOnly]
	public float nodeDistance;

	public void Execute()
	{
		Simulate();
		for (int i = 0; i < applyConstraintIterations; i++)
		{
			ApplyConstraint();
		}
		for (int j = 0; j < finalPassIterations; j++)
		{
			FinalPass();
		}
	}

	private void Simulate()
	{
		for (int i = 0; i < data.posX.Length; i++)
		{
			float4 @float = (data.posX[i] - data.lastPosX[i]) / lastDeltaTime;
			float4 float2 = (data.posY[i] - data.lastPosY[i]) / lastDeltaTime;
			float4 float3 = (data.posZ[i] - data.lastPosZ[i]) / lastDeltaTime;
			data.lastPosX[i] = data.posX[i];
			data.lastPosY[i] = data.posY[i];
			data.lastPosZ[i] = data.posZ[i];
			float4 float4 = data.lastPosX[i] + @float * deltaTime * 0.996f;
			float4 float5 = data.lastPosY[i] + float2 * deltaTime;
			float4 float6 = data.lastPosZ[i] + float3 * deltaTime * 0.996f;
			float5 += gravity * deltaTime;
			data.posX[i] = float4 * data.validNodes[i];
			data.posY[i] = float5 * data.validNodes[i];
			data.posZ[i] = float6 * data.validNodes[i];
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void dot4(ref float4 ax, ref float4 ay, ref float4 az, ref float4 bx, ref float4 by, ref float4 bz, ref float4 output)
	{
		output = ax * bx + ay * by + az * bz;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void length4(ref float4 xVals, ref float4 yVals, ref float4 zVals, ref float4 output)
	{
		float4 output2 = float4.zero;
		dot4(ref xVals, ref yVals, ref zVals, ref xVals, ref yVals, ref zVals, ref output2);
		output2 = math.abs(output2);
		output = math.sqrt(output2);
	}

	private void ConstrainRoots()
	{
		int num = 0;
		for (int i = 0; i < data.posX.Length; i += 32)
		{
			for (int j = 0; j < 4; j++)
			{
				float4 value = data.posX[i];
				float4 value2 = data.posY[i];
				float4 value3 = data.posZ[i];
				value[j] = data.ropeRoots[num].x;
				value2[j] = data.ropeRoots[num].y;
				value3[j] = data.ropeRoots[num].z;
				data.posX[i] = value;
				data.posY[i] = value2;
				data.posZ[i] = value3;
				num++;
			}
		}
	}

	private void ApplyConstraint()
	{
		ConstrainRoots();
		float4 @float = math.int4(-1, -1, -1, -1);
		for (int i = 0; i < ropeCount; i += 4)
		{
			for (int j = 0; j < 31; j++)
			{
				int num = i / 4 * 32 + j;
				float4 float2 = data.validNodes[num];
				float4 float3 = data.validNodes[num + 1];
				if (!(math.lengthsq(float3) < 0.1f))
				{
					float4 output = float4.zero;
					float4 xVals = data.posX[num] - data.posX[num + 1];
					float4 yVals = data.posY[num] - data.posY[num + 1];
					float4 zVals = data.posZ[num] - data.posZ[num + 1];
					length4(ref xVals, ref yVals, ref zVals, ref output);
					float4 float4 = math.abs(output - nodeDistance);
					float4 float5 = math.sign(output - nodeDistance);
					output += float2 - @float;
					output += 0.01f;
					float4 float6 = xVals / output;
					float4 float7 = yVals / output;
					float4 float8 = zVals / output;
					float4 float9 = float5 * float6 * float4;
					float4 float10 = float5 * float7 * float4;
					float4 float11 = float5 * float8 * float4;
					float4 float12 = data.nodeMass[num] / (data.nodeMass[num] + data.nodeMass[num + 1]);
					float4 float13 = data.nodeMass[num + 1] / (data.nodeMass[num] + data.nodeMass[num + 1]);
					data.posX[num] -= float9 * float3 * float12;
					data.posY[num] -= float10 * float3 * float12;
					data.posZ[num] -= float11 * float3 * float12;
					data.posX[num + 1] += float9 * float3 * float13;
					data.posY[num + 1] += float10 * float3 * float13;
					data.posZ[num + 1] += float11 * float3 * float13;
				}
			}
		}
	}

	private void FinalPass()
	{
		ConstrainRoots();
		float4 @float = math.int4(-1, -1, -1, -1);
		for (int i = 0; i < ropeCount; i += 4)
		{
			for (int j = 0; j < 31; j++)
			{
				int num = i / 4 * 32 + j;
				_ = (float4)data.validNodes[num];
				float4 float2 = data.validNodes[num + 1];
				float4 output = float4.zero;
				float4 xVals = data.posX[num] - data.posX[num + 1];
				float4 yVals = data.posY[num] - data.posY[num + 1];
				float4 zVals = data.posZ[num] - data.posZ[num + 1];
				length4(ref xVals, ref yVals, ref zVals, ref output);
				float4 float3 = math.abs(output - nodeDistance);
				float4 float4 = math.sign(output - nodeDistance);
				output += data.validNodes[num] - @float;
				output += 0.01f;
				float4 float5 = xVals / output;
				float4 float6 = yVals / output;
				float4 float7 = zVals / output;
				float4 float8 = float4 * float5 * float3;
				float4 float9 = float4 * float6 * float3;
				float4 float10 = float4 * float7 * float3;
				data.posX[num + 1] += float8 * float2;
				data.posY[num + 1] += float9 * float2;
				data.posZ[num + 1] += float10 * float2;
			}
		}
	}
}

using System.Collections.Generic;
using GorillaTagScripts;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

public class GorillaIKMgr : MonoBehaviour
{
	private struct IKConstantInput
	{
		public Quaternion initRotLower;

		public Quaternion initRotUpper;

		public Vector3 shoulderPosition;

		public Vector3 bodyPivotPos;

		public Quaternion bodyStartRot;

		public Quaternion shoulderRot;
	}

	private struct IKInput
	{
		public bool usingNewIK;

		public Vector3 targetPos;

		public Vector3 elbowDir;

		public Quaternion bodyRot;
	}

	private struct IKOutput(Quaternion upperArmLocalRot_, Quaternion lowerArmLocalRot_, Vector3 _handLocalPosition)
	{
		public Quaternion upperArmLocalRot = upperArmLocalRot_;

		public Quaternion lowerArmLocalRot = lowerArmLocalRot_;

		public Vector3 handLocalPosition = _handLocalPosition;
	}

	[BurstCompile]
	private struct IKJob : IJobParallelFor
	{
		public NativeArray<IKConstantInput> constantInput;

		public NativeArray<IKInput> input;

		public NativeArray<IKOutput> output;

		private static readonly Vector3 upperArmLocalPos = new Vector3(0f, 0.1454885f, -0.02598158f);

		private static readonly Vector3 forearmLocalPos = new Vector3(0f, 0.4061671f, 0f);

		private static readonly Vector3 handLocalPos = new Vector3(0f, 0.3816895f, 0f);

		public void Execute(int i)
		{
			Quaternion initRotUpper = constantInput[i].initRotUpper;
			Vector3 vector = upperArmLocalPos;
			Quaternion quaternion = initRotUpper * constantInput[i].initRotLower;
			Vector3 vector2 = vector + initRotUpper * forearmLocalPos;
			Vector3 vector3 = vector2 + quaternion * handLocalPos;
			float num = 0.001f;
			float magnitude = (vector - vector2).magnitude;
			float magnitude2 = (vector2 - vector3).magnitude;
			float max = magnitude + magnitude2 - num;
			Vector3 normalized = (vector3 - vector).normalized;
			Vector3 normalized2 = (vector2 - vector).normalized;
			Vector3 normalized3 = (vector3 - vector2).normalized;
			Vector3 normalized4 = (input[i].targetPos - vector).normalized;
			float num2 = Mathf.Clamp((input[i].targetPos - vector).magnitude, num, max);
			float num3 = Mathf.Acos(Mathf.Clamp(Vector3.Dot(normalized, normalized2), -1f, 1f));
			float num4 = Mathf.Acos(Mathf.Clamp(Vector3.Dot(-normalized2, normalized3), -1f, 1f));
			float num5 = Mathf.Acos(Mathf.Clamp(Vector3.Dot(normalized, normalized4), -1f, 1f));
			float num6 = Mathf.Acos(Mathf.Clamp((magnitude2 * magnitude2 - magnitude * magnitude - num2 * num2) / (-2f * magnitude * num2), -1f, 1f));
			float num7 = Mathf.Acos(Mathf.Clamp((num2 * num2 - magnitude * magnitude - magnitude2 * magnitude2) / (-2f * magnitude * magnitude2), -1f, 1f));
			Vector3 normalized5 = Vector3.Cross(normalized, normalized2).normalized;
			Vector3 normalized6 = Vector3.Cross(normalized, normalized4).normalized;
			Quaternion quaternion2 = Quaternion.AngleAxis((num6 - num3) * 57.29578f, Quaternion.Inverse(initRotUpper) * normalized5);
			Quaternion quaternion3 = Quaternion.AngleAxis((num7 - num4) * 57.29578f, Quaternion.Inverse(quaternion) * normalized5);
			Quaternion quaternion4 = Quaternion.AngleAxis(num5 * 57.29578f, Quaternion.Inverse(initRotUpper) * normalized6);
			Quaternion quaternion5 = constantInput[i].initRotUpper * quaternion4 * quaternion2;
			Quaternion quaternion6 = constantInput[i].initRotLower * quaternion3;
			Quaternion quaternion7 = input[i].bodyRot * constantInput[i].shoulderRot;
			Quaternion quaternion8 = quaternion7 * quaternion5;
			Quaternion quaternion9 = quaternion8 * quaternion6;
			Vector3 handLocalPosition = constantInput[i].bodyPivotPos + input[i].bodyRot * constantInput[i].shoulderPosition + quaternion7 * upperArmLocalPos + quaternion8 * forearmLocalPos + quaternion9 * handLocalPos;
			if (!input[i].usingNewIK)
			{
				output[i] = new IKOutput(quaternion5, quaternion6, handLocalPosition);
				return;
			}
			Vector3 normalized7 = input[i].elbowDir.normalized;
			Vector3 normalized8 = (vector + quaternion5 * forearmLocalPos - vector).normalized;
			Vector3 normalized9 = Vector3.Cross(normalized4, normalized7).normalized;
			quaternion5 = Quaternion.AngleAxis(Vector3.SignedAngle(Vector3.Cross(normalized4, normalized8).normalized, normalized9, normalized4), normalized4) * quaternion5;
			output[i] = new IKOutput(quaternion5, quaternion6, handLocalPosition);
		}
	}

	[BurstCompile]
	private struct IKTransformJob : IJobParallelForTransform
	{
		public NativeArray<Quaternion> transformRotations;

		public NativeArray<Vector3> transformPositions;

		public void Execute(int index, TransformAccess xform)
		{
			if (index % 8 <= 4)
			{
				xform.localRotation = transformRotations[index];
			}
			else
			{
				xform.rotation = transformRotations[index];
			}
			if (index % 8 >= 6)
			{
				xform.localPosition = transformPositions[index];
			}
		}
	}

	[OnEnterPlay_SetNull]
	private static GorillaIKMgr _instance;

	private const int MaxSize = 20;

	private List<GorillaIK> ikList = new List<GorillaIK>(20);

	private int actualListSz;

	private JobHandle jobHandle;

	private JobHandle jobXformHandle;

	private bool firstFrame = true;

	private TransformAccessArray tAA;

	private List<Transform> transformList;

	private bool updatedSinceLastRun;

	public const int tFormCount = 8;

	public static GorillaIK playerIK;

	private float lerpValue = 0.155f;

	private IKJob job;

	private IKTransformJob jobXform;

	public static GorillaIKMgr Instance => _instance;

	private void Awake()
	{
		_instance = this;
		firstFrame = true;
		tAA = new TransformAccessArray(0);
		transformList = new List<Transform>();
		job = new IKJob
		{
			constantInput = new NativeArray<IKConstantInput>(40, Allocator.Persistent),
			input = new NativeArray<IKInput>(40, Allocator.Persistent),
			output = new NativeArray<IKOutput>(40, Allocator.Persistent)
		};
		jobXform = new IKTransformJob
		{
			transformRotations = new NativeArray<Quaternion>(160, Allocator.Persistent),
			transformPositions = new NativeArray<Vector3>(160, Allocator.Persistent)
		};
	}

	private void OnDestroy()
	{
		jobHandle.Complete();
		jobXformHandle.Complete();
		jobXform.transformRotations.Dispose();
		jobXform.transformPositions.Dispose();
		tAA.Dispose();
		job.input.Dispose();
		job.constantInput.Dispose();
		job.output.Dispose();
	}

	public void RegisterIK(GorillaIK ik)
	{
		ikList.Add(ik);
		actualListSz += 2;
		updatedSinceLastRun = true;
		if (job.constantInput.IsCreated)
		{
			SetConstantData(ik, actualListSz - 2);
		}
	}

	public void DeregisterIK(GorillaIK ik)
	{
		int num = ikList.FindIndex((GorillaIK curr) => curr == ik);
		updatedSinceLastRun = true;
		ikList.RemoveAt(num);
		actualListSz -= 2;
		if (job.constantInput.IsCreated)
		{
			for (int num2 = num; num2 < actualListSz; num2++)
			{
				job.constantInput[num2] = job.constantInput[num2 + 2];
			}
		}
	}

	private void SetConstantData(GorillaIK ik, int index)
	{
		job.constantInput[index] = new IKConstantInput
		{
			initRotLower = ik.initialLowerLeft,
			initRotUpper = ik.initialUpperLeft,
			shoulderPosition = new Vector3(-0.018300775f, -0.04206751f, 0.08612572f),
			bodyPivotPos = new Vector3(0f, 0.011406422f, 1.6582015f),
			shoulderRot = new Quaternion(-0.59150106f, 0.3665933f, 0.20795153f, 0.68738055f)
		};
		job.constantInput[index + 1] = new IKConstantInput
		{
			initRotLower = ik.initialLowerRight,
			initRotUpper = ik.initialUpperRight,
			shoulderPosition = new Vector3(0.018300813f, -0.042066876f, 0.08613044f),
			bodyPivotPos = new Vector3(0f, 0.011406422f, 1.6582015f),
			shoulderRot = new Quaternion(-0.591501f, -0.3665933f, -0.20795153f, 0.6873807f)
		};
	}

	private void CopyInput()
	{
		int num = 0;
		int num2 = 0;
		while (num2 < actualListSz)
		{
			GorillaIK gorillaIK = ikList[num2 / 2];
			bool flag = gorillaIK.usingUpdatedIK && SubscriptionManager.GetSubscriptionDetails(gorillaIK.myRig).active;
			if (gorillaIK != playerIK)
			{
				gorillaIK.lerpLeftElbowDirection = Vector3.Lerp(gorillaIK.lerpLeftElbowDirection, gorillaIK.leftElbowDirection, lerpValue);
				gorillaIK.lerpRightElbowDirection = Vector3.Lerp(gorillaIK.lerpRightElbowDirection, gorillaIK.rightElbowDirection, lerpValue);
				gorillaIK.lerpBodyRot = (flag ? Quaternion.Lerp(gorillaIK.lerpBodyRot, gorillaIK.targetBodyRot, lerpValue) : gorillaIK.bodyInitialRot);
			}
			else
			{
				gorillaIK.lerpLeftElbowDirection = gorillaIK.leftElbowDirection;
				gorillaIK.lerpRightElbowDirection = gorillaIK.rightElbowDirection;
				gorillaIK.lerpBodyRot = (flag ? gorillaIK.targetBodyRot : gorillaIK.bodyInitialRot);
			}
			job.input[num2] = new IKInput
			{
				targetPos = gorillaIK.GetShoulderLocalTargetPos_Left(flag),
				elbowDir = gorillaIK.lerpLeftElbowDirection,
				bodyRot = gorillaIK.lerpBodyRot,
				usingNewIK = flag
			};
			job.input[num2 + 1] = new IKInput
			{
				targetPos = gorillaIK.GetShoulderLocalTargetPos_Right(flag),
				elbowDir = gorillaIK.lerpRightElbowDirection,
				bodyRot = gorillaIK.lerpBodyRot,
				usingNewIK = flag
			};
			gorillaIK.ClearOverrides();
			num2 += 2;
			num++;
		}
	}

	private void CopyOutput()
	{
		bool flag = false;
		if (updatedSinceLastRun || tAA.length != ikList.Count * 8)
		{
			flag = true;
			tAA.Dispose();
			transformList.Clear();
		}
		for (int i = 0; i < ikList.Count; i++)
		{
			GorillaIK gorillaIK = ikList[i];
			if (flag || updatedSinceLastRun)
			{
				transformList.Add(gorillaIK.leftUpperArm);
				transformList.Add(gorillaIK.leftLowerArm);
				transformList.Add(gorillaIK.rightUpperArm);
				transformList.Add(gorillaIK.rightLowerArm);
				transformList.Add(gorillaIK.bodyBone);
				transformList.Add(gorillaIK.headBone);
				transformList.Add(gorillaIK.leftHand);
				transformList.Add(gorillaIK.rightHand);
			}
			jobXform.transformRotations[8 * i] = job.output[i * 2].upperArmLocalRot;
			jobXform.transformRotations[8 * i + 1] = job.output[i * 2].lowerArmLocalRot;
			jobXform.transformRotations[8 * i + 2] = job.output[i * 2 + 1].upperArmLocalRot;
			jobXform.transformRotations[8 * i + 3] = job.output[i * 2 + 1].lowerArmLocalRot;
			jobXform.transformRotations[8 * i + 4] = gorillaIK.lerpBodyRot;
			jobXform.transformRotations[8 * i + 5] = gorillaIK.targetHead.rotation;
			jobXform.transformRotations[8 * i + 6] = gorillaIK.targetLeft.rotation;
			jobXform.transformRotations[8 * i + 7] = gorillaIK.targetRight.rotation;
			jobXform.transformPositions[8 * i + 6] = job.output[i * 2].handLocalPosition;
			jobXform.transformPositions[8 * i + 7] = job.output[i * 2 + 1].handLocalPosition;
		}
		if (flag)
		{
			tAA = new TransformAccessArray(transformList.ToArray());
		}
		updatedSinceLastRun = false;
	}

	public void LateUpdate()
	{
		playerIK?.SkeletonUpdate();
		if (!firstFrame)
		{
			jobXformHandle.Complete();
		}
		CopyInput();
		jobHandle = IJobParallelForExtensions.Schedule(job, actualListSz, 20);
		jobHandle.Complete();
		CopyOutput();
		jobXformHandle = jobXform.Schedule(tAA);
		firstFrame = false;
	}

	public static void AddPlayerIK(GorillaIK _playerIK)
	{
		playerIK = _playerIK;
	}
}

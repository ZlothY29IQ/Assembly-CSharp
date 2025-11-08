using System.Collections.Generic;
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
	}

	private struct IKInput
	{
		public Vector3 targetPos;
	}

	private struct IKOutput
	{
		public Quaternion upperArmLocalRot;

		public Quaternion lowerArmLocalRot;

		public IKOutput(Quaternion upperArmLocalRot_, Quaternion lowerArmLocalRot_)
		{
			upperArmLocalRot = upperArmLocalRot_;
			lowerArmLocalRot = lowerArmLocalRot_;
		}
	}

	[BurstCompile]
	private struct IKJob : IJobParallelFor
	{
		public NativeArray<IKConstantInput> constantInput;

		public NativeArray<IKInput> input;

		public NativeArray<IKOutput> output;

		private static readonly Vector3 upperArmLocalPos = new Vector3(-0.0002577677f, 0.1454885f, -0.02598158f);

		private static readonly Vector3 forearmLocalPos = new Vector3(4.204223E-06f, 0.4061671f, -1.043081E-06f);

		private static readonly Vector3 handLocalPos = new Vector3(3.073364E-08f, 0.3816895f, 1.117587E-08f);

		public void Execute(int i)
		{
			Quaternion initRotUpper = constantInput[i].initRotUpper;
			Vector3 vector = upperArmLocalPos;
			Quaternion quaternion = initRotUpper * constantInput[i].initRotLower;
			Vector3 vector2 = vector + initRotUpper * forearmLocalPos;
			Vector3 vector3 = vector2 + quaternion * handLocalPos;
			float num = 0f;
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
			Quaternion upperArmLocalRot_ = constantInput[i].initRotUpper * quaternion4 * quaternion2;
			Quaternion lowerArmLocalRot_ = constantInput[i].initRotLower * quaternion3;
			output[i] = new IKOutput(upperArmLocalRot_, lowerArmLocalRot_);
		}
	}

	[BurstCompile]
	private struct IKTransformJob : IJobParallelForTransform
	{
		public NativeArray<Quaternion> transformRotations;

		public void Execute(int index, TransformAccess xform)
		{
			if (index % 7 <= 3)
			{
				xform.localRotation = transformRotations[index];
			}
			else
			{
				xform.rotation = transformRotations[index];
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

	private int tFormCount = 7;

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
			transformRotations = new NativeArray<Quaternion>(140, Allocator.Persistent)
		};
	}

	private void OnDestroy()
	{
		jobHandle.Complete();
		jobXformHandle.Complete();
		jobXform.transformRotations.Dispose();
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
			for (int i = num; i < actualListSz; i++)
			{
				job.constantInput[i] = job.constantInput[i + 2];
			}
		}
	}

	private void SetConstantData(GorillaIK ik, int index)
	{
		job.constantInput[index] = new IKConstantInput
		{
			initRotLower = ik.initialLowerLeft,
			initRotUpper = ik.initialUpperLeft
		};
		job.constantInput[index + 1] = new IKConstantInput
		{
			initRotLower = ik.initialLowerRight,
			initRotUpper = ik.initialUpperRight
		};
	}

	private void CopyInput()
	{
		int num = 0;
		int num2 = 0;
		while (num2 < actualListSz)
		{
			GorillaIK gorillaIK = ikList[num2 / 2];
			job.input[num2] = new IKInput
			{
				targetPos = gorillaIK.GetShoulderLocalTargetPos_Left()
			};
			job.input[num2 + 1] = new IKInput
			{
				targetPos = gorillaIK.GetShoulderLocalTargetPos_Right()
			};
			gorillaIK.ClearOverrides();
			num2 += 2;
			num++;
		}
	}

	private void CopyOutput()
	{
		bool flag = false;
		if (updatedSinceLastRun || tAA.length != ikList.Count * 7)
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
				transformList.Add(gorillaIK.headBone);
				transformList.Add(gorillaIK.leftHand);
				transformList.Add(gorillaIK.rightHand);
			}
			jobXform.transformRotations[tFormCount * i] = job.output[i * 2].upperArmLocalRot;
			jobXform.transformRotations[tFormCount * i + 1] = job.output[i * 2].lowerArmLocalRot;
			jobXform.transformRotations[tFormCount * i + 2] = job.output[i * 2 + 1].upperArmLocalRot;
			jobXform.transformRotations[tFormCount * i + 3] = job.output[i * 2 + 1].lowerArmLocalRot;
			jobXform.transformRotations[tFormCount * i + 4] = gorillaIK.targetHead.rotation;
			jobXform.transformRotations[tFormCount * i + 5] = gorillaIK.targetLeft.rotation;
			jobXform.transformRotations[tFormCount * i + 6] = gorillaIK.targetRight.rotation;
		}
		if (flag)
		{
			tAA = new TransformAccessArray(transformList.ToArray());
		}
		updatedSinceLastRun = false;
	}

	public void LateUpdate()
	{
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
}
